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
        /// Creates a certificate description from Key Vault.
        /// </summary>
        /// <param name="keyVaultUrl">The Key Vault URL.</param>
        /// <param name="keyVaultCertificateName">The name of the certificate in Key Vault.</param>
        /// <returns>A certificate description.</returns>
        public static CertificateDescription FromKeyVault(string keyVaultUrl, string keyVaultCertificateName)
        {
            return new CertificateDescription
            {
                SourceType = CertificateSource.KeyVault,
                KeyVaultUrl = keyVaultUrl,
                KeyVaultCertificateName = keyVaultCertificateName,
            };
        }

        /// <summary>
        /// Create a certificate description from a Base64 encoded value.
        /// </summary>
        /// <param name="base64EncodedValue">Base64 encoded certificate value.</param>
        /// <returns>A certificate description.</returns>
        public static CertificateDescription FromBase64Encoded(string base64EncodedValue)
        {
            return new CertificateDescription
            {
                SourceType = CertificateSource.Base64Encoded,
                Base64EncodedValue = base64EncodedValue,
            };
        }

        /// <summary>
        /// Create a certificate description from path on disk.
        /// </summary>
        /// <param name="path">Path were to find the certificate file.</param>
        /// <param name="password">Certificate password.</param>
        /// <returns>A certificate description.</returns>
        public static CertificateDescription FromPath(string path, string password = null)
        {
            return new CertificateDescription
            {
                SourceType = CertificateSource.Path,
                CertificateDiskPath = path,
                CertificatePassword = password,
            };
        }

        /// <summary>
        /// Create a certificate description from a thumbprint and store location (Certificate Manager on Windows for instance).
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
                CertificateStorePath = $"{certificateStoreLocation}/{certificateStoreName}",
                CertificateThumbprint = certificateThumbprint,
            };
        }

        /// <summary>
        /// Create a certificate description from a certificate distinguished name (such as CN=name)
        /// and store location (Certificate Manager on Windows for instance).
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
                CertificateStorePath = $"{certificateStoreLocation}/{certificateStoreName}",
                CertificateDistinguishedName = certificateDistinguishedName,
            };
        }

        /// <summary>
        /// Type of the source of the certificate.
        /// </summary>
        public CertificateSource SourceType { get; set; }

        /// <summary>
        /// Container in which to find the certificate.
        /// <list type="bullet">
        /// <item>If <see cref="SourceType"/> equals <see cref="CertificateSource.KeyVault"/>, then
        /// the container is the Key Vault base URL.</item>
        /// <item>If <see cref="SourceType"/> equals <see cref="CertificateSource.Base64Encoded"/>, then
        /// this value is not used.</item>
        /// <item>If <see cref="SourceType"/> equals <see cref="CertificateSource.Path"/>, then
        /// this value is the path on disk where to find the certificate.</item>
        /// <item>If <see cref="SourceType"/> equals <see cref="CertificateSource.StoreWithDistinguishedName"/>,
        /// or <see cref="CertificateSource.StoreWithThumbprint"/>, then
        /// this value is the path to the certificate in the cert store, for instance <c>CurrentUser/My</c>.</item>
        /// </list>
        /// </summary>
        internal string Container
        {
            get
            {
                switch (SourceType)
                {
                    case CertificateSource.Certificate:
                        return null;
                    case CertificateSource.KeyVault:
                        return KeyVaultUrl;
                    case CertificateSource.Base64Encoded:
                        return null;
                    case CertificateSource.Path:
                        return CertificateDiskPath;
                    case CertificateSource.StoreWithThumbprint:
                    case CertificateSource.StoreWithDistinguishedName:
                        return CertificateStorePath;
                    default:
                        return null;
                }
            }
            set
            {
                switch (SourceType)
                {
                    case CertificateSource.Certificate:
                        break;
                    case CertificateSource.KeyVault:
                        KeyVaultUrl = value;
                        break;
                    case CertificateSource.Base64Encoded:
                        break;
                    case CertificateSource.Path:
                        CertificateDiskPath = value;
                        break;
                    case CertificateSource.StoreWithDistinguishedName:
                    case CertificateSource.StoreWithThumbprint:
                        CertificateStorePath = value;
                        break;
                    default:
                        break;
                }
            }
        }

        /// <summary>
        /// URL of the Key Vault for instance https://msidentitywebsamples.vault.azure.net.
        /// </summary>
        public string KeyVaultUrl { get; set; }

        /// <summary>
        /// Certificate store path, for instance "CurrentUser/My".
        /// </summary>
        /// <remarks>This property should only be used in conjunction with DistinguishName or Thumbprint.</remarks>
        public string CertificateStorePath { get; set; }

        /// <summary>
        /// Certificate distinguished name.
        /// </summary>
        public string CertificateDistinguishedName { get; set; }

        /// <summary>
        /// Name of the certificate in Key Vault.
        /// </summary>
        public string KeyVaultCertificateName { get; set; }

        /// <summary>
        /// Certificate thumbprint.
        /// </summary>
        public string CertificateThumbprint { get; set; }

        /// <summary>
        /// Path on disk to the certificate.
        /// </summary>
        public string CertificateDiskPath { get; set; }

        /// <summary>
        /// Path on disk to the certificate password.
        /// </summary>
        public string CertificatePassword { get; set; }

        /// <summary>
        /// Base64 encoded certificate value.
        /// </summary>
        public string Base64EncodedValue { get; set; }

        /// <summary>
        /// Reference to the certificate or value.
        /// </summary>
        /// <list type="bullet">
        /// <item>If <see cref="SourceType"/> equals <see cref="CertificateSource.KeyVault"/>, then
        /// the reference is the name of the certificate in Key Vault (maybe the version?).</item>
        /// <item>If <see cref="SourceType"/> equals <see cref="CertificateSource.Base64Encoded"/>, then
        /// this value is the base 64 encoded certificate itself.</item>
        /// <item>If <see cref="SourceType"/> equals <see cref="CertificateSource.Path"/>, then
        /// this value is the password to access the certificate (if needed).</item>
        /// <item>If <see cref="SourceType"/> equals <see cref="CertificateSource.StoreWithDistinguishedName"/>,
        /// this value is the distinguished name.</item>
        /// <item>If <see cref="SourceType"/> equals <see cref="CertificateSource.StoreWithThumbprint"/>,
        /// this value is the thumbprint.</item>
        /// </list>
        internal string ReferenceOrValue
        {
            get
            {
                switch (SourceType)
                {
                    case CertificateSource.KeyVault:
                        return KeyVaultCertificateName;
                    case CertificateSource.Path:
                        return CertificatePassword;
                    case CertificateSource.StoreWithThumbprint:
                        return CertificateThumbprint;
                    case CertificateSource.StoreWithDistinguishedName:
                        return CertificateDistinguishedName;
                    case CertificateSource.Certificate:
                    case CertificateSource.Base64Encoded:
                        return Base64EncodedValue;
                    default:
                        return null;
                }
            }
            set
            {
                switch (SourceType)
                {
                    case CertificateSource.Certificate:
                        break;
                    case CertificateSource.KeyVault:
                        KeyVaultCertificateName = value;
                        break;
                    case CertificateSource.Base64Encoded:
                        Base64EncodedValue = value;
                        break;
                    case CertificateSource.Path:
                        CertificateDiskPath = value;
                        break;
                    case CertificateSource.StoreWithThumbprint:
                        CertificateThumbprint = value;
                        break;
                    case CertificateSource.StoreWithDistinguishedName:
                        CertificateDistinguishedName = value;
                        break;
                    default:
                        break;
                }
            }
        }

        /// <summary>
        /// The certificate, either provided directly in code
        /// or loaded from the description.
        /// </summary>
        public X509Certificate2 Certificate { get; internal set; }
    }
}
