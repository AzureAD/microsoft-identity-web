// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Identity.Web;

/// <summary>
/// Handles authentication challenges for Blazor Server components.
/// Provides functionality for incremental consent and Conditional Access scenarios.
/// </summary>
/// <remarks>
/// This handler is designed specifically for Blazor Server scenarios where authentication
/// challenges need to be initiated from component code. It supports incremental consent
/// (requesting additional scopes) and Conditional Access (handling step-up authentication).
/// Use this in combination with <see cref="LoginLogoutEndpointRouteBuilderExtensions.MapLoginAndLogout"/>
/// to enable seamless authentication flows in Blazor Server applications.
/// </remarks>
public class BlazorAuthenticationChallengeHandler(
    NavigationManager navigation,
    AuthenticationStateProvider authenticationStateProvider,
    IConfiguration configuration)
{
    private const string MsaTenantId = "9188040d-6c67-4c5b-b112-36a304b66dad";

    /// <summary>
    /// Gets the current user's authentication state.
    /// </summary>
    public async Task<ClaimsPrincipal> GetUserAsync()
    {
        var authState = await authenticationStateProvider.GetAuthenticationStateAsync();
        return authState.User;
    }

    /// <summary>
    /// Checks if the current user is authenticated.
    /// </summary>
    public async Task<bool> IsAuthenticatedAsync()
    {
        var user = await GetUserAsync();
        return user.Identity?.IsAuthenticated == true;
    }

    /// <summary>
    /// Handles exceptions that may require user re-authentication.
    /// Returns true if a challenge was initiated, false otherwise.
    /// </summary>
    public async Task<bool> HandleExceptionAsync(Exception exception)
    {
        var challengeException = exception as MicrosoftIdentityWebChallengeUserException
            ?? exception.InnerException as MicrosoftIdentityWebChallengeUserException;

        if (challengeException != null)
        {
            var user = await GetUserAsync();
            ChallengeUser(user, challengeException.Scopes, challengeException.MsalUiRequiredException?.Claims);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Initiates a challenge to authenticate the user or request additional consent.
    /// </summary>
    public void ChallengeUser(ClaimsPrincipal user, string[]? scopes = null, string? claims = null)
    {
        // NavigationManager.Uri is always absolute, but the /login endpoint mapped by
        // MapLoginAndLogout validates returnUrl with RedirectUriHelper.IsLocalUrl, which
        // rejects absolute URLs and falls back to "/" — so passing the absolute URI loses
        // the user's page after the consent round-trip. Send the app-local PathAndQuery
        // instead (preserves any path base, drops the fragment — matching the MVC
        // AccountController.Challenge coercion of same-origin absolute URLs).
        //
        // Defensive re-check: PathAndQuery can still begin with "//" or "/\" for a request
        // path like "//evil.example/x", which a downstream Location header would treat as
        // protocol-relative. Re-run IsLocalUrl on the coerced value and fall back to "/",
        // mirroring the endpoint's own validation.
        var returnUrl = new Uri(navigation.Uri).PathAndQuery;
        if (!RedirectUriHelper.IsLocalUrl(returnUrl))
        {
            returnUrl = "/";
        }

        // Build scopes string (add OIDC scopes)
        var allScopes = (scopes ?? [])
            .Union(["openid", "offline_access", "profile"])
            .Distinct();
        var scopeString = Uri.EscapeDataString(string.Join(" ", allScopes));

        // Get login hint from user claims
        var loginHint = Uri.EscapeDataString(GetLoginHint(user));

        // Get domain hint
        var domainHint = Uri.EscapeDataString(GetDomainHint(user));

        // Build the challenge URL
        var challengeUrl = $"/authentication/login?returnUrl={Uri.EscapeDataString(returnUrl)}" +
                          $"&scope={scopeString}" +
                          $"&loginHint={loginHint}" +
                          $"&domainHint={domainHint}";

        // Add claims if present (for Conditional Access)
        if (!string.IsNullOrEmpty(claims))
        {
            challengeUrl += $"&claims={Uri.EscapeDataString(claims)}";
        }

        navigation.NavigateTo(challengeUrl, forceLoad: true);
    }

    /// <summary>
    /// Initiates a challenge with scopes from configuration.
    /// </summary>
    [RequiresUnreferencedCode("Binding strongly typed objects from configuration values may require generating dynamic code at runtime.")]
    [RequiresDynamicCode("Binding strongly typed objects from configuration values may require generating dynamic code at runtime.")]
    public async Task ChallengeUserWithConfiguredScopesAsync(string configurationSection)
    {
        var user = await GetUserAsync();
        var scopes = configuration.GetSection(configurationSection).Get<string[]>();
        ChallengeUser(user, scopes);
    }

    private static string GetLoginHint(ClaimsPrincipal user)
    {
        return user.FindFirst("preferred_username")?.Value ??
               user.FindFirst("login_hint")?.Value ??
               string.Empty;
    }

    private static string GetDomainHint(ClaimsPrincipal user)
    {
        var tenantId = user.FindFirst("http://schemas.microsoft.com/identity/claims/tenantid")?.Value ??
                      user.FindFirst("tid")?.Value;

        if (string.IsNullOrEmpty(tenantId))
            return "organizations";

        // MSA tenant
        if (tenantId == MsaTenantId)
            return "consumers";

        return "organizations";
    }
}
