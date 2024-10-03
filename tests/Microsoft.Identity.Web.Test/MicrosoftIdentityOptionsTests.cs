// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authentication.OpenIdConnect.Claims;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
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
        private ServiceProvider? _provider;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        private IOptionsMonitor<MicrosoftIdentityOptions> _microsoftIdentityOptionsMonitor;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

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

#pragma warning disable xUnit1012 // Null should only be used for nullable parameters
        [Theory]
        [InlineData(TestConstants.ClientId, TestConstants.AadInstance, TestConstants.GuestTenantId, null, null, AzureAd, null)]
        [InlineData(null, TestConstants.AadInstance, TestConstants.GuestTenantId, null, null, null, AzureAd, MissingParam.ClientId)]
        [InlineData("", TestConstants.AadInstance, TestConstants.GuestTenantId, null,  null, null, AzureAd, MissingParam.ClientId)]
        [InlineData(TestConstants.ClientId, null, TestConstants.GuestTenantId, null, null, null, AzureAd, MissingParam.Instance)]
        [InlineData(TestConstants.ClientId, "", TestConstants.GuestTenantId, null, null, null, AzureAd, MissingParam.Instance)]
        [InlineData(TestConstants.ClientId, TestConstants.AadInstance, null, null, null, null, AzureAd, MissingParam.TenantId)]
        [InlineData(TestConstants.ClientId, TestConstants.AadInstance, "", null, null, null, AzureAd, MissingParam.TenantId)]
        [InlineData(TestConstants.ClientId, TestConstants.B2CInstance, null, null, TestConstants.B2CSignUpSignInUserFlow, TestConstants.B2CTenant, AzureAdB2C)]
        [InlineData(TestConstants.ClientId, TestConstants.B2CInstance, null, null, TestConstants.B2CSignUpSignInUserFlow, null, AzureAdB2C, MissingParam.Domain)]
        [InlineData(TestConstants.ClientId, TestConstants.B2CInstance, null, null, TestConstants.B2CSignUpSignInUserFlow, "", AzureAdB2C, MissingParam.Domain)]
        [InlineData(TestConstants.ClientId, null, null, TestConstants.AuthorityWithTenantSpecified, null, null, AzureAd)]
        [InlineData(null, null, null, TestConstants.AuthorityWithTenantSpecified, null, null, AzureAd, MissingParam.ClientId)]
#pragma warning restore xUnit1012 // Null should only be used for nullable parameters
        public void ValidateRequiredMicrosoftIdentityOptions(
           string clientId,
           string instance,
           string tenantid,
           string authority,
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
                    Authority = authority,
                });
            }

            BuildTheRequiredServices();
            MergedOptions mergedOptions = _provider!.GetRequiredService<IMergedOptionsStore>().Get(OpenIdConnectDefaults.AuthenticationScheme);

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

        [Fact]
        public void TestMergedOptions_ContainsClaimsActions()
        {
            _microsoftIdentityOptionsMonitor = new TestOptionsMonitor<MicrosoftIdentityOptions>(new MicrosoftIdentityOptions
            {
                ClaimActions =
                {
                    new UniqueJsonKeyClaimAction(ClaimTypes.Gender, "string", "theGender"),
                },
            });

            BuildTheRequiredServices();
            MergedOptions mergedOptions = _provider!.GetRequiredService<IMergedOptionsStore>().Get(OpenIdConnectDefaults.AuthenticationScheme);

            MergedOptions.UpdateMergedOptionsFromMicrosoftIdentityOptions(_microsoftIdentityOptionsMonitor.Get(OpenIdConnectDefaults.AuthenticationScheme), mergedOptions);

            // Verify that the mergedOptions.ClaimActions has claims
            // It should contain some default ones along with our added one
            Assert.NotEmpty(mergedOptions.ClaimActions.AsEnumerable());

            // See if we can find the ClaimAction that we added
            Assert.Contains(mergedOptions.ClaimActions, action => action.ClaimType == ClaimTypes.Gender);

            // Select the single ClaimAction from the collection
            var genderClaim = mergedOptions.ClaimActions.Single(x => x.ClaimType == ClaimTypes.Gender);

            // Assert its a type of UniqueJsonKeyClaimAction
            Assert.IsType<UniqueJsonKeyClaimAction>(genderClaim);

            // Ensure gender has the value of sex
            var jsonKeyClaim = (genderClaim as UniqueJsonKeyClaimAction)!;
            Assert.Equal("theGender", jsonKeyClaim.JsonKey);
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
