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
        /// Value more than 0, less than 1, to set the In Memory (L1) cache
        /// expiration time values relative to the Distributed (L2) cache.
        /// Default is 1.
        /// </summary>
        public double L1ExpirationTimeRatio { get; set; } = 1;
    }
}
