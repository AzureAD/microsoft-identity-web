// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Web;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    public class MergedOptionsAuthorityParsingTests
    {
        [Fact]
        public void ParseAuthority_AadAuthorityOnly_SetsInstanceAndTenant()
        {
            // Arrange
            var mergedOptions = new MergedOptions
            {
                Authority = "https://login.microsoftonline.com/msidlab4.onmicrosoft.com/v2.0"
            };

            // Act
            MergedOptions.ParseAuthorityIfNecessary(mergedOptions);

            // Assert
            Assert.Equal("https://login.microsoftonline.com", mergedOptions.Instance);
            Assert.Equal("msidlab4.onmicrosoft.com", mergedOptions.TenantId);
        }

        [Fact]
        public void PrepareAuthorityInstance_ForAadAuthorityOnly_ComputesPreparedInstance()
        {
            // Arrange
            var mergedOptions = new MergedOptions
            {
                Authority = "https://login.microsoftonline.com/msidlab4.onmicrosoft.com/v2.0"
            };

            // Act
            mergedOptions.PrepareAuthorityInstanceForMsal();

            // Assert
            Assert.Equal("https://login.microsoftonline.com/", mergedOptions.PreparedInstance);
        }

        [Fact]
        public void PrepareAuthorityInstance_ForB2C_RemovesTfpSegment()
        {
            // Arrange
            var mergedOptions = new MergedOptions
            {
                Authority = "https://fabrikamb2c.b2clogin.com/tfp/fabrikamb2c.onmicrosoft.com/B2C_1_susi/v2.0",
                SignUpSignInPolicyId = "B2C_1_susi"
            };

            // Act - Parse the authority first
            MergedOptions.ParseAuthorityIfNecessary(mergedOptions);
            mergedOptions.PrepareAuthorityInstanceForMsal();

            // Assert - Instance should be parsed to the domain part
            Assert.Equal("https://fabrikamb2c.b2clogin.com", mergedOptions.Instance);
            // PreparedInstance should have /tfp/ removed if present in Instance
            Assert.Equal("https://fabrikamb2c.b2clogin.com/", mergedOptions.PreparedInstance);
        }

        [Fact]
        public void PreserveAuthority_CiamAuthority_DoesNotSetTenant()
        {
            // Arrange - CIAM authority with a path to parse
            var mergedOptions = new MergedOptions
            {
                Authority = "https://MSIDLABCIAM6.ciamlogin.com/tenant.onmicrosoft.com",
                PreserveAuthority = true
            };

            // Act
            MergedOptions.ParseAuthorityIfNecessary(mergedOptions);

            // Assert - When PreserveAuthority is true, Instance should be the full Authority
            Assert.Equal("https://MSIDLABCIAM6.ciamlogin.com/tenant.onmicrosoft.com", mergedOptions.Instance);
            // TenantId should remain null when PreserveAuthority is true
            Assert.Null(mergedOptions.TenantId);
        }

        [Fact]
        public void ParseAuthority_WithoutV2Suffix_SetsInstanceAndTenant()
        {
            // Arrange
            var mergedOptions = new MergedOptions
            {
                Authority = "https://login.microsoftonline.com/common"
            };

            // Act
            MergedOptions.ParseAuthorityIfNecessary(mergedOptions);

            // Assert
            Assert.Equal("https://login.microsoftonline.com", mergedOptions.Instance);
            Assert.Equal("common", mergedOptions.TenantId);
        }

        [Fact]
        public void ParseAuthority_InstanceAndTenantAlreadySet_DoesNotParse()
        {
            // Arrange
            var mergedOptions = new MergedOptions
            {
                Authority = "https://login.microsoftonline.com/tenant1/v2.0",
                Instance = "https://login.microsoftonline.com/",
                TenantId = "tenant2"
            };

            // Act
            MergedOptions.ParseAuthorityIfNecessary(mergedOptions);

            // Assert - Should not modify existing Instance and TenantId
            Assert.Equal("https://login.microsoftonline.com/", mergedOptions.Instance);
            Assert.Equal("tenant2", mergedOptions.TenantId);
        }

        [Fact]
        public void ParseAuthority_B2CAuthorityWithPolicy_SetsInstanceAndTenant()
        {
            // Arrange
            var mergedOptions = new MergedOptions
            {
                Authority = "https://fabrikamb2c.b2clogin.com/fabrikamb2c.onmicrosoft.com/B2C_1_susi/v2.0",
                SignUpSignInPolicyId = "B2C_1_susi"
            };

            // Act
            MergedOptions.ParseAuthorityIfNecessary(mergedOptions);

            // Assert
            Assert.Equal("https://fabrikamb2c.b2clogin.com", mergedOptions.Instance);
            Assert.Equal("fabrikamb2c.onmicrosoft.com", mergedOptions.TenantId);
        }
    }
}
