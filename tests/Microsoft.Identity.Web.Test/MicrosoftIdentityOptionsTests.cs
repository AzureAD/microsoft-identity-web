// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Globalization;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web.Test.Common;
using Microsoft.Identity.Web.Test.Common.TestHelpers;
using NSubstitute.Extensions;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    public class MicrosoftIdentityOptionsTests
    {
        private const string AzureAd = Constants.AzureAd;
        private const string AzureAdB2C = Constants.AzureAdB2C;
        private ServiceProvider _provider;
        private IOptionsMonitor<MicrosoftIdentityOptions> _microsoftIdentityOptionsMonitor;

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
        [InlineData(TestConstants.ClientId, TestConstants.AadInstance, TestConstants.GuestTenantId, null, AzureAd, null)]
        [InlineData(null, TestConstants.AadInstance, TestConstants.GuestTenantId, null, null, AzureAd, MissingParam.ClientId)]
        [InlineData("", TestConstants.AadInstance, TestConstants.GuestTenantId, null, null, AzureAd, MissingParam.ClientId)]
        [InlineData(TestConstants.ClientId, null, TestConstants.GuestTenantId, null, null, AzureAd, MissingParam.Instance)]
        [InlineData(TestConstants.ClientId, "", TestConstants.GuestTenantId, null, null, AzureAd, MissingParam.Instance)]
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
            if (optionsName == AzureAdB2C)
            {
                _microsoftIdentityOptionsMonitor = new TestOptionsMonitor<MicrosoftIdentityOptions>(new MicrosoftIdentityOptions
                {
                    SignUpSignInPolicyId = signUpSignInPolicyId,
                    Domain = domain,
                    ClientId = clientId,
                    Instance = instance,
                    TenantId = tenantid,
                });
            }
            else
            {
                _microsoftIdentityOptionsMonitor = new TestOptionsMonitor<MicrosoftIdentityOptions>(new MicrosoftIdentityOptions
                {
                    ClientId = clientId,
                    Instance = instance,
                    TenantId = tenantid,
                });
            }

            BuildTheRequiredServices();
            MergedOptions mergedOptions = _provider.GetRequiredService<IOptionsMonitor<MergedOptions>>().Get(OpenIdConnectDefaults.AuthenticationScheme);

            MergedOptions.UpdateMergedOptionsFromMicrosoftIdentityOptions(_microsoftIdentityOptionsMonitor.Get(OpenIdConnectDefaults.AuthenticationScheme), mergedOptions);

            if (missingParam != MissingParam.None)
            {
                var exception = Assert.Throws<ArgumentNullException>(() => MergedOptionsValidation.Validate(mergedOptions));

                CheckReturnValueAgainstExpectedMissingParam(missingParam, exception);
            }
            else
            {
                MergedOptionsValidation.Validate(mergedOptions);
            }
        }

        private void BuildTheRequiredServices()
        {
            var services = new ServiceCollection();
            services.AddTransient(
                provider => _microsoftIdentityOptionsMonitor);
            services.Configure<MergedOptions>(OpenIdConnectDefaults.AuthenticationScheme, options => { });
            services.AddTokenAcquisition();
            services.AddLogging();
            _provider = services.BuildServiceProvider();
        }

        private void CheckReturnValueAgainstExpectedMissingParam(MissingParam missingParam, ArgumentNullException exception)
        {
            Assert.Equal(string.Format(CultureInfo.InvariantCulture, IDWebErrorMessage.ConfigurationOptionRequired, missingParam), exception.Message);
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
