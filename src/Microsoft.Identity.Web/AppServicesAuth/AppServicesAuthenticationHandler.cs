// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// App service authentication handler.
    /// </summary>
    public class AppServicesAuthenticationHandler : AuthenticationHandler<AppServicesAuthenticationOptions>
    {
        /// <inheritdoc/>
        ///<remarks>Note the parameters are required by the base class.</remarks>
        public AppServicesAuthenticationHandler(
              IOptionsMonitor<AppServicesAuthenticationOptions> options,
              ILoggerFactory logger,
#if NET8_0_OR_GREATER
              UrlEncoder encoder)
              : base(options, logger, encoder)
#else
              UrlEncoder encoder,
              ISystemClock clock)
              : base(options, logger, encoder, clock)
#endif
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
                    return Task.FromResult(success);
                }
            }

            // Try another handler
            return Task.FromResult(AuthenticateResult.NoResult());
        }
    }
}
