// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Description of a certificate.
    /// </summary>
    public class CertificateDescription
    {
        /// <summary>
        /// Creates a certificate description from a certificate (by code).
        /// </summary>
        /// <param name="x509certificate2">Certificate.</param>
        /// <returns>A certificate description.</returns>
        public static CertificateDescription FromCertificate(X509Certificate2 x509certificate2)
        {
            return new CertificateDescription
            {
                SourceType = CertificateSource.Certificate,
                Certificate = x509certificate2,
            };
        }

        /// <summary>
        /// Creates a Certificate Description from KeyVault.
        /// </summary>
        /// <param name="keyVaultUrl"></param>
        /// <param name="keyVaultCertificateName"></param>
        /// <returns>A certificate description.</returns>
        public static CertificateDescription FromKeyVault(string keyVaultUrl, string keyVaultCertificateName)
        {
            return new CertificateDescription
            {
                SourceType = CertificateSource.KeyVault,
                Container = keyVaultUrl,
                ReferenceOrValue = keyVaultCertificateName,
            };
        }

        /// <summary>
        /// Create a certificate description from a base 64 encoded value.
        /// </summary>
        /// <param name="base64EncodedValue">base 64 encoded value.</param>
        /// <returns>A certificate description.</returns>
        public static CertificateDescription FromBase64Encoded(string base64EncodedValue)
        {
            return new CertificateDescription
            {
                SourceType = CertificateSource.Base64Encoded,
                Container = string.Empty,
                ReferenceOrValue = base64EncodedValue,
            };
        }

        /// <summary>
        /// Create a certificate description from path on disk.
        /// </summary>
        /// <param name="path">Path were to find the certificate file.</param>
        /// <param name="password">certificate password.</param>
        /// <returns>A certificate description.</returns>
        public static CertificateDescription FromPath(string path, string password = null)
        {
            return new CertificateDescription
            {
                SourceType = CertificateSource.Path,
                Container = path,
                ReferenceOrValue = password,
            };
        }

        /// <summary>
        /// Create a certificate description from a thumbprint and store location (certificate manager on Windows for instance).
        /// </summary>
        /// <param name="certificateThumbprint">Certificate thumbprint.</param>
        /// <param name="certificateStoreLocation">Store location where to find the certificate.</param>
        /// <param name="certificateStoreName">Store name where to find the certificate.</param>
        /// <returns>A certificate description.</returns>
        public static CertificateDescription FromStoreWithThumprint(
            string certificateThumbprint,
            StoreLocation certificateStoreLocation = StoreLocation.CurrentUser,
            StoreName certificateStoreName = StoreName.My)
        {
            return new CertificateDescription
            {
                SourceType = CertificateSource.StoreWithThumbprint,
                Container = $"{certificateStoreLocation}/{certificateStoreName}",
                ReferenceOrValue = certificateThumbprint,
            };
        }

        /// <summary>
        /// Create a certificate description from a certificate distinguished name (such as CN=name)
        /// and store location (certificate manager on Windows for instance).
        /// </summary>
        /// <param name="certificateDistinguishedName">Certificate distinguished named.</param>
        /// <param name="certificateStoreLocation">Store location where to find the certificate.</param>
        /// <param name="certificateStoreName">Store name where to find the certificate.</param>
        /// <returns>A certificate description.</returns>
        public static CertificateDescription FromStoreWithDistinguishedName(
            string certificateDistinguishedName,
            StoreLocation certificateStoreLocation = StoreLocation.CurrentUser,
            StoreName certificateStoreName = StoreName.My)
        {
            return new CertificateDescription
            {
                SourceType = CertificateSource.StoreWithDistinguishedName,
                Container = $"{certificateStoreLocation}/{certificateStoreName}",
                ReferenceOrValue = certificateDistinguishedName,
            };
        }

        /// <summary>
        /// Type of the source of the certificate.
        /// </summary>
        internal CertificateSource SourceType { get; set; }

        /// <summary>
        /// Container in which to find the certificate.
        /// <list type="bullet">
        /// <item>If <see cref="SourceType"/> equals <see cref="CertificateSource.KeyVault"/>, then
        /// the container is the KeyVault base URL</item>
        /// <item>If <see cref="SourceType"/> equals <see cref="CertificateSource.Base64Encoded"/>, then
        /// this value is not used</item>
        /// <item>If <see cref="SourceType"/> equals <see cref="CertificateSource.Path"/>, then
        /// this value is the path on disk where to find the certificate</item>
        /// <item>If <see cref="SourceType"/> equals <see cref="CertificateSource.StoreWithDistinguishedName"/>,
        /// or <see cref="CertificateSource.StoreWithThumbprint"/>, then
        /// this value is the path to the certificate in the cert store, for instance <c>CurrentUser/My</c></item>
        /// </list>
        /// </summary>
        internal string Container { get; set; }

        /// <summary>
        /// URL of the KeyVault for instance https://msidentitywebsamples.vault.azure.net.
        /// </summary>
        public string KeyVaultUrl
        {
            get
            {
                if (SourceType != CertificateSource.KeyVault)
                {
                    throw new ArgumentException("KeyVaultUrl can only be used when SourceType is KeyVault");
                }

                return Container;
            }
            set
            {
                SourceType = CertificateSource.KeyVault;
                Container = value;
            }
        }

        /// <summary>
        /// Certiticate store path, for instance "CurrentUser/My".
        /// </summary>
        /// <remarks>This property should only be used in conjunction with DistinguishName or Thumbprint.</remarks>
        public string CertificateStorePath
        {
            get
            {
                if (SourceType != CertificateSource.StoreWithDistinguishedName
                    && SourceType != CertificateSource.StoreWithThumbprint)
                {
                    throw new ArgumentException("CertificateStorePath can only be used when SourceType is StoreWithDistinguishedName or StoreWithThumbprint");
                }

                return Container;
            }
            set
            {
                Container = value;
            }
        }

        /// <summary>
        /// Certificate distinguished name.
        /// </summary>
        public string CertificateDistinguishedName
        {
            get
            {
                if (SourceType != CertificateSource.StoreWithDistinguishedName)
                {
                    throw new ArgumentException("CertificateDistinguishedName can only be used when SourceType is StoreWithDistinguishedName");
                }

                return ReferenceOrValue;
            }
            set
            {
                SourceType = CertificateSource.StoreWithDistinguishedName;
                ReferenceOrValue = value;
            }
        }

        /// <summary>
        /// Name of the certificate in KeyVault.
        /// </summary>
        public string KeyVaultCertificateName
        {
            get
            {
                if (SourceType != CertificateSource.KeyVault)
                {
                    throw new ArgumentException("KeyVaultCertificateName can only be used when SourceType is KeyVault");
                }

                return ReferenceOrValue;
            }
            set
            {
                SourceType = CertificateSource.KeyVault;
                ReferenceOrValue = value;
            }
        }

        /// <summary>
        /// Certificate thumbprint.
        /// </summary>
        public string CertificateThumbprint
        {
            get
            {
                if (SourceType != CertificateSource.StoreWithThumbprint)
                {
                    throw new ArgumentException("StoreWithThumbprint can only be used when SourceType is StoreWithThumbprint");
                }

                return ReferenceOrValue;
            }
            set
            {
                SourceType = CertificateSource.StoreWithThumbprint;
                ReferenceOrValue = value;
            }
        }

        /// <summary>
        /// Path on disk to the certificate.
        /// </summary>
        public string CertificateDiskPath
        {
            get
            {
                if (SourceType != CertificateSource.Path)
                {
                    throw new ArgumentException("CertificateDiskPath can only be used when SourceType is Path");
                }

                return ReferenceOrValue;
            }
            set
            {
                SourceType = CertificateSource.Path;
                ReferenceOrValue = value;
            }
        }

        /// <summary>
        /// Base 64 encoded value.
        /// </summary>
        public string Base64EncodedValue
        {
            get
            {
                if (SourceType != CertificateSource.Base64Encoded)
                {
                    throw new ArgumentException("Base64EncodedValue can only be used when SourceType is Base64Encoded");
                }

                return ReferenceOrValue;
            }
            set
            {
                SourceType = CertificateSource.Base64Encoded;
                ReferenceOrValue = value;
            }
        }

        /// <summary>
        /// Reference to the certificate or value.
        /// </summary>
        /// <list type="bullet">
        /// <item>If <see cref="SourceType"/> equals <see cref="CertificateSource.KeyVault"/>, then
        /// the reference is the name of the certificate in KeyVault (maybe the version?)</item>
        /// <item>If <see cref="SourceType"/> equals <see cref="CertificateSource.Base64Encoded"/>, then
        /// this value is the base 64 encoded certificate itself</item>
        /// <item>If <see cref="SourceType"/> equals <see cref="CertificateSource.Path"/>, then
        /// this value is the password to access the certificate (if needed)</item>
        /// <item>If <see cref="SourceType"/> equals <see cref="CertificateSource.StoreWithDistinguishedName"/>,
        /// this value is the distinguished name.</item>
        /// <item>If <see cref="SourceType"/> equals <see cref="CertificateSource.StoreWithThumbprint"/>,
        /// this value is the thumbprint.</item>
        /// </list>
        internal string ReferenceOrValue { get; set; }

        /// <summary>
        /// The certificate, either provided directly in code by the
        /// or loaded from the description.
        /// </summary>
        public X509Certificate2 Certificate { get; internal set; }
    }
}
