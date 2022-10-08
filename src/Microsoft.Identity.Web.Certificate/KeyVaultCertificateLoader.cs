// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Azure.Identity;
using Azure.Security.KeyVault.Certificates;
using Azure.Security.KeyVault.Secrets;
using System.Globalization;
using System.Security.Cryptography.X509Certificates;
using System;
using Microsoft.Identity.Abstractions;

namespace Microsoft.Identity.Web
{
    internal class KeyVaultCertificateLoader : ICredentialLoader
    {
        public CredentialSource CredentialSource => CredentialSource.KeyVault;

        public void LoadIfNeeded(CredentialDescription credentialDescription)
        {
            credentialDescription.Certificate = LoadFromKeyVault(
                            credentialDescription.KeyVaultUrl!,
                            credentialDescription.KeyVaultCertificateName!,
                            credentialDescription.ManagedIdentityClientId ?? UserAssignedManagedIdentityClientId,
                            CertificateLoaderHelper.DetermineX509KeyStorageFlag(credentialDescription));
        }

        public static string? UserAssignedManagedIdentityClientId { get; set; }

        /// <summary>
        /// Load a certificate from Key Vault, including the private key.
        /// </summary>
        /// <param name="keyVaultUrl">URL of Key Vault.</param>
        /// <param name="certificateName">Name of the certificate.</param>
        /// <param name="managedIdentityClientId"></param>
        /// <param name="x509KeyStorageFlags">Defines where and how to import the private key of an X.509 certificate.</param>
        /// <returns>An <see cref="X509Certificate2"/> certificate.</returns>
        /// <remarks>This code is inspired by Heath Stewart's code in:
        /// https://github.com/heaths/azsdk-sample-getcert/blob/master/Program.cs#L46-L82.
        /// </remarks>
        internal static X509Certificate2? LoadFromKeyVault(
            string keyVaultUrl,
            string certificateName,
            string? managedIdentityClientId,
            X509KeyStorageFlags x509KeyStorageFlags)
        {
            Uri keyVaultUri = new Uri(keyVaultUrl);
            DefaultAzureCredentialOptions options = new()
            {
                ManagedIdentityClientId = managedIdentityClientId,
            };
            DefaultAzureCredential credential = new(options);
            CertificateClient certificateClient = new(keyVaultUri, credential);
            SecretClient secretClient = new(keyVaultUri, credential);

            KeyVaultCertificateWithPolicy certificate = certificateClient.GetCertificate(certificateName);

            if (certificate.Properties.NotBefore == null || certificate.Properties.ExpiresOn == null)
            {
                return null;
            }

            if (DateTimeOffset.UtcNow < certificate.Properties.NotBefore || DateTimeOffset.UtcNow > certificate.Properties.ExpiresOn)
            {
                return null;
            }

            // Return a certificate with only the public key if the private key is not exportable.
            if (certificate.Policy?.Exportable != true)
            {
                return new X509Certificate2(
                    certificate.Cer,
                    (string?)null,
                    x509KeyStorageFlags);
            }

            // Parse the secret ID and version to retrieve the private key.
            string[] segments = certificate.SecretId.AbsolutePath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length != 3)
            {
                throw new InvalidOperationException(string.Format(
                    CultureInfo.InvariantCulture,
                    CertificateErrorMessage.IncorrectNumberOfUriSegments,
                    segments.Length,
                    certificate.SecretId));
            }

            string secretName = segments[1];
            string secretVersion = segments[2];

            KeyVaultSecret secret = secretClient.GetSecret(secretName, secretVersion);

            // For PEM, you'll need to extract the base64-encoded message body.
            // .NET 5.0 preview introduces the System.Security.Cryptography.PemEncoding class to make this easier.
            if (CertificateConstants.MediaTypePksc12.Equals(secret.Properties.ContentType, StringComparison.OrdinalIgnoreCase))
            {
                return Base64EncodedCertificateLoader.LoadFromBase64Encoded(secret.Value, x509KeyStorageFlags);
            }

            throw new NotSupportedException(
                string.Format(
                    CultureInfo.InvariantCulture,
                    CertificateErrorMessage.OnlyPkcs12IsSupported,
                    secret.Properties.ContentType));
        }
    }
}
