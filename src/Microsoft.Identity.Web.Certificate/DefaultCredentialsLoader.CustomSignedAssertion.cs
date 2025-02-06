// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Abstractions;

namespace Microsoft.Identity.Web
{
    public partial class DefaultCredentialsLoader
    {
        /// <summary>
        /// Constructor for DefaultCredentialsLoader when using custom signed assertion provider source loaders.
        /// </summary>
        /// <param name="customSignedAssertionProviders">Set of custom signed assertion providers.</param>
        /// <param name="logger">ILogger.</param>
        public DefaultCredentialsLoader(IEnumerable<ICustomSignedAssertionProvider> customSignedAssertionProviders, ILogger<DefaultCredentialsLoader>? logger) : this(logger)
        {
            _ = Throws.IfNull(customSignedAssertionProviders);
            var sourceLoaderDict = new Dictionary<string, ICustomSignedAssertionProvider>();

            foreach (ICustomSignedAssertionProvider provider in customSignedAssertionProviders)
            {
                string providerName = provider.Name ?? provider.GetType().FullName!;
                if (sourceLoaderDict.ContainsKey(providerName))
                {
                    _logger.LogWarning(CertificateErrorMessage.CustomProviderNameAlreadyExists, providerName);
                }
                else
                {
                    sourceLoaderDict.Add(providerName, provider);
                }
            }
            CustomSignedAssertionCredentialSourceLoaders = sourceLoaderDict;
        }

        /// <summary>
        /// Dictionary of custom signed assertion credential source loaders, by name (fully qualified type name).
        /// The application can add more to process additional credential sources.
        /// </summary>
        protected IDictionary<string, ICustomSignedAssertionProvider>? CustomSignedAssertionCredentialSourceLoaders { get; }

        private async Task ProcessCustomSignedAssertionAsync(CredentialDescription credentialDescription, CredentialSourceLoaderParameters? parameters)
        {
            if (CustomSignedAssertionCredentialSourceLoaders == null || CustomSignedAssertionCredentialSourceLoaders.Count == 0)
            {
                // No source loader(s)
                _logger.LogError(CertificateErrorMessage.CustomProviderSourceLoaderNullOrEmpty);
            }
            else if (string.IsNullOrEmpty(credentialDescription.CustomSignedAssertionProviderName))
            {
                // No provider name
                _logger.LogError(CertificateErrorMessage.CustomProviderNameNullOrEmpty);
            }
            else if (!CustomSignedAssertionCredentialSourceLoaders!.TryGetValue(credentialDescription.CustomSignedAssertionProviderName!, out ICustomSignedAssertionProvider? sourceLoader))
            {
                // No source loader for provider name
                _logger.LogError(CertificateErrorMessage.CustomProviderNotFound, credentialDescription.CustomSignedAssertionProviderName);
            }
            else
            {
                // Load the credentials, if there is an error, it is coming from the user's custom extension and should be logged and propagated.
                try
                {
                    await sourceLoader.LoadIfNeededAsync(credentialDescription, parameters);
                }
                catch (Exception ex)
                {
                    Logger.CustomSignedAssertionProviderLoadingFailure(_logger, credentialDescription, ex);
                    throw;
                }
                return;
            }
        }
    }
}
