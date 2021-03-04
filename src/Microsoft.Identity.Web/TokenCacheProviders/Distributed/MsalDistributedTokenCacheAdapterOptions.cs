// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Extensions.Caching.Distributed;

namespace Microsoft.Identity.Web.TokenCacheProviders.Distributed
{
    /// <summary>
    /// Options for the MSAL token cache serialization adapter,
    /// which delegates the serialization to the IDistributedCache implementations
    /// available with .NET Core.
    /// </summary>
    public class MsalDistributedTokenCacheAdapterOptions : DistributedCacheEntryOptions
    {
        /// <summary>
        /// In memory (L1) cache size limit in Mb.
        /// Default is 500 Mb.
        /// </summary>
        public long L1CacheSizeLimit { get; set; } = 500;

        /// <summary>
        /// The write and remove call to the Distributed (L2) cache will be awaited.
        /// This means the thread will return asynchronously.
        /// Setting to false, the thread will return immediatly,
        /// allowing an increase in preformance, but
        /// the Distributed (L2) cache will be eventually consistent with the In Memory
        /// (L1) cache. Set to true by default.
        /// </summary>
        public bool AwaitL2CacheOperation { get; set; } = true;

        /// <summary>
        /// Value more than 0, less than 1, to set the In Memory (L1) cache
        /// expiration time values relative to the Distributed (L2) cache.
        /// Default is 1.
        /// </summary>
        internal double L1ExpirationTimeRatio { get; set; } = 1;
    }
}
