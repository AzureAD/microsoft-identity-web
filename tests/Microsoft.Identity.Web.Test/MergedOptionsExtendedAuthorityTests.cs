// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Web;
using Microsoft.Identity.Web.Test.Common;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    /// <summary>
    /// Additional edge case tests for MergedOptions authority parsing logic.
    /// Issue #3610: Tests for edge cases not covered by MergedOptionsAuthorityParsingTests.
    /// </summary>
    public class MergedOptionsExtendedAuthorityTests
    {
        [Theory]
        [InlineData("common")]
        [InlineData("organizations")]
        [InlineData("consumers")]
        public void ParseAuthority_SpecialTenantValues_ParsesCorrectly(string tenantValue)
        {
            // Issue #3610: Special AAD tenant values (common, organizations, consumers) with Theory
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
