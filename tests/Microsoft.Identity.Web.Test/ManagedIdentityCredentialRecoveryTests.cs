// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web.Extensibility;
using Microsoft.Identity.Web.Test.Common;
using Microsoft.Identity.Web.Test.Common.Mocks;
using Microsoft.Identity.Web.TestOnly;
using NSubstitute;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    [Collection(nameof(TokenAcquirerFactorySingletonProtection))]
    public class ManagedIdentityCredentialRecoveryTests
    {
        [Theory]
        [InlineData(false, false)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(true, true)]
        public async Task AppAcquisition_NoWinner_RetriesWithoutReset(bool keyAttested, bool explicitAudience)
        {
            // Arrange
            TokenAcquirerFactoryTesting.ResetTokenAcquirerFactoryInTest();
            var factory = TokenAcquirerFactory.GetDefaultInstance();
            using var miHttp = new MockHttpClientFactory();
            using var http = new MockHttpClientFactory();
            var failure = new MsalServiceException("managed_identity_unreachable_network", "MI endpoint unavailable.");
            miHttp.AddMockHandler(new MockHttpMessageHandler { ExceptionToThrow = failure });
            var credential = CreateManagedIdentityCredential(explicitAudience ? "api://AzureADTokenExchange" : null);
            ConfigureApplication(factory, http, [credential], keyAttested);
            IMsalHttpClientFactory? previousFactory = ManagedIdentityClientAssertionTestHook.HttpClientFactoryForTests;
            ManagedIdentityClientAssertionTestHook.HttpClientFactoryForTests = miHttp;
            try
            {
                var services = factory.Build();
                var acquisition = services.GetRequiredService<ITokenAcquisition>();

                // Act
                var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
                    acquisition.GetAuthenticationResultForAppAsync(TestConstants.s_scopeForApp));

                // Assert
                Assert.Same(failure, exception.InnerException);
                Assert.False(credential.Skip);
                Assert.Null(credential.CachedValue);

                miHttp.AddMockHandler(MockHttpCreator.CreateMsiTokenHandler("recovered-assertion", "api://AzureADTokenExchange"));
                var outer = http.AddMockHandler(MockHttpCreator.CreateClientCredentialTokenHandler("outer"));
                var result = await acquisition.GetAuthenticationResultForAppAsync(TestConstants.s_scopeForApp);
                var cached = await acquisition.GetAuthenticationResultForAppAsync(TestConstants.s_scopeForApp);

                Assert.Equal("outer", result.AccessToken);
                Assert.Equal("recovered-assertion", outer.ActualRequestPostData["client_assertion"]);
                Assert.Equal(TokenSource.Cache, cached.AuthenticationResultMetadata.TokenSource);
                if (explicitAudience)
                {
                    Assert.IsType<ManagedIdentityClientAssertion>(credential.CachedValue);
                }
                else
                {
                    Assert.Null(credential.CachedValue);
                    Assert.Null(credential.TokenExchangeUrl);
                }
            }
            finally
            {
                ManagedIdentityClientAssertionTestHook.HttpClientFactoryForTests = previousFactory;
                TokenAcquirerFactoryTesting.ResetTokenAcquirerFactoryInTest();
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task AppAcquisition_CertificateFallback_RemainsSelected(bool keyAttested)
        {
            // Arrange
            TokenAcquirerFactoryTesting.ResetTokenAcquirerFactoryInTest();
            var factory = TokenAcquirerFactory.GetDefaultInstance();
            using var miHttp = new MockHttpClientFactory();
            using var http = new MockHttpClientFactory();
            miHttp.AddMockHandler(new MockHttpMessageHandler
            {
                ExceptionToThrow = new MsalServiceException("managed_identity_unreachable_network", "MI unavailable locally."),
            });
            var miFactory = Substitute.For<IMsalHttpClientFactory>();
            miFactory.GetHttpClient().Returns(_ => miHttp.GetHttpClient());
            var credential = CreateManagedIdentityCredential("api://AzureADTokenExchange");
            var certificate = CertificateDescription.FromBase64Encoded(
                TestConstants.CertificateX5cWithPrivateKey, TestConstants.CertificateX5cWithPrivateKeyPassword);
            ConfigureApplication(factory, http, [credential, certificate], keyAttested);
            var firstOuter = http.AddMockHandler(MockHttpCreator.CreateClientCredentialTokenHandler("first"));
            IMsalHttpClientFactory? previousFactory = ManagedIdentityClientAssertionTestHook.HttpClientFactoryForTests;
            ManagedIdentityClientAssertionTestHook.HttpClientFactoryForTests = miFactory;
            try
            {
                var services = factory.Build();
                var acquisition = services.GetRequiredService<ITokenAcquisition>();
                var applicationProvider = services.GetRequiredService<IConfidentialClientApplicationProvider>();

                // Act
                var first = await acquisition.GetAuthenticationResultForAppAsync(TestConstants.s_scopeForApp);
                miFactory.ClearReceivedCalls();
                var firstApplication = await applicationProvider.GetConfidentialClientApplicationAsync();
                var secondOuter = http.AddMockHandler(MockHttpCreator.CreateClientCredentialTokenHandler("second"));
                var refreshed = await acquisition.GetAuthenticationResultForAppAsync(
                    TestConstants.s_scopeForApp, tokenAcquisitionOptions: new TokenAcquisitionOptions { ForceRefresh = true });
                var secondApplication = await applicationProvider.GetConfidentialClientApplicationAsync();

                // Assert
                Assert.Equal("first", first.AccessToken);
                Assert.Equal("second", refreshed.AccessToken);
                Assert.Same(firstApplication, secondApplication);
                miFactory.DidNotReceive().GetHttpClient();
                Assert.False(credential.Skip);
                Assert.Null(credential.CachedValue);
                Assert.Equal(certificate.Certificate!.Thumbprint, firstApplication.AppConfig.ClientCredentialCertificate!.Thumbprint);
                Assert.NotEmpty(firstOuter.ActualRequestPostData["client_assertion"]);
                Assert.NotEmpty(secondOuter.ActualRequestPostData["client_assertion"]);
            }
            finally
            {
                ManagedIdentityClientAssertionTestHook.HttpClientFactoryForTests = previousFactory;
                TokenAcquirerFactoryTesting.ResetTokenAcquirerFactoryInTest();
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task DirectLoad_Failure_DoesNotCacheOrSkip_AndNextCallRecovers(bool keyAttested)
        {
            // Arrange
            using ServiceProvider services = CreateLoaderServices(keyAttested);
            var loader = services.GetRequiredService<ICredentialsLoader>();
            var credential = CreateManagedIdentityCredential("api://AzureADTokenExchange");
            using var failedHttp = new MockHttpClientFactory();
            using var recoveredHttp = new MockHttpClientFactory();
            failedHttp.AddMockHandler(new MockHttpMessageHandler
            {
                ExceptionToThrow = new MsalServiceException("managed_identity_unreachable_network", "MI unavailable."),
            });
            IMsalHttpClientFactory? previousFactory = ManagedIdentityClientAssertionTestHook.HttpClientFactoryForTests;
            ManagedIdentityClientAssertionTestHook.HttpClientFactoryForTests = failedHttp;
            try
            {
                await Assert.ThrowsAsync<MsalServiceException>(() => loader.LoadCredentialsIfNeededAsync(credential));
                Assert.Null(credential.CachedValue);
                Assert.False(credential.Skip);

                // Act
                recoveredHttp.AddMockHandler(MockHttpCreator.CreateMsiTokenHandler("recovered", "api://AzureADTokenExchange"));
                ManagedIdentityClientAssertionTestHook.HttpClientFactoryForTests = recoveredHttp;
                await loader.LoadCredentialsIfNeededAsync(credential);

                // Assert
                Assert.False(credential.Skip);
                Assert.IsType<ManagedIdentityClientAssertion>(credential.CachedValue);
            }
            finally
            {
                ManagedIdentityClientAssertionTestHook.HttpClientFactoryForTests = previousFactory;
            }
        }

        [Theory]
        [InlineData(CredentialSource.SignedAssertionFromManagedIdentity, null)]
        [InlineData(CredentialSource.SignedAssertionFilePath, null)]
        [InlineData(CredentialSource.CustomSignedAssertion, "OidcIdpSignedAssertion")]
        public async Task Selection_FailedBuiltInAssertionLoad_UsesFallback(
            CredentialSource source, string? providerName)
        {
            // Arrange
            var loader = Substitute.For<ICredentialsLoader>();
            var credential = new CredentialDescription
            {
                SourceType = source,
                CustomSignedAssertionProviderName = providerName,
            };
            var fallback = new CredentialDescription { SourceType = CredentialSource.ClientSecret, ClientSecret = "fallback" };
            var failure = new InvalidOperationException("Assertion loading failed.");
            loader.LoadCredentialsIfNeededAsync(credential, null).Returns(Task.FromException(failure));
            var provider = new CredentialsProvider(NullLogger<CredentialsProvider>.Instance, loader, [], null);

            // Act
            CredentialDescription? selected = await provider.GetCredentialAsync(
                new MergedOptions { ClientCredentials = [credential, fallback] }, null);

            // Assert
            Assert.Same(fallback, selected);
            Assert.False(credential.Skip);
        }

        [Theory]
        [InlineData(CredentialSource.SignedAssertionFromManagedIdentity, null)]
        [InlineData(CredentialSource.SignedAssertionFilePath, null)]
        [InlineData(CredentialSource.CustomSignedAssertion, "OidcIdpSignedAssertion")]
        public async Task Selection_ExplicitSkip_DoesNotProbeBuiltInAssertion(
            CredentialSource source, string? providerName)
        {
            // Arrange
            var loader = Substitute.For<ICredentialsLoader>();
            var credential = new CredentialDescription
            {
                SourceType = source,
                CustomSignedAssertionProviderName = providerName,
                Skip = true,
            };
            var fallback = new CredentialDescription { SourceType = CredentialSource.ClientSecret, ClientSecret = "fallback" };
            var provider = new CredentialsProvider(NullLogger<CredentialsProvider>.Instance, loader, [], null);

            // Act
            CredentialDescription? selected = await provider.GetCredentialAsync(
                new MergedOptions { ClientCredentials = [credential, fallback] }, null);

            // Assert
            Assert.Same(fallback, selected);
            Assert.True(credential.Skip);
            await loader.DidNotReceive().LoadCredentialsIfNeededAsync(credential, Arg.Any<CredentialSourceLoaderParameters>());
        }

        [Fact]
        public async Task Selection_CanceledLoad_PreservesOriginalException()
        {
            // Arrange
            var loader = Substitute.For<ICredentialsLoader>();
            var credential = CreateManagedIdentityCredential("api://AzureADTokenExchange");
            var cancellationToken = new CancellationToken(canceled: true);
            var failure = new OperationCanceledException(cancellationToken);
            loader.LoadCredentialsIfNeededAsync(credential, null).Returns(Task.FromException(failure));
            var provider = new CredentialsProvider(NullLogger<CredentialsProvider>.Instance, loader, [], null);

            // Act
            var exception = await Assert.ThrowsAsync<ArgumentException>(() => provider.GetCredentialAsync(
                new MergedOptions { ClientCredentials = [credential] }, null, cancellationToken));

            // Assert
            Assert.Same(failure, exception.InnerException);
            Assert.False(credential.Skip);
        }

        private static CredentialDescription CreateManagedIdentityCredential(string? audience) => new CredentialDescription
        {
            SourceType = CredentialSource.SignedAssertionFromManagedIdentity,
            ManagedIdentityClientId = Guid.NewGuid().ToString(),
            TokenExchangeUrl = audience,
        };

        private static ServiceProvider CreateLoaderServices(bool keyAttested)
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddTokenAcquisition();
            if (keyAttested)
            {
                services.AddMicrosoftIdentityWebKeyAttestation();
            }
            return services.BuildServiceProvider();
        }

        private static void ConfigureApplication(
            TokenAcquirerFactory factory, MockHttpClientFactory http, CredentialDescription[] credentials, bool keyAttested)
        {
            factory.Services.AddSingleton<IHttpClientFactory>(http);
            if (keyAttested)
            {
                factory.Services.AddMicrosoftIdentityWebKeyAttestation();
            }
            factory.Services.Configure<MicrosoftIdentityApplicationOptions>(options =>
            {
                options.Instance = "https://login.microsoftonline.com/";
                options.TenantId = "11111111-1111-1111-1111-111111111111";
                options.ClientId = Guid.NewGuid().ToString();
                options.ClientCredentials = credentials;
            });
        }
    }
}
