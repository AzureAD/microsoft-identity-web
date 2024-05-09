// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.Identity.Web.Test.Common;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    public class AuthorityHelpersTests
    {
        [Theory]
        [InlineData(TestConstants.AuthorityWithTenantSpecified, TestConstants.AuthorityWithTenantSpecifiedWithV2)]
        [InlineData(TestConstants.AuthorityWithTenantSpecifiedWithV2, TestConstants.AuthorityWithTenantSpecifiedWithV2)]
        public void IsV2Authority(string authority, string expectedResult)
        {
            Assert.Equal(expectedResult, AuthorityHelpers.EnsureAuthorityIsV2(authority));
        }

        [Fact]
        public void BuildAuthority_B2CValidOptions_ReturnsValidB2CAuthority()
        {
            MicrosoftIdentityOptions options = new MicrosoftIdentityOptions
            {
                Domain = TestConstants.B2CTenant,
                Instance = TestConstants.B2CInstance,
                SignUpSignInPolicyId = TestConstants.B2CSignUpSignInUserFlow,
            };
            string expectedResult = $"{options.Instance}/{options.Domain}/{options.DefaultUserFlow}/v2.0";

            string result = AuthorityHelpers.BuildAuthority(options);

            Assert.NotNull(result);
            Assert.Equal(expectedResult, result);
        }

        [Fact]
        public void BuildAuthority_AadValidOptions_ReturnsValidAadAuthority()
        {
            MicrosoftIdentityOptions options = new MicrosoftIdentityOptions
            {
                TenantId = TestConstants.TenantIdAsGuid,
                Instance = TestConstants.AadInstance,
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
                Instance = TestConstants.AadInstance + "/",
            };
            string expectedResult = $"{TestConstants.AadInstance}/{options.TenantId}/v2.0";

            string result = AuthorityHelpers.BuildAuthority(options);

            Assert.NotNull(result);
            Assert.Equal(expectedResult, result);
        }

        [Fact]
        public void BuildAuthority_CiamAuthority_ReturnsValidAadAuthority()
        {
            MicrosoftIdentityOptions options = new MicrosoftIdentityOptions
            {
                Authority = $"https://contoso{Constants.CiamAuthoritySuffix}/"
            };
            string expectedResult = options.Authority + TestConstants.Domain;

            string? result = AuthorityHelpers.BuildCiamAuthorityIfNeeded(options.Authority, out bool preserveAuthority);

            Assert.NotNull(result);
            Assert.Equal(expectedResult, result);
            Assert.False(preserveAuthority);
        }

        [Fact]
        public void BuildAuthority_CiamAuthorityWithTenant_ReturnsValidAadAuthority()
        {
            MicrosoftIdentityOptions options = new MicrosoftIdentityOptions
            {
                Authority = $"https://contoso{Constants.CiamAuthoritySuffix}/{TestConstants.TenantIdAsGuid}/"
            };
            string expectedResult = options.Authority ;

            string? result = AuthorityHelpers.BuildCiamAuthorityIfNeeded(options.Authority, out bool preserveAuthority);

            Assert.NotNull(result);
            Assert.Equal(expectedResult, result);
            Assert.True(preserveAuthority);
        }

        [Fact]
        public void BuildAuthority_WithQueryParams_ReturnsValidAadAuthority()
        {
            // arrange
            MicrosoftIdentityOptions options = new MicrosoftIdentityOptions
            {
                TenantId = TestConstants.TenantIdAsGuid,
                Instance = TestConstants.AadInstance,
                ExtraQueryParameters = new Dictionary <string, string>
                {
                    { "queryParam1", "value1" },
                    { "queryParam2", "value2" },
                }
            };
            var expectedQuery = QueryString.Create(options.ExtraQueryParameters);
            string expectedResult = $"{options.Instance}/{options.TenantId}/v2.0{expectedQuery}";

            // act
            string result = AuthorityHelpers.BuildAuthority(options);
            
            // assert
            Assert.NotNull(result);
            Assert.Equal(expectedResult, result);
        }

        [Fact]
        public void BuildAuthority_EmptyQueryParams_ReturnsValidAadAuthority()
        {
            // arrange
            MicrosoftIdentityOptions options = new MicrosoftIdentityOptions
            {
                TenantId = TestConstants.TenantIdAsGuid,
                Instance = TestConstants.AadInstance,
                ExtraQueryParameters = new Dictionary<string, string>()
            };

            string expectedResult = $"{options.Instance}/{options.TenantId}/v2.0";

            // act
            string result = AuthorityHelpers.BuildAuthority(options);

            // assert
            Assert.NotNull(result);
            Assert.Equal(expectedResult, result);
        }

        [Fact]
        public void BuildAuthority_NullQueryParams_ReturnsValidAadAuthority()
        {
            // arrange
            MicrosoftIdentityOptions options = new MicrosoftIdentityOptions
            {
                TenantId = TestConstants.TenantIdAsGuid,
                Instance = TestConstants.AadInstance,
                ExtraQueryParameters = null
            };
            string expectedResult = $"{options.Instance}/{options.TenantId}/v2.0";

            // act
            string result = AuthorityHelpers.BuildAuthority(options);

            // assert
            Assert.NotNull(result);
            Assert.Equal(expectedResult, result);
        }

        [Theory]
        [InlineData(TestConstants.AuthorityCommonTenant, TestConstants.AuthorityCommonTenantWithV2)]
        [InlineData(TestConstants.AuthorityOrganizationsUSTenant, TestConstants.AuthorityOrganizationsUSWithV2)]
        [InlineData(TestConstants.AuthorityCommonTenantWithV2, TestConstants.AuthorityCommonTenantWithV2)]
        [InlineData(TestConstants.AuthorityCommonTenantWithV2 + "/", TestConstants.AuthorityCommonTenantWithV2)]
        [InlineData(TestConstants.B2CAuthorityWithV2, TestConstants.B2CAuthorityWithV2)]
        [InlineData(TestConstants.B2CCustomDomainAuthorityWithV2, TestConstants.B2CCustomDomainAuthorityWithV2)]
        [InlineData(TestConstants.B2CAuthority, TestConstants.B2CAuthorityWithV2)]
        [InlineData(TestConstants.B2CCustomDomainAuthority, TestConstants.B2CCustomDomainAuthorityWithV2)]
        public void EnsureAuthorityIsV2(string initialAuthority, string expectedAuthority)
        {
            OpenIdConnectOptions options = new OpenIdConnectOptions
            {
                Authority = initialAuthority,
            };

            options.Authority = AuthorityHelpers.EnsureAuthorityIsV2(options.Authority);
            Assert.Equal(expectedAuthority, options.Authority);
        }
    }
}
