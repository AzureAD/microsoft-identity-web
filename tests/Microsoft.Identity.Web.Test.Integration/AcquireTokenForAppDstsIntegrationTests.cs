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
using Microsoft.Identity.Lab.Api;
using Microsoft.Identity.Web.Test.Common;
using Microsoft.Identity.Web.Test.Common.Mocks;
using Microsoft.Identity.Web.Test.Common.TestHelpers;
using Microsoft.Identity.Web.TokenCacheProviders.InMemory;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Identity.Web.Test.Integration
{
#if !FROM_GITHUB_ACTION
    /// <summary>
    /// These tests verify that Microsoft.Identity.Web can successfully acquire tokens
    /// </summary>
    public class AcquireTokenForAppDstsIntegrationTests
    {
        private TokenAcquisition _tokenAcquisition = null!;
        private ServiceProvider? _provider;
        private MsalTestTokenCacheProvider _msalTestTokenCacheProvider = null!;
        private IOptionsMonitor<MicrosoftIdentityOptions> _microsoftIdentityOptionsMonitor = null!;
        private IOptionsMonitor<ConfidentialClientApplicationOptions> _applicationOptionsMonitor = null!;
        private ICredentialsLoader _credentialsLoader = null!;

        private readonly string _dstsAuthority = null!;
        private readonly string _dstsClientId = null!;
        private readonly CredentialDescription _dstsCredential = null!;
        private readonly string _dstsTenantId = null!;
        private readonly string _dstsScope = null!;
        private readonly ITestOutputHelper _output;

        private ServiceProvider Provider { get => _provider!; }

        public AcquireTokenForAppDstsIntegrationTests(ITestOutputHelper output)
        {
            _output = output;

            try
            {
                // Load dSTS configuration from Key Vault (ID4sKeyVault)
                KeyVaultSecretsProvider kvProvider = new KeyVaultSecretsProvider(TestConstants.ID4sKeyVaultUri);

                var dstsConfigSecret = kvProvider.GetSecretByName(TestConstants.DstsTestClientSecret);

                if (dstsConfigSecret == null || string.IsNullOrEmpty(dstsConfigSecret.Value))
                {
                    throw new InvalidOperationException(
                        $"dSTS configuration not found in Key Vault. " +
                        $"Secret name: '{TestConstants.DstsTestClientSecret}' in vault: '{TestConstants.ID4sKeyVaultName}'");
                }

                // Parse JSON using Lab API standard format
                var jsonDoc = JsonDocument.Parse(dstsConfigSecret.Value);
                var appElement = jsonDoc.RootElement.GetProperty("app");

                // Extract configuration values
                _dstsAuthority = appElement.GetProperty("authority").GetString()!;
                _dstsClientId = appElement.GetProperty("appid").GetString()!;
                _dstsTenantId = appElement.GetProperty("tenantid").GetString()!;
                _dstsScope = appElement.GetProperty("defaultscopes").GetString()!;

                // Load certificate
                _dstsCredential = CertificateDescription.FromKeyVault(
                    TestConstants.MSIDLabLabKeyVaultName,
                    "LabAuth");

                _output.WriteLine("✅ dSTS configuration and certificate loaded successfully from Key Vault");
            }
            catch (Exception ex)
            {
                _output.WriteLine($"❌ Failed to load dSTS configuration from Key Vault: {ex.Message}");
                throw new InvalidOperationException(
                    $"Failed to initialize dSTS integration tests. " +
                    $"Please ensure the secret '{TestConstants.DstsTestClientSecret}' exists in Key Vault '{TestConstants.ID4sKeyVaultName}'. " +
                    $"See README_DSTS_TESTS.md for setup instructions.",
                    ex);
            }

            BuildTheRequiredServices();
        }

        /// <summary>
        /// Test acquiring access token from dSTS for app-only scenario.
        /// This verifies the basic dSTS token acquisition flow.
        /// </summary>
        [Fact]
        public async Task GetAccessTokenForApp_FromDsts_ReturnsAccessTokenAsync()
        {
            // Arrange
            InitializeTokenAcquisitionObjects();

            Assert.Equal(0, _msalTestTokenCacheProvider.Count);

            // Act
            string token = await _tokenAcquisition.GetAccessTokenForAppAsync(_dstsScope);

            // Assert
            Assert.NotNull(token);
            Assert.NotEmpty(token);
            _output.WriteLine("✅ Successfully acquired token from dSTS");

            // Verify token is cached
            Assert.Equal(1, _msalTestTokenCacheProvider.Count);
        }

        /// <summary>
        /// Test acquiring AuthenticationResult from dSTS for app-only scenario.
        /// This verifies that the full authentication result is returned correctly.
        /// </summary>
        [Fact(Skip = "Requires dSTS configuration in Key Vault. Set 'DstsTestConfig' secret to enable.")]
        public async Task GetAuthenticationResultForApp_FromDsts_ReturnsAuthResultAsync()
        {
            // Arrange
            InitializeTokenAcquisitionObjects();

            Assert.Equal(0, _msalTestTokenCacheProvider.Count);

            // Act
            AuthenticationResult authResult = await _tokenAcquisition.GetAuthenticationResultForAppAsync(_dstsScope);

            // Assert
            Assert.NotNull(authResult);
            Assert.NotNull(authResult.AccessToken);
            Assert.NotEmpty(authResult.AccessToken);
            Assert.Null(authResult.IdToken); // App-only flow should not return ID token
            Assert.Null(authResult.Account); // App-only flow should not return account
            _output.WriteLine("✅ Successfully acquired AuthenticationResult from dSTS");

            // Verify token is cached
            Assert.Equal(1, _msalTestTokenCacheProvider.Count);
        }

        /// <summary>
        /// Test token acquisition with PoP (Proof of Possession) from dSTS.
        /// This verifies that dSTS supports PoP tokens if configured.
        /// </summary>
        [Fact(Skip = "Requires dSTS configuration in Key Vault. Set 'DstsTestConfig' secret to enable.")]
        public async Task GetAuthenticationResultForApp_FromDsts_WithPoP_ReturnsPopTokenAsync()
        {
            // Arrange
            InitializeTokenAcquisitionObjects();

            Assert.Equal(0, _msalTestTokenCacheProvider.Count);

            TokenAcquisitionOptions tokenAcquisitionOptions = new TokenAcquisitionOptions
            {
                PoPConfiguration = new Client.AppConfig.PoPAuthenticationConfiguration(new Uri("https://management.core.windows.net"))
            };

            // Act
            AuthenticationResult authResult = await _tokenAcquisition.GetAuthenticationResultForAppAsync(
                _dstsScope,
                tokenAcquisitionOptions: tokenAcquisitionOptions);

            // Assert
            Assert.NotNull(authResult);
            Assert.NotNull(authResult.AccessToken);
            Assert.Contains("PoP", authResult.CreateAuthorizationHeader(), StringComparison.OrdinalIgnoreCase);
            _output.WriteLine("✅ Successfully acquired PoP token from dSTS");

            // Verify token is cached
            Assert.Equal(1, _msalTestTokenCacheProvider.Count);
        }

        /// <summary>
        /// Test that token is retrieved from cache on subsequent calls.
        /// This verifies the token caching mechanism works with dSTS tokens.
        /// </summary>
        [Fact(Skip = "Requires dSTS configuration in Key Vault. Set 'DstsTestConfig' secret to enable.")]
        public async Task GetAccessTokenForApp_FromDsts_MultipleCallsUseCacheAsync()
        {
            // Arrange
            InitializeTokenAcquisitionObjects();

            Assert.Equal(0, _msalTestTokenCacheProvider.Count);

            // Act - First call (should acquire from dSTS)
            string token1 = await _tokenAcquisition.GetAccessTokenForAppAsync(_dstsScope);
            Assert.NotNull(token1);
            Assert.Equal(1, _msalTestTokenCacheProvider.Count);

            // Act - Second call (should retrieve from cache)
            string token2 = await _tokenAcquisition.GetAccessTokenForAppAsync(_dstsScope);

            // Assert
            Assert.NotNull(token2);
            Assert.Equal(token1, token2); // Should be same token from cache
            Assert.Equal(1, _msalTestTokenCacheProvider.Count); // Cache count should not increase
            _output.WriteLine("✅ Successfully verified token caching for dSTS tokens");
        }

        /// <summary>
        /// Test token acquisition with SAML bearer assertion from dSTS.
        /// This verifies that dSTS tokens work with the SAML bearer authorization scheme.
        /// Note: This test verifies the code path but actual SAML assertion handling
        /// is primarily in DownstreamApi when calling downstream APIs.
        /// </summary>
        [Fact(Skip = "Requires dSTS configuration in Key Vault. Set 'DstsTestConfig' secret to enable.")]
        public async Task GetAccessTokenForApp_FromDsts_SupportsTokenForDownstreamApiAsync()
        {
            // Arrange
            InitializeTokenAcquisitionObjects();

            // Act
            string token = await _tokenAcquisition.GetAccessTokenForAppAsync(_dstsScope);

            // Assert
            Assert.NotNull(token);
            Assert.NotEmpty(token);
            _output.WriteLine("✅ Token acquired for downstream API call");

            // The actual SAML bearer header logic is tested in unit tests (DownstreamApiTests)
            // This integration test verifies we can acquire tokens that would be used
            // with the SAML bearer scheme: "http://schemas.microsoft.com/dsts/saml2-bearer"
        }

        /// <summary>
        /// Test that acquiring token with invalid scope throws appropriate exception.
        /// </summary>
        [Fact(Skip = "Requires dSTS configuration in Key Vault. Set 'DstsTestConfig' secret to enable.")]
        public async Task GetAccessTokenForApp_FromDsts_WithInvalidScope_ThrowsExceptionAsync()
        {
            // Arrange
            InitializeTokenAcquisitionObjects();

            // Act & Assert
            await Assert.ThrowsAnyAsync<MsalServiceException>(async () =>
                await _tokenAcquisition.GetAccessTokenForAppAsync("invalid.scope.that.does.not.exist/.default"));
            _output.WriteLine("✅ Successfully verified exception handling for invalid scope");
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
                Authority = _dstsAuthority,
                ClientId = _dstsClientId,
                CallbackPath = string.Empty,
                ClientCredentials = new[] { _dstsCredential }, // Modern credential approach
                SendX5C = true, // Required for dSTS certificate authentication
            });

            _applicationOptionsMonitor = new TestOptionsMonitor<ConfidentialClientApplicationOptions>(new ConfidentialClientApplicationOptions
            {
                Instance = _dstsAuthority.Substring(0, _dstsAuthority.LastIndexOf('/')), // Extract instance from authority
                TenantId = _dstsTenantId,
                ClientId = _dstsClientId,
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
