// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Xunit;

namespace TokenAcquirerTests
{
    public sealed class OnlyOnAzureDevopsFactAttribute : FactAttribute
    {
        public OnlyOnAzureDevopsFactAttribute()
        {
            if (IgnoreOnAzureDevopsFactAttribute.IsRunningOnAzureDevOps())
            {
                return;
            }

            Skip = "Ignored when not on Azure DevOps";
        }
    }
}
