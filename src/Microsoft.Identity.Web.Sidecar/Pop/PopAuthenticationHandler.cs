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

/// <summary>Options for the PoP authentication scheme.</summary>
internal sealed class PopAuthenticationSchemeOptions : AuthenticationSchemeOptions
{
}

/// <summary>
/// ASP.NET Core authentication handler for inbound SHR PoP. It recognises the "PoP" Authorization
/// scheme, runs <see cref="ShrPopValidationService"/>, and on success builds a
/// <see cref="ClaimsPrincipal"/> from the validated inner access token. The Bearer (JwtBearer) handler
/// is untouched; the <c>/Validate</c> endpoint opts into a policy that accepts both schemes.
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
            Logger.PopValidationFailed(error);
            return AuthenticateResult.Fail(error);
        }

        // Expose the validated inner access token to the /Validate endpoint seam.
        Context.Items[PopConstants.ValidatedAccessTokenItemKey] = result.ValidatedAccessToken;

        var principal = new ClaimsPrincipal(result.ClaimsIdentity!);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);
        return AuthenticateResult.Success(ticket);
    }
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
