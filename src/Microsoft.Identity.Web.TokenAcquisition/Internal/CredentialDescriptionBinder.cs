// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Abstractions;

namespace Microsoft.Identity.Web.Internal
{
    /// <summary>
    /// AOT-safe binder for <see cref="CredentialDescription"/>.
    /// Binds configuration values based on the JSON schema defined in Credentials.json.
    /// </summary>
    internal static class CredentialDescriptionBinder
    {
        /// <summary>
        /// Binds a single <see cref="CredentialDescription"/> from the specified configuration section.
        /// </summary>
        /// <param name="options">The options instance to bind to.</param>
        /// <param name="configurationSection">The configuration section containing the values.</param>
        public static void Bind(CredentialDescription options, IConfigurationSection? configurationSection)
        {
            if (configurationSection is null)
            {
                return;
            }

            // SourceType (enum) - required property that determines which other properties are relevant
            var sourceTypeValue = configurationSection[nameof(CredentialDescription.SourceType)];
            if (!string.IsNullOrEmpty(sourceTypeValue) && Enum.TryParse<CredentialSource>(sourceTypeValue, ignoreCase: true, out var sourceType))
            {
                options.SourceType = sourceType;
            }

            // ClientSecret - for SourceType = ClientSecret
            var clientSecret = configurationSection[nameof(CredentialDescription.ClientSecret)];
            if (!string.IsNullOrEmpty(clientSecret))
            {
                options.ClientSecret = clientSecret;
            }

            // KeyVault properties - for SourceType = KeyVault or SignedAssertionFromVault
            var keyVaultUrl = configurationSection[nameof(CredentialDescription.KeyVaultUrl)];
            if (!string.IsNullOrEmpty(keyVaultUrl))
            {
                options.KeyVaultUrl = keyVaultUrl;
            }

            var keyVaultCertificateName = configurationSection[nameof(CredentialDescription.KeyVaultCertificateName)];
            if (!string.IsNullOrEmpty(keyVaultCertificateName))
            {
                options.KeyVaultCertificateName = keyVaultCertificateName;
            }

            // Base64Encoded - for SourceType = Base64Encoded
            var base64EncodedValue = configurationSection[nameof(CredentialDescription.Base64EncodedValue)];
            if (!string.IsNullOrEmpty(base64EncodedValue))
            {
                options.Base64EncodedValue = base64EncodedValue;
            }

            // Path/File properties - for SourceType = Path
            var certificateDiskPath = configurationSection[nameof(CredentialDescription.CertificateDiskPath)];
            if (!string.IsNullOrEmpty(certificateDiskPath))
            {
                options.CertificateDiskPath = certificateDiskPath;
            }

            var certificatePassword = configurationSection[nameof(CredentialDescription.CertificatePassword)];
            if (!string.IsNullOrEmpty(certificatePassword))
            {
                options.CertificatePassword = certificatePassword;
            }

            // Certificate Store properties - for SourceType = StoreWithThumbprint or StoreWithDistinguishedName
            var certificateStorePath = configurationSection[nameof(CredentialDescription.CertificateStorePath)];
            if (!string.IsNullOrEmpty(certificateStorePath))
            {
                options.CertificateStorePath = certificateStorePath;
            }

            var certificateThumbprint = configurationSection[nameof(CredentialDescription.CertificateThumbprint)];
            if (!string.IsNullOrEmpty(certificateThumbprint))
            {
                options.CertificateThumbprint = certificateThumbprint;
            }

            var certificateDistinguishedName = configurationSection[nameof(CredentialDescription.CertificateDistinguishedName)];
            if (!string.IsNullOrEmpty(certificateDistinguishedName))
            {
                options.CertificateDistinguishedName = certificateDistinguishedName;
            }

            // Managed Identity properties - for SourceType = SignedAssertionFromManagedIdentity
            var managedIdentityClientId = configurationSection[nameof(CredentialDescription.ManagedIdentityClientId)];
            if (!string.IsNullOrEmpty(managedIdentityClientId))
            {
                options.ManagedIdentityClientId = managedIdentityClientId;
            }

            var tokenExchangeUrl = configurationSection[nameof(CredentialDescription.TokenExchangeUrl)];
            if (!string.IsNullOrEmpty(tokenExchangeUrl))
            {
                options.TokenExchangeUrl = tokenExchangeUrl;
            }

            // SignedAssertionFilePath - for SourceType = SignedAssertionFilePath
            var signedAssertionFileDiskPath = configurationSection[nameof(CredentialDescription.SignedAssertionFileDiskPath)];
            if (!string.IsNullOrEmpty(signedAssertionFileDiskPath))
            {
                options.SignedAssertionFileDiskPath = signedAssertionFileDiskPath;
            }

            // CustomSignedAssertion - for SourceType = CustomSignedAssertion
            var customSignedAssertionProviderName = configurationSection[nameof(CredentialDescription.CustomSignedAssertionProviderName)];
            if (!string.IsNullOrEmpty(customSignedAssertionProviderName))
            {
                options.CustomSignedAssertionProviderName = customSignedAssertionProviderName;
            }

            // Note: DecryptKeysAuthenticationOptions is a complex nested type used for AutoDecryptKeys.
            // Skipping for now as it requires a separate binder and is less commonly used.
        }

        /// <summary>
        /// Binds a collection of <see cref="CredentialDescription"/> from a configuration section
        /// representing an array of credentials (e.g., ClientCredentials, TokenDecryptionCredentials).
        /// </summary>
        /// <param name="configurationSection">The configuration section containing the array of credentials.</param>
        /// <returns>A list of bound <see cref="CredentialDescription"/> instances, or null if section is null/empty.</returns>
        public static List<CredentialDescription>? BindCollection(IConfigurationSection? configurationSection)
        {
            if (configurationSection is null)
            {
                return null;
            }

            var children = configurationSection.GetChildren();
            List<CredentialDescription>? result = null;

            foreach (var child in children)
            {
                var credential = new CredentialDescription();
                Bind(credential, child);

                // Only add if we successfully bound a SourceType (required property)
                if (credential.SourceType != default)
                {
                    result ??= new List<CredentialDescription>();
                    result.Add(credential);
                }
            }

            return result;
        }
    }
}
