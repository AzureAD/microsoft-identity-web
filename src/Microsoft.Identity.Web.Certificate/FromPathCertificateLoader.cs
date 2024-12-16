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

        public Task LoadIfNeededAsync(CredentialDescription credentialDescription, CredentialSourceLoaderParameters? _)
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
            X509KeyStorageFlags x509KeyStorageFlags = CertificateLoaderHelper.DetermineX509KeyStorageFlag();

#pragma warning disable SYSLIB0057 // Type or member is obsolete
            return new X509Certificate2(
                certificateFileName,
                password,
                x509KeyStorageFlags);
#pragma warning restore SYSLIB0057 // Type or member is obsolete
        }
    }
}
