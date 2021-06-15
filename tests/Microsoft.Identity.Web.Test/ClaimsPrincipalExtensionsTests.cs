// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Security.Claims;
using Microsoft.Identity.Web.Test.Common;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    public class ClaimsPrincipalExtensionsTests
    {
        private const string Name = "name-value";
        private const string NameV1 = "name_V1-value";

        [Fact]
        public void GetNameIdentifierId_WithUidClaim_ReturnsNameId()
        {
            var claimsPrincipalWithUid = new ClaimsPrincipal(
                new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimConstants.UniqueObjectIdentifier, TestConstants.Uid),
                }));

            Assert.Equal(TestConstants.Uid, claimsPrincipalWithUid.GetHomeObjectId());
        }

        [Fact]
        public void GetNameIdentifierId_NoUtidClaim_ReturnsNull()
        {
            var claimsPrincipal = new ClaimsPrincipal();

            Assert.Null(claimsPrincipal.GetHomeObjectId());
        }

        [Fact]
        public void GetHomeTenantId_NoUtidClaim_ReturnsNull()
        {
            var claimsPrincipal = new ClaimsPrincipal();

            Assert.Null(claimsPrincipal.GetHomeTenantId());
        }

        [Fact]
        public void GetHomeTenantId_WithUtidClaim_ReturnsHomeTenantId()
        {
            var claimsPrincipalWithUtid = new ClaimsPrincipal(
                new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimConstants.UniqueTenantIdentifier, TestConstants.Utid),
                }));

            Assert.Equal(TestConstants.Utid, claimsPrincipalWithUtid.GetHomeTenantId());
        }

        [Fact]
        public void GetUserFlowId_WithTfpOrUserFlowClaims_ReturnsUserFlowId()
        {
            var claimsPrincipalWithTfp = new ClaimsPrincipal(
                new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimConstants.Tfp, TestConstants.ClaimNameTfp),
                }));
            var claimsPrincipalWithUserFlow = new ClaimsPrincipal(
                new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimConstants.UserFlow, TestConstants.B2CSignUpSignInUserFlow),
                }));
            var claimsPrincipalWithTfpAndUserFlow = new ClaimsPrincipal(
                new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimConstants.Tfp, TestConstants.ClaimNameTfp),
                    new Claim(ClaimConstants.UserFlow, TestConstants.B2CSignUpSignInUserFlow),
                }));

            Assert.Equal(TestConstants.ClaimNameTfp, claimsPrincipalWithTfp.GetUserFlowId());
            Assert.Equal(TestConstants.B2CSignUpSignInUserFlow, claimsPrincipalWithUserFlow.GetUserFlowId());
            Assert.Equal(TestConstants.ClaimNameTfp, claimsPrincipalWithTfpAndUserFlow.GetUserFlowId());
        }

        [Fact]
        public void GetUserFlowId_NoTfpOrUserFlowClaims_ReturnsNull()
        {
            var claimsPrincipal = new ClaimsPrincipal();

            Assert.Null(claimsPrincipal.GetUserFlowId());
        }

        [Fact]
        public void GetObjectId_WithOidOrObjectIdClaims_ReturnsObjectId()
        {
            var claimsPrincipalWithOid = new ClaimsPrincipal(
                new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimConstants.Oid, TestConstants.ObjectIdAsGuid),
                }));
            var claimsPrincipalWithObjectId = new ClaimsPrincipal(
                new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimConstants.ObjectId, TestConstants.Oid),
                }));
            var claimsPrincipalWithOidAndObjectId = new ClaimsPrincipal(
                new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimConstants.Oid, TestConstants.ObjectIdAsGuid),
                    new Claim(ClaimConstants.ObjectId, TestConstants.Oid),
                }));

            Assert.Equal(TestConstants.ObjectIdAsGuid, claimsPrincipalWithOid.GetObjectId());
            Assert.Equal(TestConstants.Oid, claimsPrincipalWithObjectId.GetObjectId());
            Assert.Equal(TestConstants.ObjectIdAsGuid, claimsPrincipalWithOidAndObjectId.GetObjectId());
        }

        [Fact]
        public void GetObjectId_NoOidOrObjectIdClaims_ReturnsNull()
        {
            var claimsPrincipal = new ClaimsPrincipal();

            Assert.Null(claimsPrincipal.GetObjectId());
        }

        [Fact]
        public void GetTenantId_WithTidOrTenantIdClaims_ReturnsTenantId()
        {
            var claimsPrincipalWithTid = new ClaimsPrincipal(
                new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimConstants.Tid, TestConstants.TenantIdAsGuid),
                }));
            var claimsPrincipalWithTenantId = new ClaimsPrincipal(
                new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimConstants.TenantId, TestConstants.GuestTenantId),
                }));
            var claimsPrincipalWithTidAndTenantId = new ClaimsPrincipal(
                new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimConstants.Tid, TestConstants.TenantIdAsGuid),
                    new Claim(ClaimConstants.TenantId, TestConstants.GuestTenantId),
                }));

            Assert.Equal(TestConstants.TenantIdAsGuid, claimsPrincipalWithTid.GetTenantId());
            Assert.Equal(TestConstants.GuestTenantId, claimsPrincipalWithTenantId.GetTenantId());
            Assert.Equal(TestConstants.TenantIdAsGuid, claimsPrincipalWithTidAndTenantId.GetTenantId());
        }

        [Fact]
        public void GetTenantId_NoTidOrTenantIdClaims_ReturnsNull()
        {
            var claimsPrincipal = new ClaimsPrincipal();

            Assert.Null(claimsPrincipal.GetTenantId());
        }

        [Fact]
        public void GetDisplayName_WithSomeOrAllNameClaims_ReturnsName()
        {
            var claimsPrincipalWithPreferredUsername = new ClaimsPrincipal(
                new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimConstants.PreferredUserName, TestConstants.PreferredUsername),
                }));
            var claimsPrincipalWithNameV1 = new ClaimsPrincipal(
                new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimsIdentity.DefaultNameClaimType, NameV1),
                }));
            var claimsPrincipalWithName = new ClaimsPrincipal(
                new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimConstants.Name, Name),
                }));
            var claimsPrincipalWithNameV1AndName = new ClaimsPrincipal(
                new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimConstants.Name, Name),
                    new Claim(ClaimsIdentity.DefaultNameClaimType, NameV1),
                }));
            var claimsPrincipalWithPreferredUsernameAndNameV1AndName = new ClaimsPrincipal(
                new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimConstants.Name, Name),
                    new Claim(ClaimConstants.PreferredUserName, TestConstants.PreferredUsername),
                    new Claim(ClaimsIdentity.DefaultNameClaimType, NameV1),
                }));

            Assert.Equal(TestConstants.PreferredUsername, claimsPrincipalWithPreferredUsername.GetDisplayName());
            Assert.Equal(NameV1, claimsPrincipalWithNameV1.GetDisplayName());
            Assert.Equal(Name, claimsPrincipalWithName.GetDisplayName());
            Assert.Equal(NameV1, claimsPrincipalWithNameV1AndName.GetDisplayName());
            Assert.Equal(TestConstants.PreferredUsername, claimsPrincipalWithPreferredUsernameAndNameV1AndName.GetDisplayName());
        }

        [Fact]
        public void GetDisplayName_NoNameClaims_ReturnsNull()
        {
            var claimsPrincipal = new ClaimsPrincipal();

            Assert.Null(claimsPrincipal.GetDisplayName());
        }

        [Fact]
        public void GetDomainHint_WithTenantIdClaims_ReturnsDomainHint()
        {
            var claimsPrincipalWithMsaTenant = new ClaimsPrincipal(
                new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimConstants.TenantId, Constants.MsaTenantId),
                }));

            var claimsPrincipalWithNonMsaTenant = new ClaimsPrincipal(
                new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimConstants.TenantId, TestConstants.TenantIdAsGuid),
                }));

            Assert.Equal(Constants.Consumers, claimsPrincipalWithMsaTenant.GetDomainHint());
            Assert.Equal(Constants.Organizations, claimsPrincipalWithNonMsaTenant.GetDomainHint());
        }

        [Fact]
        public void GetDomainHint_NoTenantIdClaims_ReturnsNull()
        {
            var claimsPrincipal = new ClaimsPrincipal();

            Assert.Null(claimsPrincipal.GetDomainHint());
        }

        [Fact]
        public void GetLoginHint_WithSomeOrAllNameClaims_ReturnsName()
        {
            var claimsPrincipalWithPreferredUsername = new ClaimsPrincipal(
                new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimConstants.PreferredUserName, TestConstants.PreferredUsername),
                }));
            var claimsPrincipalWithNameV1 = new ClaimsPrincipal(
                new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimsIdentity.DefaultNameClaimType, NameV1),
                }));
            var claimsPrincipalWithName = new ClaimsPrincipal(
                new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimConstants.Name, Name),
                }));
            var claimsPrincipalWithNameV1AndName = new ClaimsPrincipal(
                new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimConstants.Name, Name),
                    new Claim(ClaimsIdentity.DefaultNameClaimType, NameV1),
                }));
            var claimsPrincipalWithPreferredUsernameAndNameV1AndName = new ClaimsPrincipal(
                new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimConstants.Name, Name),
                    new Claim(ClaimConstants.PreferredUserName, TestConstants.PreferredUsername),
                    new Claim(ClaimsIdentity.DefaultNameClaimType, NameV1),
                }));

            Assert.Equal(TestConstants.PreferredUsername, claimsPrincipalWithPreferredUsername.GetLoginHint());
            Assert.Equal(NameV1, claimsPrincipalWithNameV1.GetLoginHint());
            Assert.Equal(Name, claimsPrincipalWithName.GetLoginHint());
            Assert.Equal(NameV1, claimsPrincipalWithNameV1AndName.GetLoginHint());
            Assert.Equal(TestConstants.PreferredUsername, claimsPrincipalWithPreferredUsernameAndNameV1AndName.GetLoginHint());
        }

        [Fact]
        public void GetLoginHint_NoNameClaims_ReturnsNull()
        {
            var claimsPrincipal = new ClaimsPrincipal();

            Assert.Null(claimsPrincipal.GetLoginHint());
        }

        [Fact]
        public void GetMsalAccountId_WithNeededClaims_ReturnsFormattedMsalId()
        {
            var b2cPattern = $"{TestConstants.Uid}-{TestConstants.B2CSignUpSignInUserFlow}.{TestConstants.Utid}";
            var aadPattern = $"{TestConstants.Uid}.{TestConstants.Utid}";

            var claimsPrincipalForB2c = new ClaimsPrincipal(
                new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimConstants.UniqueObjectIdentifier, TestConstants.Uid + "-" + TestConstants.B2CSignUpSignInUserFlow),
                    new Claim(ClaimConstants.UniqueTenantIdentifier, TestConstants.Utid),
                    new Claim(ClaimConstants.Tfp, TestConstants.B2CSignUpSignInUserFlow),
                }));
            var claimsPrincipalForAad = new ClaimsPrincipal(
                new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimConstants.UniqueObjectIdentifier, TestConstants.Uid),
                    new Claim(ClaimConstants.UniqueTenantIdentifier, TestConstants.Utid),
                    new Claim(ClaimConstants.PreferredUserName, TestConstants.Utid),
                }));

            Assert.Contains(TestConstants.B2CSignUpSignInUserFlow, b2cPattern, System.StringComparison.OrdinalIgnoreCase);
            Assert.Equal(b2cPattern, claimsPrincipalForB2c.GetMsalAccountId());
            Assert.Equal(aadPattern, claimsPrincipalForAad.GetMsalAccountId());
        }

        [Fact]
        public void GetMsalAccountId_NoClaims_ReturnsNull()
        {
            var claimsPrincipal = new ClaimsPrincipal();

            Assert.Null(claimsPrincipal.GetMsalAccountId());
        }
    }
}
