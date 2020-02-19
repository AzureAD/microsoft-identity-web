// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Xunit;

namespace Microsoft.Identity.Web.Test
{
    public class AuthorityHelpersTests
    {
        [Fact]
        public void IsV2Authority_EmptyParam_ReturnsFalse()
        {
            //Arrange
            string authority = string.Empty;

            //Act
            bool? result = AuthorityHelpers.IsV2Authority(authority);

            //Assert
            Assert.False(result);
        }

        [Fact]
        public void IsV2Authority_NullParam_ReturnsFalse()
        {
            //Arrange
            string authority = null;

            //Act
            bool? result = AuthorityHelpers.IsV2Authority(authority);

            //Assert
            Assert.False(result);
        }

        [Fact]
        public void IsV2Authority_EndsWithV2_ReturnsTrue()
        {
            //Arrange
            string authority = TestConstants.AuthorityWithTenantSpecifiedWithV2;

            //Act
            bool? result = AuthorityHelpers.IsV2Authority(authority);

            //Assert
            Assert.True(result);
        }

        [Fact]
        public void IsV2Authority_DoesntEndWithV2_ReturnsFalse()
        {
            //Arrange
            string authority = TestConstants.AuthorityWithTenantSpecified;

            //Act
            bool? result = AuthorityHelpers.IsV2Authority(authority);

            //Assert
            Assert.False(result);
        }

        [Fact]
        public void BuildAuthority_NullOptions_ReturnsNull()
        {
            //Arrange
            MicrosoftIdentityOptions options = null;

            //Act
            string result = AuthorityHelpers.BuildAuthority(options);

            //Assert
            Assert.Null(result);
        }

        [Fact]
        public void BuildAuthority_EmptyInstance_ReturnsNull()
        {
            //Arrange
            MicrosoftIdentityOptions options = new MicrosoftIdentityOptions
            {
                Domain = TestConstants.Domain,
                Instance = string.Empty
            };

            //Act
            string result = AuthorityHelpers.BuildAuthority(options);

            //Assert
            Assert.Null(result);
        }

        [Fact]
        public void BuildAuthority_B2CEmptyDomain_ReturnsNull()
        {
            //Arrange
            MicrosoftIdentityOptions options = new MicrosoftIdentityOptions
            {
                Domain = string.Empty,
                Instance = TestConstants.B2CInstance,
                SignUpSignInPolicyId = TestConstants.B2CSuSiUserFlow
            };

            //Act
            string result = AuthorityHelpers.BuildAuthority(options);

            //Assert
            Assert.Null(result);
        }

        [Fact]
        public void BuildAuthority_AADEmptyTenantId_ReturnsNull()
        {
            //Arrange
            MicrosoftIdentityOptions options = new MicrosoftIdentityOptions
            {
                TenantId = string.Empty,
                Instance = TestConstants.AadInstance
            };

            //Act
            string result = AuthorityHelpers.BuildAuthority(options);

            //Assert
            Assert.Null(result);
        }

        [Fact]
        public void BuildAuthority_AadInstanceAndTenantId_BuildAadAuthority()
        {
            //Arrange
            MicrosoftIdentityOptions options = new MicrosoftIdentityOptions
            {
                TenantId = TestConstants.TenantIdAsGuid,
                Instance = TestConstants.AadInstance
            };
            string expectedResult = $"{options.Instance}/{options.TenantId}/v2.0";

            //Act
            string result = AuthorityHelpers.BuildAuthority(options);

            //Assert
            Assert.NotNull(result);
            Assert.Equal(result, expectedResult);
        }

        [Fact]
        public void BuildAuthority_OptionsInstanceWithTrailing_BuildAadAuthorityWithoutExtraTrailing()
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
            Assert.Equal(result, expectedResult);
        }

        [Fact]
        public void BuildAuthority_B2CInstanceDomainAndPolicy_BuildB2CAuthority()
        {
            //Arrange
            MicrosoftIdentityOptions options = new MicrosoftIdentityOptions
            {
                Domain = TestConstants.B2CTenant,
                Instance = TestConstants.B2CInstance,
                SignUpSignInPolicyId = TestConstants.B2CSuSiUserFlow
            };
            string expectedResult = $"{options.Instance}/{options.Domain}/{options.DefaultUserFlow}/v2.0";

            //Act
            string result = AuthorityHelpers.BuildAuthority(options);

            //Assert
            Assert.NotNull(result);
            Assert.Equal(result, expectedResult);
        }
    }
}