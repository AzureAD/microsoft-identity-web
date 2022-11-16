// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.dMSI;

namespace Microsoft.Identity.Web
{
    internal sealed class FromDmsiCredentialLoader : ICredentialSourceLoader
    {
        public CredentialSource CredentialSource => CredentialSource.SignedAssertionFromVault;

        /// <summary>
        /// Load the credentials from dMSI
        /// </summary>
        /// <param name="credentialDescription"></param>
        /// <param name="credentialSourceLoaderParameters"></param>
        /// <returns></returns>
        public async Task LoadIfNeededAsync(CredentialDescription credentialDescription, CredentialSourceLoaderParameters credentialSourceLoaderParameters)
        {
            if (credentialDescription == null)
            {
                throw new ArgumentNullException(nameof(credentialDescription));
            }
            if (credentialSourceLoaderParameters == null)
            {
                throw new ArgumentNullException(nameof(credentialSourceLoaderParameters));
            }

            if (credentialDescription.SourceType == CredentialSource.SignedAssertionFromVault)
            {
                if (credentialDescription.KeyVaultUrl == null)
                {
                    throw new ArgumentNullException(nameof(credentialDescription.KeyVaultUrl));
                }

                dMSISignedAssertion? signedAssertion = credentialDescription.CachedValue as dMSISignedAssertion;
                if (credentialDescription.CachedValue == null)
                {
                    signedAssertion = new dMSISignedAssertion(
                        credentialDescription.KeyVaultUrl,
                        credentialSourceLoaderParameters.ClientId,
                        credentialSourceLoaderParameters.Authority);
                }
                try
                {
                    // Given that managed identity can be not available locally, we need to try to get a
                    // signed assertion, and if it fails, move to the next credentials
                    _ = await signedAssertion!.GetSignedAssertion(CancellationToken.None);
                }
                catch (Exception)
                {
                    credentialDescription.CachedValue = signedAssertion;
                    credentialDescription.Skip = true;
                }
            }
        }

    }
}
