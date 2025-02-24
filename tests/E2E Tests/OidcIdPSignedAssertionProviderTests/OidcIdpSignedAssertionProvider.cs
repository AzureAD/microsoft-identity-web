// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web;

namespace CustomSignedAssertionProviderTests
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

            AcquireTokenResult result = await _tokenAcquirer.GetTokenForAppAsync(tokenExchangeUrl + "/.default");
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
