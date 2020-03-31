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
        private string _objectId = "objectId";
        private string _tenantId = "tenantId";

        [Fact]
        public void FromTenantIdAndObjectId_NullParameters_ThrowsException()
        {
            Assert.Throws<ArgumentNullException>("value", () => ClaimsPrincipalFactory.FromTenantIdAndObjectId(_tenantId, null));
            Assert.Throws<ArgumentNullException>("value", () => ClaimsPrincipalFactory.FromTenantIdAndObjectId(null, _objectId));
        }

        [Fact]
        public void FromTenantIdAndObjectId_ValidParameters_ReturnsClaimsPrincipal()
        {
            var claimsIdentityResult = ClaimsPrincipalFactory.FromTenantIdAndObjectId(_tenantId, _objectId).Identity as ClaimsIdentity;
            Assert.NotNull(claimsIdentityResult);
            Assert.Equal(2, claimsIdentityResult.Claims.Count());
            Assert.Equal(_objectId, claimsIdentityResult.FindFirst(ClaimConstants.Oid)?.Value);
            Assert.Equal(_tenantId, claimsIdentityResult.FindFirst(ClaimConstants.Tid)?.Value);
        }
    }
}
