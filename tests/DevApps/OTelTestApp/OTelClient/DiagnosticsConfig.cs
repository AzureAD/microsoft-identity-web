// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace OTelClient
{
    public static class DiagnosticsConfig
    {
        public const string ServiceName = "IdWebTestWithOTel";
        public static ActivitySource ActivitySource = new ActivitySource(ServiceName);

        public static Meter meter = new(ServiceName);
        public static Counter<long> AppTokenRequestCounter = meter.CreateCounter<long>("app_token_request_counter");
    }
}
