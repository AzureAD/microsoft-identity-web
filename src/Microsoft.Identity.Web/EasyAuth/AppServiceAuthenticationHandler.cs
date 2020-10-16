// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.using System;

using System;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
        /// <param name="options">App service authentication options</param>
        /// <param name="logger">Logger factory</param>
        /// <param name="encoder">URL encoder</param>
        /// <param name="clock">System clock</param>
        public AppServiceAuthenticationHandler(
              IOptionsMonitor<AppServiceAuthenticationOptions> options,
              ILoggerFactory logger,
              UrlEncoder encoder,
              ISystemClock clock)
              : base(options, logger, encoder, clock)
        {
        }

        // Constants
        private const string EasyAuthIdTokenHeader = "X-MS-TOKEN-AAD-ID-TOKEN";
        private const string EasyAuthRefreshTokenHeader = "X-MS-TOKEN-AAD-REFRESH-TOKEN";
        private const string EasyAuthIdpTokenHeader = "X-MS-CLIENT-PRINCIPAL-IDP";
        private const string EasyAuthEnvironmentVariable = "azure_auth_enabled";

        /// <summary>
        /// Is AppService authentication enabled ?.
        /// </summary>
        public static bool IsAppServiceAuthenticationEnabled
        {
            get
            {
                return !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(EasyAuthEnvironmentVariable));
            }
        }

        /// <inheritdoc/>
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {

            if (!IsAppServiceAuthenticationEnabled)
            {
                // Try another handler
//                return Task.FromResult(AuthenticateResult.NoResult());
            }

            string idToken = Context.Request.Headers[EasyAuthIdTokenHeader];
            string refreshToken = Context.Request.Headers[EasyAuthRefreshTokenHeader];
            string idp = Context.Request.Headers[EasyAuthIdpTokenHeader];

            JsonWebToken jsonWebToken = new JsonWebToken(idToken);
            ClaimsPrincipal claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(
                jsonWebToken.Claims,
                idp,
                "name",
                ClaimsIdentity.DefaultRoleClaimType));

            AuthenticationTicket ticket = new AuthenticationTicket(claimsPrincipal, AppServiceAuthenticationDefaults.AuthenticationScheme);
            AuthenticateResult success = AuthenticateResult.Success(ticket);
            return Task<AuthenticateResult>.FromResult<AuthenticateResult>(success);
        }
    }
}
