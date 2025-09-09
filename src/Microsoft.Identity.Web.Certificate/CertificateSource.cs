// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Source for a certificate.
    /// </summary>
    public enum CertificateSource
    {
        /// <summary>
        /// Certificate itself.
        /// </summary>
        Certificate = 0,

        /// <summary>
        /// From an Azure Key Vault.
        /// </summary>
        KeyVault = 1,

        /// <summary>
        /// Base64 encoded string directly from the configuration.
        /// </summary>
        Base64Encoded = 2,

        /// <summary>
        /// From local path on disk.
        /// </summary>
        Path = 3,

        /// <summary>
        /// From the certificate store, described by its thumbprint.
        /// </summary>
        StoreWithThumbprint = 4,

        /// <summary>
        /// From the certificate store, described by its distinguished name.
        /// </summary>
        StoreWithDistinguishedName = 5,

        /// <summary>
        /// Selected via standardized logic.
        /// </summary>
        /// <remarks>
        /// This functionality to currently Microsoft-Internal.
        /// Currently, it has no meaning outside of Microsoft-Internal scenarios.
        /// </remarks>
        ManagedCertificate = 6,
    }
}
