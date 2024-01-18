// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using Azure.Core;
using Azure.Identity;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// See https://aka.ms/ms-id-web/certificateless.
    /// </summary>
    public class ManagedIdentityClientAssertion : ClientAssertionProviderBase
    {
        private readonly TokenCredential _credential;

        /// <summary>
        /// See https://aka.ms/ms-id-web/certificateless.
        /// </summary>
        /// <param name="managedIdentityClientId">Optional ClientId of the Managed Identity or Workload Identity</param>
        public ManagedIdentityClientAssertion(string? managedIdentityClientId)
        {
            _credential = new DefaultAzureCredential(
                new DefaultAzureCredentialOptions
                {
                    ManagedIdentityClientId = managedIdentityClientId,
                    WorkloadIdentityClientId = managedIdentityClientId
                });
        }

        /// <summary>
        /// Prototype of certificate-less authentication using a signed assertion
        /// acquired with managed identity (certificateless).
        /// </summary>
        /// <returns>The signed assertion.</returns>
        protected override async Task<ClientAssertion> GetClientAssertion(CancellationToken cancellationToken)
        {
            var result = await _credential.GetTokenAsync(
                new TokenRequestContext(["api://AzureADTokenExchange/.default"], null),
                cancellationToken).ConfigureAwait(false);
            return new ClientAssertion(result.Token, result.ExpiresOn);
        }
    }
}
