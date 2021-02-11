// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
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

        [Fact]
        public async Task GetAccessTokenForApp_ReturnsAccessTokenAsync()
        {
            // Arrange
            InitializeTokenAcquisitionObjects();

            Assert.Equal(0, _msalTestTokenCacheProvider.Count);

            // Act
            string token =
                await _tokenAcquisition.GetAccessTokenForAppAsync(TestConstants.s_scopeForApp).ConfigureAwait(false);

            // Assert
            Assert.NotNull(token);

            AssertAppTokenInMemoryCache(TestConstants.ConfidentialClientId, 1);
        }

        [Theory]
        [InlineData(Constants.Organizations)]
        [InlineData(Constants.Common)]
        public async Task GetAccessTokenForApp_WithMetaTenant_ShouldThrowExceptionAsync(string metaTenant)
        {
            // Arrange
            InitializeTokenAcquisitionObjects();

            Assert.Equal(0, _msalTestTokenCacheProvider.Count);

            async Task result() =>
                await _tokenAcquisition.GetAccessTokenForAppAsync(TestConstants.s_scopeForApp, metaTenant).ConfigureAwait(false);

            ArgumentException ex = await Assert.ThrowsAsync<ArgumentException>(result).ConfigureAwait(false);
            Assert.Contains(IDWebErrorMessage.ClientCredentialTenantShouldBeTenanted, ex.Message);
            Assert.Equal(0, _msalTestTokenCacheProvider.Count);
        }

        [Fact]
        public async Task GetAccessTokenForApp_ConsumersTenantAsync()
        {
            // Arrange
            InitializeTokenAcquisitionObjects();

            Assert.Equal(0, _msalTestTokenCacheProvider.Count);

            string token =
                await _tokenAcquisition.GetAccessTokenForAppAsync(TestConstants.s_scopeForApp, Constants.Consumers).ConfigureAwait(false);

            // Assert
            Assert.NotNull(token);
            Assert.Equal(1, _msalTestTokenCacheProvider.Count);
        }

        [Fact]
        public async Task GetAccessTokenForApp_TenantSpecificAsync()
        {
            // Arrange
            InitializeTokenAcquisitionObjects();

            Assert.Equal(0, _msalTestTokenCacheProvider.Count);

            string token =
                await _tokenAcquisition.GetAccessTokenForAppAsync(
                    TestConstants.s_scopeForApp,
                    TestConstants.ConfidentialClientLabTenant).ConfigureAwait(false);

            // Assert
            Assert.NotNull(token);
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

            Assert.Contains(IDWebErrorMessage.ClientCredentialScopeParameterShouldEndInDotDefault, ex.Message);
            Assert.Equal(0, _msalTestTokenCacheProvider.Count);
        }

        private void InitializeTokenAcquisitionObjects()
        {
            _msalTestTokenCacheProvider = new MsalTestTokenCacheProvider(
                 _provider.GetService<IMemoryCache>(),
                 _provider.GetService<IOptions<MsalMemoryTokenCacheOptions>>());

            _tokenAcquisition = new TokenAcquisition(
                 _msalTestTokenCacheProvider,
                 MockHttpContextAccessor.CreateMockHttpContextAccessor(),
                 _provider.GetService<IOptions<MicrosoftIdentityOptions>>(),
                 _provider.GetService<IOptions<ConfidentialClientApplicationOptions>>(),
                 _provider.GetService<IHttpClientFactory>(),
                 _provider.GetService<ILogger<TokenAcquisition>>(),
                 _provider);
        }

        private void BuildTheRequiredServices()
        {
            var services = new ServiceCollection();

            services.AddTokenAcquisition();
            services.AddTransient(
                provider => Options.Create(new MicrosoftIdentityOptions
                {
                    Authority = TestConstants.AadInstance + "/" + TestConstants.ConfidentialClientLabTenant,
                    ClientId = TestConstants.ConfidentialClientId,
                    CallbackPath = string.Empty,
                }));
            services.AddTransient(
                provider => Options.Create(new ConfidentialClientApplicationOptions
                {
                    Instance = TestConstants.AadInstance,
                    TenantId = TestConstants.ConfidentialClientLabTenant,
                    ClientId = TestConstants.ConfidentialClientId,
                    ClientSecret = _ccaSecret,
                }));
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
