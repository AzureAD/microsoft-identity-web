// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Globalization;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Web.Test.Common;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    public class MicrosoftIdentityOptionsTests
    {
        private MicrosoftIdentityOptions microsoftIdentityOptions;
        private const string AzureAd = "AzureAd";
        private const string AzureAdB2C = "AzureAdB2C";

        [Fact]
        public void IsB2C_NotNullOrEmptyUserFlow_ReturnsTrue()
        {
            var options = new MicrosoftIdentityOptions()
            {
                SignUpSignInPolicyId = TestConstants.B2CSignUpSignInUserFlow,
            };

            Assert.True(options.IsB2C);
        }

        [Fact]
        public void IsB2C_NullOrEmptyUserFlow_ReturnsFalse()
        {
            var options = new MicrosoftIdentityOptions();

            Assert.False(options.IsB2C);

            options.SignUpSignInPolicyId = string.Empty;

            Assert.False(options.IsB2C);
        }

        [Theory]
        [InlineData(TestConstants.ClientId, TestConstants.AadInstance, TestConstants.TenantId, null, AzureAd, null)]
        [InlineData(null, TestConstants.AadInstance, TestConstants.TenantId, null, null, AzureAd, MissingParam.ClientId)]
        [InlineData("", TestConstants.AadInstance, TestConstants.TenantId, null, null, AzureAd, MissingParam.ClientId)]
        [InlineData(TestConstants.ClientId, null, TestConstants.TenantId, null, null, AzureAd, MissingParam.Instance)]
        [InlineData(TestConstants.ClientId, "", TestConstants.TenantId, null, null, AzureAd, MissingParam.Instance)]
        [InlineData(TestConstants.ClientId, TestConstants.AadInstance, null, null, null, AzureAd, MissingParam.TenantId)]
        [InlineData(TestConstants.ClientId, TestConstants.AadInstance, "", null, null, AzureAd, MissingParam.TenantId)]
        [InlineData(TestConstants.ClientId, TestConstants.B2CInstance, null, TestConstants.B2CSignUpSignInUserFlow, TestConstants.B2CTenant, AzureAdB2C)]
        [InlineData(TestConstants.ClientId, TestConstants.B2CInstance, null, TestConstants.B2CSignUpSignInUserFlow, null, AzureAdB2C, MissingParam.Domain)]
        [InlineData(TestConstants.ClientId, TestConstants.B2CInstance, null, TestConstants.B2CSignUpSignInUserFlow, "", AzureAdB2C, MissingParam.Domain)]
        public void ValidateRequiredMicrosoftIdentityOptions(
           string clientId,
           string instance,
           string tenantid,
           string signUpSignInPolicyId,
           string domain,
           string optionsName,
           MissingParam missingParam = MissingParam.None)
        {
            microsoftIdentityOptions = new MicrosoftIdentityOptions
            {
                ClientId = clientId,
                Instance = instance,
                TenantId = tenantid,
            };

            if (optionsName == AzureAdB2C)
            {
                microsoftIdentityOptions.SignUpSignInPolicyId = signUpSignInPolicyId;
                microsoftIdentityOptions.Domain = domain;
            }

            MicrosoftIdentityOptionsValidation microsoftIdentityOptionsValidation = new MicrosoftIdentityOptionsValidation();
            ValidateOptionsResult result = microsoftIdentityOptionsValidation.Validate(optionsName, microsoftIdentityOptions);

            CheckReturnValueAgainstExpectedMissingParam(missingParam, result);
        }

        private void CheckReturnValueAgainstExpectedMissingParam(MissingParam missingParam, ValidateOptionsResult result)
        {
            if (result.Failed == true)
            {
                string msg = string.Format(CultureInfo.InvariantCulture, "The '{0}' option must be provided.", missingParam);
                Assert.Equal(msg, result.FailureMessage);
            }
            else
            {
                Assert.True(result.Succeeded);
            }
        }

        public enum MissingParam
        {
            None = 0,
            ClientId = 1,
            Instance = 2,
            TenantId = 3,
            Domain = 4,
        }
    }
}
