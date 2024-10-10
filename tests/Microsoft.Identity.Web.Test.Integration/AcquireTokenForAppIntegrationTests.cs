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
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web.Test.Common;
using Microsoft.Identity.Web.Test.Common.Mocks;
using Microsoft.Identity.Web.Test.Common.TestHelpers;
using Microsoft.Identity.Lab.Api;
using Microsoft.Identity.Web.TokenCacheProviders.InMemory;
using Xunit;
using Xunit.Abstractions;
using Microsoft.Identity.Client.Platforms.Features.DesktopOs.Kerberos;
using System.Threading;
using Microsoft.AspNetCore.Builder;

namespace Microsoft.Identity.Web.Test.Integration
{
#if !FROM_GITHUB_ACTION
    public class AcquireTokenForAppIntegrationTests
    {
        private TokenAcquisition _tokenAcquisition;
        private ServiceProvider? _provider;
        private MsalTestTokenCacheProvider _msalTestTokenCacheProvider;
        private IOptionsMonitor<MicrosoftIdentityOptions> _microsoftIdentityOptionsMonitor;
        private IOptionsMonitor<ConfidentialClientApplicationOptions> _applicationOptionsMonitor;
        private ICredentialsLoader _credentialsLoader;

        private readonly string _ccaSecret;
        private readonly ITestOutputHelper _output;

        private ServiceProvider Provider { get => _provider!;  }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public AcquireTokenForAppIntegrationTests(ITestOutputHelper output) // test set-up
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
        
        [Theory]
        [InlineData(true, Constants.Bearer)]
        [InlineData(true, "PoP")]
#pragma warning disable xUnit1012 // Null should only be used for nullable parameters
        [InlineData(false, null)]
#pragma warning restore xUnit1012 // Null should only be used for nullable parameters
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
                await _tokenAcquisition.GetAuthenticationResultForAppAsync(TestConstants.s_scopeForApp, tokenAcquisitionOptions: tokenAcquisitionOptions);

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
                await _tokenAcquisition.GetAccessTokenForAppAsync(TestConstants.s_scopeForApp);

                // Assert
                Assert.NotNull(token);
            }
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
            async Task tokenResultAsync() =>
                await _tokenAcquisition.GetAccessTokenForAppAsync(TestConstants.s_scopeForApp, tenant: metaTenant);

            ArgumentException ex = await Assert.ThrowsAsync<ArgumentException>(tokenResultAsync);
            Assert.Contains(IDWebErrorMessage.ClientCredentialTenantShouldBeTenanted, ex.Message, System.StringComparison.OrdinalIgnoreCase);

            // Act & Assert
            async Task authResultAsync() =>
                await _tokenAcquisition.GetAuthenticationResultForAppAsync(TestConstants.s_scopeForApp, tenant: metaTenant);

            ArgumentException ex2 = await Assert.ThrowsAsync<ArgumentException>(authResultAsync);
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
                await _tokenAcquisition.GetAuthenticationResultForAppAsync(TestConstants.s_scopeForApp, tenant: Constants.Consumers);

                // Assert
                Assert.NotNull(authResult);
                Assert.NotNull(authResult.AccessToken);
                Assert.Null(authResult.IdToken);
                Assert.Null(authResult.Account);
            }
            else
            {
                string token =
                    await _tokenAcquisition.GetAccessTokenForAppAsync(TestConstants.s_scopeForApp, tenant: Constants.Consumers);

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
                await _tokenAcquisition.GetAuthenticationResultForAppAsync(TestConstants.s_scopeForApp, tenant: TestConstants.ConfidentialClientLabTenant);

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
                        tenant: TestConstants.ConfidentialClientLabTenant);

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
            async Task resultAsync() =>
                await _tokenAcquisition.GetAccessTokenForAppAsync(TestConstants.s_userReadScope.First());

            ArgumentException ex = await Assert.ThrowsAsync<ArgumentException>(resultAsync);

            Assert.Contains(IDWebErrorMessage.ClientCredentialScopeParameterShouldEndInDotDefault, ex.Message, System.StringComparison.OrdinalIgnoreCase);

            // Act & Assert
            async Task authResultAsync() =>
                await _tokenAcquisition.GetAuthenticationResultForAppAsync(TestConstants.s_userReadScope.First());

            ArgumentException ex2 = await Assert.ThrowsAsync<ArgumentException>(authResultAsync);

            Assert.Contains(IDWebErrorMessage.ClientCredentialScopeParameterShouldEndInDotDefault, ex2.Message, System.StringComparison.OrdinalIgnoreCase);
            Assert.Equal(0, _msalTestTokenCacheProvider.Count);
        }

        [Fact]
        public async Task GetAccessTokenForApp_WithAnonymousController_Async()
        {
            // ASP.NET Core builder.
            var serviceCollection = WebApplication.CreateBuilder().Services;
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
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
            var tokenAcquisitionHost = services.GetRequiredService<ITokenAcquisitionHost>();

            var token = await tokenAcquisition.GetAccessTokenForAppAsync("https://graph.microsoft.com/.default");

            Assert.NotNull(token);
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
