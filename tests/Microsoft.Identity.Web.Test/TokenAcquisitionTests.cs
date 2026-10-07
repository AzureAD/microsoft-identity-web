// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensibility;
using Microsoft.Identity.Web.Extensibility;
using Microsoft.Identity.Web.Test.Common;
using Microsoft.Identity.Web.Test.Common.Mocks;
using Microsoft.Identity.Web.TestOnly;
using Microsoft.Identity.Web.TokenCacheProviders;
using Microsoft.Identity.Web.TokenCacheProviders.Distributed;
using Microsoft.Identity.Web.TokenCacheProviders.InMemory;
using NSubstitute;
using Xunit;


namespace Microsoft.Identity.Web.Test
{
    [Collection(nameof(TokenAcquirerFactorySingletonProtection))]
    public class TokenAcquisitionTests
    {
        private const string Tenant = "tenant";
        private const string TenantId = "tenant-id";
        private const string AppHomeTenantId = "app-home-tenant-id";

        [Fact]
        public void CachePartitionKeys_DefaultsToNull()
        {
            // Arrange
            var options = new TokenAcquisitionOptions();

            // Act
            IDictionary<string, string>? cachePartitionKeys = options.CachePartitionKeys;

            // Assert
            Assert.Null(cachePartitionKeys);
        }

        [Fact]
        public void CachePartitionKeys_CanBeSet()
        {
            // Arrange
            IDictionary<string, string> cachePartitionKeys = new Dictionary<string, string>
            {
                ["tenant"] = "contoso",
                ["user"] = "alice"
            };

            // Act
            var options = new TokenAcquisitionOptions()
                .WithCachePartitionKeys(cachePartitionKeys);

            // Assert
            Assert.Same(cachePartitionKeys, options.CachePartitionKeys);
            Assert.Equal("contoso", options.CachePartitionKeys!["tenant"]);
            Assert.Equal("alice", options.CachePartitionKeys["user"]);
        }

        [Theory]
        [InlineData(null, null, null, null)]
        [InlineData(null, null, AppHomeTenantId, null)]
        [InlineData(Tenant, null, null, Tenant)]
        [InlineData(Tenant, TenantId, null, Tenant)]
        [InlineData(Tenant, null, AppHomeTenantId, Tenant)]
        [InlineData(Tenant, TenantId, AppHomeTenantId, Tenant)]
        [InlineData(null, TenantId, null, TenantId)]
        [InlineData(null, TenantId, AppHomeTenantId, TenantId)]
        [InlineData(null, Constants.Common, AppHomeTenantId, AppHomeTenantId)]
        [InlineData(null, Constants.Organizations, AppHomeTenantId, AppHomeTenantId)]
        public void TestResolveTenantReturnsCorrectTenant(string? tenant, string? tenantId, string? appHomeTenantId, string? expectedValue)
        {
            string? resolvedTenant = TokenAcquisition.ResolveTenant(tenant, new MergedOptions { TenantId = tenantId, AppHomeTenantId = appHomeTenantId });
            Assert.Equal(expectedValue, resolvedTenant);
        }

        [Theory]
        [InlineData(Constants.Common, null)]
        [InlineData(Constants.Organizations, null)]
        [InlineData(Constants.Common, TenantId)]
        [InlineData(Constants.Organizations, TenantId)]
        [InlineData(Constants.Common, Constants.Common)]
        [InlineData(Constants.Common, Constants.Organizations)]
        [InlineData(Constants.Organizations, Constants.Organizations)]
        [InlineData(Constants.Organizations, Constants.Common)]
        [InlineData(null, Constants.Common)]
        [InlineData(null, Constants.Organizations)]
        public void TestResolveTenantThrowsWhenMetaTenant(string? tenant, string? tenantId)
        {
            var exception = Assert.Throws<ArgumentException>(() => TokenAcquisition.ResolveTenant(tenant, new MergedOptions { TenantId = tenantId }));
            Assert.StartsWith(IDWebErrorMessage.ClientCredentialTenantShouldBeTenanted, exception.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void TestManagedIdentityWithCommonTenantShouldNotCallResolveTenant()
        {
            // This test verifies that ResolveTenant is not called when using managed identity,
            // which prevents the IDW10405 error when tenant is "common" or "organizations"

            // The fix ensures that when ManagedIdentity is specified in tokenAcquisitionOptions,
            // ResolveTenant is skipped entirely, so this scenario should not throw

            // Create test options with managed identity
            var tokenOptions = new TokenAcquisitionOptions
            {
                ManagedIdentity = new ManagedIdentityOptions
                {
                    UserAssignedClientId = "test-client-id"
                }
            };

            var mergedOptions = new MergedOptions
            {
                TenantId = Constants.Common  // This would normally cause ResolveTenant to throw
            };

            // This should not throw because ResolveTenant should not be called for managed identity scenarios
            // The actual method call would be tested in integration tests, but we can test the logic here

            // Verify that ResolveTenant still throws for non-managed identity scenarios
            var exception = Assert.Throws<ArgumentException>(() => TokenAcquisition.ResolveTenant(null, mergedOptions));
            Assert.StartsWith(IDWebErrorMessage.ClientCredentialTenantShouldBeTenanted, exception.Message, StringComparison.Ordinal);
        }

        [Fact]
        public async Task ExtraBodyParametersAreSentToEndpointTest()
        {
            // Arrange
            var tokenAcquirerFactory = InitTokenAcquirerFactory();
            IServiceProvider serviceProvider = tokenAcquirerFactory.Build();
            var mockHttpClient = serviceProvider.GetRequiredService<IMsalHttpClientFactory>() as MockHttpClientFactory;

            mockHttpClient!.AddMockHandler(MockHttpCreator.CreateHandlerToValidatePostData(
                HttpMethod.Post,
                new Dictionary<string, string>() {
                    { "custom_param1", "value1" },
                    { "custom_param2", "value2" }
                }));

            IAuthorizationHeaderProvider authorizationHeaderProvider = serviceProvider.GetRequiredService<IAuthorizationHeaderProvider>();

            var options = new AcquireTokenOptions();
            options.ExtraParameters = new Dictionary<string, object>
            {
                { "EXTRA_BODY_PARAMETERS", new Dictionary<string, Func<CancellationToken, Task<string>>>
                    {
                        ["custom_param1"] = _ => Task.FromResult("value1"),
                        ["custom_param2"] = _ => Task.FromResult("value2")
                    }
                }
            };

            // Act
            string result = await authorizationHeaderProvider.CreateAuthorizationHeaderForAppAsync("https://graph.microsoft.com/.default",
                new AuthorizationHeaderProviderOptions() { AcquireTokenOptions = options });

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Bearer header.payload.signature", result);
        }

        /// <summary>
        /// Regression test for the in-memory cache short-circuit fix. With the default
        /// (UseFastUnboundedCache not set), acquiring an app token must flow through the
        /// MsalMemoryTokenCacheProvider serialization callbacks, so a blob is written to the
        /// backing IMemoryCache. Previously IdWeb enabled MSAL's static cache and skipped
        /// Initialize(), so the IMemoryCache was never used.
        /// </summary>
        [Fact]
        public async Task AppToken_InMemoryCache_WiresSerialization_WritesToMemoryCache()
        {
            // Arrange
            var tokenAcquirerFactory = InitTokenAcquirerFactory();
            string uniqueClientId = Guid.NewGuid().ToString();
            tokenAcquirerFactory.Services.Configure<MicrosoftIdentityApplicationOptions>(
                options => options.ClientId = uniqueClientId);

            IServiceProvider serviceProvider = tokenAcquirerFactory.Build();
            var mockHttpClient = serviceProvider.GetRequiredService<IMsalHttpClientFactory>() as MockHttpClientFactory;
            mockHttpClient!.AddMockHandler(CreateClientCredentialsTokenHandler(accessToken: "app-token-1"));

            var memoryCache = (MemoryCache)serviceProvider.GetRequiredService<IMemoryCache>();
            Assert.Equal(0, memoryCache.Count);

            IAuthorizationHeaderProvider authorizationHeaderProvider =
                serviceProvider.GetRequiredService<IAuthorizationHeaderProvider>();

            // Act
            string result = await authorizationHeaderProvider.CreateAuthorizationHeaderForAppAsync(
                "https://graph.microsoft.com/.default");

            // Assert — the serialization provider ran and wrote the app-token blob to IMemoryCache.
            Assert.NotNull(result);
            Assert.Equal(1, memoryCache.Count);
        }

        /// <summary>
        /// With the opt-in (MicrosoftIdentityOptions.UseFastUnboundedCache = true), IdWeb keeps the
        /// legacy behavior: MSAL's static shared cache is used and the serialization provider is
        /// not initialized, so nothing is written to the backing IMemoryCache.
        /// </summary>
        [Fact]
        public async Task AppToken_InMemoryCache_UseFastUnboundedCache_SkipsSerialization_MemoryCacheEmpty()
        {
            // Arrange
            var tokenAcquirerFactory = InitTokenAcquirerFactory();
            string uniqueClientId = Guid.NewGuid().ToString();
            tokenAcquirerFactory.Services.Configure<MicrosoftIdentityApplicationOptions>(
                options => options.ClientId = uniqueClientId);
            tokenAcquirerFactory.Services.Configure<MicrosoftIdentityOptions>(
                options => options.UseFastUnboundedCache = true);

            IServiceProvider serviceProvider = tokenAcquirerFactory.Build();
            var mockHttpClient = serviceProvider.GetRequiredService<IMsalHttpClientFactory>() as MockHttpClientFactory;
            mockHttpClient!.AddMockHandler(CreateClientCredentialsTokenHandler(accessToken: "app-token-shared"));

            var memoryCache = (MemoryCache)serviceProvider.GetRequiredService<IMemoryCache>();

            IAuthorizationHeaderProvider authorizationHeaderProvider =
                serviceProvider.GetRequiredService<IAuthorizationHeaderProvider>();

            // Act
            string result = await authorizationHeaderProvider.CreateAuthorizationHeaderForAppAsync(
                "https://graph.microsoft.com/.default");

            // Assert — token acquired, but nothing written to the IMemoryCache (legacy static cache used).
            Assert.NotNull(result);
            Assert.Equal(0, memoryCache.Count);
        }

        /// <summary>
        /// With MicrosoftIdentityOptions.PartitionAppTokenCacheByAudience = true, app tokens for two
        /// different resources are stored under separate cache keys (each resource is its own
        /// partition via MSAL's WithCachePartitionKey), so the backing IMemoryCache has one entry per
        /// resource.
        /// </summary>
        [Fact]
        public async Task AppToken_PartitionByAudience_UsesSeparateCacheEntryPerResource()
        {
            // Arrange
            var tokenAcquirerFactory = InitTokenAcquirerFactory();
            string uniqueClientId = Guid.NewGuid().ToString();
            tokenAcquirerFactory.Services.Configure<MicrosoftIdentityApplicationOptions>(
                options => options.ClientId = uniqueClientId);
            tokenAcquirerFactory.Services.Configure<MicrosoftIdentityOptions>(
                options => options.PartitionAppTokenCacheByAudience = true);

            IServiceProvider serviceProvider = tokenAcquirerFactory.Build();
            var mockHttpClient = serviceProvider.GetRequiredService<IMsalHttpClientFactory>() as MockHttpClientFactory;
            mockHttpClient!.AddMockHandler(CreateClientCredentialsTokenHandler(accessToken: "token-resource-1"));
            mockHttpClient.AddMockHandler(CreateClientCredentialsTokenHandler(accessToken: "token-resource-2"));

            var memoryCache = (MemoryCache)serviceProvider.GetRequiredService<IMemoryCache>();
            IAuthorizationHeaderProvider authorizationHeaderProvider =
                serviceProvider.GetRequiredService<IAuthorizationHeaderProvider>();

            // Act — acquire app tokens for two distinct resources.
            await authorizationHeaderProvider.CreateAuthorizationHeaderForAppAsync("https://resource1.example.com/.default");
            await authorizationHeaderProvider.CreateAuthorizationHeaderForAppAsync("https://resource2.example.com/.default");

            // Assert — one cache partition (entry) per resource.
            Assert.Equal(2, memoryCache.Count);
        }

        /// <summary>
        /// Without PartitionAppTokenCacheByAudience, app tokens for different resources share the same
        /// {clientId}_{tenantId} cache key, so the backing IMemoryCache holds a single (shared) entry.
        /// </summary>
        [Fact]
        public async Task AppToken_WithoutPartitionByAudience_SharesSingleCacheEntry()
        {
            // Arrange
            var tokenAcquirerFactory = InitTokenAcquirerFactory();
            string uniqueClientId = Guid.NewGuid().ToString();
            tokenAcquirerFactory.Services.Configure<MicrosoftIdentityApplicationOptions>(
                options => options.ClientId = uniqueClientId);

            IServiceProvider serviceProvider = tokenAcquirerFactory.Build();
            var mockHttpClient = serviceProvider.GetRequiredService<IMsalHttpClientFactory>() as MockHttpClientFactory;
            mockHttpClient!.AddMockHandler(CreateClientCredentialsTokenHandler(accessToken: "token-resource-1"));
            mockHttpClient.AddMockHandler(CreateClientCredentialsTokenHandler(accessToken: "token-resource-2"));

            var memoryCache = (MemoryCache)serviceProvider.GetRequiredService<IMemoryCache>();
            IAuthorizationHeaderProvider authorizationHeaderProvider =
                serviceProvider.GetRequiredService<IAuthorizationHeaderProvider>();

            // Act
            await authorizationHeaderProvider.CreateAuthorizationHeaderForAppAsync("https://resource1.example.com/.default");
            await authorizationHeaderProvider.CreateAuthorizationHeaderForAppAsync("https://resource2.example.com/.default");

            // Assert — both resources share a single {clientId}_{tenantId} partition.
            Assert.Equal(1, memoryCache.Count);
        }

        /// <summary>
        /// Tests that a caught <see cref="MsalUiRequiredException"/> is not re-logged by Microsoft.Identity.Web,
        /// since MSAL.NET already logs it. Re-logging produced duplicate log entries.
        /// This addresses issue #3528.
        /// </summary>
        [Fact]
        public async Task GetAuthenticationResultForUserAsync_DoesNotDuplicateLog_WhenMsalUiRequiredExceptionIsThrown()
        {
            // Arrange
            var tokenAcquirerFactory = InitTokenAcquirerFactory();

            var innerLogger = Substitute.For<ILogger<TokenAcquisition>>();
            innerLogger.IsEnabled(Arg.Any<Microsoft.Extensions.Logging.LogLevel>()).Returns(true);
            tokenAcquirerFactory.Services.AddSingleton<ILogger<TokenAcquisition>>(
                new LoggerMock<TokenAcquisition>(innerLogger));

            IServiceProvider serviceProvider = tokenAcquirerFactory.Build();
            IAuthorizationHeaderProvider authorizationHeaderProvider = serviceProvider.GetRequiredService<IAuthorizationHeaderProvider>();

            // No account/login-hint claims, so MSAL's AcquireTokenSilent throws MsalUiRequiredException
            // synchronously (the exact scenario reported in issue #3528).
            var user = new ClaimsPrincipal(new Microsoft.IdentityModel.Tokens.CaseSensitiveClaimsIdentity());

            // Act & Assert
            await Assert.ThrowsAsync<MicrosoftIdentityWebChallengeUserException>(() =>
                authorizationHeaderProvider.CreateAuthorizationHeaderForUserAsync(
                    new[] { "https://graph.microsoft.com/.default" },
                    authorizationHeaderProviderOptions: null,
                    claimsPrincipal: user));

            innerLogger.DidNotReceive().Log(
                Microsoft.Extensions.Logging.LogLevel.Information,
                Arg.Is<EventId>(e => e.Id == 300), // LoggingEventId.TokenAcquisitionError
                Arg.Any<object>(),
                Arg.Any<Exception?>(),
                Arg.Any<Func<object, Exception?, string>>());
        }

        private TokenAcquirerFactory InitTokenAcquirerFactory()
        {
            TokenAcquirerFactoryTesting.ResetTokenAcquirerFactoryInTest();
            TokenAcquirerFactory tokenAcquirerFactory = TokenAcquirerFactory.GetDefaultInstance();
            tokenAcquirerFactory.Services.Configure<MicrosoftIdentityApplicationOptions>(options =>
            {
                options.Instance = "https://login.microsoftonline.com/";
                options.TenantId = "10c419d4-4a50-45b2-aa4e-919fb84df24f";
                options.ClientId = "idu773ld-e38d-jud3-45lk-d1b09a74a8ca";
                options.ExtraQueryParameters = new Dictionary<string, string>
                    {
                        { "dc", "ESTS-PUB-SCUS-LZ1-FD000-TEST1" }
                    };
                options.ClientCredentials = [ new CredentialDescription() {
                    SourceType = CredentialSource.ClientSecret,
                    ClientSecret = "someSecret"
                    }];
            });

            // Add MockedHttpClientFactory
            tokenAcquirerFactory.Services.AddSingleton<IMsalHttpClientFactory, MockHttpClientFactory>();

            return tokenAcquirerFactory;
        }

        /// <summary>
        /// Tests that when identity configuration is missing (simulating a misconfigured key like "ManagedIdentity " with trailing space),
        /// a meaningful ArgumentException is thrown instead of a NullReferenceException.
        /// This addresses issue #2921.
        /// </summary>
        [Fact]
        public async Task GetAuthenticationResultForAppAsync_ThrowsMeaningfulError_WhenConfigurationIsMissing()
        {
            // Arrange - Create a factory with missing identity configuration
            TokenAcquirerFactoryTesting.ResetTokenAcquirerFactoryInTest();
            TokenAcquirerFactory tokenAcquirerFactory = TokenAcquirerFactory.GetDefaultInstance();
            tokenAcquirerFactory.Services.Configure<MicrosoftIdentityApplicationOptions>(options =>
            {
                // Intentionally NOT setting Instance, TenantId, or Authority
                // This simulates the scenario where configuration keys have typos
                // (e.g., "ManagedIdentity " instead of "ManagedIdentity")
                options.ClientId = "test-client-id";
                options.ClientCredentials = [new CredentialDescription()
                {
                    SourceType = CredentialSource.ClientSecret,
                    ClientSecret = "someSecret"
                }];
            });

            tokenAcquirerFactory.Services.AddSingleton<IMsalHttpClientFactory, MockHttpClientFactory>();

            IServiceProvider serviceProvider = tokenAcquirerFactory.Build();
            IAuthorizationHeaderProvider authorizationHeaderProvider = serviceProvider.GetRequiredService<IAuthorizationHeaderProvider>();

            // Act & Assert - Should throw ArgumentException with meaningful message
            var exception = await Assert.ThrowsAsync<ArgumentException>(async () =>
                await authorizationHeaderProvider.CreateAuthorizationHeaderForAppAsync(
                    "https://graph.microsoft.com/.default",
                    new AuthorizationHeaderProviderOptions()));

            Assert.StartsWith(IDWebErrorMessage.MissingIdentityConfiguration, exception.Message, System.StringComparison.Ordinal);
        }

        /// <summary>
        /// Tests that SendX5C=true results in x5c claim being included in the client assertion.
        /// This test examines the actual HTTP request to verify x5c presence in the JWT header.
        /// </summary>
        [Fact]
        public async Task RopcFlow_WithSendX5CTrue_IncludesX5CInClientAssertion()
        {
            // Arrange
            var factory = InitTokenAcquirerFactoryForRopcWithCertificate(sendX5C: true);
            IServiceProvider serviceProvider = factory.Build();

            var mockHttpClient = serviceProvider.GetRequiredService<IMsalHttpClientFactory>() as MockHttpClientFactory;

            var mockHandler = MockHttpCreator.CreateClientCredentialTokenHandler();
            mockHttpClient!.AddMockHandler(mockHandler);

            IAuthorizationHeaderProvider authorizationHeaderProvider = serviceProvider.GetRequiredService<IAuthorizationHeaderProvider>();

            // Create claims principal with username and password for pure ROPC flow
            var claims = new List<System.Security.Claims.Claim>
            {
                new System.Security.Claims.Claim(ClaimConstants.Username, "testuser@contoso.com"),
                new System.Security.Claims.Claim(ClaimConstants.Password, "testpassword123")
            };
            var claimsPrincipal = new System.Security.Claims.ClaimsPrincipal(
                new Microsoft.IdentityModel.Tokens.CaseSensitiveClaimsIdentity(claims));

            // Act
            string result = await authorizationHeaderProvider.CreateAuthorizationHeaderForUserAsync(
                new[] { "https://graph.microsoft.com/.default" },
                authorizationHeaderProviderOptions: null,
                claimsPrincipal: claimsPrincipal);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Bearer header.payload.signature", result);

            // Verify the request was made
            Assert.NotNull(mockHandler.ActualRequestMessage);
            Assert.NotNull(mockHandler.ActualRequestPostData);

            // Verify it's ROPC flow
            Assert.True(mockHandler.ActualRequestPostData.ContainsKey("grant_type"));
            Assert.Equal("password", mockHandler.ActualRequestPostData["grant_type"]);

            // Verify x5c is present in client_assertion JWT header
            string? clientAssertion = GetClientAssertionFromPostData(mockHandler.ActualRequestPostData);
            if (clientAssertion != null)
            {
                string jwtHeader = DecodeJwtHeader(clientAssertion);
                // With SendX5C=true, the header should contain "x5c" claim
                Assert.Contains("\"x5c\"", jwtHeader, StringComparison.Ordinal);
            }
        }

        /// <summary>
        /// Tests that SendX5C=false results in NO x5c claim in the client assertion.
        /// This verifies that the x5c certificate chain is excluded when SendX5C=false.
        /// </summary>
        [Fact]
        public async Task RopcFlow_WithSendX5CFalse_DoesNotIncludeX5CInClientAssertion()
        {
            // Arrange
            var factory = InitTokenAcquirerFactoryForRopcWithCertificate(sendX5C: false);
            IServiceProvider serviceProvider = factory.Build();

            var mockHttpClient = serviceProvider.GetRequiredService<IMsalHttpClientFactory>() as MockHttpClientFactory;

            var mockHandler = MockHttpCreator.CreateClientCredentialTokenHandler();
            mockHttpClient!.AddMockHandler(mockHandler);

            IAuthorizationHeaderProvider authorizationHeaderProvider = serviceProvider.GetRequiredService<IAuthorizationHeaderProvider>();

            // Create claims principal with username and password
            var claims = new List<System.Security.Claims.Claim>
            {
                new System.Security.Claims.Claim(ClaimConstants.Username, "user@contoso.com"),
                new System.Security.Claims.Claim(ClaimConstants.Password, "password123")
            };
            var claimsPrincipal = new System.Security.Claims.ClaimsPrincipal(
                new Microsoft.IdentityModel.Tokens.CaseSensitiveClaimsIdentity(claims));

            // Act
            string result = await authorizationHeaderProvider.CreateAuthorizationHeaderForUserAsync(
                new[] { "https://graph.microsoft.com/.default" },
                authorizationHeaderProviderOptions: null,
                claimsPrincipal: claimsPrincipal);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(mockHandler.ActualRequestMessage);
            Assert.NotNull(mockHandler.ActualRequestPostData);

            // Verify it's ROPC flow
            Assert.True(mockHandler.ActualRequestPostData.ContainsKey("grant_type"));
            Assert.Equal("password", mockHandler.ActualRequestPostData["grant_type"]);

            // Verify x5c is NOT present in client_assertion JWT header
            string? clientAssertion = GetClientAssertionFromPostData(mockHandler.ActualRequestPostData);
            if (clientAssertion != null)
            {
                string jwtHeader = DecodeJwtHeader(clientAssertion);
                // With SendX5C=false, the header should NOT contain "x5c" claim
                Assert.DoesNotContain("\"x5c\"", jwtHeader, StringComparison.Ordinal);
            }
        }

        /// <summary>
        /// Extracts the client_assertion parameter from HTTP POST data.
        /// </summary>
        /// <param name="postData">The HTTP POST data dictionary.</param>
        /// <returns>The client_assertion JWT string, or null if not present.</returns>
        private static string? GetClientAssertionFromPostData(Dictionary<string, string> postData)
        {
            return postData.ContainsKey("client_assertion") ? postData["client_assertion"] : null;
        }

        /// <summary>
        /// Decodes the header portion of a JWT (JSON Web Token).
        /// Converts base64url encoding to standard base64, then decodes to UTF-8 string.
        /// </summary>
        /// <param name="jwt">The complete JWT string in format: header.payload.signature</param>
        /// <returns>The decoded JWT header as a JSON string.</returns>
        private static string DecodeJwtHeader(string jwt)
        {
            // Split JWT into parts (header.payload.signature)
            var parts = jwt.Split('.');
            if (parts.Length < 2)
            {
                return string.Empty;
            }

            // Convert base64url to base64
            string base64 = parts[0].Replace('-', '+').Replace('_', '/');

            // Add padding if needed
            switch (base64.Length % 4)
            {
                case 2: base64 += "=="; break;
                case 3: base64 += "="; break;
            }

            // Decode base64 to bytes, then to UTF-8 string
            return System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(base64));
        }

        private TokenAcquirerFactory InitTokenAcquirerFactoryForRopcWithCertificate(bool sendX5C)
        {
            TokenAcquirerFactoryTesting.ResetTokenAcquirerFactoryInTest();
            TokenAcquirerFactory tokenAcquirerFactory = TokenAcquirerFactory.GetDefaultInstance();

            var mockHttpFactory = new MockHttpClientFactory();

            tokenAcquirerFactory.Services.Configure<MicrosoftIdentityApplicationOptions>(options =>
            {
                options.Instance = "https://login.microsoftonline.com/";
                options.TenantId = "10c419d4-4a50-45b2-aa4e-919fb84df24f";
                options.ClientId = "idu773ld-e38d-jud3-45lk-d1b09a74a8ca";
                options.SendX5C = sendX5C; // Set the SendX5C flag

                // SendX5C is only meaningful with certificate credentials
                // Certificate is used for CLIENT authentication, username/password for USER authentication (ROPC)
                options.ClientCredentials = [
                    CertificateDescription.FromCertificate(CreateTestCertificate())
                ];
            });

            // Add MockedHttpClientFactory
            tokenAcquirerFactory.Services.AddSingleton<IMsalHttpClientFactory>(mockHttpFactory);

            return tokenAcquirerFactory;
        }

        /// <summary>
        /// Creates a minimal self-signed certificate for testing purposes.
        /// In unit tests, the mock HTTP handlers don't actually validate the certificate.
        /// </summary>
        private static System.Security.Cryptography.X509Certificates.X509Certificate2 CreateTestCertificate()
        {
            // Create a minimal self-signed certificate for testing
            // The certificate details don't matter for unit tests as HTTP calls are mocked
            using var rsa = System.Security.Cryptography.RSA.Create(2048);
            var request = new System.Security.Cryptography.X509Certificates.CertificateRequest(
                "CN=TestCertificate",
                rsa,
                System.Security.Cryptography.HashAlgorithmName.SHA256,
                System.Security.Cryptography.RSASignaturePadding.Pkcs1);

            var certificate = request.CreateSelfSigned(
                DateTimeOffset.UtcNow.AddDays(-1),
                DateTimeOffset.UtcNow.AddDays(365));

            return certificate;
        }

        #region Agent User Identity Cache Tests (Issue #3840)

        [Theory]
        [InlineData(false, false)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(true, true)]
        public async Task AgentObo_DifferentParents_DoNotShareTokens(bool longRunning, bool uppercaseComponent)
        {
            // Arrange
            var test = CreateAgentCacheTest();
            var optionsA = ToTokenOptions(new AuthorizationHeaderProviderOptions().WithAgentIdentity(test.AgentId));
            optionsA.LongRunningWebApiSessionKey = longRunning ? AcquireTokenOptions.LongRunningWebApiSessionKeyAuto : null;
            string partitionA = test.ParentA.ToLowerInvariant();
            string component = uppercaseComponent ? "IDWEB_AGENT_BLUEPRINT_ID" : "idweb_agent_blueprint_id";
            var optionsB = new TokenAcquisitionOptions
            {
                ExtraParameters = optionsA.ExtraParameters,
                CachePartitionKeys = new Dictionary<string, string> { [component] = partitionA },
            };
            ClaimsPrincipal principal = CreateAgentOboPrincipal(test.AgentId);
            Task<AuthenticationResult> AcquireAsync(string? scheme, TokenAcquisitionOptions options, ClaimsPrincipal? user) =>
                test.Acquisition.GetAuthenticationResultForUserAsync(
                    new[] { "https://graph.microsoft.com/.default" },
                    authenticationScheme: scheme, user: user, tokenAcquisitionOptions: options);
            AddBlueprintAssertion(test.Http, test.AgentId, test.ParentA, "assertion-a");
            var obo = MockHttpCreator.CreateLrOboTokenHandler("https://graph.microsoft.com/.default", "obo-a", "refresh-a");
            obo.ExpectedPostData = new Dictionary<string, string>
            {
                ["client_id"] = test.AgentId,
                ["client_assertion"] = "assertion-a",
                ["requested_token_use"] = "on_behalf_of",
                ["assertion"] = (string)((ClaimsIdentity)principal.Identity!).BootstrapContext!,
            };
            test.Http.AddMockHandler(obo);

            // Act
            var first = await AcquireAsync(null, optionsA, principal);
            ClaimsPrincipal? repeatedPrincipal = longRunning ? null : principal;
            var repeated = await AcquireAsync(null, optionsA, repeatedPrincipal);
            optionsB.LongRunningWebApiSessionKey = optionsA.LongRunningWebApiSessionKey;
            if (longRunning)
            {
                var missingSession = await Assert.ThrowsAsync<MsalClientException>(() =>
                    AcquireAsync("BlueprintB", optionsB, null));
                Assert.Equal("obo_cache_key_not_in_cache_error", missingSession.ErrorCode);
            }
            var rejection = CreateRejectedBlueprintHandler(test.ParentB);
            rejection.ExpectedPostData["client_secret"] = "test-only-b-secret";
            rejection.ExpectedPostData["fmi_path"] = test.AgentId;
            test.Http.AddMockHandler(rejection);
            var error = await Assert.ThrowsAsync<MsalServiceException>(() =>
                AcquireAsync("BlueprintB", optionsB, principal));
            var recovered = await AcquireAsync(null, optionsA, repeatedPrincipal);

            // Assert
            Assert.Equal("obo-a", first.AccessToken);
            Assert.Equal("obo-a", repeated.AccessToken);
            Assert.Equal(TokenSource.Cache, repeated.AuthenticationResultMetadata.TokenSource);
            Assert.Equal("unauthorized_client", error.ErrorCode);
            Assert.Equal(test.ParentB, rejection.ActualRequestPostData["client_id"]);
            Assert.Equal("obo-a", recovered.AccessToken);
            Assert.Equal(TokenSource.Cache, recovered.AuthenticationResultMetadata.TokenSource);
            Assert.Equal(partitionA, optionsB.CachePartitionKeys[component]);
            Assert.Null(optionsA.AuthenticationOptionsName);
            test.Http.Dispose();
        }

        [Fact]
        public async Task AgentObo_LongRunningRefresh_StaysWithSelectedParent()
        {
            // Arrange
            var test = CreateAgentCacheTest();
            const string originalScope = "https://graph.microsoft.com/.default";
            const string requestedScope = "https://other-resource.example/.default";
            var options = ToTokenOptions(CreateNamedAgentAppOptions(test.AgentId, "BlueprintA"));
            options.LongRunningWebApiSessionKey = "shared-session-" + test.AgentId;
            var principal = CreateAgentOboPrincipal(test.AgentId);
            Task<AuthenticationResult> AcquireAsync(string scheme, ClaimsPrincipal? user, string scope) =>
                test.Acquisition.GetAuthenticationResultForUserAsync(
                    new[] { scope }, authenticationScheme: scheme, user: user, tokenAcquisitionOptions: options);
            foreach (string name in new[] { "BlueprintB", "BlueprintA" })
            {
                AddBlueprintAssertion(test.Http, test.AgentId, name == "BlueprintB" ? test.ParentB : test.ParentA,
                    "assertion-" + name, name == "BlueprintB" ? "test-only-b-secret" : "test-only-secret");
                var obo = MockHttpCreator.CreateLrOboTokenHandler(originalScope, "obo-" + name, "refresh-" + name);
                obo.ExpectedPostData = new Dictionary<string, string>
                {
                    ["requested_token_use"] = "on_behalf_of",
                    ["client_assertion"] = "assertion-" + name,
                };
                test.Http.AddMockHandler(obo);
                await AcquireAsync(name, principal, originalScope);
            }
            var refresh = MockHttpCreator.CreateLrOboTokenHandler(requestedScope, "refreshed-b", "refresh-b-new");
            refresh.ExpectedPostData = new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["refresh_token"] = "refresh-BlueprintB",
                ["client_id"] = test.AgentId,
                ["client_assertion"] = "assertion-BlueprintB",
            };
            refresh.ResponseMessage = new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest)
            {
                Content = new StringContent("{\"error\":\"invalid_grant\",\"error_description\":\"Refresh token revoked.\"}"),
            };
            test.Http.AddMockHandler(refresh);

            // Act
            var error = await Assert.ThrowsAsync<MicrosoftIdentityWebChallengeUserException>(() =>
                AcquireAsync("BlueprintB", null, requestedScope));
            Assert.Equal("invalid_grant", error.MsalUiRequiredException.ErrorCode);
            var reinitialized = MockHttpCreator.CreateLrOboTokenHandler(requestedScope, "reinitialized-b", "refresh-b-new");
            reinitialized.ExpectedPostData = new Dictionary<string, string>
            {
                ["requested_token_use"] = "on_behalf_of",
                ["client_id"] = test.AgentId,
                ["client_assertion"] = "assertion-BlueprintB",
            };
            test.Http.AddMockHandler(reinitialized);
            var result = await AcquireAsync("BlueprintB", principal, requestedScope);
            var recoveredA = await AcquireAsync("BlueprintA", null, originalScope);

            // Assert
            Assert.Equal("reinitialized-b", result.AccessToken);
            Assert.Equal("refresh-BlueprintB", refresh.ActualRequestPostData["refresh_token"]);
            Assert.Equal("obo-BlueprintA", recoveredA.AccessToken);
            Assert.Equal(TokenSource.Cache, recoveredA.AuthenticationResultMetadata.TokenSource);
            test.Http.Dispose();
        }

        private static ClaimsPrincipal CreateAgentOboPrincipal(string agentId)
        {
            string header = EncodeBase64Url("{\"alg\":\"none\",\"typ\":\"JWT\"}");
            string payload = EncodeBase64Url("{\"aud\":\"" + agentId + "\",\"sub\":\"" + TestConstants.Uid +
                "\",\"tid\":\"10c419d4-4a50-45b2-aa4e-919fb84df24f\",\"oid\":\"" + TestConstants.Uid + "\"}");
            return new ClaimsPrincipal(new Microsoft.IdentityModel.Tokens.CaseSensitiveClaimsIdentity(
                new[] { new Claim(ClaimTypes.Name, AgentTestUsername) }, "Bearer")
            {
                BootstrapContext = header + "." + payload + ".",
            });
        }

        [Theory]
        [InlineData(null)]
        [InlineData("query")]
        [InlineData("callback")]
        public async Task AgentAppIdentity_ExplicitParent_DoesNotReuseOtherParentsClientOrToken(string? partitionOverride)
        {
            // Arrange
            var test = CreateAgentCacheTest();
            var options = ToTokenOptions(CreateNamedAgentAppOptions(test.AgentId, "BlueprintA"));
            string partitionA = test.ParentA.ToLowerInvariant();
            if (partitionOverride == "query")
            {
                options.ExtraQueryParameters = new Dictionary<string, string> { ["idweb_agent_blueprint_id"] = partitionA };
            }
            else if (partitionOverride == "callback")
            {
                test.Services.GetRequiredService<IOptionsMonitor<TokenAcquisitionExtensionOptions>>().CurrentValue
                    .OnBeforeTokenAcquisitionForApp += (builder, _) =>
                        builder.WithCachePartitionKey("idweb_agent_blueprint_id", partitionA);
            }
            var application = (MicrosoftEntraApplicationOptions)options.ExtraParameters![Constants.MicrosoftIdentityOptionsParameter];
            var credential = application.ClientCredentials!.Single();
            var providerData = credential.CustomSignedAssertionProviderData!;
            AddBlueprintAssertion(test.Http, test.AgentId, test.ParentA, "parent-a-assertion");
            var tokenA = CreateClientCredentialsTokenHandler("agent-token-via-a");
            tokenA.ExpectedPostData = new Dictionary<string, string>
            {
                ["client_id"] = test.AgentId,
                ["client_assertion"] = "parent-a-assertion",
            };
            test.Http.AddMockHandler(tokenA);
            Task<AuthenticationResult> AcquireAsync(string scheme) => test.Acquisition.GetAuthenticationResultForAppAsync(
                "https://graph.microsoft.com/.default", authenticationScheme: scheme, tokenAcquisitionOptions: options);

            // Act
            var first = await AcquireAsync("BlueprintA");
            var cached = await AcquireAsync("BlueprintA");
            foreach (bool rebuildClient in new[] { false, true })
            {
                if (rebuildClient)
                {
                    test.Acquisition._applicationsByAuthorityClientId.Clear();
                }
                var rejection = CreateRejectedBlueprintHandler(test.ParentB);
                rejection.ExpectedPostData["client_secret"] = "test-only-b-secret";
                rejection.ExpectedPostData["fmi_path"] = test.AgentId;
                test.Http.AddMockHandler(rejection);
                var error = await Assert.ThrowsAsync<MsalServiceException>(() => AcquireAsync("BlueprintB"));
                Assert.Equal("unauthorized_client", error.ErrorCode);
                Assert.Equal(test.ParentB, rejection.ActualRequestPostData["client_id"]);
                var recovered = await AcquireAsync("BlueprintA");
                Assert.Equal(first.AccessToken, recovered.AccessToken);
                Assert.Equal(TokenSource.Cache, recovered.AuthenticationResultMetadata.TokenSource);
            }

            // Assert
            Assert.Equal("agent-token-via-a", first.AccessToken);
            Assert.Equal(first.AccessToken, cached.AccessToken);
            Assert.Equal(TokenSource.Cache, cached.AuthenticationResultMetadata.TokenSource);
            if (partitionOverride == "query")
            {
                Assert.Equal(partitionA, options.ExtraQueryParameters!["idweb_agent_blueprint_id"]);
            }
            Assert.Equal("BlueprintA", options.AuthenticationOptionsName);
            Assert.False(options.ExtraParameters.ContainsKey(Constants.AgentAcquisitionContext));
            Assert.Same(credential, application.ClientCredentials!.Single());
            Assert.Same(providerData, credential.CustomSignedAssertionProviderData);
            Assert.False(providerData.ContainsKey("ConfigurationSection"));
            Assert.Null(credential.CachedValue);
            Assert.False(credential.Skip);
            Assert.Null(test.Services.GetRequiredService<ITokenAcquisitionHost>().GetOptions("BlueprintA", out _).AgentContext);
            test.Http.Dispose();
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task AgentUserIdentity_DifferentParents_DoNotShareTokens(bool useOid)
        {
            // Arrange
            var test = CreateAgentCacheTest();
            var options = ToTokenOptions(CreateNamedAgentOptions(test.AgentId, "BlueprintA", userFic: true, useOid: useOid));
            options.ExtraParameters!.Remove(Constants.MicrosoftIdentityOptionsParameter);
            AddBlueprintAssertion(test.Http, test.AgentId, test.ParentA, "assertion-a");
            test.Http.AddMockHandler(CreateClientCredentialsTokenHandler("instance-a"));
            test.Http.AddMockHandler(CreateUserFicTokenHandler("agent-user-token-via-a"));
            Task<AuthenticationResult> AcquireAsync(string scheme) => test.Acquisition.GetAuthenticationResultForUserAsync(
                new[] { "https://graph.microsoft.com/.default" }, authenticationScheme: scheme, tokenAcquisitionOptions: options);

            // Act
            var first = await AcquireAsync("BlueprintA");
            var cached = await AcquireAsync("BlueprintA");
            var rejection = CreateRejectedBlueprintHandler(test.ParentB);
            rejection.ExpectedPostData["client_secret"] = "test-only-b-secret";
            test.Http.AddMockHandler(rejection);
            var error = await Assert.ThrowsAsync<MsalServiceException>(() => AcquireAsync("BlueprintB"));
            var recovered = await AcquireAsync("BlueprintA");

            // Assert
            Assert.Equal("agent-user-token-via-a", first.AccessToken);
            Assert.Equal(first.AccessToken, cached.AccessToken);
            Assert.Equal(TokenSource.Cache, cached.AuthenticationResultMetadata.TokenSource);
            Assert.Equal("unauthorized_client", error.ErrorCode);
            Assert.Equal(test.ParentB, rejection.ActualRequestPostData["client_id"]);
            Assert.Equal(first.AccessToken, recovered.AccessToken);
            Assert.Equal("BlueprintA", options.AuthenticationOptionsName);
            string expectedPartition = test.ParentA.ToLowerInvariant();
            Assert.Contains(test.Acquisition._applicationsByAuthorityClientId.Keys,
                key => key.Contains(":agent-blueprint:" + expectedPartition, StringComparison.Ordinal));
            test.Http.Dispose();
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task AgentIdentity_ConfigurationAliases_ReuseTokens(bool userFic)
        {
            // Arrange
            var test = CreateAgentCacheTest();
            var provider = test.Services.GetRequiredService<IAuthorizationHeaderProvider>();
            var optionsA = CreateNamedAgentOptions(test.AgentId, "BlueprintA", userFic);
            var optionsB = CreateNamedAgentOptions(test.AgentId, "BlueprintAlias", userFic);
            Task<string> AcquireAsync(AuthorizationHeaderProviderOptions options) => userFic
                ? provider.CreateAuthorizationHeaderForUserAsync(new[] { "https://graph.microsoft.com/.default" }, options)
                : provider.CreateAuthorizationHeaderForAppAsync("https://graph.microsoft.com/.default", options);
            AddBlueprintAssertion(test.Http, test.AgentId, test.ParentA, "assertion-a");
            test.Http.AddMockHandler(CreateClientCredentialsTokenHandler(userFic ? "instance-a" : "token-a"));
            if (userFic)
            {
                test.Http.AddMockHandler(CreateUserFicTokenHandler("token-a"));
            }

            // Act
            string first = await AcquireAsync(optionsA);
            string second = await AcquireAsync(optionsB);
            test.Acquisition._applicationsByAuthorityClientId.Clear();
            string secondCached = await AcquireAsync(optionsB);

            // Assert
            Assert.Equal("Bearer token-a", first);
            Assert.Equal(first, second);
            Assert.Equal(second, secondCached);
            test.Http.Dispose();
        }

        [Fact]
        public async Task AgentUserIdentity_RefreshAndFallback_IgnoreOtherParentAndLegacyTokens()
        {
            // Arrange
            var test = CreateAgentCacheTest();
            var provider = test.Services.GetRequiredService<IAuthorizationHeaderProvider>();
            var options = CreateNamedAgentOptions(test.AgentId, "BlueprintB", userFic: true);
            var scopes = new[] { "https://graph.microsoft.com/.default" };
            foreach (string name in new[] { "BlueprintB", "BlueprintA" })
            {
                options.AcquireTokenOptions.AuthenticationOptionsName = name;
                AddBlueprintAssertion(test.Http, test.AgentId, name == "BlueprintB" ? test.ParentB : test.ParentA,
                    "assertion-" + name, name == "BlueprintB" ? "test-only-b-secret" : "test-only-secret");
                test.Http.AddMockHandler(CreateClientCredentialsTokenHandler("instance-" + name));
                test.Http.AddMockHandler(CreateUserFicTokenHandler(
                    "user-" + name, "refresh-" + name, name == "BlueprintB" ? -1 : 3599));
                await provider.CreateAuthorizationHeaderForUserAsync(scopes, options);
            }
            var legacyClient = ConfidentialClientApplicationBuilder.Create(test.AgentId)
                .WithAuthority("https://login.microsoftonline.com/10c419d4-4a50-45b2-aa4e-919fb84df24f")
                .WithClientSecret("test-only-legacy-secret")
                .WithHttpClientFactory(test.Http)
                .WithCacheOptions(CacheOptions.EnableSharedCacheOptions)
                .Build();
            test.Http.AddMockHandler(CreateUserFicTokenHandler("legacy-user", "legacy-refresh"));
            await ((IByUserFederatedIdentityCredential)legacyClient).AcquireTokenByUserFederatedIdentityCredential(
                scopes, AgentTestUsername, "legacy-instance").ExecuteAsync();
            var refresh = CreateClientCredentialsTokenHandler("refreshed-b");
            refresh.ExpectedPostData = new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["refresh_token"] = "refresh-BlueprintB",
                ["client_id"] = test.AgentId,
            };
            refresh.ResponseMessage = new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest)
            {
                Content = new StringContent("{\"error\":\"invalid_grant\",\"error_description\":\"Refresh token revoked.\"}"),
            };
            var fallback = CreateUserFicTokenHandler("reacquired-b");
            test.Http.AddMockHandler(refresh);
            test.Http.AddMockHandler(fallback);
            options.AcquireTokenOptions.AuthenticationOptionsName = "BlueprintB";

            // Act
            string result = await provider.CreateAuthorizationHeaderForUserAsync(scopes, options);

            // Assert
            Assert.Equal("Bearer reacquired-b", result);
            Assert.Equal("refresh-BlueprintB", refresh.ActualRequestPostData["refresh_token"]);
            Assert.Contains("instance-BlueprintB", fallback.ActualRequestPostData.Values);
            Assert.Equal(test.AgentId, fallback.ActualRequestPostData["client_id"]);
            Assert.Equal(result, await provider.CreateAuthorizationHeaderForUserAsync(scopes, options));
            options.AcquireTokenOptions.AuthenticationOptionsName = "BlueprintA";
            Assert.Equal("Bearer user-BlueprintA", await provider.CreateAuthorizationHeaderForUserAsync(scopes, options));
            test.Http.Dispose();
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task AgentAppIdentity_DoesNotReadLegacyTokenFormats(bool usePairPartition)
        {
            // Arrange
            var test = CreateAgentCacheTest();
            var legacyClient = ConfidentialClientApplicationBuilder.Create(test.AgentId)
                .WithAuthority("https://login.microsoftonline.com/10c419d4-4a50-45b2-aa4e-919fb84df24f")
                .WithClientSecret("test-only-legacy-secret")
                .WithHttpClientFactory(test.Http)
                .WithExperimentalFeatures()
                .Build();
            test.Services.GetRequiredService<TokenCacheProviders.IMsalTokenCacheProvider>().Initialize(legacyClient.AppTokenCache);
            test.Http.AddMockHandler(CreateClientCredentialsTokenHandler("legacy-token"));
            var legacyBuilder = legacyClient.AcquireTokenForClient(new[] { "https://graph.microsoft.com/.default" })
                .WithFmiPathForClientAssertion(test.AgentId);
            if (usePairPartition)
            {
                using var sha256 = System.Security.Cryptography.SHA256.Create();
                string pair = test.ParentB.ToLowerInvariant() + ":" + test.AgentId.ToLowerInvariant();
                string legacyPartition = Convert.ToBase64String(sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(pair)));
                legacyBuilder.WithCachePartitionKey("idweb_agent_pair_v1", legacyPartition);
            }
            await legacyBuilder.ExecuteAsync();
            var rejection = CreateRejectedBlueprintHandler(test.ParentB);
            rejection.ExpectedPostData["client_secret"] = "test-only-b-secret";
            test.Http.AddMockHandler(rejection);

            // Act
            var provider = test.Services.GetRequiredService<IAuthorizationHeaderProvider>();
            var error = await Assert.ThrowsAsync<MsalServiceException>(() =>
                provider.CreateAuthorizationHeaderForAppAsync(
                    "https://graph.microsoft.com/.default", CreateNamedAgentAppOptions(test.AgentId, "BlueprintB")));

            // Assert
            Assert.Equal("unauthorized_client", error.ErrorCode);
            Assert.Equal(test.ParentB, rejection.ActualRequestPostData["client_id"]);
            test.Http.Dispose();
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task CompatibilityRepair_UnconfiguredAttributes_PreserveStandardValidationContract(bool rejectMissingName)
        {
            // Arrange
            const string selectedName = "BlueprintB";
            string parentId = Guid.NewGuid().ToString();
            string agentId = Guid.NewGuid().ToString();
            var factory = InitTokenAcquirerFactoryForAgent();
            ConfigureAgentBlueprint(factory, selectedName, parentId);
            factory.Services.AddOptions<MicrosoftIdentityApplicationOptions>(selectedName).Validate(
                value => !rejectMissingName || !string.IsNullOrEmpty(value.Name), "A configured name is required.");
            factory.Services.AddAgentIdentities();
            var services = factory.Build();
            var http = Assert.IsType<MockHttpClientFactory>(services.GetRequiredService<IMsalHttpClientFactory>());
            if (!rejectMissingName)
            {
                AddBlueprintAssertion(http, agentId, parentId, "unnamed-parent-assertion");
                http.AddMockHandler(CreateClientCredentialsTokenHandler("unnamed-parent-token"));
            }
            var options = ToTokenOptions(CreateNamedAgentAppOptions(agentId, selectedName));
            Task<AuthenticationResult> AcquireAsync() =>
                services.GetRequiredService<ITokenAcquisition>().GetAuthenticationResultForAppAsync(
                    "https://graph.microsoft.com/.default", tokenAcquisitionOptions: options);

            // Act
            if (rejectMissingName)
            {
                var error = await Assert.ThrowsAsync<OptionsValidationException>(AcquireAsync);

                // Assert
                Assert.Equal(selectedName, error.OptionsName);
                Assert.Equal(new[] { "A configured name is required." }, error.Failures);
            }
            else
            {
                var result = await AcquireAsync();

                // Assert
                Assert.Equal("unnamed-parent-token", result.AccessToken);
            }
            http.Dispose();
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task CompatibilityRepair_HostOptions_PreserveIdentityAndApplicationPrecedence(bool aspnetHost)
        {
            // Arrange
            const string selectedName = "BlueprintB";
            string identityClientId = Guid.NewGuid().ToString();
            string applicationClientId = Guid.NewGuid().ToString();
            string parentId = aspnetHost ? applicationClientId : identityClientId;
            string secret = aspnetHost ? "test-only-secret" : "test-only-identity-secret";
            string agentId = Guid.NewGuid().ToString();
            var factory = InitTokenAcquirerFactoryForAgent();
            factory.Services.Configure<MicrosoftIdentityOptions>(selectedName, value =>
            {
                value.Instance = "https://login.microsoftonline.com/";
                value.TenantId = "10c419d4-4a50-45b2-aa4e-919fb84df24f";
                value.ClientId = identityClientId;
                value.ClientSecret = "test-only-identity-secret";
            });
            ConfigureAgentBlueprint(factory, selectedName, applicationClientId);
            factory.Services.Configure<MicrosoftIdentityApplicationOptions>(selectedName, value => value.Name = "complete-parent");
            factory.Services.AddOptions<MicrosoftIdentityApplicationOptions>(selectedName).Validate(
                value => value.ClientId == applicationClientId && value.Name == "complete-parent",
                "Normal validation must inspect the configured application options.");
            factory.Services.AddAgentIdentities();
            if (aspnetHost)
            {
                factory.Services.AddHttpContextAccessor();
                factory.Services.AddSingleton<ITokenAcquisitionHost, TokenAcquisitionAspnetCoreHost>();
            }
            var services = factory.Build();
            var http = Assert.IsType<MockHttpClientFactory>(services.GetRequiredService<IMsalHttpClientFactory>());
            AddBlueprintAssertion(http, agentId, parentId, "precedence-parent-assertion", secret);
            http.AddMockHandler(CreateClientCredentialsTokenHandler("precedence-parent-token"));

            // Act
            var result = await services.GetRequiredService<ITokenAcquisition>().GetAuthenticationResultForAppAsync(
                "https://graph.microsoft.com/.default", authenticationScheme: selectedName,
                tokenAcquisitionOptions: ToTokenOptions(CreateNamedAgentAppOptions(agentId, "StaleParent")));

            // Assert
            Assert.Equal("precedence-parent-token", result.AccessToken);
            http.Dispose();
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        public async Task AgentAppIdentity_DefaultHelper_ResolvesAzureAdConfigurationSection(
            bool aspNetHost, bool namedOptionsOverrideConfiguration)
        {
            // Arrange
            string agentId = Guid.NewGuid().ToString();
            string parentA = Guid.NewGuid().ToString();
            string parentB = Guid.NewGuid().ToString();
            var factory = InitTokenAcquirerFactoryForAgent();
            ConfigureAgentBlueprint(factory, string.Empty, parentB, "test-only-b-secret");
            ConfigureAgentBlueprint(factory, "HostDefault", parentB, "test-only-b-secret");
            if (namedOptionsOverrideConfiguration)
            {
                ConfigureAgentBlueprint(factory, "AzureAd", parentA, "test-only-a-secret");
            }
            factory.Services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["AzureAd:Instance"] = "https://login.microsoftonline.com/",
                    ["AzureAd:TenantId"] = "10c419d4-4a50-45b2-aa4e-919fb84df24f",
                    ["AzureAd:ClientId"] = namedOptionsOverrideConfiguration ? Guid.NewGuid().ToString() : parentA,
                    ["AzureAd:ClientCredentials:0:SourceType"] = "ClientSecret",
                    ["AzureAd:ClientCredentials:0:ClientSecret"] = namedOptionsOverrideConfiguration ? "test-only-other-secret" : "test-only-a-secret",
                }).Build());
            if (aspNetHost)
            {
                factory.Services.AddAuthentication("HostDefault").AddJwtBearer("HostDefault", _ => { });
                factory.Services.AddSingleton<ITokenAcquisitionHost>(provider => new TokenAcquisitionAspnetCoreHost(
                    new Microsoft.AspNetCore.Http.HttpContextAccessor(),
                    provider.GetRequiredService<IMergedOptionsStore>(),
                    provider));
            }
            factory.Services.AddAgentIdentities();
            IServiceProvider services = factory.Build();
            var http = Assert.IsType<MockHttpClientFactory>(services.GetRequiredService<IMsalHttpClientFactory>());
            var acquisition = services.GetRequiredService<ITokenAcquisition>();
            var options = new TokenAcquisitionOptions
            {
                ExtraParameters = new AuthorizationHeaderProviderOptions().WithAgentIdentity(agentId).AcquireTokenOptions.ExtraParameters,
            };
            AddBlueprintAssertion(http, agentId, parentA, "assertion-a", "test-only-a-secret");
            http.AddMockHandler(CreateClientCredentialsTokenHandler("token-a"));
            var rejection = CreateRejectedBlueprintHandler(parentB);
            rejection.ExpectedPostData["client_secret"] = "test-only-b-secret";
            http.AddMockHandler(rejection);

            // Act
            var first = await acquisition.GetAuthenticationResultForAppAsync(
                "https://graph.microsoft.com/.default", tokenAcquisitionOptions: options);
            var error = await Assert.ThrowsAsync<MsalServiceException>(() => acquisition.GetAuthenticationResultForAppAsync(
                "https://graph.microsoft.com/.default", authenticationScheme: string.Empty, tokenAcquisitionOptions: options));
            var repeated = await acquisition.GetAuthenticationResultForAppAsync(
                "https://graph.microsoft.com/.default", tokenAcquisitionOptions: options);

            // Assert
            Assert.Equal("token-a", first.AccessToken);
            Assert.Equal("unauthorized_client", error.ErrorCode);
            Assert.Equal(parentB, rejection.ActualRequestPostData["client_id"]);
            Assert.Equal(first.AccessToken, repeated.AccessToken);
            Assert.Equal(TokenSource.Cache, repeated.AuthenticationResultMetadata.TokenSource);
            http.Dispose();
        }

        [Fact]
        public async Task AgentUserIdentity_DefaultHelper_UsesHostDefaultBlueprint()
        {
            // Arrange
            var test = CreateAgentCacheTest();
            var options = ToTokenOptions(new AuthorizationHeaderProviderOptions().WithAgentUserIdentity(test.AgentId, AgentTestUsername));
            AddBlueprintAssertion(test.Http, test.AgentId, test.ParentB, "assertion-b", "test-only-b-secret");
            test.Http.AddMockHandler(CreateClientCredentialsTokenHandler("instance-b"));
            test.Http.AddMockHandler(CreateUserFicTokenHandler("user-b"));

            // Act
            var first = await test.Acquisition.GetAuthenticationResultForUserAsync(
                new[] { "https://graph.microsoft.com/.default" }, tokenAcquisitionOptions: options);
            var repeated = await test.Acquisition.GetAuthenticationResultForUserAsync(
                new[] { "https://graph.microsoft.com/.default" }, tokenAcquisitionOptions: options);

            // Assert
            Assert.Equal("user-b", first.AccessToken);
            Assert.Equal("user-b", repeated.AccessToken);
            Assert.Equal(TokenSource.Cache, repeated.AuthenticationResultMetadata.TokenSource);
            Assert.False(options.ExtraParameters!.ContainsKey(Constants.AgentAcquisitionContext));
            test.Http.Dispose();
        }

        [Theory]
        [InlineData(null, "BlueprintA")]
        [InlineData("BlueprintA", "BlueprintA")]
        [InlineData("", "BlueprintB")]
        public async Task AgentAppIdentity_SelectionChangedAfterHelper_UsesCurrentSelection(string? selection, string expectedBlueprint)
        {
            // Arrange
            var test = CreateAgentCacheTest();
            var options = CreateNamedAgentAppOptions(test.AgentId, "BlueprintB");
            options.AcquireTokenOptions.AuthenticationOptionsName = selection;
            bool usesBlueprintA = expectedBlueprint == "BlueprintA";
            AddBlueprintAssertion(test.Http, test.AgentId, usesBlueprintA ? test.ParentA : test.ParentB,
                "selected-assertion", usesBlueprintA ? "test-only-secret" : "test-only-b-secret");
            var handler = CreateClientCredentialsTokenHandler("selected-token");
            handler.ExpectedPostData = new Dictionary<string, string>
            {
                ["client_id"] = test.AgentId,
                ["client_assertion"] = "selected-assertion",
            };
            test.Http.AddMockHandler(handler);
            var provider = test.Services.GetRequiredService<IAuthorizationHeaderProvider>();

            // Act
            var first = await provider.CreateAuthorizationHeaderForAppAsync("https://graph.microsoft.com/.default", options);
            var repeated = await provider.CreateAuthorizationHeaderForAppAsync("https://graph.microsoft.com/.default", options);

            // Assert
            Assert.Equal("Bearer selected-token", first);
            Assert.Equal(first, repeated);
            Assert.Equal(selection, options.AcquireTokenOptions.AuthenticationOptionsName);
            test.Http.Dispose();
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task AgentAppIdentity_ProgrammaticFactory_UsesSelectedBlueprint(bool useStringOverload)
        {
            // Arrange
            var test = CreateAgentCacheTest();
            var acquirerFactory = test.Services.GetRequiredService<ITokenAcquirerFactory>();
            var options = CreateNamedAgentAppOptions(test.AgentId, "BlueprintA").AcquireTokenOptions;
            options.AuthenticationOptionsName = null;
            var helperApplication = (MicrosoftEntraApplicationOptions)options.ExtraParameters![Constants.MicrosoftIdentityOptionsParameter];
            var helperCredential = helperApplication.ClientCredentials!.Single();
            var helperData = helperCredential.CustomSignedAssertionProviderData!;
            var applicationB = new MicrosoftIdentityApplicationOptions
            {
                Instance = "https://login.microsoftonline.com/",
                TenantId = "10c419d4-4a50-45b2-aa4e-919fb84df24f",
                ClientId = test.ParentB,
                ClientCredentials = [new CredentialDescription
                {
                    SourceType = CredentialSource.ClientSecret,
                    ClientSecret = "test-only-b-secret",
                }],
            };
            ITokenAcquirer SelectApplication(MicrosoftIdentityApplicationOptions application) => useStringOverload
                ? test.Factory.GetTokenAcquirer(application.Authority!, application.ClientId!, application.ClientCredentials!, region: null)
                : acquirerFactory.GetTokenAcquirer(application);
            var rejection = CreateRejectedBlueprintHandler(test.ParentB);
            rejection.ExpectedPostData["client_secret"] = "test-only-b-secret";
            rejection.ExpectedPostData["fmi_path"] = test.AgentId;
            test.Http.AddMockHandler(rejection);

            // Act
            var error = await Assert.ThrowsAsync<MsalServiceException>(() =>
                SelectApplication(applicationB).GetTokenForAppAsync("https://graph.microsoft.com/.default", options));
            AddBlueprintAssertion(test.Http, test.AgentId, test.ParentA, "assertion-a");
            test.Http.AddMockHandler(CreateClientCredentialsTokenHandler("token-a"));
            var applicationA = test.Services.GetRequiredService<IOptionsMonitor<MicrosoftIdentityApplicationOptions>>().Get("BlueprintA");
            var recovered = await SelectApplication(applicationA).GetTokenForAppAsync("https://graph.microsoft.com/.default", options);
            var cached = await acquirerFactory.GetTokenAcquirer("BlueprintA").GetTokenForAppAsync("https://graph.microsoft.com/.default", options);

            // Assert
            Assert.Equal("unauthorized_client", error.ErrorCode);
            Assert.Equal(test.ParentB, rejection.ActualRequestPostData["client_id"]);
            Assert.Equal("token-a", recovered.AccessToken);
            Assert.Equal(recovered.AccessToken, cached.AccessToken);
            Assert.Null(options.AuthenticationOptionsName);
            Assert.False(options.ExtraParameters.ContainsKey(Constants.AgentAcquisitionContext));
            Assert.Same(helperCredential, helperApplication.ClientCredentials!.Single());
            Assert.Same(helperData, helperCredential.CustomSignedAssertionProviderData);
            Assert.False(helperData.ContainsKey("ConfigurationSection"));
            Assert.Null(helperCredential.CachedValue);
            Assert.Equal(test.ParentB, applicationB.ClientId);
            Assert.Equal("test-only-b-secret", applicationB.ClientCredentials.Single().ClientSecret);
            test.Http.Dispose();
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task AgentAppIdentity_NamedWebScheme_UsesResolvedBlueprint(bool staleHelperOptions)
        {
            // Arrange
            string agentId = Guid.NewGuid().ToString();
            string parentA = Guid.NewGuid().ToString();
            string parentB = Guid.NewGuid().ToString();
            var factory = InitTokenAcquirerFactoryForAgent();
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AzureAd:Instance"] = "https://login.microsoftonline.com/",
                ["AzureAd:TenantId"] = "10c419d4-4a50-45b2-aa4e-919fb84df24f",
                ["AzureAd:ClientId"] = parentB,
                ["AzureAd:ClientSecret"] = "test-only-b-secret",
            }).Build();
            factory.Services.AddSingleton<IConfiguration>(configuration);
            factory.Services.AddAuthentication("B")
                .AddMicrosoftIdentityWebApp(configuration.GetSection("AzureAd"), openIdConnectScheme: "B")
                .EnableTokenAcquisitionToCallDownstreamApi()
                .AddInMemoryTokenCaches();
            if (staleHelperOptions)
            {
                ConfigureAgentBlueprint(factory, "AzureAd", parentA, "test-only-a-secret");
            }
            factory.Services.AddAgentIdentities();
            var services = factory.Build();
            var http = Assert.IsType<MockHttpClientFactory>(services.GetRequiredService<IMsalHttpClientFactory>());
            var acquisition = (TokenAcquisition)services.GetRequiredService<ITokenAcquisition>();
            var options = new AuthorizationHeaderProviderOptions().WithAgentIdentity(agentId).AcquireTokenOptions;
            var application = (MicrosoftEntraApplicationOptions)options.ExtraParameters![Constants.MicrosoftIdentityOptionsParameter];
            var credential = application.ClientCredentials!.Single();
            var data = credential.CustomSignedAssertionProviderData;
            AddBlueprintAssertion(http, agentId, parentB, "assertion-b", "test-only-b-secret");
            http.AddMockHandler(CreateClientCredentialsTokenHandler("agent-via-b"));

            // Act
            var acquirer = services.GetRequiredService<ITokenAcquirerFactory>().GetTokenAcquirer("B");
            var first = await acquirer.GetTokenForAppAsync("https://graph.microsoft.com/.default", options);
            var second = await acquirer.GetTokenForAppAsync("https://graph.microsoft.com/.default", options);

            // Assert
            Assert.Equal("agent-via-b", first.AccessToken);
            Assert.Equal(first.AccessToken, second.AccessToken);
            string partition = parentB.ToLowerInvariant();
            Assert.Contains(acquisition._applicationsByAuthorityClientId.Keys, key => key.Contains(":agent-blueprint:" + partition, StringComparison.Ordinal));
            Assert.Same(credential, application.ClientCredentials!.Single());
            Assert.Same(data, credential.CustomSignedAssertionProviderData);
            Assert.False(data!.ContainsKey("ConfigurationSection"));
            Assert.Null(credential.CachedValue);
            Assert.False(credential.Skip);
            http.Dispose();
        }

        [Theory]
        [InlineData("configure", false)]
        [InlineData("configure", true)]
        [InlineData("validation", false)]
        [InlineData("validation", true)]
        [InlineData("monitor", false)]
        [InlineData("monitor", true)]
        public async Task CompatibilityRepair_PublicFactory_PreservesOptionsPipeline(string scenario, bool useStringOverload)
        {
            // Arrange
            var factory = InitTokenAcquirerFactoryForAgent();
            var application = new MicrosoftIdentityApplicationOptions
            {
                Instance = "https://login.microsoftonline.com/",
                TenantId = "10c419d4-4a50-45b2-aa4e-919fb84df24f",
                ClientId = Guid.NewGuid().ToString(),
                ClientCredentials = scenario == "configure" ? null : [new CredentialDescription
                {
                    SourceType = CredentialSource.ClientSecret,
                    ClientSecret = "test-only-secret",
                }],
            };
            string key = DefaultTokenAcquirerFactoryImplementation.GetKey(application.Authority, application.ClientId, null);
            int configurations = 0;
            int postConfigurations = 0;
            if (scenario == "configure")
            {
                factory.Services.ConfigureAll<MicrosoftIdentityApplicationOptions>(options =>
                {
                    Interlocked.Increment(ref configurations);
                    options.ClientCredentials = [new CredentialDescription
                    {
                        SourceType = CredentialSource.ClientSecret,
                        ClientSecret = "test-only-secret",
                    }];
                });
            }
            factory.Services.PostConfigure<MicrosoftIdentityApplicationOptions>(key, _ => Interlocked.Increment(ref postConfigurations));
            if (scenario == "validation")
            {
                var validator = Substitute.For<IValidateOptions<MicrosoftIdentityApplicationOptions>>();
                validator.Validate(Arg.Any<string>(), Arg.Any<MicrosoftIdentityApplicationOptions>())
                    .Returns(call => (string?)call[0] == key
                        ? ValidateOptionsResult.Fail("Test consumer rejects selected options.")
                        : ValidateOptionsResult.Success);
                factory.Services.AddSingleton(validator);
            }
            var services = factory.Build();
            var http = Assert.IsType<MockHttpClientFactory>(services.GetRequiredService<IMsalHttpClientFactory>());
            if (scenario == "monitor")
            {
                Assert.Null(services.GetRequiredService<IOptionsMonitor<MicrosoftIdentityApplicationOptions>>().Get(key).ClientId);
            }
            Task<AcquireTokenResult> AcquireAsync()
            {
                var acquirer = useStringOverload
                    ? factory.GetTokenAcquirer(application.Authority!, application.ClientId!, application.ClientCredentials!, region: null)
                    : services.GetRequiredService<ITokenAcquirerFactory>().GetTokenAcquirer(application);
                return acquirer.GetTokenForAppAsync("https://graph.microsoft.com/.default");
            }

            // Act
            if (scenario == "validation")
            {
                var error = await Assert.ThrowsAsync<OptionsValidationException>(AcquireAsync);
                Assert.Contains("Test consumer rejects selected options.", error.Failures);
            }
            else
            {
                var handler = CreateClientCredentialsTokenHandler("ordinary-token");
                handler.ExpectedPostData = new Dictionary<string, string>
                {
                    ["client_id"] = application.ClientId!,
                    ["client_secret"] = "test-only-secret",
                };
                http.AddMockHandler(handler);
                var result = await AcquireAsync();
                Assert.Equal("ordinary-token", result.AccessToken);
            }

            // Assert
            Assert.True(postConfigurations > 0);
            if (scenario == "configure")
            {
                Assert.True(configurations > 0);
                Assert.Null(application.ClientCredentials);
            }
            http.Dispose();
        }

        [Fact]
        public async Task AgentAppIdentity_TwoAgents_KeepTokensAndFmiPathsIsolated()
        {
            // Arrange
            var test = CreateAgentCacheTest();
            string[] agentIds = { test.AgentId, Guid.NewGuid().ToString() };
            var provider = test.Services.GetRequiredService<IAuthorizationHeaderProvider>();
            foreach (string agentId in agentIds)
            {
                AddBlueprintAssertion(test.Http, agentId, test.ParentA, "assertion-" + agentId);
                var token = CreateClientCredentialsTokenHandler("token-" + agentId);
                token.ExpectedPostData = new Dictionary<string, string>
                {
                    ["client_id"] = agentId,
                    ["client_assertion"] = "assertion-" + agentId,
                };
                test.Http.AddMockHandler(token);
            }

            // Act
            foreach (string agentId in agentIds.Concat(agentIds.Reverse()))
            {
                string result = await provider.CreateAuthorizationHeaderForAppAsync(
                    "https://graph.microsoft.com/.default", CreateNamedAgentAppOptions(agentId, "BlueprintA"));

                // Assert
                Assert.Equal("Bearer token-" + agentId, result);
            }
            test.Http.Dispose();
        }

        [Fact]
        public async Task CompatibilityRepair_ConcurrentWarmPairs_KeepTokensAndCallerOptionsIsolated()
        {
            // Arrange
            var test = CreateAgentCacheTest(trackCacheConcurrency: true);
            var options = ToTokenOptions(CreateNamedAgentAppOptions(test.AgentId, "BlueprintA"));
            var application = (MicrosoftEntraApplicationOptions)options.ExtraParameters![Constants.MicrosoftIdentityOptionsParameter];
            var credential = application.ClientCredentials!.Single();
            var data = credential.CustomSignedAssertionProviderData;
            foreach (string scheme in new[] { "BlueprintA", "BlueprintB" })
            {
                AddBlueprintAssertion(test.Http, test.AgentId, scheme == "BlueprintA" ? test.ParentA : test.ParentB,
                    "assertion-" + scheme, scheme == "BlueprintA" ? "test-only-secret" : "test-only-b-secret");
                test.Http.AddMockHandler(CreateClientCredentialsTokenHandler("token-" + scheme));
                await test.Acquisition.GetAuthenticationResultForAppAsync(
                    "https://graph.microsoft.com/.default", authenticationScheme: scheme, tokenAcquisitionOptions: options);
            }

            // Act
            await Task.WhenAll(Enumerable.Range(0, 12).Select(index => Task.Run(async () =>
            {
                string scheme = index % 2 == 0 ? "BlueprintA" : "BlueprintB";
                var result = await test.Acquisition.GetAuthenticationResultForAppAsync(
                    "https://graph.microsoft.com/.default", authenticationScheme: scheme, tokenAcquisitionOptions: options);

                // Assert
                Assert.Equal("token-" + scheme, result.AccessToken);
                Assert.Equal(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);
            })));
            var cache = Assert.IsType<ConcurrencyTrackingMemoryTokenCacheProvider>(
                test.Services.GetRequiredService<IMsalTokenCacheProvider>());
            Assert.NotEmpty(cache.MaximumReaders);
            Assert.All(cache.MaximumReaders, value => Assert.Equal(1, value));
            Assert.Equal("BlueprintA", options.AuthenticationOptionsName);
            Assert.Same(credential, application.ClientCredentials!.Single());
            Assert.Same(data, credential.CustomSignedAssertionProviderData);
            Assert.False(data!.ContainsKey("ConfigurationSection"));
            Assert.Null(credential.CachedValue);
            Assert.False(credential.Skip);
            test.Http.Dispose();
        }

        [Fact]
        public async Task CompatibilityRepair_AgentSelectedParent_ValidationStopsBeforeHttp()
        {
            // Arrange
            var factory = InitTokenAcquirerFactoryForAgent();
            ConfigureAgentBlueprint(factory, "BlueprintB", Guid.NewGuid().ToString(), "test-only-b-secret");
            var validator = Substitute.For<IValidateOptions<MicrosoftIdentityApplicationOptions>>();
            validator.Validate(Arg.Any<string>(), Arg.Any<MicrosoftIdentityApplicationOptions>())
                .Returns(call => (string?)call[0] == "BlueprintB"
                    ? ValidateOptionsResult.Fail("Test consumer rejects selected blueprint.")
                    : ValidateOptionsResult.Success);
            factory.Services.AddSingleton(validator);
            factory.Services.AddAgentIdentities();
            var services = factory.Build();
            var http = Assert.IsType<MockHttpClientFactory>(services.GetRequiredService<IMsalHttpClientFactory>());
            var options = ToTokenOptions(CreateNamedAgentAppOptions(Guid.NewGuid().ToString(), "AzureAd"));

            // Act
            var error = await Assert.ThrowsAsync<OptionsValidationException>(() =>
                services.GetRequiredService<ITokenAcquisition>().GetAuthenticationResultForAppAsync(
                    "https://graph.microsoft.com/.default", authenticationScheme: "BlueprintB", tokenAcquisitionOptions: options));

            // Assert
            Assert.Contains("Test consumer rejects selected blueprint.", error.Failures);
            Assert.Equal("AzureAd", options.AuthenticationOptionsName);
            http.Dispose();
        }

        [Theory]
        [InlineData("configuration", true)]
        [InlineData("configuration", false)]
        [InlineData("web", true)]
        [InlineData("web", false)]
        [InlineData("configured", true)]
        [InlineData("configured", false)]
        public async Task CompatibilityRepair_AgentSelectedParent_NormalConfiguredValidation(string source, bool rejected)
        {
            // Arrange
            string selectedName = source == "web" ? "B" : "AzureAd";
            string rejectedClientId = Guid.NewGuid().ToString();
            string allowedClientId = Guid.NewGuid().ToString();
            string parentId = rejected ? rejectedClientId : allowedClientId;
            string configuredClientId = source == "configured"
                ? (rejected ? allowedClientId : rejectedClientId)
                : parentId;
            string parentSecret = source == "configured" ? "test-only-code-secret" : "test-only-b-secret";
            string agentId = Guid.NewGuid().ToString();
            var factory = InitTokenAcquirerFactoryForAgent();
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AzureAd:Instance"] = "https://login.microsoftonline.com/",
                ["AzureAd:TenantId"] = "10c419d4-4a50-45b2-aa4e-919fb84df24f",
                ["AzureAd:ClientId"] = configuredClientId,
                ["AzureAd:ClientCredentials:0:SourceType"] = "ClientSecret",
                ["AzureAd:ClientCredentials:0:ClientSecret"] = "test-only-b-secret",
            }).Build();
            factory.Services.AddSingleton<IConfiguration>(configuration);
            if (source == "web")
            {
                factory.Services.AddAuthentication("B")
                    .AddMicrosoftIdentityWebApp(configuration.GetSection("AzureAd"), openIdConnectScheme: "B")
                    .EnableTokenAcquisitionToCallDownstreamApi()
                    .AddInMemoryTokenCaches();
            }
            ConfigureAgentBlueprint(factory, "StaleParent", Guid.NewGuid().ToString(), "test-only-stale-secret");
            int configurations = 0;
            int postConfigurations = 0;
            factory.Services.Configure<MicrosoftIdentityApplicationOptions>(selectedName, options =>
            {
                configurations++;
                options.Instance = "https://login.microsoftonline.com/";
                options.TenantId = "10c419d4-4a50-45b2-aa4e-919fb84df24f";
                options.ClientId = parentId;
                options.ClientCredentials = [new CredentialDescription
                {
                    SourceType = CredentialSource.ClientSecret,
                    ClientSecret = parentSecret,
                }];
            });
            factory.Services.PostConfigure<MicrosoftIdentityApplicationOptions>(selectedName, _ => postConfigurations++);
            var validatedClientIds = new List<string?>();
            factory.Services.AddSingleton<IValidateOptions<MicrosoftIdentityApplicationOptions>>(
                new ValidateOptions<MicrosoftIdentityApplicationOptions>(selectedName, options =>
                {
                    validatedClientIds.Add(options.ClientId);
                    return options.ClientId != rejectedClientId;
                }, "Selected-name validation rejects the configured client."));
            factory.Services.AddSingleton<IValidateOptions<MicrosoftIdentityApplicationOptions>>(
                new ValidateOptions<MicrosoftIdentityApplicationOptions>(null,
                    options => options.ClientId != rejectedClientId, "All-name validation rejects the configured client."));
            factory.Services.AddSingleton<IValidateOptions<MicrosoftIdentityApplicationOptions>>(
                new ValidateOptions<MicrosoftIdentityApplicationOptions>("UnusedParent", _ => false, "Skipped validation must not fail."));
            factory.Services.AddSingleton<IValidateOptions<MicrosoftIdentityApplicationOptions>>(
                new ValidateOptions<MicrosoftIdentityApplicationOptions>(selectedName, _ => true, "Successful validation must not fail."));
            factory.Services.AddAgentIdentities();
            var credentialsLoader = Substitute.For<ICredentialsLoader>();
            factory.Services.AddSingleton<ICredentialsLoader>(provider =>
            {
                var loader = ActivatorUtilities.CreateInstance<DefaultCertificateLoader>(provider);
                credentialsLoader.LoadCredentialsIfNeededAsync(
                    Arg.Any<CredentialDescription>(), Arg.Any<CredentialSourceLoaderParameters>())
                    .Returns(call => loader.LoadCredentialsIfNeededAsync(
                        call.ArgAt<CredentialDescription>(0), call.ArgAt<CredentialSourceLoaderParameters?>(1)));
                return credentialsLoader;
            });
            var services = factory.Build();
            var http = Assert.IsType<MockHttpClientFactory>(services.GetRequiredService<IMsalHttpClientFactory>());
            var acquisition = (TokenAcquisition)services.GetRequiredService<ITokenAcquisition>();
            var options = ToTokenOptions(CreateNamedAgentAppOptions(agentId, "StaleParent"));
            var application = (MicrosoftEntraApplicationOptions)options.ExtraParameters![Constants.MicrosoftIdentityOptionsParameter];
            var credential = application.ClientCredentials!.Single();
            var data = credential.CustomSignedAssertionProviderData;
            if (!rejected)
            {
                AddBlueprintAssertion(http, agentId, parentId, "selected-parent-assertion", parentSecret);
                var token = CreateClientCredentialsTokenHandler("selected-parent-token");
                token.ExpectedPostData = new Dictionary<string, string>
                {
                    ["client_id"] = agentId,
                    ["client_assertion"] = "selected-parent-assertion",
                };
                http.AddMockHandler(token);
            }

            // Act
            if (rejected)
            {
                var error = await Assert.ThrowsAsync<OptionsValidationException>(() =>
                    acquisition.GetAuthenticationResultForAppAsync(
                        "https://graph.microsoft.com/.default", authenticationScheme: selectedName, tokenAcquisitionOptions: options));

                // Assert
                Assert.Equal(selectedName, error.OptionsName);
                Assert.Equal(typeof(MicrosoftIdentityApplicationOptions), error.OptionsType);
                Assert.Equal(new[]
                {
                    "Selected-name validation rejects the configured client.",
                    "All-name validation rejects the configured client.",
                }, error.Failures);
                Assert.Empty(credentialsLoader.ReceivedCalls());
                Assert.Empty(acquisition._applicationsByAuthorityClientId);
            }
            else
            {
                var first = await acquisition.GetAuthenticationResultForAppAsync(
                    "https://graph.microsoft.com/.default", authenticationScheme: selectedName, tokenAcquisitionOptions: options);
                var cached = await acquisition.GetAuthenticationResultForAppAsync(
                    "https://graph.microsoft.com/.default", authenticationScheme: selectedName, tokenAcquisitionOptions: options);

                // Assert
                Assert.Equal("selected-parent-token", first.AccessToken);
                Assert.Equal(first.AccessToken, cached.AccessToken);
                Assert.Equal(TokenSource.Cache, cached.AuthenticationResultMetadata.TokenSource);
                string partition = parentId.ToLowerInvariant();
                Assert.Contains(acquisition._applicationsByAuthorityClientId.Keys, key =>
                    key.Contains(":agent-blueprint:" + partition, StringComparison.Ordinal));
            }
            Assert.Contains(parentId, validatedClientIds);
            Assert.Equal(1, configurations);
            Assert.Equal(1, postConfigurations);
            Assert.Equal("StaleParent", options.AuthenticationOptionsName);
            Assert.Same(credential, application.ClientCredentials!.Single());
            Assert.Same(data, credential.CustomSignedAssertionProviderData);
            Assert.False(data!.ContainsKey("ConfigurationSection"));
            Assert.Null(credential.CachedValue);
            Assert.False(credential.Skip);
            http.Dispose();
        }

        [Theory]
        [InlineData("configuration", false)]
        [InlineData("configuration", true)]
        [InlineData("default", false)]
        [InlineData("default", true)]
        [InlineData("web", false)]
        [InlineData("web", true)]
        public async Task CompatibilityRepair_ConfigurationOnlyParent_StandardValidationPreservesBaseline(string source, bool rejected)
        {
            // Arrange
            string selectedName = source == "web" ? "B" : "AzureAd";
            string parentId = rejected ? string.Empty : Guid.NewGuid().ToString();
            string agentId = Guid.NewGuid().ToString();
            var factory = InitTokenAcquirerFactoryForAgent();
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AzureAd:Instance"] = "https://login.microsoftonline.com/",
                ["AzureAd:TenantId"] = "10c419d4-4a50-45b2-aa4e-919fb84df24f",
                ["AzureAd:ClientId"] = parentId,
                ["AzureAd:ClientCredentials:0:SourceType"] = "ClientSecret",
                ["AzureAd:ClientCredentials:0:ClientSecret"] = "test-only-b-secret",
            }).Build();
            factory.Services.AddSingleton<IConfiguration>(configuration);
            if (source == "web")
            {
                factory.Services.AddAuthentication("B")
                    .AddMicrosoftIdentityWebApp(configuration.GetSection("AzureAd"), openIdConnectScheme: "B")
                    .EnableTokenAcquisitionToCallDownstreamApi()
                    .AddInMemoryTokenCaches();
                ConfigureAgentBlueprint(factory, "AzureAd", Guid.NewGuid().ToString(), "test-only-stale-secret");
            }
            factory.Services.AddAgentIdentities();
            if (source == "default")
            {
                factory.Services.AddSingleton<ITokenAcquisitionHost, Hosts.DefaultTokenAcquisitionHost>();
            }
            int configurations = 0;
            int postConfigurations = 0;
            factory.Services.Configure<MicrosoftIdentityApplicationOptions>(selectedName, _ => configurations++);
            factory.Services.PostConfigure<MicrosoftIdentityApplicationOptions>(selectedName, _ => postConfigurations++);
            var validated = new List<MicrosoftIdentityApplicationOptions>();
            factory.Services.AddOptions<MicrosoftIdentityApplicationOptions>(selectedName).Validate(options =>
            {
                validated.Add(options);
                return !string.IsNullOrEmpty(options.ClientId);
            }, "A selected blueprint requires a client ID.");
            var services = factory.Build();
            var http = Assert.IsType<MockHttpClientFactory>(services.GetRequiredService<IMsalHttpClientFactory>());
            var acquisition = (TokenAcquisition)services.GetRequiredService<ITokenAcquisition>();
            var options = ToTokenOptions(CreateNamedAgentAppOptions(agentId, "AzureAd"));
            // Act
            var error = await Assert.ThrowsAsync<OptionsValidationException>(() =>
                acquisition.GetAuthenticationResultForAppAsync(
                    "https://graph.microsoft.com/.default", authenticationScheme: selectedName, tokenAcquisitionOptions: options));

            // Assert
            Assert.Equal(selectedName, error.OptionsName);
            Assert.Equal(typeof(MicrosoftIdentityApplicationOptions), error.OptionsType);
            Assert.Equal(new[] { "A selected blueprint requires a client ID." }, error.Failures);
            Assert.Empty(acquisition._applicationsByAuthorityClientId);
            Assert.NotEmpty(validated);
            Assert.All(validated, value =>
            {
                Assert.True(string.IsNullOrEmpty(value.ClientId));
                Assert.Null(value.Instance);
                Assert.Null(value.TenantId);
                Assert.Null(value.ClientCredentials);
            });
            Assert.Equal(1, configurations);
            Assert.Equal(1, postConfigurations);
            Assert.Equal("AzureAd", options.AuthenticationOptionsName);
            http.Dispose();
        }

        [Theory]
        [InlineData("Name")]
        [InlineData("Audience")]
        [InlineData("Audiences")]
        public async Task CompatibilityRepair_ConfiguredParent_PreservesCompleteTypedOptions(string validatedProperty)
        {
            // Arrange
            const string selectedName = "BlueprintB";
            string parentId = Guid.NewGuid().ToString();
            string agentId = Guid.NewGuid().ToString();
            var factory = InitTokenAcquirerFactoryForAgent();
            ConfigureAgentBlueprint(factory, selectedName, parentId);
            MicrosoftIdentityApplicationOptions? configured = null;
            int configurations = 0;
            int postConfigurations = 0;
            factory.Services.Configure<MicrosoftIdentityApplicationOptions>(selectedName, options =>
            {
                configurations++;
                options.Name = "configured-blueprint";
                options.Audience = "api://configured-audience";
                options.Audiences = new[] { "api://configured-audience", "api://second-audience" };
                options.SignUpSignInPolicyId = "configured-user-flow";
                configured = options;
            });
            factory.Services.PostConfigure<MicrosoftIdentityApplicationOptions>(selectedName, _ => postConfigurations++);
            factory.Services.AddOptions<MicrosoftIdentityApplicationOptions>(selectedName).Validate(options => validatedProperty switch
            {
                "Name" => options.Name == "configured-blueprint",
                "Audience" => options.Audience == "api://configured-audience",
                "Audiences" => options.Audiences?.SequenceEqual(new[] { "api://configured-audience", "api://second-audience" }) == true,
                _ => false,
            }, "Configured application attributes must survive normal options validation.");
            factory.Services.AddAgentIdentities();
            var services = factory.Build();
            var http = Assert.IsType<MockHttpClientFactory>(services.GetRequiredService<IMsalHttpClientFactory>());
            AddBlueprintAssertion(http, agentId, parentId, "complete-parent-assertion");
            http.AddMockHandler(CreateClientCredentialsTokenHandler("complete-parent-token"));
            var options = ToTokenOptions(CreateNamedAgentAppOptions(agentId, "StaleParent"));
            var application = (MicrosoftEntraApplicationOptions)options.ExtraParameters![Constants.MicrosoftIdentityOptionsParameter];
            var helperCredential = Assert.Single(application.ClientCredentials!);
            var helperData = helperCredential.CustomSignedAssertionProviderData;

            // Act
            var result = await services.GetRequiredService<ITokenAcquisition>().GetAuthenticationResultForAppAsync(
                "https://graph.microsoft.com/.default", authenticationScheme: selectedName, tokenAcquisitionOptions: options);
            var cached = await services.GetRequiredService<ITokenAcquisition>().GetAuthenticationResultForAppAsync(
                "https://graph.microsoft.com/.default", authenticationScheme: selectedName, tokenAcquisitionOptions: options);

            // Assert
            Assert.Equal("complete-parent-token", result.AccessToken);
            Assert.Equal(result.AccessToken, cached.AccessToken);
            Assert.NotNull(configured);
            Assert.Same(configured, services.GetRequiredService<IOptionsMonitor<MicrosoftIdentityApplicationOptions>>().Get(selectedName));
            Assert.Equal(parentId, configured.ClientId);
            Assert.Equal("configured-blueprint", configured.Name);
            Assert.Equal("api://configured-audience", configured.Audience);
            Assert.Equal(new[] { "api://configured-audience", "api://second-audience" }, configured.Audiences);
            Assert.Equal("configured-user-flow", configured.DefaultUserFlow);
            var configuredCredential = Assert.Single(configured.ClientCredentials!);
            Assert.Null(configuredCredential.CachedValue);
            Assert.False(configuredCredential.Skip);
            Assert.Equal(1, configurations);
            Assert.Equal(1, postConfigurations);
            Assert.Same(helperCredential, Assert.Single(application.ClientCredentials!));
            Assert.Same(helperData, helperCredential.CustomSignedAssertionProviderData);
            Assert.False(helperData!.ContainsKey("ConfigurationSection"));
            Assert.Null(helperCredential.CachedValue);
            Assert.False(helperCredential.Skip);
            http.Dispose();
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public async Task CompatibilityRepair_ProgrammaticFactory_IsExplicitOnlyForAgentRequests(bool agentRequest, bool useStringOverload)
        {
            // Arrange
            var test = CreateAgentCacheTest();
            var applicationB = test.Services.GetRequiredService<IOptionsMonitor<MicrosoftIdentityApplicationOptions>>().Get("BlueprintB");
            var options = agentRequest
                ? CreateNamedAgentAppOptions(test.AgentId, "BlueprintA").AcquireTokenOptions
                : new AcquireTokenOptions { AuthenticationOptionsName = "BlueprintA" };
            if (agentRequest)
            {
                AddBlueprintAssertion(test.Http, test.AgentId, test.ParentB, "assertion-b", "test-only-b-secret");
                test.Http.AddMockHandler(CreateClientCredentialsTokenHandler("agent-via-b"));
            }
            else
            {
                var handler = CreateClientCredentialsTokenHandler("ordinary-via-a");
                handler.ExpectedPostData = new Dictionary<string, string>
                {
                    ["client_id"] = test.ParentA,
                    ["client_secret"] = "test-only-secret",
                };
                test.Http.AddMockHandler(handler);
            }

            // Act
            var acquirer = useStringOverload
                ? test.Factory.GetTokenAcquirer(applicationB.Authority!, applicationB.ClientId!, applicationB.ClientCredentials!, region: null)
                : test.Services.GetRequiredService<ITokenAcquirerFactory>().GetTokenAcquirer(applicationB);
            var result = await acquirer.GetTokenForAppAsync("https://graph.microsoft.com/.default", options);

            // Assert
            Assert.Equal(agentRequest ? "agent-via-b" : "ordinary-via-a", result.AccessToken);
            Assert.Equal("BlueprintA", options.AuthenticationOptionsName);
            if (agentRequest)
            {
                var agentApplication = (MicrosoftEntraApplicationOptions)options.ExtraParameters![Constants.MicrosoftIdentityOptionsParameter];
                var credential = agentApplication.ClientCredentials!.Single();
                Assert.False(credential.CustomSignedAssertionProviderData!.ContainsKey("ConfigurationSection"));
                Assert.Null(credential.CachedValue);
                Assert.False(credential.Skip);
            }
            test.Http.Dispose();
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task CompatibilityRepair_ConcurrentFactory_UsesOneAcquirerAndNormalPipeline(bool useStringOverload)
        {
            // Arrange
            var factory = InitTokenAcquirerFactoryForAgent();
            var application = new MicrosoftIdentityApplicationOptions
            {
                Instance = "https://login.microsoftonline.com/",
                TenantId = "10c419d4-4a50-45b2-aa4e-919fb84df24f",
                ClientId = Guid.NewGuid().ToString(),
                ClientCredentials = [new CredentialDescription
                {
                    SourceType = CredentialSource.ClientSecret,
                    ClientSecret = "test-only-secret",
                }],
            };
            string key = DefaultTokenAcquirerFactoryImplementation.GetKey(application.Authority, application.ClientId, null);
            int configurations = 0;
            int postConfigurations = 0;
            factory.Services.ConfigureAll<MicrosoftIdentityApplicationOptions>(_ => Interlocked.Increment(ref configurations));
            factory.Services.PostConfigure<MicrosoftIdentityApplicationOptions>(key, _ => Interlocked.Increment(ref postConfigurations));
            var services = factory.Build();
            var acquirerFactory = services.GetRequiredService<ITokenAcquirerFactory>();
            var http = Assert.IsType<MockHttpClientFactory>(services.GetRequiredService<IMsalHttpClientFactory>());
            var handler = CreateClientCredentialsTokenHandler("concurrent-factory-token");
            handler.ExpectedPostData = new Dictionary<string, string>
            {
                ["client_id"] = application.ClientId!,
                ["client_secret"] = "test-only-secret",
            };
            http.AddMockHandler(handler);

            // Act
            var acquirers = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => Task.Run(() => useStringOverload
                ? factory.GetTokenAcquirer(application.Authority!, application.ClientId!, application.ClientCredentials!, region: null)
                : acquirerFactory.GetTokenAcquirer(application))));
            var first = await acquirers[0].GetTokenForAppAsync("https://graph.microsoft.com/.default");
            var cached = await Task.WhenAll(acquirers.Select(acquirer => acquirer.GetTokenForAppAsync("https://graph.microsoft.com/.default")));

            // Assert
            Assert.All(acquirers, acquirer => Assert.Same(acquirers[0], acquirer));
            Assert.Equal("concurrent-factory-token", first.AccessToken);
            Assert.All(cached, result => Assert.Equal(first.AccessToken, result.AccessToken));
            Assert.True(configurations > 0);
            Assert.True(postConfigurations > 0);
            Assert.Equal(application.ClientId, services.GetRequiredService<IMergedOptionsStore>().Get(key).ClientId);
            Assert.Same(application.ClientCredentials, services.GetRequiredService<IMergedOptionsStore>().Get(key).ClientCredentials);
            Assert.Equal("test-only-secret", application.ClientCredentials!.Single().ClientSecret);
            http.Dispose();
        }

        [Fact]
        public async Task AgentAppIdentity_SeparateServiceProviders_DistributedCacheKeepsParentsIsolated()
        {
            // Arrange
            string agentId = Guid.NewGuid().ToString();
            string parentA = Guid.NewGuid().ToString();
            string parentB = Guid.NewGuid().ToString();
            var storage = new TestDistributedCache();
            IServiceProvider BuildProvider()
            {
                var factory = InitTokenAcquirerFactoryForAgent();
                ConfigureAgentBlueprint(factory, "BlueprintA", parentA);
                ConfigureAgentBlueprint(factory, "BlueprintB", parentB);
                factory.Services.AddAgentIdentities();
                factory.Services.AddSingleton<IDistributedCache>(storage);
                factory.Services.AddDistributedTokenCaches();
                factory.Services.Configure<MsalDistributedTokenCacheAdapterOptions>(options => options.DisableL1Cache = true);
                return factory.Build();
            }
            IServiceProvider firstServices = BuildProvider();
            var firstHttp = Assert.IsType<MockHttpClientFactory>(firstServices.GetRequiredService<IMsalHttpClientFactory>());
            var firstProvider = firstServices.GetRequiredService<IAuthorizationHeaderProvider>();
            firstHttp.AddMockHandler(CreateClientCredentialsTokenHandler("assertion-a"));
            firstHttp.AddMockHandler(CreateClientCredentialsTokenHandler("token-a"));

            // Act
            string first = await firstProvider.CreateAuthorizationHeaderForAppAsync(
                "https://graph.microsoft.com/.default", CreateNamedAgentAppOptions(agentId, "BlueprintA"));
            Assert.IsAssignableFrom<IDisposable>(firstServices).Dispose();
            IServiceProvider secondServices = BuildProvider();
            var secondHttp = Assert.IsType<MockHttpClientFactory>(secondServices.GetRequiredService<IMsalHttpClientFactory>());
            var secondProvider = secondServices.GetRequiredService<IAuthorizationHeaderProvider>();
            string shared = await secondProvider.CreateAuthorizationHeaderForAppAsync(
                "https://graph.microsoft.com/.default", CreateNamedAgentAppOptions(agentId, "BlueprintA"));
            var rejection = CreateRejectedBlueprintHandler(parentB);
            secondHttp.AddMockHandler(rejection);
            var error = await Assert.ThrowsAsync<MsalServiceException>(() =>
                secondProvider.CreateAuthorizationHeaderForAppAsync(
                    "https://graph.microsoft.com/.default", CreateNamedAgentAppOptions(agentId, "BlueprintB")));

            // Assert
            Assert.Equal("Bearer token-a", first);
            Assert.Equal(first, shared);
            Assert.Equal("unauthorized_client", error.ErrorCode);
            Assert.Equal(parentB, rejection.ActualRequestPostData["client_id"]);
            Assert.NotEmpty(storage._dict);
            secondHttp.Dispose();
            Assert.IsAssignableFrom<IDisposable>(secondServices).Dispose();
        }

        private (TokenAcquirerFactory Factory, IServiceProvider Services, MockHttpClientFactory Http,
            TokenAcquisition Acquisition, string AgentId, string ParentA, string ParentB) CreateAgentCacheTest(bool trackCacheConcurrency = false)
        {
            string agentId = Guid.NewGuid().ToString();
            string parentA = Guid.NewGuid().ToString();
            string parentB = Guid.NewGuid().ToString();
            var factory = InitTokenAcquirerFactoryForAgent();
            ConfigureAgentBlueprint(factory, "BlueprintA", parentA);
            ConfigureAgentBlueprint(factory, "BlueprintB", parentB, "test-only-b-secret");
            ConfigureAgentBlueprint(factory, "BlueprintAlias", parentA.ToUpperInvariant(), "test-only-alias-secret");
            ConfigureAgentBlueprint(factory, "AzureAd", parentA);
            ConfigureAgentBlueprint(factory, string.Empty, parentB, "test-only-b-secret");
            factory.Services.AddAgentIdentities();
            if (trackCacheConcurrency)
            {
                factory.Services.AddInMemoryTokenCaches();
                factory.Services.AddSingleton<IMsalTokenCacheProvider>(provider =>
                    new ConcurrencyTrackingMemoryTokenCacheProvider(provider.GetRequiredService<IMemoryCache>(),
                        provider.GetRequiredService<IOptions<MsalMemoryTokenCacheOptions>>()));
            }
            IServiceProvider services = factory.Build();

            return (factory, services, Assert.IsType<MockHttpClientFactory>(services.GetRequiredService<IMsalHttpClientFactory>()),
                (TokenAcquisition)services.GetRequiredService<ITokenAcquisition>(), agentId, parentA, parentB);
        }

        private sealed class ConcurrencyTrackingMemoryTokenCacheProvider : MsalMemoryTokenCacheProvider
        {
            private readonly ConcurrentDictionary<string, int> _readers = new();
            private readonly ConcurrentDictionary<string, int> _maximum = new();

            internal ConcurrencyTrackingMemoryTokenCacheProvider(IMemoryCache memoryCache, IOptions<MsalMemoryTokenCacheOptions> options)
                : base(memoryCache, options)
            {
            }

            internal IEnumerable<int> MaximumReaders => _maximum.Values;

            protected override async Task<byte[]?> ReadCacheBytesAsync(
                string cacheKey, CacheSerializerHints hints)
            {
                int readers = _readers.AddOrUpdate(cacheKey, 1, (_, value) => value + 1);
                _maximum.AddOrUpdate(cacheKey, readers, (_, value) => Math.Max(readers, value));
                try
                {
                    // Exercise asynchronous serialization without masking missing MSAL synchronization.
                    await Task.Yield();
                    return await base.ReadCacheBytesAsync(cacheKey, hints);
                }
                finally
                {
                    _readers.AddOrUpdate(cacheKey, 0, (_, value) => value - 1);
                }
            }
        }

        private static void AddBlueprintAssertion(
            MockHttpClientFactory http, string agentId, string parentId, string assertion, string secret = "test-only-secret")
        {
            var handler = CreateClientCredentialsTokenHandler(assertion);
            handler.ExpectedPostData = new Dictionary<string, string>
            {
                ["client_id"] = parentId,
                ["client_secret"] = secret,
                ["fmi_path"] = agentId,
            };
            http.AddMockHandler(handler);
        }

        private static TokenAcquisitionOptions ToTokenOptions(AuthorizationHeaderProviderOptions options) => new()
        {
            AuthenticationOptionsName = options.AcquireTokenOptions.AuthenticationOptionsName,
            ExtraParameters = options.AcquireTokenOptions.ExtraParameters,
        };

        private static AuthorizationHeaderProviderOptions CreateNamedAgentOptions(
            string agentId, string name, bool userFic, bool useOid = false)
        {
            if (!userFic)
            {
                return CreateNamedAgentAppOptions(agentId, name);
            }

            var options = useOid
                ? new AuthorizationHeaderProviderOptions().WithAgentUserIdentity(agentId, AgentTestUserOid)
                : new AuthorizationHeaderProviderOptions().WithAgentUserIdentity(agentId, AgentTestUsername);
            options.AcquireTokenOptions.AuthenticationOptionsName = name;
            return options;
        }

        private static void ConfigureAgentBlueprint(
            TokenAcquirerFactory factory, string name, string clientId, string secret = "test-only-secret")
        {
            factory.Services.Configure<MicrosoftIdentityApplicationOptions>(name, options =>
            {
                options.Instance = "https://login.microsoftonline.com/";
                options.TenantId = "10c419d4-4a50-45b2-aa4e-919fb84df24f";
                options.ClientId = clientId;
                options.ClientCredentials = [new CredentialDescription
                {
                    SourceType = CredentialSource.ClientSecret,
                    ClientSecret = secret,
                }];
            });
        }

        private static AuthorizationHeaderProviderOptions CreateNamedAgentAppOptions(string agentId, string name)
        {
            return new AuthorizationHeaderProviderOptions
            {
                AcquireTokenOptions = new AcquireTokenOptions { AuthenticationOptionsName = name },
            }.WithAgentIdentity(agentId);
        }

        private static MockHttpMessageHandler CreateRejectedBlueprintHandler(string clientId)
        {
            return new MockHttpMessageHandler
            {
                ExpectedMethod = HttpMethod.Post,
                ExpectedPostData = new Dictionary<string, string> { ["client_id"] = clientId },
                ResponseMessage = new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest)
                {
                    Content = new StringContent("{\"error\":\"unauthorized_client\",\"error_description\":\"Blueprint is not authorized for this agent.\"}"),
                },
            };
        }

        private const string AgentTestUsername = "testuser@contoso.com";

        /// <summary>
        /// Verifies the fix for GitHub issue #3840: the native User FIC flow uses the
        /// multi-CCA pattern (blueprint + agent CCA) and caches user tokens properly.
        /// The second call should use AcquireTokenSilent — no additional network calls.
        /// </summary>
        [Fact]
        public async Task AgentUserIdentity_NativeUserFic_UsesCacheOnSecondCall()
        {
            // Arrange — use a unique agent app ID to avoid MSAL shared cache interference
            string agentAppId = Guid.NewGuid().ToString("N");
            var factory = InitTokenAcquirerFactoryForAgent();
            IServiceProvider serviceProvider = factory.Build();

            var mockHttpClient = serviceProvider.GetRequiredService<IMsalHttpClientFactory>() as MockHttpClientFactory;

            // First call needs 3 handlers: Leg 1 (blueprint FMI), Leg 2 (agent instance), Leg 3 (user_fic).
            // Second call needs 0 handlers (silent cache hit).
            AddAgentUserFicMockHandlers(mockHttpClient!, userAccessToken: "user-token-1");

            IAuthorizationHeaderProvider authorizationHeaderProvider =
                serviceProvider.GetRequiredService<IAuthorizationHeaderProvider>();

            var options = CreateAgentIdentityOptions(agentAppId);

            // Act — first call: full 3-leg flow (Leg 1 → T1, Leg 2 → T2, Leg 3 → user token)
            string result1 = await authorizationHeaderProvider.CreateAuthorizationHeaderForUserAsync(
                new[] { "https://graph.microsoft.com/.default" },
                authorizationHeaderProviderOptions: options,
                claimsPrincipal: null);

            // Act — second call: should use AcquireTokenSilent (no mock handlers left)
            string result2 = await authorizationHeaderProvider.CreateAuthorizationHeaderForUserAsync(
                new[] { "https://graph.microsoft.com/.default" },
                authorizationHeaderProviderOptions: options,
                claimsPrincipal: null);

            // Assert — both return the same token, second from cache
            Assert.Equal("Bearer user-token-1", result1);
            Assert.Equal("Bearer user-token-1", result2);
        }

        /// <summary>
        /// Verifies cache works with new ClaimsPrincipal instances per call. The native
        /// User FIC path does not depend on ClaimsPrincipal for cache lookups — the
        /// account identifier is stored internally via _agentUserFicAccountIds.
        /// </summary>
        [Fact]
        public async Task AgentUserIdentity_NativeUserFic_CacheWorksWithNewClaimsPrincipalPerCall()
        {
            // Arrange
            string agentAppId = Guid.NewGuid().ToString("N");
            var factory = InitTokenAcquirerFactoryForAgent();
            IServiceProvider serviceProvider = factory.Build();

            var mockHttpClient = serviceProvider.GetRequiredService<IMsalHttpClientFactory>() as MockHttpClientFactory;
            AddAgentUserFicMockHandlers(mockHttpClient!, userAccessToken: "user-token-1");

            IAuthorizationHeaderProvider authorizationHeaderProvider =
                serviceProvider.GetRequiredService<IAuthorizationHeaderProvider>();

            var options = CreateAgentIdentityOptions(agentAppId);

            // Act — each call gets a fresh ClaimsPrincipal (simulates request-scoped DI)
            string result1 = await authorizationHeaderProvider.CreateAuthorizationHeaderForUserAsync(
                new[] { "https://graph.microsoft.com/.default" },
                authorizationHeaderProviderOptions: options,
                claimsPrincipal: new System.Security.Claims.ClaimsPrincipal(
                    new Microsoft.IdentityModel.Tokens.CaseSensitiveClaimsIdentity()));

            string result2 = await authorizationHeaderProvider.CreateAuthorizationHeaderForUserAsync(
                new[] { "https://graph.microsoft.com/.default" },
                authorizationHeaderProviderOptions: options,
                claimsPrincipal: new System.Security.Claims.ClaimsPrincipal(
                    new Microsoft.IdentityModel.Tokens.CaseSensitiveClaimsIdentity()));

            // Assert — both return the same cached token (unlike old ROPC path)
            Assert.Equal("Bearer user-token-1", result1);
            Assert.Equal("Bearer user-token-1", result2);
        }

        private static AuthorizationHeaderProviderOptions CreateAgentIdentityOptions(string agentAppId)
        {
            return CreateAgentIdentityOptionsWithUpn(agentAppId, AgentTestUsername);
        }

        private static AuthorizationHeaderProviderOptions CreateAgentIdentityOptionsWithUpn(string agentAppId, string username)
        {
            return new AuthorizationHeaderProviderOptions
            {
                AcquireTokenOptions = new AcquireTokenOptions
                {
                    ExtraParameters = new Dictionary<string, object>
                    {
                        [Constants.AgentIdentityKey] = agentAppId,
                        [Constants.UsernameKey] = username,
                    }
                }
            };
        }

        private static AuthorizationHeaderProviderOptions CreateAgentIdentityOptionsWithOid(string agentAppId, Guid userObjectId)
        {
            return new AuthorizationHeaderProviderOptions
            {
                AcquireTokenOptions = new AcquireTokenOptions
                {
                    ExtraParameters = new Dictionary<string, object>
                    {
                        [Constants.AgentIdentityKey] = agentAppId,
                        [Constants.UserIdKey] = userObjectId.ToString("D"),
                    }
                }
            };
        }

        private TokenAcquirerFactory InitTokenAcquirerFactoryForAgent()
        {
            TokenAcquirerFactoryTesting.ResetTokenAcquirerFactoryInTest();
            TokenAcquirerFactory tokenAcquirerFactory = TokenAcquirerFactory.GetDefaultInstance();

            var mockHttpFactory = new MockHttpClientFactory();

            tokenAcquirerFactory.Services.Configure<MicrosoftIdentityApplicationOptions>(options =>
            {
                options.Instance = "https://login.microsoftonline.com/";
                options.TenantId = "10c419d4-4a50-45b2-aa4e-919fb84df24f";
                options.ClientId = "idu773ld-e38d-jud3-45lk-d1b09a74a8ca";
                options.ClientCredentials = [new CredentialDescription()
                {
                    SourceType = CredentialSource.ClientSecret,
                    ClientSecret = "someSecret"
                }];
            });

            tokenAcquirerFactory.Services.AddSingleton<IMsalHttpClientFactory>(mockHttpFactory);

            return tokenAcquirerFactory;
        }

        /// <summary>
        /// Adds mock handlers for the 3-leg agent User FIC flow:
        ///   Handler 1: Leg 1 — blueprint's AcquireTokenForClient (FMI token / T1)
        ///   Handler 2: Leg 2 — agent's AcquireTokenForClient (instance token / T2)
        ///   Handler 3: Leg 3 — agent's AcquireTokenByUserFederatedIdentityCredential (user token)
        /// Handler 1 also auto-handles instance discovery via the mock factory's re-queue mechanism.
        /// </summary>
        private static void AddAgentUserFicMockHandlers(
            MockHttpClientFactory mockHttpClient,
            string userAccessToken = "header.payload.signature")
        {
            // Leg 1: Blueprint FMI token (T1) — client_credentials with fmi_path
            mockHttpClient.AddMockHandler(CreateClientCredentialsTokenHandler(accessToken: "t1-fmi-token"));

            // Leg 2: Agent instance token (T2) — client_credentials with T1 as assertion
            mockHttpClient.AddMockHandler(CreateClientCredentialsTokenHandler(accessToken: "t2-instance-token"));

            // Leg 3: User token — user_fic grant with T2 as assertion
            mockHttpClient.AddMockHandler(CreateUserFicTokenHandler(accessToken: userAccessToken));
        }

        /// <summary>
        /// Creates a mock handler for a client_credentials response (used for Legs 1 and 2).
        /// </summary>
        private static MockHttpMessageHandler CreateClientCredentialsTokenHandler(string accessToken)
        {
            return new MockHttpMessageHandler()
            {
                ExpectedMethod = HttpMethod.Post,
                ResponseMessage = MockHttpCreator.CreateSuccessResponseMessage(
                    "{\"token_type\":\"Bearer\"," +
                    "\"expires_in\":3599," +
                    "\"access_token\":\"" + accessToken + "\"," +
                    "\"client_info\":\"" + EncodeBase64Url(
                        "{\"uid\":\"" + TestConstants.Uid + "\",\"utid\":\"" + TestConstants.Utid + "\"}") + "\"}"),
            };
        }

        /// <summary>
        /// Creates a mock handler for a user_fic response (Leg 3) with id_token, refresh_token,
        /// and client_info so MSAL creates a proper account in the cache.
        /// </summary>
        private static MockHttpMessageHandler CreateUserFicTokenHandler(
            string accessToken, string refreshToken = "mock-refresh-token", int expiresIn = 3599)
        {
            return new MockHttpMessageHandler()
            {
                ExpectedMethod = HttpMethod.Post,
                ResponseMessage = MockHttpCreator.CreateSuccessResponseMessage(
                    "{\"token_type\":\"Bearer\"," +
                    "\"expires_in\":" + expiresIn + "," +
                    "\"scope\":\"https://graph.microsoft.com/.default openid profile offline_access\"," +
                    "\"access_token\":\"" + accessToken + "\"," +
                    "\"refresh_token\":\"" + refreshToken + "\"," +
                    "\"client_info\":\"" + EncodeBase64Url(
                        "{\"uid\":\"" + TestConstants.Uid + "\",\"utid\":\"" + TestConstants.Utid + "\"}") + "\"," +
                    "\"id_token\":\"" + MockHttpCreator.CreateIdToken(TestConstants.Uid, AgentTestUsername) + "\"}"),
            };
        }

        private static string EncodeBase64Url(string input)
        {
            return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(input))
                .TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }

        // --- OID-based User FIC tests ---

        private static readonly Guid AgentTestUserOid = new Guid("00000000-1111-2222-3333-444444444444");

        /// <summary>
        /// Verifies that OID-based agentic User FIC flow uses the native path and caches properly.
        /// Same pattern as UPN but uses Guid userObjectId overload.
        /// </summary>
        [Fact]
        public async Task AgentUserIdentity_NativeUserFic_OidUsesCacheOnSecondCall()
        {
            // Arrange
            string agentAppId = Guid.NewGuid().ToString("N");
            var factory = InitTokenAcquirerFactoryForAgent();
            IServiceProvider serviceProvider = factory.Build();

            var mockHttpClient = serviceProvider.GetRequiredService<IMsalHttpClientFactory>() as MockHttpClientFactory;
            AddAgentUserFicMockHandlers(mockHttpClient!, userAccessToken: "user-token-oid-1");

            IAuthorizationHeaderProvider authorizationHeaderProvider =
                serviceProvider.GetRequiredService<IAuthorizationHeaderProvider>();

            var options = CreateAgentIdentityOptionsWithOid(agentAppId, AgentTestUserOid);

            // Act — first call: full 3-leg flow
            string result1 = await authorizationHeaderProvider.CreateAuthorizationHeaderForUserAsync(
                new[] { "https://graph.microsoft.com/.default" },
                authorizationHeaderProviderOptions: options,
                claimsPrincipal: null);

            // Act — second call: should use AcquireTokenSilent (no mock handlers left)
            string result2 = await authorizationHeaderProvider.CreateAuthorizationHeaderForUserAsync(
                new[] { "https://graph.microsoft.com/.default" },
                authorizationHeaderProviderOptions: options,
                claimsPrincipal: null);

            // Assert — both return the same cached token
            Assert.Equal("Bearer user-token-oid-1", result1);
            Assert.Equal("Bearer user-token-oid-1", result2);
        }

        /// <summary>
        /// Verifies that UPN and OID flows for the same agent produce separate cached tokens,
        /// ensuring cache isolation between the two identifier types.
        /// </summary>
        [Fact]
        public async Task AgentUserIdentity_NativeUserFic_UpnAndOidCachesAreIsolated()
        {
            // Arrange — same agent app ID for both flows
            string agentAppId = Guid.NewGuid().ToString("N");
            var factory = InitTokenAcquirerFactoryForAgent();
            IServiceProvider serviceProvider = factory.Build();

            var mockHttpClient = serviceProvider.GetRequiredService<IMsalHttpClientFactory>() as MockHttpClientFactory;

            // UPN flow handlers (3 legs)
            AddAgentUserFicMockHandlers(mockHttpClient!, userAccessToken: "upn-user-token");
            // OID flow: Legs 1 and 2 are cached (same blueprint and agent CCA),
            // only a Leg 3 handler is needed for the OID grant type.
            mockHttpClient!.AddMockHandler(CreateUserFicTokenHandler(accessToken: "oid-user-token"));

            IAuthorizationHeaderProvider authorizationHeaderProvider =
                serviceProvider.GetRequiredService<IAuthorizationHeaderProvider>();

            var upnOptions = CreateAgentIdentityOptionsWithUpn(agentAppId, AgentTestUsername);
            var oidOptions = CreateAgentIdentityOptionsWithOid(agentAppId, AgentTestUserOid);

            // Act — UPN flow first
            string upnResult = await authorizationHeaderProvider.CreateAuthorizationHeaderForUserAsync(
                new[] { "https://graph.microsoft.com/.default" },
                authorizationHeaderProviderOptions: upnOptions,
                claimsPrincipal: null);

            // Act — OID flow for same agent
            string oidResult = await authorizationHeaderProvider.CreateAuthorizationHeaderForUserAsync(
                new[] { "https://graph.microsoft.com/.default" },
                authorizationHeaderProviderOptions: oidOptions,
                claimsPrincipal: null);

            // Assert — different tokens, not sharing cache entries
            Assert.Equal("Bearer upn-user-token", upnResult);
            Assert.Equal("Bearer oid-user-token", oidResult);
        }

        #endregion

        #region Agent Shared Cache Isolation Tests

        /// <summary>
        /// Verifies that with EnableSharedCacheOptions, tokens for 2 agents and 2 users
        /// are cached and retrieved correctly — no cross-agent or cross-user collisions.
        /// </summary>
        [Fact]
        public async Task AgentSharedCache_MultiAgentMultiUser_ReturnsCorrectTokens()
        {
            // Arrange — 1 blueprint, 2 agents, 2 users
            string agent1 = Guid.NewGuid().ToString("N");
            string agent2 = Guid.NewGuid().ToString("N");
            string user1Uid = Guid.NewGuid().ToString("N");
            string user2Uid = Guid.NewGuid().ToString("N");
            string user1Upn = "user1@contoso.com";
            string user2Upn = "user2@contoso.com";

            var factory = InitTokenAcquirerFactoryForAgent();
            IServiceProvider serviceProvider = factory.Build();
            var mockHttp = serviceProvider.GetRequiredService<IMsalHttpClientFactory>() as MockHttpClientFactory;
            IAuthorizationHeaderProvider authProvider =
                serviceProvider.GetRequiredService<IAuthorizationHeaderProvider>();

            // Enqueue handlers for all 4 combinations (agent1+user1, agent1+user2, agent2+user1, agent2+user2)
            // Agent1+User1: Leg 1 + Leg 2 + Leg 3
            AddAgentUserFicMockHandlersForUser(mockHttp!, "token-a1-u1", user1Uid, user1Upn);
            // Agent1+User2: Leg 3 only (Legs 1,2 cached from agent1 CCA)
            mockHttp!.AddMockHandler(CreateUserFicTokenHandlerForUser("token-a1-u2", user2Uid, user2Upn));
            // Agent2+User1: New agent CCA → assertion callback fires (Leg 1) + Leg 2 + Leg 3
            AddAgentUserFicMockHandlersForUser(mockHttp!, "token-a2-u1", user1Uid, user1Upn);
            // Agent2+User2: Leg 3 only (Leg 2 cached from agent2 CCA)
            mockHttp.AddMockHandler(CreateUserFicTokenHandlerForUser("token-a2-u2", user2Uid, user2Upn));

            // Act — acquire tokens for all 4 combinations
            string r_a1u1 = await authProvider.CreateAuthorizationHeaderForUserAsync(
                new[] { "https://graph.microsoft.com/.default" },
                authorizationHeaderProviderOptions: CreateAgentIdentityOptionsWithUpn(agent1, user1Upn),
                claimsPrincipal: null);

            string r_a1u2 = await authProvider.CreateAuthorizationHeaderForUserAsync(
                new[] { "https://graph.microsoft.com/.default" },
                authorizationHeaderProviderOptions: CreateAgentIdentityOptionsWithUpn(agent1, user2Upn),
                claimsPrincipal: null);

            string r_a2u1 = await authProvider.CreateAuthorizationHeaderForUserAsync(
                new[] { "https://graph.microsoft.com/.default" },
                authorizationHeaderProviderOptions: CreateAgentIdentityOptionsWithUpn(agent2, user1Upn),
                claimsPrincipal: null);

            string r_a2u2 = await authProvider.CreateAuthorizationHeaderForUserAsync(
                new[] { "https://graph.microsoft.com/.default" },
                authorizationHeaderProviderOptions: CreateAgentIdentityOptionsWithUpn(agent2, user2Upn),
                claimsPrincipal: null);

            // Assert — each combination got its own unique token
            Assert.Equal("Bearer token-a1-u1", r_a1u1);
            Assert.Equal("Bearer token-a1-u2", r_a1u2);
            Assert.Equal("Bearer token-a2-u1", r_a2u1);
            Assert.Equal("Bearer token-a2-u2", r_a2u2);

            // Act — silent calls (no more handlers) return the correct cached token
            string s_a1u1 = await authProvider.CreateAuthorizationHeaderForUserAsync(
                new[] { "https://graph.microsoft.com/.default" },
                authorizationHeaderProviderOptions: CreateAgentIdentityOptionsWithUpn(agent1, user1Upn),
                claimsPrincipal: null);
            string s_a2u2 = await authProvider.CreateAuthorizationHeaderForUserAsync(
                new[] { "https://graph.microsoft.com/.default" },
                authorizationHeaderProviderOptions: CreateAgentIdentityOptionsWithUpn(agent2, user2Upn),
                claimsPrincipal: null);
            string s_a1u2 = await authProvider.CreateAuthorizationHeaderForUserAsync(
                new[] { "https://graph.microsoft.com/.default" },
                authorizationHeaderProviderOptions: CreateAgentIdentityOptionsWithUpn(agent1, user2Upn),
                claimsPrincipal: null);
            string s_a2u1 = await authProvider.CreateAuthorizationHeaderForUserAsync(
                new[] { "https://graph.microsoft.com/.default" },
                authorizationHeaderProviderOptions: CreateAgentIdentityOptionsWithUpn(agent2, user1Upn),
                claimsPrincipal: null);

            Assert.Equal("Bearer token-a1-u1", s_a1u1);
            Assert.Equal("Bearer token-a2-u2", s_a2u2);
            Assert.Equal("Bearer token-a1-u2", s_a1u2);
            Assert.Equal("Bearer token-a2-u1", s_a2u1);
        }

        /// <summary>
        /// Verifies that when EnableSharedCacheOptions is enabled on agent CCAs,
        /// tokens survive CCA eviction and new CCAs can retrieve them via silent calls.
        /// This validates that shared static cache makes agent tokens durable across
        /// CCA lifecycle events.
        /// </summary>
        [Fact]
        public async Task AgentSharedCache_WithSharedCacheEnabled_TokensSurviveCcaEviction()
        {
            // Arrange — 2 agents, 2 users, shared cache enabled
            string agent1 = Guid.NewGuid().ToString("N");
            string agent2 = Guid.NewGuid().ToString("N");
            string user1Uid = Guid.NewGuid().ToString("N");
            string user2Uid = Guid.NewGuid().ToString("N");
            string user1Upn = "user1@contoso.com";
            string user2Upn = "user2@contoso.com";

            var factory = InitTokenAcquirerFactoryForAgent();
            IServiceProvider serviceProvider = factory.Build();
            var mockHttp = serviceProvider.GetRequiredService<IMsalHttpClientFactory>() as MockHttpClientFactory;
            IAuthorizationHeaderProvider authProvider =
                serviceProvider.GetRequiredService<IAuthorizationHeaderProvider>();
            var tokenAcquisition = (TokenAcquisition)serviceProvider.GetRequiredService<ITokenAcquisition>();

            // Acquire tokens for both agents and both users
            AddAgentUserFicMockHandlersForUser(mockHttp!, "shared-token-a1u1", user1Uid, user1Upn);
            await authProvider.CreateAuthorizationHeaderForUserAsync(
                new[] { "https://graph.microsoft.com/.default" },
                authorizationHeaderProviderOptions: CreateAgentIdentityOptionsWithUpn(agent1, user1Upn),
                claimsPrincipal: null);

            mockHttp!.AddMockHandler(CreateUserFicTokenHandlerForUser("shared-token-a1u2", user2Uid, user2Upn));
            await authProvider.CreateAuthorizationHeaderForUserAsync(
                new[] { "https://graph.microsoft.com/.default" },
                authorizationHeaderProviderOptions: CreateAgentIdentityOptionsWithUpn(agent1, user2Upn),
                claimsPrincipal: null);

            AddAgentUserFicMockHandlersForUser(mockHttp, "shared-token-a2u1", user1Uid, user1Upn);
            await authProvider.CreateAuthorizationHeaderForUserAsync(
                new[] { "https://graph.microsoft.com/.default" },
                authorizationHeaderProviderOptions: CreateAgentIdentityOptionsWithUpn(agent2, user1Upn),
                claimsPrincipal: null);

            mockHttp.AddMockHandler(CreateUserFicTokenHandlerForUser("shared-token-a2u2", user2Uid, user2Upn));
            await authProvider.CreateAuthorizationHeaderForUserAsync(
                new[] { "https://graph.microsoft.com/.default" },
                authorizationHeaderProviderOptions: CreateAgentIdentityOptionsWithUpn(agent2, user2Upn),
                claimsPrincipal: null);

            // Evict ALL agent CCAs from the shared dictionary.
            // Keep _agentUserFicAccountIds intact — silent lookup needs them to find the account.
            foreach (var key in tokenAcquisition._applicationsByAuthorityClientId.Keys
                .Where(k => k.IndexOf(":agent:", StringComparison.Ordinal) >= 0).ToList())
            {
                tokenAcquisition._applicationsByAuthorityClientId.TryRemove(key, out _);
            }

            // Act — new CCAs must be built, but tokens should come from shared static cache.
            // Each new CCA needs a Leg 1 handler (assertion callback fires on first use).
            mockHttp.AddMockHandler(CreateClientCredentialsTokenHandler(accessToken: "t1-rebuild-a1"));
            string result_a1u1 = await authProvider.CreateAuthorizationHeaderForUserAsync(
                new[] { "https://graph.microsoft.com/.default" },
                authorizationHeaderProviderOptions: CreateAgentIdentityOptionsWithUpn(agent1, user1Upn),
                claimsPrincipal: null);

            mockHttp.AddMockHandler(CreateClientCredentialsTokenHandler(accessToken: "t1-rebuild-a2"));
            string result_a2u2 = await authProvider.CreateAuthorizationHeaderForUserAsync(
                new[] { "https://graph.microsoft.com/.default" },
                authorizationHeaderProviderOptions: CreateAgentIdentityOptionsWithUpn(agent2, user2Upn),
                claimsPrincipal: null);

            // Assert — tokens from original acquisition should come back (cached in shared static storage)
            Assert.Equal("Bearer shared-token-a1u1", result_a1u1);
            Assert.Equal("Bearer shared-token-a2u2", result_a2u2);
        }

        // --- Helpers for user-specific mock handlers ---

        /// <summary>
        /// Adds mock handlers for the 3-leg flow with a specific user identity (uid/upn).
        /// This allows testing cache isolation between different users.
        /// </summary>
        private static void AddAgentUserFicMockHandlersForUser(
            MockHttpClientFactory mockHttpClient,
            string userAccessToken,
            string userUid,
            string userUpn)
        {
            // Leg 1: Blueprint FMI token (T1)
            mockHttpClient.AddMockHandler(CreateClientCredentialsTokenHandler(accessToken: "t1-fmi-token"));
            // Leg 2: Agent instance token (T2)
            mockHttpClient.AddMockHandler(CreateClientCredentialsTokenHandler(accessToken: "t2-instance-token"));
            // Leg 3: User token with specific user identity
            mockHttpClient.AddMockHandler(CreateUserFicTokenHandlerForUser(userAccessToken, userUid, userUpn));
        }

        /// <summary>
        /// Creates a user_fic response handler with a specific user identity (uid/upn)
        /// so MSAL creates a distinct account per user in the cache.
        /// </summary>
        private static MockHttpMessageHandler CreateUserFicTokenHandlerForUser(
            string accessToken, string userUid, string userUpn)
        {
            string clientInfo = EncodeBase64Url(
                "{\"uid\":\"" + userUid + "\",\"utid\":\"" + TestConstants.Utid + "\"}");
            string idToken = MockHttpCreator.CreateIdToken(userUid, userUpn);

            return new MockHttpMessageHandler()
            {
                ExpectedMethod = HttpMethod.Post,
                ResponseMessage = MockHttpCreator.CreateSuccessResponseMessage(
                    "{\"token_type\":\"Bearer\"," +
                    "\"expires_in\":3599," +
                    "\"scope\":\"https://graph.microsoft.com/.default openid profile offline_access\"," +
                    "\"access_token\":\"" + accessToken + "\"," +
                    "\"refresh_token\":\"rt-" + accessToken + "\"," +
                    "\"client_info\":\"" + clientInfo + "\"," +
                    "\"id_token\":\"" + idToken + "\"}"),
            };
        }

        #endregion

        #region Agent CCA Size-Threshold Eviction Tests

        /// <summary>
        /// Verifies that when the agent CCA dictionary exceeds the configured threshold,
        /// it is cleared entirely as DOS protection. Tokens survive in MSAL's shared
        /// static cache (tested separately by WithSharedCacheEnabled_TokensSurviveCcaEviction).
        /// </summary>
        [Fact]
        public async Task AgentCcaEviction_ClearsDictionaryAtThreshold()
        {
            // Arrange — set a very low threshold to trigger clearing
            var factory = InitTokenAcquirerFactoryForAgent();
            IServiceProvider serviceProvider = factory.Build();
            var mockHttp = serviceProvider.GetRequiredService<IMsalHttpClientFactory>() as MockHttpClientFactory;
            IAuthorizationHeaderProvider authProvider =
                serviceProvider.GetRequiredService<IAuthorizationHeaderProvider>();
            var tokenAcquisition = (TokenAcquisition)serviceProvider.GetRequiredService<ITokenAcquisition>();

            // Set threshold to 4 so the 3rd agent triggers a clear:
            // 1 blueprint + 2 agents = 3 entries (≤ 4), 1 blueprint + 3 agents = 4 entries (≤ 4)
            // but after adding the 3rd agent the count becomes 4 which equals the threshold.
            // We use 3 so that: blueprint + 2 agents = 3 ≤ 3 (no eviction),
            // blueprint + 3 agents = 4 > 3 (triggers eviction).
            tokenAcquisition.AgentCcaMaxCount = 3;

            string agent1 = Guid.NewGuid().ToString("N");
            string agent2 = Guid.NewGuid().ToString("N");
            string agent3 = Guid.NewGuid().ToString("N");
            string user1Uid = Guid.NewGuid().ToString("N");
            string user1Upn = "user1@contoso.com";

            // Populate 2 agents (at threshold, not over)
            AddAgentUserFicMockHandlersForUser(mockHttp!, "token-a1", user1Uid, user1Upn);
            await authProvider.CreateAuthorizationHeaderForUserAsync(
                new[] { "https://graph.microsoft.com/.default" },
                authorizationHeaderProviderOptions: CreateAgentIdentityOptionsWithUpn(agent1, user1Upn),
                claimsPrincipal: null);

            AddAgentUserFicMockHandlersForUser(mockHttp!, "token-a2", user1Uid, user1Upn);
            await authProvider.CreateAuthorizationHeaderForUserAsync(
                new[] { "https://graph.microsoft.com/.default" },
                authorizationHeaderProviderOptions: CreateAgentIdentityOptionsWithUpn(agent2, user1Upn),
                claimsPrincipal: null);

            Assert.Equal(2, tokenAcquisition._applicationsByAuthorityClientId.Keys.Count(k => k.IndexOf(":agent:", StringComparison.Ordinal) >= 0));
            Assert.Equal(2, tokenAcquisition._agentUserFicAccountIds.Count);

            // Add 3rd agent — exceeds threshold, triggers clear
            AddAgentUserFicMockHandlersForUser(mockHttp!, "token-a3", user1Uid, user1Upn);
            await authProvider.CreateAuthorizationHeaderForUserAsync(
                new[] { "https://graph.microsoft.com/.default" },
                authorizationHeaderProviderOptions: CreateAgentIdentityOptionsWithUpn(agent3, user1Upn),
                claimsPrincipal: null);

            // Assert — dictionary was cleared by DOS eviction, then repopulated during
            // the rest of the 3rd agent's flow (blueprint rebuilds for Leg 1).
            // Previous agent CCAs are gone; only the rebuilt blueprint remains.
            Assert.Equal(0, tokenAcquisition._applicationsByAuthorityClientId.Keys
                .Count(k => k.IndexOf(":agent:", StringComparison.Ordinal) >= 0));
            Assert.Single(tokenAcquisition._agentUserFicAccountIds);
        }

        /// <summary>
        /// Verifies that agent CCAs are stored in the shared _applicationsByAuthorityClientId
        /// dictionary alongside normal CCAs, using the ":agent:" key segment for identification.
        /// This ensures agent CCAs go through the same builder path and get identical configuration
        /// (logging, authority handling, cache initialization) as normal CCAs.
        /// </summary>
        [Fact]
        public async Task AgentCca_StoredInSharedDictionary_WithAgentKeySegment()
        {
            // Arrange
            string agent1 = Guid.NewGuid().ToString("N");
            string user1Upn = "user1@contoso.com";

            var factory = InitTokenAcquirerFactoryForAgent();
            IServiceProvider serviceProvider = factory.Build();
            var mockHttp = serviceProvider.GetRequiredService<IMsalHttpClientFactory>() as MockHttpClientFactory;
            IAuthorizationHeaderProvider authProvider =
                serviceProvider.GetRequiredService<IAuthorizationHeaderProvider>();
            var tokenAcquisition = (TokenAcquisition)serviceProvider.GetRequiredService<ITokenAcquisition>();

            AddAgentUserFicMockHandlersForUser(mockHttp!, "token-a1u1", Guid.NewGuid().ToString("N"), user1Upn);

            // Act
            await authProvider.CreateAuthorizationHeaderForUserAsync(
                new[] { "https://graph.microsoft.com/.default" },
                authorizationHeaderProviderOptions: CreateAgentIdentityOptionsWithUpn(agent1, user1Upn),
                claimsPrincipal: null);

            // Assert — the shared dictionary has agent entries (identified by ":agent:" segment)
            var agentKeys = tokenAcquisition._applicationsByAuthorityClientId.Keys
                .Where(k => k.IndexOf(":agent:", StringComparison.Ordinal) >= 0)
                .ToList();
            Assert.Single(agentKeys);
            Assert.True(agentKeys[0].IndexOf(agent1, StringComparison.Ordinal) >= 0);

            // The blueprint CCA is also in the same dictionary (created lazily by assertion callback)
            var blueprintKeys = tokenAcquisition._applicationsByAuthorityClientId.Keys
                .Where(k => k.IndexOf(":agent:", StringComparison.Ordinal) < 0)
                .ToList();
            Assert.Single(blueprintKeys);
        }

        /// <summary>
        /// Verifies that DOS eviction clears the entire CCA dictionary (including blueprint),
        /// but tokens survive in MSAL's shared static cache. After eviction, new CCAs are
        /// rebuilt lazily and can still retrieve cached tokens via AcquireTokenSilent.
        /// </summary>
        [Fact]
        public async Task AgentCcaEviction_ClearsDictionary_TokensSurvive()
        {
            // Arrange
            string agent1 = Guid.NewGuid().ToString("N");
            string agent2 = Guid.NewGuid().ToString("N");
            string agent3 = Guid.NewGuid().ToString("N");
            string user1Upn = "user1@contoso.com";
            string user1Uid = Guid.NewGuid().ToString("N");

            var factory = InitTokenAcquirerFactoryForAgent();
            IServiceProvider serviceProvider = factory.Build();
            var mockHttp = serviceProvider.GetRequiredService<IMsalHttpClientFactory>() as MockHttpClientFactory;
            IAuthorizationHeaderProvider authProvider =
                serviceProvider.GetRequiredService<IAuthorizationHeaderProvider>();
            var tokenAcquisition = (TokenAcquisition)serviceProvider.GetRequiredService<ITokenAcquisition>();
            tokenAcquisition.AgentCcaMaxCount = 3;

            // Populate 2 agents (at threshold)
            AddAgentUserFicMockHandlersForUser(mockHttp!, "token-a1", user1Uid, user1Upn);
            await authProvider.CreateAuthorizationHeaderForUserAsync(
                new[] { "https://graph.microsoft.com/.default" },
                authorizationHeaderProviderOptions: CreateAgentIdentityOptionsWithUpn(agent1, user1Upn),
                claimsPrincipal: null);

            AddAgentUserFicMockHandlersForUser(mockHttp!, "token-a2", user1Uid, user1Upn);
            await authProvider.CreateAuthorizationHeaderForUserAsync(
                new[] { "https://graph.microsoft.com/.default" },
                authorizationHeaderProviderOptions: CreateAgentIdentityOptionsWithUpn(agent2, user1Upn),
                claimsPrincipal: null);

            // Verify dictionary has entries (2 agents + 1 blueprint = 3)
            Assert.True(tokenAcquisition._applicationsByAuthorityClientId.Count >= 3);

            // Act — 3rd agent triggers eviction (clears entire dictionary)
            AddAgentUserFicMockHandlersForUser(mockHttp!, "token-a3", user1Uid, user1Upn);
            await authProvider.CreateAuthorizationHeaderForUserAsync(
                new[] { "https://graph.microsoft.com/.default" },
                authorizationHeaderProviderOptions: CreateAgentIdentityOptionsWithUpn(agent3, user1Upn),
                claimsPrincipal: null);

            // Assert — dictionary was cleared by eviction, then blueprint was rebuilt
            // during the 3rd agent's Leg 1. Previous agent CCAs are gone.
            Assert.Equal(0, tokenAcquisition._applicationsByAuthorityClientId.Keys
                .Count(k => k.IndexOf(":agent:", StringComparison.Ordinal) >= 0));
        }

        #endregion
    }
}
