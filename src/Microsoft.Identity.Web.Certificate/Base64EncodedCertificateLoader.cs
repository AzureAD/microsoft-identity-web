// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Security.Cryptography.X509Certificates;
using System;
using Microsoft.Identity.Abstractions;

namespace Microsoft.Identity.Web
{
    internal class Base64EncodedCertificateLoader : ICredentialLoader
    {
        public CredentialSource CredentialSource => CredentialSource.Base64Encoded;

        public void LoadIfNeeded(CredentialDescription credentialDescription)
        {
            _ = credentialDescription ?? throw new ArgumentNullException(nameof(credentialDescription));

            if (credentialDescription.Certificate != null)
                return;

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

        }

        internal static X509Certificate2 LoadFromBase64Encoded(string certificateBase64, X509KeyStorageFlags x509KeyStorageFlags)
        {
            return new X509Certificate2(
                Convert.FromBase64String(certificateBase64),
                (string?)null,
                x509KeyStorageFlags);
        }

        internal static X509Certificate2 LoadFromBase64Encoded(string certificateBase64, string password, X509KeyStorageFlags x509KeyStorageFlags)
        {
            return new X509Certificate2(
                Convert.FromBase64String(certificateBase64),
                password,
                x509KeyStorageFlags);
        }
    }
}
