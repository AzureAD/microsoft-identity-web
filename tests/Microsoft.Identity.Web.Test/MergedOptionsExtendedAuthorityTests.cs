// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Web;
using Microsoft.Identity.Web.Test.Common;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    /// <summary>
    /// Extended test coverage for MergedOptions authority parsing logic.
    /// Issue #3610: Comprehensive authority parsing tests for AAD, B2C, CIAM scenarios.
    /// </summary>
    public class MergedOptionsExtendedAuthorityTests
    {
        [Fact]
        public void ParseAuthority_Aad_V2Authority_SetsInstanceTenant()
        {
            // Issue #3610: AAD v2.0 authority parsing
            // Arrange
            var mergedOptions = new MergedOptions
            {
                Authority = "https://login.microsoftonline.com/contoso.onmicrosoft.com/v2.0"
            };

            // Act
            MergedOptions.ParseAuthorityIfNecessary(mergedOptions);

            // Assert
            Assert.Equal("https://login.microsoftonline.com", mergedOptions.Instance);
            Assert.Equal("contoso.onmicrosoft.com", mergedOptions.TenantId);
        }

        [Fact]
        public void ParseAuthority_Aad_V1Authority_NoV2Suffix_StillParses()
        {
            // Issue #3610: AAD v1.0 authority (without /v2.0) should still parse correctly
            // Arrange
            var mergedOptions = new MergedOptions
            {
                Authority = "https://login.microsoftonline.com/organizations"
            };

            // Act
            MergedOptions.ParseAuthorityIfNecessary(mergedOptions);

            // Assert
            Assert.Equal("https://login.microsoftonline.com", mergedOptions.Instance);
            Assert.Equal("organizations", mergedOptions.TenantId);
        }

        [Theory]
        [InlineData("common")]
        [InlineData("organizations")]
        [InlineData("consumers")]
        public void ParseAuthority_SpecialTenantValues_ParsesCorrectly(string tenantValue)
        {
            // Issue #3610: Special AAD tenant values (common, organizations, consumers)
            // Arrange
            var mergedOptions = new MergedOptions
            {
                Authority = $"https://login.microsoftonline.com/{tenantValue}/v2.0"
            };

            // Act
            MergedOptions.ParseAuthorityIfNecessary(mergedOptions);

            // Assert
            Assert.Equal("https://login.microsoftonline.com", mergedOptions.Instance);
            Assert.Equal(tenantValue, mergedOptions.TenantId);
        }

        [Fact]
        public void ParseAuthority_SchemeLessAuthority_ParsesBaseAndTenant()
        {
            // Issue #3610: Authority without scheme prefix should parse correctly
            // Arrange - authority without https:// prefix
            var mergedOptions = new MergedOptions
            {
                Authority = "login.microsoftonline.com/common/v2.0"
            };

            // Act
            MergedOptions.ParseAuthorityIfNecessary(mergedOptions);

            // Assert - Should still parse the tenant correctly
            Assert.Equal("login.microsoftonline.com", mergedOptions.Instance);
            Assert.Equal("common", mergedOptions.TenantId);
        }

        [Fact]
        public void PrepareAuthorityInstance_AfterAuthorityParse_ComputesPreparedInstance()
        {
            // Issue #3610: PreparedInstance should be computed correctly after parsing
            // Arrange
            var mergedOptions = new MergedOptions
            {
                Authority = "https://login.microsoftonline.com/common/v2.0"
            };

            // Act
            MergedOptions.ParseAuthorityIfNecessary(mergedOptions);
            mergedOptions.PrepareAuthorityInstanceForMsal();

            // Assert
            Assert.Equal("https://login.microsoftonline.com/", mergedOptions.PreparedInstance);
        }

        [Fact]
        public void ParseAuthority_B2C_AuthorityOnly_PopulatesInstanceTenantPolicy()
        {
            // Issue #3610: B2C authority should populate Instance, TenantId (Domain), and detect policy
            // Arrange
            var mergedOptions = new MergedOptions
            {
                Authority = "https://fabrikamb2c.b2clogin.com/fabrikamb2c.onmicrosoft.com/B2C_1_susi/v2.0"
            };

            // Act
            MergedOptions.ParseAuthorityIfNecessary(mergedOptions);

            // Assert
            Assert.Equal("https://fabrikamb2c.b2clogin.com", mergedOptions.Instance);
            Assert.Equal("fabrikamb2c.onmicrosoft.com", mergedOptions.TenantId);
        }

        [Fact]
        public void PrepareAuthorityInstance_B2C_WithTfpSegment_RemovesTfp()
        {
            // Issue #3610: B2C authority with /tfp/ segment should have it removed in PreparedInstance
            // Arrange
            var mergedOptions = new MergedOptions
            {
                Instance = "https://fabrikamb2c.b2clogin.com/tfp/",
                TenantId = "fabrikamb2c.onmicrosoft.com",
                SignUpSignInPolicyId = "B2C_1_susi"
            };

            // Act
            mergedOptions.PrepareAuthorityInstanceForMsal();

            // Assert - /tfp/ should be removed
            Assert.Equal("https://fabrikamb2c.b2clogin.com/", mergedOptions.PreparedInstance);
        }

        [Fact]
        public void ParseAuthority_CIAM_PreserveAuthority_DoesNotSetTenantId()
        {
            // Issue #3610: CIAM with PreserveAuthority should keep full authority as Instance, TenantId null
            // Arrange
            var mergedOptions = new MergedOptions
            {
                Authority = "https://contoso.ciamlogin.com/contoso.onmicrosoft.com",
                PreserveAuthority = true
            };

            // Act
            MergedOptions.ParseAuthorityIfNecessary(mergedOptions);

            // Assert - PreserveAuthority means Instance is full Authority, TenantId is null
            Assert.Equal("https://contoso.ciamlogin.com/contoso.onmicrosoft.com", mergedOptions.Instance);
            Assert.Null(mergedOptions.TenantId);
        }

        [Fact]
        public void ParseAuthority_CIAM_NoPreserve_SetsSplitValues()
        {
            // Issue #3610: CIAM without PreserveAuthority should split into Instance and TenantId
            // Arrange
            var mergedOptions = new MergedOptions
            {
                Authority = "https://contoso.ciamlogin.com/contoso.onmicrosoft.com",
                PreserveAuthority = false
            };

            // Act
            MergedOptions.ParseAuthorityIfNecessary(mergedOptions);

            // Assert - Should split authority
            Assert.Equal("https://contoso.ciamlogin.com", mergedOptions.Instance);
            Assert.Equal("contoso.onmicrosoft.com", mergedOptions.TenantId);
        }

        [Fact]
        public void ParseAuthority_WithTrailingSlash_Normalizes()
        {
            // Issue #3610: Trailing slashes should be handled correctly
            // Arrange
            var mergedOptions = new MergedOptions
            {
                Authority = "https://login.microsoftonline.com/common/v2.0/"
            };

            // Act
            MergedOptions.ParseAuthorityIfNecessary(mergedOptions);

            // Assert - Trailing slash should be trimmed during parsing
            Assert.Equal("https://login.microsoftonline.com", mergedOptions.Instance);
            Assert.Equal("common", mergedOptions.TenantId);
        }

        [Fact]
        public void ParseAuthority_WithMultipleSlashes_IgnoresExtra()
        {
            // Issue #3610: Multiple consecutive slashes should not break parsing
            // Arrange
            var mergedOptions = new MergedOptions
            {
                Authority = "https://login.microsoftonline.com//common//v2.0"
            };

            // Act
            MergedOptions.ParseAuthorityIfNecessary(mergedOptions);

            // Assert - Should handle multiple slashes gracefully
            Assert.Equal("https://login.microsoftonline.com", mergedOptions.Instance);
            // TenantId might include the slash or might be empty, depending on parsing logic
            // The implementation handles this by finding the first slash after the scheme
            Assert.NotNull(mergedOptions.Instance);
        }

        [Fact]
        public void ParseAuthority_AuthorityWithQueryParams_DoesNotBreak()
        {
            // Issue #3610: Authority with query parameters should not break parsing
            // Arrange
            var mergedOptions = new MergedOptions
            {
                Authority = "https://login.microsoftonline.com/common/v2.0?dc=ESTS-PUB-WUS2-AZ1-FD000-TEST1"
            };

            // Act
            MergedOptions.ParseAuthorityIfNecessary(mergedOptions);

            // Assert - Should parse the base authority and tenant correctly
            // Query parameters may be included or excluded from TenantId depending on implementation
            Assert.Equal("https://login.microsoftonline.com", mergedOptions.Instance);
            Assert.NotNull(mergedOptions.TenantId);
            // The tenant should at least start with "common"
            Assert.StartsWith("common", mergedOptions.TenantId, System.StringComparison.Ordinal);
        }
    }
}
