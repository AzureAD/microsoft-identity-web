// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Security.Cryptography.X509Certificates;
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
            Assert.Equal(certificateName, certificateDescription.KeyVaultCertificateName);
            Assert.Equal(keyVaultUrl, certificateDescription.KeyVaultUrl);
        }

        [Theory]
        [InlineData(@"C:\Users\myname\Documents\TestJWE.pfx", "TestJWE")]
        public void TestFromPath(string certificatePath, string password)
        {
            CertificateDescription certificateDescription = CertificateDescription.FromPath(certificatePath, password);
            Assert.Equal(CertificateSource.Path, certificateDescription.SourceType);
            Assert.Equal(certificatePath, certificateDescription.Container);
            Assert.Equal(password, certificateDescription.ReferenceOrValue);
            Assert.Equal(certificatePath, certificateDescription.CertificateDiskPath);
            Assert.Equal(password, certificateDescription.CertificatePassword);
        }

        [Theory]
        [InlineData("440A5BE6C4BE2FF02A0ADBED1AAA43D6CF12E269")]
        public void TestFromBase64Encoded(string base64Encoded)
        {
            CertificateDescription certificateDescription = CertificateDescription.FromBase64Encoded(base64Encoded);
            Assert.Equal(CertificateSource.Base64Encoded, certificateDescription.SourceType);
            Assert.Equal(base64Encoded, certificateDescription.ReferenceOrValue);
            Assert.Equal(base64Encoded, certificateDescription.Base64EncodedValue);
        }

        [Theory]
        [InlineData("CN=TestCert", StoreLocation.LocalMachine, StoreName.Root)]
        public void TestFromCertificateDistinguishedName(string certificateDistinguishedName, StoreLocation storeLocation, StoreName storeName)
        {
            CertificateDescription certificateDescription =
                CertificateDescription.FromStoreWithDistinguishedName(certificateDistinguishedName, storeLocation, storeName);
            Assert.Equal(CertificateSource.StoreWithDistinguishedName, certificateDescription.SourceType);
            Assert.Equal($"{storeLocation}/{storeName}", certificateDescription.Container);
            Assert.Equal(certificateDistinguishedName, certificateDescription.ReferenceOrValue);
            Assert.Equal(certificateDistinguishedName, certificateDescription.CertificateDistinguishedName);
            Assert.Equal($"{storeLocation}/{storeName}", certificateDescription.CertificateStorePath);
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
            Assert.Equal($"{storeLocation}/{storeName}", certificateDescription.CertificateStorePath);
            Assert.Equal(certificateThumbprint, certificateDescription.CertificateThumbprint);
        }

        [Fact]
        public void TestFromCertificate()
        {
            using (X509Certificate2 certificate2 = new X509Certificate2())
            {
                CertificateDescription certificateDescription =
                    CertificateDescription.FromCertificate(certificate2);
            }
        }
    }
}
