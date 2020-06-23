// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web.Test.Common;
using Microsoft.Identity.Web.Test.Common.TestHelpers;
using Microsoft.Identity.Web.Test.LabInfrastructure;
using Microsoft.Identity.Web.TokenCacheProviders.InMemory;
using NSubstitute;
using Xunit;
using Xunit.Abstractions;
using IHttpContextAccessor = Microsoft.AspNetCore.Http.IHttpContextAccessor;

namespace Microsoft.Identity.Web.Test.Integration
{
#if !FROM_GITHUB_ACTION
    public class AcquireTokenForAppIntegrationTests
    {
        private TokenAcquisition _tokenAcquisition;
        private ServiceProvider _provider;
        private MsalTestTokenCacheProvider _msalTestTokenCacheProvider;

        private KeyVaultSecretsProvider _keyVault;
        private string _ccaSecret;
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
                await _tokenAcquisition.GetAccessTokenForAppAsync(TestConstants.s_scopesForApp).ConfigureAwait(false);

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
                await _tokenAcquisition.GetAccessTokenForAppAsync(TestConstants.s_scopesForUser).ConfigureAwait(false);

            MsalServiceException ex = await Assert.ThrowsAsync<MsalServiceException>(result).ConfigureAwait(false);

            Assert.Contains(TestConstants.InvalidScopeError, ex.Message);
            Assert.Equal(TestConstants.InvalidScope, ex.ErrorCode);
            Assert.StartsWith(TestConstants.InvalidScopeErrorcode, ex.Message);
            Assert.Equal(0, _msalTestTokenCacheProvider.Count);
        }

        private void InitializeTokenAcquisitionObjects()
        {
            IOptions<MicrosoftIdentityOptions> microsoftIdentityOptions = _provider.GetService<IOptions<MicrosoftIdentityOptions>>();
            IOptions<MsalMemoryTokenCacheOptions> tokenOptions = _provider.GetService<IOptions<MsalMemoryTokenCacheOptions>>();
            IOptions<ConfidentialClientApplicationOptions> ccOptions = _provider.GetService<IOptions<ConfidentialClientApplicationOptions>>();
            ILogger<TokenAcquisition> logger = _provider.GetService<ILogger<TokenAcquisition>>();
            IHttpClientFactory httpClientFactory = _provider.GetService<IHttpClientFactory>();

            IHttpContextAccessor httpContextAccessor = CreateMockHttpContextAccessor();

            _msalTestTokenCacheProvider = new MsalTestTokenCacheProvider(
                microsoftIdentityOptions,
                httpContextAccessor,
                _provider.GetService<IMemoryCache>(),
                tokenOptions);

            _tokenAcquisition = new TokenAcquisition(
                _msalTestTokenCacheProvider,
                httpContextAccessor,
                microsoftIdentityOptions,
                ccOptions,
                httpClientFactory,
                logger);
        }

        private static IHttpContextAccessor CreateMockHttpContextAccessor()
        {
            var mockHttpContextAccessor = Substitute.For<IHttpContextAccessor>();
            mockHttpContextAccessor.HttpContext = new DefaultHttpContext();
            mockHttpContextAccessor.HttpContext.Request.Scheme = "https";
            mockHttpContextAccessor.HttpContext.Request.Host = new HostString("IdentityDotNetSDKAutomation");
            mockHttpContextAccessor.HttpContext.Request.PathBase = "/";

            return mockHttpContextAccessor;
        }

        private void BuildTheRequiredServices()
        {
            var services = new ServiceCollection();

            services.AddTokenAcquisition();
            services.AddTransient(
                provider => Options.Create(new MicrosoftIdentityOptions
                {
                    Authority = TestConstants.AuthorityCommonTenant,
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
            string appTokenKey = clientId + "_AppTokenCache";
            Assert.True(_msalTestTokenCacheProvider.MemoryCache.TryGetValue(appTokenKey, out _));
            Assert.Equal(tokenCount, _msalTestTokenCacheProvider.Count);
        }
    }
#endif //FROM_GITHUB_ACTION
}
