// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web.Test.Common;
using Microsoft.Identity.Web.Test.Common.Mocks;
using Microsoft.Identity.Web.TestOnly;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    /// <summary>
    /// Unit tests for vanilla dSTS (Dedicated Security Token Service) scenarios in Microsoft.Identity.Web.
    ///
    /// Vanilla dSTS uses a different authority format than AAD/Entra ID (eSTS), and must:
    ///   1. Skip the AAD instance discovery call (login.microsoftonline.com/common/discovery/instance).
    ///   2. POST the client credentials grant directly to the dSTS token endpoint:
    ///      https://{host}/dstsv2/{tenantGuid}/oauth2/v2.0/token
    ///   3. Send <c>x5c</c> in the client_assertion JWT header when <c>SendX5C=true</c>
    ///      (required for dSTS certificate-based authentication).
    ///
    /// Configuration model (per PR review feedback):
    ///   - dSTS users MUST configure <see cref="MicrosoftIdentityApplicationOptions.Instance"/>
    ///     and <see cref="MicrosoftIdentityApplicationOptions.TenantId"/> separately. This routes
    ///     the request through MSAL's <c>WithAuthority()</c> API, which is dSTS-compatible.
    ///   - The single-string <see cref="MicrosoftIdentityApplicationOptions.Authority"/> option is
    ///     reserved for vanilla OIDC / CIAM scenarios and routes through MSAL's
    ///     <c>WithOidcAuthority()</c> API, which is NOT compatible with dSTS. Configuring a
    ///     dSTS-style URL there now throws an <see cref="InvalidOperationException"/> with a
    ///     clear, actionable error message (see <see cref="DstsAuthorityViaAuthorityOption_ThrowsClearErrorAsync"/>).
    ///
    /// These tests use the existing <see cref="MockHttpClientFactory"/> infrastructure to mock
    /// the dSTS token endpoint, so no network/Key Vault/real certificate is required and the
    /// tests can run in any CI environment.
    /// </summary>
    [Collection(nameof(TokenAcquirerFactorySingletonProtection))]
    public class DstsTokenAcquisitionTests
    {
        // Vanilla dSTS authority pieces: https://{host}/dstsv2/{tenantGuid}
        // NOTE: all values below are synthetic placeholders for unit-test purposes only.
        // They do not correspond to any real Microsoft / Azure / dSTS deployment, tenant,
        // or application registration.
        private const string DstsHost = "fake-dsts.test.invalid";
        private const string DstsTenantId = "00000000-0000-0000-0000-000000000001";

        // Canonical dSTS configuration: Instance + TenantId, used separately.
        // Instance contains the literal "/dstsv2" path segment; TenantId is the GUID.
        private const string DstsInstance = "https://" + DstsHost + "/dstsv2";

        // Composite URL used only for assertions (expected request URI) and for the negative
        // test that verifies the unsupported single-Authority form is rejected.
        private const string DstsAuthorityFullUrl = "https://" + DstsHost + "/dstsv2/" + DstsTenantId;
        private const string DstsTokenEndpoint = DstsAuthorityFullUrl + "/oauth2/v2.0/token";

        // NOTE: each test uses a distinct ClientId so that they get distinct MSAL app token
        // caches. MSAL's confidential-client app token cache is keyed by ClientId/Authority and
        // is preserved across TokenAcquirerFactory resets, which would otherwise cause a token
        // acquired by one test to be served (from cache) to another test that registered a
        // different mock HTTP handler — making mock handlers unused and Dispose assertions fail.
        private const string DefaultDstsClientId = "00000000-0000-0000-0000-00000000c11d";
        private const string DstsScope = "https://" + DstsHost + "/.default";

        private static string NewDstsClientId() => Guid.NewGuid().ToString();

        /// <summary>
        /// Verifies that for a vanilla dSTS authority Id.Web/MSAL POSTs the client_credentials
        /// grant to the dSTS token endpoint (and not to the AAD eSTS endpoint).
        /// Uses <see cref="MockHttpMessageHandler.ExpectedUrl"/> to lock the endpoint.
        /// </summary>
        [Fact]
        public async Task GetAccessTokenForApp_DstsAuthority_PostsToDstsTokenEndpointAsync()
        {
            // Arrange
            var tokenAcquirerFactory = InitDstsTokenAcquirerFactoryWithSecret(NewDstsClientId());
            IServiceProvider serviceProvider = tokenAcquirerFactory.Build();
            var mockHttpClient = serviceProvider.GetRequiredService<IMsalHttpClientFactory>() as MockHttpClientFactory;

            var tokenHandler = MockHttpCreator.CreateClientCredentialTokenHandler();
            tokenHandler.ExpectedUrl = DstsTokenEndpoint;
            mockHttpClient!.AddMockHandler(tokenHandler);

            IAuthorizationHeaderProvider authorizationHeaderProvider =
                serviceProvider.GetRequiredService<IAuthorizationHeaderProvider>();

            // Act
            string result = await authorizationHeaderProvider.CreateAuthorizationHeaderForAppAsync(DstsScope);

            // Assert
            Assert.Equal("Bearer header.payload.signature", result);
            Assert.NotNull(tokenHandler.ActualRequestMessage);
            Assert.Equal(HttpMethod.Post, tokenHandler.ActualRequestMessage.Method);
            Assert.NotNull(tokenHandler.ActualRequestMessage.RequestUri);
            Assert.Equal(DstsTokenEndpoint, tokenHandler.ActualRequestMessage.RequestUri!.GetLeftPart(UriPartial.Path));
        }

        /// <summary>
        /// Verifies that the client_credentials grant body sent to dSTS contains the expected
        /// parameters (grant_type, scope, client_id, client_secret).
        /// </summary>
        [Fact]
        public async Task GetAccessTokenForApp_DstsAuthority_SendsClientCredentialsGrantAsync()
        {
            // Arrange
            string clientId = NewDstsClientId();
            var tokenAcquirerFactory = InitDstsTokenAcquirerFactoryWithSecret(clientId);
            IServiceProvider serviceProvider = tokenAcquirerFactory.Build();
            var mockHttpClient = serviceProvider.GetRequiredService<IMsalHttpClientFactory>() as MockHttpClientFactory;

            mockHttpClient!.AddMockHandler(MockHttpCreator.CreateHandlerToValidatePostData(
                HttpMethod.Post,
                new Dictionary<string, string>
                {
                    { "grant_type", "client_credentials" },
                    { "scope", DstsScope },
                    { "client_id", clientId },
                    { "client_secret", "someSecret" },
                }));

            IAuthorizationHeaderProvider authorizationHeaderProvider =
                serviceProvider.GetRequiredService<IAuthorizationHeaderProvider>();

            // Act
            string result = await authorizationHeaderProvider.CreateAuthorizationHeaderForAppAsync(DstsScope);

            // Assert - if any expected POST field were missing or different, MockHttpMessageHandler
            // would have failed inside SendAsync via Assert.Equal/Assert.True.
            Assert.Equal("Bearer header.payload.signature", result);
        }

        /// <summary>
        /// Verifies that two consecutive token acquisitions for the same dSTS scope only hit the
        /// dSTS token endpoint once (i.e. the second call is served from MSAL's app token cache).
        /// We register exactly one mock handler — if MSAL tried to call dSTS a second time, the
        /// queue would be empty and the request would throw.
        /// </summary>
        [Fact]
        public async Task GetAccessTokenForApp_DstsAuthority_SecondCallUsesCacheAsync()
        {
            // Arrange
            var tokenAcquirerFactory = InitDstsTokenAcquirerFactoryWithSecret(NewDstsClientId());
            IServiceProvider serviceProvider = tokenAcquirerFactory.Build();
            var mockHttpClient = serviceProvider.GetRequiredService<IMsalHttpClientFactory>() as MockHttpClientFactory;

            // Register exactly ONE token-endpoint handler.
            var tokenHandler = MockHttpCreator.CreateClientCredentialTokenHandler();
            mockHttpClient!.AddMockHandler(tokenHandler);

            IAuthorizationHeaderProvider authorizationHeaderProvider =
                serviceProvider.GetRequiredService<IAuthorizationHeaderProvider>();

            // Act - first call hits dSTS, second call should be a cache hit.
            string first = await authorizationHeaderProvider.CreateAuthorizationHeaderForAppAsync(DstsScope);
            string second = await authorizationHeaderProvider.CreateAuthorizationHeaderForAppAsync(DstsScope);

            // Assert
            Assert.Equal("Bearer header.payload.signature", first);
            Assert.Equal(first, second);

            // The single handler MUST have been consumed by the first call.
            Assert.NotNull(tokenHandler.ActualRequestMessage);

            // And the second call MUST have come from MSAL's app token cache: if it had hit the
            // network, MockHttpClientFactory would have thrown ("no more mock handlers")
            // because we only registered one. Additionally, MockHttpClientFactory.Dispose
            // asserts that the queue is empty (i.e. exactly the one handler was consumed).
        }

        /// <summary>
        /// Verifies that when the dSTS token endpoint returns an OAuth2 error, Id.Web surfaces it
        /// as <see cref="MsalServiceException"/>.
        /// </summary>
        [Fact]
        public async Task GetAccessTokenForApp_DstsAuthority_TokenEndpointError_ThrowsMsalServiceExceptionAsync()
        {
            // Arrange
            var tokenAcquirerFactory = InitDstsTokenAcquirerFactoryWithSecret(NewDstsClientId());
            IServiceProvider serviceProvider = tokenAcquirerFactory.Build();
            var mockHttpClient = serviceProvider.GetRequiredService<IMsalHttpClientFactory>() as MockHttpClientFactory;

            const string errorBody =
                "{\"error\":\"invalid_scope\"," +
                "\"error_description\":\"The scope is not valid for the dSTS resource.\"}";

            mockHttpClient!.AddMockHandler(new MockHttpMessageHandler
            {
                ExpectedMethod = HttpMethod.Post,
                ResponseMessage = new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent(errorBody),
                },
            });

            IAuthorizationHeaderProvider authorizationHeaderProvider =
                serviceProvider.GetRequiredService<IAuthorizationHeaderProvider>();

            // Act & Assert
            var ex = await Assert.ThrowsAsync<MsalServiceException>(
                async () => await authorizationHeaderProvider.CreateAuthorizationHeaderForAppAsync(DstsScope));

            Assert.Equal("invalid_scope", ex.ErrorCode);
        }

        /// <summary>
        /// Verifies that when a dSTS app is configured with a certificate credential and
        /// <see cref="MicrosoftIdentityApplicationOptions.SendX5C"/> is true, the JWT client_assertion
        /// header sent to the dSTS token endpoint includes the <c>x5c</c> claim. This is required
        /// for dSTS to validate the certificate chain.
        /// </summary>
        [Fact]
        public async Task GetAccessTokenForApp_DstsAuthority_WithCertificateAndSendX5C_IncludesX5CHeaderAsync()
        {
            // Arrange
            var tokenAcquirerFactory = InitDstsTokenAcquirerFactoryWithCertificate(sendX5C: true);
            IServiceProvider serviceProvider = tokenAcquirerFactory.Build();
            var mockHttpClient = serviceProvider.GetRequiredService<IMsalHttpClientFactory>() as MockHttpClientFactory;

            var tokenHandler = MockHttpCreator.CreateClientCredentialTokenHandler();
            mockHttpClient!.AddMockHandler(tokenHandler);

            IAuthorizationHeaderProvider authorizationHeaderProvider =
                serviceProvider.GetRequiredService<IAuthorizationHeaderProvider>();

            // Act
            string result = await authorizationHeaderProvider.CreateAuthorizationHeaderForAppAsync(DstsScope);

            // Assert
            Assert.Equal("Bearer header.payload.signature", result);
            Assert.NotNull(tokenHandler.ActualRequestPostData);

            // dSTS certificate auth uses client_assertion (JWT signed by the cert).
            Assert.True(tokenHandler.ActualRequestPostData.ContainsKey("client_assertion"),
                "Expected client_assertion in the POST body for certificate-based dSTS auth.");
            Assert.Equal(
                "urn:ietf:params:oauth:client-assertion-type:jwt-bearer",
                tokenHandler.ActualRequestPostData["client_assertion_type"]);

            string clientAssertion = tokenHandler.ActualRequestPostData["client_assertion"];
            string jwtHeader = DecodeJwtHeader(clientAssertion);

            // SendX5C=true must propagate the x5c chain into the JWT header.
            Assert.Contains("\"x5c\"", jwtHeader, StringComparison.Ordinal);
        }

        /// <summary>
        /// Negative counterpart to the previous test: when SendX5C=false, the JWT header
        /// must NOT contain the x5c chain (only x5t/kid).
        /// </summary>
        [Fact]
        public async Task GetAccessTokenForApp_DstsAuthority_WithCertificateAndNoSendX5C_OmitsX5CHeaderAsync()
        {
            // Arrange
            var tokenAcquirerFactory = InitDstsTokenAcquirerFactoryWithCertificate(sendX5C: false);
            IServiceProvider serviceProvider = tokenAcquirerFactory.Build();
            var mockHttpClient = serviceProvider.GetRequiredService<IMsalHttpClientFactory>() as MockHttpClientFactory;

            var tokenHandler = MockHttpCreator.CreateClientCredentialTokenHandler();
            mockHttpClient!.AddMockHandler(tokenHandler);

            IAuthorizationHeaderProvider authorizationHeaderProvider =
                serviceProvider.GetRequiredService<IAuthorizationHeaderProvider>();

            // Act
            string result = await authorizationHeaderProvider.CreateAuthorizationHeaderForAppAsync(DstsScope);

            // Assert
            Assert.Equal("Bearer header.payload.signature", result);
            Assert.NotNull(tokenHandler.ActualRequestPostData);
            Assert.True(tokenHandler.ActualRequestPostData.ContainsKey("client_assertion"));

            string clientAssertion = tokenHandler.ActualRequestPostData["client_assertion"];
            string jwtHeader = DecodeJwtHeader(clientAssertion);

            Assert.DoesNotContain("\"x5c\"", jwtHeader, StringComparison.Ordinal);
        }

        /// <summary>
        /// Negative test: configuring a dSTS-style URL via the single
        /// <see cref="MicrosoftIdentityApplicationOptions.Authority"/> option (instead of the
        /// canonical <c>Instance</c> + <c>TenantId</c> pair) must throw a clear, actionable
        /// <see cref="InvalidOperationException"/> with a message that points users to the
        /// correct configuration shape — rather than letting MSAL surface its opaque
        /// <c>"The DSTS authority URI should have at least 2 segments..."</c> error later.
        /// </summary>
        [Fact]
        public async Task DstsAuthorityViaAuthorityOption_ThrowsClearErrorAsync()
        {
            // Arrange
            TokenAcquirerFactoryTesting.ResetTokenAcquirerFactoryInTest();
            TokenAcquirerFactory tokenAcquirerFactory = TokenAcquirerFactory.GetDefaultInstance();
            tokenAcquirerFactory.Services.Configure<MicrosoftIdentityApplicationOptions>(options =>
            {
                // ⚠️ Unsupported configuration shape for dSTS: the single composite Authority URL.
                // Id.Web should reject this with a clear error and tell the user to use
                // Instance + TenantId instead.
                options.Authority = DstsAuthorityFullUrl;
                options.ClientId = NewDstsClientId();
                options.ClientCredentials = new[]
                {
                    new CredentialDescription
                    {
                        SourceType = CredentialSource.ClientSecret,
                        ClientSecret = "someSecret",
                    },
                };
            });
            tokenAcquirerFactory.Services.AddSingleton<IMsalHttpClientFactory, MockHttpClientFactory>();

            IServiceProvider serviceProvider = tokenAcquirerFactory.Build();
            IAuthorizationHeaderProvider authorizationHeaderProvider =
                serviceProvider.GetRequiredService<IAuthorizationHeaderProvider>();

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await authorizationHeaderProvider.CreateAuthorizationHeaderForAppAsync(DstsScope));

            // The error message must mention both the unsupported option and the canonical fix
            // so the developer can act on it without having to dig into Id.Web internals.
            Assert.Contains("Authority", ex.Message, StringComparison.Ordinal);
            Assert.Contains("Instance", ex.Message, StringComparison.Ordinal);
            Assert.Contains("TenantId", ex.Message, StringComparison.Ordinal);
            Assert.Contains("dSTS", ex.Message, StringComparison.Ordinal);
        }

        // ---- helpers ----

        /// <summary>
        /// Builds a <see cref="TokenAcquirerFactory"/> configured for vanilla dSTS using the
        /// canonical <see cref="MicrosoftIdentityApplicationOptions.Instance"/> +
        /// <see cref="MicrosoftIdentityApplicationOptions.TenantId"/> configuration shape, with
        /// a client-secret credential. This is the configuration shape dSTS users MUST use —
        /// the single-Authority shape is rejected by <c>MergedOptions.ParseAuthorityIfNecessary</c>.
        /// </summary>
        private static TokenAcquirerFactory InitDstsTokenAcquirerFactoryWithSecret(string? clientId = null)
        {
            string effectiveClientId = clientId ?? DefaultDstsClientId;
            TokenAcquirerFactoryTesting.ResetTokenAcquirerFactoryInTest();
            TokenAcquirerFactory tokenAcquirerFactory = TokenAcquirerFactory.GetDefaultInstance();
            tokenAcquirerFactory.Services.Configure<MicrosoftIdentityApplicationOptions>(options =>
            {
                // Canonical dSTS configuration: Instance and TenantId are set separately.
                // This routes through MSAL.WithAuthority() (dSTS-compatible) rather than
                // MSAL.WithOidcAuthority() (vanilla OIDC / CIAM only, NOT dSTS-compatible).
                options.Instance = DstsInstance;     // "https://{host}/dstsv2"
                options.TenantId = DstsTenantId;
                options.ClientId = effectiveClientId;
                options.ClientCredentials = new[]
                {
                    new CredentialDescription
                    {
                        SourceType = CredentialSource.ClientSecret,
                        ClientSecret = "someSecret",
                    },
                };
            });

            tokenAcquirerFactory.Services.AddSingleton<IMsalHttpClientFactory, MockHttpClientFactory>();

            return tokenAcquirerFactory;
        }

        /// <summary>
        /// Builds a <see cref="TokenAcquirerFactory"/> configured for vanilla dSTS with a
        /// (self-signed) certificate credential, using the canonical
        /// <c>Instance</c> + <c>TenantId</c> configuration shape. The mock HTTP handler does not
        /// validate the certificate, so a self-signed cert is sufficient for unit tests.
        /// </summary>
        private static TokenAcquirerFactory InitDstsTokenAcquirerFactoryWithCertificate(bool sendX5C, string? clientId = null)
        {
            string effectiveClientId = clientId ?? NewDstsClientId();
            TokenAcquirerFactoryTesting.ResetTokenAcquirerFactoryInTest();
            TokenAcquirerFactory tokenAcquirerFactory = TokenAcquirerFactory.GetDefaultInstance();
            tokenAcquirerFactory.Services.Configure<MicrosoftIdentityApplicationOptions>(options =>
            {
                options.Instance = DstsInstance;
                options.TenantId = DstsTenantId;
                options.ClientId = effectiveClientId;
                options.SendX5C = sendX5C;
                options.ClientCredentials = new[]
                {
                    CertificateDescription.FromCertificate(CreateTestCertificate()),
                };
            });

            tokenAcquirerFactory.Services.AddSingleton<IMsalHttpClientFactory, MockHttpClientFactory>();

            return tokenAcquirerFactory;
        }

        /// <summary>
        /// Creates a transient self-signed certificate for unit tests. The mock HTTP handler does
        /// not perform any cryptographic validation against dSTS, so a throwaway cert is fine.
        /// </summary>
        private static X509Certificate2 CreateTestCertificate()
        {
            using var rsa = RSA.Create(2048);
            var request = new CertificateRequest(
                "CN=DstsUnitTest",
                rsa,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);

            return request.CreateSelfSigned(
                DateTimeOffset.UtcNow.AddDays(-1),
                DateTimeOffset.UtcNow.AddDays(365));
        }

        /// <summary>
        /// Decodes the (base64url-encoded) header of a JWT and returns it as a UTF-8 JSON string.
        /// </summary>
        private static string DecodeJwtHeader(string jwt)
        {
            var parts = jwt.Split('.');
            if (parts.Length < 2)
            {
                return string.Empty;
            }

            string base64 = parts[0].Replace('-', '+').Replace('_', '/');
            switch (base64.Length % 4)
            {
                case 2: base64 += "=="; break;
                case 3: base64 += "="; break;
            }

            return System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(base64));
        }
    }
}