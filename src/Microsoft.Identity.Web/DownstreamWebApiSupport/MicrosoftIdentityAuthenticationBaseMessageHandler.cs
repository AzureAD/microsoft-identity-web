// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net.Http;

using Microsoft.Extensions.Options;
using Microsoft.Identity.Client.AppConfig;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Base class for Microsoft Identity authentication message handlers.
    /// </summary>
    public abstract class MicrosoftIdentityAuthenticationBaseMessageHandler : DelegatingHandler
    {
        private readonly IOptionsMonitor<MicrosoftIdentityAuthenticationMessageHandlerOptions> _namedMessageHandlerOptions;
        private readonly string? _serviceName;

        /// <summary>
        /// Gets the token acquisition service.
        /// </summary>
        protected ITokenAcquisition TokenAcquisition { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MicrosoftIdentityAuthenticationBaseMessageHandler"/> class.
        /// </summary>
        /// <param name="tokenAcquisition">Token acquisition service.</param>
        /// <param name="namedMessageHandlerOptions">Named options provider.</param>
        /// <param name="serviceName">Name of the service describing the downstream web API.</param>
        protected MicrosoftIdentityAuthenticationBaseMessageHandler(
            ITokenAcquisition tokenAcquisition,
            IOptionsMonitor<MicrosoftIdentityAuthenticationMessageHandlerOptions> namedMessageHandlerOptions,
            string? serviceName = null)
        {
            TokenAcquisition = tokenAcquisition;
            _namedMessageHandlerOptions = namedMessageHandlerOptions;
            _serviceName = serviceName;
        }

        /// <summary>
        /// Gets the options for the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>The configured options.</returns>
        protected MicrosoftIdentityAuthenticationMessageHandlerOptions GetOptionsForRequest(HttpRequestMessage request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var options = _serviceName == null
                ? _namedMessageHandlerOptions.CurrentValue
                : _namedMessageHandlerOptions.Get(_serviceName);

            if (string.IsNullOrEmpty(options.Scopes))
            {
                throw new ArgumentException(IDWebErrorMessage.ScopesNotConfiguredInConfigurationOrViaDelegate);
            }

            // clone before overriding with request specific data
            options = options.Clone();
            CreateProofOfPossessionConfiguration(options, request.RequestUri!, request.Method);

            return options;
        }

        private static void CreateProofOfPossessionConfiguration(MicrosoftIdentityAuthenticationMessageHandlerOptions options, Uri apiUri, HttpMethod method)
        {
            if (options.IsProofOfPossessionRequest)
            {
                if (options.TokenAcquisitionOptions == null)
                {
                    options.TokenAcquisitionOptions = new TokenAcquisitionOptions();
                }

                options.TokenAcquisitionOptions.PoPConfiguration = new PoPAuthenticationConfiguration(apiUri)
                {
                    HttpMethod = method,
                };
            }
        }
    }
}
