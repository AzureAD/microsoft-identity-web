// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web.Test.Common;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    public class WebAppExtentionsTests
    {
        [Theory]
        [InlineData(TestConstants.AuthorityCommonTenant, TestConstants.AuthorityCommonTenantWithV2)]
        [InlineData(TestConstants.AuthorityOrganizationsUSTenant, TestConstants.AuthorityOrganizationsUSWithV2)]
        [InlineData(TestConstants.AuthorityCommonTenantWithV2, TestConstants.AuthorityCommonTenantWithV2)]
        [InlineData(TestConstants.AuthorityCommonTenantWithV2 + "/", TestConstants.AuthorityCommonTenantWithV2)]
        [InlineData(TestConstants.B2CAuthorityWithV2, TestConstants.B2CAuthorityWithV2)]
        [InlineData(TestConstants.B2CCustomDomainAuthorityWithV2, TestConstants.B2CCustomDomainAuthorityWithV2)]
        [InlineData(TestConstants.B2CAuthority, TestConstants.B2CAuthorityWithV2)]
        [InlineData(TestConstants.B2CCustomDomainAuthority, TestConstants.B2CCustomDomainAuthorityWithV2)]
        public void EnsureAuthorityIsV2_0(string initialAuthority, string expectedAuthority)
        {
            OpenIdConnectOptions options = new OpenIdConnectOptions
            {
                Authority = initialAuthority
            };

            WebAppAuthenticationBuilderExtensions.EnsureAuthorityIsV2_0(options);
            Assert.Equal(expectedAuthority, options.Authority);
        }
    }
}
