// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Security.Cryptography.X509Certificates;
using Microsoft.Identity.Abstractions;

namespace Microsoft.Identity.Web
{
    internal sealed class FromPathCertificateLoader : ICredentialLoader
    {
        public CredentialSource CredentialSource => CredentialSource.Path;

        public void LoadIfNeeded(CredentialDescription credentialDescription)
        {
            credentialDescription.Certificate = LoadFromPath(
                           credentialDescription.CertificateDiskPath!,
                           credentialDescription.CertificatePassword!);
            credentialDescription.CachedValue = credentialDescription.Certificate;
        }

        private static X509Certificate2 LoadFromPath(
         string certificateFileName,
         string? password = null)
        {
#if NET462 || NETSTANDARD2_0
            return new X509Certificate2(
                certificateFileName,
                password,
                X509KeyStorageFlags.MachineKeySet);
#else
            return new X509Certificate2(
                certificateFileName,
                password,
                X509KeyStorageFlags.EphemeralKeySet);
#endif
        }
    }
}
