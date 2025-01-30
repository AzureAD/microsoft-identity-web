// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Identity.Abstractions;

namespace Microsoft.Identity.Web
{
    public partial class DefaultCredentialsLoader : ICredentialsLoader
    {
        /// <summary>
        /// Constructor for DefaultCredentialsLoader when using custom signed assertion provider source loaders.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="customSignedAssertionProviders">Set of custom signed assertion providers.</param>
        public DefaultCredentialsLoader(ILogger<DefaultCredentialsLoader>? logger, IEnumerable<ICustomSignedAssertionProvider> customSignedAssertionProviders)
        {
            _logger = logger ?? new NullLogger<DefaultCredentialsLoader>();

            CredentialSourceLoaders = new Dictionary<CredentialSource, ICredentialSourceLoader>
            {
                { CredentialSource.KeyVault, new KeyVaultCertificateLoader() },
                { CredentialSource.Path, new FromPathCertificateLoader() },
                { CredentialSource.StoreWithThumbprint, new StoreWithThumbprintCertificateLoader() },
                { CredentialSource.StoreWithDistinguishedName, new StoreWithDistinguishedNameCertificateLoader() },
                { CredentialSource.Base64Encoded, new Base64EncodedCertificateLoader() },
                { CredentialSource.SignedAssertionFromManagedIdentity, new SignedAssertionFromManagedIdentityCredentialLoader(_logger) },
                { CredentialSource.SignedAssertionFilePath, new SignedAssertionFilePathCredentialsLoader(_logger) }
            };

            var CustomSignedAssertionCredentialSourceLoaders = new Dictionary<string, ICredentialSourceLoader>();
            foreach (var provider in customSignedAssertionProviders)
            {
                CustomSignedAssertionCredentialSourceLoaders.Add(provider.Name ?? provider.GetType().FullName!, provider);
            }
        }

        /// <summary>
        /// Dictionary of custom signed assertion credential source loaders, by name (fully qualified type name).
        /// </summary>
        public IDictionary<string, ICredentialSourceLoader>? CustomSignedAssertionCredentialSourceLoaders { get; }


        private async Task ProcessCustomSignedAssertionAsync(CredentialDescription credentialDescription, CredentialSourceLoaderParameters? parameters)
        {
            // No source loader(s)
            if (CustomSignedAssertionCredentialSourceLoaders == null || !CustomSignedAssertionCredentialSourceLoaders.Any())
            {
                Logger.CredentialLoadingFailure(_logger, credentialDescription, CustomSignedAssertionProviderNotFoundException.SourceLoadersNullOrEmpty());
            }

            // No provider name
            else if (string.IsNullOrEmpty(credentialDescription.CustomSignedAssertionProviderName))
            {
                Logger.CredentialLoadingFailure(_logger, credentialDescription, CustomSignedAssertionProviderNotFoundException.ProviderNameNullOrEmpty());
            }

            // No source loader for provider name
            else if (!CustomSignedAssertionCredentialSourceLoaders!.TryGetValue(credentialDescription.CustomSignedAssertionProviderName!, out ICredentialSourceLoader? sourceLoader))
            {
                Logger.CredentialLoadingFailure(_logger, credentialDescription, CustomSignedAssertionProviderNotFoundException.ProviderNameNotFound(credentialDescription.CustomSignedAssertionProviderName!));
            }

            // Load the credentials
            else
            {
                await sourceLoader.LoadIfNeededAsync(credentialDescription, parameters);
            }
        }
    }

    internal class CustomSignedAssertionProviderNotFoundException : Exception
    {
        private const string NameNullOrEmpty = "The name of the custom signed assertion provider is null or empty.";
        private const string SourceLoaderNullOrEmpty = "The dictionary of SourceLoaders for custom signed assertion providers is null or empty.";
        private const string ProviderNotFound = "The custom signed assertion provider with name '{0}' was not found.";

        public CustomSignedAssertionProviderNotFoundException(string message) : base(message)
        {
        }

        /// <summary>
        /// Use when the SourceLoader library has entries, but the given name is not found.
        /// </summary>
        /// <param name="name">Name of custom signed assertion provider</param>
        /// <returns>An instance of this exception with a relevant message</returns>
        public static CustomSignedAssertionProviderNotFoundException ProviderNameNotFound(string name)
        {
            return new CustomSignedAssertionProviderNotFoundException(message: string.Format(CultureInfo.InvariantCulture, ProviderNotFound, name));
        }

        /// <summary>
        /// Use when the name of the custom signed assertion provider is null or empty.
        /// </summary>
        /// <returns>An instance of this exception with a relevant message</returns>
        public static CustomSignedAssertionProviderNotFoundException ProviderNameNullOrEmpty()
        {
            return new CustomSignedAssertionProviderNotFoundException(NameNullOrEmpty);
        }

        /// <summary>
        /// Use when the SourceLoader library is null or empty.
        /// </summary>
        /// <returns>An instance of this exception with a relevant message</returns>
        public static CustomSignedAssertionProviderNotFoundException SourceLoadersNullOrEmpty()
        {
            return new CustomSignedAssertionProviderNotFoundException(SourceLoaderNullOrEmpty);
        }
    }
}
