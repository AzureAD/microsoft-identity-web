// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    public class AppServicesAuthenticationInformationTests
    {
        [Fact]
        public void SimulateGettingHeaderFromDebugEnvironmentVariable()
        {
            try
            {
                Environment.SetEnvironmentVariable(
                  AppServicesAuthenticationInformation.AppServicesAuthDebugHeadersEnvironmentVariable,
                  $"a;{AppServicesAuthenticationInformation.AppServicesAuthIdTokenHeader}:xyz");

                var res = AppServicesAuthenticationInformation.GetIdToken(
                    new Dictionary<string, StringValues>()
                    {
                        { AppServicesAuthenticationInformation.AppServicesAuthIdTokenHeader, new StringValues(string.Empty) },
                    });

#if DEBUG
                Assert.Equal("xyz", res);
#else
                Assert.Equal(string.Empty, res);
#endif
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
