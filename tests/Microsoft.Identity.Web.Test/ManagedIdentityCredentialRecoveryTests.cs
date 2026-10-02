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
using Microsoft.Identity.Web.Test.Common;
using Microsoft.Identity.Web.Test.Common.Mocks;
using Microsoft.Identity.Web.TestOnly;
using Xunit;

namespace Microsoft.Identity.Web.Test;

[Collection(nameof(TokenAcquirerFactorySingletonProtection))]
public class ManagedIdentityCredentialRecoveryTests
{
    [Theory]
    [InlineData("https://login.microsoftonline.com", "api://AzureADTokenExchange")]
    [InlineData("https://login.microsoftonline.us", "api://AzureADTokenExchangeUSGov")]
    [InlineData("https://login.partner.microsoftonline.cn", "api://AzureADTokenExchangeChina")]
    [InlineData("https://login.microsoftonline.us", "api://CustomTokenExchange")]
    public async Task GetAccessTokenForApp_InitialManagedIdentityFailure_RecoversWithSameServicesAsync(
        string instance, string audience)
    {
        // Arrange
        TokenAcquirerFactoryTesting.ResetTokenAcquirerFactoryInTest();
        var factory = TokenAcquirerFactory.GetDefaultInstance();
        string authority = instance + "/11111111-1111-1111-1111-111111111111";
        var credential = new CredentialDescription
        {
            SourceType = CredentialSource.SignedAssertionFromManagedIdentity,
            ManagedIdentityClientId = Guid.NewGuid().ToString(),
            TokenExchangeUrl = audience,
        };
        using var http = new MockHttpClientFactory();
        using var miHttp = new MockHttpClientFactory();
        var failure = new MsalServiceException(
            "managed_identity_unreachable_network", "The managed identity endpoint is temporarily unavailable.");
        miHttp.AddMockHandler(new MockHttpMessageHandler { ExceptionToThrow = failure });
        factory.Services.AddSingleton<IHttpClientFactory>(http);
        factory.Services.Configure<MicrosoftIdentityApplicationOptions>(options =>
        {
            options.Authority = authority;
            options.ClientId = Guid.NewGuid().ToString();
            options.ClientCredentials = new[] { credential };
        });

        IMsalHttpClientFactory? previousFactory = ManagedIdentityClientAssertionTestHook.HttpClientFactoryForTests;
        ManagedIdentityClientAssertionTestHook.HttpClientFactoryForTests = miHttp;
        try
        {
            IServiceProvider services = factory.Build();
            var acquisition = services.GetRequiredService<ITokenAcquisition>();
            var host = services.GetRequiredService<ITokenAcquisitionHost>();
            Assert.Same(credential, Assert.Single(host.GetOptions(null, out _).ClientCredentials!));

            // Act: let the first request fail before restoring the dependency.
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => acquisition.GetAccessTokenForAppAsync(TestConstants.s_scopeForApp));
            Assert.StartsWith("IDW10109:", exception.Message, StringComparison.Ordinal);
            Assert.Same(failure, exception.InnerException);
            Assert.Null(credential.CachedValue);

            MockHttpMessageHandler assertionHandler = miHttp.AddMockHandler(
                MockHttpCreator.CreateMsiTokenHandler("recovered-assertion", audience));
            MockHttpMessageHandler tokenHandler = http.AddMockHandler(
                MockHttpCreator.CreateClientCredentialTokenHandler("recovered-application-token"));
            tokenHandler.ExpectedUrl = authority + "/oauth2/v2.0/token";

            string token = await acquisition.GetAccessTokenForAppAsync(TestConstants.s_scopeForApp);
            using var cancellation = new CancellationTokenSource();
            await cancellation.CancelAsync();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => acquisition.GetAccessTokenForAppAsync(
                TestConstants.s_scopeForApp,
                tokenAcquisitionOptions: new TokenAcquisitionOptions
                {
                    ForceRefresh = true,
                    CancellationToken = cancellation.Token,
                }));
            string cachedToken = await acquisition.GetAccessTokenForAppAsync(TestConstants.s_scopeForApp);

            // Assert
            Assert.Equal("recovered-application-token", token);
            Assert.Equal(token, cachedToken);
            Assert.Same(acquisition, services.GetRequiredService<ITokenAcquisition>());
            Assert.Same(credential, Assert.Single(host.GetOptions(null, out _).ClientCredentials!));
            Assert.False(credential.Skip);
            Assert.IsType<ManagedIdentityClientAssertion>(credential.CachedValue);
            Assert.Equal(audience, credential.TokenExchangeUrl);
            Assert.Contains("resource=" + audience, assertionHandler.ActualRequestMessage.RequestUri!.Query, StringComparison.Ordinal);
            Assert.Contains("client_id=" + credential.ManagedIdentityClientId, assertionHandler.ActualRequestMessage.RequestUri.Query, StringComparison.Ordinal);
            Assert.Equal("recovered-assertion", tokenHandler.ActualRequestPostData["client_assertion"]);
        }
        finally
        {
            ManagedIdentityClientAssertionTestHook.HttpClientFactoryForTests = previousFactory;
            TokenAcquirerFactoryTesting.ResetTokenAcquirerFactoryInTest();
        }
    }

    [Fact]
    public async Task GetCredential_FailedManagedIdentity_RemainsEligibleInCredentialChainAsync()
    {
        // Arrange
        using var miHttp = new MockHttpClientFactory();
        var credential = new CredentialDescription
        {
            SourceType = CredentialSource.SignedAssertionFromManagedIdentity,
            ManagedIdentityClientId = Guid.NewGuid().ToString(),
            TokenExchangeUrl = "api://AzureADTokenExchangeUSGov",
        };
        var fallback = new CredentialDescription
        {
            SourceType = CredentialSource.ClientSecret,
            ClientSecret = TestConstants.ClientSecret,
        };
        var options = new MergedOptions { ClientCredentials = new[] { credential, fallback } };
        var parameters = new CredentialSourceLoaderParameters("client", "https://login.microsoftonline.us/tenant");
        var provider = new CredentialsProvider(NullLogger<CredentialsProvider>.Instance, new DefaultCredentialsLoader(), []);
        var failure = new MsalServiceException("managed_identity_unreachable_network", "Managed identity is unavailable.");
        miHttp.AddMockHandler(new MockHttpMessageHandler { ExceptionToThrow = failure });
        IMsalHttpClientFactory? previousFactory = ManagedIdentityClientAssertionTestHook.HttpClientFactoryForTests;
        ManagedIdentityClientAssertionTestHook.HttpClientFactoryForTests = miHttp;
        try
        {
            // Act
            Assert.Same(fallback, await provider.GetCredentialAsync(options, parameters));
            Assert.False(credential.Skip);
            Assert.Null(credential.CachedValue);
            miHttp.AddMockHandler(MockHttpCreator.CreateMsiTokenHandler("recovered-assertion", credential.TokenExchangeUrl));
            CredentialDescription? recovered = await provider.GetCredentialAsync(options, parameters);

            // Assert
            Assert.Same(credential, recovered);
            Assert.IsType<ManagedIdentityClientAssertion>(credential.CachedValue);
            Assert.False(credential.Skip);
        }
        finally
        {
            ManagedIdentityClientAssertionTestHook.HttpClientFactoryForTests = previousFactory;
        }
    }

    [Theory]
    [InlineData("managed_identity_unreachable_network")]
    [InlineData("invalid_client")]
    public async Task LoadCredentials_RepeatedManagedIdentityFailure_PropagatesWithoutCachingOrSkippingAsync(string errorCode)
    {
        // Arrange
        using var miHttp = new MockHttpClientFactory();
        var credential = new CredentialDescription
        {
            SourceType = CredentialSource.SignedAssertionFromManagedIdentity,
            ManagedIdentityClientId = Guid.NewGuid().ToString(),
            TokenExchangeUrl = "api://AzureADTokenExchangeUSGov",
        };
        var loader = new DefaultCredentialsLoader();
        var parameters = new CredentialSourceLoaderParameters("client", "https://login.microsoftonline.us/tenant");
        IMsalHttpClientFactory? previousFactory = ManagedIdentityClientAssertionTestHook.HttpClientFactoryForTests;
        ManagedIdentityClientAssertionTestHook.HttpClientFactoryForTests = miHttp;
        try
        {
            for (int attempt = 0; attempt < 2; attempt++)
            {
                var failure = new MsalServiceException(errorCode, "Managed identity acquisition failed.");
                miHttp.AddMockHandler(new MockHttpMessageHandler { ExceptionToThrow = failure });

                // Act
                var exception = await Assert.ThrowsAsync<MsalServiceException>(
                    () => loader.LoadCredentialsIfNeededAsync(credential, parameters));

                // Assert
                Assert.Same(failure, exception);
                Assert.Null(credential.CachedValue);
                Assert.False(credential.Skip);
            }
        }
        finally
        {
            ManagedIdentityClientAssertionTestHook.HttpClientFactoryForTests = previousFactory;
        }
    }
}
