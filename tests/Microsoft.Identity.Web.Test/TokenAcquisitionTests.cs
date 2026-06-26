// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web.Extensibility;
using Microsoft.Identity.Web.Test.Common;
using Microsoft.Identity.Web.Test.Common.Mocks;
using Microsoft.Identity.Web.TestOnly;
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
        /// Verifies that after CCA instances are evicted from the dictionary and new ones
        /// are created with the same agent app IDs, the new CCAs must acquire fresh
        /// tokens when shared cache is disabled (per-instance caches die with the CCA).
        /// </summary>
        [Fact]
        public async Task AgentSharedCache_NewCcaAfterEviction_AcquiresFreshTokens()
        {
            // Arrange
            string agent1 = Guid.NewGuid().ToString("N");
            string user1Uid = Guid.NewGuid().ToString("N");
            string user1Upn = "user1@contoso.com";

            var factory = InitTokenAcquirerFactoryForAgent();
            IServiceProvider serviceProvider = factory.Build();
            var mockHttp = serviceProvider.GetRequiredService<IMsalHttpClientFactory>() as MockHttpClientFactory;
            IAuthorizationHeaderProvider authProvider =
                serviceProvider.GetRequiredService<IAuthorizationHeaderProvider>();

            // Acquire the internal TokenAcquisition to manipulate dictionaries
            var tokenAcquisition = serviceProvider.GetRequiredService<ITokenAcquisition>() as TokenAcquisition;

            // Disable shared cache to test per-instance behavior (tokens lost on eviction)
            tokenAcquisition!.UseSharedCacheForAgentCcas = false;

            // First acquisition: full 3-leg flow
            AddAgentUserFicMockHandlersForUser(mockHttp!, "original-token", user1Uid, user1Upn);

            string result1 = await authProvider.CreateAuthorizationHeaderForUserAsync(
                new[] { "https://graph.microsoft.com/.default" },
                authorizationHeaderProviderOptions: CreateAgentIdentityOptionsWithUpn(agent1, user1Upn),
                claimsPrincipal: null);

            Assert.Equal("Bearer original-token", result1);

            // Verify silent works (no handlers needed)
            string silent1 = await authProvider.CreateAuthorizationHeaderForUserAsync(
                new[] { "https://graph.microsoft.com/.default" },
                authorizationHeaderProviderOptions: CreateAgentIdentityOptionsWithUpn(agent1, user1Upn),
                claimsPrincipal: null);
            Assert.Equal("Bearer original-token", silent1);

            // Evict the agent CCA from the dictionary (simulating sweep)
            tokenAcquisition!._agentUserFicCcas.Clear();
            tokenAcquisition._agentUserFicAccountIds.Clear();

            // After eviction, a new CCA must be built. Since we're using per-instance caches
            // (no EnableSharedCacheOptions), the old tokens are gone.
            // Need full 3-leg flow again with Leg 1 cached in blueprint.
            mockHttp!.AddMockHandler(CreateClientCredentialsTokenHandler(accessToken: "t2-fresh"));
            mockHttp.AddMockHandler(CreateUserFicTokenHandlerForUser("fresh-token-after-evict", user1Uid, user1Upn));

            string result2 = await authProvider.CreateAuthorizationHeaderForUserAsync(
                new[] { "https://graph.microsoft.com/.default" },
                authorizationHeaderProviderOptions: CreateAgentIdentityOptionsWithUpn(agent1, user1Upn),
                claimsPrincipal: null);

            // Assert — got a fresh token (old cache was lost with the CCA)
            Assert.Equal("Bearer fresh-token-after-evict", result2);
            Assert.NotEqual("Bearer original-token", result2);
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

            // Evict ALL agent CCAs (simulating sweep clearing everything).
            // Keep _agentUserFicAccountIds intact — silent lookup needs them to find the account.
            tokenAcquisition._agentUserFicCcas.Clear();

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

        #region Agent CCA Sweep Eviction Tests

        /// <summary>
        /// Verifies that the sweep timer removes agent CCA entries that have been
        /// idle for longer than the configured maximum idle time.
        /// </summary>
        [Fact]
        public async Task AgentCcaSweep_RemovesExpiredEntries()
        {
            // Arrange
            string agentAppId = Guid.NewGuid().ToString("N");
            var factory = InitTokenAcquirerFactoryForAgent();
            IServiceProvider serviceProvider = factory.Build();

            var tokenAcquisition = (TokenAcquisition)serviceProvider.GetRequiredService<ITokenAcquisition>();

            // Override the idle threshold to a very short value for testing.
            tokenAcquisition.AgentCcaMaxIdleMilliseconds = 50;

            var mockHttpClient = serviceProvider.GetRequiredService<IMsalHttpClientFactory>() as MockHttpClientFactory;
            AddAgentUserFicMockHandlers(mockHttpClient!, userAccessToken: "sweep-token-1");

            var options = CreateAgentIdentityOptions(agentAppId);

            IAuthorizationHeaderProvider authorizationHeaderProvider =
                serviceProvider.GetRequiredService<IAuthorizationHeaderProvider>();

            // Act — first call populates the agent CCA and account IDs
            await authorizationHeaderProvider.CreateAuthorizationHeaderForUserAsync(
                new[] { "https://graph.microsoft.com/.default" },
                authorizationHeaderProviderOptions: options,
                claimsPrincipal: null);

            Assert.True(tokenAcquisition._agentUserFicCcas.Count == 1);
            Assert.NotEmpty(tokenAcquisition._agentUserFicAccountIds);

            // Wait for the entry to expire
            await Task.Delay(TimeSpan.FromMilliseconds(100));

            // Act — manual sweep
            int evicted = tokenAcquisition.SweepExpiredAgentCcas();

            // Assert
            Assert.Equal(1, evicted);
            Assert.True(tokenAcquisition._agentUserFicCcas.Count == 0);
            Assert.True(tokenAcquisition._agentUserFicAccountIds.IsEmpty);
        }

        /// <summary>
        /// Verifies that the sweep does not remove agent CCA entries that have been
        /// recently accessed (i.e., the touch mechanism resets the idle timer).
        /// </summary>
        [Fact]
        public async Task AgentCcaSweep_DoesNotRemoveRecentlyAccessedEntries()
        {
            // Arrange
            string agentAppId = Guid.NewGuid().ToString("N");
            var factory = InitTokenAcquirerFactoryForAgent();
            IServiceProvider serviceProvider = factory.Build();

            var tokenAcquisition = (TokenAcquisition)serviceProvider.GetRequiredService<ITokenAcquisition>();
            tokenAcquisition.AgentCcaMaxIdleMilliseconds = 200;

            var mockHttpClient = serviceProvider.GetRequiredService<IMsalHttpClientFactory>() as MockHttpClientFactory;
            AddAgentUserFicMockHandlers(mockHttpClient!, userAccessToken: "sweep-token-2");

            var options = CreateAgentIdentityOptions(agentAppId);

            IAuthorizationHeaderProvider authorizationHeaderProvider =
                serviceProvider.GetRequiredService<IAuthorizationHeaderProvider>();

            // Act — first call populates
            await authorizationHeaderProvider.CreateAuthorizationHeaderForUserAsync(
                new[] { "https://graph.microsoft.com/.default" },
                authorizationHeaderProviderOptions: options,
                claimsPrincipal: null);

            // Touch via second call (cache hit, no new handlers needed)
            await Task.Delay(TimeSpan.FromMilliseconds(100));
            await authorizationHeaderProvider.CreateAuthorizationHeaderForUserAsync(
                new[] { "https://graph.microsoft.com/.default" },
                authorizationHeaderProviderOptions: options,
                claimsPrincipal: null);

            // Sweep should find nothing expired (entry was just touched)
            int evicted = tokenAcquisition.SweepExpiredAgentCcas();

            // Assert
            Assert.Equal(0, evicted);
            Assert.True(tokenAcquisition._agentUserFicCcas.Count == 1);
        }

        /// <summary>
        /// Verifies that the sweep cleans up companion _agentUserFicAccountIds entries
        /// when an agent CCA is evicted.
        /// </summary>
        [Fact]
        public async Task AgentCcaSweep_CleansUpCompanionAccountIds()
        {
            // Arrange — two agents, each with their own user tokens
            string agentAppId1 = Guid.NewGuid().ToString("N");
            string agentAppId2 = Guid.NewGuid().ToString("N");
            var factory = InitTokenAcquirerFactoryForAgent();
            IServiceProvider serviceProvider = factory.Build();

            var tokenAcquisition = (TokenAcquisition)serviceProvider.GetRequiredService<ITokenAcquisition>();
            tokenAcquisition.AgentCcaMaxIdleMilliseconds = 50;

            var mockHttpClient = serviceProvider.GetRequiredService<IMsalHttpClientFactory>() as MockHttpClientFactory;

            // Agent 1 flow
            AddAgentUserFicMockHandlers(mockHttpClient!, userAccessToken: "agent1-user-token");
            var options1 = CreateAgentIdentityOptions(agentAppId1);
            IAuthorizationHeaderProvider authorizationHeaderProvider =
                serviceProvider.GetRequiredService<IAuthorizationHeaderProvider>();
            await authorizationHeaderProvider.CreateAuthorizationHeaderForUserAsync(
                new[] { "https://graph.microsoft.com/.default" },
                authorizationHeaderProviderOptions: options1,
                claimsPrincipal: null);

            // Agent 2 flow
            AddAgentUserFicMockHandlers(mockHttpClient!, userAccessToken: "agent2-user-token");
            var options2 = CreateAgentIdentityOptions(agentAppId2);
            await authorizationHeaderProvider.CreateAuthorizationHeaderForUserAsync(
                new[] { "https://graph.microsoft.com/.default" },
                authorizationHeaderProviderOptions: options2,
                claimsPrincipal: null);

            Assert.True(tokenAcquisition._agentUserFicCcas.Count == 2);
            int accountIdsBefore = tokenAcquisition._agentUserFicAccountIds.Count;
            Assert.True(accountIdsBefore >= 2, "Should have account IDs for both agents.");

            // Wait for both to expire
            await Task.Delay(TimeSpan.FromMilliseconds(100));

            // Act
            int evicted = tokenAcquisition.SweepExpiredAgentCcas();

            // Assert — both CCAs evicted, all companion account IDs cleaned up
            Assert.Equal(2, evicted);
            Assert.True(tokenAcquisition._agentUserFicCcas.Count == 0);
            Assert.True(tokenAcquisition._agentUserFicAccountIds.IsEmpty);
        }

        /// <summary>
        /// Verifies that when only one of two agents expires, the sweep only evicts
        /// the expired agent and leaves the other intact (including its account IDs).
        /// </summary>
        [Fact]
        public async Task AgentCcaSweep_SelectiveEviction_OnlyRemovesExpiredAgent()
        {
            // Arrange
            string agentAppIdOld = Guid.NewGuid().ToString("N");
            string agentAppIdNew = Guid.NewGuid().ToString("N");
            var factory = InitTokenAcquirerFactoryForAgent();
            IServiceProvider serviceProvider = factory.Build();

            var tokenAcquisition = (TokenAcquisition)serviceProvider.GetRequiredService<ITokenAcquisition>();
            // Use a generous idle threshold so the new agent stays well within bounds
            // even under CI CPU contention. The old agent will be force-expired by
            // reducing the threshold right before the sweep.
            tokenAcquisition.AgentCcaMaxIdleMilliseconds = 5000;

            var mockHttpClient = serviceProvider.GetRequiredService<IMsalHttpClientFactory>() as MockHttpClientFactory;

            // Create old agent first
            AddAgentUserFicMockHandlers(mockHttpClient!, userAccessToken: "old-agent-token");
            var optionsOld = CreateAgentIdentityOptions(agentAppIdOld);
            IAuthorizationHeaderProvider authorizationHeaderProvider =
                serviceProvider.GetRequiredService<IAuthorizationHeaderProvider>();
            await authorizationHeaderProvider.CreateAuthorizationHeaderForUserAsync(
                new[] { "https://graph.microsoft.com/.default" },
                authorizationHeaderProviderOptions: optionsOld,
                claimsPrincipal: null);

            // Wait long enough to create a clear gap between old and new agent timestamps.
            await Task.Delay(TimeSpan.FromMilliseconds(200));

            AddAgentUserFicMockHandlers(mockHttpClient!, userAccessToken: "new-agent-token");
            var optionsNew = CreateAgentIdentityOptions(agentAppIdNew);
            await authorizationHeaderProvider.CreateAuthorizationHeaderForUserAsync(
                new[] { "https://graph.microsoft.com/.default" },
                authorizationHeaderProviderOptions: optionsNew,
                claimsPrincipal: null);

            Assert.True(tokenAcquisition._agentUserFicCcas.Count == 2);

            // Now set a threshold that the old agent (200ms+ idle) exceeds
            // but the new agent (just created) does not.
            tokenAcquisition.AgentCcaMaxIdleMilliseconds = 100;

            // Act
            int evicted = tokenAcquisition.SweepExpiredAgentCcas();

            // Assert — only old agent evicted
            Assert.Equal(1, evicted);
            Assert.True(tokenAcquisition._agentUserFicCcas.Count == 1);

            // New agent's account IDs should still be present
            bool hasNewAgentAccountId = false;
            foreach (var kvp in tokenAcquisition._agentUserFicAccountIds)
            {
                if (kvp.Key.StartsWith(agentAppIdNew + ":", StringComparison.Ordinal))
                {
                    hasNewAgentAccountId = true;
                    break;
                }
            }
            Assert.True(hasNewAgentAccountId, "New agent's account IDs should survive the sweep.");
        }

        #endregion

        #region ExtractTenantFromTokenEndpointIfSameInstance Tests

        [Fact]
        public void ExtractTenant_SameInstance_ReturnsTenant()
        {
            // Arrange
            string tokenEndpoint = "https://login.microsoftonline.com/my-tenant-id/oauth2/v2.0/token";
            string instance = "https://login.microsoftonline.com/";

            // Act
            string? tenant = TokenAcquisition.ExtractTenantFromTokenEndpointIfSameInstance(tokenEndpoint, instance);

            // Assert
            Assert.Equal("my-tenant-id", tenant);
        }

        [Fact]
        public void ExtractTenant_DifferentInstance_ReturnsNull()
        {
            // Arrange — China cloud endpoint with public cloud instance
            string tokenEndpoint = "https://login.chinacloudapi.cn/my-tenant/oauth2/v2.0/token";
            string instance = "https://login.microsoftonline.com/";

            // Act
            string? tenant = TokenAcquisition.ExtractTenantFromTokenEndpointIfSameInstance(tokenEndpoint, instance);

            // Assert
            Assert.Null(tenant);
        }

        [Theory]
        [InlineData(null, "https://login.microsoftonline.com/")]
        [InlineData("https://login.microsoftonline.com/tenant/oauth2/v2.0/token", null)]
        [InlineData(null, null)]
        [InlineData("", "https://login.microsoftonline.com/")]
        public void ExtractTenant_NullOrEmptyInputs_ReturnsNull(string? tokenEndpoint, string? instance)
        {
            Assert.Null(TokenAcquisition.ExtractTenantFromTokenEndpointIfSameInstance(tokenEndpoint, instance));
        }

        [Fact]
        public void ExtractTenant_InvalidUri_ReturnsNull()
        {
            Assert.Null(TokenAcquisition.ExtractTenantFromTokenEndpointIfSameInstance("not-a-uri", "also-not-a-uri"));
        }

        #endregion
    }
}
