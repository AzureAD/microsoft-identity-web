// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Web.Test.Common;
using Microsoft.Identity.Web.Test.Common.Mocks;
using Microsoft.Identity.Web.TokenCacheProviders.Distributed;
using Microsoft.IdentityModel.Abstractions;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    public class CacheExtensionsTests
    {
        [Fact]
        public void InMemoryCacheExtensionsTests()
        {
            var confidentialApp = CreateCca();
            confidentialApp.AddInMemoryTokenCache();

            Assert.NotNull(confidentialApp.UserTokenCache);
            Assert.NotNull(confidentialApp.AppTokenCache);
        }

        [Fact]
        public async Task CacheExtensions_CcaAlreadyExists_TestsAsync()
        {
            AuthenticationResult result;
            TestTelemetryClient testTelemetryClient = new TestTelemetryClient(TestConstants.ClientId);
            // new InMemory serializer and new cca
            result = await CreateAppAndGetTokenAsync(CacheType.InMemory, testTelemetryClient).ConfigureAwait(false);
            Assert.Equal(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
            AssertCacheTelemetry(testTelemetryClient, CacheLevel.None);

            result = await CreateAppAndGetTokenAsync(CacheType.InMemory, testTelemetryClient, addTokenMock: false).ConfigureAwait(false);
            Assert.Equal(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);
            AssertCacheTelemetry(testTelemetryClient, CacheLevel.L1Cache);

            //Resetting token caches due to potential collision with other tests
            TokenCacheExtensions.s_serviceProviderFromAction.Clear();

            // new DistributedInMemory and same cca
            result = await CreateAppAndGetTokenAsync(CacheType.DistributedInMemory, testTelemetryClient).ConfigureAwait(false);
            Assert.Equal(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
            AssertCacheTelemetry(testTelemetryClient, CacheLevel.None);

            result = await CreateAppAndGetTokenAsync(CacheType.DistributedInMemory, testTelemetryClient, addTokenMock: false).ConfigureAwait(false);
            Assert.Equal(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);
            AssertCacheTelemetry(testTelemetryClient, CacheLevel.L1Cache);
        }

        [Fact]
        public async Task CacheExtensions_CcaAlreadyExistsL2_TestsAsync()
        {
            AuthenticationResult result;
            TestTelemetryClient testTelemetryClient = new TestTelemetryClient(TestConstants.ClientId);
            // new DistributedInMemory serializer with L1 cache disabled
            result = await CreateAppAndGetTokenAsync(CacheType.DistributedInMemory, testTelemetryClient, disableL1Cache: true).ConfigureAwait(false);
            Assert.Equal(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
            AssertCacheTelemetry(testTelemetryClient, CacheLevel.None);

            result = await CreateAppAndGetTokenAsync(CacheType.DistributedInMemory, testTelemetryClient, addTokenMock: false, disableL1Cache: true).ConfigureAwait(false);
            Assert.Equal(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);
            AssertCacheTelemetry(testTelemetryClient, CacheLevel.L2Cache);
        }

        [Fact]
        public void InMemoryCacheExtensions_NoCca_ThrowsException_Tests()
        {
            IConfidentialClientApplication confidentialApp = null!;
            var ex = Assert.Throws<ArgumentNullException>(() => confidentialApp.AddInMemoryTokenCache());

            Assert.Equal("confidentialClientApp", ex.ParamName);
        }

        [Fact]
        public void InMemoryCache_WithServices_ExtensionsTests()
        {
            var confidentialApp = CreateCca();
            confidentialApp.AddInMemoryTokenCache(services =>
            {
                services.AddMemoryCache();
            });

            Assert.NotNull(confidentialApp.UserTokenCache);
            Assert.NotNull(confidentialApp.AppTokenCache);
        }

        [Fact]
        public void InMemoryCache_WithServices_NoCca_ThrowsException_Tests()
        {
            IConfidentialClientApplication confidentialApp = null!;
            var ex = Assert.Throws<ArgumentNullException>(() => confidentialApp.AddInMemoryTokenCache(services =>
            {
                services.AddMemoryCache();
            }));

            Assert.Equal("confidentialClientApp", ex.ParamName);
        }

        [Fact]
        public void InMemoryCache_WithServices_NoService_ThrowsException_Tests()
        {
            var confidentialApp = CreateCca();
            var ex = Assert.Throws<ArgumentNullException>(() => confidentialApp.AddInMemoryTokenCache(null!));

            Assert.Equal("initializeMemoryCache", ex.ParamName);
        }

        [Fact]
        public void DistributedCacheExtensionsTests()
        {
            var confidentialApp = CreateCca();
            confidentialApp.AddDistributedTokenCache(services =>
            {
                services.AddDistributedMemoryCache();
            });

            Assert.NotNull(confidentialApp.UserTokenCache);
            Assert.NotNull(confidentialApp.AppTokenCache);
        }

        [Fact]
        public void DistributedCacheExtensions_NoCca_ThrowsException_Tests()
        {
            IConfidentialClientApplication confidentialApp = null!;
            var ex = Assert.Throws<ArgumentNullException>(() => confidentialApp.AddDistributedTokenCache(services =>
            {
                services.AddDistributedMemoryCache();
            }));

            Assert.Equal("confidentialClientApp", ex.ParamName);
        }

        [Fact]
        public void DistributedCacheExtensions_NoService_ThrowsException_Tests()
        {
            var confidentialApp = CreateCca();
            var ex = Assert.Throws<ArgumentNullException>(() => confidentialApp.AddDistributedTokenCache(null!));

            Assert.Equal("initializeDistributedCache", ex.ParamName);
        }

        [Fact]
        public async Task SingletonMsal_ResultsInCorrectCacheEntries_Test()
        {
            var tenantId1 = "tenant1";
            var tenantId2 = "tenant2";
            var cacheKey1 = $"{TestConstants.ClientId}_{tenantId1}_AppTokenCache";
            var cacheKey2 = $"{TestConstants.ClientId}_{tenantId2}_AppTokenCache";

            //Resetting token caches due to potential collision with other tests
            TokenCacheExtensions.s_serviceProviderFromAction.Clear();

            using MockHttpClientFactory mockHttpClient = new MockHttpClientFactory();
            using (mockHttpClient.AddMockHandler(MockHttpCreator.CreateClientCredentialTokenHandler()))
            using (mockHttpClient.AddMockHandler(MockHttpCreator.CreateClientCredentialTokenHandler()))
            {
                var confidentialApp = ConfidentialClientApplicationBuilder
                               .Create(TestConstants.ClientId)
                               .WithAuthority(TestConstants.AuthorityCommonTenant)
                               .WithHttpClientFactory(mockHttpClient)
                               .WithInstanceDiscovery(false)
                               .WithClientSecret(TestConstants.ClientSecret)
                               .Build();

                var distributedCache = new TestDistributedCache();
                confidentialApp.AddDistributedTokenCache(services =>
                {
                    services.AddSingleton<IDistributedCache>(distributedCache);
                });

                // Different tenants used to created different cache entries
                var result1 = await confidentialApp.AcquireTokenForClient(new[] { TestConstants.s_scopeForApp })
                    .WithTenantId(tenantId1)
                    .ExecuteAsync().ConfigureAwait(false);
                var result2 = await confidentialApp.AcquireTokenForClient(new[] { TestConstants.s_scopeForApp })
                    .WithTenantId(tenantId2)
                    .ExecuteAsync().ConfigureAwait(false);

                Assert.Equal(TokenSource.IdentityProvider, result1.AuthenticationResultMetadata.TokenSource);
                Assert.Equal(TokenSource.IdentityProvider, result2.AuthenticationResultMetadata.TokenSource);
                Assert.Equal(2, distributedCache._dict.Count);
                Assert.Equal(distributedCache.Get(cacheKey1)!.Length, distributedCache.Get(cacheKey2)!.Length);
            }
        }

        private enum CacheType
        {
            InMemory,
            DistributedInMemory,
        }

        private static async Task<AuthenticationResult> CreateAppAndGetTokenAsync(
            CacheType cacheType,
            ITelemetryClient telemetryClient,
            bool addTokenMock = true,
            bool disableL1Cache = false)
        {
            using MockHttpClientFactory mockHttp = new MockHttpClientFactory();
            using var tokenHandler = MockHttpCreator.CreateClientCredentialTokenHandler();

            // for when the token is requested from ESTS
            if (addTokenMock)
            {
                mockHttp.AddMockHandler(tokenHandler);

                //Enables the mock handler to requeue requests that have been intercepted for instance discovery for example
                tokenHandler.ReplaceMockHttpMessageHandler = mockHttp.AddMockHandler;
            }

            var confidentialApp = ConfidentialClientApplicationBuilder
                           .Create(TestConstants.ClientId)
                           .WithAuthority(TestConstants.AuthorityCommonTenant)
                           .WithHttpClientFactory(mockHttp)
                           .WithClientSecret(TestConstants.ClientSecret)
                           .WithTelemetryClient(telemetryClient)
                           .Build();

            switch (cacheType)
            {
                case CacheType.InMemory:
                    confidentialApp.AddInMemoryTokenCache();
                    break;

                case CacheType.DistributedInMemory:
                    confidentialApp.AddDistributedTokenCache(services =>
                    {
                        services.AddDistributedMemoryCache();
                        services.AddLogging(configure => configure.AddConsole())
                        .Configure<LoggerFilterOptions>(options => options.MinLevel = Microsoft.Extensions.Logging.LogLevel.Warning);

                        if (disableL1Cache)
                        {
                            services.Configure<MsalDistributedTokenCacheAdapterOptions>(options =>
                            {
                                options.DisableL1Cache = true;
                            });
                        }
                    });
                    break;
            }

            var result = await confidentialApp.AcquireTokenForClient(new[] { TestConstants.s_scopeForApp })
                .ExecuteAsync().ConfigureAwait(false);

            tokenHandler.ReplaceMockHttpMessageHandler = null!;
            return result;
        }

        private void AssertCacheTelemetry(TestTelemetryClient testTelemetryClient, CacheLevel cacheLevel)
        {
            TelemetryEventDetails eventDetails = testTelemetryClient.TestTelemetryEventDetails;
            Assert.Equal(Convert.ToInt64(cacheLevel, new CultureInfo("en-US")), eventDetails.Properties["CacheLevel"]);
        }

        private IConfidentialClientApplication CreateCca() =>
                        ConfidentialClientApplicationBuilder
                           .Create(TestConstants.ClientId)
                           .WithAuthority(TestConstants.AuthorityCommonTenant)
                           .WithClientSecret(TestConstants.ClientSecret)
                           .Build();
    }
}
