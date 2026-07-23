// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.Identity.Web.TokenCacheProviders.InMemory
{
    /// <summary>
    /// MSAL's in-memory token cache options.
    /// </summary>
    public class MsalMemoryTokenCacheOptions
    {
        internal static TimeSpan DefaultAbsoluteExpirationRelativeToNow = TimeSpan.FromDays(14);

        /// <summary>Initializes a new instance of the <see cref="MsalMemoryTokenCacheOptions"/> class.
        /// By default, the sliding expiration is set for 14 days.</summary>
        public MsalMemoryTokenCacheOptions()
        {
            AbsoluteExpirationRelativeToNow = DefaultAbsoluteExpirationRelativeToNow;
        }

        /// <summary>
        /// Gets or sets the value of the duration after which the cache entry will expire unless it's used
        /// This is the duration the tokens are kept in memory cache.
        /// In production, a higher value, up-to 90 days is recommended.
        /// </summary>
        /// <value>
        /// The AbsoluteExpirationRelativeToNow value.
        /// </value>
        public TimeSpan AbsoluteExpirationRelativeToNow
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the in-memory token cache should use MSAL's
        /// internal shared (static) cache instead of the <see cref="Microsoft.Extensions.Caching.Memory.IMemoryCache"/>
        /// serialization cache.
        /// <para>
        /// Defaults to <see langword="false"/>. When <see langword="false"/> (recommended), tokens are stored via
        /// the <c>IMemoryCache</c> serialization provider, so the cache honors <see cref="AbsoluteExpirationRelativeToNow"/>,
        /// the memory cache size limit, and per-entry expiry, and supports cache-key partitioning through
        /// <c>MsalMemoryTokenCacheProvider.GetSuggestedCacheKey</c>.
        /// </para>
        /// <para>
        /// Set to <see langword="true"/> only to restore the legacy behavior, where Microsoft.Identity.Web
        /// enabled MSAL's opaque static cache and bypassed the serialization provider. In that mode the
        /// expiry/size options above and the partitioning hook have no effect, and the cache is unbounded.
        /// </para>
        /// </summary>
        public bool UseSharedCache
        {
            get;
            set;
        }
    }
}
