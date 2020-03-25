// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Security.Claims;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    public class ClaimsPrincipalFactoryTests
    {
        [Fact]
        public void FromTenantIdAndObjectId_NullParameters_ThrowsException()
        {
            var objectId = "objectId";
            var tenantId = "tenantId";

            Assert.Throws<ArgumentNullException>("value", () => ClaimsPrincipalFactory.FromTenantIdAndObjectId(tenantId, null));
            Assert.Throws<ArgumentNullException>("value", () => ClaimsPrincipalFactory.FromTenantIdAndObjectId(null, objectId));
        }

        [Fact]
        public void FromTenantIdAndObjectId_ValidParameters_ReturnsClaimsPrincipal()
        {
            var objectId = "objectId";
            var tenantId = "tenantId";

            var claimsIdentityResult = ClaimsPrincipalFactory.FromTenantIdAndObjectId(tenantId, objectId).Identity as ClaimsIdentity;
            Assert.NotNull(claimsIdentityResult);
            Assert.Equal(2, claimsIdentityResult.Claims.Count());
            Assert.Equal(objectId, claimsIdentityResult.FindFirst(ClaimConstants.Oid)?.Value);
            Assert.Equal(tenantId, claimsIdentityResult.FindFirst(ClaimConstants.Tid)?.Value);
        }
    }
}
