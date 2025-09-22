// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web.Test.Common;
using Xunit;

namespace Microsoft.Identity.Web.Test.Certificates
{
    public class DefaultCertificateLoaderTests
    {
        // https://portal.azure.com/#blade/Microsoft_AAD_RegisteredApps/ApplicationMenuBlade/Credentials/appId/9a192b78-6580-4f8a-aace-f36ffea4f7be/isMSAApp/
        // [InlineData(CertificateSource.KeyVault, TestConstants.KeyVaultContainer, TestConstants.KeyVaultReference)]
        // [InlineData(CertificateSource.Path, @"c:\temp\WebAppCallingWebApiCert.pfx", "")]
        // [InlineData(CertificateSource.StoreWithDistinguishedName, "CurrentUser/My", "CN=WebAppCallingWebApiCert")]
        // [InlineData(CertificateSource.StoreWithThumbprint, "CurrentUser/My", "962D129A859174EE8B5596985BD18EFEB6961684")]
#pragma warning disable xUnit1012 // Null should only be used for nullable parameters
        [InlineData(CertificateSource.Base64Encoded, null, TestConstants.CertificateX5c)]
#pragma warning restore xUnit1012 // Null should only be used for nullable parameters
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
                    certificateDescription = new CertificateDescription();
                    break;
            }

            ICertificateLoader loader = new DefaultCertificateLoader();
            loader.LoadIfNeeded(certificateDescription);

            Assert.NotNull(certificateDescription.Certificate);
        }

        [Fact]
        public void TestLoadFirstCertificate()
        {
            IEnumerable<CertificateDescription> certDescriptions = [CertificateDescription.FromBase64Encoded(TestConstants.CertificateX5c)];
            X509Certificate2? certificate = DefaultCertificateLoader.LoadFirstCertificate(certDescriptions);

            Assert.NotNull(certificate);
            Assert.Equal("CN=ACS2ClientCertificate", certificate.Issuer);
        }

        [Fact]
        public void TestLoadAllCertificates()
        {
            List<CertificateDescription> certDescriptions = [CertificateDescription.FromBase64Encoded(TestConstants.CertificateX5c)];

            certDescriptions.Add(CertificateDescription.FromBase64Encoded(TestConstants.CertificateX5c));
            certDescriptions.Add(CertificateDescription.FromCertificate(null!));

            IEnumerable<X509Certificate2?> certificates = DefaultCertificateLoader.LoadAllCertificates(certDescriptions);

            Assert.NotNull(certificates);
            Assert.Equal(2, certificates.Count());
            Assert.Equal(3, certDescriptions.Count);
            Assert.NotNull(certificates.First());
            Assert.Equal("CN=ACS2ClientCertificate", certificates.First()!.Issuer);
            Assert.NotNull(certificates.Last());
            Assert.Equal("CN=ACS2ClientCertificate", certificates.Last()!.Issuer);
            Assert.Null(certDescriptions.ElementAt(2).Certificate);
        }

        [InlineData(CertificateSource.Base64Encoded, TestConstants.CertificateX5cWithPrivateKey, TestConstants.CertificateX5cWithPrivateKeyPassword)]
        //[InlineData(CertificateSource.Path, "Certificates\\SelfSignedTestCert.pfx", TestConstants.CertificateX5cWithPrivateKeyPassword)]
        [Theory]
        public void TestLoadCertificateWithPrivateKey(
                    CertificateSource certificateSource,
                    string container,
                    string password)
        {
            CertificateDescription certificateDescription;

            if (certificateSource == CertificateSource.Base64Encoded)
            {
                certificateDescription = CertificateDescription.FromBase64Encoded(container, password);
            }
            else
            {
                certificateDescription = CertificateDescription.FromPath(container, password);
            }

            DefaultCertificateLoader defaultCertificateLoader = new DefaultCertificateLoader();
            defaultCertificateLoader.LoadIfNeeded(certificateDescription);

            Assert.NotNull(certificateDescription.Certificate);
            Assert.True(certificateDescription.Certificate.HasPrivateKey);
        }

        [Fact]
        public void TestDefaultCredentialsLoaderWithCustomLoaders()
        {
            // Arrange
            var customLoaders = new List<ICredentialSourceLoader>
            {
                new MockCredentialSourceLoader(CredentialSource.Base64Encoded, "custom-mock")
            };

            // Act
            var loader = new DefaultCredentialsLoader(null, customLoaders);

            // Assert
            Assert.NotNull(loader.CredentialSourceLoaders);
            Assert.True(loader.CredentialSourceLoaders.ContainsKey(CredentialSource.Base64Encoded));
            
            // Verify the custom loader overrode the built-in one
            var customLoader = loader.CredentialSourceLoaders[CredentialSource.Base64Encoded] as MockCredentialSourceLoader;
            Assert.NotNull(customLoader);
            Assert.Equal("custom-mock", customLoader.TestValue);
        }

        [Fact]
        public void TestDefaultCertificateLoaderWithCustomLoaders()
        {
            // Arrange
            var customLoaders = new List<ICredentialSourceLoader>
            {
                new MockCredentialSourceLoader(CredentialSource.Path, "certificate-mock")
            };

            // Act
            var loader = new DefaultCertificateLoader(null, customLoaders);

            // Assert
            Assert.NotNull(loader.CredentialSourceLoaders);
            Assert.True(loader.CredentialSourceLoaders.ContainsKey(CredentialSource.Path));
            
            // Verify the custom loader overrode the built-in one
            var customLoader = loader.CredentialSourceLoaders[CredentialSource.Path] as MockCredentialSourceLoader;
            Assert.NotNull(customLoader);
            Assert.Equal("certificate-mock", customLoader.TestValue);
        }

        [Fact]
        public async Task TestCustomLoaderIsUsed()
        {
            // Arrange
            var customLoaders = new List<ICredentialSourceLoader>
            {
                new MockCredentialSourceLoader(CredentialSource.StoreWithThumbprint, "used-custom-loader")
            };
            var loader = new DefaultCredentialsLoader(null, customLoaders);
            var credentialDescription = new CredentialDescription
            {
                SourceType = CredentialSource.StoreWithThumbprint
            };

            // Act
            await loader.LoadCredentialsIfNeededAsync(credentialDescription);

            // Assert
            Assert.Equal("used-custom-loader", credentialDescription.CachedValue);
        }

        [Fact]
        public void TestConstructorWithNullCustomLoaders()
        {
            // Arrange & Act
            var loader = new DefaultCredentialsLoader(null, null);

            // Assert - should still have built-in loaders
            Assert.NotNull(loader.CredentialSourceLoaders);
            Assert.True(loader.CredentialSourceLoaders.ContainsKey(CredentialSource.KeyVault));
            Assert.True(loader.CredentialSourceLoaders.ContainsKey(CredentialSource.Base64Encoded));
        }

        [Fact]
        public void TestBackwardCompatibilityExistingConstructors()
        {
            // Test that existing constructors still work
            var loader1 = new DefaultCredentialsLoader();
            var loader2 = new DefaultCredentialsLoader(null);
            var loader3 = new DefaultCertificateLoader();
            var loader4 = new DefaultCertificateLoader(null);

            // All should have built-in loaders
            Assert.NotNull(loader1.CredentialSourceLoaders);
            Assert.NotNull(loader2.CredentialSourceLoaders);
            Assert.NotNull(loader3.CredentialSourceLoaders);
            Assert.NotNull(loader4.CredentialSourceLoaders);

            Assert.True(loader1.CredentialSourceLoaders.Count >= 7); // Should have at least 7 built-in loaders
            Assert.True(loader2.CredentialSourceLoaders.Count >= 7);
            Assert.True(loader3.CredentialSourceLoaders.Count >= 7);
            Assert.True(loader4.CredentialSourceLoaders.Count >= 7);
        }

        [Fact]
        public void TestCustomLoaderWithNonConflictingCredentialSources()
        {
            // Arrange - Use a custom loader that doesn't override any built-in loader
            var customLoaders = new List<ICredentialSourceLoader>
            {
                new MockCredentialSourceLoader(CredentialSource.Certificate, "custom-certificate")
            };

            // Act
            var loader = new DefaultCredentialsLoader(null, customLoaders);

            // Assert - should have all built-in loaders plus the custom one
            Assert.NotNull(loader.CredentialSourceLoaders);
            Assert.True(loader.CredentialSourceLoaders.Count >= 8); // 7 built-in + 1 custom

            // Verify built-in loaders are still present
            Assert.True(loader.CredentialSourceLoaders.ContainsKey(CredentialSource.KeyVault));
            Assert.True(loader.CredentialSourceLoaders.ContainsKey(CredentialSource.Base64Encoded));
            Assert.True(loader.CredentialSourceLoaders.ContainsKey(CredentialSource.Path));

            // Verify custom loader is added
            Assert.True(loader.CredentialSourceLoaders.ContainsKey(CredentialSource.Certificate));
            var customLoader = loader.CredentialSourceLoaders[CredentialSource.Certificate] as MockCredentialSourceLoader;
            Assert.NotNull(customLoader);
            Assert.Equal("custom-certificate", customLoader.TestValue);
        }

        /// <summary>
        /// Mock credential source loader for testing
        /// </summary>
        internal class MockCredentialSourceLoader : ICredentialSourceLoader
        {
            public CredentialSource CredentialSource { get; }
            public string TestValue { get; }

            public MockCredentialSourceLoader(CredentialSource credentialSource, string testValue = "mock")
            {
                CredentialSource = credentialSource;
                TestValue = testValue;
            }

            public Task LoadIfNeededAsync(CredentialDescription credentialDescription, CredentialSourceLoaderParameters? parameters = null)
            {
                // Mock implementation - just mark that this loader was used
                credentialDescription.CachedValue = TestValue;
                return Task.CompletedTask;
            }
        }
    }
}
