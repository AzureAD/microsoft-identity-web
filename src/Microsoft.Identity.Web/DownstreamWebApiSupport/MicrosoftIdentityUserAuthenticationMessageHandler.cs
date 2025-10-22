// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Options;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// A DelegatingHandler implementation that add an authorization header with a token on behalf of the current user.
    /// </summary>
    public class MicrosoftIdentityUserAuthenticationMessageHandler : MicrosoftIdentityAuthenticationBaseMessageHandler
    {
        private readonly IOptionsMonitor<MicrosoftIdentityOptions> _microsoftIdentityOptions;

        /// <summary>
        /// Initializes a new instance of the <see cref="MicrosoftIdentityUserAuthenticationMessageHandler"/> class.
        /// </summary>
        /// <param name="tokenAcquisition">Token acquisition service.</param>
        /// <param name="namedMessageHandlerOptions">Named options provider.</param>
        /// <param name="microsoftIdentityOptions">Configuration options.</param>
        /// <param name="serviceName">Name of the service describing the downstream web API.</param>
        public MicrosoftIdentityUserAuthenticationMessageHandler(
            ITokenAcquisition tokenAcquisition,
            IOptionsMonitor<MicrosoftIdentityAuthenticationMessageHandlerOptions> namedMessageHandlerOptions,
            IOptionsMonitor<MicrosoftIdentityOptions> microsoftIdentityOptions,
            string? serviceName = null)
            : base(tokenAcquisition, namedMessageHandlerOptions, serviceName)
        {
            _microsoftIdentityOptions = microsoftIdentityOptions;
        }

        /// <inheritdoc/>
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // validate arguments
            _ = Throws.IfNull(request);

            // authenticate
            var options = GetOptionsForRequest(request);
            var microsoftIdentityOptions = _microsoftIdentityOptions
                .Get(TokenAcquisition.GetEffectiveAuthenticationScheme(options.AuthenticationScheme));

            var userflow = microsoftIdentityOptions.IsB2C && string.IsNullOrEmpty(options.UserFlow)
                ? microsoftIdentityOptions.DefaultUserFlow
                : options.UserFlow;

            var authResult = await TokenAcquisition.GetAuthenticationResultForUserAsync(
                options.GetScopes(),
                authenticationScheme: options.AuthenticationScheme,
                tenantId: options.Tenant,
                userFlow: userflow,
                tokenAcquisitionOptions: options.TokenAcquisitionOptions)
                .ConfigureAwait(false);

            // add or replace authorization header
            if (request.Headers.Contains(Constants.Authorization))
            {
                request.Headers.Remove(Constants.Authorization);
            }

            var authorizationHeaderInformation = authResult.CreateAuthorizationHeader();

            request.Headers.Add(
                Constants.Authorization,
                authorizationHeaderInformation.AuthorizationHeaderValue);

            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }
    }
}
