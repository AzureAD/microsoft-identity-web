// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
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
            oAuthChallengeProperties.RedirectUri = redirectUri;

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
    }
}
