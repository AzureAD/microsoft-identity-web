// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Security.Authentication;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    public class HomeAccountIdentifierClaimsTests
    {
        private const string Uid = "objectId-value";
        private const string Utid = "tenantId-value";

        [Fact]
        public void AddOrValidate_StampsUidUtid_WhenBothPresent()
        {
            var identity = new CaseSensitiveClaimsIdentity();

            HomeAccountIdentifierClaims.AddOrValidate(identity, Uid, Utid);

            Assert.Equal(Uid, identity.FindFirst(ClaimConstants.UniqueObjectIdentifier)?.Value);
            Assert.Equal(Utid, identity.FindFirst(ClaimConstants.UniqueTenantIdentifier)?.Value);
        }

        [Fact]
        public void AddOrValidate_NoOp_WhenIdentityIsNull()
        {
            // Should not throw.
            HomeAccountIdentifierClaims.AddOrValidate(null, Uid, Utid);
        }

        [Theory]
        [InlineData(null, Utid)]
        [InlineData(Uid, null)]
        [InlineData(null, null)]
        public void AddOrValidate_DoesNotStamp_WhenEitherIdentifierMissing(string? uid, string? utid)
        {
            var identity = new CaseSensitiveClaimsIdentity();

            HomeAccountIdentifierClaims.AddOrValidate(identity, uid, utid);

            Assert.False(identity.HasClaim(c => c.Type == ClaimConstants.UniqueObjectIdentifier));
            Assert.False(identity.HasClaim(c => c.Type == ClaimConstants.UniqueTenantIdentifier));
        }

        [Fact]
        public void AddOrValidate_Throws_WhenExistingObjectIdentifierConflicts()
        {
            var identity = new CaseSensitiveClaimsIdentity(new[]
            {
                new Claim(ClaimConstants.UniqueObjectIdentifier, "application-injected-uid"),
            });

            var ex = Assert.Throws<AuthenticationException>(
                () => HomeAccountIdentifierClaims.AddOrValidate(identity, Uid, Utid));

            Assert.Contains(ClaimConstants.UniqueObjectIdentifier, ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void AddOrValidate_Throws_WhenExistingTenantIdentifierConflicts()
        {
            var identity = new CaseSensitiveClaimsIdentity(new[]
            {
                new Claim(ClaimConstants.UniqueTenantIdentifier, "application-injected-utid"),
            });

            var ex = Assert.Throws<AuthenticationException>(
                () => HomeAccountIdentifierClaims.AddOrValidate(identity, Uid, Utid));

            Assert.Contains(ClaimConstants.UniqueTenantIdentifier, ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void AddOrValidate_Succeeds_WhenExistingClaimsMatch()
        {
            var identity = new CaseSensitiveClaimsIdentity(new[]
            {
                new Claim(ClaimConstants.UniqueObjectIdentifier, Uid),
                new Claim(ClaimConstants.UniqueTenantIdentifier, Utid),
            });

            // Matching pre-existing claims must not fail authentication.
            HomeAccountIdentifierClaims.AddOrValidate(identity, Uid, Utid);

            Assert.Equal(Uid, identity.FindFirst(ClaimConstants.UniqueObjectIdentifier)?.Value);
            Assert.Equal(Utid, identity.FindFirst(ClaimConstants.UniqueTenantIdentifier)?.Value);
        }
    }
}
