// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web;

namespace Microsoft.Identity.Web.OidcFic
{
    internal partial class OidcIdpSignedAssertionProvider : ClientAssertionProviderBase
    {
        private ITokenAcquirer? _tokenAcquirer = null;
        private readonly ITokenAcquirerFactory _tokenAcquirerFactory;
        private readonly MicrosoftIdentityApplicationOptions _options;
        private readonly string? _tokenExchangeUrl;
        private readonly ILogger? _logger;

        public bool RequiresSignedAssertionFmiPath { get; internal set; }

        public OidcIdpSignedAssertionProvider(ITokenAcquirerFactory tokenAcquirerFactory, MicrosoftIdentityApplicationOptions options, string? tokenExchangeUrl, ILogger? logger)
        {
            _tokenAcquirerFactory = tokenAcquirerFactory;
            _options = options;
            _tokenExchangeUrl = tokenExchangeUrl;
            _logger = logger;
        }

        protected override async Task<ClientAssertion> GetClientAssertionAsync(AssertionRequestOptions? assertionRequestOptions)
        {
            _tokenAcquirer ??= _tokenAcquirerFactory.GetTokenAcquirer(_options);

            string tokenExchangeUrl = _tokenExchangeUrl ?? "api://AzureADTokenExchange";
            AcquireTokenOptions? acquireTokenOptions = null;

            // During the construction of the CCA, IdWeb tried to understand which Credential description to use and to skip, and thefore
            // attempts to load the credentials with assertionRequestOptions = null (whereas it's not null when MSAL calls GetSignedAssertionAsync).
            // If assertionRequestOptions = null and RequiresSignedAssertionFmiPath is true
            // we postpone getting the signed assertion until the first call, when ClientAssertionFmiPath will be provided.
            if (RequiresSignedAssertionFmiPath && assertionRequestOptions == null)
            {
                if (_logger is not null)
                {
                    Logger.PostponingSignedAssertionAcquisition(_logger);
                }

                // By using Now, we are certain to be called immediately again
                return new ClientAssertion(null!, DateTimeOffset.Now);
            }

            if (assertionRequestOptions != null && !string.IsNullOrEmpty(assertionRequestOptions.ClientAssertionFmiPath))
            {
                acquireTokenOptions = new AcquireTokenOptions()
                {
                    FmiPath = assertionRequestOptions.ClientAssertionFmiPath
                };
            }

            if (_logger is not null)
            {
                Logger.AcquiringTokenForTokenExchange(_logger, tokenExchangeUrl, acquireTokenOptions?.FmiPath);
            }
            string effectiveTokenExchangeUrl = (tokenExchangeUrl.EndsWith("/.default", StringComparison.OrdinalIgnoreCase)
                ? tokenExchangeUrl : tokenExchangeUrl + "/.default");
            AcquireTokenResult result = await _tokenAcquirer.GetTokenForAppAsync(effectiveTokenExchangeUrl, acquireTokenOptions);
            if (_logger is not null)
            {
                Logger.AcquiredTokenForTokenExchange(_logger, acquireTokenOptions?.FmiPath);
            }
            ClientAssertion clientAssertion;
            if (result != null)
            {
                clientAssertion = new ClientAssertion(result.AccessToken!, result.ExpiresOn);
            }
            else
            {
                clientAssertion = null!;
            }
            return clientAssertion;
        }
    }
}
