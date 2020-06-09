// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Xunit;

namespace Microsoft.Identity.Web.Test.Certificates
{
    public class CertificateDescriptionTests
    {
        [Theory]
        [InlineData("https://vaultname.vault.azure.net/certificates/certificateid/3fb1c62f74b844b0a2d9f1a3d289648d", "certificateName")]
        public void TestFromKeyVault(string keyVaultUrl, string certificateName)
        {
            CertificateDescription certificateDescription = CertificateDescription.FromKeyVault(keyVaultUrl, certificateName);
            Assert.Equal(CertificateSource.KeyVault, certificateDescription.SourceType);
            Assert.Equal(keyVaultUrl, certificateDescription.Container);
            Assert.Equal(certificateName, certificateDescription.ReferenceOrValue);
        }

        [Theory]
        [InlineData(@"C:\Users\myname\Documents\TestJWE.pfx", "TestJWE")]
        public void TestFromPath(string certificatePath, string password)
        {
            CertificateDescription certificateDescription = CertificateDescription.FromPath(certificatePath, password);
            Assert.Equal(CertificateSource.Path, certificateDescription.SourceType);
            Assert.Equal(certificatePath, certificateDescription.Container);
            Assert.Equal(password, certificateDescription.ReferenceOrValue);
        }

        [Theory]
        [InlineData("440A5BE6C4BE2FF02A0ADBED1AAA43D6CF12E269")]
        public void TestFromBase64Encoded(string base64Encoded)
        {
            CertificateDescription certificateDescription = CertificateDescription.FromBase64Encoded(base64Encoded);
            Assert.Equal(CertificateSource.Base64Encoded, certificateDescription.SourceType);
            Assert.Equal(base64Encoded, certificateDescription.ReferenceOrValue);
        }

        [Theory]
        [InlineData("CN=TestCert", StoreLocation.LocalMachine, StoreName.Root)]
        public void TestFromStoreWithDistinguishedName(string certificateDistinguisedName, StoreLocation storeLocation, StoreName storeName)
        {
            CertificateDescription certificateDescription =
                CertificateDescription.FromStoreWithDistinguishedName(certificateDistinguisedName, storeLocation, storeName);
            Assert.Equal(CertificateSource.StoreWithDistinguishedName, certificateDescription.SourceType);
            Assert.Equal($"{storeLocation}/{storeName}", certificateDescription.Container);
            Assert.Equal(certificateDistinguisedName, certificateDescription.ReferenceOrValue);
        }

        [Theory]
        [InlineData("440A5BE6", StoreLocation.LocalMachine, StoreName.Root)]
        public void TestFromStoreWithThumprint(string certificateThumbprint, StoreLocation storeLocation, StoreName storeName)
        {
            CertificateDescription certificateDescription =
                CertificateDescription.FromStoreWithThumprint(certificateThumbprint, storeLocation, storeName);
            Assert.Equal(CertificateSource.StoreWithThumbprint, certificateDescription.SourceType);
            Assert.Equal($"{storeLocation}/{storeName}", certificateDescription.Container);
            Assert.Equal(certificateThumbprint, certificateDescription.ReferenceOrValue);
        }

        [Fact]
        public void TestFromCertificate()
        {
            using (X509Certificate2 certificate2 = new X509Certificate2())
            {
                CertificateDescription certificateDescription =
                    CertificateDescription.FromCertificate(certificate2);
                Assert.Equal(CertificateSource.Certificate, certificateDescription.SourceType);
                Assert.Equal(certificate2, certificateDescription.Certificate);
            }
        }
    }
}
