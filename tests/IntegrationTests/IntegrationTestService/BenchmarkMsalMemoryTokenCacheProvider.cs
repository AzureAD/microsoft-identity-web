using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Web.TokenCacheProviders.InMemory;

namespace IntegrationTestService
{
    /// <summary>
    /// Adds benchmarking counters on top of <see cref="MsalMemoryTokenCacheProvider"/>.
    /// </summary>
    public class BenchmarkMsalMemoryTokenCacheProvider : MsalMemoryTokenCacheProvider
    {
        public BenchmarkMsalMemoryTokenCacheProvider(
            IMemoryCache memoryCache,
            IOptions<MsalMemoryTokenCacheOptions> cacheOptions) : base (memoryCache, cacheOptions)
        {
        }

        /// <summary>
        /// Removes a token cache identified by its key, from the serialization
        /// cache.
        /// </summary>
        /// <param name="cacheKey">token cache key.</param>
        /// <returns>A <see cref="Task"/> that completes when key removal has completed.</returns>
        protected override Task RemoveKeyAsync(string cacheKey)
        {
            MemoryCacheEventSource.Log.IncrementRemoveCount();
            return base.RemoveKeyAsync(cacheKey);
        }

        /// <summary>
        /// Reads a blob from the serialization cache (identified by its key).
        /// </summary>
        /// <param name="cacheKey">Token cache key.</param>
        /// <returns>Read Bytes.</returns>
        protected override Task<byte[]> ReadCacheBytesAsync(string cacheKey)
        {
            MemoryCacheEventSource.Log.IncrementReadCount();
            return base.ReadCacheBytesAsync(cacheKey);
        }

        /// <summary>
        /// Writes a token cache blob to the serialization cache (identified by its key).
        /// </summary>
        /// <param name="cacheKey">Token cache key.</param>
        /// <param name="bytes">Bytes to write.</param>
        /// <returns>A <see cref="Task"/> that completes when a write operation has completed.</returns>
        protected override Task WriteCacheBytesAsync(string cacheKey, byte[] bytes)
        {
            MemoryCacheEventSource.Log.IncrementWriteCount();
            return base.WriteCacheBytesAsync(cacheKey, bytes);
        }
    }
}
