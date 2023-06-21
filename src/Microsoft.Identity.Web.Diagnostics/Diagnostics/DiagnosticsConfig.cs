// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Diagnostics;

namespace Microsoft.Identity.Web.Diagnostics.Diagnostics
{
    internal static class DiagnosticsConfig
    {
        public const string ServiceName = "IdWebTestWithOTel";
        public static ActivitySource s_activitySource = new(ServiceName);
    }
}
