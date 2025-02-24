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
        private readonly ITokenAcquirer _tokenAcquirer;
        private readonly string? _tokenExchangeUrl;

        public OidcIdpSignedAssertionProvider(ITokenAcquirer tokenAcquirer, string? tokenExchangeUrl)
        {
            _tokenAcquirer = tokenAcquirer;
            _tokenExchangeUrl = tokenExchangeUrl;
        }

        protected override async Task<ClientAssertion> GetClientAssertionAsync(AssertionRequestOptions? assertionRequestOptions)
        {
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
