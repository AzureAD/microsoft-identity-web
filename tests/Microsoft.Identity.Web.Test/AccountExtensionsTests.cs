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
            IAccount account = null;

            var claimsPrincipalResult = account.ToClaimsPrincipal();

            Assert.Null(claimsPrincipalResult);
        }

        [Fact]
        public void ToClaimsPrincipal_ValidAccount_ReturnsClaimsPrincipal()
        {
            var username = "username@test.com";
            var oid = "objectId";
            var tid = "tenantId";

            IAccount account = Substitute.For<IAccount>();
            account.Username.Returns(username);
            account.HomeAccountId.Returns(new AccountId("identifier", oid, tid));

            var claimsIdentityResult = account.ToClaimsPrincipal().Identity as ClaimsIdentity;
            Assert.NotNull(claimsIdentityResult);
            Assert.Equal(3, claimsIdentityResult.Claims.Count());
            Assert.Equal(username, claimsIdentityResult.FindFirst(ClaimTypes.Upn)?.Value);
            Assert.Equal(oid, claimsIdentityResult.FindFirst(ClaimConstants.Oid)?.Value);
            Assert.Equal(tid, claimsIdentityResult.FindFirst(ClaimConstants.Tid)?.Value);
        }

        [Fact]
        public void ToClaimsPrincipal_AccountWithNullValues_ThrowsException()
        {
            IAccount account = Substitute.For<IAccount>();

            Assert.Throws<ArgumentNullException>("value", () => account.ToClaimsPrincipal());
        }
    }
}
