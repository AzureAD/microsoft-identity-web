// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Web.Test.Common;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    public class AuthorityHelpersTests
    {
        [Theory]
        [InlineData("", false)]
        [InlineData(null, false)]
        [InlineData(TestConstants.AuthorityWithTenantSpecified, false)]
        [InlineData(TestConstants.AuthorityWithTenantSpecifiedWithV2, true)]
        public void IsV2Authority(string authority, bool expectedResult)
        {
            bool result = AuthorityHelpers.IsV2Authority(authority);

            Assert.Equal(expectedResult, result);
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
                SignUpSignInPolicyId = TestConstants.B2CSignUpSignInUserFlow
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
                SignUpSignInPolicyId = TestConstants.B2CSignUpSignInUserFlow
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
            MicrosoftIdentityOptions options = new MicrosoftIdentityOptions
            {
                TenantId = TestConstants.TenantIdAsGuid,
                Instance = TestConstants.AadInstance + "/"
            };
            string expectedResult = $"{TestConstants.AadInstance}/{options.TenantId}/v2.0";

            string result = AuthorityHelpers.BuildAuthority(options);

            Assert.NotNull(result);
            Assert.Equal(expectedResult, result);
        }
    }
}