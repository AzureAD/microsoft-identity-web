using System.Security.Claims;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace Microsoft.Identity.Web;

/// <summary>
/// Handles authentication challenges for Blazor Server components.
/// Provides functionality for incremental consent and Conditional Access scenarios.
/// </summary>
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
        var currentUri = navigation.Uri;

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
        var challengeUrl = $"/authentication/login?returnUrl={Uri.EscapeDataString(currentUri)}" +
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
