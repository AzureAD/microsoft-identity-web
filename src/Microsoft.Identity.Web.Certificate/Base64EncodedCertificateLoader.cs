// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Security.Cryptography.X509Certificates;
using System;
using Microsoft.Identity.Abstractions;
using System.Threading.Tasks;

namespace Microsoft.Identity.Web
{
    internal sealed class Base64EncodedCertificateLoader : ICredentialSourceLoader
    {
        public CredentialSource CredentialSource => CredentialSource.Base64Encoded;

        public Task LoadIfNeededAsync(CredentialDescription credentialDescription, CredentialSourceLoaderParameters? credentialSourceLoaderParameters)
        {
            _ = credentialDescription ?? throw new ArgumentNullException(nameof(credentialDescription));

            if (credentialDescription.Certificate != null)
                return Task.CompletedTask;

            if (string.IsNullOrEmpty(credentialDescription.CertificatePassword))
            {
                credentialDescription.Certificate = LoadFromBase64Encoded(
                                credentialDescription.Base64EncodedValue!,
                                CertificateLoaderHelper.DetermineX509KeyStorageFlag(credentialDescription));
            }
            else
            {
                credentialDescription.Certificate = LoadFromBase64Encoded(
                                credentialDescription.Base64EncodedValue!,
                                credentialDescription.CertificatePassword!,
                                CertificateLoaderHelper.DetermineX509KeyStorageFlag(credentialDescription));
            }

            credentialDescription.CachedValue = credentialDescription.Certificate;
            return Task.CompletedTask;
        }

        internal static X509Certificate2 LoadFromBase64Encoded(string certificateBase64, X509KeyStorageFlags x509KeyStorageFlags)
        {
#pragma warning disable SYSLIB0057 // Type or member is obsolete
            return new X509Certificate2(
                Convert.FromBase64String(certificateBase64),
                (string?)null,
                x509KeyStorageFlags);
#pragma warning restore SYSLIB0057 // Type or member is obsolete
        }

        internal static X509Certificate2 LoadFromBase64Encoded(string certificateBase64, string password, X509KeyStorageFlags x509KeyStorageFlags)
        {
#pragma warning disable SYSLIB0057 // Type or member is obsolete
            return new X509Certificate2(
                Convert.FromBase64String(certificateBase64),
                password,
                x509KeyStorageFlags);
#pragma warning restore SYSLIB0057 // Type or member is obsolete
        }
    }
}
