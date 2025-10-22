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
    /// A DelegatingHandler implementation that add an authorization header with a token for the application.
    /// </summary>
    public class MicrosoftIdentityAppAuthenticationMessageHandler : MicrosoftIdentityAuthenticationBaseMessageHandler
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MicrosoftIdentityAppAuthenticationMessageHandler"/> class.
        /// </summary>
        /// <param name="tokenAcquisition">Token acquisition service.</param>
        /// <param name="namedMessageHandlerOptions">Named options provider.</param>
        /// <param name="serviceName">Name of the service describing the downstream web API.</param>
        public MicrosoftIdentityAppAuthenticationMessageHandler(
            ITokenAcquisition tokenAcquisition,
            IOptionsMonitor<MicrosoftIdentityAuthenticationMessageHandlerOptions> namedMessageHandlerOptions,
            string? serviceName = null)
            : base(tokenAcquisition, namedMessageHandlerOptions, serviceName)
        {
        }

        /// <inheritdoc/>
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // validate arguments
            _ = Throws.IfNull(request);

            // authenticate
            var options = GetOptionsForRequest(request);

            var authResult = await TokenAcquisition.GetAuthenticationResultForAppAsync(
                options.Scopes!,
                options.AuthenticationScheme,
                options.Tenant,
                options.TokenAcquisitionOptions)
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
