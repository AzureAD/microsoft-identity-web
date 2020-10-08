// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Web.TokenCacheProviders;
using Microsoft.Identity.Web.TokenCacheProviders.InMemory;

namespace PerformanceTestService
{
    /// <summary>
    /// <see cref="MsalMemoryTokenCacheProvider"/> with added benchmarking counters.
    /// </summary>
    public class BenchmarkMsalMemoryTokenCacheProvider : MsalAbstractTokenCacheProvider
    {
        /// <summary>
        /// .NET Core memory cache.
        /// </summary>
        private readonly IMemoryCache _memoryCache;

        /// <summary>
        /// MSAL memory token cache options.
        /// </summary>
        private readonly MsalMemoryTokenCacheOptions _cacheOptions;

        /// <summary>
        /// Initializes a new instance of the <see cref="BenchmarkMsalMemoryTokenCacheProvider"/> class.
        /// </summary>
        /// <param name="memoryCache">Cache instance to use.</param>
        /// <param name="cacheOptions">Options for the token cache.</param>

        public BenchmarkMsalMemoryTokenCacheProvider(
            IMemoryCache memoryCache,
            IOptions<MsalMemoryTokenCacheOptions> cacheOptions)
        {
            if (cacheOptions == null)
            {
                throw new ArgumentNullException(nameof(cacheOptions));
            }

            _memoryCache = memoryCache;
            _cacheOptions = cacheOptions.Value;
        }


        /// <summary>
        /// Removes a token cache identified by its key, from the serialization
        /// cache.
        /// </summary>
        /// <param name="cacheKey">token cache key.</param>
        /// <returns>A <see cref="Task"/> that completes when key removal has completed.</returns>
        protected override Task RemoveKeyAsync(string cacheKey)
        {
            byte[] tokenCacheBytes = (byte[])_memoryCache.Get(cacheKey);
            _memoryCache.Remove(cacheKey);

            MemoryCacheEventSource.Log.IncrementRemoveCount();
            if (tokenCacheBytes != null)
            {
                MemoryCacheEventSource.Log.DecrementSize(tokenCacheBytes.Length);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Reads a blob from the serialization cache (identified by its key).
        /// </summary>
        /// <param name="cacheKey">Token cache key.</param>
        /// <returns>Read Bytes.</returns>
        protected override Task<byte[]> ReadCacheBytesAsync(string cacheKey)
        {
            var stopwatch = Stopwatch.StartNew();
            byte[] tokenCacheBytes = (byte[])_memoryCache.Get(cacheKey);
            stopwatch.Stop();

            MemoryCacheEventSource.Log.IncrementReadCount();
            MemoryCacheEventSource.Log.AddReadDuration(stopwatch.Elapsed.TotalMilliseconds);
            if (tokenCacheBytes == null)
            {
                MemoryCacheEventSource.Log.IncrementReadMissCount();
            }

            return Task.FromResult(tokenCacheBytes);
        }

        /// <summary>
        /// Writes a token cache blob to the serialization cache (identified by its key).
        /// </summary>
        /// <param name="cacheKey">Token cache key.</param>
        /// <param name="bytes">Bytes to write.</param>
        /// <returns>A <see cref="Task"/> that completes when a write operation has completed.</returns>
        protected override Task WriteCacheBytesAsync(string cacheKey, byte[] bytes)
        {
            MemoryCacheEntryOptions memoryCacheEntryOptions = new MemoryCacheEntryOptions()
            {
                AbsoluteExpirationRelativeToNow = _cacheOptions.AbsoluteExpirationRelativeToNow,
                Size = bytes?.Length,
            };

            var stopwatch = Stopwatch.StartNew();
            _memoryCache.Set(cacheKey, bytes, memoryCacheEntryOptions);
            stopwatch.Stop();

            MemoryCacheEventSource.Log.IncrementWriteCount();
            MemoryCacheEventSource.Log.AddWriteDuration(stopwatch.Elapsed.TotalMilliseconds);
            if (bytes != null)
            {
                MemoryCacheEventSource.Log.IncrementSize(bytes.Length);
            }

            return Task.CompletedTask;
        }
    }
}
