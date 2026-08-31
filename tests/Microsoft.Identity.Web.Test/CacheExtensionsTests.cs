// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Web.Test.Common;
using Microsoft.Identity.Web.Test.Common.Mocks;
using Microsoft.Identity.Web.TokenCacheProviders;
using Microsoft.Identity.Web.TokenCacheProviders.Distributed;
using Microsoft.Identity.Web.TokenCacheProviders.InMemory;
using Microsoft.IdentityModel.Abstractions;
using NSubstitute;
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
        public async Task ParameterlessInMemoryCache_ReusesCacheAcrossApplicationsAsync()
        {
            AuthenticationResult result = await CreateAppAndGetTokenAsync();
            Assert.Equal(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
            AssertCacheTelemetry(result, CacheLevel.None);

            result = await CreateAppAndGetTokenAsync(addTokenMock: false);
            Assert.Equal(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);
            AssertCacheTelemetry(result, CacheLevel.L1Cache);
        }

        [Fact]
        public async Task ConfiguredDistributedCaches_UseDistinctProvidersAndBackendsAsync()
        {
            using MockHttpClientFactory firstHttpClient = new MockHttpClientFactory();
            using MockHttpClientFactory secondHttpClient = new MockHttpClientFactory();
            using MockHttpMessageHandler firstHandler = firstHttpClient.AddMockHandler(MockHttpCreator.CreateClientCredentialTokenHandler());
            using MockHttpMessageHandler secondHandler = secondHttpClient.AddMockHandler(MockHttpCreator.CreateClientCredentialTokenHandler());
            IConfidentialClientApplication firstApp = CreateCca("first-client", firstHttpClient);
            IConfidentialClientApplication secondApp = CreateCca("second-client", secondHttpClient);
            var firstCache = new TestDistributedCache();
            var secondCache = new TestDistributedCache();
            var caches = new Queue<TestDistributedCache>(new[] { firstCache, secondCache });
            var providers = new List<IMsalTokenCacheProvider>();
            Action<IServiceCollection> configure = services =>
            {
                services.AddSingleton<IDistributedCache>(caches.Dequeue());
                services.AddSingleton<IMsalTokenCacheProvider>(serviceProvider =>
                {
                    IMsalTokenCacheProvider provider =
                        ActivatorUtilities.CreateInstance<MsalDistributedTokenCacheAdapter>(serviceProvider);
                    providers.Add(provider);

                    return provider;
                });
            };

            firstApp.AddDistributedTokenCache(configure);
            secondApp.AddDistributedTokenCache(configure);

            AuthenticationResult firstResult = await firstApp.AcquireTokenForClient(new[] { TestConstants.s_scopeForApp }).ExecuteAsync();
            AuthenticationResult secondResult = await secondApp.AcquireTokenForClient(new[] { TestConstants.s_scopeForApp }).ExecuteAsync();

            Assert.Equal(2, providers.Count);
            Assert.NotSame(providers[0], providers[1]);
            Assert.Equal(TokenSource.IdentityProvider, firstResult.AuthenticationResultMetadata.TokenSource);
            Assert.Equal(TokenSource.IdentityProvider, secondResult.AuthenticationResultMetadata.TokenSource);
            Assert.Single(firstCache._dict);
            Assert.Single(secondCache._dict);
        }

        [Fact]
        public void ConfiguredInMemoryCaches_UseDistinctProvidersAndOptions()
        {
            IConfidentialClientApplication firstApp = CreateCca();
            IConfidentialClientApplication secondApp = CreateCca();
            TimeSpan firstExpiration = TimeSpan.FromMinutes(1);
            TimeSpan secondExpiration = TimeSpan.FromMinutes(2);
            var expirations = new Queue<TimeSpan>(new[] { firstExpiration, secondExpiration });
            var providers = new List<MsalMemoryTokenCacheProvider>();
            Action<IServiceCollection> configure = services =>
            {
                TimeSpan expiration = expirations.Dequeue();
                services.Configure<MsalMemoryTokenCacheOptions>(
                    options => options.AbsoluteExpirationRelativeToNow = expiration);
                services.AddSingleton<IMsalTokenCacheProvider>(serviceProvider =>
                {
                    var provider = ActivatorUtilities.CreateInstance<MsalMemoryTokenCacheProvider>(serviceProvider);
                    providers.Add(provider);

                    return provider;
                });
            };

            firstApp.AddInMemoryTokenCache(configure);
            secondApp.AddInMemoryTokenCache(configure);

            Assert.Equal(2, providers.Count);
            Assert.NotSame(providers[0], providers[1]);
            Assert.Equal(firstExpiration, providers[0].DetermineCacheEntryExpiry(new CacheSerializerHints()));
            Assert.Equal(secondExpiration, providers[1].DetermineCacheEntryExpiry(new CacheSerializerHints()));
        }

        [Fact]
        public void ConfiguredCache_InitializesAppAndUserCaches()
        {
            IConfidentialClientApplication app = CreateCca();
            IMsalTokenCacheProvider provider = Substitute.For<IMsalTokenCacheProvider>();

            app.AddInMemoryTokenCache(services => services.AddSingleton(provider));

            provider.Received(1).Initialize(app.UserTokenCache);
            provider.Received(1).Initialize(app.AppTokenCache);
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
        public async Task SingletonMsal_ResultsInCorrectCacheEntries_TestAsync()
        {
            var tenantId1 = "tenant1";
            var tenantId2 = "tenant2";
            var cacheKey1 = $"{TestConstants.ClientId}_{tenantId1}_AppTokenCache";
            var cacheKey2 = $"{TestConstants.ClientId}_{tenantId2}_AppTokenCache";

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
                    .ExecuteAsync();
                var result2 = await confidentialApp.AcquireTokenForClient(new[] { TestConstants.s_scopeForApp })
                    .WithTenantId(tenantId2)
                    .ExecuteAsync();

                Assert.Equal(TokenSource.IdentityProvider, result1.AuthenticationResultMetadata.TokenSource);
                Assert.Equal(TokenSource.IdentityProvider, result2.AuthenticationResultMetadata.TokenSource);
                Assert.Equal(2, distributedCache._dict.Count);
                Assert.Equal(distributedCache.Get(cacheKey1)!.Length, distributedCache.Get(cacheKey2)!.Length);
            }
        }

        #region CacheKeyExtensibility test
        private const int TokenCacheMemoryLimitInMb = 100;
        private static MemoryCache s_memoryCache = InitiatlizeMemoryCache();

        private static MemoryCache InitiatlizeMemoryCache()
        {
            // For 100 MB limit ... ~2KB per token entry means 50,000 entries
            var options = Options.Create(new MemoryCacheOptions() { SizeLimit = (TokenCacheMemoryLimitInMb / 2) * 1000 });
            var cache = new MemoryCache(options);

            return cache;
        }

        /// <summary>
        /// Token cache for MSAL based on MemoryCache, which can be partitioned by an additional key.
        /// For app tokens, the default key is ClientID + TenantID (and MSAL also looks for resource).
        /// </summary>
        private class PartitionedMsalTokenMemoryCacheProvider : MsalMemoryTokenCacheProvider
        {
            private readonly string? _cacheKeySuffix;

            /// <summary>
            /// Ctor
            /// </summary>
            /// <param name="memoryCache">A memory cache which can be configured for max size etc.</param>
            /// <param name="cacheOptions">Additional cache options, which canbe ignored for app tokens.</param>
            /// <param name="cachePartition">An aditional partition key. If let null, the original cache scoping is used (clientID, tenantID). MSAL also looks for resource.</param>
            public PartitionedMsalTokenMemoryCacheProvider(
                IMemoryCache memoryCache,
                IOptions<MsalMemoryTokenCacheOptions> cacheOptions,
                string? cachePartition) : base(memoryCache, cacheOptions)
            {
                _cacheKeySuffix = cachePartition;
            }

            public override string GetSuggestedCacheKey(TokenCacheNotificationArgs args)
            {
                return base.GetSuggestedCacheKey(args) + (_cacheKeySuffix ?? "");
            }
        }

        private async Task<AuthenticationResult> GetTokensAssociatedWithKeyAsync(string? cachePartition, bool expectCacheHit)
        {
            MockHttpMessageHandler? handler = null;
            MockHttpClientFactory? mockHttpClient = null;
            try
            {

                if (expectCacheHit == false)
                {
                    mockHttpClient = new MockHttpClientFactory();
                    handler = mockHttpClient.AddMockHandler(MockHttpCreator.CreateClientCredentialTokenHandler());
                }

                var msalMemoryTokenCacheProvider =
                    new PartitionedMsalTokenMemoryCacheProvider(
                        s_memoryCache,
                        Options.Create(new MsalMemoryTokenCacheOptions()),
                        cachePartition: cachePartition);

                var confidentialApp = ConfidentialClientApplicationBuilder
                                    .Create(TestConstants.ClientId)
                                    .WithAuthority(TestConstants.AuthorityCommonTenant)
                                    .WithHttpClientFactory(mockHttpClient)
                                    .WithInstanceDiscovery(false)
                                    .WithClientSecret(TestConstants.ClientSecret)
                                    .Build();

                await msalMemoryTokenCacheProvider.InitializeAsync(confidentialApp.AppTokenCache);

                AuthenticationResult result = await confidentialApp
                    .AcquireTokenForClient(["https://graph.microsoft.com/.default"])
                    .ExecuteAsync()
                    ;

                Assert.Equal(
                    expectCacheHit ?
                        TokenSource.Cache :
                        TokenSource.IdentityProvider,
                    result.AuthenticationResultMetadata.TokenSource);

                return result;

            }
            finally
            {
                handler?.Dispose();
                mockHttpClient?.Dispose();
            }
        }

        #endregion

        [Fact]
        public async Task CacheKeyExtensibilityAsync()
        {
            var result = await GetTokensAssociatedWithKeyAsync("foo", expectCacheHit: false);
            result = await GetTokensAssociatedWithKeyAsync("bar", expectCacheHit: false);
            result = await GetTokensAssociatedWithKeyAsync(null, expectCacheHit: false);

            result = await GetTokensAssociatedWithKeyAsync("foo", expectCacheHit: true);
            result = await GetTokensAssociatedWithKeyAsync("bar", expectCacheHit: true);
            result = await GetTokensAssociatedWithKeyAsync(null, expectCacheHit: true);
        }

        private static async Task<AuthenticationResult> CreateAppAndGetTokenAsync(
            bool addTokenMock = true)
        {
            using MockHttpClientFactory mockHttp = new MockHttpClientFactory();
            using var tokenHandler = MockHttpCreator.CreateClientCredentialTokenHandler();

            // for when the token is requested from ESTS
            if (addTokenMock)
            {
                mockHttp.AddMockHandler(tokenHandler);
            }

            var confidentialApp = ConfidentialClientApplicationBuilder
                           .Create(TestConstants.ClientId)
                           .WithAuthority(TestConstants.AuthorityCommonTenant)
                           .WithHttpClientFactory(mockHttp)
                           .WithClientSecret(TestConstants.ClientSecret)
                           .Build();

            confidentialApp.AddInMemoryTokenCache();

            var result = await confidentialApp.AcquireTokenForClient(new[] { TestConstants.s_scopeForApp })
                .ExecuteAsync();

            return result;
        }

        private void AssertCacheTelemetry(AuthenticationResult result, CacheLevel expectedCacheLevel)
        {
            Assert.Equal(result.AuthenticationResultMetadata.CacheLevel, expectedCacheLevel);
        }

        private IConfidentialClientApplication CreateCca() =>
                        ConfidentialClientApplicationBuilder
                           .Create(TestConstants.ClientId)
                           .WithAuthority(TestConstants.AuthorityCommonTenant)
                           .WithClientSecret(TestConstants.ClientSecret)
                           .Build();

        private static IConfidentialClientApplication CreateCca(
            string clientId,
            IMsalHttpClientFactory httpClientFactory) =>
                        ConfidentialClientApplicationBuilder
                           .Create(clientId)
                           .WithAuthority(TestConstants.AuthorityCommonTenant)
                           .WithHttpClientFactory(httpClientFactory)
                           .WithInstanceDiscovery(false)
                           .WithClientSecret(TestConstants.ClientSecret)
                           .Build();
    }
}
