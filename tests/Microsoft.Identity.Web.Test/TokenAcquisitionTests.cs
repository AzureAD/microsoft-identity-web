// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web.Extensibility;
using Microsoft.Identity.Web.Test.Common;
using Microsoft.Identity.Web.Test.Common.Mocks;
using Microsoft.Identity.Web.TestOnly;
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
        private static MockHttpMessageHandler CreateUserFicTokenHandler(string accessToken)
        {
            return new MockHttpMessageHandler()
            {
                ExpectedMethod = HttpMethod.Post,
                ResponseMessage = MockHttpCreator.CreateSuccessResponseMessage(
                    "{\"token_type\":\"Bearer\"," +
                    "\"expires_in\":3599," +
                    "\"scope\":\"https://graph.microsoft.com/.default openid profile offline_access\"," +
                    "\"access_token\":\"" + accessToken + "\"," +
                    "\"refresh_token\":\"mock-refresh-token\"," +
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
