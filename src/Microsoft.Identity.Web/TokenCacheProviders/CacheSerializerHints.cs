// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;

namespace Microsoft.Identity.Web.TokenCacheProviders
{
    /// <summary>
    /// Set of properties that the token cache serialization implementations might use to optimize the cache.
    /// </summary>
    public class CacheSerializerHints
    {
        /// <summary>
        /// CancellationToken enabling cooperative cancellation between threads, thread pool, or Task objects.
        /// </summary>
        public CancellationToken CancellationToken { get; set; }
    }
}
