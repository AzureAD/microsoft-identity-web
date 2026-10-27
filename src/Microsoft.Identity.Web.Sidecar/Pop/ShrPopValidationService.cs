// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Web.Sidecar.Configuration;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols.SignedHttpRequest;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.Identity.Web.Sidecar.Pop;

/// <summary>
/// Validates an inbound Signed HTTP Request (SHR) Proof-of-Possession token. Scope: app-only (client-credentials)
/// tokens with timestamp-only (<c>ts</c>) freshness. Server nonce is intentionally not wired.
/// </summary>
internal sealed class ShrPopValidationService
{
    // SignedHttpRequestHandler is stateless and thread-safe, so a single shared instance is used.
    private static readonly SignedHttpRequestHandler s_handler = new();

    private readonly IOptionsMonitor<JwtBearerOptions> _jwtBearerOptionsMonitor;
    private readonly IOptionsMonitor<SidecarOptions> _sidecarOptionsMonitor;

    public ShrPopValidationService(
        IOptionsMonitor<JwtBearerOptions> jwtBearerOptionsMonitor,
        IOptionsMonitor<SidecarOptions> sidecarOptionsMonitor)
    {
        _jwtBearerOptionsMonitor = jwtBearerOptionsMonitor;
        _sidecarOptionsMonitor = sidecarOptionsMonitor;
    }

    /// <summary>
    /// Maps the operator-configurable <see cref="PopValidationOptions"/> onto the identity model's
    /// <see cref="SignedHttpRequestValidationParameters"/>. With the options at their defaults (no
    /// <c>Sidecar:PopValidation</c> section) this yields the secure default: m/u/p/ts on; q/h/b off;
    /// unsigned headers/query accepted; five-minute lifetime.
    /// </summary>
    internal static SignedHttpRequestValidationParameters CreateValidationParameters(PopValidationOptions options) => new()
    {
        ValidateM = options.ValidateM,
        ValidateU = options.ValidateU,
        ValidateP = options.ValidateP,
        ValidateQ = options.ValidateQ,

        // Timestamp (ts) validation is always on as the only replay/freshness guard in the nonce-less design.
        ValidateTs = true,

        // Header (h) and body-hash (b) binding are intentionally not configurable: the sidecar
        // receives only the request line (method + URI), not the signed headers or body, so h and b stay
        // off and unsigned headers are accepted.
        ValidateH = false,
        ValidateB = false,
        AcceptUnsignedHeaders = true,

        AcceptUnsignedQueryParameters = options.AcceptUnsignedQueryParameters,
        ValidatePresentClaims = options.ValidatePresentClaims,
        ClaimsToValidateWhenPresent = options.ClaimsToValidateWhenPresent,
        SignedHttpRequestLifetime = options.SignedHttpRequestLifetime,
    };

    public async Task<ShrPopValidationResult> ValidateAsync(
        string encodedSignedHttpRequest,
        HttpRequest request,
        CancellationToken cancellationToken)
    {
        // Request-line contract -> HttpRequestData (binds m/u/p).
        if (!PopHttpRequestFactory.TryCreate(request, out var requestData, out var contractError))
        {
            return ShrPopValidationResult.Fail(contractError!);
        }

        // Reuse the TokenValidationParameters JwtBearer built from AzureAd config - one source of truth
        // for issuer/audience/signing keys. Clone so the shared instance is never mutated per-request.
        JwtBearerOptions bearerOptions =
            _jwtBearerOptionsMonitor.Get(JwtBearerDefaults.AuthenticationScheme);
        TokenValidationParameters? bearerTvp = bearerOptions.TokenValidationParameters;
        if (bearerTvp is null)
        {
            return ShrPopValidationResult.Fail("The Bearer scheme's TokenValidationParameters are not configured.");
        }

        TokenValidationParameters accessTokenValidationParameters = bearerTvp.Clone();

        // Ensure the cloned TVP can resolve the embedded token's signing keys via live JWKS. The stock
        // JwtBearer post-configure copies ConfigurationManager onto the shared TVP lazily inside its
        // OnMessageReceived event, which only fires when the Bearer scheme authenticates first. Copying
        // it here removes that ordering dependency; if it were ever null, inner-token validation fails closed.
        accessTokenValidationParameters.ConfigurationManager ??=
            bearerOptions.ConfigurationManager as BaseConfigurationManager;

        SignedHttpRequestValidationResult result;
        try
        {
            var validationContext = new SignedHttpRequestValidationContext(
                encodedSignedHttpRequest,
                requestData,
                accessTokenValidationParameters,
                CreateValidationParameters(_sidecarOptionsMonitor.CurrentValue.PopValidation));

            result = await s_handler
                .ValidateSignedHttpRequestAsync(validationContext, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // The identity model throws for malformed SHR/JWT input or invalid validation parameters;
            // treat as a validation failure so the request fails closed with a 401 rather than a 500.
            return ShrPopValidationResult.Fail($"SHR validation threw: {ex.Message}");
        }

        if (!result.IsValid)
        {
            return ShrPopValidationResult.Fail(result.Exception?.Message ?? "SHR validation failed.");
        }

        // The SHR handler validated both the outer PoP signature and the inner access token via the
        // reused TVP (issuer/audience/expiry/signing key all enforced on the embedded token).
        TokenValidationResult accessTokenResult = result.AccessTokenValidationResult;
        if (accessTokenResult is null ||
            !accessTokenResult.IsValid ||
            accessTokenResult.SecurityToken is not JsonWebToken validatedAccessToken ||
            accessTokenResult.ClaimsIdentity is null)
        {
            return ShrPopValidationResult.Fail("Embedded access token validation failed.");
        }

        // Enforce app-only scope. SHR/inner-token validation proves the embedded token is authentic but
        // does not distinguish an app-only (client-credentials) token from a delegated one. Without this
        // guard a cnf-bound delegated token wrapped in an SHR would be accepted and, because the PoP path
        // does not apply the delegated-scope (scp) gate, would bypass the AzureAd:Scopes check a Bearer
        // caller faces. Reject anything that is not app-only; fail closed.
        if (!IsAppOnlyToken(validatedAccessToken))
        {
            return ShrPopValidationResult.Fail(
                "The embedded access token is not an app-only token; inbound SHR PoP is scoped to app-only (client-credentials) tokens.");
        }

        return ShrPopValidationResult.Success(validatedAccessToken, accessTokenResult.ClaimsIdentity);
    }

    /// <summary>
    /// Distinguishes an app-only (client-credentials) token from a delegated/user token. A delegated
    /// token carries the <c>scp</c> (scope) claim while an app-only token does not; the optional
    /// <c>idtyp</c> claim, when emitted, states this authoritatively (<c>"app"</c> vs <c>"user"</c>). A
    /// token is treated as app-only only when it has no <c>scp</c> AND (no <c>idtyp</c>, or
    /// <c>idtyp == "app"</c>). <c>idtyp</c> is not required because it is an optional claim many tenants
    /// do not emit. The token is signed and validated, so a caller cannot evade this by reshaping claims.
    /// </summary>
    private static bool IsAppOnlyToken(JsonWebToken token)
    {
        if (token.TryGetPayloadValue<string>("scp", out _))
        {
            return false;
        }

        if (token.TryGetPayloadValue<string>("idtyp", out string? idtyp) &&
            !string.Equals(idtyp, "app", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }
}
