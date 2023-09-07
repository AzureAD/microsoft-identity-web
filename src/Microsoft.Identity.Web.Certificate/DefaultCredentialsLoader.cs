// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Abstractions;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Default credentials loader.
    /// </summary>
    public class DefaultCredentialsLoader : ICredentialsLoader
    {
        ILogger<DefaultCredentialsLoader>? _logger;
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _loadingSemaphores = new ConcurrentDictionary<string, SemaphoreSlim>();

        /// <summary>
        /// Constructor with a logger
        /// </summary>
        /// <param name="logger"></param>
        public DefaultCredentialsLoader(ILogger<DefaultCredentialsLoader>? logger)
        {
            _logger = logger;
            CredentialSourceLoaders = new Dictionary<CredentialSource, ICredentialSourceLoader>
            {
                { CredentialSource.KeyVault, new KeyVaultCertificateLoader() },
                { CredentialSource.Path, new FromPathCertificateLoader() },
                { CredentialSource.StoreWithThumbprint, new StoreWithThumbprintCertificateLoader() },
                { CredentialSource.StoreWithDistinguishedName, new StoreWithDistinguishedNameCertificateLoader() },
                { CredentialSource.Base64Encoded, new Base64EncodedCertificateLoader() },
                { CredentialSource.SignedAssertionFromManagedIdentity, new SignedAssertionFromManagedIdentityCredentialLoader() },
                { CredentialSource.SignedAssertionFilePath, new SignedAssertionFilePathCredentialsLoader(_logger) }
            };
        }

        /// <summary>
        /// Default constructor (for backward compatibility)
        /// </summary>
        public DefaultCredentialsLoader() : this(null)
        {
        }

        /// <summary>
        /// Dictionary of credential loaders per credential source. The application can add more to 
        /// process additional credential sources(like dSMS).
        /// </summary>
        public IDictionary<CredentialSource, ICredentialSourceLoader> CredentialSourceLoaders { get; }

        /// <inheritdoc/>
        /// Load the credentials from the description, if needed.
        public async Task LoadCredentialsIfNeededAsync(CredentialDescription credentialDescription, CredentialSourceLoaderParameters? parameters = null)
        {
            _ = Throws.IfNull(credentialDescription);

            if (credentialDescription.CachedValue == null)
            {
                // Get or create a semaphore for this credentialDescription
                var semaphore = _loadingSemaphores.GetOrAdd(credentialDescription.Id, new SemaphoreSlim(1));

                // Wait to acquire the semaphore
                await semaphore.WaitAsync();

                try
                {
                    if (credentialDescription.CachedValue == null)
                    {
                        if (CredentialSourceLoaders.TryGetValue(credentialDescription.SourceType, out ICredentialSourceLoader? loader))
                            await loader.LoadIfNeededAsync(credentialDescription, parameters);
                    }
                }
                finally
                {
                    // Release the semaphore
                    semaphore.Release();
                }
            }
        }

        private string GenerateKey(CredentialDescription credentialDescription)
        {
            // Generate a unique key for the credentialDescription
            return $"{credentialDescription.SourceType}_{credentialDescription.ReferenceOrValue}";
        }

        /// <inheritdoc/>
        public async Task<CredentialDescription?> LoadFirstValidCredentialsAsync(IEnumerable<CredentialDescription> credentialDescriptions, CredentialSourceLoaderParameters? parameters = null)
        {
            foreach (var credentialDescription in credentialDescriptions)
            {
                await LoadCredentialsIfNeededAsync(credentialDescription, parameters);
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
