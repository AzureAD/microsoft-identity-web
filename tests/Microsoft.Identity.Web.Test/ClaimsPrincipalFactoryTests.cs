// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Security.Claims;
using Microsoft.Identity.Web.Test.Common;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    [Collection("ClaimsIdentity_AppContextSwitch_Tests")]
    public class ClaimsPrincipalFactoryTests
    {
        public ClaimsPrincipalFactoryTests()
        {
            AppContextSwitches.ResetState();
        }

        [Fact]
        public void FromHomeTenantIdAndHomeObjectId_NullParameters_ThrowsException()
        {
            Assert.Throws<ArgumentNullException>(TestConstants.Value, () => ClaimsPrincipalFactory.FromHomeTenantIdAndHomeObjectId(TestConstants.Utid, null!));
            Assert.Throws<ArgumentNullException>(TestConstants.Value, () => ClaimsPrincipalFactory.FromHomeTenantIdAndHomeObjectId(null!, TestConstants.Uid));
        }

        [Fact]
        public void FromHomeTenantIdAndHomeObjectId_ValidParameters_ReturnsClaimsPrincipal_WithCaseSensitiveClaimsIdentity()
        {
            var claimsPrincipal = ClaimsPrincipalFactory.FromHomeTenantIdAndHomeObjectId(TestConstants.Utid, TestConstants.Uid);

            Assert.IsType<CaseSensitiveClaimsIdentity>(claimsPrincipal.Identity);
            var claimsIdentityResult = (CaseSensitiveClaimsIdentity)claimsPrincipal.Identity;

            Assert.Equal(2, claimsIdentityResult.Claims.Count());
            Assert.Equal(TestConstants.Uid, claimsIdentityResult.FindFirst(ClaimConstants.UniqueObjectIdentifier)?.Value);
            Assert.Equal(TestConstants.Utid, claimsIdentityResult.FindFirst(ClaimConstants.UniqueTenantIdentifier)?.Value);
        }

        [Fact]
        public void FromHomeTenantIdAndHomeObjectId_WithContextSwitch_ValidParameters_ReturnsClaimsPrincipal_WithClaimsIdentity()
        {
            AppContext.SetSwitch(AppContextSwitches.UseClaimsIdentityTypeSwitchName, true);

            var claimsPrincipal = ClaimsPrincipalFactory.FromHomeTenantIdAndHomeObjectId(TestConstants.Utid, TestConstants.Uid);

            Assert.IsType<ClaimsIdentity>(claimsPrincipal.Identity);
            var claimsIdentityResult = (ClaimsIdentity)claimsPrincipal.Identity;

            Assert.Equal(2, claimsIdentityResult.Claims.Count());
            Assert.Equal(TestConstants.Uid, claimsIdentityResult.FindFirst(ClaimConstants.UniqueObjectIdentifier)?.Value);
            Assert.Equal(TestConstants.Utid, claimsIdentityResult.FindFirst(ClaimConstants.UniqueTenantIdentifier)?.Value);

            AppContextSwitches.ResetState();
        }

        [Fact]
        public void FromTenantIdAndObjectId_NullParameters_ThrowsException()
        {
            Assert.Throws<ArgumentNullException>(TestConstants.Value, () => ClaimsPrincipalFactory.FromTenantIdAndObjectId(TestConstants.ClaimNameTid, null!));
            Assert.Throws<ArgumentNullException>(TestConstants.Value, () => ClaimsPrincipalFactory.FromTenantIdAndObjectId(null!, TestConstants.Oid));
        }

        [Fact]
        public void FromTenantIdAndObjectId_ValidParameters_ReturnsClaimsPrincipal_WithCaseSensitiveClaimsIdentity()
        {
            var claimsPrincipal = ClaimsPrincipalFactory.FromTenantIdAndObjectId(TestConstants.GuestTenantId, TestConstants.Oid);

            Assert.IsType<CaseSensitiveClaimsIdentity>(claimsPrincipal.Identity);
            var claimsIdentityResult = (CaseSensitiveClaimsIdentity)claimsPrincipal.Identity;

            Assert.Equal(2, claimsIdentityResult.Claims.Count());
            Assert.Equal(TestConstants.Oid, claimsIdentityResult.FindFirst(ClaimConstants.Oid)?.Value);
            Assert.Equal(TestConstants.GuestTenantId, claimsIdentityResult.FindFirst(ClaimConstants.Tid)?.Value);
        }

        [Fact]
        public void FromTenantIdAndObjectId_WithContextSwitch_ValidParameters_ReturnsClaimsPrincipal_WithClaimsIdentity()
        {
            AppContext.SetSwitch(AppContextSwitches.UseClaimsIdentityTypeSwitchName, true);

            var claimsPrincipal = ClaimsPrincipalFactory.FromTenantIdAndObjectId(TestConstants.GuestTenantId, TestConstants.Oid);

            Assert.IsType<ClaimsIdentity>(claimsPrincipal.Identity);
            var claimsIdentityResult = (ClaimsIdentity)claimsPrincipal.Identity;

            Assert.Equal(2, claimsIdentityResult.Claims.Count());
            Assert.Equal(TestConstants.Oid, claimsIdentityResult.FindFirst(ClaimConstants.Oid)?.Value);
            Assert.Equal(TestConstants.GuestTenantId, claimsIdentityResult.FindFirst(ClaimConstants.Tid)?.Value);

            AppContextSwitches.ResetState();
        }
    }
}
