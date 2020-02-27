// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web.Test.Common;
using Microsoft.Identity.Web.Test.Common.Mocks;
using Microsoft.Identity.Web.Test.Common.TestHelpers;
using Microsoft.Identity.Web.TokenCacheProviders.InMemory;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Identity.Web.Test.Integration
{
    public class AcquireTokenForAppIntegrationTests
    {
        TokenAcquisition _tokenAcquisition;
        readonly ServiceProvider _provider;
        private MsalTestTokenCacheProvider _msalTestTokenCacheProvider;

        public AcquireTokenForAppIntegrationTests() //test set-up
        {
            var services = new ServiceCollection();
            services.AddTokenAcquisition();
            services.AddTransient(
                _provider => Options.Create(new MicrosoftIdentityOptions
                {
                    Instance = "https://login.microsoftonline.com/",
                    TenantId = "msidentitysamplestesting.onmicrosoft.com",
                    ClientId = "d6921528-eb23-4423-a023-99b1b60f6285",
                    Authority = "https://login.microsoftonline.com/common"
                }));
            services.AddTransient(
                _provider => Options.Create(new ConfidentialClientApplicationOptions
                {
                    Instance = "https://login.microsoftonline.com/",
                    TenantId = "msidentitysamplestesting.onmicrosoft.com",
                    ClientId = "d6921528-eb23-4423-a023-99b1b60f6285",
                    ClientSecret = ""
                }
                ));
            services.AddLogging();
            services.AddInMemoryTokenCaches();
            _provider = services.BuildServiceProvider();
        }

        [Fact]
        public async Task GetAccessTokenForApp_ReturnsAccessTokenAsync()
        {
            InitializeTokenAcquisitionObjects();
            Assert.Equal(0, _msalTestTokenCacheProvider.Count);
            string token =
                await _tokenAcquisition.AcquireTokenForAppAsync(TestConstants.s_scopesForApp).ConfigureAwait(false);

            Assert.NotNull(token);

            AssertAppTokenInMemoryCache("d6921528-eb23-4423-a023-99b1b60f6285", 1);
        }

        [Fact]
        public async Task GetAccessTokenForApp_IncludesWrongScope_MsalServiceExceptionThrownAsync()
        {
            IEnumerable<string> wrongScopesForApp = new[]
            {
                "user.read"
            };

            InitializeTokenAcquisitionObjects();

            async Task result() =>
                await _tokenAcquisition.AcquireTokenForAppAsync(wrongScopesForApp).ConfigureAwait(false);

            Exception ex = await Assert.ThrowsAsync<MsalServiceException>(result);

            Assert.StartsWith(TestConstants.InvalidScopeErrorcode, ex.Message);
            Assert.Contains(TestConstants.InvalidScopeError, ex.Message);
            Assert.Equal(0, _msalTestTokenCacheProvider.Count);
        }

        private void InitializeTokenAcquisitionObjects()
        {
            IOptions<MicrosoftIdentityOptions> microsoftIdentityOptions = _provider.GetService<IOptions<MicrosoftIdentityOptions>>();
            IOptions<MsalMemoryTokenCacheOptions> tokenOptions = _provider.GetService<IOptions<MsalMemoryTokenCacheOptions>>();
            IOptions<ConfidentialClientApplicationOptions> ccOptions = _provider.GetService<IOptions<ConfidentialClientApplicationOptions>>();
            ILogger<TokenAcquisition> logger = _provider.GetService<ILogger<TokenAcquisition>>();

            Assert.NotNull(microsoftIdentityOptions);

            _msalTestTokenCacheProvider = new MsalTestTokenCacheProvider(
                microsoftIdentityOptions,
                new MockHttpContextAccessor(),
                _provider.GetService<IMemoryCache>(),
                tokenOptions);

            _tokenAcquisition = new TokenAcquisition(
                _msalTestTokenCacheProvider,
                new MockHttpContextAccessor(),
                microsoftIdentityOptions,
                ccOptions,
                logger);
        }

        private void AssertAppTokenInMemoryCache(string clientId, int tokenCount)
        {
            string appTokenKey = clientId + "_AppTokenCache";
            Assert.True(_msalTestTokenCacheProvider.MemoryCache.TryGetValue(appTokenKey, out _));
            Assert.Equal(tokenCount, _msalTestTokenCacheProvider.Count);
        }
    }
}
