// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
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

        [Theory]
        [MemberData(nameof(AddAuthorityQueryToOptionsTheoryData))]
        [MemberData(nameof(AddAuthorityQueryToOptionsExistingValuesTheoryData))]
        [MemberData(nameof(AddAuthorityQueryToOptionsOverlappingExistingValuesTheoryData))]
        public void AddAuthorityQueryToOptions(AuthorityHelpersTheoryData theoryData)
        {
            // arrange
            MicrosoftIdentityOptions options = new()
            {
                Authority = theoryData.Authority,
                ExtraQueryParameters = theoryData.ExtraQueryParameters
            };

            // act
            AuthorityHelpers.AddAuthorityQueryToOptions(options);

            // assert
            Assert.NotNull(options.ExtraQueryParameters);
            Assert.Equal(theoryData.ExpectedExtraQueryParameters.Count, options.ExtraQueryParameters.Count);
            foreach (var key in theoryData.ExpectedExtraQueryParameters.Keys)
            {
                Assert.True(options.ExtraQueryParameters.ContainsKey(key));
                Assert.Equal(theoryData.ExpectedExtraQueryParameters[key], options.ExtraQueryParameters[key]);
            }
        }

        public static TheoryData<AuthorityHelpersTheoryData> AddAuthorityQueryToOptionsTheoryData()
        {
            var singleQuery = "?key1=value1";
            var multipleQueries = "?key1=value1&key2=value2";
            var emptyQuery = "?";
            var queryNoValue = "?key1";

            var singleExpectedExtraQueryParams = new Dictionary<string, string>
            {
                { "key1", "value1" }
            };
            
            var multipleExpectedExtraQueryParams = new Dictionary<string, string>
            {
                { "key1", "value1" },
                { "key2", "value2" }
            };

            var emptyExpectedExtraQueryParams = new Dictionary<string, string>();

            var theoryData = new TheoryData<AuthorityHelpersTheoryData>
            {
                new("AuthorityCommonTenant_SingleQuery")
                {
                    Authority = TestConstants.AuthorityCommonTenant + singleQuery,
                    ExpectedExtraQueryParameters = singleExpectedExtraQueryParams
                },
                new("AuthorityCommonTenant_MultipleQueries")
                {
                    Authority = TestConstants.AuthorityCommonTenant + multipleQueries,
                    ExpectedExtraQueryParameters = multipleExpectedExtraQueryParams
                },
                new("AuthorityCommonTenant_EmptyQuery")
                {
                    Authority = TestConstants.AuthorityCommonTenant + emptyQuery,
                    ExpectedExtraQueryParameters = emptyExpectedExtraQueryParams
                },
                new("AuthorityCommonTenant_QueryNoValue")
                {
                    Authority = TestConstants.AuthorityCommonTenant + queryNoValue,
                    ExpectedExtraQueryParameters = emptyExpectedExtraQueryParams
                },
                new("AuthorityCommonTenantWithV2_SingleQuery")
                {
                    Authority = TestConstants.AuthorityCommonTenantWithV2 + singleQuery,
                    ExpectedExtraQueryParameters = singleExpectedExtraQueryParams
                },
                new("AuthorityCommonTenantWithV2_MultipleQueries")
                {
                    Authority = TestConstants.AuthorityCommonTenantWithV2 + multipleQueries,
                    ExpectedExtraQueryParameters = multipleExpectedExtraQueryParams
                },
                new("AuthorityCommonTenantWithV2_EmptyQuery")
                {
                    Authority = TestConstants.AuthorityCommonTenantWithV2 + emptyQuery,
                    ExpectedExtraQueryParameters = emptyExpectedExtraQueryParams
                },
                new("AuthorityCommonTenantWithV2_QueryNoValue")
                {
                    Authority = TestConstants.AuthorityCommonTenantWithV2 + queryNoValue,
                    ExpectedExtraQueryParameters = emptyExpectedExtraQueryParams
                },
                new("B2CAuthorityWithV2_SingleQuery")
                {
                    Authority = TestConstants.B2CAuthorityWithV2 + singleQuery,
                    ExpectedExtraQueryParameters = singleExpectedExtraQueryParams
                },
                new("B2CAuthorityWithV2_MultipleQueries")
                {
                    Authority = TestConstants.B2CAuthorityWithV2 + multipleQueries,
                    ExpectedExtraQueryParameters = multipleExpectedExtraQueryParams
                },
                new("B2CAuthorityWithV2_EmptyQuery")
                {
                    Authority = TestConstants.B2CAuthorityWithV2 + emptyQuery,
                    ExpectedExtraQueryParameters = emptyExpectedExtraQueryParams
                },
                new("B2CAuthorityWithV2_QueryNoValue")
                {
                    Authority = TestConstants.B2CAuthorityWithV2 + queryNoValue,
                    ExpectedExtraQueryParameters = emptyExpectedExtraQueryParams
                },
                new("B2CCustomDomainAuthorityWithV2_SingleQuery")
                {
                    Authority = TestConstants.B2CCustomDomainAuthorityWithV2 + singleQuery,
                    ExpectedExtraQueryParameters = singleExpectedExtraQueryParams
                },
                new("B2CCustomDomainAuthorityWithV2_MultipleQueries")
                {
                    Authority = TestConstants.B2CCustomDomainAuthorityWithV2 + multipleQueries,
                    ExpectedExtraQueryParameters = multipleExpectedExtraQueryParams
                },
                new("B2CCustomDomainAuthorityWithV2_EmptyQuery")
                {
                    Authority = TestConstants.B2CCustomDomainAuthorityWithV2 + emptyQuery,
                    ExpectedExtraQueryParameters = emptyExpectedExtraQueryParams
                },
                new("B2CCustomDomainAuthorityWithV2_QueryNoValue")
                {
                    Authority = TestConstants.B2CCustomDomainAuthorityWithV2 + queryNoValue,
                    ExpectedExtraQueryParameters = emptyExpectedExtraQueryParams
                },
                new("B2CAuthority_SingleQuery")
                {
                    Authority = TestConstants.B2CAuthority + singleQuery,
                    ExpectedExtraQueryParameters = singleExpectedExtraQueryParams
                },
                new("B2CAuthority_MultipleQueries")
                {
                    Authority = TestConstants.B2CAuthority + multipleQueries,
                    ExpectedExtraQueryParameters = multipleExpectedExtraQueryParams
                },
                new("B2CAuthority_EmptyQuery")
                {
                    Authority = TestConstants.B2CAuthority + emptyQuery,
                    ExpectedExtraQueryParameters = emptyExpectedExtraQueryParams
                },
                new("B2CAuthority_QueryNoValue")
                {
                    Authority = TestConstants.B2CAuthority + queryNoValue,
                    ExpectedExtraQueryParameters = emptyExpectedExtraQueryParams
                },
                new("B2CCustomDomainAuthority_SingleQuery")
                {
                    Authority = TestConstants.B2CCustomDomainAuthority + singleQuery,
                    ExpectedExtraQueryParameters = singleExpectedExtraQueryParams
                },
                new("B2CCustomDomainAuthority_MultipleQueries")
                {
                    Authority = TestConstants.B2CCustomDomainAuthority + multipleQueries,
                    ExpectedExtraQueryParameters = multipleExpectedExtraQueryParams
                },
                new("B2CCustomDomainAuthority_EmptyQuery")
                {
                    Authority = TestConstants.B2CCustomDomainAuthority + emptyQuery,
                    ExpectedExtraQueryParameters = emptyExpectedExtraQueryParams
                },
                new("B2CCustomDomainAuthority_QueryNoValue")
                {
                    Authority = TestConstants.B2CCustomDomainAuthority + queryNoValue,
                    ExpectedExtraQueryParameters = emptyExpectedExtraQueryParams
                }
            };

            return theoryData;
        }

        public static TheoryData<AuthorityHelpersTheoryData> AddAuthorityQueryToOptionsExistingValuesTheoryData()
        {
            var singleQuery = "?key1=value1";
            var multipleQueries = "?key1=value1&key2=value2";
            var emptyQuery = "?";
            var queryNoValue = "?key1";

            var singleExpectedExtraQueryParams = new Dictionary<string, string>
            {
                { "key1", "value1" },
                { "key3", "value3" },
                { "key4", "value4" }
            };

            var multipleExpectedExtraQueryParams = new Dictionary<string, string>
            {
                { "key1", "value1" },
                { "key2", "value2" },
                { "key3", "value3" },
                { "key4", "value4" }
            };

            var emptyExpectedExtraQueryParams = new Dictionary<string, string>
            {
                { "key3", "value3" },
                { "key4", "value4" }
            };

            var theoryData = new TheoryData<AuthorityHelpersTheoryData>
            {
                new("AuthorityCommonTenant_SingleQuery")
                {
                    Authority = TestConstants.AuthorityCommonTenant + singleQuery,
                    ExtraQueryParameters = new Dictionary<string, string> { { "key3", "value3" }, { "key4", "value4" } },
                    ExpectedExtraQueryParameters = singleExpectedExtraQueryParams
                },
                new("AuthorityCommonTenant_MultipleQueries")
                {
                    Authority = TestConstants.AuthorityCommonTenant + multipleQueries,
                    ExtraQueryParameters = new Dictionary<string, string> { { "key3", "value3" }, { "key4", "value4" } },
                    ExpectedExtraQueryParameters = multipleExpectedExtraQueryParams
                },
                new("AuthorityCommonTenant_EmptyQuery")
                {
                    Authority = TestConstants.AuthorityCommonTenant + emptyQuery,
                    ExtraQueryParameters = new Dictionary<string, string> { { "key3", "value3" }, { "key4", "value4" } },
                    ExpectedExtraQueryParameters = emptyExpectedExtraQueryParams
                },
                new("AuthorityCommonTenant_QueryNoValue")
                {
                    Authority = TestConstants.AuthorityCommonTenant + queryNoValue,
                    ExtraQueryParameters = new Dictionary<string, string> { { "key3", "value3" }, { "key4", "value4" } },
                    ExpectedExtraQueryParameters = emptyExpectedExtraQueryParams
                },
                new("AuthorityCommonTenantWithV2_SingleQuery")
                {
                    Authority = TestConstants.AuthorityCommonTenantWithV2 + singleQuery,
                    ExtraQueryParameters = new Dictionary<string, string> { { "key3", "value3" }, { "key4", "value4" } },
                    ExpectedExtraQueryParameters = singleExpectedExtraQueryParams
                },
                new("AuthorityCommonTenantWithV2_MultipleQueries")
                {
                    Authority = TestConstants.AuthorityCommonTenantWithV2 + multipleQueries,
                    ExtraQueryParameters = new Dictionary<string, string> { { "key3", "value3" }, { "key4", "value4" } },
                    ExpectedExtraQueryParameters = multipleExpectedExtraQueryParams
                },
                new("AuthorityCommonTenantWithV2_EmptyQuery")
                {
                    Authority = TestConstants.AuthorityCommonTenantWithV2 + emptyQuery,
                    ExtraQueryParameters = new Dictionary<string, string> { { "key3", "value3" }, { "key4", "value4" } },
                    ExpectedExtraQueryParameters = emptyExpectedExtraQueryParams
                },
                new("AuthorityCommonTenantWithV2_QueryNoValue")
                {
                    Authority = TestConstants.AuthorityCommonTenantWithV2 + queryNoValue,
                    ExtraQueryParameters = new Dictionary<string, string> { { "key3", "value3" }, { "key4", "value4" } },
                    ExpectedExtraQueryParameters = emptyExpectedExtraQueryParams
                },
                new("B2CAuthorityWithV2_SingleQuery")
                {
                    Authority = TestConstants.B2CAuthorityWithV2 + singleQuery,
                    ExtraQueryParameters = new Dictionary<string, string> { { "key3", "value3" }, { "key4", "value4" } },
                    ExpectedExtraQueryParameters = singleExpectedExtraQueryParams
                },
                new("B2CAuthorityWithV2_MultipleQueries")
                {
                    Authority = TestConstants.B2CAuthorityWithV2 + multipleQueries,
                    ExtraQueryParameters = new Dictionary<string, string> { { "key3", "value3" }, { "key4", "value4" } },
                    ExpectedExtraQueryParameters = multipleExpectedExtraQueryParams
                },
                new("B2CAuthorityWithV2_EmptyQuery")
                {
                    Authority = TestConstants.B2CAuthorityWithV2 + emptyQuery,
                    ExtraQueryParameters = new Dictionary < string, string > { { "key3", "value3" }, { "key4", "value4" } },
                    ExpectedExtraQueryParameters = emptyExpectedExtraQueryParams
                },
                new("B2CAuthorityWithV2_QueryNoValue")
                {
                    Authority = TestConstants.B2CAuthorityWithV2 + queryNoValue,
                    ExtraQueryParameters = new Dictionary < string, string > { { "key3", "value3" }, { "key4", "value4" } },
                    ExpectedExtraQueryParameters = emptyExpectedExtraQueryParams
                },
                new("B2CCustomDomainAuthorityWithV2_SingleQuery")
                {
                    Authority = TestConstants.B2CCustomDomainAuthorityWithV2 + singleQuery,
                    ExtraQueryParameters = new Dictionary < string, string > { { "key3", "value3" }, { "key4", "value4" } },
                    ExpectedExtraQueryParameters = singleExpectedExtraQueryParams
                },
                new("B2CCustomDomainAuthorityWithV2_MultipleQueries")
                {
                    Authority = TestConstants.B2CCustomDomainAuthorityWithV2 + multipleQueries,
                    ExtraQueryParameters = new Dictionary < string, string > { { "key3", "value3" }, { "key4", "value4" } },
                    ExpectedExtraQueryParameters = multipleExpectedExtraQueryParams
                },
                new("B2CCustomDomainAuthorityWithV2_EmptyQuery")
                {
                    Authority = TestConstants.B2CCustomDomainAuthorityWithV2 + emptyQuery,
                    ExtraQueryParameters = new Dictionary < string, string > { { "key3", "value3" }, { "key4", "value4" } },
                    ExpectedExtraQueryParameters = emptyExpectedExtraQueryParams
                },
                new("B2CCustomDomainAuthorityWithV2_QueryNoValue")
                {
                    Authority = TestConstants.B2CCustomDomainAuthorityWithV2 + queryNoValue,
                    ExtraQueryParameters = new Dictionary < string, string > { { "key3", "value3" }, { "key4", "value4" } },
                    ExpectedExtraQueryParameters = emptyExpectedExtraQueryParams
                },
                new("B2CAuthority_SingleQuery")
                {
                    Authority = TestConstants.B2CAuthority + singleQuery,
                    ExtraQueryParameters = new Dictionary < string, string > { { "key3", "value3" }, { "key4", "value4" } },
                    ExpectedExtraQueryParameters = singleExpectedExtraQueryParams
                },
                new("B2CAuthority_MultipleQueries")
                {
                    Authority = TestConstants.B2CAuthority + multipleQueries,
                    ExtraQueryParameters = new Dictionary < string, string > { { "key3", "value3" }, { "key4", "value4" } },
                    ExpectedExtraQueryParameters = multipleExpectedExtraQueryParams
                },
                new("B2CAuthority_EmptyQuery")
                {
                    Authority = TestConstants.B2CAuthority + emptyQuery,
                    ExtraQueryParameters = new Dictionary < string, string > { { "key3", "value3" }, { "key4", "value4" } },
                    ExpectedExtraQueryParameters = emptyExpectedExtraQueryParams
                },
                new("B2CAuthority_QueryNoValue")
                {
                    Authority = TestConstants.B2CAuthority + queryNoValue,
                    ExtraQueryParameters = new Dictionary < string, string > { { "key3", "value3" }, { "key4", "value4" } },
                    ExpectedExtraQueryParameters = emptyExpectedExtraQueryParams
                },
                new("B2CCustomDomainAuthority_SingleQuery")
                {
                    Authority = TestConstants.B2CCustomDomainAuthority + singleQuery,
                    ExtraQueryParameters = new Dictionary < string, string > { { "key3", "value3" }, { "key4", "value4" } },
                    ExpectedExtraQueryParameters = singleExpectedExtraQueryParams
                },
                new("B2CCustomDomainAuthority_MultipleQueries")
                {
                    Authority = TestConstants.B2CCustomDomainAuthority + multipleQueries,
                    ExtraQueryParameters = new Dictionary < string, string > { { "key3", "value3" }, { "key4", "value4" } },
                    ExpectedExtraQueryParameters = multipleExpectedExtraQueryParams
                },
                new("B2CCustomDomainAuthority_EmptyQuery")
                {
                    Authority = TestConstants.B2CCustomDomainAuthority + emptyQuery,
                    ExtraQueryParameters = new Dictionary < string, string > { { "key3", "value3" }, { "key4", "value4" } },
                    ExpectedExtraQueryParameters = emptyExpectedExtraQueryParams
                },
                new("B2CCustomDomainAuthority_QueryNoValue")
                {
                    Authority = TestConstants.B2CCustomDomainAuthority + queryNoValue,
                    ExtraQueryParameters = new Dictionary < string, string > { { "key3", "value3" }, { "key4", "value4" } },
                    ExpectedExtraQueryParameters = emptyExpectedExtraQueryParams
                }
            };

            return theoryData;
        }

        public static TheoryData<AuthorityHelpersTheoryData> AddAuthorityQueryToOptionsOverlappingExistingValuesTheoryData()
        {
            var singleQuery = "?key1=value1";
            var multipleQueries = "?key1=value1&key2=value2";
            var emptyQuery = "?";
            var queryNoValue = "?key1";

            var singleExpectedExtraQueryParams = new Dictionary<string, string>
            {
                { "key1", "value1" },
                { "key2", "existingValue2" }
            };

            var multipleExpectedExtraQueryParams = new Dictionary<string, string>
            {
                { "key1", "value1" },
                { "key2", "value2" }
            };

            var emptyExpectedExtraQueryParams = new Dictionary<string, string>
            {
                { "key1", "existingValue1" },
                { "key2", "existingValue2" }
            };

            var theoryData = new TheoryData<AuthorityHelpersTheoryData>
            {
                new("AuthorityCommonTenant_SingleQuery")
                {
                    Authority = TestConstants.AuthorityCommonTenant + singleQuery,
                    ExtraQueryParameters = new Dictionary < string, string > { { "key1", "existingValue1" }, { "key2", "existingValue2" } },
                    ExpectedExtraQueryParameters = singleExpectedExtraQueryParams
                },
                new("AuthorityCommonTenant_MultipleQueries")
                {
                    Authority = TestConstants.AuthorityCommonTenant + multipleQueries,
                    ExtraQueryParameters = new Dictionary < string, string > { { "key1", "existingValue1" }, { "key2", "existingValue2" } },
                    ExpectedExtraQueryParameters = multipleExpectedExtraQueryParams
                },
                new("AuthorityCommonTenant_EmptyQuery")
                {
                    Authority = TestConstants.AuthorityCommonTenant + emptyQuery,
                    ExtraQueryParameters = new Dictionary < string, string > { { "key1", "existingValue1" }, { "key2", "existingValue2" } },
                    ExpectedExtraQueryParameters = emptyExpectedExtraQueryParams
                },
                new("AuthorityCommonTenant_QueryNoValue")
                {
                    Authority = TestConstants.AuthorityCommonTenant + queryNoValue,
                    ExtraQueryParameters = new Dictionary < string, string > { { "key1", "existingValue1" }, { "key2", "existingValue2" } },
                    ExpectedExtraQueryParameters = emptyExpectedExtraQueryParams
                },
                new("AuthorityCommonTenantWithV2_SingleQuery")
                {
                    Authority = TestConstants.AuthorityCommonTenantWithV2 + singleQuery,
                        ExtraQueryParameters = new Dictionary < string, string > { { "key1", "existingValue1" }, { "key2", "existingValue2" } },
                    ExpectedExtraQueryParameters = singleExpectedExtraQueryParams
                },
                new("AuthorityCommonTenantWithV2_MultipleQueries")
                {
                    Authority = TestConstants.AuthorityCommonTenantWithV2 + multipleQueries,
                    ExtraQueryParameters =                  new Dictionary < string, string > { { "key1", "existingValue1" }, { "key2", "existingValue2" } },
                    ExpectedExtraQueryParameters = multipleExpectedExtraQueryParams
                },
                new("AuthorityCommonTenantWithV2_EmptyQuery")
                {
                    Authority = TestConstants.AuthorityCommonTenantWithV2 + emptyQuery,
                    ExtraQueryParameters = new Dictionary < string, string > { { "key1", "existingValue1" }, { "key2", "existingValue2" } },
                    ExpectedExtraQueryParameters = emptyExpectedExtraQueryParams
                },
                new("AuthorityCommonTenantWithV2_QueryNoValue")
                {
                    Authority = TestConstants.AuthorityCommonTenantWithV2 + queryNoValue,
                    ExtraQueryParameters = new Dictionary < string, string > { { "key1", "existingValue1" }, { "key2", "existingValue2" } },
                    ExpectedExtraQueryParameters = emptyExpectedExtraQueryParams
                },
                new("B2CAuthorityWithV2_SingleQuery")
                {
                    Authority = TestConstants.B2CAuthorityWithV2 + singleQuery,
                    ExtraQueryParameters = new Dictionary < string, string > { { "key1", "existingValue1" }, { "key2", "existingValue2" } },
                    ExpectedExtraQueryParameters = singleExpectedExtraQueryParams
                },
                new("B2CAuthorityWithV2_MultipleQueries")
                {
                    Authority = TestConstants.B2CAuthorityWithV2 + multipleQueries,
                    ExtraQueryParameters = new Dictionary < string, string > { { "key1", "existingValue1" }, { "key2", "existingValue2" } },
                    ExpectedExtraQueryParameters = multipleExpectedExtraQueryParams
                },
                new("B2CAuthorityWithV2_EmptyQuery")
                {
                    Authority = TestConstants.B2CAuthorityWithV2 + emptyQuery,
                    ExtraQueryParameters = new Dictionary < string, string > { { "key1", "existingValue1" }, { "key2", "existingValue2" } },
                    ExpectedExtraQueryParameters = emptyExpectedExtraQueryParams
                },
                new("B2CAuthorityWithV2_QueryNoValue")
                {
                    Authority = TestConstants.B2CAuthorityWithV2 + queryNoValue,
                    ExtraQueryParameters = new Dictionary < string, string > { { "key1", "existingValue1" }, { "key2", "existingValue2" } },
                    ExpectedExtraQueryParameters = emptyExpectedExtraQueryParams
                },
                new("B2CCustomDomainAuthorityWithV2_SingleQuery")
                {
                    Authority = TestConstants.B2CCustomDomainAuthorityWithV2 + singleQuery,
                    ExtraQueryParameters = new Dictionary < string, string > { { "key1", "existingValue1" }, { "key2", "existingValue2" } },
                    ExpectedExtraQueryParameters = singleExpectedExtraQueryParams
                },
                new("B2CCustomDomainAuthorityWithV2_MultipleQueries")
                {
                    Authority = TestConstants.B2CCustomDomainAuthorityWithV2 + multipleQueries,
                    ExtraQueryParameters = new Dictionary < string, string > { { "key1", "existingValue1" }, { "key2", "existingValue2" } },
                    ExpectedExtraQueryParameters = multipleExpectedExtraQueryParams
                },
                new("B2CCustomDomainAuthorityWithV2_EmptyQuery")
                {
                    Authority = TestConstants.B2CCustomDomainAuthorityWithV2 + emptyQuery,
                    ExtraQueryParameters = new Dictionary < string, string > { { "key1", "existingValue1" }, { "key2", "existingValue2" } },
                    ExpectedExtraQueryParameters = emptyExpectedExtraQueryParams
                },
                new("B2CCustomDomainAuthorityWithV2_QueryNoValue")
                {
                    Authority = TestConstants.B2CCustomDomainAuthorityWithV2 + queryNoValue,
                    ExtraQueryParameters = new Dictionary < string, string > { { "key1", "existingValue1" }, { "key2", "existingValue2" } },
                    ExpectedExtraQueryParameters = emptyExpectedExtraQueryParams
                },
                new("B2CAuthority_SingleQuery")
                {
                    Authority = TestConstants.B2CAuthority + singleQuery,
                    ExtraQueryParameters = new Dictionary < string, string > { { "key1", "existingValue1" }, { "key2", "existingValue2" } },
                    ExpectedExtraQueryParameters = singleExpectedExtraQueryParams
                },
                new("B2CAuthority_MultipleQueries")
                {
                    Authority = TestConstants.B2CAuthority + multipleQueries,
                    ExtraQueryParameters = new Dictionary < string, string > { { "key1", "existingValue1" }, { "key2", "existingValue2" } },
                    ExpectedExtraQueryParameters = multipleExpectedExtraQueryParams
                },
                new("B2CAuthority_EmptyQuery")
                {
                    Authority = TestConstants.B2CAuthority + emptyQuery,
                    ExtraQueryParameters = new Dictionary < string, string > { { "key1", "existingValue1" }, { "key2", "existingValue2" } },   
                    ExpectedExtraQueryParameters = emptyExpectedExtraQueryParams
                },
                new("B2CAuthority_QueryNoValue")
                {
                    Authority = TestConstants.B2CAuthority + queryNoValue,
                    ExtraQueryParameters = new Dictionary < string, string > { { "key1", "existingValue1" }, { "key2", "existingValue2" } },
                    ExpectedExtraQueryParameters = emptyExpectedExtraQueryParams
                },
                new("B2CCustomDomainAuthority_SingleQuery")
                {
                    Authority = TestConstants.B2CCustomDomainAuthority + singleQuery,
                    ExtraQueryParameters = new Dictionary < string, string > { { "key1", "existingValue1" }, { "key2", "existingValue2" } },
                    ExpectedExtraQueryParameters = singleExpectedExtraQueryParams
                },
                new("B2CCustomDomainAuthority_MultipleQueries")
                {
                    Authority = TestConstants.B2CCustomDomainAuthority + multipleQueries,
                    ExtraQueryParameters = new Dictionary < string, string > { { "key1", "existingValue1" }, { "key2", "existingValue2" } },
                    ExpectedExtraQueryParameters = multipleExpectedExtraQueryParams
                },
                new("B2CCustomDomainAuthority_EmptyQuery")
                {
                    Authority = TestConstants.B2CCustomDomainAuthority + emptyQuery,
                    ExtraQueryParameters = new Dictionary < string, string > { { "key1", "existingValue1" }, { "key2", "existingValue2" } },
                    ExpectedExtraQueryParameters = emptyExpectedExtraQueryParams
                },
                new("B2CCustomDomainAuthority_QueryNoValue")
                {
                    Authority = TestConstants.B2CCustomDomainAuthority + queryNoValue,
                    ExtraQueryParameters = new Dictionary < string, string > { { "key1", "existingValue1" }, { "key2", "existingValue2" } },
                    ExpectedExtraQueryParameters = emptyExpectedExtraQueryParams
                }
            };

            return theoryData;
        }
    }

    public class AuthorityHelpersTheoryData : TheoryDataBase
    {
        public AuthorityHelpersTheoryData(string testId) : base(testId)
        {
        }

        public string Authority { get; set; } = string.Empty;

        public IDictionary<string,string> ExtraQueryParameters { get; set; } = new Dictionary<string, string>();

        public IDictionary<string,string> ExpectedExtraQueryParameters { get; set; } = new Dictionary<string, string>();
    }
}
