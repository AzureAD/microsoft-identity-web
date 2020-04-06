// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Authentication.JwtBearer;
using System;
using Xunit;
using System.Linq;
using Microsoft.Identity.Web.Test.Common;

namespace Microsoft.Identity.Web.Test
{
    public class WebApiServiceCollectionExtensionsTests
    {
        [Fact]
        public void TestAuthority()
        {
            // Arrange
            JwtBearerOptions options = new JwtBearerOptions
            {
                Authority = TestConstants.AuthorityCommonTenant
            };

            // Act and Assert
            WebApiAuthenticationBuilderExtensions.EnsureAuthorityIsV2_0(options);
            Assert.Equal(TestConstants.AuthorityCommonTenantWithV2, options.Authority);

            options.Authority = TestConstants.AuthorityOrganizationsUSTenant;
            WebApiAuthenticationBuilderExtensions.EnsureAuthorityIsV2_0(options);
            Assert.Equal(TestConstants.AuthorityOrganizationsUSWithV2, options.Authority);

            options.Authority = TestConstants.AadInstance + "/common/";
            WebApiAuthenticationBuilderExtensions.EnsureAuthorityIsV2_0(options);
            Assert.Equal(TestConstants.AuthorityCommonTenantWithV2, options.Authority);

            options.Authority = TestConstants.AuthorityCommonTenantWithV2;
            WebApiAuthenticationBuilderExtensions.EnsureAuthorityIsV2_0(options);
            Assert.Equal(TestConstants.AuthorityCommonTenantWithV2, options.Authority);

            options.Authority = TestConstants.AuthorityCommonTenantWithV2 + "/";
            WebApiAuthenticationBuilderExtensions.EnsureAuthorityIsV2_0(options);
            Assert.Equal(TestConstants.AuthorityCommonTenantWithV2, options.Authority);
        }

        [Fact]
        public void TestB2CAuthority()
        {
            // Arrange
            JwtBearerOptions options = new JwtBearerOptions
            {
                Authority = TestConstants.B2CAuthorityWithV2
            };

            // Act and Assert
            WebApiAuthenticationBuilderExtensions.EnsureAuthorityIsV2_0(options);
            Assert.Equal(TestConstants.B2CAuthorityWithV2, options.Authority);

            options.Authority = TestConstants.B2CCustomDomainAuthorityWithV2;
            WebApiAuthenticationBuilderExtensions.EnsureAuthorityIsV2_0(options);
            Assert.Equal(TestConstants.B2CCustomDomainAuthorityWithV2, options.Authority);

            options.Authority = TestConstants.B2CCustomDomainAuthority;
            WebApiAuthenticationBuilderExtensions.EnsureAuthorityIsV2_0(options);
            Assert.Equal(TestConstants.B2CCustomDomainAuthorityWithV2, options.Authority);

            options.Authority = TestConstants.B2CAuthority;
            WebApiAuthenticationBuilderExtensions.EnsureAuthorityIsV2_0(options);
            Assert.Equal(TestConstants.B2CAuthorityWithV2, options.Authority);
        }

        [Fact]
        public void TestAudience()
        {
            JwtBearerOptions options = new JwtBearerOptions();
            MicrosoftIdentityOptions microsoftIdentityOptions = new MicrosoftIdentityOptions() { ClientId = Guid.NewGuid().ToString() };

            // Act and Assert
            options.Audience = TestConstants.HttpLocalHost;
            WebApiAuthenticationBuilderExtensions.EnsureValidAudiencesContainsApiGuidIfGuidProvided(options, microsoftIdentityOptions);
            Assert.True(options.TokenValidationParameters.ValidAudiences.Count() == 1);
            Assert.True(options.TokenValidationParameters.ValidAudiences.First() == TestConstants.HttpLocalHost);

            options.Audience = TestConstants.ApiAudience;
            WebApiAuthenticationBuilderExtensions.EnsureValidAudiencesContainsApiGuidIfGuidProvided(options, microsoftIdentityOptions);
            Assert.True(options.TokenValidationParameters.ValidAudiences.Count() == 1);
            Assert.True(options.TokenValidationParameters.ValidAudiences.First() == TestConstants.ApiAudience);

            options.Audience = TestConstants.ApiClientId;
            WebApiAuthenticationBuilderExtensions.EnsureValidAudiencesContainsApiGuidIfGuidProvided(options, microsoftIdentityOptions);
            Assert.True(options.TokenValidationParameters.ValidAudiences.Count() == 2);
            Assert.True(options.TokenValidationParameters.ValidAudiences.ElementAt(1) == TestConstants.ApiAudience);
            Assert.True(options.TokenValidationParameters.ValidAudiences.ElementAt(0) == TestConstants.ApiClientId);

            options.Audience = null;
            WebApiAuthenticationBuilderExtensions.EnsureValidAudiencesContainsApiGuidIfGuidProvided(options, microsoftIdentityOptions);
            Assert.True(options.TokenValidationParameters.ValidAudiences.Count() == 2);
            Assert.Contains($"api://{microsoftIdentityOptions.ClientId}", options.TokenValidationParameters.ValidAudiences);
            Assert.Contains($"{microsoftIdentityOptions.ClientId}", options.TokenValidationParameters.ValidAudiences);

            options.Audience = string.Empty;
            WebApiAuthenticationBuilderExtensions.EnsureValidAudiencesContainsApiGuidIfGuidProvided(options, microsoftIdentityOptions);
            Assert.True(options.TokenValidationParameters.ValidAudiences.Count() == 2);
            Assert.Contains($"api://{microsoftIdentityOptions.ClientId}", options.TokenValidationParameters.ValidAudiences);
            Assert.Contains($"{microsoftIdentityOptions.ClientId}", options.TokenValidationParameters.ValidAudiences);
        }
    }
}