// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Identity.Web
{
    internal class CertificateLoaderHelper
    {
        internal static X509KeyStorageFlags DetermineX509KeyStorageFlag(CredentialDescription credentialDescription)
        {
            X509KeyStorageFlags x509KeyStorageFlags;
            if (credentialDescription is CertificateDescription)
            {
                x509KeyStorageFlags = ((CertificateDescription)credentialDescription).X509KeyStorageFlags;
            }
            else
            {
#if NET462 || NETSTANDARD2_0
                x509KeyStorageFlags = X509KeyStorageFlags.MachineKeySet;
#else
                x509KeyStorageFlags = X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.EphemeralKeySet;
#endif
            }

            return x509KeyStorageFlags;
        }

        internal static void ParseStoreLocationAndName(
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
                    CertificateErrorMessage.InvalidCertificateStorePath,
                    string.Join("', '", typeof(StoreName).GetEnumNames())));
            }
        }

        /// <summary>
        /// Find a certificate by criteria.
        /// </summary>
        internal static X509Certificate2? FindCertificateByCriterium(
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
