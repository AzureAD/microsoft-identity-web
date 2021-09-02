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
        // SessionCacheProvider EventIds 200+
        public static readonly EventId SessionCache = new EventId(200, "SessionCache");
        public static readonly EventId SessionCacheKeyNotFound = new EventId(201, "SessionCacheKeyNotFound");

        // TokenAcquisition EventIds 300+
        public static readonly EventId TokenAcquisitionError = new EventId(300, "TokenAcquisitionError");
        public static readonly EventId TokenAcquisitionMsalAuthenticationResultTime = new EventId(301, "TokenAcquisitionMsalAuthenticationResultTime");
#pragma warning restore IDE1006 // Naming Styles
    }
}
