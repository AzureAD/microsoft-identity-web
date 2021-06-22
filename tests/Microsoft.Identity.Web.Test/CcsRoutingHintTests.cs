// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Security.Claims;
using Microsoft.Identity.Web.Test.Common;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    public class CcsRoutingHintTests
    {
        [Fact]
        public void ClaimsPrincipalReturnsRoutingHintTest()
        {
            ClaimsPrincipal claimsPrincipal = new ClaimsPrincipal(
               new ClaimsIdentity(new Claim[]
               {
                    new Claim(ClaimConstants.Oid, TestConstants.Oid),
                    new Claim(ClaimConstants.Tid, TestConstants.TenantIdAsGuid),
               }));

            string hint = CcsRoutingHintExtensions.CreateCcsRoutingHintFromHttpContext(claimsPrincipal);
            Assert.StartsWith($"oid:{TestConstants.Oid}", hint, StringComparison.OrdinalIgnoreCase);
            Assert.EndsWith(TestConstants.TenantIdAsGuid, hint, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("@", hint, StringComparison.OrdinalIgnoreCase);
        }

        [Theory]
        [InlineData(TestConstants.Uid, "")]
        [InlineData("", TestConstants.TenantIdAsGuid)]
        [InlineData("", "")]
        public void ClaimsPrincipalMissingOidTid_ReturnsEmtpyStringTest(string oid, string tid)
        {
            ClaimsPrincipal claimsPrincipal = new ClaimsPrincipal(
               new ClaimsIdentity(new Claim[]
               {
                    new Claim(ClaimConstants.Oid, oid),
                    new Claim(ClaimConstants.Tid, tid),
               }));

            Assert.Equal(string.Empty, CcsRoutingHintExtensions.CreateCcsRoutingHintFromHttpContext(claimsPrincipal));
        }
    }
}
