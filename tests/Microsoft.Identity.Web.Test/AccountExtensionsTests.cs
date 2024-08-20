// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Security.Claims;
using Microsoft.Identity.Client;
using Microsoft.IdentityModel.Tokens;
using NSubstitute;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    [Collection("ClaimsIdentity_AppContextSwitch_Tests")]
    public class AccountExtensionsTests
    {
        public AccountExtensionsTests()
        {
            AppContextSwitches.ResetState();
        }

        [Fact]
        public void ToClaimsPrincipal_NullAccount_ReturnsNull()
        {
            IAccount account = null!; // Forcing null to test the argument null exception

            Assert.Throws<ArgumentNullException>("account", () => account.ToClaimsPrincipal());
        }

        [Fact]
        public void ToClaimsPrincipal_ValidAccount_ReturnsClaimsPrincipal_WithCaseSensitiveClaimsIdentity()
        {
            var username = "username@test.com";
            var oid = "objectId";
            var tid = "tenantId";

            IAccount account = Substitute.For<IAccount>();
            account.Username.Returns(username);
            // AccountId is in the x.y format, MSAL has some DEBUG only checks on that format
            account.HomeAccountId.Returns(new AccountId($"{oid}.{tid}", oid, tid));

            var claimsPrincipal = account.ToClaimsPrincipal();

            Assert.IsType<CaseSensitiveClaimsIdentity>(claimsPrincipal.Identity);
            var claimsIdentityResult = (CaseSensitiveClaimsIdentity)claimsPrincipal.Identity;

            Assert.Equal(3, claimsIdentityResult.Claims.Count());
            Assert.Equal(username, claimsIdentityResult.FindFirst(ClaimTypes.Upn)?.Value);
            Assert.Equal(oid, claimsIdentityResult.FindFirst(ClaimConstants.Oid)?.Value);
            Assert.Equal(tid, claimsIdentityResult.FindFirst(ClaimConstants.Tid)?.Value);
        }

        [Fact]
        public void ToClaimsPrincipal_AccountWithNullValues_ReturnsEmptyPrincipal_WithCaseSensitiveClaimsIdentity()
        {
            IAccount account = Substitute.For<IAccount>();

            var claimsPrincipal = account.ToClaimsPrincipal();

            Assert.IsType<CaseSensitiveClaimsIdentity>(claimsPrincipal.Identity);
            var claimsIdentityResult = (CaseSensitiveClaimsIdentity)claimsPrincipal.Identity;

            Assert.Single(claimsIdentityResult.Claims);
        }

        [Fact]
        public void ToClaimsPrincipal_WithContextSwitch_AccountWithNullValues_ReturnsEmptyPrincipal_WithClaimsIdentity()
        {
            AppContext.SetSwitch(AppContextSwitches.UseClaimsIdentityTypeSwitchName, true);

            IAccount account = Substitute.For<IAccount>();

            var claimsPrincipal = account.ToClaimsPrincipal();

            Assert.IsType<ClaimsIdentity>(claimsPrincipal.Identity);
            var claimsIdentityResult = (ClaimsIdentity)claimsPrincipal.Identity;

            Assert.Single(claimsIdentityResult.Claims);

            AppContextSwitches.ResetState();
        }
    }
}
