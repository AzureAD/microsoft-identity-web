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
        /// Certificate itself
        /// </summary>
        Certificate = 0,

        /// <summary>
        /// KeyVault
        /// </summary>
        KeyVault = 1,

        /// <summary>
        /// Base 64 encoded directly in the configuration.
        /// </summary>
        Base64Encoded = 2,

        /// <summary>
        /// Local path on disk
        /// </summary>
        Path = 3,

        /// <summary>
        /// From the certificate store, described by its thumbprint.
        /// </summary>
        StoreWithThumbprint = 4,

        /// <summary>
        /// From the certificate store, described by its Distinguished name.
        /// </summary>
        StoreWithDistinguishedName = 5,
    }
}
