using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Azure.Identity;
using System.Threading;
using Microsoft.Identity.Abstractions;
using System.Threading.Tasks;

namespace Microsoft.Identity.Web.Certificate
{
    internal class SignedAssertionFromManagedIdentityCredentialLoader : ICredentialSourceLoader
    {
        public CredentialSource CredentialSource => CredentialSource.SignedAssertionFromManagedIdentity;

        public async Task LoadIfNeededAsync(CredentialDescription credentialDescription, bool throwExceptions = false)
        {
            if (credentialDescription.SourceType == CredentialSource.SignedAssertionFromManagedIdentity)
            {
                ManagedIdentityClientAssertion? managedIdentityClientAssertion = credentialDescription.CachedValue as ManagedIdentityClientAssertion;
                if (credentialDescription.CachedValue == null)
                {
                    managedIdentityClientAssertion = new ManagedIdentityClientAssertion(credentialDescription.ManagedIdentityClientId);
                }
                try
                {
                    // Given that managed identity can be not available locally, we need to try to get a
                    // signed assertion, and if it fails, move to the next credentials
                    _= await managedIdentityClientAssertion!.GetSignedAssertion(CancellationToken.None);
                }
                catch (AuthenticationFailedException)
                {
                    credentialDescription.CachedValue = managedIdentityClientAssertion;
                    credentialDescription.Skip = true;
                    if (throwExceptions)
                    {
                        throw;
                    }
                }
            }
        }
    }
}
