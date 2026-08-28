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
/// SPIKE (throwaway): validates an inbound Signed HTTP Request (SHR) Proof-of-Possession token by
/// re-hosting MISE.AuthN's <see cref="SignedHttpRequestHandler"/>-based validation (see MISE
/// <c>PopTokenHandlerValidationRule</c> + <c>SignedHttpRequestValidationContextFactory</c>).
/// Scope: app-only tokens, timestamp-only (ts) validation. Server nonce is intentionally NOT wired.
/// </summary>
internal sealed class ShrPopValidationService
{
    // SignedHttpRequestHandler is stateless and thread-safe; MISE also holds a single instance.
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
    /// Maps the operator-configurable <see cref="PopValidationOptions"/> onto Wilson's
    /// <see cref="SignedHttpRequestValidationParameters"/>, mirroring MISE's
    /// <c>SignedHttpRequestValidationContextFactory.CreateParametersFromOptions</c>. With the options at
    /// their defaults (no <c>Sidecar:PopValidation</c> section) this reproduces the original hard-coded
    /// behavior: m/u/p/ts ON; q/h OFF; b OFF; unsigned headers/query accepted; 5-minute lifetime.
    /// </summary>
    internal static SignedHttpRequestValidationParameters CreateValidationParameters(PopValidationOptions options) => new()
    {
        ValidateM = options.ValidateM,
        ValidateU = options.ValidateU,
        ValidateP = options.ValidateP,
        ValidateTs = options.ValidateTs,
        ValidateH = options.ValidateH,
        ValidateQ = options.ValidateQ,

        // Body-hash (b) stays OFF and is intentionally NOT operator-configurable: validating it requires
        // buffering/reading the request body, and MISE never surfaces it. See the design doc deferrals.
        ValidateB = false,

        AcceptUnsignedHeaders = options.AcceptUnsignedHeaders,
        AcceptUnsignedQueryParameters = options.AcceptUnsignedQueryParameters,
        ValidatePresentClaims = options.ValidatePresentClaims,
        ClaimsToValidateWhenPresent = options.ClaimsToValidateWhenPresent,
        SignedHttpRequestLifetime = options.SignedHttpRequestLifetime,

        // Server nonce is out of scope (timestamp-only): NonceValidatorAsync stays unset. jku key
        // resolution is left at Wilson defaults (AllowResolvingPopKeyFromJku = false) - no egress.
    };

    public async Task<ShrPopValidationResult> ValidateAsync(
        string encodedSignedHttpRequest,
        HttpRequest request,
        CancellationToken cancellationToken)
    {
        // (1) Request-line contract -> Wilson HttpRequestData (binds m/u/p).
        if (!PopHttpRequestFactory.TryCreate(request, out var requestData, out var contractError))
        {
            return ShrPopValidationResult.Fail(contractError!);
        }

        // (3) Reuse the SAME TokenValidationParameters JwtBearer built from AzureAd config - one source
        //     of truth for issuer/audience/signing-keys. Clone so the shared instance is never mutated
        //     per-request (mirrors MISE PopTokenTypeInfo.GetClonedValidationParameters).
        JwtBearerOptions bearerOptions =
            _jwtBearerOptionsMonitor.Get(JwtBearerDefaults.AuthenticationScheme);
        TokenValidationParameters? bearerTvp = bearerOptions.TokenValidationParameters;
        if (bearerTvp is null)
        {
            return ShrPopValidationResult.Fail("Bearer TokenValidationParameters are not configured.");
        }

        TokenValidationParameters accessTokenValidationParameters = bearerTvp.Clone();

        // Ensure the cloned TVP can resolve the embedded AT's signing keys via live JWKS. In production
        // Microsoft.Identity.Web copies options.ConfigurationManager onto the shared TVP lazily, inside
        // the JwtBearer handler's OnMessageReceived event (IdentityOptionsHelpers.InitializeJwtBearerEvents),
        // which only fires when the JwtBearer scheme is authenticated first. Copying it explicitly here
        // removes that ordering/timing dependency so inner-AT signature validation always has a key source.
        accessTokenValidationParameters.ConfigurationManager ??=
            bearerOptions.ConfigurationManager as BaseConfigurationManager;

        var validationContext = new SignedHttpRequestValidationContext(
            encodedSignedHttpRequest,
            requestData,
            accessTokenValidationParameters,
            CreateValidationParameters(_sidecarOptionsMonitor.CurrentValue.PopValidation));

        SignedHttpRequestValidationResult result;
        try
        {
            result = await s_handler
                .ValidateSignedHttpRequestAsync(validationContext, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // Wilson throws for malformed SHR/JWT input; treat as a validation failure.
            return ShrPopValidationResult.Fail($"SHR validation threw: {ex.Message}");
        }

        if (!result.IsValid)
        {
            return ShrPopValidationResult.Fail(result.Exception?.Message ?? "SHR validation failed.");
        }

        // The SHR handler validated BOTH the outer PoP signature AND the inner access token via the
        // reused TVP (issuer/audience/expiry/signing-key all enforced on the embedded AT here).
        TokenValidationResult accessTokenResult = result.AccessTokenValidationResult;
        if (accessTokenResult is null ||
            !accessTokenResult.IsValid ||
            accessTokenResult.SecurityToken is not JsonWebToken validatedAccessToken ||
            accessTokenResult.ClaimsIdentity is null)
        {
            return ShrPopValidationResult.Fail("Embedded access token validation failed.");
        }

        // SPIKE (throwaway): enforce the declared app-only scope. SHR/inner-AT validation above proves
        // the embedded token is authentic but does NOT distinguish an app-only (client-credentials)
        // token from a delegated/user one. Without this guard a valid, cnf-bound *delegated* token
        // wrapped in an SHR would be accepted as PoP - and because the /Validate PoP branch skips the
        // delegated-scope (scp) gate, it would sail past the AzureAd:Scopes check a Bearer caller faces.
        // Reject anything that is not app-only; fail-closed (the handler logs the reason and 401s).
        if (!IsAppOnlyToken(validatedAccessToken))
        {
            return ShrPopValidationResult.Fail(
                "The embedded access token is not an app-only token; inbound SHR PoP is scoped to app-only (client-credentials) tokens.");
        }

        return ShrPopValidationResult.Success(validatedAccessToken, accessTokenResult.ClaimsIdentity);
    }

    /// <summary>
    /// Distinguishes an app-only (client-credentials) token from a delegated/user token. Per Entra
    /// token semantics a delegated token carries the <c>scp</c> (scope) claim while an app-only token
    /// does not; the optional <c>idtyp</c> claim, when emitted, states this authoritatively
    /// (<c>"app"</c> vs <c>"user"</c>). A token is treated as app-only only when it has no <c>scp</c>
    /// AND (no <c>idtyp</c>, or <c>idtyp == "app"</c>). <c>idtyp</c> presence is not required because it
    /// is an optional claim many tenants do not emit. Entra always emits <c>scp</c> as a space-delimited
    /// string and the token is signed/validated, so a caller cannot evade this by reshaping the claim.
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
