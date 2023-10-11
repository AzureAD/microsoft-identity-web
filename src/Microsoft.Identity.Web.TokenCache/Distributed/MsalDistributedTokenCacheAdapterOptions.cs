// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;

namespace Microsoft.Identity.Web.TokenCacheProviders.Distributed
{
    /// <summary>
    /// Options for the MSAL token cache serialization adapter,
    /// which delegates the serialization to the <c>IDistributedCache</c> implementations
    /// available with .NET Core.
    /// </summary>
    public class MsalDistributedTokenCacheAdapterOptions : DistributedCacheEntryOptions
    {
        internal const int FiveHundredMb = 500 * 1024 * 1024;

        /// <summary>
        /// Options of the in-memory (L1) cache.
        /// </summary>
        public MemoryCacheOptions L1CacheOptions { get; set; } = new MemoryCacheOptions
        {
            SizeLimit = FiveHundredMb,   // 500 Mb
        };

        /// <summary>
        /// Callback offered to the app to be notified when the L2 cache fails.
        /// This way the app is given the possibility to act on the L2 cache,
        /// for instance, in the case of Redis exception, to reconnect. This is left to the application as it's
        /// the only one that knows about the real implementation of the L2 cache.
        /// The handler should return <c>true</c> if the cache should try the operation again, and
        /// <c>false</c> otherwise. When <c>true</c> is passed and the retry fails, an exception
        /// will be thrown.
        /// </summary>
        public Func<Exception, bool>? OnL2CacheFailure { get; set; }

        /// <summary>
        /// Value must be more than 0 and less than or equal to 1.
        /// Sets the ratio of the in-memory (L1) cache expiration time
        /// relative to the distributed (L2) cache (e.g. when set to .5, 
        /// the L1 cache entry expiry is half the time of the L2 cache expiry).
        /// Default is 1.
        /// </summary>
        internal double L1ExpirationTimeRatio { get; set; } = 1;

        /// <summary>
        /// Should the token cache be encrypted.
        /// </summary>
        /// The default is <c>false.</c>
        public bool Encrypt { get; set; }

        /// <summary>
        /// Disable the in-memory (L1) cache.
        /// Useful in scenarios where multiple apps share the same
        /// distributed (L2) cache.
        /// </summary>
        /// The default is <c>false.</c>
        public bool DisableL1Cache { get; set; }

        /// <summary>
        /// Enable writing to the distributed (L2) cache to be async (i.e. fire-and-forget).
        /// This improves performance as the MSAL.NET will not have to wait
        /// for the write to complete.
        /// </summary>
        /// The default is <c>false.</c>
        public bool EnableAsyncL2Write { get; set; }
    }
}
