// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using Azure.Core;
using Azure.Identity;

namespace Microsoft.Identity.Web
{
    // Do we want to provide this class as product code?
    // Or change default certificate loader so that it provides it?
    internal class MsiSignedAssertionProvider : ClientAssertionDescription
    {
        public MsiSignedAssertionProvider(string userAssignedManagedIdentityClientId)
            : base(null!)
        {
            this.userAssignedManagedIdentityClientId = userAssignedManagedIdentityClientId;
            ClientAssertionProvider = GetSignedAssertionFromMsi;
        }

        private string userAssignedManagedIdentityClientId;

        /// <summary>
        /// Prototype of certificate-less authentication using a signed assertion
        /// acquired with MSI (federated identity).
        /// </summary>
        /// <returns>The signed assertion.</returns>
        private async Task<ClientAssertion> GetSignedAssertionFromMsi(CancellationToken cancellationToken)
        {
            var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions { ManagedIdentityClientId = userAssignedManagedIdentityClientId });
            var result = await credential.GetTokenAsync(
                new TokenRequestContext(new[] { "api://AzureADTokenExchange/.default" }, null),
                cancellationToken).ConfigureAwait(false);
            return new ClientAssertion(result.Token, result.ExpiresOn);
        }
    }
}
