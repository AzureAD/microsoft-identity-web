// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.Identity.Web.TokenCacheProviders.Distributed
{
    /// <summary>
    /// An implementation of the token cache for both Confidential and Public clients backed by a Distributed Cache.
    /// The Distributed Cache (L2), by default creates a Memory Cache (L1), for faster look up, resulting in a two level cache.
    /// </summary>
    /// <seealso>https://aka.ms/msal-net-token-cache-serialization</seealso>
    public class MsalDistributedTokenCacheAdapter : MsalAbstractTokenCacheProvider
    {
        /// <summary>
        /// .NET Core Memory cache.
        /// </summary>
        internal /*for tests*/ readonly IDistributedCache _distributedCache;
        internal /*for tests*/ readonly MemoryCache _memoryCache;
        private readonly ILogger<MsalDistributedTokenCacheAdapter> _logger;
        private readonly TimeSpan? _expirationTime;

        /// <summary>
        /// MSAL distributed token cache options.
        /// </summary>
        private readonly MsalDistributedTokenCacheAdapterOptions _distributedCacheOptions;

        /// <summary>
        /// Initializes a new instance of the <see cref="MsalDistributedTokenCacheAdapter"/> class.
        /// </summary>
        /// <param name="distributedCache">Distributed cache instance to use.</param>
        /// <param name="distributedCacheOptions">Options for the token cache.</param>
        /// <param name="logger">MsalDistributedTokenCacheAdapter logger.</param>
        public MsalDistributedTokenCacheAdapter(
                                            IDistributedCache distributedCache,
                                            IOptions<MsalDistributedTokenCacheAdapterOptions> distributedCacheOptions,
                                            ILogger<MsalDistributedTokenCacheAdapter> logger)
        {
            if (distributedCacheOptions == null)
            {
                throw new ArgumentNullException(nameof(distributedCacheOptions));
            }

            _distributedCache = distributedCache;
            _distributedCacheOptions = distributedCacheOptions.Value;
            _memoryCache = new MemoryCache(new MemoryCacheOptions { SizeLimit = _distributedCacheOptions.L1CacheSizeLimit * (1024 * 1024) });
            _logger = logger;

            if (_distributedCacheOptions.AbsoluteExpirationRelativeToNow != null)
            {
                if (_distributedCacheOptions.L1ExpirationTimeRatio <= 0 || _distributedCacheOptions.L1ExpirationTimeRatio > 1)
                {
                    throw new ArgumentOutOfRangeException(nameof(_distributedCacheOptions.L1ExpirationTimeRatio), "L1ExpirationTimeRatio must be greater than 0, less than 1. ");
                }

                _expirationTime = TimeSpan.FromMilliseconds(_distributedCacheOptions.AbsoluteExpirationRelativeToNow.Value.TotalMilliseconds * _distributedCacheOptions.L1ExpirationTimeRatio);
            }
        }

        /// <summary>
        /// Removes a specific token cache, described by its cache key
        /// from the distributed cache.
        /// </summary>
        /// <param name="cacheKey">Key of the cache to remove.</param>
        /// <returns>A <see cref="Task"/> that completes when key removal has completed.</returns>
        protected override async Task RemoveKeyAsync(string cacheKey)
        {
            var startTicks = Utility.Watch.Elapsed.Ticks;
            _memoryCache.Remove(cacheKey);
            _logger.LogDebug($"[IdWebCache] MemoryCache: Remove cacheKey {cacheKey} Time in Ticks: {Utility.Watch.Elapsed.Ticks - startTicks}. ");

            try
            {
                await _distributedCache.RemoveAsync(cacheKey).ConfigureAwait(false);
                _logger.LogDebug($"[IdWebCache] DistributedCache: Remove cacheKey {cacheKey} Time in Ticks: {Utility.Watch.Elapsed.Ticks - startTicks}. ");
            }
            catch (Exception ex)
            {
                _logger.LogError($"[IdWebCache] Connection issue encountered with Distributed cache. Currently using In Memory cache only. Error message: {ex.Message} ");
            }
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
            var startTicks = Utility.Watch.Elapsed.Ticks;

            // check memory cache first
            byte[] result = (byte[])_memoryCache.Get(cacheKey);
            _logger.LogDebug($"[IdWebCache] MemoryCache: Read cache {cacheKey} Byte size: {result?.Length}. ");

            if (result == null)
            {
                // not found in memory, check distributed cache
                try
                {
                    result = await _distributedCache.GetAsync(cacheKey).ConfigureAwait(false);
                    _logger.LogDebug($"[IdWebCache] DistributedCache read: No result in memory, distributed cache result - Byte size: {result?.Length}. ");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"[IdWebCache] Connection issue encountered with Distributed cache. Currently using In Memory cache only. Error message: {ex.Message} ");
                }

                // back propagate to memory cache
                if (result != null)
                {
                    MemoryCacheEntryOptions memoryCacheEntryOptions = new MemoryCacheEntryOptions()
                    {
                        AbsoluteExpirationRelativeToNow = _expirationTime,
                        Size = result?.Length,
                    };

                    _logger.LogDebug($"[IdWebCache] Back propagate from Distributed to Memory, Byte size: {result?.Length}");
                    _memoryCache.Set(cacheKey, result, memoryCacheEntryOptions);
                    _logger.LogDebug($"[IdWebCache] MemoryCache: Count: {_memoryCache.Count}");
                }
            }

            _logger.LogDebug($"[IdWebCache] Read caches for {cacheKey} returned Bytes {result?.Length} Time in Ticks: {Utility.Watch.Elapsed.Ticks - startTicks}. ");
#pragma warning disable CS8603 // Possible null reference return.
            return result;
#pragma warning restore CS8603 // Possible null reference return.
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
                AbsoluteExpirationRelativeToNow = _expirationTime,
                Size = bytes?.Length,
            };

            // write in both
            var startTicks = Utility.Watch.Elapsed.Ticks;

            _memoryCache.Set(cacheKey, bytes, memoryCacheEntryOptions);
            _logger.LogDebug($"[IdWebCache] MemoryCache: Write cacheKey {cacheKey} Byte size: {bytes?.Length} Time in Ticks: {Utility.Watch.Elapsed.Ticks - startTicks}. ");
            _logger.LogDebug($"[IdWebCache] MemoryCache: Count: {_memoryCache.Count}");

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            Task.Run(async () =>
            {
                try
                {
                    await _distributedCache.SetAsync(cacheKey, bytes, _distributedCacheOptions).ConfigureAwait(false);
                    _logger.LogDebug($"[IdWebCache] DistributedCache: Write cacheKey {cacheKey} Byte size {bytes?.Length} Time in Ticks: {Utility.Watch.Elapsed.Ticks - startTicks}. ");

                }
                catch (Exception ex)
                {
                    _logger.LogError($"[IdWebCache] Connection issue encountered with Distributed cache. Currently using In Memory cache only. Error message: {ex.Message} ");
                }
            });
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            await Task.CompletedTask.ConfigureAwait(false);
        }
    }
}
