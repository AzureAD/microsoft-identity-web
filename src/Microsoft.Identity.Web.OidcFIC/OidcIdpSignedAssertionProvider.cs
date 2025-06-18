// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web;

namespace Microsoft.Identity.Web.OidcFic
{
    internal class OidcIdpSignedAssertionProvider : ClientAssertionProviderBase
    {
        private ITokenAcquirer? _tokenAcquirer = null;
        private readonly ITokenAcquirerFactory _tokenAcquirerFactory;
        private readonly MicrosoftIdentityApplicationOptions _options;
        private readonly string? _tokenExchangeUrl;

        public OidcIdpSignedAssertionProvider(ITokenAcquirerFactory tokenAcquirerFactory, MicrosoftIdentityApplicationOptions options, string? tokenExchangeUrl)
        {
            _tokenAcquirerFactory = tokenAcquirerFactory;
            _options = options;
            _tokenExchangeUrl = tokenExchangeUrl;
        }

        protected override async Task<ClientAssertion> GetClientAssertionAsync(AssertionRequestOptions? assertionRequestOptions)
        {
            _tokenAcquirer ??= _tokenAcquirerFactory.GetTokenAcquirer(_options);

            string tokenExchangeUrl = _tokenExchangeUrl ?? "api://AzureADTokenExchange";
            AcquireTokenOptions? acquireTokenOptions = null;

            if (assertionRequestOptions != null && !string.IsNullOrEmpty(assertionRequestOptions.ClientAssertionFmiPath))
            {
                acquireTokenOptions = new AcquireTokenOptions()
                {
                    FmiPath = assertionRequestOptions.ClientAssertionFmiPath
                };
            }

            AcquireTokenResult result = await _tokenAcquirer.GetTokenForAppAsync(tokenExchangeUrl + "/.default", acquireTokenOptions);
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
