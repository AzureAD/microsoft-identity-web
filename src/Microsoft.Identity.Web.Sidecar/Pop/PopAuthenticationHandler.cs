// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Web.Sidecar.Logging;

namespace Microsoft.Identity.Web.Sidecar.Pop;

/// <summary>SPIKE (throwaway): options for the PoP authentication scheme.</summary>
internal sealed class PopAuthenticationSchemeOptions : AuthenticationSchemeOptions
{
}

/// <summary>
/// SPIKE (throwaway): ASP.NET Core authentication handler for inbound SHR PoP. This is the
/// sidecar-native equivalent of MISE's PoP protocol parser + validation rule: it recognises the
/// "PoP" Authorization scheme, runs <see cref="ShrPopValidationService"/>, and on success produces a
/// <see cref="ClaimsPrincipal"/> from the validated inner access token. The existing Bearer
/// (JwtBearer) handler is untouched; the /Validate endpoint opts into a
/// multi-scheme authorization policy that accepts both Bearer and PoP.
/// </summary>
internal sealed class PopAuthenticationHandler : AuthenticationHandler<PopAuthenticationSchemeOptions>
{
    private readonly ShrPopValidationService _validationService;

    public PopAuthenticationHandler(
        IOptionsMonitor<PopAuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ShrPopValidationService validationService)
        : base(options, logger, encoder)
    {
        _validationService = validationService;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        string authorization = Request.Headers.Authorization.ToString();
        if (string.IsNullOrEmpty(authorization))
        {
            return AuthenticateResult.NoResult();
        }

        if (!AuthenticationHeaderValue.TryParse(authorization, out AuthenticationHeaderValue? headerValue) ||
            !string.Equals(headerValue.Scheme, PopConstants.SchemeName, StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrEmpty(headerValue.Parameter))
        {
            return AuthenticateResult.NoResult();
        }

        ShrPopValidationResult result = await _validationService
            .ValidateAsync(headerValue.Parameter, Request, Context.RequestAborted)
            .ConfigureAwait(false);

        if (!result.Succeeded)
        {
            string error = result.Error ?? "PoP validation failed.";

            // Record the reason server-side (parity with JwtBearer's TokenValidationFailed) so the
            // failure is diagnosable from logs while the challenge response stays generic.
            Logger.PopValidationFailed(error);
            return AuthenticateResult.Fail(error);
        }

        // Expose the validated inner access token to the /Validate endpoint seam.
        Context.Items[PopConstants.ValidatedAccessTokenItemKey] = result.ValidatedAccessToken;

        var principal = new ClaimsPrincipal(result.ClaimsIdentity!);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);
        return AuthenticateResult.Success(ticket);
    }

    // SPIKE (throwaway): mirror JwtBearer's failure surface for the PoP scheme. Stock JwtBearer
    // answers an auth failure with 401 + "WWW-Authenticate: Bearer error=...". The /Validate policy
    // lists BOTH schemes, so on failure ASP.NET Core challenges each: the JwtBearer handler emits its
    // Bearer challenge (unchanged), and this override emits the matching "WWW-Authenticate: PoP" so a
    // PoP caller is told PoP is the scheme in play instead of only (misleadingly) Bearer. The header
    // is appended (never overwritten), and only when the caller actually used the PoP scheme - so a
    // plain Bearer or no-credential 401 still advertises Bearer alone, exactly as before.
    //
    // We deliberately emit only error="invalid_token" and no verbose error_description: unlike
    // Bearer's IncludeErrorDetails default, we avoid echoing internal SHR validation text into a
    // response header (matching MISE's DPoP response-header creator, which keeps error_description
    // generic). The precise reason is available in the server-side log; the description is an easy
    // future knob, and the nonce challenge params (nonce=/error="use_dpop_nonce") are additive later.
    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        if (AuthenticationHeaderValue.TryParse(Request.Headers.Authorization.ToString(), out AuthenticationHeaderValue? headerValue) &&
            string.Equals(headerValue.Scheme, PopConstants.SchemeName, StringComparison.OrdinalIgnoreCase))
        {
            Response.StatusCode = StatusCodes.Status401Unauthorized;
            Response.Headers.Append(
                Microsoft.Net.Http.Headers.HeaderNames.WWWAuthenticate,
                $"{PopConstants.SchemeName} error=\"invalid_token\"");
        }

        return Task.CompletedTask;
    }
}
