// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web.Test.Common;
using Microsoft.Identity.Web.Test.Common.Mocks;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    public class CacheExtensionsTests
    {
        private IConfidentialClientApplication _confidentialApp;

        [Fact]
        public void InMemoryCacheExtensionsTests()
        {
            CreateCca();
            _confidentialApp.AddInMemoryTokenCache();

            Assert.NotNull(_confidentialApp.UserTokenCache);
            Assert.NotNull(_confidentialApp.AppTokenCache);
        }

        [Fact]
        // bug: https://github.com/AzureAD/microsoft-identity-web/issues/1390
        public async Task InMemoryCacheExtensionsAgainTestsAsync()
        {
            AuthenticationResult result;
            result = await CreateAppAndGetTokenAsync(CacheType.InMemory).ConfigureAwait(false);
            Assert.Equal(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);

            result = await CreateAppAndGetTokenAsync(CacheType.InMemory, false, false).ConfigureAwait(false);
            Assert.Equal(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);

            result = await CreateAppAndGetTokenAsync(CacheType.DistributedInMemory, true, false).ConfigureAwait(false);
            Assert.Equal(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);

            result = await CreateAppAndGetTokenAsync(CacheType.DistributedInMemory, false, false).ConfigureAwait(false);
            Assert.Equal(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);
        }

        private enum CacheType
        {
            InMemory,
            DistributedInMemory,
        }

        private static async Task<AuthenticationResult> CreateAppAndGetTokenAsync(
            CacheType cacheType,
            bool addTokenMock = true,
            bool addInstanceMock = true)
        {
            using (MockHttpClientFactory mockHttp = new MockHttpClientFactory())
            using (var discoveryHandler = MockHttpCreator.CreateInstanceDiscoveryMockHandler())
            using (var tokenHandler = MockHttpCreator.CreateClientCredentialTokenHandler())
            {

                if (addInstanceMock)
                {
                    mockHttp.AddMockHandler(discoveryHandler);
                }

                // for when the token is requested from ESTS
                if (addTokenMock)
                {
                    mockHttp.AddMockHandler(tokenHandler);
                }

                var confidentialApp = ConfidentialClientApplicationBuilder
                               .Create(TestConstants.ClientId)
                               .WithAuthority(TestConstants.AuthorityCommonTenant)
                               .WithHttpClientFactory(mockHttp)
                               .WithClientSecret(TestConstants.ClientSecret)
                               .Build();

                switch (cacheType)
                {
                    case CacheType.InMemory:
                        confidentialApp.AddInMemoryTokenCache();
                        break;

                    case CacheType.DistributedInMemory:
                        confidentialApp.AddDistributedTokenCache(services =>
                        {
                            services.AddDistributedMemoryCache();
                            services.AddLogging(configure => configure.AddConsole())
                            .Configure<LoggerFilterOptions>(options => options.MinLevel = Microsoft.Extensions.Logging.LogLevel.Warning);
                        });
                        break;
                }

                var result = await confidentialApp.AcquireTokenForClient(new[] { TestConstants.s_scopeForApp })
                    .ExecuteAsync().ConfigureAwait(false);

                return result;
            }
        }

        [Fact]
        public void InMemoryCacheExtensions_NoCca_ThrowsException_Tests()
        {
            var ex = Assert.Throws<ArgumentNullException>(() => _confidentialApp.AddInMemoryTokenCache());

            Assert.Equal("confidentialClientApp", ex.ParamName);
        }

        [Fact]
        public void DistributedCacheExtensionsTests()
        {
            CreateCca();
            _confidentialApp.AddDistributedTokenCache(services =>
            {
                services.AddDistributedMemoryCache();
            });

            Assert.NotNull(_confidentialApp.UserTokenCache);
            Assert.NotNull(_confidentialApp.AppTokenCache);
        }

        [Fact]
        public void DistributedCacheExtensions_NoCca_ThrowsException_Tests()
        {
            var ex = Assert.Throws<ArgumentNullException>(() => _confidentialApp.AddDistributedTokenCache(services =>
            {
                services.AddDistributedMemoryCache();
            }));

            Assert.Equal("confidentialClientApp", ex.ParamName);
        }

        [Fact]
        public void DistributedCacheExtensions_NoService_ThrowsException_Tests()
        {
            CreateCca();

            var ex = Assert.Throws<ArgumentNullException>(() => _confidentialApp.AddDistributedTokenCache(null));

            Assert.Equal("initializeDistributedCache", ex.ParamName);
        }

        private void CreateCca()
        {
            if (_confidentialApp == null)
            {
                _confidentialApp = ConfidentialClientApplicationBuilder
                           .Create(TestConstants.ClientId)
                           .WithAuthority(TestConstants.AuthorityCommonTenant)
                           .WithClientSecret(TestConstants.ClientSecret)
                           .Build();
            }
        }
    }
}
