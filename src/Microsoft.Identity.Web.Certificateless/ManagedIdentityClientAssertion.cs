// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Web.Certificateless;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// See https://aka.ms/ms-id-web/certificateless.
    /// </summary>
    public class ManagedIdentityClientAssertion 
        : ClientAssertionProviderBase
    {
        private readonly IManagedIdentityApplication _managedIdentityApplication;
        private readonly string _tokenExchangeUrl;

        /// <summary>
        /// See https://aka.ms/ms-id-web/certificateless.
        /// </summary>
        /// <param name="managedIdentityClientId">Optional ClientId of the Managed Identity or Workload Identity</param>
        public ManagedIdentityClientAssertion(string? managedIdentityClientId)
        {
            var id = ManagedIdentityId.SystemAssigned;
            if (!string.IsNullOrEmpty(managedIdentityClientId))
            {
                id = ManagedIdentityId.WithUserAssignedClientId(managedIdentityClientId);
            }

            _managedIdentityApplication = ManagedIdentityApplicationBuilder.Create(id).Build();
            _tokenExchangeUrl = CertificatelessConstants.DefaultTokenExchangeUrl;
        }

        /// <summary>
        /// See https://aka.ms/ms-id-web/certificateless.
        /// </summary>
        /// <param name="managedIdentityClientId">Optional ClientId of the Managed Identity or Workload Identity</param>
        /// <param name="tokenExchangeUrl">Optional token exchange resource url. Default value is "api://AzureADTokenExchange/".</param>
        public ManagedIdentityClientAssertion(string? managedIdentityClientId, string? tokenExchangeUrl) : this (managedIdentityClientId)
        {
            _tokenExchangeUrl = tokenExchangeUrl ?? CertificatelessConstants.DefaultTokenExchangeUrl;
        }

        /// <summary>
        /// Prototype of certificate-less authentication using a signed assertion
        /// acquired with managed identity (certificateless).
        /// </summary>
        /// <returns>The signed assertion.</returns>
        protected override async Task<ClientAssertion> GetClientAssertion(CancellationToken cancellationToken)
        {
            var result = await _managedIdentityApplication
               .AcquireTokenForManagedIdentity(_tokenExchangeUrl)
               .ExecuteAsync(cancellationToken)
               .ConfigureAwait(false);

            return new ClientAssertion(result.AccessToken, result.ExpiresOn);
        }
    }
}
