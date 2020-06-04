// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
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
        /// <param name="microsoftIdentityOptions">Configuration options.</param>
        /// <param name="httpContextAccessor">Accessor to the HttpContext.</param>
        /// <param name="memoryCache">serialization cache.</param>
        /// <param name="cacheOptions">Memory cache options.</param>
        public MsalMemoryTokenCacheProvider(
            IOptions<MicrosoftIdentityOptions> microsoftIdentityOptions,
            IHttpContextAccessor httpContextAccessor,
            IMemoryCache memoryCache,
            IOptions<MsalMemoryTokenCacheOptions> cacheOptions)
            : base(microsoftIdentityOptions, httpContextAccessor)
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
        protected override Task<byte[]> ReadCacheBytesAsync(string cacheKey)
        {
            byte[] tokenCacheBytes = (byte[])_memoryCache.Get(cacheKey);
            return Task.FromResult(tokenCacheBytes);
        }

        /// <summary>
        /// Writes a token cache blob to the serialization cache (identified by its key).
        /// </summary>
        /// <param name="cacheKey">Token cache key.</param>
        /// <param name="bytes">Bytes to write.</param>
        protected override Task WriteCacheBytesAsync(string cacheKey, byte[] bytes)
        {
            _memoryCache.Set(cacheKey, bytes, _cacheOptions.SlidingExpiration);
            return Task.CompletedTask;
        }
    }
}
