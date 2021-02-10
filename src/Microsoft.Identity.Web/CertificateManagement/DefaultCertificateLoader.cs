// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Azure.Identity;
using Azure.Security.KeyVault.Certificates;
using Azure.Security.KeyVault.Secrets;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Certificate Loader.
    /// Only use when loading a certificate from a daemon application, or an ASP NET app, using MSAL .NET directly.
    /// For an ASP NET Core app, <b>Microsoft Identity Web will handle the certificate loading</b> for you.
    /// <example><code>
    /// IConfidentialClientApplication app;
    /// ICertificateLoader certificateLoader = new DefaultCertificateLoader();
    ///     certificateLoader.LoadIfNeeded(config.CertificateDescription);
    ///
    ///    app = ConfidentialClientApplicationBuilder.Create(config.ClientId)
    ///           .WithCertificate(config.CertificateDescription.Certificate)
    ///           .WithAuthority(new Uri(config.Authority))
    ///           .Build();
    /// </code></example>
    /// </summary>
    public class DefaultCertificateLoader : ICertificateLoader
    {
        /// <summary>
        /// Load the certificate from the description, if needed.
        /// </summary>
        /// <param name="certificateDescription">Description of the certificate.</param>
        public void LoadIfNeeded(CertificateDescription certificateDescription)
        {
            if (certificateDescription == null)
            {
                throw new ArgumentNullException(nameof(certificateDescription));
            }

            if (certificateDescription.Certificate == null)
            {
                switch (certificateDescription.SourceType)
                {
                    case CertificateSource.KeyVault:
                        certificateDescription.Certificate = LoadFromKeyVault(
                            certificateDescription.Container!,
                            certificateDescription.ReferenceOrValue!);
                        break;
                    case CertificateSource.Base64Encoded:
                        certificateDescription.Certificate = LoadFromBase64Encoded(
                            certificateDescription.ReferenceOrValue!);
                        break;
                    case CertificateSource.Path:
                        certificateDescription.Certificate = LoadFromPath(
                            certificateDescription.Container!,
                            certificateDescription.ReferenceOrValue!);
                        break;
                    case CertificateSource.StoreWithThumbprint:
                        certificateDescription.Certificate = LoadFromStoreWithThumbprint(
                            certificateDescription.ReferenceOrValue!,
                            certificateDescription.Container!);
                        break;
                    case CertificateSource.StoreWithDistinguishedName:
                        certificateDescription.Certificate = LoadFromStoreWithDistinguishedName(
                            certificateDescription.ReferenceOrValue!,
                            certificateDescription.Container!);
                        break;
                    default:
                        break;
                }
            }
        }

        private static X509Certificate2 LoadFromBase64Encoded(string certificateBase64)
        {
            byte[] decoded = Convert.FromBase64String(certificateBase64);
            return new X509Certificate2(
                decoded,
                (string?)null,
                X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.EphemeralKeySet);
        }

        /// <summary>
        /// Load a certificate from Key Vault, including the private key.
        /// </summary>
        /// <param name="keyVaultUrl">URL of Key Vault.</param>
        /// <param name="certificateName">Name of the certificate.</param>
        /// <returns>An <see cref="X509Certificate2"/> certificate.</returns>
        /// <remarks>This code is inspired by Heath Stewart's code in:
        /// https://github.com/heaths/azsdk-sample-getcert/blob/master/Program.cs#L46-L82.
        /// </remarks>
        private static X509Certificate2 LoadFromKeyVault(string keyVaultUrl, string certificateName)
        {
            Uri keyVaultUri = new Uri(keyVaultUrl);
            DefaultAzureCredential credential = new DefaultAzureCredential();
            CertificateClient certificateClient = new CertificateClient(keyVaultUri, credential);
            SecretClient secretClient = new SecretClient(keyVaultUri, credential);

            KeyVaultCertificateWithPolicy certificate = certificateClient.GetCertificate(certificateName);

            // Return a certificate with only the public key if the private key is not exportable.
            if (certificate.Policy?.Exportable != true)
            {
                return new X509Certificate2(
                    certificate.Cer,
                    (string?)null,
                    X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.EphemeralKeySet);
            }

            // Parse the secret ID and version to retrieve the private key.
            string[] segments = certificate.SecretId.AbsolutePath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length != 3)
            {
                throw new InvalidOperationException(string.Format(
                    CultureInfo.InvariantCulture,
                    IDWebErrorMessage.IncorrectNumberOfUriSegments,
                    segments.Length,
                    certificate.SecretId));
            }

            string secretName = segments[1];
            string secretVersion = segments[2];

            KeyVaultSecret secret = secretClient.GetSecret(secretName, secretVersion);

            // For PEM, you'll need to extract the base64-encoded message body.
            // .NET 5.0 preview introduces the System.Security.Cryptography.PemEncoding class to make this easier.
            if (Constants.MediaTypePksc12.Equals(secret.Properties.ContentType, StringComparison.InvariantCultureIgnoreCase))
            {
                return LoadFromBase64Encoded(secret.Value);
            }

            throw new NotSupportedException(
                string.Format(
                    CultureInfo.InvariantCulture,
                    IDWebErrorMessage.OnlyPkcs12IsSupported,
                    secret.Properties.ContentType));
        }

        private static X509Certificate2? LoadFromStoreWithThumbprint(
            string certificateThumbprint,
            string storeDescription = Constants.PersonalUserCertificateStorePath)
        {
            StoreLocation certificateStoreLocation = StoreLocation.CurrentUser;
            StoreName certificateStoreName = StoreName.My;
            ParseStoreLocationAndName(
                storeDescription,
                ref certificateStoreLocation,
                ref certificateStoreName);

            X509Certificate2? cert;
            using (X509Store x509Store = new X509Store(
                certificateStoreName,
                certificateStoreLocation))
            {
                cert = FindCertificateByCriterium(
                   x509Store,
                   X509FindType.FindByThumbprint,
                   certificateThumbprint);
            }

            return cert;
        }

        private static X509Certificate2? LoadFromStoreWithDistinguishedName(
            string certificateSubjectDistinguishedName,
            string storeDescription = Constants.PersonalUserCertificateStorePath)
        {
            StoreLocation certificateStoreLocation = StoreLocation.CurrentUser;
            StoreName certificateStoreName = StoreName.My;
            ParseStoreLocationAndName(
                storeDescription,
                ref certificateStoreLocation,
                ref certificateStoreName);

            X509Certificate2? cert;
            using (X509Store x509Store = new X509Store(
                 certificateStoreName,
                 certificateStoreLocation))
            {
                cert = FindCertificateByCriterium(
                    x509Store,
                    X509FindType.FindBySubjectDistinguishedName,
                    certificateSubjectDistinguishedName);
            }

            return cert;
        }

        private static X509Certificate2 LoadFromPath(
            string certificateFileName,
            string? password = null)
        {
            return new X509Certificate2(
                certificateFileName,
                password,
                X509KeyStorageFlags.EphemeralKeySet);
        }

        private static void ParseStoreLocationAndName(
            string storeDescription,
            ref StoreLocation certificateStoreLocation,
            ref StoreName certificateStoreName)
        {
            string[] path = storeDescription.Split('/');

            if (path.Length != 2
                || !Enum.TryParse<StoreLocation>(path[0], true, out certificateStoreLocation)
                || !Enum.TryParse<StoreName>(path[1], true, out certificateStoreName))
            {
                throw new ArgumentException(string.Format(
                    CultureInfo.InvariantCulture,
                    IDWebErrorMessage.InvalidCertificateStorePath,
                    string.Join("', '", typeof(StoreName).GetEnumNames())));
            }
        }

        /// <summary>
        /// Find a certificate by criteria.
        /// </summary>
        private static X509Certificate2? FindCertificateByCriterium(
            X509Store x509Store,
            X509FindType identifierCriterium,
            string certificateIdentifier)
        {
            x509Store.Open(OpenFlags.ReadOnly);

            X509Certificate2Collection certCollection = x509Store.Certificates;

            // Find unexpired certificates.
            X509Certificate2Collection currentCerts = certCollection.Find(X509FindType.FindByTimeValid, DateTime.Now, false);

            // From the collection of unexpired certificates, find the ones with the correct name.
            X509Certificate2Collection signingCert = currentCerts.Find(identifierCriterium, certificateIdentifier, false);

            // Return the first certificate in the collection, has the right name and is current.
            var cert = signingCert.OfType<X509Certificate2>().OrderByDescending(c => c.NotBefore).FirstOrDefault();
            return cert;
        }

        internal /*for test only*/ static X509Certificate2? LoadFirstCertificate(IEnumerable<CertificateDescription> certificateDescription)
        {
            DefaultCertificateLoader defaultCertificateLoader = new DefaultCertificateLoader();
            CertificateDescription certDescription = certificateDescription.First();
            defaultCertificateLoader.LoadIfNeeded(certDescription);
            return certDescription.Certificate;
        }

        internal /*for test only*/ static IEnumerable<X509Certificate2?> LoadAllCertificates(IEnumerable<CertificateDescription> certificateDescriptions)
        {
            DefaultCertificateLoader defaultCertificateLoader = new DefaultCertificateLoader();
            foreach (var certDescription in certificateDescriptions)
            {
                defaultCertificateLoader.LoadIfNeeded(certDescription);
                yield return certDescription.Certificate;
            }
        }
    }
}
