// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Identity.Web.Perf.Client
{
    public class TestRunnerOptions
    {
        public string TestServiceBaseUri { get; set; }
        public string TestUri { get; set; }
        public int RuntimeInMinutes { get; set; }
        public bool RunIndefinitely { get; set; }
        public int UserNumberToStart { get; set; } = 1;
        public int UsersCountToTest { get; set; } = 1;
        public string UserPassword { get; set; }
        public string UsernamePrefix { get; set; } = "MIWTestUser";
        public string TenantDomain { get; set; }
        public string ApiScopes { get; set; }
        public string ClientId { get; set; }
    }
}
