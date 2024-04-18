// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using Azure.Identity;
using Microsoft.Identity.Abstractions;

namespace Microsoft.Identity.Web
{
    internal class SignedAssertionFromManagedIdentityCredentialLoader : ICredentialSourceLoader
    {
        public CredentialSource CredentialSource => CredentialSource.SignedAssertionFromManagedIdentity;

        public async Task LoadIfNeededAsync(CredentialDescription credentialDescription, CredentialSourceLoaderParameters? credentialSourceLoaderParameters)
        {
            if (credentialDescription.SourceType == CredentialSource.SignedAssertionFromManagedIdentity)
            {
                ManagedIdentityClientAssertion? managedIdentityClientAssertion = credentialDescription.CachedValue as ManagedIdentityClientAssertion;
                if (credentialDescription.CachedValue == null)
                {
                    managedIdentityClientAssertion = new ManagedIdentityClientAssertion(credentialDescription.ManagedIdentityClientId, credentialDescription.TokenExchangeUrl);
                }
                try
                {
                    // Given that managed identity can be not available locally, we need to try to get a
                    // signed assertion, and if it fails, move to the next credentials
                    _= await managedIdentityClientAssertion!.GetSignedAssertion(CancellationToken.None);
                    credentialDescription.CachedValue = managedIdentityClientAssertion;
                }
                catch (AuthenticationFailedException)
                {
                    credentialDescription.Skip = true;
                    throw;
                }
            }
        }
    }
}
