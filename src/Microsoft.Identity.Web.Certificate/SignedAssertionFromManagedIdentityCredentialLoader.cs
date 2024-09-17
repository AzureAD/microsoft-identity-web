// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using Azure.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Client;

namespace Microsoft.Identity.Web
{
    internal class SignedAssertionFromManagedIdentityCredentialLoader : ICredentialSourceLoader
    {
        private readonly ILogger<DefaultCredentialsLoader> _logger;

        public SignedAssertionFromManagedIdentityCredentialLoader(ILogger<DefaultCredentialsLoader> logger)
        {
            _logger = logger;
        }

        public CredentialSource CredentialSource => CredentialSource.SignedAssertionFromManagedIdentity;

        public async Task LoadIfNeededAsync(CredentialDescription credentialDescription, CredentialSourceLoaderParameters? credentialSourceLoaderParameters)
        {
            if (credentialDescription.SourceType == CredentialSource.SignedAssertionFromManagedIdentity)
            {
                ManagedIdentityClientAssertion? managedIdentityClientAssertion = credentialDescription.CachedValue as ManagedIdentityClientAssertion;
                if (credentialDescription.CachedValue == null)
                {
                    managedIdentityClientAssertion = new ManagedIdentityClientAssertion(
                        credentialDescription.ManagedIdentityClientId, 
                        credentialDescription.TokenExchangeUrl,
                        _logger);
                }
                try
                {
                    // Given that managed identity can be not available locally, we need to try to get a
                    // signed assertion, and if it fails, move to the next credentials
                    _ = await managedIdentityClientAssertion!.GetSignedAssertionAsync(null);
                    credentialDescription.CachedValue = managedIdentityClientAssertion;
                }
                catch (MsalServiceException)
                {
                    credentialDescription.Skip = true;
                    throw;
                }
            }
        }
    }
}
