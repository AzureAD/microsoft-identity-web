// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Xunit;

namespace Microsoft.Identity.Web.Test.Common
{
    public sealed class OnlyOnAzureDevopsFactAttribute : FactAttribute
    {
        public OnlyOnAzureDevopsFactAttribute()
        {
            if (Environment.GetEnvironmentVariable("SYSTEM_DEFINITIONID") != null)
            {
                return;
            }
            Skip = "Ignored when not on Azure DevOps";
        }
    }
}
