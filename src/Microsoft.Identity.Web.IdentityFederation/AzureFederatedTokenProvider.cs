// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core;
using Azure.Identity;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// See https://aka.ms/ms-id-web/identity-federation.
    /// </summary>
    public class AzureFederatedTokenProvider
    {
        /// <summary>
        /// See https://aka.ms/ms-id-web/identity-federation.
        /// </summary>
        public AzureFederatedTokenProvider(): this(null)
        {           
        }

        /// <summary>
        /// See https://aka.ms/ms-id-web/identity-federation.
        /// </summary>
        /// <param name="federatedClientId"></param>
        public AzureFederatedTokenProvider(string? federatedClientId)
        {
            _federatedClientId = federatedClientId;
            ClientAssertionProvider = GetSignedAssertionFromFederatedTokenProvider;
        }

        private readonly string? _federatedClientId;

        /// <summary>
        /// Prototype of certificate-less authentication using a signed assertion
        /// acquired with Managed Identity (federated identity).
        /// </summary>
        /// <returns>The signed assertion.</returns>
        private async Task<ClientAssertion> GetSignedAssertionFromFederatedTokenProvider(CancellationToken cancellationToken)
        {
            var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions { ManagedIdentityClientId = _federatedClientId });

            var result = await credential.GetTokenAsync(
                new TokenRequestContext(new[] { "api://AzureADTokenExchange/.default" }, null),
                cancellationToken).ConfigureAwait(false);
            return new ClientAssertion(result.Token, result.ExpiresOn);
        }

        /// <summary>
        /// Delegate to get the client assertion.
        /// </summary>
        private Func<CancellationToken, Task<ClientAssertion>> ClientAssertionProvider { get; set; }

        /// <summary>
        /// Client assertion.
        /// </summary>
        private ClientAssertion? _clientAssertion;

        /// <summary>
        /// Get the signed assertion (and refreshes it if needed).
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The signed assertion.</returns>
        public async Task<string> GetSignedAssertion(CancellationToken cancellationToken)
        {
            if (_clientAssertion == null || (Expiry != null && DateTimeOffset.Now > Expiry))
            {
                _clientAssertion = await ClientAssertionProvider(cancellationToken).ConfigureAwait(false);
            }

            return _clientAssertion.SignedAssertion;
        }

        /// <summary>
        /// Expiry of the client assertion.
        /// </summary>
        private DateTimeOffset? Expiry
        {
            get
            {
                return _clientAssertion?.Expiry;
            }
        }
    }
}
