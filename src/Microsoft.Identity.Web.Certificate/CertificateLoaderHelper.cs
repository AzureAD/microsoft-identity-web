// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web.Diagnostics;

namespace Microsoft.Identity.Web
{
    internal sealed class CertificateLoaderHelper
    {
        private static Lazy<X509KeyStorageFlags> s_x509KeyStorageFlags = 
            new Lazy<X509KeyStorageFlags>(DetermineX509KeyStorageFlagLazy);

        internal static X509KeyStorageFlags DetermineX509KeyStorageFlag(CredentialDescription credentialDescription)
        {
            if (credentialDescription is CertificateDescription credDescription)
            {
                return ((CertificateDescription)credentialDescription).X509KeyStorageFlags;
            }
            else
            {
                return DetermineX509KeyStorageFlag();
            }
        }
        
        internal static X509KeyStorageFlags DetermineX509KeyStorageFlag()
        {
            return s_x509KeyStorageFlags.Value;
        }

        private static X509KeyStorageFlags DetermineX509KeyStorageFlagLazy()
        {
#if NET462 || NETSTANDARD2_0
            return X509KeyStorageFlags.MachineKeySet;
#else
            // This is for app developers using a Mac. MacOS does not support the EphemeralKeySet flag.
            // See https://learn.microsoft.com/en-us/dotnet/standard/security/cross-platform-cryptography#write-a-pkcs12pfx
            if (OsHelper.IsMacPlatform())
            {
                return X509KeyStorageFlags.DefaultKeySet;
            }

            return X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.EphemeralKeySet;
#endif
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
