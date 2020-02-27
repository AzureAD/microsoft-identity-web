// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

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
using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Identity.Web.Test.Integration
{
    public class AcquireTokenForAppIntegrationTests
    {
        TokenAcquisition _tokenAcquisition;
        ServiceProvider _provider;
        private MsalTestTokenCacheProvider _msalTestTokenCacheProvider;

        private KeyVaultSecretsProvider _keyVault;
        private string _ccaSecret;
        private readonly ITestOutputHelper _output;

        public AcquireTokenForAppIntegrationTests(ITestOutputHelper output) //test set-up
        {
            _output = output;

            _keyVault = new KeyVaultSecretsProvider();
            _ccaSecret = _keyVault.GetSecret(TestConstants.ConfidentialClientKeyVaultUri).Value;

            if (!string.IsNullOrEmpty(_ccaSecret)) //Need the secret before building the services
            {
                BuildTheRequiredServices();
            }
            else
            {
                _output.WriteLine("Connection to keyvault failed. No secret returned. ");
                throw new ArgumentNullException(nameof(_ccaSecret), "No secret returned from keyvault. ");
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
                await _tokenAcquisition.AcquireTokenForAppAsync(TestConstants.s_scopesForApp).ConfigureAwait(false);

            // Assert
            Assert.NotNull(token);

            AssertAppTokenInMemoryCache(TestConstants.ConfidentialClientId, 1);
        }

        [Fact]
        public async Task GetAccessTokenForApp_WithUserScope_MsalServiceExceptionThrownAsync()
        {
             // Arrange
            InitializeTokenAcquisitionObjects();

            // Act & Assert
            async Task result() =>
                await _tokenAcquisition.AcquireTokenForAppAsync(TestConstants.s_scopesForUser).ConfigureAwait(false);

            Exception ex = await Assert.ThrowsAsync<MsalServiceException>(result);

            Assert.StartsWith(TestConstants.InvalidScopeErrorcode, ex.Message);
            Assert.Contains(TestConstants.InvalidScopeError, ex.Message);
            Assert.Equal(0, _msalTestTokenCacheProvider.Count);
        }

        private void InitializeTokenAcquisitionObjects()
        {
            IOptions<MicrosoftIdentityOptions> microsoftIdentityOptions = _provider.GetService<IOptions<MicrosoftIdentityOptions>>();
            IOptions<MsalMemoryTokenCacheOptions> tokenOptions = _provider.GetService<IOptions<MsalMemoryTokenCacheOptions>>();
            IOptions<ConfidentialClientApplicationOptions> ccOptions = _provider.GetService<IOptions<ConfidentialClientApplicationOptions>>();
            ILogger<TokenAcquisition> logger = _provider.GetService<ILogger<TokenAcquisition>>();

            _msalTestTokenCacheProvider = new MsalTestTokenCacheProvider(
                microsoftIdentityOptions,
                new MockHttpContextAccessor(),
                _provider.GetService<IMemoryCache>(),
                tokenOptions);

            _tokenAcquisition = new TokenAcquisition(
                _msalTestTokenCacheProvider,
                new MockHttpContextAccessor(),
                microsoftIdentityOptions,
                ccOptions,
                logger);
        }

        private void BuildTheRequiredServices()
        {
            var services = new ServiceCollection();
            services.AddTokenAcquisition();
            services.AddTransient(
                _provider => Options.Create(new MicrosoftIdentityOptions
                {
                    Instance = TestConstants.AadInstance,
                    TenantId = TestConstants.ConfidentialClientLabTenant,
                    ClientId = TestConstants.ConfidentialClientId,
                    Authority = TestConstants.AuthorityCommonTenant
                }));
            services.AddTransient(
                _provider => Options.Create(new ConfidentialClientApplicationOptions
                {
                    Instance = TestConstants.AadInstance,
                    TenantId = TestConstants.ConfidentialClientLabTenant,
                    ClientId = TestConstants.ConfidentialClientId,
                    ClientSecret = _ccaSecret
                }
                ));
            services.AddLogging();
            services.AddInMemoryTokenCaches();
            _provider = services.BuildServiceProvider();
        }

        private void AssertAppTokenInMemoryCache(string clientId, int tokenCount)
        {
            string appTokenKey = clientId + "_AppTokenCache";
            Assert.True(_msalTestTokenCacheProvider.MemoryCache.TryGetValue(appTokenKey, out _));
            Assert.Equal(tokenCount, _msalTestTokenCacheProvider.Count);
        }
    }
}
