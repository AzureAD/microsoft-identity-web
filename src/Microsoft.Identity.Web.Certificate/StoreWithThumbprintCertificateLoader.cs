// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Identity.Abstractions;

namespace Microsoft.Identity.Web 
{
    internal sealed class StoreWithThumbprintCertificateLoader : ICredentialSourceLoader
    {
        public CredentialSource CredentialSource => CredentialSource.StoreWithThumbprint;

        public Task LoadIfNeededAsync(CredentialDescription credentialDescription, bool throwExceptions = false)
        {
            credentialDescription.Certificate = LoadFromStoreWithThumbprint(
                            credentialDescription.CertificateThumbprint!,
                            credentialDescription.CertificateStorePath!);
            credentialDescription.CachedValue = credentialDescription.Certificate;
            return Task.CompletedTask;
        }

        private static X509Certificate2? LoadFromStoreWithThumbprint(
            string certificateThumbprint,
            string storeDescription = CertificateConstants.PersonalUserCertificateStorePath)
        {
            StoreLocation certificateStoreLocation = StoreLocation.CurrentUser;
            StoreName certificateStoreName = StoreName.My;
            CertificateLoaderHelper.ParseStoreLocationAndName(
                storeDescription,
                ref certificateStoreLocation,
                ref certificateStoreName);

            X509Certificate2? cert;
            using (X509Store x509Store = new X509Store(
                certificateStoreName,
                certificateStoreLocation))
            {
                cert = CertificateLoaderHelper.FindCertificateByCriterium(
                   x509Store,
                   X509FindType.FindByThumbprint,
                   certificateThumbprint);
            }

            return cert;
        }
    }
}
