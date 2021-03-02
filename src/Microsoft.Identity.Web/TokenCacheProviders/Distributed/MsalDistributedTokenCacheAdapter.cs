// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Web.TokenCacheProviders.InMemory;

namespace Microsoft.Identity.Web.TokenCacheProviders.Distributed
{
    /// <summary>
    /// An implementation of the token cache for both Confidential and Public clients backed by a DistributedCache.
    /// The DistributedCache, by default create a Memory Cache, for faster look up, two level cache.
    /// </summary>
    /// <seealso>https://aka.ms/msal-net-token-cache-serialization</seealso>
    public class MsalDistributedTokenCacheAdapter : MsalAbstractTokenCacheProvider
    {
        /// <summary>
        /// .NET Core Memory cache.
        /// </summary>
        private readonly IDistributedCache _distributedCache;
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<MsalDistributedTokenCacheAdapter> _logger;

        /// <summary>
        /// MSAL distributed token cache options.
        /// </summary>
        private readonly MsalDistributedTokenCacheAdapterOptions _distributedCacheOptions;

        /// <summary>
        /// MSAL memory token cache options.
        /// </summary>
        private readonly MsalMemoryTokenCacheOptions _memoryCacheOptions;

        /// <summary>
        /// Initializes a new instance of the <see cref="MsalDistributedTokenCacheAdapter"/> class.
        /// </summary>
        /// <param name="distributedCache">Distributed cache instance to use.</param>
        /// <param name="distributedCacheOptions">Options for the token cache.</param>
        /// <param name="logger">MsalDistributedTokenCacheAdapter logger.</param>
        /// <param name="memoryCache">Memory cache instance to use.</param>
        /// <param name="memoryCacheOptions">Memory cache options.</param>
        public MsalDistributedTokenCacheAdapter(
                                            IDistributedCache distributedCache,
                                            IOptions<MsalDistributedTokenCacheAdapterOptions> distributedCacheOptions,
                                            ILogger<MsalDistributedTokenCacheAdapter> logger,
                                            IMemoryCache? memoryCache = null,
                                            IOptions<MsalMemoryTokenCacheOptions>? memoryCacheOptions = null)
        {
            if (distributedCacheOptions == null)
            {
                throw new ArgumentNullException(nameof(distributedCacheOptions));
            }

            if (memoryCacheOptions == null)
            {
                memoryCacheOptions = Options.Create(new MsalMemoryTokenCacheOptions());
            }

            _distributedCache = distributedCache;
            _distributedCacheOptions = distributedCacheOptions.Value;
            _memoryCache = memoryCache ?? new MemoryCache(new MemoryCacheOptions());
            _memoryCacheOptions = memoryCacheOptions.Value;
            _logger = logger;
        }

        /// <summary>
        /// Removes a specific token cache, described by its cache key
        /// from the distributed cache.
        /// </summary>
        /// <param name="cacheKey">Key of the cache to remove.</param>
        /// <returns>A <see cref="Task"/> that completes when key removal has completed.</returns>
        protected override async Task RemoveKeyAsync(string cacheKey)
        {
            // remove in both
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            _memoryCache.Remove(cacheKey);
            stopwatch.Stop();
            _logger.LogInformation($"[IdWebCache]: Remove cacheKey {cacheKey} MemoryCache time: {stopwatch.Elapsed.TotalMilliseconds}. ");
            stopwatch.Start();
            await _distributedCache.RemoveAsync(cacheKey).ConfigureAwait(false);
            stopwatch.Stop();
            _logger.LogInformation($"[IdWebCache]: Remove cacheKey {cacheKey} DistributedCache time: {stopwatch.Elapsed.TotalMilliseconds}. ");
        }

        /// <summary>
        /// Read a specific token cache, described by its cache key, from the
        /// distributed cache.
        /// </summary>
        /// <param name="cacheKey">Key of the cache item to retrieve.</param>
        /// <returns>Read blob representing a token cache for the cache key
        /// (account or app).</returns>
        protected override async Task<byte[]> ReadCacheBytesAsync(string cacheKey)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            // check memory cache first, logic from MISE
            byte[] result = (byte[])_memoryCache.Get(cacheKey);
            _logger.LogInformation($"[IdWebCache]: Read memory cache {cacheKey} result: {result?.Length}. ");
            if (result == null)
            {
                // not found in memory, check distributed cache
                result = await _distributedCache.GetAsync(cacheKey).ConfigureAwait(false);
                _logger.LogInformation($"[IdWebCache]: No result in memory, check distributed cache: {result?.Length}. ");

                // back propogate to memory cache
                if (result != null)
                {
                    MemoryCacheEntryOptions memoryCacheEntryOptions = new MemoryCacheEntryOptions()
                    {
                        AbsoluteExpirationRelativeToNow = _memoryCacheOptions.AbsoluteExpirationRelativeToNow,
                        Size = result?.Length,
                    };

                    _logger.LogInformation($"[IdWebCache]: Back propogate from Distributed to Memory: {result?.Length}");
                    _memoryCache.Set(cacheKey, result, memoryCacheEntryOptions);
                }
            }

            stopwatch.Stop();
            _logger.LogInformation($"[IdWebCache]: Read cache {cacheKey} time: {stopwatch.Elapsed.TotalMilliseconds}. ");
            return result;
        }

        /// <summary>
        /// Writes a token cache blob to the serialization cache (by key).
        /// </summary>
        /// <param name="cacheKey">Cache key.</param>
        /// <param name="bytes">blob to write.</param>
        /// <returns>A <see cref="Task"/> that completes when a write operation has completed.</returns>
        protected override async Task WriteCacheBytesAsync(string cacheKey, byte[] bytes)
        {
            MemoryCacheEntryOptions memoryCacheEntryOptions = new MemoryCacheEntryOptions()
            {
                AbsoluteExpirationRelativeToNow = _memoryCacheOptions.AbsoluteExpirationRelativeToNow,
                Size = bytes?.Length,
            };

            // write in both
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            _memoryCache.Set(cacheKey, bytes, memoryCacheEntryOptions);
            stopwatch.Stop();
            _logger.LogInformation($"[IdWebCache]: Write cacheKey {cacheKey} size {bytes?.Length} MemoryCache time: {stopwatch.Elapsed.TotalMilliseconds}. ");
            stopwatch.Start();
            await _distributedCache.SetAsync(cacheKey, bytes, _distributedCacheOptions).ConfigureAwait(false);
            stopwatch.Stop();
            _logger.LogInformation($"[IdWebCache]: Write cacheKey {cacheKey} size {bytes?.Length} DistributedCache time: {stopwatch.Elapsed.TotalMilliseconds}. ");
        }
    }
}
