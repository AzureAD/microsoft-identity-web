﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Security.Cryptography.X509Certificates;
using Microsoft.Identity.Abstractions;

namespace Microsoft.Identity.Web
{
    internal sealed class StoreWithDistinguishedNameCertificateLoader : ICredentialLoader
    {
        public CredentialSource CredentialSource => CredentialSource.StoreWithDistinguishedName;

        public void LoadIfNeeded(CredentialDescription credentialDescription)
        {
            credentialDescription.Certificate = LoadFromStoreWithDistinguishedName(
                            credentialDescription.CertificateDistinguishedName!,
                            credentialDescription.CertificateStorePath!);
            credentialDescription.CachedValue = credentialDescription.Certificate;
        }

        private static X509Certificate2? LoadFromStoreWithDistinguishedName(
            string certificateSubjectDistinguishedName,
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
                    X509FindType.FindBySubjectDistinguishedName,
                    certificateSubjectDistinguishedName);
            }

            return cert;
        }
    }
}
