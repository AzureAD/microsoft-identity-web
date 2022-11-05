// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Azure.Identity;
using System.Threading;
using Microsoft.Identity.Abstractions;
using System.Threading.Tasks;

namespace Microsoft.Identity.Web
{
    internal class SignedAssertionFromManagedIdentityCredentialLoader : ICredentialSourceLoader
    {
        public CredentialSource CredentialSource => CredentialSource.SignedAssertionFromManagedIdentity;

        public async Task LoadIfNeededAsync(CredentialDescription credentialDescription)
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
                    _ = await managedIdentityClientAssertion!.GetSignedAssertion(CancellationToken.None);
                }
                catch (AuthenticationFailedException)
                {
                    credentialDescription.CachedValue = managedIdentityClientAssertion;
                    credentialDescription.Skip = true;
                    throw;
                }
            }
        }
    }
}
