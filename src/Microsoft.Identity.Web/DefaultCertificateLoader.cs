// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Azure.Identity;
using Azure.Security.KeyVault.Certificates;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Certificate Loader.
    /// </summary>
    internal class DefaultCertificateLoader : ICertificateLoader
    {
        /// <summary>
        /// Load the certificate from the description if needed.
        /// </summary>
        /// <param name="certificateDescription">Description of the certificate.</param>
        public void LoadIfNeeded(CertificateDescription certificateDescription)
        {
            if (certificateDescription.Certificate == null)
            {
                switch (certificateDescription.SourceType)
                {
                    case CertificateSource.KeyVault:
                        certificateDescription.Certificate = LoadFromKeyVault(certificateDescription.Container, certificateDescription.ReferenceOrValue);
                        break;
                    case CertificateSource.Base64Encoded:
                        certificateDescription.Certificate = LoadFromBase64Encoded(certificateDescription.ReferenceOrValue);
                        break;
                    case CertificateSource.Path:
                        certificateDescription.Certificate = LoadFromPath(certificateDescription.Container, certificateDescription.ReferenceOrValue);
                        break;
                    case CertificateSource.StoreWithThumbprint:
                        certificateDescription.Certificate = LoadLocalCertificateFromThumbprint(certificateDescription.Container, certificateDescription.ReferenceOrValue);
                        break;
                    case CertificateSource.StoreWithDistinguishedName:
                        certificateDescription.Certificate = LoadFromStoreWithDistinguishedName(certificateDescription.Container, certificateDescription.ReferenceOrValue);
                        break;
                    default:
                        break;
                }
            }
        }

        private static X509Certificate2 LoadFromBase64Encoded(string certificateBase64)
        {
            byte[] decoded = Convert.FromBase64String(certificateBase64);
            return new X509Certificate2(decoded);
        }

        private static X509Certificate2 LoadFromKeyVault(string keyVaultUrl, string certificateName)
        {
            var client = new CertificateClient(new Uri(keyVaultUrl), new DefaultAzureCredential());
            KeyVaultCertificateWithPolicy certificateWithPolicy = client.GetCertificate(certificateName);
            return new X509Certificate2(certificateWithPolicy.Cer);
        }

        private static X509Certificate2 LoadLocalCertificateFromThumbprint(
            string certificateThumbprint,
            string storeDescription = "CurrentUser/My")
        {
            StoreLocation certificateStoreLocation = StoreLocation.CurrentUser;
            StoreName certificateStoreName = StoreName.My;
            ParseStoreLocationAndName(storeDescription, ref certificateStoreLocation, ref certificateStoreName);

            X509Certificate2 cert;
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

        private static X509Certificate2 LoadFromStoreWithDistinguishedName(string certificateSubjectDistinguishedName, string storeDescription = "CurrentUser/My")
        {
            StoreLocation certificateStoreLocation = StoreLocation.CurrentUser;
            StoreName certificateStoreName = StoreName.My;
            ParseStoreLocationAndName(storeDescription, ref certificateStoreLocation, ref certificateStoreName);

            X509Certificate2 cert;
            using (X509Store x509Store = new X509Store(
                 certificateStoreName,
                 certificateStoreLocation))
            {
                var by = X509FindType.FindBySubjectDistinguishedName;
                cert = FindCertificateByCriterium(x509Store, by, certificateSubjectDistinguishedName);
            }

            return cert;
        }

        private static X509Certificate2 LoadFromPath(
            string certificateFileName,
            string password = null)
        {
            return new X509Certificate2(
                certificateFileName,
                password,
                X509KeyStorageFlags.EphemeralKeySet);
        }

        private static void ParseStoreLocationAndName(string storeDescription, ref StoreLocation certificateStoreLocation, ref StoreName certificateStoreName)
        {
            string[] path = storeDescription.Split('/');

            if (path.Length == 2)
            {
                if (path.Length != 2
                    || !Enum.TryParse<StoreLocation>(path[0], true, out certificateStoreLocation)
                    || !Enum.TryParse<StoreName>(path[1], true, out certificateStoreName))
                {
                    throw new ArgumentException("store should be of the form 'StoreLocation/StoreName' with StoreLocation begin 'CurrentUser' or 'CurrentMachine'"
                        + $" and StoreName begin '' or in '{string.Join(", ", typeof(StoreName).GetEnumNames())}'");
                }
            }
        }

        /// <summary>
        /// Find a certificate by criteria.
        /// </summary>
        /// <param name="x509Store"></param>
        /// <param name="identifierCriterium"></param>
        /// <param name="certificateIdentifier"></param>
        /// <returns></returns>
        private static X509Certificate2 FindCertificateByCriterium(
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
    }
}
