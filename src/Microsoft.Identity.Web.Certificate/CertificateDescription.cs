// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Identity.Abstractions;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Description of a certificate.
    /// </summary>
    public class CertificateDescription : CredentialDescription
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public CertificateDescription()
        {
        }

        /// <summary>
        /// Creates a certificate description from a credential description.
        /// </summary>
        /// <param name="credentialDescription"></param>
        public CertificateDescription(CredentialDescription credentialDescription)
            : base(credentialDescription)
        {
            _ = Throws.IfNull(credentialDescription);

            // TODO: Check credentialDescription is really a cert
            SourceType = (CertificateSource)credentialDescription.SourceType;
        }

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
        public static CertificateDescription FromKeyVault(
            string keyVaultUrl,
            string keyVaultCertificateName)
        {
            return new CertificateDescription
            {
                SourceType = CertificateSource.KeyVault,
                KeyVaultUrl = keyVaultUrl,
                KeyVaultCertificateName = keyVaultCertificateName,
            };
        }

        /// <summary>
        /// Creates a certificate description from a Base64 encoded value.
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
        /// Creates a certificate description from a Base64 encoded value.
        /// </summary>
        /// <param name="base64EncodedValue">Base64 encoded certificate value.</param>
        /// <param name="password">The password to use when decoding the certificate.</param>
        /// <returns>A certificate description.</returns>
        public static CertificateDescription FromBase64Encoded(string base64EncodedValue, string password)
        {
            return new CertificateDescription
            {
                SourceType = CertificateSource.Base64Encoded,
                Base64EncodedValue = base64EncodedValue,
                CertificatePassword = password
            };
        }

        /// <summary>
        /// Creates a certificate description from path on disk.
        /// </summary>
        /// <param name="path">Path where to find the certificate file.</param>
        /// <param name="password">Certificate password.</param>
        /// <returns>A certificate description.</returns>
        public static CertificateDescription FromPath(string path, string? password = null)
        {
            return new CertificateDescription
            {
                SourceType = CertificateSource.Path,
                CertificateDiskPath = path,
                CertificatePassword = password,
            };
        }

        /// <summary>
        /// Creates a certificate description from a thumbprint and store location (Certificate Manager on Windows, for instance).
        /// </summary>
        /// <param name="certificateThumbprint">Certificate thumbprint.</param>
        /// <param name="certificateStoreLocation">Store location where to find the certificate.</param>
        /// <param name="certificateStoreName">Store name where to find the certificate.</param>
        /// <returns>A certificate description.</returns>
        public static CertificateDescription FromStoreWithThumbprint(
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
        /// Creates a certificate description from a certificate distinguished name (such as CN=name)
        /// and store location (Certificate Manager on Windows, for instance).
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
        ///  Defines where and how to import the private key of an X.509 certificate.
        /// </summary>
        public X509KeyStorageFlags X509KeyStorageFlags { get; set; } = CertificateLoaderHelper.DetermineX509KeyStorageFlag();

        // Should Container and ReferenceOrValue be moved to
        // the tests (As extension methods)
        /// <summary>
        /// <inheritdoc/>.
        /// </summary>
        public new X509Certificate2? Certificate
        {
            get { return base.Certificate; }
            protected internal set { base.Certificate = value; }
        }
        
        /// <summary>
        /// <inheritdoc/>.
        /// </summary>
        public new CertificateSource SourceType
        {
            get { return (CertificateSource)base.SourceType; }
            set { base.SourceType = (CredentialSource)value; }
        }
    }
}
