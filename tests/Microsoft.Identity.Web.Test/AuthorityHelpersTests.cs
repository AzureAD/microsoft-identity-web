// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Xunit;
using Microsoft.Identity.Web.Test.Common;

namespace Microsoft.Identity.Web.Test
{
    public class AuthorityHelpersTests
    {
        [Fact]
        public void IsV2Authority_EmptyAuthority_ReturnsFalse()
        {
            bool result = AuthorityHelpers.IsV2Authority(string.Empty);

            Assert.False(result);
        }

        [Fact]
        public void IsV2Authority_NullAuthority_ReturnsFalse()
        {
            bool result = AuthorityHelpers.IsV2Authority(null);

            Assert.False(result);
        }

        [Fact]
        public void IsV2Authority_AuthorityEndsWithV2_ReturnsTrue()
        {
            bool result = AuthorityHelpers.IsV2Authority(TestConstants.AuthorityWithTenantSpecifiedWithV2);

            Assert.True(result);
        }

        [Fact]
        public void IsV2Authority_AuthorityDoesntEndWithV2_ReturnsFalse()
        {
            bool result = AuthorityHelpers.IsV2Authority(TestConstants.AuthorityWithTenantSpecified);

            Assert.False(result);
        }

        [Fact]
        public void BuildAuthority_NullOptions_ReturnsNull()
        {
            string result = AuthorityHelpers.BuildAuthority(null);

            Assert.Null(result);
        }

        [Fact]
        public void BuildAuthority_EmptyInstance_ReturnsNull()
        {
            MicrosoftIdentityOptions options = new MicrosoftIdentityOptions
            {
                Domain = TestConstants.Domain,
                Instance = string.Empty
            };

            string result = AuthorityHelpers.BuildAuthority(options);

            Assert.Null(result);
        }

        [Fact]
        public void BuildAuthority_B2CEmptyDomain_ReturnsNull()
        {
            MicrosoftIdentityOptions options = new MicrosoftIdentityOptions
            {
                Domain = string.Empty,
                Instance = TestConstants.B2CInstance,
                SignUpSignInPolicyId = TestConstants.B2CSuSiUserFlow
            };

            string result = AuthorityHelpers.BuildAuthority(options);

            Assert.Null(result);
        }

        [Fact]
        public void BuildAuthority_B2CValidOptions_ReturnsValidB2CAuthority()
        {
            MicrosoftIdentityOptions options = new MicrosoftIdentityOptions
            {
                Domain = TestConstants.B2CTenant,
                Instance = TestConstants.B2CInstance,
                SignUpSignInPolicyId = TestConstants.B2CSuSiUserFlow
            };
            string expectedResult = $"{options.Instance}/{options.Domain}/{options.DefaultUserFlow}/v2.0";

            string result = AuthorityHelpers.BuildAuthority(options);

            Assert.NotNull(result);
            Assert.Equal(expectedResult, result);
        }

        [Fact]
        public void BuildAuthority_AadEmptyTenantId_ReturnsNull()
        {
            MicrosoftIdentityOptions options = new MicrosoftIdentityOptions
            {
                TenantId = string.Empty,
                Instance = TestConstants.AadInstance
            };

            string result = AuthorityHelpers.BuildAuthority(options);

            Assert.Null(result);
        }

        [Fact]
        public void BuildAuthority_AadValidOptions_ReturnsValidAadAuthority()
        {
            MicrosoftIdentityOptions options = new MicrosoftIdentityOptions
            {
                TenantId = TestConstants.TenantIdAsGuid,
                Instance = TestConstants.AadInstance
            };
            string expectedResult = $"{options.Instance}/{options.TenantId}/v2.0";

            string result = AuthorityHelpers.BuildAuthority(options);

            Assert.NotNull(result);
            Assert.Equal(expectedResult, result);
        }

        [Fact]
        public void BuildAuthority_AadInstanceWithTrailingSlash_ReturnsValidAadAuthority()
        {
            //Arrange
            MicrosoftIdentityOptions options = new MicrosoftIdentityOptions
            {
                TenantId = TestConstants.TenantIdAsGuid,
                Instance = TestConstants.AadInstance + "/"
            };
            string expectedResult = $"{TestConstants.AadInstance}/{options.TenantId}/v2.0";

            //Act
            string result = AuthorityHelpers.BuildAuthority(options);

            //Assert
            Assert.NotNull(result);
            Assert.Equal(expectedResult, result);
        }
    }
}