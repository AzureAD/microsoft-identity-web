// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Globalization;
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
        TokenAcquisition _tokenAcquisition;
        ServiceProvider _provider;
        private MsalTestTokenCacheProvider _msalTestTokenCacheProvider;
        private IOptions<MicrosoftIdentityOptions> microsoftIdentityOptions;

        private KeyVaultSecretsProvider _keyVault;
        private string _ccaSecret;
        private readonly ITestOutputHelper _output;

        public AcquireTokenForAppIntegrationTests(ITestOutputHelper output) // test set-up
        {
            _output = output;

            _keyVault = new KeyVaultSecretsProvider();
            _ccaSecret = _keyVault.GetSecret(TestConstants.ConfidentialClientKeyVaultUri).Value;

            if (!string.IsNullOrEmpty(_ccaSecret)) // Need the secret before building the services
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

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        [InlineData("some_secret")]
        public void ApplicationOptionsIncludeClientSecret(string clientSecret)
        {
            // Arrange
            InitializeTokenAcquisitionObjects();

            var options = new ConfidentialClientApplicationOptions
            {
                ClientSecret = clientSecret,
            };

            MicrosoftIdentityOptionsValidation microsoftIdentityOptionsValidation = new MicrosoftIdentityOptionsValidation();
            ValidateOptionsResult result = microsoftIdentityOptionsValidation.ValidateClientSecret(options);
            if (result.Failed)
            {
                string msg = string.Format(CultureInfo.InvariantCulture, "The 'ClientSecret' option must be provided.");
                Assert.Equal(msg, result.FailureMessage);
            }
            else
            {
                Assert.True(result.Succeeded);
            }
        }

        [Theory]
        [InlineData(null, false)]
        [InlineData("", false)]
        [InlineData("notAUri", false)]
        [InlineData("htt://nonhttp/", false)]
        [InlineData("https://login.microsoftonline.com/", true)]
        [InlineData("https://login.microsoftonline.com", true)]
        [InlineData("https://cats.b2clogin.com/", true)]
        [InlineData("https://cats.b2clogin.com/signout-callback-oidc", true)]
        [InlineData("http://cats.b2clogin.com/signout-callback-oidc", true)]
        public void ValidateRedirectUriFromMicrosoftIdentityOptions(
            string redirectUri,
            bool expectConfiguredUri)
        {
            string httpContextRedirectUri = "https://IdentityDotNetSDKAutomation/";

            InitializeTokenAcquisitionObjects();
            microsoftIdentityOptions.Value.RedirectUri = redirectUri;

            if (expectConfiguredUri)
            {
                Assert.Equal(microsoftIdentityOptions.Value.RedirectUri, _tokenAcquisition.CreateRedirectUri());
            }
            else
            {
                Assert.Equal(httpContextRedirectUri, _tokenAcquisition.CreateRedirectUri());
            }
        }

        private void InitializeTokenAcquisitionObjects()
        {
            microsoftIdentityOptions = _provider.GetService<IOptions<MicrosoftIdentityOptions>>();
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

        private IHttpContextAccessor CreateMockHttpContextAccessor()
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
                _provider => Options.Create(new MicrosoftIdentityOptions
                {
                    Authority = TestConstants.AuthorityCommonTenant,
                    ClientId = TestConstants.ConfidentialClientId,
                    CallbackPath = "",
                }));
            services.AddTransient(
                _provider => Options.Create(new ConfidentialClientApplicationOptions
                {
                    Instance = TestConstants.AadInstance,
                    TenantId = TestConstants.ConfidentialClientLabTenant,
                    ClientId = TestConstants.ConfidentialClientId,
                    ClientSecret = _ccaSecret,
                }
                ));
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
