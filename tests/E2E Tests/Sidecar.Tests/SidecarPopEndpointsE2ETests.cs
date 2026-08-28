// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#if !FROM_GITHUB_ACTION

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.Sidecar.Models;
using Microsoft.Identity.Web.TokenCacheProviders.InMemory;
using Xunit;

namespace Sidecar.Tests;

/// <summary>
/// SPIKE (throwaway) END-TO-END pipeline tests for inbound SHR PoP validation - the design-doc §9
/// "E2E (pipeline)" deliverable. Unlike <see cref="ShrPopValidationTests"/> (which mints tokens from an
/// in-process mock IdP), these exercise the WHOLE real path: a REAL app-only token is acquired from
/// Entra, cnf-bound and wrapped in an ARM-shaped Signed HTTP Request by MSAL (via MSIdWeb's
/// WithSignedHttpRequestProofOfPossession), then validated by the sidecar through its REAL AzureAd
/// discovery/JWKS path (configured on <see cref="SidecarApiFactory"/>).
///
/// Coverage mirrors the Bearer E2E surface (<see cref="SidecarEndpointsE2ETests"/> +
/// <see cref="ValidateEndpointTestsExtended"/>) and adds the PoP-specific guarantees that only a real
/// token can prove end-to-end:
///   - Happy paths: real PoP → Protocol "PoP"; real Bearer control → Protocol "Bearer".
///   - Bad credential: garbage PoP → 401 (clean, not 500) with a PoP challenge.
///   - Request-line BINDING against a real SHR: tampered method, tampered URI, and missing request-line
///     headers all → 401 (the anti-replay guarantee, proven against an ESTS-minted SHR + real key).
///   - Scheme ISOLATION with real tokens: a real SHR presented as Bearer → 401, and a real Bearer token
///     presented as PoP → 401 (the two schemes never cross-validate).
///   - No credential → 401 advertising Bearer.
///
/// GATING: compiled out of GitHub Actions (FROM_GITHUB_ACTION) and, like the sibling Bearer E2E suite,
/// requires lab-tenant credentials to RUN:
///   - the certificate CN=LabAuth.MSIDLab.com present in a reachable store (see CertificateStorePath), and
///   - the client app (<see cref="TestClientApplication"/>) registered and permitted to acquire an
///     SHR PoP (cnf-bound) app-only token for <see cref="AgentApplicationScope"/>.
/// Intended to run behind a pipeline stage gate that supplies those credentials; without them the
/// token-acquisition step throws (an environment failure), exactly like the sibling E2E suite.
/// </summary>
public class SidecarPopEndpointsE2ETests : IClassFixture<SidecarApiFactory>
{
    private readonly SidecarApiFactory _factory;

    public SidecarPopEndpointsE2ETests(SidecarApiFactory factory) => _factory = factory;

    const string TenantId = "10c419d4-4a50-45b2-aa4e-919fb84df24f";         // Replace with your tenant ID
    const string AgentApplication = "aab5089d-e764-47e3-9f28-cc11c2513821"; // Sidecar's AzureAd ClientId/Audience (see SidecarApiFactory)
    const string TestClientApplication = "825940df-c1fb-4604-8104-02965f55b1ee"; // Replace with the client used for app-only calls
    const string Instance = "https://login.microsoftonline.com/";           // Entra ID authority instance
    const string CertificateStorePath = "CurrentUser/My";                   // Local dev: CurrentUser avoids the LocalMachine private-key ACL/elevation requirement (pipeline may use LocalMachine/My)
    const string CertificateDistinguishedName = "CN=LabAuth.MSIDLab.com";   // Replace with the certificate subject name
    static readonly string AgentApplicationScope = $"api://{AgentApplication}/.default";

    // An ARM-shaped request line the SHR is signed over: this binds the SHR's m/u/p. The INNER token's
    // audience is still the sidecar's configured AzureAd audience (AgentApplication), which is what the
    // sidecar validates the embedded access token against - the SHR host/path need not equal that.
    private const string SignedUri =
        "https://management.azure.com/subscriptions/00000000-0000-0000-0000-000000000000/resourceGroups/rg1/providers/Microsoft.Compute/virtualMachines/vm1?api-version=2023-07-01";
    private const string SignedMethod = "POST";

    // A DIFFERENT absolute URI (different resource/path) used to prove path binding: presenting this as
    // original-uri against an SHR signed over SignedUri must fail the 'p' (and here 'u' is same host) check.
    private const string DifferentUri =
        "https://management.azure.com/subscriptions/00000000-0000-0000-0000-000000000000/resourceGroups/rg1/providers/Microsoft.Compute/virtualMachines/vm2?api-version=2023-07-01";

    private static readonly JsonSerializerOptions s_webOptions = new(JsonSerializerDefaults.Web);

    // ---------------------------------------------------------------------------------------------
    // Happy paths
    // ---------------------------------------------------------------------------------------------

    [Fact]
    public async Task Validate_RealEntraAppOnlyShrPop_Returns200_AndProtocolPoPAsync()
    {
        // Acquire a REAL app-only SHR PoP token from Entra, signed over the ARM-shaped request, and send
        // it with the matching request-line headers. The sidecar validates the outer SHR + the inner AT
        // (via the reused Bearer TVP over live JWKS) and reports Protocol "PoP".
        string shr = await AcquireRealShrAsync(SignedMethod, SignedUri);

        var response = await SendToValidateAsync("PoP", shr, SignedMethod, SignedUri);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ValidateAuthorizationHeaderResult>(s_webOptions);
        Assert.NotNull(result);
        Assert.Equal("PoP", result!.Protocol);
        Assert.NotNull(result.Claims);
    }

    [Fact]
    public async Task Validate_RealEntraBearer_StillReturns200_AndProtocolBearerAsync()
    {
        // The multi-scheme [Bearer, PoP] policy must not disturb Bearer. A plain app-only token (no
        // PoPConfiguration) sent on the Bearer scheme still validates and reports Protocol "Bearer".
        string bearer = await AcquireRealBearerAsync();

        var response = await SendToValidateAsync("Bearer", bearer, originalMethod: null, originalUri: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ValidateAuthorizationHeaderResult>(s_webOptions);
        Assert.NotNull(result);
        Assert.Equal("Bearer", result!.Protocol);
        Assert.NotNull(result.Claims);
    }

    // ---------------------------------------------------------------------------------------------
    // Bad credential (mirrors the Bearer bad-token E2E test)
    // ---------------------------------------------------------------------------------------------

    [Fact]
    public async Task Validate_RealEntraInvalidPopToken_ReturnsUnauthorizedAsync()
    {
        // A syntactically-bogus PoP credential must fail closed with a clean 401 (never a 500) and carry
        // the generic PoP challenge alongside Bearer.
        var response = await SendToValidateAsync("PoP", "not-a-valid-shr", SignedMethod, SignedUri);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        string wwwAuthenticate = response.Headers.WwwAuthenticate.ToString();
        Assert.Contains("PoP", wwwAuthenticate, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("invalid_token", wwwAuthenticate, StringComparison.OrdinalIgnoreCase);
    }

    // ---------------------------------------------------------------------------------------------
    // Request-line binding against a REAL SHR (the anti-replay guarantee)
    // ---------------------------------------------------------------------------------------------

    [Fact]
    public async Task Validate_RealEntraShrPop_TamperedMethod_ReturnsUnauthorizedAsync()
    {
        // Real SHR signed over POST, but the caller claims the method was GET -> 'm' binding fails -> 401.
        string shr = await AcquireRealShrAsync(SignedMethod, SignedUri);

        var response = await SendToValidateAsync("PoP", shr, originalMethod: "GET", originalUri: SignedUri);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Validate_RealEntraShrPop_TamperedUri_ReturnsUnauthorizedAsync()
    {
        // Real SHR signed over vm1, but the caller claims a different URI (vm2) -> 'p' binding fails -> 401.
        string shr = await AcquireRealShrAsync(SignedMethod, SignedUri);

        var response = await SendToValidateAsync("PoP", shr, originalMethod: SignedMethod, originalUri: DifferentUri);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Validate_RealEntraShrPop_MissingRequestLineHeaders_ReturnsUnauthorizedAsync()
    {
        // Real SHR, but no original-method / original-uri headers -> the sidecar cannot reconstruct the
        // signed request line, so validation fails closed -> 401.
        string shr = await AcquireRealShrAsync(SignedMethod, SignedUri);

        var response = await SendToValidateAsync("PoP", shr, originalMethod: null, originalUri: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ---------------------------------------------------------------------------------------------
    // Scheme isolation with REAL tokens (the two schemes never cross-validate)
    // ---------------------------------------------------------------------------------------------

    [Fact]
    public async Task Validate_RealEntraShrPop_PresentedOnBearerScheme_ReturnsUnauthorizedAsync()
    {
        // A real SHR is not a plain access token. Presented on the Bearer scheme, JwtBearer rejects it
        // (wrong signer/issuer/audience) and the PoP handler never runs -> 401. Proves an SHR cannot slip
        // through as a Bearer token.
        string shr = await AcquireRealShrAsync(SignedMethod, SignedUri);

        var response = await SendToValidateAsync("Bearer", shr, originalMethod: null, originalUri: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Validate_RealEntraBearerToken_PresentedOnPopScheme_ReturnsUnauthorizedAsync()
    {
        // A plain Bearer access token is not a Signed HTTP Request. Presented on the PoP scheme, SHR
        // validation fails (it is not a signed SHR) -> 401. Proves a captured Bearer token cannot be
        // replayed through the PoP path.
        string bearer = await AcquireRealBearerAsync();

        var response = await SendToValidateAsync("PoP", bearer, SignedMethod, SignedUri);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ---------------------------------------------------------------------------------------------
    // No credential (mirrors the Bearer no-auth E2E test + design-doc "no credential -> advertise Bearer")
    // ---------------------------------------------------------------------------------------------

    [Fact]
    public async Task Validate_WithoutAuthorization_ReturnsUnauthorized_ChallengesBearerAsync()
    {
        var response = await _factory.CreateClient().GetAsync("/Validate");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains("Bearer", response.Headers.WwwAuthenticate.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    // ---------------------------------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------------------------------

    // Acquires a REAL app-only SHR PoP token from Entra, cnf-bound and signed over (method, uri).
    private static async Task<string> AcquireRealShrAsync(string method, string uri)
    {
        ITokenAcquisition tokenAcquisition = CreateAppTokenAcquisition();
        using var signedRequest = new HttpRequestMessage(new HttpMethod(method), new Uri(uri));

        AuthenticationResult authResult = await tokenAcquisition.GetAuthenticationResultForAppAsync(
            AgentApplicationScope,
            tokenAcquisitionOptions: new TokenAcquisitionOptions
            {
                PoPConfiguration = new PoPAuthenticationConfiguration(signedRequest),
            });

        // Sanity: ESTS/MSAL actually issued a PoP (SHR) token, not a plain Bearer.
        Assert.Equal("PoP", authResult.TokenType, ignoreCase: true);
        return authResult.AccessToken;
    }

    // Acquires a plain app-only Bearer access token from Entra (no PoP).
    private static async Task<string> AcquireRealBearerAsync()
    {
        ITokenAcquisition tokenAcquisition = CreateAppTokenAcquisition();
        AuthenticationResult authResult =
            await tokenAcquisition.GetAuthenticationResultForAppAsync(AgentApplicationScope);
        return authResult.AccessToken;
    }

    // Sends GET /Validate with the given Authorization scheme/credential and optional request-line headers.
    private async Task<HttpResponseMessage> SendToValidateAsync(
        string scheme, string credential, string? originalMethod, string? originalUri)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/Validate");
        request.Headers.Authorization = new AuthenticationHeaderValue(scheme, credential);

        // Request-line contract headers (identical to the MISE container: original-method / original-uri).
        // The app supplies these because /Validate receives a validation request, not the signed request.
        if (originalMethod is not null)
        {
            request.Headers.TryAddWithoutValidation("original-method", originalMethod);
        }

        if (originalUri is not null)
        {
            request.Headers.TryAddWithoutValidation("original-uri", originalUri);
        }

        return await _factory.CreateClient().SendAsync(request);
    }

    // Builds an app-only ITokenAcquisition against the real lab tenant, using the same certificate the
    // sibling E2E suite uses (mirrors SidecarEndpointsE2ETests.GetAuthorizationHeaderToCallTheSideCarAsync).
    private static ITokenAcquisition CreateAppTokenAcquisition()
    {
        ServiceCollection services = new();
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        services.AddSingleton<IConfiguration>(configuration);
        configuration["Instance"] = Instance;
        configuration["TenantId"] = TenantId;
        configuration["ClientId"] = TestClientApplication;
        configuration["SendX5C"] = "true";
        configuration["ClientCredentials:0:SourceType"] = "StoreWithDistinguishedName";
        configuration["ClientCredentials:0:CertificateStorePath"] = CertificateStorePath;
        configuration["ClientCredentials:0:CertificateDistinguishedName"] = CertificateDistinguishedName;

        services.AddTokenAcquisition().AddHttpClient().AddInMemoryTokenCaches();
        services.Configure<MicrosoftIdentityApplicationOptions>(configuration);
        IServiceProvider serviceProvider = services.BuildServiceProvider();

        return serviceProvider.GetRequiredService<ITokenAcquisition>();
    }
}

#endif
