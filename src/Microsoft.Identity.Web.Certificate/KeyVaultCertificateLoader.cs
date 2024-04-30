// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Azure.Identity;
using Azure.Security.KeyVault.Certificates;
using Azure.Security.KeyVault.Secrets;
using System.Globalization;
using System.Security.Cryptography.X509Certificates;
using System;
using Microsoft.Identity.Abstractions;
using System.Threading.Tasks;

namespace Microsoft.Identity.Web
{
    internal sealed class KeyVaultCertificateLoader : ICredentialSourceLoader
    {
        public CredentialSource CredentialSource => CredentialSource.KeyVault;

        public async Task LoadIfNeededAsync(CredentialDescription credentialDescription, CredentialSourceLoaderParameters? _)
        {
            credentialDescription.Certificate = await LoadFromKeyVault(
                            credentialDescription.KeyVaultUrl!,
                            credentialDescription.KeyVaultCertificateName!,
                            credentialDescription.ManagedIdentityClientId ?? UserAssignedManagedIdentityClientId ?? Environment.GetEnvironmentVariable("AZURE_CLIENT_ID"),
                            CertificateLoaderHelper.DetermineX509KeyStorageFlag(credentialDescription));
            credentialDescription.CachedValue = credentialDescription.Certificate;
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
        internal static Task<X509Certificate2?> LoadFromKeyVault(
            string keyVaultUrl,
            string certificateName,
            string? managedIdentityClientId,
            X509KeyStorageFlags x509KeyStorageFlags)
        {
            Uri keyVaultUri = new Uri(keyVaultUrl);
            ManagedIdentityCredential credential = new(managedIdentityClientId);
            CertificateClient certificateClient = new(keyVaultUri, credential);
            SecretClient secretClient = new(keyVaultUri, credential);

            KeyVaultCertificateWithPolicy certificate = certificateClient.GetCertificate(certificateName);

            if (certificate.Properties.NotBefore == null || certificate.Properties.ExpiresOn == null)
            {
                return Task.FromResult<X509Certificate2?>(null);
            }

            if (DateTimeOffset.UtcNow < certificate.Properties.NotBefore || DateTimeOffset.UtcNow > certificate.Properties.ExpiresOn)
            {
                return Task.FromResult<X509Certificate2?>(null);
            }

            // Return a certificate with only the public key if the private key is not exportable.
            if (certificate.Policy?.Exportable != true)
            {
                return Task.FromResult<X509Certificate2?>(new X509Certificate2(
                    certificate.Cer,
                    (string?)null,
                    x509KeyStorageFlags));
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
                return Task.FromResult<X509Certificate2?>(Base64EncodedCertificateLoader.LoadFromBase64Encoded(secret.Value, x509KeyStorageFlags));
            }

            throw new NotSupportedException(
                string.Format(
                    CultureInfo.InvariantCulture,
                    CertificateErrorMessage.OnlyPkcs12IsSupported,
                    secret.Properties.ContentType));
        }
    }
}
