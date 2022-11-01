using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Azure.Identity;
using System.Threading;
using Microsoft.Identity.Abstractions;

namespace Microsoft.Identity.Web.Certificate
{
    internal class SignedAssertionFromManagedIdentityCredentialLoader : ICredentialSourceLoader
    {
        public CredentialSource CredentialSource => CredentialSource.SignedAssertionFromManagedIdentity;

        public void LoadIfNeeded(CredentialDescription credentialDescription)
        {
            if (credentialDescription.SourceType == CredentialSource.SignedAssertionFromManagedIdentity)
            {
                ManagedIdentityClientAssertion? managedIdentityClientAssertion = credentialDescription.CachedValue as ManagedIdentityClientAssertion;
                if (credentialDescription.CachedValue == null)
                {
                    managedIdentityClientAssertion = new ManagedIdentityClientAssertion(credentialDescription.ManagedIdentityClientId);
                    credentialDescription.CachedValue = managedIdentityClientAssertion;
                }
                try
                {
                    // Given that managed identity can be not available locally, we need to try to get a
                    // signed assertion, and if it fails, move to the next credentials
                    managedIdentityClientAssertion!.GetSignedAssertion(CancellationToken.None).GetAwaiter().GetResult();
                }
                catch (AuthenticationFailedException)
                {
                    credentialDescription.Skip = true;
                }
            }
        }
    }
}
