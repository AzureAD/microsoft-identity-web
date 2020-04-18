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
        private const string ObjectId = "objectId-value";
        private const string Oid = "oid-value";
        private const string PreferredUsername = "preferred_username-value";
        private const string TenantId = "some-tenant-id";
        private const string Tfp = "tfp-value";
        private const string Tid = "tid-value";
        private const string Userflow = "userflow-value";
        private const string Utid = "utid-value";

        [Fact]
        public void GetNameIdentifierId_WithUtidClaim_ReturnsNameId()
        {
            var claimsPrincipalWithUtid = new ClaimsPrincipal(
                new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimConstants.UniqueObjectIdentifier, Utid)
                }));

            Assert.Equal(Utid, claimsPrincipalWithUtid.GetNameIdentifierId());
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
                    new Claim(ClaimConstants.Tfp, Tfp)
                }));
            var claimsPrincipalWithUserFlow = new ClaimsPrincipal(
                new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimConstants.UserFlow, Userflow)
                }));
            var claimsPrincipalWithTfpAndUserFlow = new ClaimsPrincipal(
                new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimConstants.Tfp, Tfp),
                    new Claim(ClaimConstants.UserFlow, Userflow)
                }));

            Assert.Equal(Tfp, claimsPrincipalWithTfp.GetUserFlowId());
            Assert.Equal(Userflow, claimsPrincipalWithUserFlow.GetUserFlowId());
            Assert.Equal(Tfp, claimsPrincipalWithTfpAndUserFlow.GetUserFlowId());
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
                    new Claim(ClaimConstants.Oid, Oid)
                }));
            var claimsPrincipalWithObjectId = new ClaimsPrincipal(
                new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimConstants.ObjectId, ObjectId)
                }));
            var claimsPrincipalWithOidAndObjectId = new ClaimsPrincipal(
                new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimConstants.Oid, Oid),
                    new Claim(ClaimConstants.ObjectId, ObjectId)
                }));

            Assert.Equal(Oid, claimsPrincipalWithOid.GetObjectId());
            Assert.Equal(ObjectId, claimsPrincipalWithObjectId.GetObjectId());
            Assert.Equal(Oid, claimsPrincipalWithOidAndObjectId.GetObjectId());
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
                    new Claim(ClaimConstants.Tid, Tid)
                }));
            var claimsPrincipalWithTenantId = new ClaimsPrincipal(
                new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimConstants.TenantId, TenantId)
                }));
            var claimsPrincipalWithTidAndTenantId = new ClaimsPrincipal(
                new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimConstants.Tid, Tid),
                    new Claim(ClaimConstants.TenantId, TenantId)
                }));

            Assert.Equal(Tid, claimsPrincipalWithTid.GetTenantId());
            Assert.Equal(TenantId, claimsPrincipalWithTenantId.GetTenantId());
            Assert.Equal(Tid, claimsPrincipalWithTidAndTenantId.GetTenantId());
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
                    new Claim(ClaimConstants.PreferredUserName, PreferredUsername)
                }));
            var claimsPrincipalWithNameV1 = new ClaimsPrincipal(
                new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimsIdentity.DefaultNameClaimType, NameV1)
                }));
            var claimsPrincipalWithName = new ClaimsPrincipal(
                new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimConstants.Name, Name)
                }));
            var claimsPrincipalWithNameV1AndName = new ClaimsPrincipal(
                new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimConstants.Name, Name),
                    new Claim(ClaimsIdentity.DefaultNameClaimType, NameV1)
                }));
            var claimsPrincipalWithPreferredUsernameAndNameV1AndName = new ClaimsPrincipal(
                new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimConstants.Name, Name),
                    new Claim(ClaimConstants.PreferredUserName, PreferredUsername),
                    new Claim(ClaimsIdentity.DefaultNameClaimType, NameV1)
                }));

            Assert.Equal(PreferredUsername, claimsPrincipalWithPreferredUsername.GetDisplayName());
            Assert.Equal(NameV1, claimsPrincipalWithNameV1.GetDisplayName());
            Assert.Equal(Name, claimsPrincipalWithName.GetDisplayName());
            Assert.Equal(NameV1, claimsPrincipalWithNameV1AndName.GetDisplayName());
            Assert.Equal(PreferredUsername, claimsPrincipalWithPreferredUsernameAndNameV1AndName.GetDisplayName());
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
                }));

            var claimsPrincipalWithNonMsaTenant = new ClaimsPrincipal(
                new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimConstants.TenantId, TestConstants.TenantIdAsGuid)
                }));

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
                    new Claim(ClaimConstants.PreferredUserName, PreferredUsername)
                }));
            var claimsPrincipalWithNameV1 = new ClaimsPrincipal(
                new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimsIdentity.DefaultNameClaimType, NameV1)
                }));
            var claimsPrincipalWithName = new ClaimsPrincipal(
                new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimConstants.Name, Name)
                }));
            var claimsPrincipalWithNameV1AndName = new ClaimsPrincipal(
                new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimConstants.Name, Name),
                    new Claim(ClaimsIdentity.DefaultNameClaimType, NameV1)
                }));
            var claimsPrincipalWithPreferredUsernameAndNameV1AndName = new ClaimsPrincipal(
                new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimConstants.Name, Name),
                    new Claim(ClaimConstants.PreferredUserName, PreferredUsername),
                    new Claim(ClaimsIdentity.DefaultNameClaimType, NameV1)
                }));

            Assert.Equal(PreferredUsername, claimsPrincipalWithPreferredUsername.GetLoginHint());
            Assert.Equal(NameV1, claimsPrincipalWithNameV1.GetLoginHint());
            Assert.Equal(Name, claimsPrincipalWithName.GetLoginHint());
            Assert.Equal(NameV1, claimsPrincipalWithNameV1AndName.GetLoginHint());
            Assert.Equal(PreferredUsername, claimsPrincipalWithPreferredUsernameAndNameV1AndName.GetLoginHint());
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
            var b2cPattern = $"{Utid}.{Tid}";
            var aadPattern = $"{Oid}.{Tid}";

            var claimsPrincipalForB2c = new ClaimsPrincipal(
                new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimConstants.Oid, Oid),
                    new Claim(ClaimConstants.UniqueObjectIdentifier, Utid),
                    new Claim(ClaimConstants.TenantId, Tid),
                    new Claim(ClaimConstants.Tfp, Userflow)
                }));
            var claimsPrincipalForAad = new ClaimsPrincipal(
                new ClaimsIdentity(new Claim[]
                {
                                new Claim(ClaimConstants.Oid, Oid),
                                new Claim(ClaimConstants.PreferredUserName, Utid),
                                new Claim(ClaimConstants.TenantId, Tid)
                }));

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
