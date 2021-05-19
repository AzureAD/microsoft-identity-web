// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// EventIds for Logging.
    /// </summary>
    internal static class LoggingEventId
    {
#pragma warning disable IDE1006 // Naming Styles
        // DistributedCacheAdapter EventIds 100+
        public static readonly EventId DistributedCacheState = new EventId(100, "DistributedCacheState");
        public static readonly EventId DistributedCacheStateWithTime = new EventId(101, "DistributedCacheStateWithTime");
        public static readonly EventId DistributedCacheReadTime = new EventId(102, "DistributedCacheReadTime");
        public static readonly EventId DistributedCacheConnectionError = new EventId(103, "DistributedCacheConnectionError");
        public static readonly EventId DistributedCacheRetry = new EventId(104, "DistributedCacheRetry");
        public static readonly EventId MemoryCacheRemove = new EventId(105, "MemoryCacheRemove");
        public static readonly EventId MemoryCacheRead = new EventId(106, "MemoryCacheRead");
        public static readonly EventId MemoryCacheCount = new EventId(107, "MemoryCacheCount");
        public static readonly EventId BackPropagateL2toL1 = new EventId(108, "BackPropagateL2toL1");

        // SessionCacheProvider EventIds 200+
        public static readonly EventId SessionCache = new EventId(200, "SessionCache");
        public static readonly EventId SessionCacheKeyNotFound = new EventId(201, "SessionCacheKeyNotFound");

        // TokenAcquisition EventIds 300+
        public static readonly EventId TokenAcquisitionError = new EventId(300, "TokenAcquisitionError");
        public static readonly EventId TokenAcquisitionMsalAuthenticationResultTime = new EventId(301, "TokenAcquisitionMsalAuthenticationResultTime");
#pragma warning restore IDE1006 // Naming Styles
    }
}
