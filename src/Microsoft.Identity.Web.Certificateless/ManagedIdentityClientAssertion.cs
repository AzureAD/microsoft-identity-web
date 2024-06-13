// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using Azure.Core;
using Azure.Identity;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web.Certificateless;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// See https://aka.ms/ms-id-web/certificateless.
    /// </summary>
    public class ManagedIdentityClientAssertion : ClientAssertionProviderBase
    {
        private readonly TokenCredential _credential;
        private readonly string _tokenExchangeUrl;

        /// <summary>
        /// See https://aka.ms/ms-id-web/certificateless.
        /// </summary>
        /// <param name="managedIdentityClientId">Optional ClientId of the Managed Identity</param>
        public ManagedIdentityClientAssertion(string? managedIdentityClientId)
        {
            _credential = new ManagedIdentityCredential(managedIdentityClientId);
            _tokenExchangeUrl = CertificatelessConstants.DefaultTokenExchangeUrl;
        }

        /// <summary>
        /// See https://aka.ms/ms-id-web/certificateless.
        /// </summary>
        /// <param name="managedIdentityClientId">Optional ClientId of the Managed Identity</param>
        /// <param name="tokenExchangeUrl">Optional audience of the token to be requested from Managed Identity. Default value is "api://AzureADTokenExchange/.default". This value is different on other clouds.</param>
        public ManagedIdentityClientAssertion(string? managedIdentityClientId, string? tokenExchangeUrl) : this (managedIdentityClientId)
        {
            _tokenExchangeUrl = tokenExchangeUrl ?? CertificatelessConstants.DefaultTokenExchangeUrl;
        }

        /// <summary>
        /// Prototype of certificate-less authentication using a signed assertion
        /// acquired with managed identity (certificateless).
        /// </summary>
        /// <returns>The signed assertion.</returns>
        protected override async Task<ClientAssertion> GetClientAssertionAsync(AssertionRequestOptions? assertionRequestOptions)
        {
            var result = await _credential.GetTokenAsync(
                new TokenRequestContext([_tokenExchangeUrl], null),
                assertionRequestOptions?.CancellationToken ?? default).ConfigureAwait(false);

            return new ClientAssertion(result.Token, result.ExpiresOn);
        }
    }
}
