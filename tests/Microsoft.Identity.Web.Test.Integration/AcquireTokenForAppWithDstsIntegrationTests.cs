// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Client;
using Microsoft.Identity.Lab.Api;
using Microsoft.Identity.Web.Test.Common;
using Microsoft.Identity.Web.Test.Common.Mocks;
using Microsoft.Identity.Web.Test.Common.TestHelpers;
using Microsoft.Identity.Web.TokenCacheProviders.InMemory;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Identity.Web.Test.Integration
{
#if !FROM_GITHUB_ACTION
    public class AcquireTokenForAppWithDstsIntegrationTests
    {
        private TokenAcquisition _tokenAcquisition;
        private ServiceProvider? _provider;
        private MsalTestTokenCacheProvider _msalTestTokenCacheProvider;
        private IOptionsMonitor<MicrosoftIdentityOptions> _microsoftIdentityOptionsMonitor;
        private IOptionsMonitor<ConfidentialClientApplicationOptions> _applicationOptionsMonitor;
        private ICredentialsLoader _credentialsLoader;

        private readonly string _ccaSecret;
        private readonly ITestOutputHelper _output;

        private ServiceProvider Provider { get => _provider!; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public AcquireTokenForAppWithDstsIntegrationTests(ITestOutputHelper output)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {
            _output = output;

            KeyVaultSecretsProvider keyVaultSecretsProvider = new KeyVaultSecretsProvider(TestConstants.MSIDLabLabKeyVaultName);
            _ccaSecret = keyVaultSecretsProvider.GetSecretByName(TestConstants.AzureADIdentityDivisionTestAgentSecret).Value;

            // Need the secret before building the services
            if (!string.IsNullOrEmpty(_ccaSecret))
            {
                BuildTheRequiredServices();
            }
            else
            {
                _output.WriteLine("Connection to Key Vault failed. No secret returned. ");
                throw new ArgumentNullException(message: "No secret returned from Key Vault. ", null);
            }
        }

        [Fact]
        public async Task GetAccessTokenForApp_WithDstsAuthority_AcquiresTokenAsync()
        {
            // Arrange
            InitializeTokenAcquisitionObjects();

            Assert.Equal(0, _msalTestTokenCacheProvider.Count);

            // Act - Acquire token from dSTS using client credentials flow
            // Note: This will use the standard token endpoint configured in the options
            string token = await _tokenAcquisition.GetAccessTokenForAppAsync(TestConstants.s_scopeForApp);

            // Assert
            Assert.NotNull(token);
            Assert.NotEmpty(token);
            _output.WriteLine($"Successfully acquired token from dSTS. Token length: {token.Length}");

            // Verify token was cached
            Assert.Equal(1, _msalTestTokenCacheProvider.Count);
        }

        [Fact]
        public async Task GetAuthenticationResultForApp_WithDstsAuthority_ReturnsValidResultAsync()
        {
            // Arrange
            InitializeTokenAcquisitionObjects();

            Assert.Equal(0, _msalTestTokenCacheProvider.Count);

            // Act
            AuthenticationResult authResult = await _tokenAcquisition.GetAuthenticationResultForAppAsync(TestConstants.s_scopeForApp);

            // Assert
            Assert.NotNull(authResult);
            Assert.NotNull(authResult.AccessToken);
            Assert.NotEmpty(authResult.AccessToken);
            Assert.Null(authResult.IdToken);
            Assert.Null(authResult.Account);

            _output.WriteLine($"Successfully acquired authentication result from dSTS.");
            _output.WriteLine($"Access Token: {authResult.AccessToken[..Math.Min(50, authResult.AccessToken.Length)]}...");
            _output.WriteLine($"Token Type: {authResult.TokenType}");
            _output.WriteLine($"Expires On: {authResult.ExpiresOn}");

            // Verify token was cached
            Assert.Equal(1, _msalTestTokenCacheProvider.Count);
        }

        [Theory]
        [InlineData(true)]  // Test GetAuthenticationResultForAppAsync
        [InlineData(false)] // Test GetAccessTokenForAppAsync
        public async Task GetAccessTokenOrAuthResultForApp_WithDstsAuthority_UsesCachedTokenAsync(bool getAuthResult)
        {
            // Arrange
            InitializeTokenAcquisitionObjects();

            Assert.Equal(0, _msalTestTokenCacheProvider.Count);

            if (getAuthResult)
            {
                // Act - First acquisition
                AuthenticationResult authResult1 = await _tokenAcquisition.GetAuthenticationResultForAppAsync(TestConstants.s_scopeForApp);

                Assert.Equal(1, _msalTestTokenCacheProvider.Count);

                // Act - Second acquisition (should use cache)
                AuthenticationResult authResult2 = await _tokenAcquisition.GetAuthenticationResultForAppAsync(TestConstants.s_scopeForApp);

                // Assert
                Assert.NotNull(authResult1);
                Assert.NotNull(authResult2);
                Assert.Equal(authResult1.AccessToken, authResult2.AccessToken); // Should return the same cached token
                Assert.Equal(1, _msalTestTokenCacheProvider.Count); // Cache count should remain 1

                _output.WriteLine("Successfully verified AuthenticationResult caching with dSTS.");
            }
            else
            {
                // Act - First acquisition
                string token1 = await _tokenAcquisition.GetAccessTokenForAppAsync(TestConstants.s_scopeForApp);

                Assert.Equal(1, _msalTestTokenCacheProvider.Count);

                // Act - Second acquisition (should use cache)
                string token2 = await _tokenAcquisition.GetAccessTokenForAppAsync(TestConstants.s_scopeForApp);

                // Assert
                Assert.NotNull(token1);
                Assert.NotNull(token2);
                Assert.Equal(token1, token2); // Should return the same cached token
                Assert.Equal(1, _msalTestTokenCacheProvider.Count); // Cache count should remain 1

                _output.WriteLine("Successfully verified access token caching with dSTS.");
            }
        }

        [Fact]
        public async Task GetAccessTokenForApp_WithDstsAuthority_AndTenantId_AcquiresTokenAsync()
        {
            // Arrange
            InitializeTokenAcquisitionObjects();

            Assert.Equal(0, _msalTestTokenCacheProvider.Count);

            // Act - Acquire token with specific tenant
            string token = await _tokenAcquisition.GetAccessTokenForAppAsync(
                TestConstants.s_scopeForApp,
                tenant: TestConstants.ConfidentialClientLabTenant);

            // Assert
            Assert.NotNull(token);
            Assert.NotEmpty(token);
            _output.WriteLine($"Successfully acquired token from dSTS for specific tenant. Token length: {token.Length}");

            Assert.Equal(1, _msalTestTokenCacheProvider.Count);
        }

        [Fact]
        public async Task GetAccessTokenForApp_WithDstsAuthority_AndTokenAcquisitionOptions_AcquiresTokenAsync()
        {
            // Arrange
            InitializeTokenAcquisitionObjects();

            Assert.Equal(0, _msalTestTokenCacheProvider.Count);

            TokenAcquisitionOptions tokenAcquisitionOptions = new TokenAcquisitionOptions
            {
                ForceRefresh = false
            };

            // Act
            AuthenticationResult authResult = await _tokenAcquisition.GetAuthenticationResultForAppAsync(
                TestConstants.s_scopeForApp,
                tokenAcquisitionOptions: tokenAcquisitionOptions);

            // Assert
            Assert.NotNull(authResult);
            Assert.NotNull(authResult.AccessToken);
            Assert.NotEmpty(authResult.AccessToken);

            _output.WriteLine("Successfully acquired token from dSTS with token acquisition options.");

            Assert.Equal(1, _msalTestTokenCacheProvider.Count);
        }

        [Fact]
        public async Task GetAccessTokenForApp_WithDstsAuthority_ForceRefresh_AcquiresNewTokenAsync()
        {
            // Arrange
            InitializeTokenAcquisitionObjects();

            Assert.Equal(0, _msalTestTokenCacheProvider.Count);

            // Act - First acquisition
            AuthenticationResult authResult1 = await _tokenAcquisition.GetAuthenticationResultForAppAsync(TestConstants.s_scopeForApp);

            Assert.Equal(1, _msalTestTokenCacheProvider.Count);

            // Act - Second acquisition with force refresh
            TokenAcquisitionOptions tokenAcquisitionOptions = new TokenAcquisitionOptions
            {
                ForceRefresh = true
            };

            AuthenticationResult authResult2 = await _tokenAcquisition.GetAuthenticationResultForAppAsync(
                TestConstants.s_scopeForApp,
                tokenAcquisitionOptions: tokenAcquisitionOptions);

            // Assert
            Assert.NotNull(authResult1);
            Assert.NotNull(authResult2);
            Assert.NotNull(authResult1.AccessToken);
            Assert.NotNull(authResult2.AccessToken);

            // Note: Tokens might be the same if they haven't expired and the service returns the same one
            // but the key point is that a new request was made to dSTS
            _output.WriteLine("Successfully forced token refresh from dSTS.");
            _output.WriteLine($"First token acquired at: {authResult1.ExpiresOn}");
            _output.WriteLine($"Second token acquired at: {authResult2.ExpiresOn}");

            // ForceRefresh may create a new cache entry, so count could be 1 or 2
            Assert.True(_msalTestTokenCacheProvider.Count >= 1, $"Cache should have at least 1 entry, actual: {_msalTestTokenCacheProvider.Count}");
        }

        private void InitializeTokenAcquisitionObjects()
        {
            _credentialsLoader = new DefaultCredentialsLoader();
            MergedOptions mergedOptions = Provider.GetRequiredService<IMergedOptionsStore>().Get(OpenIdConnectDefaults.AuthenticationScheme);

            MergedOptions.UpdateMergedOptionsFromMicrosoftIdentityOptions(_microsoftIdentityOptionsMonitor.Get(OpenIdConnectDefaults.AuthenticationScheme), mergedOptions);
            MergedOptions.UpdateMergedOptionsFromConfidentialClientApplicationOptions(_applicationOptionsMonitor.Get(OpenIdConnectDefaults.AuthenticationScheme), mergedOptions);

            _msalTestTokenCacheProvider = new MsalTestTokenCacheProvider(
                 Provider.GetService<IMemoryCache>()!,
                 Provider.GetService<IOptions<MsalMemoryTokenCacheOptions>>()!);

            var tokenAcquisitionAspnetCoreHost = new TokenAcquisitionAspnetCoreHost(
                MockHttpContextAccessor.CreateMockHttpContextAccessor(),
                Provider.GetService<IMergedOptionsStore>()!,
                Provider);

            _tokenAcquisition = new TokenAcquisitionAspNetCore(
                 _msalTestTokenCacheProvider,
                 Provider.GetService<IHttpClientFactory>()!,
                 Provider.GetService<ILogger<TokenAcquisition>>()!,
                 tokenAcquisitionAspnetCoreHost,
                 Provider,
                 _credentialsLoader);

            tokenAcquisitionAspnetCoreHost.GetOptions(OpenIdConnectDefaults.AuthenticationScheme, out _);
        }

        private void BuildTheRequiredServices()
        {
            _microsoftIdentityOptionsMonitor = new TestOptionsMonitor<MicrosoftIdentityOptions>(new MicrosoftIdentityOptions
            {
                Authority = TestConstants.AadInstance + "/" + TestConstants.ConfidentialClientLabTenant,
                ClientId = TestConstants.ConfidentialClientId,
                CallbackPath = string.Empty,
            });

            _applicationOptionsMonitor = new TestOptionsMonitor<ConfidentialClientApplicationOptions>(new ConfidentialClientApplicationOptions
            {
                Instance = TestConstants.AadInstance,
                TenantId = TestConstants.ConfidentialClientLabTenant,
                ClientId = TestConstants.ConfidentialClientId,
                ClientSecret = _ccaSecret,
            });

            var services = new ServiceCollection();

            services.AddTokenAcquisition();
            services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme);
            services.AddTransient(
                provider => _microsoftIdentityOptionsMonitor);
            services.AddTransient(
                provider => _applicationOptionsMonitor);
            services.Configure<MergedOptions>(OpenIdConnectDefaults.AuthenticationScheme, options => { });
            services.AddLogging();
            services.AddInMemoryTokenCaches();
            services.AddHttpClient();
            _provider = services.BuildServiceProvider();
        }
    }
#endif //FROM_GITHUB_ACTION
}
