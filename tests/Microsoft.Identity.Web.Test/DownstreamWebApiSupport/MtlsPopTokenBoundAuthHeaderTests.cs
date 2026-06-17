// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web.Test.Resource;
using Xunit;

namespace Microsoft.Identity.Web.Tests
{
    /// <summary>
    /// Validates that developers can configure the x-ms-tokenboundauth header
    /// required by Azure Key Vault for mTLS PoP via ExtraHeaderParameters.
    /// This is the recommended approach per IdWeb's extensibility model.
    /// </summary>
    public class MtlsPopTokenBoundAuthHeaderTests
    {
        private readonly DownstreamApi _downstreamApi;

        public MtlsPopTokenBoundAuthHeaderTests()
        {
            var authorizationHeaderProvider = new TestAuthorizationHeaderProvider();
            var httpClientFactory = new HttpClientFactoryTest();
            var namedOptions = new TestOptionsMonitor();
            var loggerFactory = new LoggerFactory();
            var logger = loggerFactory.CreateLogger<DownstreamApi>();
            var provider = new CredentialsProvider(
                loggerFactory.CreateLogger<CredentialsProvider>(),
                new DefaultCredentialsLoader(), [], null);

            _downstreamApi = new DownstreamApi(
                authorizationHeaderProvider,
                namedOptions,
                httpClientFactory,
                logger,
                msalHttpClientFactory: null,
                credentialsProvider: provider);
        }

        [Fact]
        public async Task ExtraHeaderParameters_TokenBoundAuth_AddsHeaderToAkvRequest()
        {
            // Arrange — simulates appsettings.json:
            //   "AzureKeyVault": {
            //     "BaseUrl": "https://myvault.vault.azure.net/",
            //     "ProtocolScheme": "MTLS_POP",
            //     "Scopes": [ "https://vault.azure.net/.default" ],
            //     "ExtraHeaderParameters": { "x-ms-tokenboundauth": "true" }
            //   }
            var httpRequestMessage = new HttpRequestMessage(
                HttpMethod.Get,
                "https://myvault.vault.azure.net/secrets/mysecret?api-version=7.4");

            var options = new DownstreamApiOptions
            {
                ExtraHeaderParameters = new Dictionary<string, string>
                {
                    { "x-ms-tokenboundauth", "true" }
                }
            };

            // Act
            await _downstreamApi.UpdateRequestWithCertificateAsync(
                httpRequestMessage, null, options, false, null, CancellationToken.None);

            // Assert
            Assert.True(
                httpRequestMessage.Headers.Contains("x-ms-tokenboundauth"),
                "x-ms-tokenboundauth header should be present on AKV request");
            Assert.Equal(
                "true",
                httpRequestMessage.Headers.GetValues("x-ms-tokenboundauth").Single());
        }

        [Fact]
        public void ExtraHeaderParameters_TokenBoundAuth_IsNotReserved()
        {
            // Verify x-ms-tokenboundauth is not in the reserved header list
            // (if it were, ExtraHeaderParameters would silently drop it)
            Assert.False(
                ReservedHeaderNames.IsReserved("x-ms-tokenboundauth"),
                "x-ms-tokenboundauth must not be reserved — developers need to set it via ExtraHeaderParameters");
        }

        [Fact]
        public async Task ExtraHeaderParameters_TokenBoundAuth_WorksWithSovereignCloudVaults()
        {
            // Arrange — China sovereign cloud AKV
            var httpRequestMessage = new HttpRequestMessage(
                HttpMethod.Get,
                "https://myvault.vault.azure.cn/secrets/mysecret?api-version=7.4");

            var options = new DownstreamApiOptions
            {
                ExtraHeaderParameters = new Dictionary<string, string>
                {
                    { "x-ms-tokenboundauth", "true" }
                }
            };

            // Act
            await _downstreamApi.UpdateRequestWithCertificateAsync(
                httpRequestMessage, null, options, false, null, CancellationToken.None);

            // Assert
            Assert.True(httpRequestMessage.Headers.Contains("x-ms-tokenboundauth"));
            Assert.Equal("true", httpRequestMessage.Headers.GetValues("x-ms-tokenboundauth").Single());
        }

        [Fact]
        public async Task CustomizeHttpRequestMessage_TokenBoundAuth_AddsHeaderDynamically()
        {
            // Arrange — demonstrates the code-based approach:
            //   options.CustomizeHttpRequestMessage = msg =>
            //       msg.Headers.TryAddWithoutValidation("x-ms-tokenboundauth", "true");
            var httpRequestMessage = new HttpRequestMessage(
                HttpMethod.Get,
                "https://myvault.vault.azure.net/secrets/mysecret?api-version=7.4");

            var options = new DownstreamApiOptions
            {
                CustomizeHttpRequestMessage = msg =>
                    msg.Headers.TryAddWithoutValidation("x-ms-tokenboundauth", "true")
            };

            // Act
            await _downstreamApi.UpdateRequestWithCertificateAsync(
                httpRequestMessage, null, options, false, null, CancellationToken.None);

            // Assert
            Assert.True(httpRequestMessage.Headers.Contains("x-ms-tokenboundauth"));
            Assert.Equal("true", httpRequestMessage.Headers.GetValues("x-ms-tokenboundauth").Single());
        }

        [Fact]
        public async Task ExtraHeaderParameters_TokenBoundAuth_DoesNotDuplicateIfAlreadyPresent()
        {
            // Arrange — header already on the request (e.g., set by a DelegatingHandler upstream)
            var httpRequestMessage = new HttpRequestMessage(
                HttpMethod.Get,
                "https://myvault.vault.azure.net/secrets/mysecret?api-version=7.4");
            httpRequestMessage.Headers.TryAddWithoutValidation("x-ms-tokenboundauth", "true");

            var options = new DownstreamApiOptions
            {
                ExtraHeaderParameters = new Dictionary<string, string>
                {
                    { "x-ms-tokenboundauth", "true" }
                }
            };

            // Act
            await _downstreamApi.UpdateRequestWithCertificateAsync(
                httpRequestMessage, null, options, false, null, CancellationToken.None);

            // Assert — should still be a single value, not duplicated
            Assert.Single(httpRequestMessage.Headers.GetValues("x-ms-tokenboundauth"));
        }

        [Fact]
        public async Task ExtraHeaderParameters_TokenBoundAuth_CaseInsensitiveDuplicateCheck()
        {
            // Arrange — header present with different casing (HTTP headers are case-insensitive)
            var httpRequestMessage = new HttpRequestMessage(
                HttpMethod.Get,
                "https://myvault.vault.azure.net/secrets/mysecret?api-version=7.4");
            httpRequestMessage.Headers.TryAddWithoutValidation("X-MS-TOKENBOUNDAUTH", "true");

            var options = new DownstreamApiOptions
            {
                ExtraHeaderParameters = new Dictionary<string, string>
                {
                    { "x-ms-tokenboundauth", "true" }
                }
            };

            // Act
            await _downstreamApi.UpdateRequestWithCertificateAsync(
                httpRequestMessage, null, options, false, null, CancellationToken.None);

            // Assert — Contains is case-insensitive for HTTP headers, so no duplicate
            Assert.Single(httpRequestMessage.Headers.GetValues("x-ms-tokenboundauth"));
        }

        [Fact]
        public async Task ExtraHeaderParameters_TokenBoundAuth_WorksWithUsGovCloud()
        {
            // Arrange — US Government cloud AKV
            var httpRequestMessage = new HttpRequestMessage(
                HttpMethod.Get,
                "https://myvault.vault.usgovcloudapi.net/secrets/mysecret?api-version=7.4");

            var options = new DownstreamApiOptions
            {
                ExtraHeaderParameters = new Dictionary<string, string>
                {
                    { "x-ms-tokenboundauth", "true" }
                }
            };

            // Act
            await _downstreamApi.UpdateRequestWithCertificateAsync(
                httpRequestMessage, null, options, false, null, CancellationToken.None);

            // Assert
            Assert.True(httpRequestMessage.Headers.Contains("x-ms-tokenboundauth"));
            Assert.Equal("true", httpRequestMessage.Headers.GetValues("x-ms-tokenboundauth").Single());
        }

        [Fact]
        public async Task ExtraHeaderParameters_MultipleHeaders_AllApplied()
        {
            // Arrange — realistic scenario: tokenboundauth + a custom correlation header
            var httpRequestMessage = new HttpRequestMessage(
                HttpMethod.Get,
                "https://myvault.vault.azure.net/secrets/mysecret?api-version=7.4");

            var options = new DownstreamApiOptions
            {
                ExtraHeaderParameters = new Dictionary<string, string>
                {
                    { "x-ms-tokenboundauth", "true" },
                    { "x-correlation-id", "test-run-001" }
                }
            };

            // Act
            await _downstreamApi.UpdateRequestWithCertificateAsync(
                httpRequestMessage, null, options, false, null, CancellationToken.None);

            // Assert — both headers present
            Assert.Equal("true", httpRequestMessage.Headers.GetValues("x-ms-tokenboundauth").Single());
            Assert.Equal("test-run-001", httpRequestMessage.Headers.GetValues("x-correlation-id").Single());
        }

        [Fact]
        public async Task ExtraHeaderParameters_ReservedHeader_IsIgnored()
        {
            // Arrange — Authorization is reserved; confirm it's silently skipped
            // while x-ms-tokenboundauth still goes through
            var httpRequestMessage = new HttpRequestMessage(
                HttpMethod.Get,
                "https://myvault.vault.azure.net/secrets/mysecret?api-version=7.4");

            var options = new DownstreamApiOptions
            {
                ExtraHeaderParameters = new Dictionary<string, string>
                {
                    { "Authorization", "Bearer should-be-ignored" },
                    { "x-ms-tokenboundauth", "true" }
                }
            };

            // Act
            await _downstreamApi.UpdateRequestWithCertificateAsync(
                httpRequestMessage, null, options, false, null, CancellationToken.None);

            // Assert — tokenboundauth applied, Authorization NOT applied via ExtraHeaders
            Assert.True(httpRequestMessage.Headers.Contains("x-ms-tokenboundauth"));
            // The reserved "Authorization" from ExtraHeaderParameters should be silently dropped.
            // httpRequestMessage.Headers.Authorization remains null because
            // UpdateRequestWithCertificateAsync does not set it (that happens later in the pipeline).
            Assert.Null(httpRequestMessage.Headers.Authorization);
        }

        [Fact]
        public async Task ExtraHeaderParameters_EmptyDictionary_NoHeaderAdded()
        {
            // Arrange — empty ExtraHeaderParameters should be a no-op
            var httpRequestMessage = new HttpRequestMessage(
                HttpMethod.Get,
                "https://myvault.vault.azure.net/secrets/mysecret?api-version=7.4");

            var options = new DownstreamApiOptions
            {
                ExtraHeaderParameters = new Dictionary<string, string>()
            };

            // Act
            await _downstreamApi.UpdateRequestWithCertificateAsync(
                httpRequestMessage, null, options, false, null, CancellationToken.None);

            // Assert — no x-ms-tokenboundauth header
            Assert.False(httpRequestMessage.Headers.Contains("x-ms-tokenboundauth"));
        }

        [Fact]
        public async Task CustomizeHttpRequestMessage_ConditionalLogic_OnlyAddsForVaultHosts()
        {
            // Arrange — demonstrates conditional header injection pattern
            // (for developers calling multiple resources from same service config)
            Action<HttpRequestMessage> conditionalCustomizer = msg =>
            {
                if (msg.RequestUri?.Host?.EndsWith(".vault.azure.net", StringComparison.OrdinalIgnoreCase) == true)
                {
                    msg.Headers.TryAddWithoutValidation("x-ms-tokenboundauth", "true");
                }
            };

            // Test with AKV URL — should add header
            var akvRequest = new HttpRequestMessage(HttpMethod.Get, "https://myvault.vault.azure.net/secrets/foo");
            var akvOptions = new DownstreamApiOptions { CustomizeHttpRequestMessage = conditionalCustomizer };
            await _downstreamApi.UpdateRequestWithCertificateAsync(akvRequest, null, akvOptions, false, null, CancellationToken.None);
            Assert.True(akvRequest.Headers.Contains("x-ms-tokenboundauth"));

            // Test with non-AKV URL — should NOT add header
            var armRequest = new HttpRequestMessage(HttpMethod.Get, "https://management.azure.com/subscriptions");
            var armOptions = new DownstreamApiOptions { CustomizeHttpRequestMessage = conditionalCustomizer };
            await _downstreamApi.UpdateRequestWithCertificateAsync(armRequest, null, armOptions, false, null, CancellationToken.None);
            Assert.False(armRequest.Headers.Contains("x-ms-tokenboundauth"));
        }

        // --- Full config shape tests: MSI + FIC with mTLS PoP for AKV ---

        [Fact]
        public async Task FullConfigShape_MsiMtlsPop_AzureKeyVault_HeaderApplied()
        {
            // Arrange — mirrors the exact appsettings.json structure a developer would use
            // for Managed Identity mTLS PoP against Azure Key Vault:
            //
            //   "AzureKeyVault": {
            //     "BaseUrl": "https://myvault.vault.azure.net/",
            //     "RelativePath": "secrets/mysecret?api-version=7.4",
            //     "RequestAppToken": true,
            //     "ProtocolScheme": "MTLS_POP",
            //     "Scopes": [ "https://vault.azure.net/.default" ],
            //     "AcquireTokenOptions": {
            //       "ManagedIdentity": {
            //         "UserAssignedClientId": "4b7a4b0b-ecb2-409e-879a-1e21a15ddaf6"
            //       }
            //     },
            //     "ExtraHeaderParameters": { "x-ms-tokenboundauth": "true" }
            //   }
            var httpRequestMessage = new HttpRequestMessage(
                HttpMethod.Get,
                "https://myvault.vault.azure.net/secrets/mysecret?api-version=7.4");

            var options = new DownstreamApiOptions
            {
                BaseUrl = "https://myvault.vault.azure.net/",
                RelativePath = "secrets/mysecret?api-version=7.4",
                RequestAppToken = true,
                ProtocolScheme = "MTLS_POP",
                Scopes = new[] { "https://vault.azure.net/.default" },
                AcquireTokenOptions = new AcquireTokenOptions
                {
                    ManagedIdentity = new ManagedIdentityOptions
                    {
                        UserAssignedClientId = "4b7a4b0b-ecb2-409e-879a-1e21a15ddaf6"
                    }
                },
                ExtraHeaderParameters = new Dictionary<string, string>
                {
                    { "x-ms-tokenboundauth", "true" }
                }
            };

            // Act
            await _downstreamApi.UpdateRequestWithCertificateAsync(
                httpRequestMessage, null, options, false, null, CancellationToken.None);

            // Assert — header is present regardless of token acquisition path
            Assert.True(httpRequestMessage.Headers.Contains("x-ms-tokenboundauth"));
            Assert.Equal("true", httpRequestMessage.Headers.GetValues("x-ms-tokenboundauth").Single());
        }

        [Fact]
        public async Task FullConfigShape_MsiMtlsPop_SystemAssigned_HeaderApplied()
        {
            // Arrange — System-assigned MSI (no UserAssignedClientId)
            //   "AcquireTokenOptions": { "ManagedIdentity": { } }
            var httpRequestMessage = new HttpRequestMessage(
                HttpMethod.Get,
                "https://myvault.vault.azure.net/secrets/mysecret?api-version=7.4");

            var options = new DownstreamApiOptions
            {
                BaseUrl = "https://myvault.vault.azure.net/",
                RequestAppToken = true,
                ProtocolScheme = "MTLS_POP",
                Scopes = new[] { "https://vault.azure.net/.default" },
                AcquireTokenOptions = new AcquireTokenOptions
                {
                    ManagedIdentity = new ManagedIdentityOptions() // system-assigned
                },
                ExtraHeaderParameters = new Dictionary<string, string>
                {
                    { "x-ms-tokenboundauth", "true" }
                }
            };

            // Act
            await _downstreamApi.UpdateRequestWithCertificateAsync(
                httpRequestMessage, null, options, false, null, CancellationToken.None);

            // Assert
            Assert.True(httpRequestMessage.Headers.Contains("x-ms-tokenboundauth"));
            Assert.Equal("true", httpRequestMessage.Headers.GetValues("x-ms-tokenboundauth").Single());
        }

        [Fact]
        public async Task FullConfigShape_FicMtlsPop_AzureKeyVault_HeaderApplied()
        {
            // Arrange — Federated Identity Credential (FIC) with mTLS PoP against AKV.
            // FIC uses SignedAssertionFromManagedIdentity as the credential source,
            // but the downstream call config is the same shape — ExtraHeaderParameters
            // applies identically regardless of how the token was acquired.
            //
            //   "AzureKeyVault": {
            //     "BaseUrl": "https://myvault.vault.azure.net/",
            //     "RequestAppToken": true,
            //     "ProtocolScheme": "MTLS_POP",
            //     "Scopes": [ "https://vault.azure.net/.default" ],
            //     "ExtraHeaderParameters": { "x-ms-tokenboundauth": "true" }
            //   }
            //
            //   Note: FIC credential source (SignedAssertionFromManagedIdentity) is
            //   configured under MicrosoftIdentityOptions.ClientCredentials, not here.
            var httpRequestMessage = new HttpRequestMessage(
                HttpMethod.Get,
                "https://myvault.vault.azure.net/secrets/mysecret?api-version=7.4");

            var options = new DownstreamApiOptions
            {
                BaseUrl = "https://myvault.vault.azure.net/",
                RequestAppToken = true,
                ProtocolScheme = "MTLS_POP",
                Scopes = new[] { "https://vault.azure.net/.default" },
                ExtraHeaderParameters = new Dictionary<string, string>
                {
                    { "x-ms-tokenboundauth", "true" }
                }
            };

            // Act
            await _downstreamApi.UpdateRequestWithCertificateAsync(
                httpRequestMessage, null, options, false, null, CancellationToken.None);

            // Assert — FIC or MSI, the header injection behavior is the same
            Assert.True(httpRequestMessage.Headers.Contains("x-ms-tokenboundauth"));
            Assert.Equal("true", httpRequestMessage.Headers.GetValues("x-ms-tokenboundauth").Single());
        }

        [Fact]
        public async Task FullConfigShape_MsiMtlsPop_NonVaultResource_NoHeaderNeeded()
        {
            // Arrange — ARM does NOT need x-ms-tokenboundauth (it binds cert on initial handshake).
            // This test documents that developers should NOT add ExtraHeaderParameters
            // for non-AKV resources.
            var httpRequestMessage = new HttpRequestMessage(
                HttpMethod.Get,
                "https://management.azure.com/subscriptions?api-version=2023-01-01");

            var options = new DownstreamApiOptions
            {
                BaseUrl = "https://management.azure.com/",
                RequestAppToken = true,
                ProtocolScheme = "MTLS_POP",
                Scopes = new[] { "https://management.azure.com/.default" },
                AcquireTokenOptions = new AcquireTokenOptions
                {
                    ManagedIdentity = new ManagedIdentityOptions
                    {
                        UserAssignedClientId = "4b7a4b0b-ecb2-409e-879a-1e21a15ddaf6"
                    }
                }
                // No ExtraHeaderParameters — ARM doesn't need it
            };

            // Act
            await _downstreamApi.UpdateRequestWithCertificateAsync(
                httpRequestMessage, null, options, false, null, CancellationToken.None);

            // Assert — header is NOT present (correct behavior for non-AKV)
            Assert.False(httpRequestMessage.Headers.Contains("x-ms-tokenboundauth"));
        }

        [Fact]
        public async Task FullConfigShape_MsiMtlsPop_SovereignClouds_AllHeaderApplied()
        {
            // Arrange — validate all sovereign cloud AKV endpoints work with the config
            var sovereignUrls = new[]
            {
                "https://myvault.vault.azure.cn/secrets/mysecret?api-version=7.4",        // China
                "https://myvault.vault.usgovcloudapi.net/secrets/mysecret?api-version=7.4" // US Gov
            };

            foreach (var url in sovereignUrls)
            {
                var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, url);

                var options = new DownstreamApiOptions
                {
                    RequestAppToken = true,
                    ProtocolScheme = "MTLS_POP",
                    Scopes = new[] { "https://vault.azure.net/.default" },
                    AcquireTokenOptions = new AcquireTokenOptions
                    {
                        ManagedIdentity = new ManagedIdentityOptions()
                    },
                    ExtraHeaderParameters = new Dictionary<string, string>
                    {
                        { "x-ms-tokenboundauth", "true" }
                    }
                };

                // Act
                await _downstreamApi.UpdateRequestWithCertificateAsync(
                    httpRequestMessage, null, options, false, null, CancellationToken.None);

                // Assert
                Assert.True(
                    httpRequestMessage.Headers.Contains("x-ms-tokenboundauth"),
                    $"Header should be present for sovereign URL: {url}");
            }
        }

        // --- Test infrastructure (follows ExtraParametersTests.cs pattern) ---

        private class TestAuthorizationHeaderProvider : IAuthorizationHeaderProvider
        {
            public Task<string> CreateAuthorizationHeaderForAppAsync(
                string scopes,
                AuthorizationHeaderProviderOptions? downstreamApiOptions = null,
                CancellationToken cancellationToken = default)
                => Task.FromResult("Bearer ey");

            public Task<string> CreateAuthorizationHeaderForUserAsync(
                IEnumerable<string> scopes,
                AuthorizationHeaderProviderOptions? authorizationHeaderProviderOptions = null,
                ClaimsPrincipal? claimsPrincipal = null,
                CancellationToken cancellationToken = default)
                => Task.FromResult("Bearer ey");

            public Task<string> CreateAuthorizationHeaderAsync(
                IEnumerable<string> scopes,
                AuthorizationHeaderProviderOptions? authorizationHeaderProviderOptions = null,
                ClaimsPrincipal? claimsPrincipal = null,
                CancellationToken cancellationToken = default)
                => Task.FromResult("Bearer ey");
        }

        private class TestOptionsMonitor : IOptionsMonitor<DownstreamApiOptions>
        {
            public DownstreamApiOptions CurrentValue => new DownstreamApiOptions();
            public DownstreamApiOptions Get(string? name) => new DownstreamApiOptions();
            public DownstreamApiOptions Get(string name, string key) => new DownstreamApiOptions();
            public IDisposable OnChange(Action<DownstreamApiOptions, string> listener)
                => throw new NotImplementedException();
        }
    }
}
