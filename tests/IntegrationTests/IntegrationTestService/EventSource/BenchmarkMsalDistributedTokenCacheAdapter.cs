using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Web.TokenCacheProviders.Distributed;

namespace IntegrationTestService.EventSource
{
    /// <summary>
    /// Adds benchmarking counters on top of <see cref="MsalDistributedTokenCacheAdapter"/>.
    /// </summary>
    public class BenchmarkMsalDistributedTokenCacheAdapter : MsalDistributedTokenCacheAdapter
    {
        public BenchmarkMsalDistributedTokenCacheAdapter(
            IDistributedCache memoryCache,
            IOptions<MsalDistributedTokenCacheAdapterOptions> cacheOptions) : base(memoryCache, cacheOptions)
        {
        }

        /// <summary>
        /// Removes a specific token cache, described by its cache key
        /// from the distributed cache.
        /// </summary>
        /// <param name="cacheKey">Key of the cache to remove.</param>
        /// <returns>A <see cref="Task"/> that completes when key removal has completed.</returns>
        protected override async Task RemoveKeyAsync(string cacheKey)
        {
            MemoryCacheEventSource.Log.IncrementRemoveCount();
            await base.RemoveKeyAsync(cacheKey);
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
            MemoryCacheEventSource.Log.IncrementReadCount();
            return await base.ReadCacheBytesAsync(cacheKey);
        }

        /// <summary>
        /// Writes a token cache blob to the serialization cache (by key).
        /// </summary>
        /// <param name="cacheKey">Cache key.</param>
        /// <param name="bytes">blob to write.</param>
        /// <returns>A <see cref="Task"/> that completes when a write operation has completed.</returns>
        protected override async Task WriteCacheBytesAsync(string cacheKey, byte[] bytes)
        {
            MemoryCacheEventSource.Log.IncrementWriteCount();
            await base.WriteCacheBytesAsync(cacheKey, bytes);
        }
    }
}
