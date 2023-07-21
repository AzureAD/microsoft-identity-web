// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Security.Claims;
using Microsoft.Identity.Client;
using NSubstitute;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    public class AccountExtensionsTests
    {
        [Fact]
        public void ToClaimsPrincipal_NullAccount_ReturnsNull()
        {
            IAccount account = null!; // Forcing null to test the argument null exception

            Assert.Throws<ArgumentNullException>("account", () => account.ToClaimsPrincipal());
        }

        [Fact]
        public void ToClaimsPrincipal_ValidAccount_ReturnsClaimsPrincipal()
        {
            var username = "username@test.com";
            var oid = "objectId";
            var tid = "tenantId";

            IAccount account = Substitute.For<IAccount>();
            account.Username.Returns(username);
            // AccountId is in the x.y format, MSAL has some DEBUG only checks on that format
            account.HomeAccountId.Returns(new AccountId($"{oid}.{tid}", oid, tid)); 

            var claimsIdentityResult = account.ToClaimsPrincipal().Identity as ClaimsIdentity;

            Assert.NotNull(claimsIdentityResult);
            Assert.Equal(3, claimsIdentityResult.Claims.Count());
            Assert.Equal(username, claimsIdentityResult.FindFirst(ClaimTypes.Upn)?.Value);
            Assert.Equal(oid, claimsIdentityResult.FindFirst(ClaimConstants.Oid)?.Value);
            Assert.Equal(tid, claimsIdentityResult.FindFirst(ClaimConstants.Tid)?.Value);
        }

        [Fact]
        public void ToClaimsPrincipal_AccountWithNullValues_ReturnsEmptyPrincipal()
        {
            IAccount account = Substitute.For<IAccount>();

            var claimsIdentityResult = account.ToClaimsPrincipal().Identity as ClaimsIdentity;

            Assert.NotNull(claimsIdentityResult);
            Assert.Single(claimsIdentityResult.Claims);
        }
    }
}
