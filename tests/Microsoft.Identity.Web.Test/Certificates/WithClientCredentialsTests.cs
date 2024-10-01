// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Abstractions;
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
                    SourceType = CredentialSource.KeyVault,
                    KeyVaultUrl = "https://bogus.net",
                    KeyVaultCertificateName = "Self-Signed-5-5-22"
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
                SourceType = CredentialSource.KeyVault,
                KeyVaultUrl = "https://bogus.net",
                KeyVaultCertificateName = "Self-Signed-5-5-22"
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
                SourceType = CredentialSource.KeyVault,
                KeyVaultUrl = "https://bogus.net",
                KeyVaultCertificateName = "Self-Signed-5-5-22"
            };

            await RunFailToLoadLogicAsync(new[] { ficCredential, certCredential });
        }

        [Fact]
        public async Task FailsForCertAndFic_ReturnsMeaningfulMessageAsync()
        {
            var certCredential = new CredentialDescription
            {
                SourceType = CredentialSource.KeyVault,
                KeyVaultUrl = "https://bogus.net",
                KeyVaultCertificateName = "Self-Signed-5-5-22"
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
                SourceType = CredentialSource.KeyVault,
                KeyVaultUrl = "https://bogus.net",
                KeyVaultCertificateName = "Self-Signed-5-5-22"
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

    }
}
