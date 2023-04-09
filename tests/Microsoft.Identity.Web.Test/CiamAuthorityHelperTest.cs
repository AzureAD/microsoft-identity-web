// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Abstractions;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    public class CiamAuthorityHelperTest
    {
        [Fact]
        public void NotCiam()
        {
            MicrosoftIdentityApplicationOptions options = new MicrosoftIdentityApplicationOptions
            {
                Instance = "https://login.microsoftonline.com/",
                TenantId = "common"
            };

            CiamAuthorityHelper.BuildCiamAuthorityIfNeeded(options);
            Assert.Equal("https://login.microsoftonline.com/common/v2.0", options.Authority);
            Assert.Equal("common", options.TenantId);
        }

        [Fact]
        public void CiamTenantSpecified()
        {
            MicrosoftIdentityApplicationOptions options = new MicrosoftIdentityApplicationOptions
            {
                Instance = "https://tenant.ciamlogin.com/",
                TenantId = "aDomain"
            };

            CiamAuthorityHelper.BuildCiamAuthorityIfNeeded(options);
            Assert.Equal("https://tenant.ciamlogin.com/aDomain/v2.0", options.Authority);
            Assert.Equal("aDomain", options.TenantId);
            Assert.Equal("https://tenant.ciamlogin.com/", options.Instance);
        }

        [Fact]
        public void CiamTenantInferredFromAuthority()
        {
            MicrosoftIdentityApplicationOptions options = new MicrosoftIdentityApplicationOptions
            {
                Instance = "https://tenant.ciamlogin.com/",
            };

            CiamAuthorityHelper.BuildCiamAuthorityIfNeeded(options);
            Assert.Equal("https://tenant.ciamlogin.com/tenant.onmicrosoft.com/v2.0", options.Authority);
            Assert.Equal("tenant.onmicrosoft.com", options.TenantId);
            Assert.Equal("https://tenant.ciamlogin.com/", options.Instance);
        }

        [Fact]
        public void CiamTenantInferredFromInstance()
        {
            MicrosoftIdentityApplicationOptions options = new MicrosoftIdentityApplicationOptions
            {
                Authority = "https://tenant.ciamlogin.com/",
            };

            CiamAuthorityHelper.BuildCiamAuthorityIfNeeded(options);
            Assert.Equal("https://tenant.ciamlogin.com/tenant.onmicrosoft.com/v2.0", options.Authority);
            Assert.Equal("tenant.onmicrosoft.com", options.TenantId);
            Assert.Equal("https://tenant.ciamlogin.com/", options.Instance);
        }
    }
}
