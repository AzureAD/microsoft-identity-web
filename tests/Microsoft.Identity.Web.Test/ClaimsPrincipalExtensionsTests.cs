// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Security.Claims;
using Microsoft.Identity.Web.Test.Common;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    public class ClaimsPrincipalExtensionsTests
    {

        [Fact]
        public void GetNameIdentifierId_WithUtidClaim_ReturnsNameId()
        {
            var claimsPrincipalWithUtid = new ClaimsPrincipal(
                new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimConstants.UniqueObjectIdentifier, TestConstants.Utid)
                })
            );

            Assert.Equal(TestConstants.Utid, claimsPrincipalWithUtid.GetNameIdentifierId());
        }

        [Fact]
        public void GetNameIdentifierId_NoUtidClaim_ReturnsNull()
        {
            var claimsPrincipal = new ClaimsPrincipal();

            Assert.Null(claimsPrincipal.GetNameIdentifierId());
        }


        [Fact]
        public void GetUserFlowId_WithTfpOrUserFlowClaims_ReturnsUserFlowId()
        {
            var claimsPrincipalWithTfp = new ClaimsPrincipal(
                new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimConstants.Tfp, TestConstants.Tfp)
                })
            );
            var claimsPrincipalWithUserFlow = new ClaimsPrincipal(
                new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimConstants.UserFlow, TestConstants.Userflow)
                })
            );
            var claimsPrincipalWithTfpAndUserFlow = new ClaimsPrincipal(
                new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimConstants.Tfp, TestConstants.Tfp),
                    new Claim(ClaimConstants.UserFlow, TestConstants.Userflow)
                })
            );

            Assert.Equal(TestConstants.Tfp, claimsPrincipalWithTfp.GetUserFlowId());
            Assert.Equal(TestConstants.Userflow, claimsPrincipalWithUserFlow.GetUserFlowId());
            Assert.Equal(TestConstants.Tfp, claimsPrincipalWithTfpAndUserFlow.GetUserFlowId());
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
                    new Claim(ClaimConstants.Oid, TestConstants.Oid)
                })
            );
            var claimsPrincipalWithObjectId = new ClaimsPrincipal(
                new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimConstants.ObjectId, TestConstants.ObjectId)
                })
            );
            var claimsPrincipalWithOidAndObjectId = new ClaimsPrincipal(
                new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimConstants.Oid, TestConstants.Oid),
                    new Claim(ClaimConstants.ObjectId, TestConstants.ObjectId)
                })
            );

            Assert.Equal(TestConstants.Oid, claimsPrincipalWithOid.GetObjectId());
            Assert.Equal(TestConstants.ObjectId, claimsPrincipalWithObjectId.GetObjectId());
            Assert.Equal(TestConstants.Oid, claimsPrincipalWithOidAndObjectId.GetObjectId());
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
                    new Claim(ClaimConstants.Tid, TestConstants.Tid)
                })
            );
            var claimsPrincipalWithTenantId = new ClaimsPrincipal(
                new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimConstants.TenantId, TestConstants.TenantId)
                })
            );
            var claimsPrincipalWithTidAndTenantId = new ClaimsPrincipal(
                new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimConstants.Tid, TestConstants.Tid),
                    new Claim(ClaimConstants.TenantId, TestConstants.TenantId)
                })
            );

            Assert.Equal(TestConstants.Tid, claimsPrincipalWithTid.GetTenantId());
            Assert.Equal(TestConstants.TenantId, claimsPrincipalWithTenantId.GetTenantId());
            Assert.Equal(TestConstants.Tid, claimsPrincipalWithTidAndTenantId.GetTenantId());
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
                    new Claim(ClaimConstants.PreferredUserName, TestConstants.PreferredUsername)
                })
            );
            var claimsPrincipalWithNameV1 = new ClaimsPrincipal(
                new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimsIdentity.DefaultNameClaimType, TestConstants.NameV1)
                })
            );
            var claimsPrincipalWithName = new ClaimsPrincipal(
                new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimConstants.Name, TestConstants.Name)
                })
            );
            var claimsPrincipalWithNameV1AndName = new ClaimsPrincipal(
                new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimConstants.Name, TestConstants.Name),
                    new Claim(ClaimsIdentity.DefaultNameClaimType, TestConstants.NameV1)
                })
            );
            var claimsPrincipalWithPreferredUsernameAndNameV1AndName = new ClaimsPrincipal(
                new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimConstants.Name, TestConstants.Name),
                    new Claim(ClaimConstants.PreferredUserName, TestConstants.PreferredUsername),
                    new Claim(ClaimsIdentity.DefaultNameClaimType, TestConstants.NameV1)
                })
            );

            Assert.Equal(TestConstants.PreferredUsername, claimsPrincipalWithPreferredUsername.GetDisplayName());
            Assert.Equal(TestConstants.NameV1, claimsPrincipalWithNameV1.GetDisplayName());
            Assert.Equal(TestConstants.Name, claimsPrincipalWithName.GetDisplayName());
            Assert.Equal(TestConstants.NameV1, claimsPrincipalWithNameV1AndName.GetDisplayName());
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
            var msaTenantId = "9188040D-6c67-4c5b-b112-36a304b66dad";

            var claimsPrincipalWithMsaTenant = new ClaimsPrincipal(
                new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimConstants.TenantId, msaTenantId)
                })
            );

            var claimsPrincipalWithNonMsaTenant = new ClaimsPrincipal(
                new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimConstants.TenantId, TestConstants.TenantIdAsGuid)
                })
            );

            Assert.Equal("consumers", claimsPrincipalWithMsaTenant.GetDomainHint());
            Assert.Equal("organizations", claimsPrincipalWithNonMsaTenant.GetDomainHint());
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
                    new Claim(ClaimConstants.PreferredUserName, TestConstants.PreferredUsername)
                })
            );
            var claimsPrincipalWithNameV1 = new ClaimsPrincipal(
                new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimsIdentity.DefaultNameClaimType, TestConstants.NameV1)
                })
            );
            var claimsPrincipalWithName = new ClaimsPrincipal(
                new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimConstants.Name, TestConstants.Name)
                })
            );
            var claimsPrincipalWithNameV1AndName = new ClaimsPrincipal(
                new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimConstants.Name, TestConstants.Name),
                    new Claim(ClaimsIdentity.DefaultNameClaimType, TestConstants.NameV1)
                })
            );
            var claimsPrincipalWithPreferredUsernameAndNameV1AndName = new ClaimsPrincipal(
                new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimConstants.Name, TestConstants.Name),
                    new Claim(ClaimConstants.PreferredUserName, TestConstants.PreferredUsername),
                    new Claim(ClaimsIdentity.DefaultNameClaimType, TestConstants.NameV1)
                })
            );

            Assert.Equal(TestConstants.PreferredUsername, claimsPrincipalWithPreferredUsername.GetLoginHint());
            Assert.Equal(TestConstants.NameV1, claimsPrincipalWithNameV1.GetLoginHint());
            Assert.Equal(TestConstants.Name, claimsPrincipalWithName.GetLoginHint());
            Assert.Equal(TestConstants.NameV1, claimsPrincipalWithNameV1AndName.GetLoginHint());
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
            var b2cPattern = $"{TestConstants.Utid}.{TestConstants.Tid}";
            var aadPattern = $"{TestConstants.Oid}.{TestConstants.Tid}";

            var claimsPrincipalForB2c = new ClaimsPrincipal(
                new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimConstants.Oid, TestConstants.Oid),
                    new Claim(ClaimConstants.UniqueObjectIdentifier, TestConstants.Utid),
                    new Claim(ClaimConstants.TenantId, TestConstants.Tid),
                    new Claim(ClaimConstants.Tfp, TestConstants.Userflow)
                })
            );
            var claimsPrincipalForAad = new ClaimsPrincipal(
                new ClaimsIdentity(new Claim[]
                {
                                new Claim(ClaimConstants.Oid, TestConstants.Oid),
                                new Claim(ClaimConstants.PreferredUserName, TestConstants.Utid),
                                new Claim(ClaimConstants.TenantId, TestConstants.Tid)
                })
            );

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
