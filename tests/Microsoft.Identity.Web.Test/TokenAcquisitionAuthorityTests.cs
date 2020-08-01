// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web.Test.Common;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    public class TokenAcquisitionAuthorityTests
    {
        private TokenAcquisition _tokenAcquisition;
        private ServiceProvider _provider;
        private ConfidentialClientApplicationOptions _applicationOptions;

        private void InitializeTokenAcquisitionObjects()
        {
            _tokenAcquisition = new TokenAcquisition(
                null,
                null,
                _provider.GetService<IOptions<MicrosoftIdentityOptions>>(),
                _provider.GetService<IOptions<ConfidentialClientApplicationOptions>>(),
                null,
                null,
                _provider);
        }

        private void BuildTheRequiredServices()
        {
            var services = new ServiceCollection();

            _applicationOptions = new ConfidentialClientApplicationOptions
            {
                Instance = TestConstants.AadInstance,
                ClientId = TestConstants.ConfidentialClientId,
                ClientSecret = "cats",
            };

            services.AddTokenAcquisition();
            services.AddTransient(
                provider => Options.Create(new MicrosoftIdentityOptions
                {
                    Authority = TestConstants.AuthorityCommonTenant,
                    ClientId = TestConstants.ConfidentialClientId,
                    CallbackPath = string.Empty,
                }));
            services.AddTransient(
                provider => Options.Create(_applicationOptions));
            _provider = services.BuildServiceProvider();
        }

        [Theory]
        [InlineData(TestConstants.GuestTenantId)]
        [InlineData(TestConstants.HomeTenantId)]
        [InlineData(null)]
        [InlineData("")]
        public void VerifyCorrectAuthorityUsedInTokenAcquisitionTests(string tenant)
        {
            BuildTheRequiredServices();
            InitializeTokenAcquisitionObjects();
            IConfidentialClientApplication app = ConfidentialClientApplicationBuilder
                 .CreateWithApplicationOptions(_applicationOptions)
                 .WithAuthority(TestConstants.AuthorityCommonTenant).Build();

            if (!string.IsNullOrEmpty(tenant))
            {
                Assert.Equal(
                    string.Format(
                        CultureInfo.InvariantCulture, "{0}/{1}/", TestConstants.AadInstance, tenant),
                    _tokenAcquisition.CreateAuthorityBasedOnTenantIfProvided(
                        app,
                        tenant));
            }
            else
            {
                Assert.Equal(app.Authority, _tokenAcquisition.CreateAuthorityBasedOnTenantIfProvided(app, tenant));
            }
        }
    }
}
