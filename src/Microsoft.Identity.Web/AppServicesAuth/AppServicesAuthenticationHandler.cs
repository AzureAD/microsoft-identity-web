// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT-License.

using System.Linq;
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
    public class AppServicesAuthenticationHandler : AuthenticationHandler<AppServicesAuthenticationOptions>
    {
        /// <summary>
        /// Constructor for the AppServiceAuthenticationHandler.
        /// Note the parameters are required by the base class.
        /// </summary>
        /// <param name="options">App service authentication options.</param>
        /// <param name="logger">Logger factory.</param>
        /// <param name="encoder">URL encoder.</param>
        /// <param name="clock">System clock.</param>
        public AppServicesAuthenticationHandler(
              IOptionsMonitor<AppServicesAuthenticationOptions> options,
              ILoggerFactory logger,
              UrlEncoder encoder,
              ISystemClock clock)
              : base(options, logger, encoder, clock)
        {
        }

        /// <inheritdoc/>
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (AppServicesAuthenticationInformation.IsAppServicesAadAuthenticationEnabled)
            {
                ClaimsPrincipal? claimsPrincipal = AppServicesAuthenticationInformation.GetUser(Context.Request.Headers);
                if (claimsPrincipal != null)
                {
                    AuthenticationTicket ticket = new AuthenticationTicket(claimsPrincipal, AppServicesAuthenticationDefaults.AuthenticationScheme);
                    AuthenticateResult success = AuthenticateResult.Success(ticket);
                    return Task<AuthenticateResult>.FromResult<AuthenticateResult>(success);
                }
            }

            // Try another handler
            return Task.FromResult(AuthenticateResult.NoResult());
        }
    }
}
