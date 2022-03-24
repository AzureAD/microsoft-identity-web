// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;

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
        protected override async Task<AuthenticationResult> GetTokenAsync(MicrosoftIdentityAuthenticationMessageHandlerOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            return await TokenAcquisition.GetAuthenticationResultForAppAsync(
                options.Scopes!,
                options.AuthenticationScheme,
                options.Tenant,
                options.TokenAcquisitionOptions)
                .ConfigureAwait(false);
        }

        /// <inheritdoc/>
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // validate arguments
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            // authenticate
            var options = GetOptionsForRequest(request);

            var authResult = await GetTokenAsync(options).ConfigureAwait(false);

            // add or replace authorization header
            if (request.Headers.Contains(Constants.Authorization))
            {
                request.Headers.Remove(Constants.Authorization);
            }

            request.Headers.Add(
                Constants.Authorization,
                authResult.CreateAuthorizationHeader());

            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }
    }
}
