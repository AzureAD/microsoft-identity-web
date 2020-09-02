// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Security.Claims;
using Microsoft.Identity.Web.Test.Common;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    public class ClaimsPrincipalFactoryTests
    {
        [Fact]
        public void FromTenantIdAndObjectId_NullParameters_ThrowsException()
        {
            Assert.Throws<ArgumentNullException>(TestConstants.Value, () => ClaimsPrincipalFactory.FromTenantIdAndObjectId(TestConstants.Utid, null));
            Assert.Throws<ArgumentNullException>(TestConstants.Value, () => ClaimsPrincipalFactory.FromTenantIdAndObjectId(null, TestConstants.Uid));
        }

        [Fact]
        public void FromTenantIdAndObjectId_ValidParameters_ReturnsClaimsPrincipal()
        {
            var claimsIdentityResult = ClaimsPrincipalFactory.FromTenantIdAndObjectId(TestConstants.Utid, TestConstants.Uid).Identity as ClaimsIdentity;
            Assert.NotNull(claimsIdentityResult);
            Assert.Equal(2, claimsIdentityResult.Claims.Count());
            Assert.Equal(TestConstants.Uid, claimsIdentityResult.FindFirst(ClaimConstants.UniqueObjectIdentifier)?.Value);
            Assert.Equal(TestConstants.Utid, claimsIdentityResult.FindFirst(ClaimConstants.UniqueTenantIdentifier)?.Value);
        }
    }
}
