// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Web.Sidecar.Models;
using Xunit;

namespace Sidecar.Tests;

/// <summary>
/// SPIKE (throwaway): round-trip + negative tests for inbound SHR PoP validation in the sidecar.
/// Positive: a valid ARM-shaped SHR (app-only AT, cnf-bound to a test key) returns 200 + Protocol "PoP".
/// Negatives: tampered method/uri, expired ts, bad signature, and (proving the reused AzureAd TVP)
/// wrong-issuer / expired embedded access token are all rejected.
/// </summary>
public class ShrPopValidationTests : IClassFixture<PopSidecarApiFactory>
{
    private const string ArmUri =
        "https://management.azure.com/subscriptions/00000000-0000-0000-0000-000000000000/resourceGroups/rg1/providers/Microsoft.Compute/virtualMachines/vm1?api-version=2023-07-01";
    private const string ArmMethod = "POST";

    private static readonly JsonSerializerOptions s_webOptions = new(JsonSerializerDefaults.Web);

    private readonly PopSidecarApiFactory _factory;

    public ShrPopValidationTests(PopSidecarApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Validate_WithValidShrPop_Returns200_AndProtocolPoPAsync()
    {
        // Arrange
        string accessToken = PopTestCrypto.CreateAccessToken(
            PopTestCrypto.TestIssuer, PopTestCrypto.TestAudience, DateTime.UtcNow.AddMinutes(10),
            PopTestCrypto.PopSigningKey, PopTestCrypto.PopKeyId);
        string shr = PopTestCrypto.CreateSignedHttpRequest(
            accessToken, ArmMethod, ArmUri, PopTestCrypto.PopSigningKey, PopTestCrypto.PopKeyId);
        var client = _factory.CreateClient();

        // Act
        var response = await client.SendAsync(BuildPopRequest(shr, ArmUri, ArmMethod));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ValidateAuthorizationHeaderResult>(s_webOptions);
        Assert.NotNull(result);
        Assert.Equal("PoP", result!.Protocol);
        Assert.Equal(accessToken, result.Token);
        Assert.NotNull(result.Claims);
    }

    [Fact]
    public async Task Validate_FullLoop_TokenAcquiredFromMockIdp_Returns200_AndProtocolPoPAsync()
    {
        // Arrange: full real-world round-trip.
        //  1. The caller presents its PoP public key (req_cnf) to the mock IdP's token endpoint.
        //  2. The IdP returns an app-only access token whose cnf binds that key, signed by the key it
        //     publishes via JWKS.
        //  3. The caller mints an SHR over the real request, signed by its PoP private key.
        //  4. The sidecar validates the embedded token through its live JwtBearer discovery/JWKS path
        //     (no statically injected key), then the PoP signature + m/u/p/ts.
        string reqCnf = PopTestCrypto.BuildReqCnf(PopTestCrypto.PopSigningKey, PopTestCrypto.PopKeyId);
        string accessToken = await _factory.Idp.AcquireAppOnlyTokenAsync(reqCnf);
        string shr = PopTestCrypto.CreateSignedHttpRequest(
            accessToken, ArmMethod, ArmUri, PopTestCrypto.PopSigningKey, PopTestCrypto.PopKeyId);
        var client = _factory.CreateClient();

        // Act
        var response = await client.SendAsync(BuildPopRequest(shr, ArmUri, ArmMethod));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ValidateAuthorizationHeaderResult>(s_webOptions);
        Assert.NotNull(result);
        Assert.Equal("PoP", result!.Protocol);
        Assert.Equal(accessToken, result.Token);
        Assert.NotNull(result.Claims);
    }

    [Fact]
    public async Task Validate_WithDelegatedEmbeddedToken_ReturnsUnauthorizedAsync()
    {
        // Arrange: a fully valid, cnf-bound SHR whose EMBEDDED token is a DELEGATED (user) token
        // (carries 'scp' / idtyp "user"). The outer SHR and the inner signature are otherwise valid,
        // so this isolates the app-only guard: inbound SHR PoP is scoped to app-only, so the sidecar
        // must reject a delegated token wrapped in an SHR.
        string delegatedToken = PopTestCrypto.CreateDelegatedAccessToken(
            PopTestCrypto.TestIssuer, PopTestCrypto.TestAudience, DateTime.UtcNow.AddMinutes(10),
            PopTestCrypto.PopSigningKey, PopTestCrypto.PopKeyId);
        string shr = PopTestCrypto.CreateSignedHttpRequest(
            delegatedToken, ArmMethod, ArmUri, PopTestCrypto.PopSigningKey, PopTestCrypto.PopKeyId);
        var client = _factory.CreateClient();

        // Act
        var response = await client.SendAsync(BuildPopRequest(shr, ArmUri, ArmMethod));

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Validate_WithTamperedMethod_ReturnsUnauthorizedAsync()
    {
        // Arrange: SHR signs POST, caller claims GET.
        string accessToken = PopTestCrypto.CreateAccessToken(
            PopTestCrypto.TestIssuer, PopTestCrypto.TestAudience, DateTime.UtcNow.AddMinutes(10),
            PopTestCrypto.PopSigningKey, PopTestCrypto.PopKeyId);
        string shr = PopTestCrypto.CreateSignedHttpRequest(
            accessToken, ArmMethod, ArmUri, PopTestCrypto.PopSigningKey, PopTestCrypto.PopKeyId);
        var client = _factory.CreateClient();

        // Act
        var response = await client.SendAsync(BuildPopRequest(shr, ArmUri, "GET"));

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Validate_WithTamperedUri_ReturnsUnauthorizedAsync()
    {
        // Arrange: SHR signs the ARM URI, caller claims a different path.
        string accessToken = PopTestCrypto.CreateAccessToken(
            PopTestCrypto.TestIssuer, PopTestCrypto.TestAudience, DateTime.UtcNow.AddMinutes(10),
            PopTestCrypto.PopSigningKey, PopTestCrypto.PopKeyId);
        string shr = PopTestCrypto.CreateSignedHttpRequest(
            accessToken, ArmMethod, ArmUri, PopTestCrypto.PopSigningKey, PopTestCrypto.PopKeyId);
        var client = _factory.CreateClient();

        const string tamperedUri = "https://management.azure.com/subscriptions/00000000-0000-0000-0000-000000000000/resourceGroups/rg1/providers/Microsoft.Compute/virtualMachines/EVIL?api-version=2023-07-01";

        // Act
        var response = await client.SendAsync(BuildPopRequest(shr, tamperedUri, ArmMethod));

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Validate_WithExpiredTs_ReturnsUnauthorizedAsync()
    {
        // Arrange: embed a signed ts 10 minutes in the past (lifetime is 5 minutes).
        long expiredTs = DateTimeOffset.UtcNow.AddMinutes(-10).ToUnixTimeSeconds();
        string accessToken = PopTestCrypto.CreateAccessToken(
            PopTestCrypto.TestIssuer, PopTestCrypto.TestAudience, DateTime.UtcNow.AddMinutes(10),
            PopTestCrypto.PopSigningKey, PopTestCrypto.PopKeyId);
        string shr = PopTestCrypto.CreateSignedHttpRequest(
            accessToken, ArmMethod, ArmUri, PopTestCrypto.PopSigningKey, PopTestCrypto.PopKeyId, expiredTs);
        var client = _factory.CreateClient();

        // Act
        var response = await client.SendAsync(BuildPopRequest(shr, ArmUri, ArmMethod));

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Validate_WithBadSignature_ReturnsUnauthorizedAsync()
    {
        // Arrange: cnf advertises the PoP key, but the SHR is signed by an untrusted key.
        string accessToken = PopTestCrypto.CreateAccessToken(
            PopTestCrypto.TestIssuer, PopTestCrypto.TestAudience, DateTime.UtcNow.AddMinutes(10),
            PopTestCrypto.PopSigningKey, PopTestCrypto.PopKeyId);
        string shr = PopTestCrypto.CreateSignedHttpRequest(
            accessToken, ArmMethod, ArmUri, PopTestCrypto.UntrustedKey, PopTestCrypto.PopKeyId);
        var client = _factory.CreateClient();

        // Act
        var response = await client.SendAsync(BuildPopRequest(shr, ArmUri, ArmMethod));

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Validate_WithWrongIssuerEmbeddedToken_ReturnsUnauthorizedAsync()
    {
        // Arrange (objective 3 evidence): embedded AT issuer is not the reused TVP's ValidIssuer.
        string accessToken = PopTestCrypto.CreateAccessToken(
            "https://sts.windows.net/evil-tenant/", PopTestCrypto.TestAudience, DateTime.UtcNow.AddMinutes(10),
            PopTestCrypto.PopSigningKey, PopTestCrypto.PopKeyId);
        string shr = PopTestCrypto.CreateSignedHttpRequest(
            accessToken, ArmMethod, ArmUri, PopTestCrypto.PopSigningKey, PopTestCrypto.PopKeyId);
        var client = _factory.CreateClient();

        // Act
        var response = await client.SendAsync(BuildPopRequest(shr, ArmUri, ArmMethod));

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Validate_WithExpiredEmbeddedToken_ReturnsUnauthorizedAsync()
    {
        // Arrange (objective 3 evidence): the SHR ts is fresh, but the embedded AT is expired, so the
        // reused TVP's lifetime validation must reject it.
        string accessToken = PopTestCrypto.CreateAccessToken(
            PopTestCrypto.TestIssuer, PopTestCrypto.TestAudience, DateTime.UtcNow.AddMinutes(-10),
            PopTestCrypto.PopSigningKey, PopTestCrypto.PopKeyId);
        string shr = PopTestCrypto.CreateSignedHttpRequest(
            accessToken, ArmMethod, ArmUri, PopTestCrypto.PopSigningKey, PopTestCrypto.PopKeyId);
        var client = _factory.CreateClient();

        // Act
        var response = await client.SendAsync(BuildPopRequest(shr, ArmUri, ArmMethod));

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Validate_WithMissingOriginalUriHeader_ReturnsUnauthorizedAsync()
    {
        // Arrange: valid SHR, but the request-line contract header is absent.
        string accessToken = PopTestCrypto.CreateAccessToken(
            PopTestCrypto.TestIssuer, PopTestCrypto.TestAudience, DateTime.UtcNow.AddMinutes(10),
            PopTestCrypto.PopSigningKey, PopTestCrypto.PopKeyId);
        string shr = PopTestCrypto.CreateSignedHttpRequest(
            accessToken, ArmMethod, ArmUri, PopTestCrypto.PopSigningKey, PopTestCrypto.PopKeyId);
        var client = _factory.CreateClient();

        // Act
        var response = await client.SendAsync(BuildPopRequest(shr, originalUri: null, originalMethod: ArmMethod));

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Validate_WithoutAuthorization_ChallengesBearer_NotPoPAsync()
    {
        // Arrange: no Authorization header -> the selector must forward to the unchanged Bearer handler.
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/Validate");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        string wwwAuthenticate = response.Headers.WwwAuthenticate.ToString();
        Assert.Contains("Bearer", wwwAuthenticate, StringComparison.OrdinalIgnoreCase);
        // A no-credential request must NOT advertise PoP: the PoP challenge fires only when the caller
        // actually used the PoP scheme, so the Bearer failure surface is unchanged from today.
        Assert.DoesNotContain("PoP", wwwAuthenticate, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Validate_WithInvalidPop_Challenges_WwwAuthenticatePoPAsync()
    {
        // Arrange: a PoP request that fails validation (SHR signed by an untrusted key). Like the
        // Bearer path (which returns 401 + WWW-Authenticate: Bearer), the PoP path must answer with a
        // 401 that advertises its OWN scheme via WWW-Authenticate: PoP - not the wrong scheme.
        string accessToken = PopTestCrypto.CreateAccessToken(
            PopTestCrypto.TestIssuer, PopTestCrypto.TestAudience, DateTime.UtcNow.AddMinutes(10),
            PopTestCrypto.PopSigningKey, PopTestCrypto.PopKeyId);
        string shr = PopTestCrypto.CreateSignedHttpRequest(
            accessToken, ArmMethod, ArmUri, PopTestCrypto.UntrustedKey, PopTestCrypto.PopKeyId);
        var client = _factory.CreateClient();

        // Act
        var response = await client.SendAsync(BuildPopRequest(shr, ArmUri, ArmMethod));

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains("PoP", response.Headers.WwwAuthenticate.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Validate_WithValidBearerToken_Returns200_AndProtocolBearerAsync()
    {
        // Arrange (hard constraint: the Bearer path must keep working, unchanged, through the new
        // [Bearer, PoP] policy). A plain app-only access token - NO SHR wrapper - signed by the key the
        // mock IdP publishes via JWKS, presented under the Bearer scheme.
        string accessToken = PopTestCrypto.CreateAccessToken(
            PopTestCrypto.TestIssuer, PopTestCrypto.TestAudience, DateTime.UtcNow.AddMinutes(10),
            PopTestCrypto.PopSigningKey, PopTestCrypto.PopKeyId);
        var client = _factory.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Get, "/Validate");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        // Act
        var response = await client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ValidateAuthorizationHeaderResult>(s_webOptions);
        Assert.NotNull(result);
        Assert.Equal("Bearer", result!.Protocol);
        Assert.Equal(accessToken, result.Token);
        Assert.NotNull(result.Claims);
    }

    [Fact]
    public async Task Validate_WithMalformedPopToken_ReturnsUnauthorized_NotServerErrorAsync()
    {
        // Arrange: a non-JWT garbage string under the PoP scheme. The validator wraps Wilson in a
        // try/catch, so this must surface as a clean 401 - never a 500.
        var client = _factory.CreateClient();

        // Act
        var response = await client.SendAsync(BuildPopRequest("not-a-real-shr-jwt", ArmUri, ArmMethod));

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Validate_WithWrongAudienceEmbeddedToken_ReturnsUnauthorizedAsync()
    {
        // Arrange (objective 3 evidence): embedded AT audience is not the reused TVP's ValidAudience.
        string accessToken = PopTestCrypto.CreateAccessToken(
            PopTestCrypto.TestIssuer, "api://wrong-audience", DateTime.UtcNow.AddMinutes(10),
            PopTestCrypto.PopSigningKey, PopTestCrypto.PopKeyId);
        string shr = PopTestCrypto.CreateSignedHttpRequest(
            accessToken, ArmMethod, ArmUri, PopTestCrypto.PopSigningKey, PopTestCrypto.PopKeyId);
        var client = _factory.CreateClient();

        // Act
        var response = await client.SendAsync(BuildPopRequest(shr, ArmUri, ArmMethod));

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Validate_WithEmbeddedTokenMissingCnf_ReturnsUnauthorizedAsync()
    {
        // Arrange: the embedded AT carries no cnf claim, so the SHR handler has no confirmation key to
        // bind the outer signature to - PoP binding must be rejected.
        string accessToken = PopTestCrypto.CreateAccessTokenWithoutCnf(
            PopTestCrypto.TestIssuer, PopTestCrypto.TestAudience, DateTime.UtcNow.AddMinutes(10));
        string shr = PopTestCrypto.CreateSignedHttpRequest(
            accessToken, ArmMethod, ArmUri, PopTestCrypto.PopSigningKey, PopTestCrypto.PopKeyId);
        var client = _factory.CreateClient();

        // Act
        var response = await client.SendAsync(BuildPopRequest(shr, ArmUri, ArmMethod));

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Validate_WithNonAbsoluteOriginalUri_ReturnsUnauthorizedAsync()
    {
        // Arrange: valid SHR, but the original-uri header is a relative path. PopHttpRequestFactory
        // requires an absolute URI (Uri.TryCreate(..., Absolute)) and must reject otherwise.
        string accessToken = PopTestCrypto.CreateAccessToken(
            PopTestCrypto.TestIssuer, PopTestCrypto.TestAudience, DateTime.UtcNow.AddMinutes(10),
            PopTestCrypto.PopSigningKey, PopTestCrypto.PopKeyId);
        string shr = PopTestCrypto.CreateSignedHttpRequest(
            accessToken, ArmMethod, ArmUri, PopTestCrypto.PopSigningKey, PopTestCrypto.PopKeyId);
        var client = _factory.CreateClient();

        // Act
        var response = await client.SendAsync(BuildPopRequest(shr, "/relative/path", ArmMethod));

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Validate_WithMissingOriginalMethodHeader_ReturnsUnauthorizedAsync()
    {
        // Arrange: valid SHR, but the original-method request-line contract header is absent.
        string accessToken = PopTestCrypto.CreateAccessToken(
            PopTestCrypto.TestIssuer, PopTestCrypto.TestAudience, DateTime.UtcNow.AddMinutes(10),
            PopTestCrypto.PopSigningKey, PopTestCrypto.PopKeyId);
        string shr = PopTestCrypto.CreateSignedHttpRequest(
            accessToken, ArmMethod, ArmUri, PopTestCrypto.PopSigningKey, PopTestCrypto.PopKeyId);
        var client = _factory.CreateClient();

        // Act
        var response = await client.SendAsync(BuildPopRequest(shr, ArmUri, originalMethod: null));

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Validate_WithInvalidPop_RecordsFailureReasonInServerLogsAsync()
    {
        // Arrange: capture server-side logs on an isolated host, then send a PoP request that fails
        // validation (SHR signed by an untrusted key). JwtBearer logs TokenValidationFailed at
        // Information; the PoP path must likewise record the reason server-side (the design-doc claim)
        // even though the 401 challenge returned to the caller stays generic.
        var logs = new CapturingLoggerProvider();
        using var loggingFactory = _factory.WithWebHostBuilder(builder =>
            builder.ConfigureLogging(logging => logging.AddProvider(logs)));

        string accessToken = PopTestCrypto.CreateAccessToken(
            PopTestCrypto.TestIssuer, PopTestCrypto.TestAudience, DateTime.UtcNow.AddMinutes(10),
            PopTestCrypto.PopSigningKey, PopTestCrypto.PopKeyId);
        string shr = PopTestCrypto.CreateSignedHttpRequest(
            accessToken, ArmMethod, ArmUri, PopTestCrypto.UntrustedKey, PopTestCrypto.PopKeyId);
        var client = loggingFactory.CreateClient();

        // Act
        var response = await client.SendAsync(BuildPopRequest(shr, ArmUri, ArmMethod));

        // Assert: 401 to the caller, and the specific reason recorded server-side at Information (parity
        // with JwtBearer's TokenValidationFailed) so the failure is diagnosable from logs.
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains(logs.Entries, e =>
            e.EventId.Name == "Pop_ValidationFailed" &&
            e.Level == LogLevel.Information &&
            e.Message.Contains("PoP validation failed", StringComparison.OrdinalIgnoreCase));
    }

    private static HttpRequestMessage BuildPopRequest(string shr, string? originalUri, string? originalMethod)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/Validate");
        request.Headers.Authorization = new AuthenticationHeaderValue("PoP", shr);

        if (originalUri is not null)
        {
            request.Headers.TryAddWithoutValidation("original-uri", originalUri);
        }

        if (originalMethod is not null)
        {
            request.Headers.TryAddWithoutValidation("original-method", originalMethod);
        }

        return request;
    }

    /// <summary>
    /// SPIKE (throwaway): minimal in-memory <see cref="ILoggerProvider"/> that records every emitted
    /// log entry so a test can assert the PoP handler wrote its failure reason to the server-side log.
    /// </summary>
    private sealed class CapturingLoggerProvider : ILoggerProvider
    {
        public ConcurrentQueue<(LogLevel Level, EventId EventId, string Message)> Entries { get; } = new();

        public ILogger CreateLogger(string categoryName) => new CapturingLogger(Entries);

        public void Dispose()
        {
        }

        private sealed class CapturingLogger : ILogger
        {
            private readonly ConcurrentQueue<(LogLevel Level, EventId EventId, string Message)> _entries;

            public CapturingLogger(ConcurrentQueue<(LogLevel Level, EventId EventId, string Message)> entries) =>
                _entries = entries;

            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
                _entries.Enqueue((logLevel, eventId, formatter(state, exception)));
        }
    }
}
