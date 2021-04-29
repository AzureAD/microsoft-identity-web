// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    public class AppServicesAuthenticationInformationTests
    {
        [Fact]
        public void SimulateGetttingHeaderFromDebugEnvironmentVariable()
        {
            try
            {
                Environment.SetEnvironmentVariable(
                  AppServicesAuthenticationInformation.AppServicesAuthDebugHeadersEnvironmentVariable,
                  "a;at:xyz");

                var res = AppServicesAuthenticationInformation.SimulateGetttingHeaderFromDebugEnvironmentVariable("at");

                Assert.Equal("xyz", res);

                Environment.SetEnvironmentVariable(
                 AppServicesAuthenticationInformation.AppServicesAuthDebugHeadersEnvironmentVariable,
                 "at:xyz");

                res = AppServicesAuthenticationInformation.SimulateGetttingHeaderFromDebugEnvironmentVariable("at");

                Assert.Equal("xyz", res);
            }
            finally
            {
                Environment.SetEnvironmentVariable(
                   AppServicesAuthenticationInformation.AppServicesAuthDebugHeadersEnvironmentVariable,
                   string.Empty);
            }
        }
    }
}
