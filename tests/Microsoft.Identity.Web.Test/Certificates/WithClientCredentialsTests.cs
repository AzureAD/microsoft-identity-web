// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
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
            var logger = Substitute.For<ILogger<CredentialsProvider>>();
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

            CredentialsProvider provider = new CredentialsProvider(
                logger,
                credLoader,
                [],
                null);

            // Act
            CredentialDescription? cd = await provider.GetCredentialAsync(
                new MergedOptions()
                {
                    ClientCredentials = credentialDescriptions,
                },
                null);

            Assert.Equal(credentialDescriptions[1], cd);
        }

        #region Test around failure to load creds
        [Fact]
        public async Task FailsForFic_ReturnsMeaningfulMessageAsync()
        {

            var ficCredential = new CredentialDescription
            {
                SourceType = CredentialSource.SignedAssertionFromManagedIdentity,
                ManagedIdentityClientId = "a599ce88-0a5f-4a6e-beca-e67d3fc427f4"
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
                ManagedIdentityClientId = "a599ce88-0a5f-4a6e-beca-e67d3fc427f4"
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
                ManagedIdentityClientId = "a599ce88-0a5f-4a6e-beca-e67d3fc427f4"
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
            var logger = Substitute.For<ILogger<CredentialsProvider>>();
            ICredentialsLoader credLoader = Substitute.For<ICredentialsLoader>();

            // Mock the credential loader to fail to load any certificate
            credLoader.LoadCredentialsIfNeededAsync(Arg.Any<CredentialDescription>(), Arg.Any<CredentialSourceLoaderParameters>())
                .Returns(args =>
                {

                    var cd = (args[0] as CredentialDescription)!;
                    cd.Skip = true;  // mimic the credential loader

                    return Task.FromException(new Exception($"Failed to load credential with ID {cd.Id}"));
                });

            CredentialsProvider provider = new CredentialsProvider(
                logger,
                credLoader,
                [],
                null);

            // Act
            var ex = await Assert.ThrowsAsync<ArgumentException>(() => provider.GetCredentialAsync(
                new MergedOptions()
                {
                    ClientCredentials = credentialDescriptions,
                },
                null));

            // Assert
            foreach (var cd in credentialDescriptions)
            {
                Assert.True(ex.Message.Contains($"Failed to load credential with ID {cd.Id}", StringComparison.OrdinalIgnoreCase));
            }
        }
        #endregion

        #region Client credentials for token binding tests

        [Fact]
        public async Task WithBindingCertificateAsync_ValidCertificate_ReturnsOriginalBuilderWithCertificate()
        {
            // Arrange
            var logger = Substitute.For<ILogger<CredentialsProvider>>();
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
            var result = await builder.WithClientCredentialsAsync(
                new MergedOptions()
                {
                    ClientCredentials = new[] { credentialDescription },
                },
                new CredentialsProvider(logger, credLoader, [], null),
                credentialSourceLoaderParameters: null,
                isTokenBinding: true);

            // Assert
            Assert.NotNull(result);
            Assert.Same(builder, result); // Should return the same builder instance
            await credLoader.Received(1).LoadCredentialsIfNeededAsync(credentialDescription, null);
        }

        [Fact]
        public async Task WithBindingCertificateAsync_NoValidCredentials_ThrowsException()
        {
            // Arrange
            var logger = Substitute.For<ILogger<CredentialsProvider>>();
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

            CredentialsProvider provider = new CredentialsProvider(
                logger,
                credLoader,
                [],
                null);

            // Act & Assert
            // This should throw because LoadCredentialForMsalOrFailAsync throws when no credentials can be loaded
            await Assert.ThrowsAsync<ArgumentException>(
                () => builder.WithClientCredentialsAsync(
                    new MergedOptions()
                    {
                        ClientCredentials = [ credentialDescription ],
                    },
                    provider,
                    credentialSourceLoaderParameters: null,
                    isTokenBinding: true));
        }

        [Fact]
        public async Task WithBindingCertificateAsync_CredentialWithoutCertificate_ThrowsException()
        {
            // Arrange
            var logger = Substitute.For<ILogger<CredentialsProvider>>();
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

            CredentialsProvider provider = new CredentialsProvider(
                logger,
                credLoader,
                [],
                null);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => builder.WithClientCredentialsAsync(
                    new MergedOptions()
                    {
                        ClientCredentials = [credentialDescription],
                    },
                    provider,
                    credentialSourceLoaderParameters: null,
                    isTokenBinding: true));
        }

        [Fact]
        public async Task WithBindingCertificateAsync_CredentialLoadingFails_PropagatesException()
        {
            // Arrange
            var logger = Substitute.For<ILogger<CredentialsProvider>>();
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

            CredentialsProvider provider = new CredentialsProvider(
                logger,
                credLoader,
                [],
                null);

            // Act & Assert
            var actualException = await Assert.ThrowsAsync<ArgumentException>(
                () => builder.WithClientCredentialsAsync(
                    new MergedOptions()
                    {
                        ClientCredentials = [credentialDescription],
                    },
                    provider,
                    credentialSourceLoaderParameters: null,
                    isTokenBinding: true));

            // Verify the exception is propagated from LoadCredentialForMsalOrFailAsync
            Assert.Contains("Certificate not found", actualException.Message, StringComparison.Ordinal);
        }

        [Fact]
        public async Task WithBindingCertificateAsync_EmptyCredentialsList_ThrowsException()
        {
            // Arrange
            var logger = Substitute.For<ILogger<CredentialsProvider>>();
            var credLoader = Substitute.For<ICredentialsLoader>();
            var builder = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                .WithAuthority(TestConstants.AuthorityCommonTenant);

            CredentialsProvider provider = new CredentialsProvider(
                logger,
                credLoader,
                [],
                null);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => builder.WithClientCredentialsAsync(
                    new MergedOptions(),
                    provider,
                    credentialSourceLoaderParameters: null,
                    isTokenBinding: true));
        }

        [Fact]
        public async Task WithBindingCertificateAsync_WithCredentialSourceLoaderParameters_PassesParametersCorrectly()
        {
            // Arrange
            var logger = Substitute.For<ILogger<CredentialsProvider>>();
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

            CredentialsProvider provider = new CredentialsProvider(
                logger,
                credLoader,
                [],
                null);

            // Act
            var result = await builder.WithClientCredentialsAsync(
                new MergedOptions()
                {
                    ClientCredentials = [credentialDescription],
                },
                provider,
                credentialSourceLoaderParameters,
                isTokenBinding: true);

            // Assert
            Assert.NotNull(result);
            await credLoader.Received(1).LoadCredentialsIfNeededAsync(credentialDescription, credentialSourceLoaderParameters);
        }

        [Fact]
        public async Task WithBindingCertificateAsync_FicWithManagedIdentityAssertion_ReturnsBuilder()
        {
            // Arrange — FIC backed by a Managed Identity-signed assertion (the new mTLS PoP path).
            // The MI assertion will mint a binding certificate at request time; we only need to verify
            // that WithClientCredentialsAsync wires the bound delegate instead of throwing.
            var logger = Substitute.For<ILogger<CredentialsProvider>>();
            var credLoader = Substitute.For<ICredentialsLoader>();
            var builder = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                .WithAuthority(TestConstants.AuthorityCommonTenant);

            var credentialDescription = new CredentialDescription
            {
                SourceType = CredentialSource.SignedAssertionFromManagedIdentity,
                ManagedIdentityClientId = "a599ce88-0a5f-4a6e-beca-e67d3fc427f4"
            };

            // Mimic CredentialsLoader behaviour: hydrate CachedValue with a ManagedIdentityClientAssertion.
            credLoader.LoadCredentialsIfNeededAsync(Arg.Any<CredentialDescription>(), Arg.Any<CredentialSourceLoaderParameters>())
                .Returns(args =>
                {
                    var cd = (args[0] as CredentialDescription)!;
                    cd.CachedValue = new ManagedIdentityClientAssertion(cd.ManagedIdentityClientId);
                    return Task.CompletedTask;
                });

            CredentialsProvider provider = new CredentialsProvider(
                logger,
                credLoader,
                [],
                null);

            // Act
            var result = await builder.WithClientCredentialsAsync(
                new MergedOptions()
                {
                    ClientCredentials = [credentialDescription],
                },
                provider,
                credentialSourceLoaderParameters: null,
                isTokenBinding: true);

            // Assert — FIC + MI path should now succeed (bound assertion delegate registered, no throw).
            Assert.NotNull(result);
            Assert.Same(builder, result);
        }

        [Fact]
        public async Task WithBindingCertificateAsync_FicWithFileBasedAssertion_StillThrows()
        {
            // Arrange — non-MI signed assertion (file/K8s-style) must remain unsupported for mTLS PoP
            // because it cannot produce a binding certificate. Regression guard for IDW10115.
            var logger = Substitute.For<ILogger<CredentialsProvider>>();
            var credLoader = Substitute.For<ICredentialsLoader>();
            var builder = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                .WithAuthority(TestConstants.AuthorityCommonTenant);

            var credentialDescription = new CredentialDescription
            {
                SourceType = CredentialSource.SignedAssertionFilePath,
                SignedAssertionFileDiskPath = "/var/run/secrets/azure/tokens/azure-identity-token"
            };

            credLoader.LoadCredentialsIfNeededAsync(Arg.Any<CredentialDescription>(), Arg.Any<CredentialSourceLoaderParameters>())
                .Returns(args =>
                {
                    var cd = (args[0] as CredentialDescription)!;
                    cd.CachedValue = new AzureIdentityForKubernetesClientAssertion(cd.SignedAssertionFileDiskPath);
                    return Task.CompletedTask;
                });

            CredentialsProvider provider = new CredentialsProvider(
                logger,
                credLoader,
                [],
                null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => builder.WithClientCredentialsAsync(
                    new MergedOptions()
                    {
                        ClientCredentials = [credentialDescription],
                    },
                    provider,
                    credentialSourceLoaderParameters: null,
                    isTokenBinding: true));

            Assert.Contains("IDW10115", ex.Message, StringComparison.Ordinal);
        }

        #endregion

        #region UseBoundCredential dispatch tests

        [Fact]
        public async Task WithClientCredentialsAsync_CertificateWithUseBoundCredentialTrue_ReturnsBuilderAsync()
        {
            // Arrange
            var logger = Substitute.For<ILogger<CredentialsProvider>>();
            var credLoader = Substitute.For<ICredentialsLoader>();
            var builder = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                .WithAuthority(TestConstants.AuthorityCommonTenant);

            var credentialDescription = new CredentialDescription
            {
                SourceType = CredentialSource.StoreWithThumbprint,
                CertificateThumbprint = "test-thumbprint",
                CertificateStorePath = "CurrentUser/My",
                UseBoundCredential = true,
            };

            var testCertificate = Base64EncodedCertificateLoader.LoadFromBase64Encoded(
                TestConstants.CertificateX5cWithPrivateKey,
                TestConstants.CertificateX5cWithPrivateKeyPassword,
                X509KeyStorageFlags.DefaultKeySet);

            credLoader.LoadCredentialsIfNeededAsync(Arg.Any<CredentialDescription>(), Arg.Any<CredentialSourceLoaderParameters>())
                .Returns(args =>
                {
                    var cd = (args[0] as CredentialDescription)!;
                    cd.Certificate = testCertificate;
                    return Task.CompletedTask;
                });

            // Act — the bound-credential path should call
            // WithCertificate(cert, new CertificateOptions { SendCertificateOverMtls = true });
            // we verify the dispatch does not throw and a builder comes back.
            var result = await builder.WithClientCredentialsAsync(
                new MergedOptions()
                {
                    ClientCredentials = new[] { credentialDescription },
                },
                new CredentialsProvider(logger, credLoader, [], null),
                credentialSourceLoaderParameters: null,
                isTokenBinding: false);

            // Assert
            Assert.NotNull(result);
            Assert.Same(builder, result);
            await credLoader.Received(1).LoadCredentialsIfNeededAsync(credentialDescription, null);
        }

        [Fact]
        public async Task WithClientCredentialsAsync_CertificateWithUseBoundCredentialFalse_ReturnsBuilderAsync()
        {
            // Arrange — same setup as the bound-credential test, but with
            // UseBoundCredential explicitly false. Guards against regression
            // in the legacy (non-mTLS) cert dispatch.
            var logger = Substitute.For<ILogger<CredentialsProvider>>();
            var credLoader = Substitute.For<ICredentialsLoader>();
            var builder = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                .WithAuthority(TestConstants.AuthorityCommonTenant);

            var credentialDescription = new CredentialDescription
            {
                SourceType = CredentialSource.StoreWithThumbprint,
                CertificateThumbprint = "test-thumbprint",
                CertificateStorePath = "CurrentUser/My",
                UseBoundCredential = false,
            };

            var testCertificate = Base64EncodedCertificateLoader.LoadFromBase64Encoded(
                TestConstants.CertificateX5cWithPrivateKey,
                TestConstants.CertificateX5cWithPrivateKeyPassword,
                X509KeyStorageFlags.DefaultKeySet);

            credLoader.LoadCredentialsIfNeededAsync(Arg.Any<CredentialDescription>(), Arg.Any<CredentialSourceLoaderParameters>())
                .Returns(args =>
                {
                    var cd = (args[0] as CredentialDescription)!;
                    cd.Certificate = testCertificate;
                    return Task.CompletedTask;
                });

            // Act
            var result = await builder.WithClientCredentialsAsync(
                new MergedOptions()
                {
                    ClientCredentials = new[] { credentialDescription },
                },
                new CredentialsProvider(logger, credLoader, [], null),
                credentialSourceLoaderParameters: null,
                isTokenBinding: false);

            // Assert
            Assert.NotNull(result);
            Assert.Same(builder, result);
        }

        #endregion

        #region Bound signed-assertion (UseBoundCredential / isTokenBinding) wiring tests

        /// <summary>
        /// Minimal binding-capable provider used to verify that the credential wiring is generic
        /// (not OIDC-specific). It can be configured to advertise or not advertise token binding,
        /// and to return a specific (or null) <see cref="ClientSignedAssertion"/>.
        /// </summary>
        private sealed class TestBindingProvider : ClientAssertionProviderBase
        {
            private readonly ClientSignedAssertion? _boundResult;
            private readonly bool _supportsTokenBinding;

            public TestBindingProvider(ClientSignedAssertion? boundResult, bool supportsTokenBinding = true)
            {
                _boundResult = boundResult;
                _supportsTokenBinding = supportsTokenBinding;
            }

            public override bool SupportsTokenBinding => _supportsTokenBinding;

            protected override Task<ClientAssertion> GetClientAssertionAsync(AssertionRequestOptions? assertionRequestOptions)
                => Task.FromResult(new ClientAssertion("string-assertion", DateTimeOffset.UtcNow.AddHours(1)));

            public override Task<ClientSignedAssertion?> GetSignedAssertionWithBindingAsync(
                AssertionRequestOptions? assertionRequestOptions,
                CancellationToken cancellationToken = default)
                => Task.FromResult(_boundResult);
        }

        private static CredentialsProvider ProviderThatCaches(object cachedValue)
        {
            var logger = Substitute.For<ILogger<CredentialsProvider>>();
            var credLoader = Substitute.For<ICredentialsLoader>();
            credLoader.LoadCredentialsIfNeededAsync(Arg.Any<CredentialDescription>(), Arg.Any<CredentialSourceLoaderParameters>())
                .Returns(args =>
                {
                    var cd = (args[0] as CredentialDescription)!;
                    cd.CachedValue = cachedValue;
                    return Task.CompletedTask;
                });
            return new CredentialsProvider(logger, credLoader, [], null);
        }

        private static X509Certificate2 LoadTestCertificate() =>
            Base64EncodedCertificateLoader.LoadFromBase64Encoded(
                TestConstants.CertificateX5cWithPrivateKey,
                TestConstants.CertificateX5cWithPrivateKeyPassword,
                X509KeyStorageFlags.DefaultKeySet);

        [Fact]
        public async Task WithClientCredentialsAsync_TokenBinding_BindingCapableProvider_WiresBoundCallbackAsync()
        {
            var builder = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                .WithAuthority(TestConstants.AuthorityCommonTenant);

            var provider = new TestBindingProvider(new ClientSignedAssertion
            {
                Assertion = "a",
                TokenBindingCertificate = LoadTestCertificate(),
            });

            var credentialDescription = new CredentialDescription
            {
                SourceType = CredentialSource.CustomSignedAssertion,
                CustomSignedAssertionProviderName = "OidcIdpSignedAssertion",
            };

            var result = await builder.WithClientCredentialsAsync(
                new MergedOptions { ClientCredentials = new[] { credentialDescription } },
                ProviderThatCaches(provider),
                credentialSourceLoaderParameters: null,
                isTokenBinding: true);

            Assert.Same(builder, result);
        }

        [Fact]
        public async Task WithClientCredentialsAsync_UseBoundCredential_BindingCapableProvider_WiresBoundCallbackAsync()
        {
            var builder = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                .WithAuthority(TestConstants.AuthorityCommonTenant);

            var provider = new TestBindingProvider(new ClientSignedAssertion
            {
                Assertion = "a",
                TokenBindingCertificate = LoadTestCertificate(),
            });

            var credentialDescription = new CredentialDescription
            {
                SourceType = CredentialSource.CustomSignedAssertion,
                CustomSignedAssertionProviderName = "OidcIdpSignedAssertion",
                UseBoundCredential = true,
            };

            // isTokenBinding=false + UseBoundCredential=true => bound callback, final token Bearer.
            var result = await builder.WithClientCredentialsAsync(
                new MergedOptions { ClientCredentials = new[] { credentialDescription } },
                ProviderThatCaches(provider),
                credentialSourceLoaderParameters: null,
                isTokenBinding: false);

            Assert.Same(builder, result);
        }

        [Fact]
        public async Task WithClientCredentialsAsync_UseBoundCredentialFalse_SignedAssertion_UsesStringCallbackAsync()
        {
            var builder = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                .WithAuthority(TestConstants.AuthorityCommonTenant);

            var provider = new TestBindingProvider(new ClientSignedAssertion
            {
                Assertion = "a",
                TokenBindingCertificate = LoadTestCertificate(),
            });

            var credentialDescription = new CredentialDescription
            {
                SourceType = CredentialSource.CustomSignedAssertion,
                CustomSignedAssertionProviderName = "OidcIdpSignedAssertion",
                UseBoundCredential = false,
            };

            var result = await builder.WithClientCredentialsAsync(
                new MergedOptions { ClientCredentials = new[] { credentialDescription } },
                ProviderThatCaches(provider),
                credentialSourceLoaderParameters: null,
                isTokenBinding: false);

            Assert.Same(builder, result);
        }

        [Fact]
        public async Task WithClientCredentialsAsync_UseBoundCredential_ManagedIdentityAssertion_WiresBoundCallbackAsync()
        {
            var builder = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                .WithAuthority(TestConstants.AuthorityCommonTenant);

            var provider = new ManagedIdentityClientAssertion("a599ce88-0a5f-4a6e-beca-e67d3fc427f4");

            var credentialDescription = new CredentialDescription
            {
                SourceType = CredentialSource.SignedAssertionFromManagedIdentity,
                ManagedIdentityClientId = "a599ce88-0a5f-4a6e-beca-e67d3fc427f4",
                UseBoundCredential = true,
            };

            var result = await builder.WithClientCredentialsAsync(
                new MergedOptions { ClientCredentials = new[] { credentialDescription } },
                ProviderThatCaches(provider),
                credentialSourceLoaderParameters: null,
                isTokenBinding: false);

            Assert.Same(builder, result);
        }

        [Fact]
        public async Task WithClientCredentialsAsync_UseBoundCredential_FileAssertion_ThrowsIDW10115Async()
        {
            var builder = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                .WithAuthority(TestConstants.AuthorityCommonTenant);

            var provider = new AzureIdentityForKubernetesClientAssertion("/var/run/secrets/azure/tokens/azure-identity-token");

            var credentialDescription = new CredentialDescription
            {
                SourceType = CredentialSource.SignedAssertionFilePath,
                SignedAssertionFileDiskPath = "/var/run/secrets/azure/tokens/azure-identity-token",
                UseBoundCredential = true,
            };

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => builder.WithClientCredentialsAsync(
                    new MergedOptions { ClientCredentials = new[] { credentialDescription } },
                    ProviderThatCaches(provider),
                    credentialSourceLoaderParameters: null,
                    isTokenBinding: false));

            Assert.Contains("IDW10115", ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        public async Task WithClientCredentialsAsync_UseBoundCredential_NullBoundResult_ThrowsClearExceptionAsync()
        {
            var builder = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                .WithAuthority(TestConstants.AuthorityCommonTenant);

            // Advertises binding support but returns null: the wiring must surface a clear
            // Identity Web exception (IDW10115) instead of a NullReferenceException.
            var provider = new TestBindingProvider(boundResult: null);

            var credentialDescription = new CredentialDescription
            {
                SourceType = CredentialSource.CustomSignedAssertion,
                CustomSignedAssertionProviderName = "OidcIdpSignedAssertion",
                UseBoundCredential = true,
            };

            var configured = await builder.WithClientCredentialsAsync(
                new MergedOptions { ClientCredentials = new[] { credentialDescription } },
                ProviderThatCaches(provider),
                credentialSourceLoaderParameters: null,
                isTokenBinding: false);

            var cca = configured.Build();

            // The bound callback is invoked during ExecuteAsync (before any network I/O), so the
            // null result surfaces as the clear IDW10115 error, not a NullReferenceException.
            var ex = await Assert.ThrowsAnyAsync<Exception>(
                () => cca.AcquireTokenForClient(new[] { "https://graph.microsoft.com/.default" }).ExecuteAsync());

            Assert.DoesNotContain(nameof(NullReferenceException), ex.ToString(), StringComparison.Ordinal);
            Assert.Contains("IDW10115", ex.ToString(), StringComparison.Ordinal);
        }

        /// <summary>
        /// Provider whose two callbacks throw distinct sentinels, so executing the built CCA stops at
        /// whichever callback MSAL selects (before any network I/O). This proves callback *selection*
        /// (bound vs. string) rather than merely that a builder was returned.
        /// </summary>
        private sealed class ThrowingSelectionProvider : ClientAssertionProviderBase
        {
            public const string StringCallbackSentinel = "STRING_CALLBACK_INVOKED";
            public const string BoundCallbackSentinel = "BOUND_CALLBACK_INVOKED";

            public override bool SupportsTokenBinding => true;

            protected override Task<ClientAssertion> GetClientAssertionAsync(AssertionRequestOptions? assertionRequestOptions)
                => throw new InvalidOperationException(StringCallbackSentinel);

            public override Task<ClientSignedAssertion?> GetSignedAssertionWithBindingAsync(
                AssertionRequestOptions? assertionRequestOptions,
                CancellationToken cancellationToken = default)
                => throw new InvalidOperationException(BoundCallbackSentinel);
        }

        [Fact]
        public async Task WithClientCredentialsAsync_UseBoundCredentialTrue_SelectsBoundCallback_NotStringCallbackAsync()
        {
            var builder = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                .WithAuthority(TestConstants.AuthorityCommonTenant);

            var credentialDescription = new CredentialDescription
            {
                SourceType = CredentialSource.CustomSignedAssertion,
                CustomSignedAssertionProviderName = "OidcIdpSignedAssertion",
                UseBoundCredential = true,
            };

            var configured = await builder.WithClientCredentialsAsync(
                new MergedOptions { ClientCredentials = new[] { credentialDescription } },
                ProviderThatCaches(new ThrowingSelectionProvider()),
                credentialSourceLoaderParameters: null,
                isTokenBinding: false);

            var cca = configured.Build();

            // UseBoundCredential=true must wire the ClientSignedAssertion (bound) callback, so executing
            // reaches GetSignedAssertionWithBindingAsync (not the string GetClientAssertionAsync).
            var ex = await Assert.ThrowsAnyAsync<Exception>(
                () => cca.AcquireTokenForClient(new[] { "https://graph.microsoft.com/.default" }).ExecuteAsync());

            Assert.Contains(ThrowingSelectionProvider.BoundCallbackSentinel, ex.ToString(), StringComparison.Ordinal);
            Assert.DoesNotContain(ThrowingSelectionProvider.StringCallbackSentinel, ex.ToString(), StringComparison.Ordinal);
        }

        [Fact]
        public async Task WithClientCredentialsAsync_UseBoundCredentialFalse_SelectsStringCallback_NotBoundCallbackAsync()
        {
            var builder = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                .WithAuthority(TestConstants.AuthorityCommonTenant);

            var credentialDescription = new CredentialDescription
            {
                SourceType = CredentialSource.CustomSignedAssertion,
                CustomSignedAssertionProviderName = "OidcIdpSignedAssertion",
                UseBoundCredential = false,
            };

            var configured = await builder.WithClientCredentialsAsync(
                new MergedOptions { ClientCredentials = new[] { credentialDescription } },
                ProviderThatCaches(new ThrowingSelectionProvider()),
                credentialSourceLoaderParameters: null,
                isTokenBinding: false);

            var cca = configured.Build();

            // UseBoundCredential=false must wire the string assertion callback, so executing reaches
            // GetClientAssertionAsync (not the bound GetSignedAssertionWithBindingAsync) — even though
            // this provider advertises SupportsTokenBinding=true.
            var ex = await Assert.ThrowsAnyAsync<Exception>(
                () => cca.AcquireTokenForClient(new[] { "https://graph.microsoft.com/.default" }).ExecuteAsync());

            Assert.Contains(ThrowingSelectionProvider.StringCallbackSentinel, ex.ToString(), StringComparison.Ordinal);
            Assert.DoesNotContain(ThrowingSelectionProvider.BoundCallbackSentinel, ex.ToString(), StringComparison.Ordinal);
        }

        #endregion

        #region IDW10109 inner exception preservation tests

        [Fact]
        public async Task AllCredentialsFail_SingleFailure_PreservesInnerExceptionAndErrorText()
        {
            // Arrange
            var logger = Substitute.For<ILogger<CredentialsProvider>>();
            ICredentialsLoader credLoader = Substitute.For<ICredentialsLoader>();

            var msalException = new MsalServiceException(
                "AADSTS700027",
                "AADSTS700027: Client assertion contains an invalid signature.");

            credLoader.LoadCredentialsIfNeededAsync(Arg.Any<CredentialDescription>(), Arg.Any<CredentialSourceLoaderParameters>())
                .Returns(args =>
                {
                    var cd = (args[0] as CredentialDescription)!;
                    cd.Skip = true;
                    return Task.FromException(msalException);
                });

            CredentialsProvider provider = new CredentialsProvider(logger, credLoader, [], null);

            var credential = new CredentialDescription
            {
                SourceType = CredentialSource.SignedAssertionFromManagedIdentity,
                ManagedIdentityClientId = "test-client-id"
            };

            // Act
            var ex = await Assert.ThrowsAsync<ArgumentException>(() => provider.GetCredentialAsync(
                new MergedOptions { ClientCredentials = new[] { credential } }, null));

            // Assert — original exception is preserved as InnerException
            Assert.NotNull(ex.InnerException);
            var inner = Assert.IsType<MsalServiceException>(ex.InnerException);
            Assert.Equal("AADSTS700027", inner.ErrorCode);

            // Assert — error message includes IDW10109 code and FIC guidance
            Assert.StartsWith("IDW10109:", ex.Message, StringComparison.Ordinal);
            Assert.Contains("Federated Identity Credential", ex.Message, StringComparison.Ordinal);
            Assert.Contains("inner exception", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task AllCredentialsFail_MultipleFailures_PreservesAllExceptionsViaAggregateException()
        {
            // Arrange
            var logger = Substitute.For<ILogger<CredentialsProvider>>();
            ICredentialsLoader credLoader = Substitute.For<ICredentialsLoader>();

            var msalException = new MsalServiceException("AADSTS700027", "Invalid signature.");
            var certException = new InvalidOperationException("Certificate not found in store.");

            int callCount = 0;
            credLoader.LoadCredentialsIfNeededAsync(Arg.Any<CredentialDescription>(), Arg.Any<CredentialSourceLoaderParameters>())
                .Returns(args =>
                {
                    var cd = (args[0] as CredentialDescription)!;
                    cd.Skip = true;
                    return Task.FromException(callCount++ == 0 ? (Exception)msalException : certException);
                });

            CredentialsProvider provider = new CredentialsProvider(logger, credLoader, [], null);

            var credentials = new[]
            {
                new CredentialDescription
                {
                    SourceType = CredentialSource.SignedAssertionFromManagedIdentity,
                    ManagedIdentityClientId = "test-client-id"
                },
                new CredentialDescription
                {
                    SourceType = CredentialSource.StoreWithDistinguishedName,
                    CertificateStorePath = "LocalMachine/My",
                    CertificateDistinguishedName = "CN=Test"
                }
            };

            // Act
            var ex = await Assert.ThrowsAsync<ArgumentException>(() => provider.GetCredentialAsync(
                new MergedOptions { ClientCredentials = credentials }, null));

            // Assert — InnerException is AggregateException containing both originals
            Assert.NotNull(ex.InnerException);
            var aggEx = Assert.IsType<AggregateException>(ex.InnerException);
            Assert.Equal(2, aggEx.InnerExceptions.Count);
            Assert.IsType<MsalServiceException>(aggEx.InnerExceptions[0]);
            Assert.IsType<InvalidOperationException>(aggEx.InnerExceptions[1]);
        }

        #endregion
    }
}
