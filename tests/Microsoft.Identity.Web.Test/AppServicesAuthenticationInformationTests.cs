// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Security.Claims;
using Microsoft.Extensions.Primitives;
using Microsoft.Identity.Web.Test.Common;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    [Collection("ClaimsIdentity_AppContextSwitch_Tests")]
    public class AppServicesAuthenticationInformationTests
    {
        public AppServicesAuthenticationInformationTests()
        {
            AppContextSwitches.ResetState();
        }

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

        [Fact]
        public void GetUser_NullParameters_ReturnsNull()
        {
            var headersIdTokenOnly = new Dictionary<string, StringValues>
            {
                { AppServicesAuthenticationInformation.AppServicesAuthIdTokenHeader, new StringValues(TestConstants.IdToken) },
            };

            var headersIdpOnly = new Dictionary<string, StringValues>
            {
                { "X-MS-CLIENT-PRINCIPAL-IDP", new StringValues(TestConstants.AadInstance) },
            };

            Assert.Null(AppServicesAuthenticationInformation.GetUser(headersIdpOnly));
            Assert.Null(AppServicesAuthenticationInformation.GetUser(headersIdTokenOnly));
        }

        [Fact]
        public void GetUser_ReturnsClaimsPrincipal_WithCaseSensitiveClaimsIdentity()
        {
            var headers = new Dictionary<string, StringValues>
            {
                { AppServicesAuthenticationInformation.AppServicesAuthIdTokenHeader, new StringValues(TestConstants.IdToken) },
                { "X-MS-CLIENT-PRINCIPAL-IDP", new StringValues(TestConstants.AadInstance) },
            };

            var claimsPrincipal = AppServicesAuthenticationInformation.GetUser(headers);

            Assert.NotNull(claimsPrincipal);
            Assert.IsType<CaseSensitiveClaimsIdentity>(claimsPrincipal.Identity);
            var claimsIdentityResult = (CaseSensitiveClaimsIdentity)claimsPrincipal.Identity;

            Assert.Single(claimsIdentityResult.Claims);
        }

        [Fact]
        public void GetUser_WithContextSwitch_ReturnsClaimsPrincipal_WithClaimsIdentity()
        {
            AppContext.SetSwitch(AppContextSwitches.UseClaimsIdentityTypeSwitchName, true);

            var headers = new Dictionary<string, StringValues>
            {
                { AppServicesAuthenticationInformation.AppServicesAuthIdTokenHeader, new StringValues(TestConstants.IdToken) },
                { "X-MS-CLIENT-PRINCIPAL-IDP", new StringValues(TestConstants.AadInstance) },
            };

            var claimsPrincipal = AppServicesAuthenticationInformation.GetUser(headers);

            Assert.NotNull(claimsPrincipal);
            Assert.IsType<ClaimsIdentity>(claimsPrincipal.Identity);
            var claimsIdentityResult = (ClaimsIdentity)claimsPrincipal.Identity;

            Assert.Single(claimsIdentityResult.Claims);

            AppContextSwitches.ResetState();
        }
    }
}
