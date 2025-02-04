// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
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
        /// <param name="logger"></param>
        public DefaultCredentialsLoader(IEnumerable<ICustomSignedAssertionProvider> customSignedAssertionProviders, ILogger<DefaultCredentialsLoader>? logger) : this(logger)
        {
            var sourceLoaderDict = new Dictionary<string, ICustomSignedAssertionProvider>();

            foreach (ICustomSignedAssertionProvider provider in customSignedAssertionProviders)
            {
                sourceLoaderDict.Add(provider.Name ?? provider.GetType().FullName!, provider);
            }

            CustomSignedAssertionCredentialSourceLoaders = sourceLoaderDict;
        }

        /// <summary>
        /// Dictionary of custom signed assertion credential source loaders, by name (fully qualified type name).
        /// </summary>
        public IDictionary<string, ICustomSignedAssertionProvider>? CustomSignedAssertionCredentialSourceLoaders { get; }


        private async Task ProcessCustomSignedAssertionAsync(CredentialDescription credentialDescription, CredentialSourceLoaderParameters? parameters)
        {
            // No source loader(s)
            if (CustomSignedAssertionCredentialSourceLoaders == null || !CustomSignedAssertionCredentialSourceLoaders.Any())
            {
                _logger.LogError(CertificateErrorMessage.CustomProviderSourceLoaderNullOrEmpty);
            }

            // No provider name
            else if (string.IsNullOrEmpty(credentialDescription.CustomSignedAssertionProviderName))
            {
                _logger.LogError(CertificateErrorMessage.CustomProviderNameNullOrEmpty);
            }

            // No source loader for provider name
            else if (!CustomSignedAssertionCredentialSourceLoaders!.TryGetValue(credentialDescription.CustomSignedAssertionProviderName!, out ICredentialSourceLoader? sourceLoader))
            {
                _logger.LogError(CertificateErrorMessage.CustomProviderNotFound, credentialDescription.CustomSignedAssertionProviderName);
            }

            // Load the credentials, if there is an error, it is coming from the user's custom extension and should be logged and propagated.
            else
            {
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
