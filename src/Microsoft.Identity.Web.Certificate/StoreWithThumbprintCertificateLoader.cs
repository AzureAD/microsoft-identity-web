// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Identity.Web 
{
    internal class StoreWithThumbprintCertificateLoader : ICredentialLoader
    {
        public CredentialSource CredentialSource => CredentialSource.StoreWithThumbprint;

        public void LoadIfNeeded(CredentialDescription credentialDescription)
        {
            credentialDescription.Certificate = LoadFromStoreWithThumbprint(
                            credentialDescription.CertificateThumbprint!,
                            credentialDescription.CertificateStorePath!);
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
