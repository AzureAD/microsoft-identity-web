// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.TelemetryCore.TelemetryClient;
using Microsoft.Identity.Web.Test.Common.TestHelpers;
using Microsoft.Identity.Web.TokenCacheProviders;
using Microsoft.Identity.Web.TokenCacheProviders.Distributed;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    public class L1L2CacheTests
    {
        private const string DefaultCacheKey = "default-key";
        private const string AnotherCacheKey = "another-key";
        private ServiceProvider? _provider;
        private ServiceProvider Provider { get { return _provider!; } }
        private readonly TestMsalDistributedTokenCacheAdapter _testCacheAdapter;

        private TestDistributedCache L2Cache
        {
            get { return (_testCacheAdapter._distributedCache as TestDistributedCache)!; }
        }

        public L1L2CacheTests()
        {
            BuildTheRequiredServices();
            _testCacheAdapter = new TestMsalDistributedTokenCacheAdapter(
                MakeMockDistributedCache(),
                Provider.GetService<IOptions<MsalDistributedTokenCacheAdapterOptions>>()!,
                Provider.GetService<ILogger<MsalDistributedTokenCacheAdapter>>()!);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task WriteCache_WritesInL1L2_TestAsync(bool enableAsyncL2Write)
        {
            // Arrange
            Provider.GetService<IOptions<MsalDistributedTokenCacheAdapterOptions>>()!.Value.EnableAsyncL2Write = enableAsyncL2Write;
            byte[] cache = new byte[3];
            AssertCacheValues(_testCacheAdapter);
            Assert.Equal(0, _testCacheAdapter._memoryCache!.Count);
            Assert.Empty(L2Cache._dict);

            // Act
            TestDistributedCache.ResetEvent.Reset();
            await _testCacheAdapter.TestWriteCacheBytesAsync(DefaultCacheKey, cache);

            // Assert
            Assert.Equal(1, _testCacheAdapter._memoryCache.Count);
            TestDistributedCache.ResetEvent.Wait();
            Assert.Single(L2Cache._dict);
        }

        [Fact]
        public async Task WriteCache_NegativeExpiry_TestAsync()
        {
            // Arrange & Act
            await CreateL1L2TestWithSerializerHintsAsync(System.DateTimeOffset.Now - System.TimeSpan.FromHours(1), 0);

            // Assert
            Assert.NotNull(_testCacheAdapter._memoryCache);
            Assert.Null(_testCacheAdapter._memoryCache.Get(DefaultCacheKey));

            await _testCacheAdapter.TestReadCacheBytesAsync(DefaultCacheKey);
            Assert.Equal(1, _testCacheAdapter._memoryCache.Count);
        }

        [Fact]
        public async Task WriteCacheL1L2_NegativeExpiry_TestAsync()
        {
            // Arrange & Act
            await CreateL1L2TestWithSerializerHintsAsync(System.DateTimeOffset.Now - System.TimeSpan.FromHours(1), 0);

            // Assert
            Assert.NotNull(_testCacheAdapter._memoryCache);
            Assert.Null(_testCacheAdapter._memoryCache.Get(DefaultCacheKey));
            var options = (_testCacheAdapter._distributedCache as TestDistributedCache)!.GetDistributedCacheEntryOptions(DefaultCacheKey);
            Assert.NotNull(options);
            Assert.NotNull(options.AbsoluteExpiration);
            await _testCacheAdapter.TestReadCacheBytesAsync(DefaultCacheKey);
            Assert.Equal(1, _testCacheAdapter._memoryCache.Count);
        }

        [Fact]
        public async Task WriteCacheL1L2_PositiveExpiry_TestAsync()
        {
            // Arrange & Act
            var timespan = System.TimeSpan.FromHours(1);
            var expiry = System.DateTimeOffset.UtcNow + timespan;
            await CreateL1L2TestWithSerializerHintsAsync(expiry, 1);

            // Assert
            Assert.NotNull(_testCacheAdapter._memoryCache);
            Assert.NotNull(_testCacheAdapter._memoryCache.Get(DefaultCacheKey));
            TestDistributedCache? testDistributedCache = _testCacheAdapter._distributedCache as TestDistributedCache;
            Assert.NotNull(testDistributedCache);
            var options = testDistributedCache.GetDistributedCacheEntryOptions(DefaultCacheKey);
            Assert.NotNull(options);
            Assert.NotNull(options.AbsoluteExpiration);
            Assert.Equal(expiry, options.AbsoluteExpiration.Value);
            await _testCacheAdapter.TestReadCacheBytesAsync(DefaultCacheKey);
            Assert.Equal(1, _testCacheAdapter._memoryCache.Count);
        }

        [Fact]
        public async Task WriteCacheL1L2_PositiveExpiryAndAbsoluteOptions_TestAsync()
        {
            await CreateL1L2TestWithAbsoluteOptionsAsync(1.5);
        }

        [Fact]
        public async Task WriteCacheL1L2_PositiveExpiryAndAbsoluteOptionsLessThanSuggestedExpiry_TestAsync()
        {
            await CreateL1L2TestWithAbsoluteOptionsAsync(.5);
        }

        private async Task CreateL1L2TestWithAbsoluteOptionsAsync(double time)
        {
            // Arrange & Act
            var timespan = System.TimeSpan.FromHours(1);
            var suggestedExpiry = System.DateTimeOffset.UtcNow + timespan;
            var absoluteOptions = Provider.GetService<IOptions<MsalDistributedTokenCacheAdapterOptions>>();
            Assert.NotNull(absoluteOptions);
            absoluteOptions.Value.AbsoluteExpiration = System.DateTimeOffset.Now + System.TimeSpan.FromHours(time);
            await CreateL1L2TestWithSerializerHintsAsync(suggestedExpiry, 1);

            // Assert
            Assert.NotNull(_testCacheAdapter._memoryCache);
            Assert.NotNull(_testCacheAdapter._memoryCache.Get(DefaultCacheKey));
            Assert.NotNull(_testCacheAdapter._distributedCache as TestDistributedCache);
            var options = (_testCacheAdapter._distributedCache as TestDistributedCache)!.GetDistributedCacheEntryOptions(DefaultCacheKey);
            Assert.NotNull(options);
            Assert.NotNull(options.AbsoluteExpiration);
            if (time < 1)
            {
                Assert.Equal(absoluteOptions.Value.AbsoluteExpiration, options.AbsoluteExpiration.Value);
            }
            else
            {
                Assert.Equal(suggestedExpiry, options.AbsoluteExpiration.Value);
            }

            absoluteOptions.Value.AbsoluteExpiration = null;
            await _testCacheAdapter.TestReadCacheBytesAsync(DefaultCacheKey);
            Assert.Equal(1, _testCacheAdapter._memoryCache.Count);
        }

        private async Task CreateL1L2TestWithSerializerHintsAsync(
            System.DateTimeOffset dateTimeOffset,
            int memoryCacheExpectedCount)
        {
            // Arrange
            byte[] cache = new byte[3];
            AssertCacheValues(_testCacheAdapter);
            Assert.NotNull(_testCacheAdapter._memoryCache);
            Assert.Equal(0, _testCacheAdapter._memoryCache.Count);
            Assert.Empty(L2Cache._dict);
            CacheSerializerHints cacheSerializerHints = new CacheSerializerHints();
            cacheSerializerHints.SuggestedCacheExpiry = dateTimeOffset;

            // Act
            TestDistributedCache.ResetEvent.Reset();
            await _testCacheAdapter.TestWriteCacheBytesAsync(DefaultCacheKey, cache, cacheSerializerHints);

            // Assert
            Assert.Equal(memoryCacheExpectedCount, _testCacheAdapter._memoryCache.Count);
            TestDistributedCache.ResetEvent.Wait();
            Assert.Single(L2Cache._dict);
        }

        [Fact]
        public async Task SetL1Cache_ReadL1_TestAsync()
        {
            // Arrange
            byte[] cache = new byte[3];
            cache[0] = 4;
            TelemetryData telemetryData = new TelemetryData();
            AssertCacheValues(_testCacheAdapter);
            Assert.NotNull(_testCacheAdapter._memoryCache);
            Assert.Equal(0, _testCacheAdapter._memoryCache.Count);
            _testCacheAdapter._memoryCache.Set(DefaultCacheKey, cache, new MemoryCacheEntryOptions { Size = cache.Length });
            Assert.Equal(1, _testCacheAdapter._memoryCache.Count);
            Assert.Empty(L2Cache._dict);

            // Act
            byte[]? result = await _testCacheAdapter.TestReadCacheBytesAsync(DefaultCacheKey, telemetryData);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(4, result[0]);
            Assert.Equal(CacheLevel.L1Cache, telemetryData.CacheLevel);
        }

        [Fact]
        public async Task EmptyL1Cache_ReadL2AndSetL1_TestAsync()
        {
            // Arrange
            byte[] cache = new byte[3];
            cache[0] = 4;
            TelemetryData telemetryData = new TelemetryData();
            AssertCacheValues(_testCacheAdapter);
            _testCacheAdapter._distributedCache.Set(DefaultCacheKey, cache);
            Assert.Single(L2Cache._dict);
            Assert.NotNull(_testCacheAdapter._memoryCache);
            Assert.Equal(0, _testCacheAdapter._memoryCache.Count);

            // Act
            byte[]? result = await _testCacheAdapter.TestReadCacheBytesAsync(DefaultCacheKey, telemetryData);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(4, result[0]);
            Assert.Equal(1, _testCacheAdapter._memoryCache.Count);
            Assert.Single(L2Cache._dict);
            Assert.Equal(CacheLevel.L2Cache, telemetryData.CacheLevel);
        }

        [Fact]
        public async Task EmptyL1Cache_ReadL2AndSetL1_ForTelemetryTestAsync()
        {
            // Arrange
            byte[] cache = new byte[3];
            cache[0] = 4;
            AssertCacheValues(_testCacheAdapter);
            _testCacheAdapter._distributedCache.Set(DefaultCacheKey, cache);
            TelemetryData telemetryData = new TelemetryData();
            Assert.Single(L2Cache._dict);
            Assert.NotNull(_testCacheAdapter._memoryCache);
            Assert.Equal(0, _testCacheAdapter._memoryCache.Count);

            // Act
            byte[]? result = await _testCacheAdapter.TestReadCacheBytesAsync(DefaultCacheKey, telemetryData);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(4, result[0]);
            Assert.Equal(1, _testCacheAdapter._memoryCache.Count);
            Assert.Single(L2Cache._dict);
            Assert.Equal(CacheLevel.L2Cache, telemetryData.CacheLevel);

            // Act
            result = await _testCacheAdapter.TestReadCacheBytesAsync(DefaultCacheKey, telemetryData);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(4, result[0]);
            Assert.Equal(1, _testCacheAdapter._memoryCache.Count);
            Assert.Single(L2Cache._dict);
            Assert.Equal(CacheLevel.L1Cache, telemetryData.CacheLevel);
        }

        [Fact]
        public async Task EmptyL1L2Cache_ReturnNullCacheResult_TestAsync()
        {
            // Arrange
            byte[] cache = new byte[3];
            cache[0] = 4;
            TelemetryData telemetryData = new TelemetryData();
            AssertCacheValues(_testCacheAdapter);
            Assert.NotNull(_testCacheAdapter._memoryCache);
            Assert.Equal(0, _testCacheAdapter._memoryCache.Count);

            // Act
            byte[]? result = await _testCacheAdapter.TestReadCacheBytesAsync(DefaultCacheKey, telemetryData);

            // Assert
            Assert.Null(result);
            Assert.Equal(0, _testCacheAdapter._memoryCache.Count);
            Assert.Empty(L2Cache._dict);
            Assert.Equal(CacheLevel.None, telemetryData.CacheLevel);
        }

        [Fact]
        public async Task SetL1Cache_ReadL1WithDifferentCacheKey__ReturnNullCacheResult_TestAsync()
        {
            // Arrange
            byte[] cache = new byte[3];
            cache[0] = 4;
            AssertCacheValues(_testCacheAdapter);
            Assert.NotNull(_testCacheAdapter._memoryCache);
            Assert.Equal(0, _testCacheAdapter._memoryCache.Count);
            Assert.Empty(L2Cache._dict);
            _testCacheAdapter._memoryCache.Set(AnotherCacheKey, cache, new MemoryCacheEntryOptions { Size = cache.Length });
            Assert.Equal(1, _testCacheAdapter._memoryCache.Count);

            // Act
            byte[]? result = await _testCacheAdapter.TestReadCacheBytesAsync(DefaultCacheKey);

            // Assert
            Assert.Null(result);
            Assert.Empty(L2Cache._dict);
        }

        [Fact]
        public async Task SetL1CacheAndL2CacheWithDifferentCache_ReadL1WithCacheKey__ReturnL2CacheResult_TestAsync()
        {
            // Arrange
            byte[] cacheL1 = new byte[3];
            cacheL1[0] = 4;
            byte[] cacheL2 = new byte[2];
            cacheL2[0] = 9;
            AssertCacheValues(_testCacheAdapter);
            Assert.NotNull(_testCacheAdapter._memoryCache);
            Assert.Equal(0, _testCacheAdapter._memoryCache.Count);
            _testCacheAdapter._memoryCache.Set(AnotherCacheKey, cacheL1, new MemoryCacheEntryOptions { Size = cacheL1.Length });
            _testCacheAdapter._distributedCache.Set(DefaultCacheKey, cacheL2);
            Assert.Equal(1, _testCacheAdapter._memoryCache.Count);
            Assert.Single(L2Cache._dict);

            // Act & Assert
            byte[]? result = await _testCacheAdapter.TestReadCacheBytesAsync(DefaultCacheKey);
            Assert.NotNull(result);
            Assert.Equal(9, result[0]);
            Assert.Equal(2, _testCacheAdapter._memoryCache.Count);
            Assert.Single(L2Cache._dict);

            byte[]? result2 = await _testCacheAdapter.TestReadCacheBytesAsync(AnotherCacheKey);
            Assert.NotNull(result2);
            Assert.Equal(4, result2[0]);
            Assert.Equal(2, _testCacheAdapter._memoryCache.Count);
            Assert.Single(L2Cache._dict);
        }

        [Fact]
        public async Task RemoveL1CacheItem_TestAsync()
        {
            // Arrange
            byte[] cacheL1 = new byte[3];
            cacheL1[0] = 4;
            AssertCacheValues(_testCacheAdapter);
            Assert.NotNull(_testCacheAdapter._memoryCache);
            Assert.Equal(0, _testCacheAdapter._memoryCache.Count);
            _testCacheAdapter._memoryCache.Set(DefaultCacheKey, cacheL1, new MemoryCacheEntryOptions { Size = cacheL1.Length });
            Assert.Equal(1, _testCacheAdapter._memoryCache.Count);

            // Act
            await _testCacheAdapter.TestRemoveKeyAsync(DefaultCacheKey);

            // Assert
            Assert.Equal(0, _testCacheAdapter._memoryCache.Count);
        }

        [Fact]
        public async Task RemoveL2CacheItem_TestAsync()
        {
            // Arrange
            byte[] cacheL2 = new byte[3];
            cacheL2[0] = 4;
            AssertCacheValues(_testCacheAdapter);
            Assert.NotNull(_testCacheAdapter._memoryCache);
            Assert.Equal(0, _testCacheAdapter._memoryCache.Count);
            _testCacheAdapter._distributedCache.Set(DefaultCacheKey, cacheL2);
            Assert.Single(L2Cache._dict);

            // Act
            await _testCacheAdapter.TestRemoveKeyAsync(DefaultCacheKey);

            // Assert
            Assert.Equal(0, _testCacheAdapter._memoryCache.Count);
            Assert.Empty(L2Cache._dict);
        }

        [Fact]
        public async Task RemoveOneCacheItem_OneCacheItemsRemains_TestAsync()
        {
            // Arrange
            byte[] cacheL1 = new byte[3];
            byte[] cacheL2 = new byte[2];
            AssertCacheValues(_testCacheAdapter);
            Assert.NotNull(_testCacheAdapter._memoryCache);
            Assert.Equal(0, _testCacheAdapter._memoryCache.Count);
            _testCacheAdapter._memoryCache.Set(AnotherCacheKey, cacheL1, new MemoryCacheEntryOptions { Size = cacheL1.Length });
            _testCacheAdapter._distributedCache.Set(DefaultCacheKey, cacheL2);

            // Act & Assert
            await _testCacheAdapter.TestRemoveKeyAsync(DefaultCacheKey);
            Assert.Equal(1, _testCacheAdapter._memoryCache.Count);
            Assert.Empty(L2Cache._dict);
            await _testCacheAdapter.TestRemoveKeyAsync(AnotherCacheKey);
            Assert.Equal(0, _testCacheAdapter._memoryCache.Count);
            Assert.Empty(L2Cache._dict);
        }

        private static void AssertCacheValues(TestMsalDistributedTokenCacheAdapter testCache)
        {
            Assert.NotNull(testCache);
            Assert.NotNull(testCache._distributedCache);
            Assert.NotNull(testCache._memoryCache);
        }

        private void BuildTheRequiredServices()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddDistributedTokenCaches();
            _provider = services.BuildServiceProvider();
        }

        private static IDistributedCache MakeMockDistributedCache()
        {
            return new TestDistributedCache();
        }
    }
}
