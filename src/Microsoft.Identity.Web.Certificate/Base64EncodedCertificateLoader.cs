// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Security.Cryptography.X509Certificates;
using System;
using Microsoft.Identity.Abstractions;

namespace Microsoft.Identity.Web
{
    internal sealed class Base64EncodedCertificateLoader : ICredentialLoader
    {
        public CredentialSource CredentialSource => CredentialSource.Base64Encoded;

        public void LoadIfNeeded(CredentialDescription credentialDescription)
        {
            credentialDescription.Certificate = LoadFromBase64Encoded(
                            credentialDescription.Base64EncodedValue!,
                            CertificateLoaderHelper.DetermineX509KeyStorageFlag(credentialDescription));
            credentialDescription.CachedValue = credentialDescription.Certificate;
        }

        internal static X509Certificate2 LoadFromBase64Encoded(string certificateBase64, X509KeyStorageFlags x509KeyStorageFlags)
        {
            byte[] decoded = Convert.FromBase64String(certificateBase64);
            return new X509Certificate2(
                decoded,
                (string?)null,
                x509KeyStorageFlags);
        }
    }
}
