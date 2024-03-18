// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web.Test.Common;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    public class CiamAuthorityHelperTest
    {
        [Fact]
        public void CiamHelperWithNonCiamAuthority()
        {
            MicrosoftIdentityApplicationOptions options = new MicrosoftIdentityApplicationOptions
            {
                Instance = TestConstants.AadInstance +"/",
                TenantId = TestConstants.Organizations
            };

            CiamAuthorityHelper.BuildCiamAuthorityIfNeeded(options);
            Assert.Equal(TestConstants.AuthorityOrganizationsWithV2, options.Authority);
            Assert.Equal(TestConstants.Organizations, options.TenantId);
        }

        [Fact]
        public void CiamHelperWithCiamTenantSpecified()
        {
            MicrosoftIdentityApplicationOptions options = new MicrosoftIdentityApplicationOptions
            {
                Instance = $"https://tenant{Constants.CiamAuthoritySuffix}/",
                TenantId = TestConstants.B2CCustomDomainTenant
            };

            CiamAuthorityHelper.BuildCiamAuthorityIfNeeded(options);
            Assert.Equal($"https://tenant{Constants.CiamAuthoritySuffix}/{TestConstants.B2CCustomDomainTenant}/v2.0", options.Authority);
            Assert.Equal(TestConstants.B2CCustomDomainTenant, options.TenantId);
            Assert.Equal($"https://tenant{Constants.CiamAuthoritySuffix}/", options.Instance);
        }

        [Fact]
        public void CiamHelperWithCiamTenantInferredFromAuthority()
        {
            MicrosoftIdentityApplicationOptions options = new MicrosoftIdentityApplicationOptions
            {
                Instance = $"https://cpimtestpartners{Constants.CiamAuthoritySuffix}/",
            };

            CiamAuthorityHelper.BuildCiamAuthorityIfNeeded(options);
            Assert.Equal($"https://cpimtestpartners{Constants.CiamAuthoritySuffix}/{TestConstants.B2CCustomDomainTenant}/v2.0", options.Authority);
            Assert.Equal(TestConstants.B2CCustomDomainTenant, options.TenantId);
            Assert.Equal($"https://cpimtestpartners{Constants.CiamAuthoritySuffix}/", options.Instance);
        }

        [Fact]
        public void CiamHelperWithCiamTenantInferredFromInstance()
        {
            MicrosoftIdentityApplicationOptions options = new MicrosoftIdentityApplicationOptions
            {
                Authority = $"https://cpimtestpartners{Constants.CiamAuthoritySuffix}/",
            };

            CiamAuthorityHelper.BuildCiamAuthorityIfNeeded(options);
            Assert.Equal($"https://cpimtestpartners{Constants.CiamAuthoritySuffix}/{TestConstants.B2CCustomDomainTenant}/v2.0", options.Authority);
            Assert.Equal(TestConstants.B2CCustomDomainTenant, options.TenantId);
            Assert.Equal($"https://cpimtestpartners{Constants.CiamAuthoritySuffix}/", options.Instance);
        }
    }
}
