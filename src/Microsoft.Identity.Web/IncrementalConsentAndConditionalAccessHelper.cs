// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Identity.Client;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Helper methods to handle incremental consent and conditional access in
    /// a Web app.
    /// </summary>
    internal static class IncrementalConsentAndConditionalAccessHelper
    {
        /// <summary>
        /// Can the exception be solved by re-signing-in the users?.
        /// </summary>
        /// <param name="ex">Exception from which the decision will be made.</param>
        /// <returns>Returns <c>true</c> if the issue can be solved by signing-in
        /// the user, and <c>false</c>, otherwise.</returns>
        public static bool CanBeSolvedByReSignInOfUser(MsalUiRequiredException ex)
        {
            if (ex == null)
            {
                throw new ArgumentNullException(nameof(ex));
            }

            // ex.ErrorCode != MsalUiRequiredException.UserNullError indicates a cache problem.
            // When calling an [Authenticate]-decorated controller we expect an authenticated
            // user and therefore its account should be in the cache. However in the case of an
            // InMemoryCache, the cache could be empty if the server was restarted. This is why
            // the null_user exception is thrown.
            return ex.ErrorCode.ContainsAny(new[] { MsalError.UserNullError, MsalError.InvalidGrantError });
        }

        /// <summary>
        /// Build authentication properties needed for incremental consent.
        /// </summary>
        /// <param name="scopes">Scopes to request.</param>
        /// <param name="ex"><see cref="MsalUiRequiredException"/> instance.</param>
        /// <param name="user">User.</param>
        /// <param name="userflow">Userflow being invoked for AAD B2C.</param>
        /// <returns>AuthenticationProperties.</returns>
        public static AuthenticationProperties BuildAuthenticationProperties(
            string[]? scopes,
            MsalUiRequiredException ex,
            ClaimsPrincipal user,
            string? userflow = null)
        {
            if (ex == null)
            {
                throw new ArgumentNullException(nameof(ex));
            }

            scopes ??= new string[0];
            var properties = new AuthenticationProperties();

            // Set the scopes, including the scopes that ADAL.NET / MSAL.NET need for the token cache
            string[] additionalBuiltInScopes =
            {
                 OidcConstants.ScopeOpenId,
                 OidcConstants.ScopeOfflineAccess,
                 OidcConstants.ScopeProfile,
            };

            properties.SetParameter<ICollection<string>>(
                OpenIdConnectParameterNames.Scope,
                scopes.Union(additionalBuiltInScopes).ToList());

            // Attempts to set the login_hint to avoid the logged-in user to be presented with an account selection dialog
            var loginHint = user.GetLoginHint();
            if (!string.IsNullOrWhiteSpace(loginHint))
            {
                properties.SetParameter(OpenIdConnectParameterNames.LoginHint, loginHint);

                var domainHint = user.GetDomainHint();
                properties.SetParameter(OpenIdConnectParameterNames.DomainHint, domainHint);
            }

            // Additional claims required (for instance MFA)
            if (!string.IsNullOrEmpty(ex.Claims))
            {
                properties.Items.Add(OidcConstants.AdditionalClaims, ex.Claims);
            }

            // Include current userflow for B2C
            if (!string.IsNullOrEmpty(userflow))
            {
                properties.Items.Add(OidcConstants.PolicyKey, userflow);
            }

            return properties;
        }
    }
}
