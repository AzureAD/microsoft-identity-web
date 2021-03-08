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
            _memoryCache = new MemoryCache(_distributedCacheOptions.L1CacheOptions ?? new MemoryCacheOptions { SizeLimit = 500 * 1024 * 1024 });
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
            _memoryCache.Remove(cacheKey);
            _logger.LogDebug($"[MsIdWeb] MemoryCache: Removed cacheKey {cacheKey}. ");

            await L2OperationWithRetryOnFailureAsync(
                "Removed",
                (cacheKey) => _distributedCache.RemoveAsync(cacheKey),
                cacheKey).ConfigureAwait(false);
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
            // check memory cache first
            byte[]? result = (byte[])_memoryCache.Get(cacheKey);
            _logger.LogDebug($"[MsIdWeb] MemoryCache: Read {cacheKey} cache size: {result?.Length}. ");

            if (result == null)
            {
                var measure = await Task.Run(async () =>
                {
                    // not found in memory, check distributed cache
                    result = await L2OperationWithRetryOnFailureAsync(
                        "Read",
                        (cacheKey) => _distributedCache.GetAsync(cacheKey),
                        cacheKey).ConfigureAwait(false);
                }).Measure().ConfigureAwait(false);

                _logger.LogDebug($"[MsIdWeb] DistributedCache: Read time in Ticks: {measure.Ticks}");

                // back propagate to memory cache
                if (result != null)
                {
                    MemoryCacheEntryOptions memoryCacheEntryOptions = new MemoryCacheEntryOptions()
                    {
                        AbsoluteExpirationRelativeToNow = _expirationTime,
                        Size = result?.Length,
                    };

                    _logger.LogDebug($"[MsIdWeb] Back propagate from Distributed to Memory, cache size: {result?.Length}");
                    _memoryCache.Set(cacheKey, result, memoryCacheEntryOptions);
                    _logger.LogDebug($"[MsIdWeb] MemoryCache: Count: {_memoryCache.Count}");
                }
            }

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
            _memoryCache.Set(cacheKey, bytes, memoryCacheEntryOptions);
            _logger.LogDebug($"[MsIdWeb] MemoryCache: Write cacheKey {cacheKey} cache size: {bytes?.Length}. ");
            _logger.LogDebug($"[MsIdWeb] MemoryCache: Count: {_memoryCache.Count}");

            var measure = await L2OperationWithRetryOnFailureAsync(
                "Write",
                (cacheKey) => _distributedCache.SetAsync(cacheKey, bytes, _distributedCacheOptions),
                cacheKey).Measure().ConfigureAwait(false);
        }

        private async Task L2OperationWithRetryOnFailureAsync(
            string operation,
            Func<string, Task> cacheOperation,
            string cacheKey,
            byte[]? bytes = null,
            bool inRetry = false)
        {
            try
            {
                var measure = await cacheOperation(cacheKey).Measure().ConfigureAwait(false);
                _logger.LogDebug($"[MsIdWeb] DistributedCache: {operation} cacheKey {cacheKey} cache size {bytes?.Length} InRetry? {inRetry} Time in Ticks: {measure.Ticks}. ");
            }
            catch (Exception ex)
            {
                _logger.LogError($"[MsIdWeb] DistributedCache: Connection issue. InRetry? {inRetry} Error message: {ex.Message} ");

                if (_distributedCacheOptions.OnL2CacheFailure != null && _distributedCacheOptions.OnL2CacheFailure(ex) && !inRetry)
                {
                    _logger.LogDebug($"[MsIdWeb] DistributedCache: Retrying {operation} cacheKey {cacheKey}. ");
                    await L2OperationWithRetryOnFailureAsync(
                        operation,
                        cacheOperation,
                        cacheKey,
                        bytes,
                        true).ConfigureAwait(false);
                }
            }
        }

        private async Task<byte[]?> L2OperationWithRetryOnFailureAsync(
            string operation,
            Func<string, Task<byte[]>> cacheOperation,
            string cacheKey,
            bool inRetry = false)
        {
            byte[]? result = null;
            try
            {
                result = await cacheOperation(cacheKey).ConfigureAwait(false);
                _logger.LogDebug($"[MsIdWeb] DistributedCache: {operation} cacheKey {cacheKey} cache size {result?.Length} InRetry? {inRetry}. ");
            }
            catch (Exception ex)
            {
                _logger.LogError($"[MsIdWeb] DistributedCache: Connection issue. InRetry? {inRetry} Error message: {ex.Message} ");

                if (_distributedCacheOptions.OnL2CacheFailure != null && _distributedCacheOptions.OnL2CacheFailure(ex) && !inRetry)
                {
                    _logger.LogDebug($"[MsIdWeb] DistributedCache: Retrying {operation} cacheKey {cacheKey}. ");
                    result = await L2OperationWithRetryOnFailureAsync(
                        operation,
                        cacheOperation,
                        cacheKey,
                        true).ConfigureAwait(false);
                }
            }

            return result;
        }
    }
}
