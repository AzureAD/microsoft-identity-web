// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Abstractions
{
    /// <summary>
    /// Source for a credential.
    /// </summary>
    public enum CredentialSource
    {
        /// <summary>
        /// Certificate.
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
        /// Client secret.
        /// </summary>
        ClientSecret=6,

        /// <summary>
        /// Certificateless with managed identity.
        /// </summary>
        SignedAssertionFromManagedIdentity=7,

        /// <summary>
        /// Path to the file containing the signed assertion (for Kubernetes).
        /// </summary>
        SignedAssertionFilePath = 8,
    }
}
