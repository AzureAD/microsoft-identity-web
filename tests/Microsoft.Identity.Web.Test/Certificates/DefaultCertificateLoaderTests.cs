// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Identity.Web.Test.Common;
using Xunit;

namespace Microsoft.Identity.Web.Test.Certificates
{
    public class DefaultCertificateLoaderTests
    {
        // https://portal.azure.com/#blade/Microsoft_AAD_RegisteredApps/ApplicationMenuBlade/Credentials/appId/86699d80-dd21-476a-bcd1-7c1a3d471f75/isMSAApp/
        // [InlineData(CertificateSource.KeyVault, TestConstants.KeyVaultContainer, TestConstants.KeyVaultReference)]
        // [InlineData(CertificateSource.Path, @"c:\temp\WebAppCallingWebApiCert.pfx", "")]
        // [InlineData(CertificateSource.StoreWithDistinguishedName, "CurrentUser/My", "CN=WebAppCallingWebApiCert")]
        // [InlineData(CertificateSource.StoreWithThumbprint, "CurrentUser/My", "962D129A859174EE8B5596985BD18EFEB6961684")]
        [InlineData(CertificateSource.Base64Encoded, null, TestConstants.CertificateX5c)]
        [Theory]
        public void TestDefaultCertificateLoader(CertificateSource certificateSource, string container, string referenceOrValue)
        {
            CertificateDescription certificateDescription;
            switch (certificateSource)
            {
                case CertificateSource.KeyVault:
                    certificateDescription = CertificateDescription.FromKeyVault(container, referenceOrValue);
                    break;
                case CertificateSource.Base64Encoded:
                    certificateDescription = CertificateDescription.FromBase64Encoded(referenceOrValue);
                    break;
                case CertificateSource.Path:
                    certificateDescription = CertificateDescription.FromPath(container, referenceOrValue);
                    break;
                case CertificateSource.StoreWithThumbprint:
                    certificateDescription = new CertificateDescription() { SourceType = CertificateSource.StoreWithThumbprint };
                    certificateDescription.CertificateThumbprint = referenceOrValue;
                    certificateDescription.CertificateStorePath = container;
                    break;
                case CertificateSource.StoreWithDistinguishedName:
                    certificateDescription = new CertificateDescription() { SourceType = CertificateSource.StoreWithDistinguishedName };
                    certificateDescription.CertificateDistinguishedName = referenceOrValue;
                    certificateDescription.CertificateStorePath = container;
                    break;
                default:
                    certificateDescription = null;
                    break;
            }

            ICertificateLoader loader = new DefaultCertificateLoader();
            loader.LoadIfNeeded(certificateDescription);

            Assert.NotNull(certificateDescription.Certificate);
        }

        [InlineData(CertificateSource.Base64Encoded, null, TestConstants.CertificateX5c)]
        [Theory]
        public void TestLoadFirstCertificate(
            CertificateSource certificateSource,
            string container,
            string referenceOrValue)
        {
            IEnumerable<CertificateDescription> certDescriptions = CreateCertificateDescriptions(
                certificateSource,
                container,
                referenceOrValue);

            X509Certificate2 certificate = DefaultCertificateLoader.LoadFirstCertificate(certDescriptions);

            Assert.NotNull(certificate);
            Assert.Equal("CN=ACS2ClientCertificate", certificate.Issuer);
        }

        [InlineData(CertificateSource.Base64Encoded, null, TestConstants.CertificateX5c)]
        [Theory]
        public void TestLoadAllCertificates(
           CertificateSource certificateSource,
           string container,
           string referenceOrValue)
        {
            List<CertificateDescription> certDescriptions = CreateCertificateDescriptions(
                certificateSource,
                container,
                referenceOrValue).ToList();

            certDescriptions.Add(new CertificateDescription
            {
                SourceType = certificateSource,
                Container = container,
                ReferenceOrValue = referenceOrValue,
            });

            IEnumerable<X509Certificate2> certificates = DefaultCertificateLoader.LoadAllCertificates(certDescriptions);

            Assert.NotNull(certificates);
            Assert.Equal(2, certificates.Count());
            Assert.Equal("CN=ACS2ClientCertificate", certificates.First().Issuer);
            Assert.Equal("CN=ACS2ClientCertificate", certificates.Last().Issuer);
        }

        private IEnumerable<CertificateDescription> CreateCertificateDescriptions(
            CertificateSource certificateSource,
            string container,
            string referenceOrValue)
        {
            List<CertificateDescription> certificateDescription = new List<CertificateDescription>();

            certificateDescription.Add(new CertificateDescription
            {
                SourceType = certificateSource,
                Container = container,
                ReferenceOrValue = referenceOrValue,
            });

            return certificateDescription;
        }
    }
}
