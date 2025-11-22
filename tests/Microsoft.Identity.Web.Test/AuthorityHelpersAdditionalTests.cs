// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.Test.Common;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    /// <summary>
    /// Additional tests for AuthorityHelpers utility methods.
    /// Issue #3610: Extended coverage for authority helper utilities.
    /// </summary>
    public class AuthorityHelpersAdditionalTests
    {
        [Theory]
        [InlineData("https://login.microsoftonline.com/common", "https://login.microsoftonline.com/common/v2.0")]
        [InlineData("https://login.microsoftonline.com/common/", "https://login.microsoftonline.com/common/v2.0")]
        [InlineData("https://login.microsoftonline.com/organizations", "https://login.microsoftonline.com/organizations/v2.0")]
        [InlineData("https://login.microsoftonline.com/consumers/v2.0", "https://login.microsoftonline.com/consumers/v2.0")]
        public void EnsureAuthorityIsV2_AppendsSuffixIfMissing_AAD(string input, string expected)
        {
            // Issue #3610: AAD authorities should have /v2.0 appended if missing
            // Act
            string result = AuthorityHelpers.EnsureAuthorityIsV2(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("https://fabrikamb2c.b2clogin.com/fabrikamb2c.onmicrosoft.com/B2C_1_susi", "https://fabrikamb2c.b2clogin.com/fabrikamb2c.onmicrosoft.com/B2C_1_susi/v2.0")]
        [InlineData("https://fabrikamb2c.b2clogin.com/fabrikamb2c.onmicrosoft.com/B2C_1_susi/v2.0", "https://fabrikamb2c.b2clogin.com/fabrikamb2c.onmicrosoft.com/B2C_1_susi/v2.0")]
        public void EnsureAuthorityIsV2_AppendsSuffixIfMissing_B2C(string input, string expected)
        {
            // Issue #3610: B2C authorities should have /v2.0 appended if missing
            // Act
            string result = AuthorityHelpers.EnsureAuthorityIsV2(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void GetAuthorityWithoutQueryIfNeeded_ExtractsParams_SingleQuery()
        {
            // Issue #3610: Authority with single query parameter should extract it
            // Arrange
            var options = new MicrosoftIdentityOptions
            {
                Authority = "https://login.microsoftonline.com/common/v2.0?dc=ESTS-PUB-WUS2-AZ1"
            };

            // Act
            string result = AuthorityHelpers.GetAuthorityWithoutQueryIfNeeded(options);

            // Assert
            Assert.Equal("https://login.microsoftonline.com/common/v2.0", result);
            Assert.NotNull(options.ExtraQueryParameters);
            Assert.True(options.ExtraQueryParameters.ContainsKey("dc"));
            Assert.Equal("ESTS-PUB-WUS2-AZ1", options.ExtraQueryParameters["dc"]);
        }

        [Fact]
        public void GetAuthorityWithoutQueryIfNeeded_ExtractsParams_MultipleQuery()
        {
            // Issue #3610: Authority with multiple query parameters should extract all
            // Arrange
            var options = new MicrosoftIdentityOptions
            {
                Authority = "https://login.microsoftonline.com/common/v2.0?dc=ESTS-PUB&slice=testslice&env=prod"
            };

            // Act
            string result = AuthorityHelpers.GetAuthorityWithoutQueryIfNeeded(options);

            // Assert
            Assert.Equal("https://login.microsoftonline.com/common/v2.0", result);
            Assert.NotNull(options.ExtraQueryParameters);
            Assert.Equal(3, options.ExtraQueryParameters.Count);
            Assert.Equal("ESTS-PUB", options.ExtraQueryParameters["dc"]);
            Assert.Equal("testslice", options.ExtraQueryParameters["slice"]);
            Assert.Equal("prod", options.ExtraQueryParameters["env"]);
        }

        [Fact]
        public void GetAuthorityWithoutQueryIfNeeded_NoQuery_ReturnsOriginal()
        {
            // Issue #3610: Authority without query parameters should return as-is
            // Arrange
            var options = new MicrosoftIdentityOptions
            {
                Authority = "https://login.microsoftonline.com/common/v2.0"
            };

            // Act
            string result = AuthorityHelpers.GetAuthorityWithoutQueryIfNeeded(options);

            // Assert
            Assert.Equal("https://login.microsoftonline.com/common/v2.0", result);
        }

        [Fact]
        public void GetAuthorityWithoutQueryIfNeeded_PreservesExistingExtraQueryParams()
        {
            // Issue #3610: Existing ExtraQueryParameters should be preserved/merged
            // Arrange
            var options = new MicrosoftIdentityOptions
            {
                Authority = "https://login.microsoftonline.com/common/v2.0?dc=ESTS-PUB",
                ExtraQueryParameters = new Dictionary<string, string>
                {
                    { "existing", "value" }
                }
            };

            // Act
            string result = AuthorityHelpers.GetAuthorityWithoutQueryIfNeeded(options);

            // Assert
            Assert.Equal("https://login.microsoftonline.com/common/v2.0", result);
            Assert.NotNull(options.ExtraQueryParameters);
            Assert.Equal(2, options.ExtraQueryParameters.Count);
            Assert.Equal("value", options.ExtraQueryParameters["existing"]);
            Assert.Equal("ESTS-PUB", options.ExtraQueryParameters["dc"]);
        }

        [Fact]
        public void BuildAuthority_B2C_UsesDomainPolicy()
        {
            // Issue #3610: B2C authority should be built from Instance, Domain, and Policy
            // Arrange
            var options = new MicrosoftIdentityOptions
            {
                Instance = "https://fabrikamb2c.b2clogin.com/",
                Domain = "fabrikamb2c.onmicrosoft.com",
                SignUpSignInPolicyId = "B2C_1_susi"
            };

            // Act
            string result = AuthorityHelpers.BuildAuthority(options);

            // Assert
            Assert.Equal("https://fabrikamb2c.b2clogin.com/fabrikamb2c.onmicrosoft.com/B2C_1_susi/v2.0", result);
        }

        [Fact]
        public void BuildAuthority_Aad_UsesTenant()
        {
            // Issue #3610: AAD authority should be built from Instance and TenantId
            // Arrange
            var options = new MicrosoftIdentityOptions
            {
                Instance = "https://login.microsoftonline.com/",
                TenantId = TestConstants.TenantIdAsGuid
            };

            // Act
            string result = AuthorityHelpers.BuildAuthority(options);

            // Assert
            Assert.Equal($"https://login.microsoftonline.com/{TestConstants.TenantIdAsGuid}/v2.0", result);
        }

        [Fact]
        public void BuildAuthority_Aad_InstanceWithTrailingSlash_BuildsCorrectly()
        {
            // Issue #3610: AAD authority with trailing slash in Instance should normalize
            // Arrange
            var options = new MicrosoftIdentityOptions
            {
                Instance = "https://login.microsoftonline.com/",
                TenantId = "common"
            };

            // Act
            string result = AuthorityHelpers.BuildAuthority(options);

            // Assert
            Assert.Equal("https://login.microsoftonline.com/common/v2.0", result);
        }

        [Fact]
        public void BuildAuthority_Aad_InstanceWithoutTrailingSlash_BuildsCorrectly()
        {
            // Issue #3610: AAD authority without trailing slash in Instance should work
            // Arrange
            var options = new MicrosoftIdentityOptions
            {
                Instance = "https://login.microsoftonline.com",
                TenantId = "organizations"
            };

            // Act
            string result = AuthorityHelpers.BuildAuthority(options);

            // Assert
            Assert.Equal("https://login.microsoftonline.com/organizations/v2.0", result);
        }

        [Fact]
        public void BuildCiamAuthorityIfNeeded_CiamWithoutTenant_AppendsTenant()
        {
            // Issue #3610: CIAM authority without tenant should append default tenant
            // Arrange
            string authority = "https://contoso.ciamlogin.com/";

            // Act
            string? result = AuthorityHelpers.BuildCiamAuthorityIfNeeded(authority, out bool preserveAuthority);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("https://contoso.ciamlogin.com/contoso.onmicrosoft.com", result);
            Assert.False(preserveAuthority);
        }

        [Fact]
        public void BuildCiamAuthorityIfNeeded_CiamWithTenant_PreservesAuthority()
        {
            // Issue #3610: CIAM authority with tenant should be preserved
            // Arrange
            string authority = "https://contoso.ciamlogin.com/contoso.onmicrosoft.com";

            // Act
            string? result = AuthorityHelpers.BuildCiamAuthorityIfNeeded(authority, out bool preserveAuthority);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(authority, result);
            Assert.True(preserveAuthority);
        }

        [Fact]
        public void BuildCiamAuthorityIfNeeded_NonCiam_ReturnsOriginal()
        {
            // Issue #3610: Non-CIAM authority should be returned as-is
            // Arrange
            string authority = "https://login.microsoftonline.com/common/v2.0";

            // Act
            string? result = AuthorityHelpers.BuildCiamAuthorityIfNeeded(authority, out bool preserveAuthority);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(authority, result);
            Assert.True(preserveAuthority);
        }
    }
}
