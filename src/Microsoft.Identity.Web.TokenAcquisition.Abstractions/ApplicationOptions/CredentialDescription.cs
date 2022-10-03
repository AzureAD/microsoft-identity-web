// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Identity.Abstractions
{
    /// <summary>
    /// Description of a credential.
    /// </summary>
    public class CredentialDescription
    {
        /// <summary>
        /// Type of the source of the credential.
        /// </summary>
        public CredentialSource SourceType { get; set; }

        /// <summary>
        /// Container in which to find the credential.
        /// <list type="bullet">
        /// <item>If <see cref="SourceType"/> equals <see cref="CredentialSource.KeyVault"/>, then
        /// the container is the Key Vault base URL.</item>
        /// <item>If <see cref="SourceType"/> equals <see cref="CredentialSource.Base64Encoded"/>, then
        /// this value is not used.</item>
        /// <item>If <see cref="SourceType"/> equals <see cref="CredentialSource.Path"/>, then
        /// this value is the path on disk where to find the credential.</item>
        /// <item>If <see cref="SourceType"/> equals <see cref="CredentialSource.StoreWithDistinguishedName"/>,
        /// or <see cref="CredentialSource.StoreWithThumbprint"/>, then
        /// this value is the path to the credential in the cert store, for instance <c>CurrentUser/My</c>.</item>
        /// </list>
        /// </summary>
        public string? Container
        {
            get
            {
                return SourceType switch
                {
                    CredentialSource.Certificate => null,
                    CredentialSource.KeyVault => KeyVaultUrl,
                    CredentialSource.Base64Encoded => null,
                    CredentialSource.Path => CertificateDiskPath,
                    CredentialSource.StoreWithThumbprint or CredentialSource.StoreWithDistinguishedName => CertificateStorePath,
                    _ => null,
                };
            }
            set
            {
                switch (SourceType)
                {
                    case CredentialSource.Certificate:
                        break;
                    case CredentialSource.KeyVault:
                        KeyVaultUrl = value;
                        break;
                    case CredentialSource.Base64Encoded:
                        break;
                    case CredentialSource.Path:
                        CertificateDiskPath = value;
                        break;
                    case CredentialSource.StoreWithDistinguishedName:
                    case CredentialSource.StoreWithThumbprint:
                        CertificateStorePath = value;
                        break;
                    default:
                        break;
                }
            }
        }

        /// <summary>
        /// URL of the Key Vault, for instance https://msidentitywebsamples.vault.azure.net.
        /// </summary>
        public string? KeyVaultUrl { get; set; }

        /// <summary>
        /// Certificate store path, for instance "CurrentUser/My".
        /// </summary>
        /// <remarks>This property should only be used in conjunction with DistinguishedName or Thumbprint.</remarks>
        public string? CertificateStorePath { get; set; }

        /// <summary>
        /// Certificate distinguished name.
        /// </summary>
        public string? CertificateDistinguishedName { get; set; }

        /// <summary>
        /// Name of the certificate in Key Vault.
        /// </summary>
        public string? KeyVaultCertificateName { get; set; }

        /// <summary>
        /// Certificate thumbprint.
        /// </summary>
        public string? CertificateThumbprint { get; set; }

        /// <summary>
        /// Path on disk to the certificate.
        /// </summary>
        public string? CertificateDiskPath { get; set; }

        /// <summary>
        /// Path on disk to the certificate password.
        /// </summary>
        public string? CertificatePassword { get; set; }

        /// <summary>
        /// Base64 encoded certificate value.
        /// </summary>
        public string? Base64EncodedValue { get; set; }

        /// <summary>
        /// Client Secret.
        /// </summary>
        public string? ClientSecret { get; set; }

        /// <summary>
        /// ClientId of the Azure managed identity.
        /// </summary>
        public string? ManagedIdentityClientId { get; set; }

        /// <summary>
        /// Path on disk to the signed assertion (for Kubernetes).
        /// </summary>
        public string? SignedAssertionFileDiskPath { get; set; }

        /// <summary>
        /// Reference to the certificate or value.
        /// </summary>
        /// <list type="bullet">
        /// <item>If <see cref="SourceType"/> equals <see cref="CredentialSource.KeyVault"/>, then
        /// the reference is the name of the certificate in Key Vault (maybe the version?).</item>
        /// <item>If <see cref="SourceType"/> equals <see cref="CredentialSource.Base64Encoded"/>, then
        /// this value is the base 64 encoded certificate itself.</item>
        /// <item>If <see cref="SourceType"/> equals <see cref="CredentialSource.Path"/>, then
        /// this value is the password to access the certificate (if needed).</item>
        /// <item>If <see cref="SourceType"/> equals <see cref="CredentialSource.StoreWithDistinguishedName"/>,
        /// this value is the distinguished name.</item>
        /// <item>If <see cref="SourceType"/> equals <see cref="CredentialSource.StoreWithThumbprint"/>,
        /// this value is the thumbprint.</item>
        /// </list>
        public string? ReferenceOrValue
        {
            get
            {
                return SourceType switch
                {
                    CredentialSource.KeyVault => KeyVaultCertificateName,
                    CredentialSource.Path => CertificatePassword,
                    CredentialSource.StoreWithThumbprint => CertificateThumbprint,
                    CredentialSource.StoreWithDistinguishedName => CertificateDistinguishedName,
                    CredentialSource.Certificate or CredentialSource.Base64Encoded => Base64EncodedValue,
                    CredentialSource.ClientSecret => ClientSecret,
                    CredentialSource.SignedAssertionFromManagedIdentity => ManagedIdentityClientId,
                    CredentialSource.SignedAssertionFilePath => SignedAssertionFileDiskPath,
                    _ => null,
                };
            }
            set
            {
                switch (SourceType)
                {
                    case CredentialSource.Certificate:
                        break;
                    case CredentialSource.KeyVault:
                        KeyVaultCertificateName = value;
                        break;
                    case CredentialSource.Base64Encoded:
                        Base64EncodedValue = value;
                        break;
                    case CredentialSource.Path:
                        CertificateDiskPath = value;
                        break;
                    case CredentialSource.StoreWithThumbprint:
                        CertificateThumbprint = value;
                        break;
                    case CredentialSource.StoreWithDistinguishedName:
                        CertificateDistinguishedName = value;
                        break;
                    case CredentialSource.ClientSecret:
                        ClientSecret = value;
                        break;
                    case CredentialSource.SignedAssertionFromManagedIdentity:
                        ManagedIdentityClientId = value;
                        break;
                    case CredentialSource.SignedAssertionFilePath:
                        SignedAssertionFileDiskPath = value;
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
        public X509Certificate2? Certificate { get; set; }

        /// <summary>
        /// Cached value for the credential
        /// </summary>
        public virtual object? CachedValue { get; set; }

        /// <summary>
        /// Skip this credential. This is useful when, you specify a list of
        /// credentials, some of which don't apply in a particular deployment.
        /// </summary>
        public bool Skip { get; set; }

        /// <summary>
        /// Describes the type of credentials
        /// </summary>
        public CredentialType CredentialType
        {
            get
            {
                return SourceType switch
                {
                    CredentialSource.KeyVault or CredentialSource.Path or CredentialSource.StoreWithThumbprint or CredentialSource.StoreWithDistinguishedName or CredentialSource.Certificate or CredentialSource.Base64Encoded => CredentialType.Certificate,
                    CredentialSource.ClientSecret => CredentialType.Secret,
                    CredentialSource.SignedAssertionFromManagedIdentity or CredentialSource.SignedAssertionFilePath => CredentialType.SignedAssertion,
                    _ => default,
                };
            }
        }
    }
}
