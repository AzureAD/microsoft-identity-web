// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;

namespace Microsoft.Identity.Web
{
    internal static class DownstreamApiLoggingEventId
    {
#pragma warning disable IDE1006 // Naming styles
        // DownstreamApi EventIds 100+
        public static readonly EventId HttpRequestError = new(100, "HttpRequestError");
        public static readonly EventId UnauthenticatedApiCall = new(101, "UnauthenticatedApiCall");
#pragma warning restore IDE1006 // Naming styles
    }
}
