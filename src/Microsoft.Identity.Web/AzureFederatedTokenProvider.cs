// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using Azure.Core;
using Azure.Identity;

namespace Microsoft.Identity.Web
{
    internal class AzureFederatedTokenProvider : ClientAssertionDescription
    {
        public AzureFederatedTokenProvider(string? federatedClientId)
            : base(null!)
        {
            _federatedClientId = federatedClientId;
            ClientAssertionProvider = GetSignedAssertionFromFederatedTokenProvider;
        }

        private readonly string? _federatedClientId;

        /// <summary>
        /// Prototype of certificate-less authentication using a signed assertion
        /// acquired with MSI (federated identity).
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
    }
}
