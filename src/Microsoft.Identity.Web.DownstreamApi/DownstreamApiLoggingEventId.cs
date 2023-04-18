// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.Identity.Web
{
    internal static class DownstreamApiLoggingEventId
    {
#pragma warning disable IDE1006 // Naming styles
        // DownstreamApi EventIds 100+
        public static readonly EventId HttpRequestError = new EventId(100, "HttpRequestError");
        public static readonly EventId UnauthenticatedApiCall = new EventId(101, "UnauthenticatedApiCall");
#pragma warning restore IDE1006 // Naming styles
    }
}
