﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Identity.Web
{
    internal class FromPathCertificateLoader : ICredentialLoader
    {
        public CredentialSource CredentialSource => CredentialSource.Path;

        public void LoadIfNeeded(CredentialDescription credentialDescription)
        {
            credentialDescription.Certificate = LoadFromPath(
                           credentialDescription.CertificateStorePath!,
                           credentialDescription.CertificatePassword!);
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