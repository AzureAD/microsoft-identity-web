// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Web.TokenCacheProviders;
using Microsoft.Identity.Web.TokenCacheProviders.InMemory;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    public class MsalTokenCacheProviderTests
    {
        /// <summary>
        /// Implementation based on a Memory cache, But could be Redis, SQL, ...
        /// </summary>
        /// <returns>In memory cache.</returns>
        private static IMemoryCache GetMemoryCache()
        {
            if (s_memoryCache == null)
            {
                IOptions<MemoryCacheOptions> options = Options.Create(new MemoryCacheOptions());
                s_memoryCache = new MemoryCache(options);
            }

            return s_memoryCache;
        }

        private static IMemoryCache s_memoryCache;

        private static IMsalTokenCacheProvider CreateTokenCacheSerializer()
        {
            IOptions<MsalMemoryTokenCacheOptions> msalCacheOptions = Options.Create(new MsalMemoryTokenCacheOptions());

            MsalMemoryTokenCacheProvider memoryTokenCacheProvider = new MsalMemoryTokenCacheProvider(GetMemoryCache(), msalCacheOptions);
            return memoryTokenCacheProvider;
        }

        [Fact]
        public void CreateTokenCacheSerializerTest()
        {
            IMsalTokenCacheProvider tokenCacheProvider = CreateTokenCacheSerializer();
            Assert.NotNull(tokenCacheProvider);
        }
    }
}
