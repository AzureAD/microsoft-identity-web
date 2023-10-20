// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Microsoft.Identity.Web.TokenCacheProviders.InMemory
{
    /// <summary>
    /// An implementation of token cache for both Confidential and Public clients backed by MemoryCache.
    /// </summary>
    /// <seealso>https://aka.ms/msal-net-token-cache-serialization</seealso>
    public class MsalMemoryTokenCacheProvider : MsalAbstractTokenCacheProvider
    {
        /// <summary>
        /// .NET Core Memory cache.
        /// </summary>
        private readonly IMemoryCache _memoryCache;

        /// <summary>
        /// MSAL memory token cache options.
        /// </summary>
        private readonly MsalMemoryTokenCacheOptions _cacheOptions;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="memoryCache">serialization cache.</param>
        /// <param name="cacheOptions">Memory cache options.</param>
        public MsalMemoryTokenCacheProvider(
            IMemoryCache memoryCache,
            IOptions<MsalMemoryTokenCacheOptions> cacheOptions)
        {
            _ = Throws.IfNull(cacheOptions);

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
            _memoryCache.Remove(cacheKey);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Reads a blob from the serialization cache (identified by its key).
        /// </summary>
        /// <param name="cacheKey">Token cache key.</param>
        /// <returns>Read Bytes.</returns>
        protected override Task<byte[]?> ReadCacheBytesAsync(string cacheKey)
        {
            byte[]? tokenCacheBytes = (byte[]?)_memoryCache.Get(cacheKey);
            return Task.FromResult(tokenCacheBytes);
        }

        /// <summary>
        /// Method to be overridden by concrete cache serializers to Read the cache bytes.
        /// </summary>
        /// <param name="cacheKey">Cache key.</param>
        /// <param name="cacheSerializerHints">Hints for the cache serialization implementation optimization.</param>
        /// <returns>Read bytes.</returns>
        protected override Task<byte[]?> ReadCacheBytesAsync(string cacheKey, CacheSerializerHints cacheSerializerHints)
        {
            byte[]? tokenCacheBytes = (byte[]?)_memoryCache.Get(cacheKey);

            if (tokenCacheBytes != null && cacheSerializerHints.TelemetryData != null)
            {
                cacheSerializerHints.TelemetryData.CacheLevel = Client.Cache.CacheLevel.L1Cache;
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
            return WriteCacheBytesAsync(cacheKey, bytes, new CacheSerializerHints());
        }

        /// <summary>
        /// Writes a token cache blob to the serialization cache (identified by its key).
        /// </summary>
        /// <param name="cacheKey">Token cache key.</param>
        /// <param name="bytes">Bytes to write.</param>
        /// <param name="cacheSerializerHints">Hints for the cache serialization implementation optimization.</param>
        /// <returns>A <see cref="Task"/> that completes when a write operation has completed.</returns>
        protected override Task WriteCacheBytesAsync(
            string cacheKey,
            byte[] bytes,
            CacheSerializerHints cacheSerializerHints)
        {
            MemoryCacheEntryOptions memoryCacheEntryOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = DetermineCacheEntryExpiry(cacheSerializerHints),
                Size = bytes?.Length,
            };

            _memoryCache.Set(cacheKey, bytes, memoryCacheEntryOptions);
            return Task.CompletedTask;
        }

        /// <summary>
        /// _cacheOptions.AbsoluteExpirationRelativeToNow represents either a user-provided expiration or the default, if not set.
        /// Between the suggested expiry and expiry from options, the shorter one takes precedence.
        /// </summary>
        internal TimeSpan DetermineCacheEntryExpiry(CacheSerializerHints cacheSerializerHints)
        {
            TimeSpan? cacheExpiry = null;
            if (cacheSerializerHints != null && cacheSerializerHints.SuggestedCacheExpiry != null)
            {
                cacheExpiry = cacheSerializerHints.SuggestedCacheExpiry.Value.UtcDateTime - DateTime.UtcNow;
                if (cacheExpiry < TimeSpan.Zero)
                {
                    cacheExpiry = TimeSpan.FromMilliseconds(1);
                }
            }

            return cacheExpiry is null || _cacheOptions.AbsoluteExpirationRelativeToNow < cacheExpiry
                ? _cacheOptions.AbsoluteExpirationRelativeToNow
                : cacheExpiry.Value;
        }
    }
}
