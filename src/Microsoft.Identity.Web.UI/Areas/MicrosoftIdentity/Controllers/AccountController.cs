// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Microsoft.Identity.Web.UI.Areas.MicrosoftIdentity.Controllers
{
    /// <summary>
    /// Controller used in web apps to manage accounts.
    /// </summary>
    [NonController]
    [AllowAnonymous]
    [Area("MicrosoftIdentity")]
    [Route("[area]/[controller]/[action]")]
    public class AccountController : Controller
    {
        private readonly IOptionsMonitor<MicrosoftIdentityOptions> _optionsMonitor;

        /// <summary>
        /// Constructor of <see cref="AccountController"/> from <see cref="MicrosoftIdentityOptions"/>
        /// This constructor is used by dependency injection.
        /// </summary>
        /// <param name="microsoftIdentityOptionsMonitor">Configuration options.</param>
        public AccountController(IOptionsMonitor<MicrosoftIdentityOptions> microsoftIdentityOptionsMonitor)
        {
            _optionsMonitor = microsoftIdentityOptionsMonitor;
        }

        /// <summary>
        /// Handles user sign in.
        /// </summary>
        /// <param name="scheme">Authentication scheme.</param>
        /// <param name="redirectUri">Redirect URI.</param>
        /// <param name="loginHint">Login hint (user's email address).</param>
        /// <param name="domainHint">Domain hint.</param>
        /// <returns>Challenge generating a redirect to Azure AD to sign in the user.</returns>
        [HttpGet("{scheme?}")]
        public IActionResult SignIn(
            [FromRoute] string scheme,
            [FromQuery] string redirectUri,
            [FromQuery] string? loginHint = null,
            [FromQuery] string? domainHint = null)
        {
            scheme ??= OpenIdConnectDefaults.AuthenticationScheme;
            string redirect;
            if (!string.IsNullOrEmpty(redirectUri) && Url.IsLocalUrl(redirectUri))
            {
                redirect = redirectUri;
            }
            else
            {
                redirect = Url.Content("~/")!;
            }
            var authProps = new AuthenticationProperties { RedirectUri = redirect };
            if (!string.IsNullOrEmpty(loginHint))
            {
                authProps.Parameters[Constants.LoginHint] = loginHint;
            }

            if (!string.IsNullOrEmpty(domainHint))
            {
                authProps.Parameters[Constants.DomainHint] = domainHint;
            }
            return Challenge(
                authProps,
                scheme);
        }

        /// <summary>
        /// Challenges the user.
        /// </summary>
        /// <param name="redirectUri">Redirect URI.</param>
        /// <param name="scope">Scopes to request.</param>
        /// <param name="loginHint">Login hint.</param>
        /// <param name="domainHint">Domain hint.</param>
        /// <param name="claims">Claims.</param>
        /// <param name="policy">AAD B2C policy.</param>
        /// <param name="scheme">Authentication scheme.</param>
        /// <returns>Challenge generating a redirect to Azure AD to sign in the user.</returns>
        [HttpGet("{scheme?}")]
        public IActionResult Challenge(
            string redirectUri,
            string scope,
            string loginHint,
            string domainHint,
            string claims,
            string policy,
            [FromRoute] string scheme)
        {
            scheme ??= OpenIdConnectDefaults.AuthenticationScheme;
            Dictionary<string, string?> items = new Dictionary<string, string?>
            {
                { Constants.Claims, claims },
                { Constants.Policy, policy },
            };
            Dictionary<string, object?> parameters = new Dictionary<string, object?>
            {
                { Constants.LoginHint, loginHint },
                { Constants.DomainHint, domainHint },
            };

            OAuthChallengeProperties oAuthChallengeProperties = new OAuthChallengeProperties(items, parameters);
            if (scope != null)
            {
                oAuthChallengeProperties.Scope = scope.Split(" ");
            }

            // Validate the redirect URI. Accept:
            //   * Local URLs (e.g. "/path") — the common MVC pattern.
            //   * Same-origin absolute URLs (coerced to PathAndQuery) — required because
            //     Microsoft.Identity.Web's own MicrosoftIdentityConsentAndConditionalAccessHandler
            //     passes NavigationManager.Uri (always absolute) for Blazor Server step-up
            //     consent and for Razor Pages / MVC step-up. Rejecting those would break the
            //     canonical [AuthorizeForScopes] / MsalUiRequiredException flow.
            // Reject everything else. The post-sign-in 302 honors AuthenticationProperties.RedirectUri
            // as-is (CookieAuthenticationHandler does not enforce IsLocalUrl), so the check must
            // happen here. This closes the open-redirect class of bug matching the SignIn action's
            // own IsLocalUrl gate added in PR #1219.
            string? safeRedirect = null;
            if (!string.IsNullOrEmpty(redirectUri))
            {
                if (Url.IsLocalUrl(redirectUri) && !IsPercentEncodedSlashBypass(redirectUri))
                {
                    safeRedirect = redirectUri;
                }
                else if (Uri.TryCreate(redirectUri, UriKind.Absolute, out var absolute)
                         && IsSameOrigin(absolute, HttpContext.Request))
                {
                    // PathAndQuery of a same-origin absolute URL can still begin with "//" or "/\"
                    // for inputs like "http://victim.app//evil.com/x" (Uri.Host="victim.app",
                    // PathAndQuery="//evil.com/x") — a protocol-relative URL that CookieAuthenticationHandler
                    // would emit verbatim in its Location header. Re-run IsLocalUrl on the coerced value
                    // to reject those shapes.
                    var candidate = absolute.PathAndQuery;
                    if (Url.IsLocalUrl(candidate) && !IsPercentEncodedSlashBypass(candidate))
                    {
                        safeRedirect = candidate;
                    }
                }
            }

            oAuthChallengeProperties.RedirectUri = safeRedirect ?? Url.Content("~/")!;

            return Challenge(
                oAuthChallengeProperties,
                scheme);
        }

        /// <summary>
        /// Handles the user sign-out.
        /// </summary>
        /// <param name="scheme">Authentication scheme.</param>
        /// <returns>Sign out result.</returns>
        [HttpGet("{scheme?}")]
        public IActionResult SignOut(
            [FromRoute] string scheme)
        {
            if (AppServicesAuthenticationInformation.IsAppServicesAadAuthenticationEnabled)
            {
                if (AppServicesAuthenticationInformation.LogoutUrl != null)
                {
                    return LocalRedirect(AppServicesAuthenticationInformation.LogoutUrl);
                }
                return Ok();
            }
            else
            {
                scheme ??= OpenIdConnectDefaults.AuthenticationScheme;
                var callbackUrl = Url.Page("/Account/SignedOut", pageHandler: null, values: null, protocol: Request.Scheme);
                return SignOut(
                     new AuthenticationProperties
                     {
                         RedirectUri = callbackUrl,
                     },
                     CookieAuthenticationDefaults.AuthenticationScheme,
                     scheme);
            }
        }

        /// <summary>
        /// In B2C applications handles the Reset password policy.
        /// </summary>
        /// <param name="scheme">Authentication scheme.</param>
        /// <returns>Challenge generating a redirect to Azure AD B2C.</returns>
        [HttpGet("{scheme?}")]
        public IActionResult ResetPassword([FromRoute] string scheme)
        {
            scheme ??= OpenIdConnectDefaults.AuthenticationScheme;

            var redirectUrl = Url.Content("~/");
            var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
            properties.Items[Constants.Policy] = _optionsMonitor.Get(scheme).ResetPasswordPolicyId;
            return Challenge(properties, scheme);
        }

        /// <summary>
        /// In B2C applications, handles the Edit Profile policy.
        /// </summary>
        /// <param name="scheme">Authentication scheme.</param>
        /// <returns>Challenge generating a redirect to Azure AD B2C.</returns>
        [HttpGet("{scheme?}")]
        public async Task<IActionResult> EditProfile([FromRoute] string scheme)
        {
            scheme ??= OpenIdConnectDefaults.AuthenticationScheme;
            var authenticated = await HttpContext.AuthenticateAsync(scheme).ConfigureAwait(false);
            if (!authenticated.Succeeded)
            {
                return Challenge(scheme);
            }

            var redirectUrl = Url.Content("~/");
            var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
            properties.Items[Constants.Policy] = _optionsMonitor.Get(scheme).EditProfilePolicyId;
            return Challenge(properties, scheme);
        }

        /// <summary>
        /// Returns <c>true</c> when <paramref name="absolute"/> has the same origin (scheme + host + port)
        /// as <paramref name="request"/>. Used by <c>Challenge</c> to accept same-origin absolute redirect URIs
        /// without opening an open-redirect sink.
        /// </summary>
        private static bool IsSameOrigin(Uri absolute, HttpRequest request)
        {
            if (!string.Equals(absolute.Scheme, request.Scheme, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (!string.Equals(absolute.Host, request.Host.Host, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            int requestPort = request.Host.Port ?? (request.IsHttps ? 443 : 80);
            return absolute.Port == requestPort;
        }

        /// <summary>
        /// Defense-in-depth: reject paths whose first segment starts with a percent-encoded
        /// forward or backward slash (<c>%2f</c>/<c>%5c</c>). Browsers per RFC 3986 treat these
        /// as literal path characters, but misconfigured reverse proxies (NGINX, IIS ARR, F5)
        /// can decode them into <c>//</c> or <c>/\</c> when rewriting the <c>Location</c>
        /// header, reopening the protocol-relative bypass that this controller otherwise
        /// closes. Comparison is case-insensitive because the RFC 3986 encoding is
        /// hex-case-insensitive.
        /// </summary>
        private static bool IsPercentEncodedSlashBypass(string path) =>
            path.StartsWith("/%2f", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/%5c", StringComparison.OrdinalIgnoreCase);
    }
}
