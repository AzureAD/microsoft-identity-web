// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web.Test.Common;
using Microsoft.Identity.Web.Test.Common.Mocks;
using Microsoft.Identity.Web.Test.Common.TestHelpers;
using Microsoft.Identity.Web.Test.LabInfrastructure;
using Microsoft.Identity.Web.TokenCacheProviders.InMemory;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Identity.Web.Test.Integration
{
#if !FROM_GITHUB_ACTION
    public class AcquireTokenForAppIntegrationTests
    {
        private TokenAcquisition _tokenAcquisition;
        private ServiceProvider _provider;
        private MsalTestTokenCacheProvider _msalTestTokenCacheProvider;
        private IOptionsMonitor<MicrosoftIdentityOptions> _microsoftIdentityOptionsMonitor;
        private IOptionsMonitor<ConfidentialClientApplicationOptions> _applicationOptionsMonitor;

        private readonly KeyVaultSecretsProvider _keyVault;
        private readonly string _ccaSecret;
        private readonly ITestOutputHelper _output;

        public AcquireTokenForAppIntegrationTests(ITestOutputHelper output) // test set-up
        {
            _output = output;

            _keyVault = new KeyVaultSecretsProvider();
            _ccaSecret = _keyVault.GetSecret(TestConstants.ConfidentialClientKeyVaultUri).Value;

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

        [Theory]
        [InlineData(true, Constants.Bearer)]
        [InlineData(true, "PoP")]
        [InlineData(false, null)]
        public async Task GetAccessTokenOrAuthResultForApp_ReturnsAccessTokenOrAuthResultAsync(bool getAuthResult, string authHeaderPrefix)
        {
            // Arrange
            InitializeTokenAcquisitionObjects();

            Assert.Equal(0, _msalTestTokenCacheProvider.Count);

            // Act
            if (getAuthResult)
            {
                TokenAcquisitionOptions tokenAcquisitionOptions = new TokenAcquisitionOptions();
                if (authHeaderPrefix == "PoP")
                {
                    tokenAcquisitionOptions.PoPConfiguration = new Client.AppConfig.PoPAuthenticationConfiguration(new Uri("https://localhost/foo"));
                }

                AuthenticationResult authResult =
                await _tokenAcquisition.GetAuthenticationResultForAppAsync(TestConstants.s_scopeForApp, tokenAcquisitionOptions: tokenAcquisitionOptions).ConfigureAwait(false);

                // Assert
                Assert.NotNull(authResult);
                Assert.NotNull(authResult.AccessToken);
                Assert.Contains(authHeaderPrefix, authResult.CreateAuthorizationHeader(), System.StringComparison.OrdinalIgnoreCase);
                Assert.Null(authResult.IdToken);
                Assert.Null(authResult.Account);
            }
            else
            {
                string token =
                await _tokenAcquisition.GetAccessTokenForAppAsync(TestConstants.s_scopeForApp).ConfigureAwait(false);

                // Assert
                Assert.NotNull(token);
            }

            AssertAppTokenInMemoryCache(TestConstants.ConfidentialClientId, 1);
        }

        [Theory]
        [InlineData(Constants.Organizations)]
        [InlineData(Constants.Common)]
        public async Task GetAccessTokenForAppAndAuthResultForApp_WithMetaTenant_ShouldThrowExceptionAsync(string metaTenant)
        {
            // Arrange
            InitializeTokenAcquisitionObjects();

            Assert.Equal(0, _msalTestTokenCacheProvider.Count);

            // Act & Assert
            async Task tokenResult() =>
                await _tokenAcquisition.GetAccessTokenForAppAsync(TestConstants.s_scopeForApp, tenant: metaTenant).ConfigureAwait(false);

            ArgumentException ex = await Assert.ThrowsAsync<ArgumentException>(tokenResult).ConfigureAwait(false);
            Assert.Contains(IDWebErrorMessage.ClientCredentialTenantShouldBeTenanted, ex.Message, System.StringComparison.OrdinalIgnoreCase);

            // Act & Assert
            async Task authResult() =>
                await _tokenAcquisition.GetAuthenticationResultForAppAsync(TestConstants.s_scopeForApp, tenant: metaTenant).ConfigureAwait(false);

            ArgumentException ex2 = await Assert.ThrowsAsync<ArgumentException>(authResult).ConfigureAwait(false);
            Assert.Contains(IDWebErrorMessage.ClientCredentialTenantShouldBeTenanted, ex2.Message, System.StringComparison.OrdinalIgnoreCase);

            Assert.Equal(0, _msalTestTokenCacheProvider.Count);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task GetAccessTokenOrAuthResultForApp_ConsumersTenantAsync(bool getAuthResult)
        {
            // Arrange
            InitializeTokenAcquisitionObjects();

            Assert.Equal(0, _msalTestTokenCacheProvider.Count);

            if (getAuthResult)
            {
                AuthenticationResult authResult =
                await _tokenAcquisition.GetAuthenticationResultForAppAsync(TestConstants.s_scopeForApp, tenant: Constants.Consumers).ConfigureAwait(false);

                // Assert
                Assert.NotNull(authResult);
                Assert.NotNull(authResult.AccessToken);
                Assert.Null(authResult.IdToken);
                Assert.Null(authResult.Account);
            }
            else
            {
                string token =
                    await _tokenAcquisition.GetAccessTokenForAppAsync(TestConstants.s_scopeForApp, tenant: Constants.Consumers).ConfigureAwait(false);

                // Assert
                Assert.NotNull(token);
            }

            Assert.Equal(1, _msalTestTokenCacheProvider.Count);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task GetAccessTokenOrAuthResultForApp_TenantSpecificAsync(bool getAuthResult)
        {
            // Arrange
            InitializeTokenAcquisitionObjects();

            Assert.Equal(0, _msalTestTokenCacheProvider.Count);

            if (getAuthResult)
            {
                AuthenticationResult authResult =
                await _tokenAcquisition.GetAuthenticationResultForAppAsync(TestConstants.s_scopeForApp, tenant: TestConstants.ConfidentialClientLabTenant).ConfigureAwait(false);

                // Assert
                Assert.NotNull(authResult);
                Assert.NotNull(authResult.AccessToken);
                Assert.Null(authResult.IdToken);
                Assert.Null(authResult.Account);
            }
            else
            {
                string token =
                    await _tokenAcquisition.GetAccessTokenForAppAsync(
                        TestConstants.s_scopeForApp,
                        tenant: TestConstants.ConfidentialClientLabTenant).ConfigureAwait(false);

                // Assert
                Assert.NotNull(token);
            }

            Assert.Equal(1, _msalTestTokenCacheProvider.Count);
        }

        [Fact]
        public async Task GetAccessTokenForApp_WithUserScope_MsalServiceExceptionThrownAsync()
        {
            // Arrange
            InitializeTokenAcquisitionObjects();

            // Act & Assert
            async Task result() =>
                await _tokenAcquisition.GetAccessTokenForAppAsync(TestConstants.s_userReadScope.FirstOrDefault()).ConfigureAwait(false);

            ArgumentException ex = await Assert.ThrowsAsync<ArgumentException>(result).ConfigureAwait(false);

            Assert.Contains(IDWebErrorMessage.ClientCredentialScopeParameterShouldEndInDotDefault, ex.Message, System.StringComparison.OrdinalIgnoreCase);

            // Act & Assert
            async Task authResult() =>
                await _tokenAcquisition.GetAuthenticationResultForAppAsync(TestConstants.s_userReadScope.FirstOrDefault()).ConfigureAwait(false);

            ArgumentException ex2 = await Assert.ThrowsAsync<ArgumentException>(authResult).ConfigureAwait(false);

            Assert.Contains(IDWebErrorMessage.ClientCredentialScopeParameterShouldEndInDotDefault, ex2.Message, System.StringComparison.OrdinalIgnoreCase);
            Assert.Equal(0, _msalTestTokenCacheProvider.Count);
        }

        [Fact]
        public async Task GetAccessTokenForApp_WithAnonymousController_Async()
        {
            var serviceCollection = new ServiceCollection();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    { "AzureAd:Instance", "https://login.microsoftonline.com/" },
                    { "AzureAd:TenantId", TestConstants.ConfidentialClientLabTenant },
                    { "AzureAd:ClientId", TestConstants.ConfidentialClientId },
                    { "AzureAd:ClientSecret", _ccaSecret },
                })
                .Build();
            serviceCollection.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddMicrosoftIdentityWebApi(configuration)
                .EnableTokenAcquisitionToCallDownstreamApi()
                .AddInMemoryTokenCaches();

            var services = serviceCollection.BuildServiceProvider();

            var tokenAcquisition = services.GetRequiredService<ITokenAcquisition>();

            var token = await tokenAcquisition.GetAccessTokenForAppAsync("https://graph.microsoft.com/.default").ConfigureAwait(false);

            Assert.NotNull(token);
        }

        [Fact]
        public async Task GetAccessTokenForApp_UpdateOptionsInRuntime_Async()
        {
            var serviceCollection = new ServiceCollection();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    { "AzureAd:Instance", "https://login.microsoftonline.com/" },
                    { "AzureAd:TenantId", TestConstants.ConfidentialClientLabTenant },
                    { "AzureAd:ClientId", TestConstants.ConfidentialClientId },
                    { "AzureAd:ClientSecret", _ccaSecret },
                })
                .Build();
            serviceCollection.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddMicrosoftIdentityWebApi(configuration)
                .EnableTokenAcquisitionToCallDownstreamApi()
                .AddInMemoryTokenCaches();

            var services = serviceCollection.BuildServiceProvider();

            var tokenAcquisition = services.GetRequiredService<ITokenAcquisition>();

            var token = await tokenAcquisition.GetAccessTokenForAppAsync("https://graph.microsoft.com/.default").ConfigureAwait(false);

            Assert.NotNull(token);

            var ccaOptions = services.GetRequiredService<IOptionsMonitor<ConfidentialClientApplicationOptions>>();
            ccaOptions.Get(JwtBearerDefaults.AuthenticationScheme).ClientId = "blabla";

            var token2 = await tokenAcquisition.GetAccessTokenForAppAsync("https://graph.microsoft.com/.default").ConfigureAwait(false);

        }

        private void InitializeTokenAcquisitionObjects()
        {
            MergedOptions mergedOptions = _provider.GetRequiredService<IOptionsMonitor<MergedOptions>>().Get(OpenIdConnectDefaults.AuthenticationScheme);

            MergedOptions.UpdateMergedOptionsFromMicrosoftIdentityOptions(_microsoftIdentityOptionsMonitor.Get(OpenIdConnectDefaults.AuthenticationScheme), mergedOptions);
            MergedOptions.UpdateMergedOptionsFromConfidentialClientApplicationOptions(_applicationOptionsMonitor.Get(OpenIdConnectDefaults.AuthenticationScheme), mergedOptions);

            _msalTestTokenCacheProvider = new MsalTestTokenCacheProvider(
                 _provider.GetService<IMemoryCache>(),
                 _provider.GetService<IOptions<MsalMemoryTokenCacheOptions>>());

            _tokenAcquisition = new TokenAcquisition(
                 _msalTestTokenCacheProvider,
                 MockHttpContextAccessor.CreateMockHttpContextAccessor(),
                 _provider.GetService<IOptionsMonitor<MergedOptions>>(),
                 _provider.GetService<IHttpClientFactory>(),
                 _provider.GetService<ILogger<TokenAcquisition>>(),
                 _provider);
            _tokenAcquisition.GetOptions(OpenIdConnectDefaults.AuthenticationScheme);
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

        private void AssertAppTokenInMemoryCache(string clientId, int tokenCount)
        {
            string appTokenKey = clientId + "_" + TestConstants.ConfidentialClientLabTenant + "_AppTokenCache";
            Assert.True(_msalTestTokenCacheProvider.MemoryCache.TryGetValue(appTokenKey, out _));
            Assert.Equal(tokenCount, _msalTestTokenCacheProvider.Count);
        }
    }
#endif //FROM_GITHUB_ACTION
}
