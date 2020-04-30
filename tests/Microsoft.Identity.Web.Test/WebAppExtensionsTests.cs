// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web.Test.Common;
using System;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    public class WebAppExtensionsTests
    {
        private MicrosoftIdentityOptions microsoftIdentityOptions;

        [Theory]
        [InlineData(TestConstants.AuthorityCommonTenant, TestConstants.AuthorityCommonTenantWithV2)]
        [InlineData(TestConstants.AuthorityOrganizationsUSTenant, TestConstants.AuthorityOrganizationsUSWithV2)]
        [InlineData(TestConstants.AuthorityCommonTenantWithV2, TestConstants.AuthorityCommonTenantWithV2)]
        [InlineData(TestConstants.AuthorityCommonTenantWithV2 + "/", TestConstants.AuthorityCommonTenantWithV2)]
        [InlineData(TestConstants.B2CAuthorityWithV2, TestConstants.B2CAuthorityWithV2)]
        [InlineData(TestConstants.B2CCustomDomainAuthorityWithV2, TestConstants.B2CCustomDomainAuthorityWithV2)]
        [InlineData(TestConstants.B2CAuthority, TestConstants.B2CAuthorityWithV2)]
        [InlineData(TestConstants.B2CCustomDomainAuthority, TestConstants.B2CCustomDomainAuthorityWithV2)]
        public void EnsureAuthorityIsV2_0(string initialAuthority, string expectedAuthority)
        {
            OpenIdConnectOptions options = new OpenIdConnectOptions
            {
                Authority = initialAuthority
            };

            options.Authority = AuthorityHelpers.EnsureAuthorityIsV2(options.Authority);
            Assert.Equal(expectedAuthority, options.Authority);
        }

        [Theory]
        [InlineData(false, TestConstants.ClientId, TestConstants.AadInstance, TestConstants.TenantId, null, null)]
        [InlineData(false, null, TestConstants.AadInstance, TestConstants.TenantId, null, null, MissingParam.ClientId)]
        [InlineData(false, "", TestConstants.AadInstance, TestConstants.TenantId, null, null, MissingParam.ClientId)]
        [InlineData(false, TestConstants.ClientId, null, TestConstants.TenantId, null, null, MissingParam.Instance)]
        [InlineData(false, TestConstants.ClientId, "", TestConstants.TenantId, null, null, MissingParam.Instance)]
        [InlineData(false, TestConstants.ClientId, TestConstants.AadInstance, null, null, null, MissingParam.TenantId)]
        [InlineData(false, TestConstants.ClientId, TestConstants.AadInstance, "", null, null, MissingParam.TenantId)]
        [InlineData(true, TestConstants.ClientId, TestConstants.B2CInstance, null, TestConstants.B2CSignUpSignInUserFlow, TestConstants.B2CTenant)]
        [InlineData(true, TestConstants.ClientId, TestConstants.B2CInstance, null, TestConstants.B2CSignUpSignInUserFlow, null, MissingParam.Domain)]
        [InlineData(true, TestConstants.ClientId, TestConstants.B2CInstance, null, TestConstants.B2CSignUpSignInUserFlow, "", MissingParam.Domain)]
        public void ValidateRequiredMicrosoftIdentityOptionsOrThrowIfNullOrEmptyString(
            bool isB2C,
            string clientId,
            string instance,
            string tenantid,
            string signUpSignInPolicyId,
            string domain,
            MissingParam missingParam = MissingParam.None)
        {
            microsoftIdentityOptions = new MicrosoftIdentityOptions
            {
                ClientId = clientId,
                Instance = instance,
                TenantId = tenantid,
            };

            if (isB2C)
            {
                microsoftIdentityOptions.SignUpSignInPolicyId = signUpSignInPolicyId;
                microsoftIdentityOptions.Domain = domain;
            }

            try
            {
                WebAppAuthenticationBuilderExtensions.ValidateRequiredOptions(microsoftIdentityOptions);
            }
            catch (ArgumentNullException ex)
            {
                string msg = "parameter cannot be null or whitespace, " +
                    "and must be included in the configuration of the web app. " +
                    "For instance, in the appsettings.json file. ";

                switch (missingParam)
                {
                    case MissingParam.ClientId:
                        Assert.Equal("ClientId", ex.ParamName);
                        Assert.Contains("The ClientId " + msg, ex.Message, StringComparison.InvariantCulture);
                        break;
                    case MissingParam.Instance:
                        Assert.Equal("Instance", ex.ParamName);
                        Assert.Contains("The Instance " + msg, ex.Message, StringComparison.InvariantCulture);
                        break;
                    case MissingParam.TenantId:
                        Assert.Equal("TenantId", ex.ParamName);
                        Assert.Contains("The TenantId " + msg, ex.Message, StringComparison.InvariantCulture);
                        break;
                    case MissingParam.Domain:
                        Assert.Equal("Domain", ex.ParamName);
                        Assert.Contains("The Domain " + msg, ex.Message, StringComparison.InvariantCulture);
                        break;
                    default:
                        break;
                }
            }
        }
    }

    public enum MissingParam
    {
        None = 0,
        ClientId = 1,
        Instance = 2,
        TenantId = 3,
        Domain = 4
    }
}
