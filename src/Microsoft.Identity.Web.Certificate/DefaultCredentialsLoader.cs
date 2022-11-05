// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Identity.Abstractions;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Default credentials loader.
    /// </summary>
    public class DefaultCredentialsLoader : ICredentialsLoader
    {
        /// <summary>
        /// Dictionary of credential loaders per credential source. The application can add more to 
        /// process additional credential sources(like dSMS).
        /// </summary>
        public IDictionary<CredentialSource, ICredentialSourceLoader> CredentialSourceLoaders { get; } = new Dictionary<CredentialSource, ICredentialSourceLoader>
        {
            { CredentialSource.KeyVault, new KeyVaultCertificateLoader() },
            { CredentialSource.Path, new FromPathCertificateLoader() },
            { CredentialSource.StoreWithThumbprint, new StoreWithThumbprintCertificateLoader() },
            { CredentialSource.StoreWithDistinguishedName, new StoreWithDistinguishedNameCertificateLoader() },
            { CredentialSource.Base64Encoded, new Base64EncodedCertificateLoader() },
            { CredentialSource.SignedAssertionFromManagedIdentity, new SignedAssertionFromManagedIdentityCredentialLoader() },
            { CredentialSource.SignedAssertionFilePath, new SignedAssertionFilePathCredentialsLoader() },
        };

        /// <inheritdoc/>
        /// Load the credentials from the description, if needed.
        public async Task LoadCredentialsIfNeededAsync(CredentialDescription credentialDescription)
        {
            _ = Throws.IfNull(credentialDescription);

            if (credentialDescription.CachedValue == null)
            {
                if (CredentialSourceLoaders.TryGetValue(credentialDescription.SourceType, out ICredentialSourceLoader? loader))
                {
                    await loader.LoadIfNeededAsync(credentialDescription);
                }
            }
        }

        /// <inheritdoc/>
        public async Task<CredentialDescription?> LoadFirstValidCredentialsAsync(IEnumerable<CredentialDescription> credentialDescriptions)
        {
            foreach (var credentialDescription in credentialDescriptions)
            {
                await LoadCredentialsIfNeededAsync(credentialDescription);
                if (!credentialDescription.Skip)
                {
                    return credentialDescription;
                }
            }
            return null;
        }

        /// <inheritdoc/>
        public void ResetCredentials(IEnumerable<CredentialDescription> credentialDescriptions)
        {
            foreach (var credentialDescription in credentialDescriptions)
            {
                credentialDescription.CachedValue = null;
                credentialDescription.Skip = false;
                if (credentialDescription.SourceType != CredentialSource.Certificate)
                {
                    credentialDescription.Certificate = null;
                }
            }
        }

    }
}
