// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web.Test.Common;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Microsoft.Identity.Web.Test.Certificates
{
    public class WithClientCredentialsTests
    {
        [Fact]
        public async Task FicFails_CertificateFallbackAsync()
        {
            // Arrange
            ILogger logger = Substitute.For<ILogger>();
            ICredentialsLoader credLoader = Substitute.For<ICredentialsLoader>();

            CredentialDescription[] credentialDescriptions = new[]
            {
                new CredentialDescription
                {
                    SourceType = CredentialSource.SignedAssertionFromManagedIdentity,
                },

                new CredentialDescription
                {
                    SourceType = CredentialSource.StoreWithDistinguishedName,
                    CertificateStorePath = "LocalMachine/My",
                    CertificateDistinguishedName = "CN=LabAuth.MSIDLab.com"
                }
            };

            // Mock the credential loader to fail to load FIC but load the cert
            credLoader.LoadCredentialsIfNeededAsync(Arg.Any<CredentialDescription>(), Arg.Any<CredentialSourceLoaderParameters>())
                .Returns(args =>
                {

                    var cd = (args[0] as CredentialDescription)!;

                    if (cd.CredentialType == CredentialType.SignedAssertion)
                    {
                        cd.Skip = true;  // mimic the credential loader
                        return Task.FromException(new Exception($"Failed to load credential with ID {cd.Id}"));
                    }
                    else
                    {
                        cd.Certificate = Base64EncodedCertificateLoader.LoadFromBase64Encoded(
                            TestConstants.CertificateX5c, X509KeyStorageFlags.DefaultKeySet);
                        return Task.CompletedTask;
                    }
                });

            // Act
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
            CredentialDescription cd = await ConfidentialClientApplicationBuilderExtension.LoadCredentialForMsalOrFailAsync(
                credentialDescriptions,
                logger,
                credLoader,
                null);
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.

            Assert.Equal(credentialDescriptions[1], cd);
        }

        #region Test around failure to load creds
        [Fact]
        public async Task FailsForFic_ReturnsMeaningfulMessageAsync()
        {

            var ficCredential = new CredentialDescription
            {
                SourceType = CredentialSource.SignedAssertionFromManagedIdentity,
                ManagedIdentityClientId = "9a192b78-6580-4f8a-aace-f36ffea4f7be"
            };

            await RunFailToLoadLogicAsync(new[] { ficCredential });
        }

        [Fact]
        public async Task FailsForCerts_ReturnsMeaningfulMessageAsync()
        {

            var certCredential1 = new CredentialDescription
            {
                SourceType = CredentialSource.StoreWithDistinguishedName,
                CertificateStorePath = "LocalMachine/My",
                CertificateDistinguishedName = "CN=LabAuth.MSIDLab.com"
            };
            var certCredential2 = new CredentialDescription
            {
                SourceType = CredentialSource.StoreWithThumbprint,
                CertificateThumbprint = "x5t",
                CertificateStorePath = "CurrentUser/My"
            };


            await RunFailToLoadLogicAsync(new[] { certCredential1, certCredential2 });
        }

        [Fact]
        public async Task FailsForFicAndCert_ReturnsMeaningfulMessageAsync()
        {
            var ficCredential = new CredentialDescription
            {
                SourceType = CredentialSource.SignedAssertionFromManagedIdentity,
                ManagedIdentityClientId = "9a192b78-6580-4f8a-aace-f36ffea4f7be"
            };

            var certCredential = new CredentialDescription
            {
                SourceType = CredentialSource.StoreWithDistinguishedName,
                CertificateStorePath = "LocalMachine/My",
                CertificateDistinguishedName = "CN=LabAuth.MSIDLab.com"
            };

            await RunFailToLoadLogicAsync(new[] { ficCredential, certCredential });
        }

        [Fact]
        public async Task FailsForCertAndFic_ReturnsMeaningfulMessageAsync()
        {
            var certCredential = new CredentialDescription
            {
                SourceType = CredentialSource.StoreWithDistinguishedName,
                CertificateStorePath = "LocalMachine/My",
                CertificateDistinguishedName = "CN=LabAuth.MSIDLab.com"
            };

            var ficCredential = new CredentialDescription
            {
                SourceType = CredentialSource.SignedAssertionFromManagedIdentity,
                ManagedIdentityClientId = "9a192b78-6580-4f8a-aace-f36ffea4f7be"
            };

            await RunFailToLoadLogicAsync(new[] { ficCredential, certCredential });
        }

        [Fact]
        public async Task FailsForPodAndCert_ReturnsMeaningfulMessageAsync()
        {
            var certCredential = new CredentialDescription
            {
                SourceType = CredentialSource.StoreWithDistinguishedName,
                CertificateStorePath = "LocalMachine/My",
                CertificateDistinguishedName = "CN=LabAuth.MSIDLab.com"
            };

            var ficCredential = new CredentialDescription
            {
                SourceType = CredentialSource.SignedAssertionFilePath,
            };

            await RunFailToLoadLogicAsync(new[] { ficCredential, certCredential });
        }

        private static async Task RunFailToLoadLogicAsync(IEnumerable<CredentialDescription> credentialDescriptions)
        {
            // Arrange
            var logger = Substitute.For<ILogger>();
            ICredentialsLoader credLoader = Substitute.For<ICredentialsLoader>();

            // Mock the credential loader to fail to load any certificate
            credLoader.LoadCredentialsIfNeededAsync(Arg.Any<CredentialDescription>(), Arg.Any<CredentialSourceLoaderParameters>())
                .Returns(args =>
                {

                    var cd = (args[0] as CredentialDescription)!;
                    cd.Skip = true;  // mimic the credential loader

                    return Task.FromException(new Exception($"Failed to load credential with ID {cd.Id}"));
                });


            // Act
            var ex = await Assert.ThrowsAsync<ArgumentException>(() => ConfidentialClientApplicationBuilderExtension.LoadCredentialForMsalOrFailAsync(
                credentialDescriptions,
                logger,
                credLoader,
                null));

            // Assert
            foreach (var cd in credentialDescriptions)
            {
                Assert.True(ex.Message.Contains($"Failed to load credential with ID {cd.Id}", StringComparison.OrdinalIgnoreCase));
            }
        }
        #endregion

        #region WithBindingCertificateAsync tests

        [Fact]
        public async Task WithBindingCertificateAsync_ValidCertificate_ReturnsOriginalBuilderWithCertificate()
        {
            // Arrange
            var logger = Substitute.For<ILogger>();
            var credLoader = Substitute.For<ICredentialsLoader>();
            var builder = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                .WithAuthority(TestConstants.AuthorityCommonTenant);

            var credentialDescription = new CredentialDescription
            {
                SourceType = CredentialSource.StoreWithThumbprint,
                CertificateThumbprint = "test-thumbprint",
                CertificateStorePath = "CurrentUser/My"
            };

            var testCertificate = Base64EncodedCertificateLoader.LoadFromBase64Encoded(
                TestConstants.CertificateX5cWithPrivateKey,
                TestConstants.CertificateX5cWithPrivateKeyPassword,
                X509KeyStorageFlags.DefaultKeySet);

            // Mock the credential loader to successfully load the certificate
            credLoader.LoadCredentialsIfNeededAsync(Arg.Any<CredentialDescription>(), Arg.Any<CredentialSourceLoaderParameters>())
                .Returns(args =>
                {
                    var cd = (args[0] as CredentialDescription)!;
                    cd.Certificate = testCertificate;
                    return Task.CompletedTask;
                });

            // Act
            var result = await builder.WithBindingCertificateAsync(
                new[] { credentialDescription },
                logger,
                credLoader,
                null);

            // Assert
            Assert.NotNull(result);
            Assert.Same(builder, result); // Should return the same builder instance
            await credLoader.Received(1).LoadCredentialsIfNeededAsync(credentialDescription, null);
        }

        [Fact]
        public async Task WithBindingCertificateAsync_NoValidCredentials_ReturnsOriginalBuilderAfterException()
        {
            // Arrange
            var logger = Substitute.For<ILogger>();
            var credLoader = Substitute.For<ICredentialsLoader>();
            var builder = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                .WithAuthority(TestConstants.AuthorityCommonTenant);

            var credentialDescription = new CredentialDescription
            {
                SourceType = CredentialSource.StoreWithThumbprint,
                CertificateThumbprint = "test-thumbprint",
                CertificateStorePath = "CurrentUser/My"
            };

            // Mock the credential loader to fail loading (Skip = true causes LoadCredentialForMsalOrFailAsync to throw)
            credLoader.LoadCredentialsIfNeededAsync(Arg.Any<CredentialDescription>(), Arg.Any<CredentialSourceLoaderParameters>())
                .Returns(args =>
                {
                    var cd = (args[0] as CredentialDescription)!;
                    cd.Skip = true;
                    return Task.CompletedTask;
                });

            // Act & Assert
            // This should throw because LoadCredentialForMsalOrFailAsync throws when no credentials can be loaded
            await Assert.ThrowsAsync<ArgumentException>(
                () => builder.WithBindingCertificateAsync(
                    new[] { credentialDescription },
                    logger,
                    credLoader,
                    null));
        }

        [Fact]
        public async Task WithBindingCertificateAsync_CredentialWithoutCertificate_ReturnsOriginalBuilder()
        {
            // Arrange
            var logger = Substitute.For<ILogger>();
            var credLoader = Substitute.For<ICredentialsLoader>();
            var builder = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                .WithAuthority(TestConstants.AuthorityCommonTenant);

            var credentialDescription = new CredentialDescription
            {
                SourceType = CredentialSource.ClientSecret,
                ClientSecret = "test-secret"
            };

            // Mock the credential loader to load a credential without a certificate
            credLoader.LoadCredentialsIfNeededAsync(Arg.Any<CredentialDescription>(), Arg.Any<CredentialSourceLoaderParameters>())
                .Returns(args =>
                {
                    var cd = (args[0] as CredentialDescription)!;
                    // Certificate is null by default
                    return Task.CompletedTask;
                });

            // Act
            var result = await builder.WithBindingCertificateAsync(
                new[] { credentialDescription },
                logger,
                credLoader,
                null);

            // Assert
            Assert.NotNull(result);
            Assert.Same(builder, result); // Should return the same builder instance
        }

        [Fact]
        public async Task WithBindingCertificateAsync_CredentialLoadingFails_PropagatesException()
        {
            // Arrange
            var logger = Substitute.For<ILogger>();
            var credLoader = Substitute.For<ICredentialsLoader>();
            var builder = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                .WithAuthority(TestConstants.AuthorityCommonTenant);

            var credentialDescription = new CredentialDescription
            {
                SourceType = CredentialSource.StoreWithThumbprint,
                CertificateThumbprint = "invalid-thumbprint",
                CertificateStorePath = "CurrentUser/My"
            };

            var expectedException = new Exception("Certificate not found");

            // Mock the credential loader to throw an exception
            credLoader.LoadCredentialsIfNeededAsync(Arg.Any<CredentialDescription>(), Arg.Any<CredentialSourceLoaderParameters>())
                .ThrowsAsync(expectedException);

            // Act & Assert
            var actualException = await Assert.ThrowsAsync<ArgumentException>(
                () => builder.WithBindingCertificateAsync(
                    new[] { credentialDescription },
                    logger,
                    credLoader,
                    null));

            // Verify the exception is propagated from LoadCredentialForMsalOrFailAsync
            Assert.Contains("Certificate not found", actualException.Message, StringComparison.Ordinal);
        }

        [Fact]
        public async Task WithBindingCertificateAsync_EmptyCredentialsList_ReturnsOriginalBuilder()
        {
            // Arrange
            var logger = Substitute.For<ILogger>();
            var credLoader = Substitute.For<ICredentialsLoader>();
            var builder = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                .WithAuthority(TestConstants.AuthorityCommonTenant);

            // Act
            var result = await builder.WithBindingCertificateAsync(
                new CredentialDescription[0],
                logger,
                credLoader,
                null);

            // Assert
            Assert.NotNull(result);
            Assert.Same(builder, result); // Should return the same builder instance

            // Verify that no credentials were attempted to be loaded (empty list bypasses loader)
            await credLoader.DidNotReceive().LoadCredentialsIfNeededAsync(Arg.Any<CredentialDescription>(), Arg.Any<CredentialSourceLoaderParameters>());
        }

        [Fact]
        public async Task WithBindingCertificateAsync_WithCredentialSourceLoaderParameters_PassesParametersCorrectly()
        {
            // Arrange
            var logger = Substitute.For<ILogger>();
            var credLoader = Substitute.For<ICredentialsLoader>();
            var builder = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                .WithAuthority(TestConstants.AuthorityCommonTenant);

            var credentialDescription = new CredentialDescription
            {
                SourceType = CredentialSource.StoreWithThumbprint,
                CertificateThumbprint = "test-thumbprint",
                CertificateStorePath = "CurrentUser/My"
            };

            var testCertificate = Base64EncodedCertificateLoader.LoadFromBase64Encoded(
                TestConstants.CertificateX5cWithPrivateKey,
                TestConstants.CertificateX5cWithPrivateKeyPassword,
                X509KeyStorageFlags.DefaultKeySet);

            var credentialSourceLoaderParameters = new CredentialSourceLoaderParameters("test-client-id", "test-tenant-id");

            // Mock the credential loader to successfully load the certificate
            credLoader.LoadCredentialsIfNeededAsync(Arg.Any<CredentialDescription>(), Arg.Any<CredentialSourceLoaderParameters>())
                .Returns(args =>
                {
                    var cd = (args[0] as CredentialDescription)!;
                    cd.Certificate = testCertificate;
                    return Task.CompletedTask;
                });

            // Act
            var result = await builder.WithBindingCertificateAsync(
                new[] { credentialDescription },
                logger,
                credLoader,
                credentialSourceLoaderParameters);

            // Assert
            Assert.NotNull(result);
            await credLoader.Received(1).LoadCredentialsIfNeededAsync(credentialDescription, credentialSourceLoaderParameters);
        }

        #endregion

    }
}
