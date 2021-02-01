// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Globalization;
using System.Net.Http;
using System.Threading.Tasks;
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
        private ConfidentialClientApplicationOptions _applicationOptions;
        private MicrosoftIdentityOptions _microsoftIdentityOptions;

        private void InitializeTokenAcquisitionObjects()
        {
            _tokenAcquisition = new TokenAcquisition(
                new MsalTestTokenCacheProvider(
                _provider.GetService<IMemoryCache>(),
                _provider.GetService<IOptions<MsalMemoryTokenCacheOptions>>()),
                MockHttpContextAccessor.CreateMockHttpContextAccessor(),
                _provider.GetService<IOptions<MicrosoftIdentityOptions>>(),
                _provider.GetService<IOptions<ConfidentialClientApplicationOptions>>(),
                _provider.GetService<IHttpClientFactory>(),
                _provider.GetService<ILogger<TokenAcquisition>>(),
                _provider);
        }

        private void BuildTheRequiredServices(string instance = TestConstants.AadInstance)
        {
            var services = new ServiceCollection();

            _applicationOptions = new ConfidentialClientApplicationOptions
            {
                Instance = instance,
                ClientId = TestConstants.ConfidentialClientId,
                ClientSecret = TestConstants.ClientSecret,
            };

            services.AddTokenAcquisition();
            services.AddLogging();
            services.AddTransient(
                provider => Options.Create(_microsoftIdentityOptions));
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
            _microsoftIdentityOptions = new MicrosoftIdentityOptions
            {
                Authority = TestConstants.AuthorityCommonTenant,
                ClientId = TestConstants.ConfidentialClientId,
                CallbackPath = string.Empty,
            };

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

        [Theory]
        [InlineData(TestConstants.B2CInstance)]
        [InlineData(TestConstants.B2CLoginMicrosoft)]
        [InlineData(TestConstants.B2CInstance, true)]
        [InlineData(TestConstants.B2CLoginMicrosoft, true)]
        public async Task VerifyCorrectAuthorityUsedInTokenAcquisition_B2CAuthorityTestsAsync(
            string authorityInstance,
            bool withTfp = false)
        {
            _microsoftIdentityOptions = new MicrosoftIdentityOptions
            {
                SignUpSignInPolicyId = TestConstants.B2CSignUpSignInUserFlow,
                Domain = TestConstants.B2CTenant,
            };

            if (withTfp)
            {
                BuildTheRequiredServices(authorityInstance + "/tfp/");
            }
            else
            {
                BuildTheRequiredServices(authorityInstance);
            }

            InitializeTokenAcquisitionObjects();

            IConfidentialClientApplication app = await _tokenAcquisition.GetOrBuildConfidentialClientApplicationAsync().ConfigureAwait(false);

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
        public async Task VerifyCorrectRedirectUriAsync(
            string redirectUri)
        {
            _microsoftIdentityOptions = new MicrosoftIdentityOptions
            {
                Authority = TestConstants.AuthorityCommonTenant,
                ClientId = TestConstants.ConfidentialClientId,
                CallbackPath = string.Empty,
            };

            BuildTheRequiredServices();
            _applicationOptions.RedirectUri = redirectUri;

            InitializeTokenAcquisitionObjects();

            IConfidentialClientApplication app = await _tokenAcquisition.GetOrBuildConfidentialClientApplicationAsync().ConfigureAwait(false);

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
