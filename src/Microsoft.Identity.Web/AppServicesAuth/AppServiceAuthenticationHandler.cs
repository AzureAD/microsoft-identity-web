// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// App service authentication handler.
    /// </summary>
    public class AppServiceAuthenticationHandler : AuthenticationHandler<AppServiceAuthenticationOptions>
    {
        /// <summary>
        /// Constructor for the AppServiceAuthenticationHandler.
        /// Note the parameters are required by the base class.
        /// </summary>
        /// <param name="options">App service authentication options.</param>
        /// <param name="logger">Logger factory.</param>
        /// <param name="encoder">URL encoder.</param>
        /// <param name="clock">System clock.</param>
        public AppServiceAuthenticationHandler(
              IOptionsMonitor<AppServiceAuthenticationOptions> options,
              ILoggerFactory logger,
              UrlEncoder encoder,
              ISystemClock clock)
              : base(options, logger, encoder, clock)
        {
        }

        // Constants
        private const string AppServicesAuthIdTokenHeader = "X-MS-TOKEN-AAD-ID-TOKEN";
        private const string AppServicesAuthIdpTokenHeader = "X-MS-CLIENT-PRINCIPAL-IDP";

        /// <inheritdoc/>
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (AppServiceAuthenticationInformation.IsAppServiceAadAuthenticationEnabled)
            {
                string? idToken = GetIdToken();
                string? idp = GetIdp();

                if (idToken != null && idp != null)
                {
                    JsonWebToken jsonWebToken = new JsonWebToken(idToken);
                    ClaimsPrincipal claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(
                        jsonWebToken.Claims,
                        idp,
                        Constants.NameClaim, // v1.0
                        ClaimsIdentity.DefaultRoleClaimType));

                    AuthenticationTicket ticket = new AuthenticationTicket(claimsPrincipal, AppServiceAuthenticationDefaults.AuthenticationScheme);
                    AuthenticateResult success = AuthenticateResult.Success(ticket);
                    return Task<AuthenticateResult>.FromResult<AuthenticateResult>(success);
                }
            }

            // Try another handler
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        private string? GetIdp()
        {
            string? idp = Context.Request.Headers[AppServicesAuthIdpTokenHeader];
#if DEBUG
            if (string.IsNullOrEmpty(idp))
            {
                idp = AppServiceAuthenticationInformation.SimulateGetttingHeaderFromDebugEnvironmentVariable(AppServicesAuthIdpTokenHeader);
            }
#endif
            return idp;
        }

        private string? GetIdToken()
        {
            string? idToken = Context.Request.Headers[AppServicesAuthIdTokenHeader];
#if DEBUG
            if (string.IsNullOrEmpty(idToken))
            {
                idToken = AppServiceAuthenticationInformation.SimulateGetttingHeaderFromDebugEnvironmentVariable(AppServicesAuthIdTokenHeader);
            }
#endif
            return idToken;
        }
    }
}
