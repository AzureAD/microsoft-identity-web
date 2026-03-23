// NOTE: This file is included in Microsoft.Identity.Web package (v3.3.0+).
// This copy is maintained for AI skill reference and documentation purposes.
// For production use, reference the NuGet package directly.

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Microsoft.Identity.Web;

/// <summary>
/// Extension methods for mapping login and logout endpoints that support
/// incremental consent and Conditional Access scenarios.
/// </summary>
public static class LoginLogoutEndpointRouteBuilderExtensions
{
    /// <summary>
    /// Maps login and logout endpoints under the current route group.
    /// The login endpoint supports incremental consent via scope, loginHint, domainHint, and claims parameters.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <returns>The endpoint convention builder for further configuration.</returns>
    public static IEndpointConventionBuilder MapLoginAndLogout(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("");

        // Enhanced login endpoint that supports incremental consent and Conditional Access
        group.MapGet("/login", (
            string? returnUrl,
            string? scope,
            string? loginHint,
            string? domainHint,
            string? claims) =>
        {
            var properties = GetAuthProperties(returnUrl);

            // Add scopes if provided (for incremental consent)
            if (!string.IsNullOrEmpty(scope))
            {
                var scopes = scope.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                properties.SetParameter(OpenIdConnectParameterNames.Scope, scopes);
            }

            // Add login hint (pre-fills username)
            if (!string.IsNullOrEmpty(loginHint))
            {
                properties.SetParameter(OpenIdConnectParameterNames.LoginHint, loginHint);
            }

            // Add domain hint (skips home realm discovery)
            if (!string.IsNullOrEmpty(domainHint))
            {
                properties.SetParameter(OpenIdConnectParameterNames.DomainHint, domainHint);
            }

            // Add claims challenge (for Conditional Access / step-up auth)
            if (!string.IsNullOrEmpty(claims))
            {
                properties.Items["claims"] = claims;
            }

            return TypedResults.Challenge(properties, [OpenIdConnectDefaults.AuthenticationScheme]);
        })
        .AllowAnonymous();

        group.MapPost("/logout", async (HttpContext context) =>
        {
            string? returnUrl = null;
            if (context.Request.HasFormContentType)
            {
                var form = await context.Request.ReadFormAsync();
                returnUrl = form["ReturnUrl"];
            }

            return TypedResults.SignOut(GetAuthProperties(returnUrl),
                [CookieAuthenticationDefaults.AuthenticationScheme, OpenIdConnectDefaults.AuthenticationScheme]);
        })
        .DisableAntiforgery();

        return group;
    }

    private static AuthenticationProperties GetAuthProperties(string? returnUrl)
    {
        const string pathBase = "/";
        if (string.IsNullOrEmpty(returnUrl)) returnUrl = pathBase;
        else if (returnUrl.StartsWith("//", StringComparison.Ordinal)) returnUrl = pathBase; // Prevent protocol-relative redirects
        else if (!Uri.IsWellFormedUriString(returnUrl, UriKind.Relative)) returnUrl = new Uri(returnUrl, UriKind.Absolute).PathAndQuery;
        else if (returnUrl[0] != '/') returnUrl = $"{pathBase}{returnUrl}";
        return new AuthenticationProperties { RedirectUri = returnUrl };
    }
}
