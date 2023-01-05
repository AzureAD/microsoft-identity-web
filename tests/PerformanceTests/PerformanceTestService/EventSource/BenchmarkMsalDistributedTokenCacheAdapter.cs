// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Web.TokenCacheProviders;
using Microsoft.Identity.Web.TokenCacheProviders.Distributed;

namespace PerformanceTestService.EventSource
{
    /// <summary>
    /// <see cref="MsalDistributedTokenCacheAdapter"/> with added benchmarking counters.
    /// </summary>
    public class BenchmarkMsalDistributedTokenCacheAdapter : MsalAbstractTokenCacheProvider
    {
        /// <summary>
        /// .NET Core memory cache.
        /// </summary>
        private readonly IDistributedCache _distributedCache;

        /// <summary>
        /// MSAL memory token cache options.
        /// </summary>
        private readonly MsalDistributedTokenCacheAdapterOptions _cacheOptions;

        /// <summary>
        /// Initializes a new instance of the <see cref="BenchmarkMsalDistributedTokenCacheAdapter"/> class.
        /// </summary>
        /// <param name="memoryCache">Distributed cache instance to use.</param>
        /// <param name="cacheOptions">Options for the token cache.</param>

        public BenchmarkMsalDistributedTokenCacheAdapter(
            IDistributedCache memoryCache,
            IOptions<MsalDistributedTokenCacheAdapterOptions> cacheOptions)
        {
            if (cacheOptions == null)
            {
                throw new ArgumentNullException(nameof(cacheOptions));
            }

            _distributedCache = memoryCache;
            _cacheOptions = cacheOptions.Value;
        }

        /// <summary>
        /// Removes a specific token cache, described by its cache key
        /// from the distributed cache.
        /// </summary>
        /// <param name="cacheKey">Key of the cache to remove.</param>
        /// <returns>A <see cref="Task"/> that completes when key removal has completed.</returns>
        protected override async Task RemoveKeyAsync(string cacheKey)
        {
            var bytes = await _distributedCache.GetAsync(cacheKey).ConfigureAwait(false);           
            await _distributedCache.RemoveAsync(cacheKey).ConfigureAwait(false);
            
            MemoryCacheEventSource.Log.IncrementRemoveCount();
            if (bytes != null)
            {
                MemoryCacheEventSource.Log.DecrementSize(bytes.Length);
            }
        }

        /// <summary>
        /// Read a specific token cache, described by its cache key, from the
        /// distributed cache.
        /// </summary>
        /// <param name="cacheKey">Key of the cache item to retrieve.</param>
        /// <returns>Read blob representing a token cache for the cache key
        /// (account or app).</returns>
        protected override async Task<byte[]?> ReadCacheBytesAsync(string cacheKey)
        {
            var stopwatch = Stopwatch.StartNew();
            var bytes = await _distributedCache.GetAsync(cacheKey).ConfigureAwait(false);
            stopwatch.Stop();

            MemoryCacheEventSource.Log.IncrementReadCount();
            MemoryCacheEventSource.Log.AddReadDuration(stopwatch.Elapsed.TotalMilliseconds);
            if (bytes == null)
            {
                MemoryCacheEventSource.Log.IncrementReadMissCount();
            }

            return bytes;
        }

        /// <summary>
        /// Writes a token cache blob to the serialization cache (by key).
        /// </summary>
        /// <param name="cacheKey">Cache key.</param>
        /// <param name="bytes">blob to write.</param>
        /// <returns>A <see cref="Task"/> that completes when a write operation has completed.</returns>
        protected override async Task WriteCacheBytesAsync(string cacheKey, byte[] bytes)
        {
            var stopwatch = Stopwatch.StartNew();
            await _distributedCache.SetAsync(cacheKey, bytes, _cacheOptions).ConfigureAwait(false);
            stopwatch.Stop();

            MemoryCacheEventSource.Log.IncrementWriteCount();
            MemoryCacheEventSource.Log.AddWriteDuration(stopwatch.Elapsed.TotalMilliseconds);
            if (bytes != null)
            {
                MemoryCacheEventSource.Log.IncrementSize(bytes.Length);
            }
        }
    }
}
