// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Identity.Abstractions;

namespace Microsoft.Identity.Web
{
    internal sealed class FromPathCertificateLoader : ICredentialSourceLoader
    {
        public CredentialSource CredentialSource => CredentialSource.Path;

        public Task LoadIfNeededAsync(CredentialDescription credentialDescription, bool throwExceptions = false)
        {
            credentialDescription.Certificate = LoadFromPath(
                           credentialDescription.CertificateDiskPath!,
                           credentialDescription.CertificatePassword!);
            credentialDescription.CachedValue = credentialDescription.Certificate;
            return Task.CompletedTask;
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
