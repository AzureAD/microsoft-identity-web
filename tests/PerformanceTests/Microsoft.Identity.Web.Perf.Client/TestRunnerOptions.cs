// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Web.Perf.Client
{
    public class TestRunnerOptions
    {
        public string TestServiceBaseUri { get; set; }
        public string TestUri { get; set; }
        public int RuntimeInMinutes { get; set; } = 1;
        public bool RunIndefinitely { get; set; } = false;
        public int RequestDelayInMilliseconds { get; set; } = 1;
        public int StartUserIndex { get; set; } = 1;
        public int NumberOfUsersToTest { get; set; } = 1;
        public string UserPassword { get; set; }
        public string UsernamePrefix { get; set; } = "MIWTestUser";
        public string TenantDomain { get; set; }
        public string ApiScopes { get; set; }
        public string ClientId { get; set; }
        public bool EnableMsalLogging { get; set; } = true;
        public int NumberOfParallelTasks { get; set; } = 1;
    }
}
