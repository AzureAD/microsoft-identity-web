// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Web.TokenCacheProviders;
using Microsoft.Identity.Web.TokenCacheProviders.InMemory;
using System.Threading.Tasks;

namespace Microsoft.Identity.Web.Test.Common.TestHelpers
{
    public class MsalTestTokenCacheProvider : MsalAbstractTokenCacheProvider
    {
        public MsalTestTokenCacheProvider(
            IOptions<MicrosoftIdentityOptions> microsoftIdentityOptions,
            IHttpContextAccessor httpContextAccessor,
            IMemoryCache memoryCache,
            IOptions<MsalMemoryTokenCacheOptions> cacheOptions) :
            base(microsoftIdentityOptions, httpContextAccessor)
        {
            MemoryCache = memoryCache;
            _cacheOptions = cacheOptions.Value;
        }

        public IMemoryCache MemoryCache { get; }

        public int Count { get; internal set; }

        private readonly MsalMemoryTokenCacheOptions _cacheOptions;

        protected override Task<byte[]> ReadCacheBytesAsync(string cacheKey)
        {
            byte[] tokenCacheBytes = (byte[])MemoryCache.Get(cacheKey);
            return Task.FromResult(tokenCacheBytes);
        }

        protected override Task RemoveKeyAsync(string cacheKey)
        {
            MemoryCache.Remove(cacheKey);
            Count--;
            return Task.CompletedTask;
        }

        protected override Task WriteCacheBytesAsync(string cacheKey, byte[] bytes)
        {
            MemoryCache.Set(cacheKey, bytes, _cacheOptions.SlidingExpiration);
            Count++;
            return Task.CompletedTask;
        }
    }
}
