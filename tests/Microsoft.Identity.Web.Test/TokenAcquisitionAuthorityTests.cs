// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Globalization;
using System.Net.Http;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web.Test.Common;
using Microsoft.Identity.Web.Test.Common.Mocks;
using Microsoft.Identity.Web.Test.Common.TestHelpers;
using Microsoft.Identity.Web.TokenCacheProviders.InMemory;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    public class TokenAcquisitionAuthorityTests
    {
        private TokenAcquisition _tokenAcquisition;
        private ServiceProvider _provider;
        private IOptionsMonitor<ConfidentialClientApplicationOptions> _applicationOptionsMonitor;
        private IOptionsMonitor<MicrosoftIdentityOptions> _microsoftIdentityOptionsMonitor;

        private void InitializeTokenAcquisitionObjects()
        {
            _tokenAcquisition = new TokenAcquisition(
                new MsalTestTokenCacheProvider(
                _provider.GetService<IMemoryCache>(),
                _provider.GetService<IOptions<MsalMemoryTokenCacheOptions>>()),
                MockHttpContextAccessor.CreateMockHttpContextAccessor(),
                _provider.GetService<IOptionsMonitor<MicrosoftIdentityOptions>>(),
                _provider.GetService<IOptionsMonitor<ConfidentialClientApplicationOptions>>(),
                _provider.GetService<IHttpClientFactory>(),
                _provider.GetService<ILogger<TokenAcquisition>>(),
                _provider);
            _tokenAcquisition.GetOptions(OpenIdConnectDefaults.AuthenticationScheme);
        }

        private void BuildTheRequiredServices()
        {
            var services = new ServiceCollection();
            services.AddTransient(
                provider => _microsoftIdentityOptionsMonitor);
            services.AddTransient(
                provider => _applicationOptionsMonitor);
            services.AddTokenAcquisition();
            services.AddLogging();
            _provider = services.BuildServiceProvider();
        }

        [Theory]
        [InlineData(TestConstants.GuestTenantId)]
        [InlineData(TestConstants.HomeTenantId)]
        [InlineData(null)]
        [InlineData("")]
        public void VerifyCorrectAuthorityUsedInTokenAcquisitionTests(string tenant)
        {
            _microsoftIdentityOptionsMonitor = new TestOptionsMonitor<MicrosoftIdentityOptions>(new MicrosoftIdentityOptions
            {
                Authority = TestConstants.AuthorityCommonTenant,
                ClientId = TestConstants.ConfidentialClientId,
                CallbackPath = string.Empty,
            });

            _applicationOptionsMonitor = new TestOptionsMonitor<ConfidentialClientApplicationOptions>(new ConfidentialClientApplicationOptions
            {
                Instance = TestConstants.AadInstance,
                ClientId = TestConstants.ConfidentialClientId,
                ClientSecret = TestConstants.ClientSecret,
            });

            BuildTheRequiredServices();
            InitializeTokenAcquisitionObjects();
            IConfidentialClientApplication app = ConfidentialClientApplicationBuilder
                 .CreateWithApplicationOptions(_applicationOptionsMonitor.Get(OpenIdConnectDefaults.AuthenticationScheme))
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

        [Theory]
        [InlineData(TestConstants.B2CInstance)]
        [InlineData(TestConstants.B2CLoginMicrosoft)]
        [InlineData(TestConstants.B2CInstance, true)]
        [InlineData(TestConstants.B2CLoginMicrosoft, true)]
        public void VerifyCorrectAuthorityUsedInTokenAcquisition_B2CAuthorityTests(
            string authorityInstance,
            bool withTfp = false)
        {
            _microsoftIdentityOptionsMonitor = new TestOptionsMonitor<MicrosoftIdentityOptions>(new MicrosoftIdentityOptions
            {
                SignUpSignInPolicyId = TestConstants.B2CSignUpSignInUserFlow,
                Domain = TestConstants.B2CTenant,
            });

            if (withTfp)
            {
                _applicationOptionsMonitor = new TestOptionsMonitor<ConfidentialClientApplicationOptions>(new ConfidentialClientApplicationOptions
                {
                    Instance = authorityInstance + "/tfp/",
                    ClientId = TestConstants.ConfidentialClientId,
                    ClientSecret = TestConstants.ClientSecret,
                });
                BuildTheRequiredServices();
            }
            else
            {
                _applicationOptionsMonitor = new TestOptionsMonitor<ConfidentialClientApplicationOptions>(new ConfidentialClientApplicationOptions
                {
                    Instance = authorityInstance,
                    ClientId = TestConstants.ConfidentialClientId,
                    ClientSecret = TestConstants.ClientSecret,
                });

                BuildTheRequiredServices();
            }

            InitializeTokenAcquisitionObjects();

            IConfidentialClientApplication app = _tokenAcquisition.GetOrBuildConfidentialClientApplication(OpenIdConnectDefaults.AuthenticationScheme);

            string expectedAuthority = string.Format(
                CultureInfo.InvariantCulture,
                "{0}/tfp/{1}/{2}/",
                authorityInstance,
                TestConstants.B2CTenant,
                TestConstants.B2CSignUpSignInUserFlow);

            Assert.Equal(expectedAuthority, app.Authority);
        }

        [Theory]
        [InlineData("https://localhost:1234")]
        [InlineData("")]
        public void VerifyCorrectRedirectUriAsync(
            string redirectUri)
        {
            _microsoftIdentityOptionsMonitor = new TestOptionsMonitor<MicrosoftIdentityOptions>(new MicrosoftIdentityOptions
            {
                Authority = TestConstants.AuthorityCommonTenant,
                ClientId = TestConstants.ConfidentialClientId,
                CallbackPath = string.Empty,
            });

            _applicationOptionsMonitor = new TestOptionsMonitor<ConfidentialClientApplicationOptions>(new ConfidentialClientApplicationOptions
            {
                Instance = TestConstants.AadInstance,
                RedirectUri = redirectUri,
                ClientSecret = TestConstants.ClientSecret,
            });

            BuildTheRequiredServices();

            InitializeTokenAcquisitionObjects();

            IConfidentialClientApplication app = _tokenAcquisition.GetOrBuildConfidentialClientApplication(OpenIdConnectDefaults.AuthenticationScheme);

            if (!string.IsNullOrEmpty(redirectUri))
            {
                Assert.Equal(redirectUri, app.AppConfig.RedirectUri);
            }
            else
            {
                Assert.Equal("https://IdentityDotNetSDKAutomation/", app.AppConfig.RedirectUri);
            }
        }
    }
}
