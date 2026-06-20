// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Xunit;

namespace Microsoft.Identity.Web.Test.Common
{
    public sealed class IgnoreOnAzureDevopsFactAttribute : FactAttribute
    {
        public IgnoreOnAzureDevopsFactAttribute()
        {
            // No longer auto-skipping on Azure DevOps — these tests should run
            // on self-hosted agents (e.g., MSALMSIV2) that have the required
            // certificates and KeyVault access.
        }

        /// <summary>Determine if runtime is Azure DevOps.</summary>
        /// <returns>True if being executed in Azure DevOps, false otherwise.</returns>
        public static bool IsRunningOnAzureDevOps()
        {
            return Environment.GetEnvironmentVariable("SYSTEM_DEFINITIONID") != null;
        }
    }
}
