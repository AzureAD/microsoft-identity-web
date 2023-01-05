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
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        private IConfidentialClientApplication _confidentialApp;
        // Non nullable needed for the Argument null exception tests
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        [Fact]
        public void InMemoryCacheExtensionsTests()
        {
            CreateCca();
            _confidentialApp.AddInMemoryTokenCache();

            Assert.NotNull(_confidentialApp.UserTokenCache);
            Assert.NotNull(_confidentialApp.AppTokenCache);
        }

        [Fact]
        public async Task CacheExtensions_CcaAlreadyExists_TestsAsync()
        {
            AuthenticationResult result;
            // new InMemory serializer and new cca
            result = await CreateAppAndGetTokenAsync(CacheType.InMemory, addInstanceMock: true).ConfigureAwait(false);
            Assert.Equal(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);

            result = await CreateAppAndGetTokenAsync(CacheType.InMemory, addTokenMock: false).ConfigureAwait(false);
            Assert.Equal(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);

            // new DistributedInMemory and same cca
            result = await CreateAppAndGetTokenAsync(CacheType.DistributedInMemory).ConfigureAwait(false);
            Assert.Equal(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);

            result = await CreateAppAndGetTokenAsync(CacheType.DistributedInMemory, addTokenMock: false).ConfigureAwait(false);
            Assert.Equal(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);
        }

        [Fact]
        public void InMemoryCacheExtensions_NoCca_ThrowsException_Tests()
        {
            var ex = Assert.Throws<ArgumentNullException>(() => _confidentialApp.AddInMemoryTokenCache());

            Assert.Equal("confidentialClientApp", ex.ParamName);
        }

        [Fact]
        public void InMemoryCache_WithServices_ExtensionsTests()
        {
            CreateCca();
            _confidentialApp.AddInMemoryTokenCache(services =>
            {
                services.AddMemoryCache();
            });

            Assert.NotNull(_confidentialApp.UserTokenCache);
            Assert.NotNull(_confidentialApp.AppTokenCache);
        }

        [Fact]
        public void InMemoryCache_WithServices_NoCca_ThrowsException_Tests()
        {
            var ex = Assert.Throws<ArgumentNullException>(() => _confidentialApp.AddInMemoryTokenCache(services =>
            {
                services.AddMemoryCache();
            }));

            Assert.Equal("confidentialClientApp", ex.ParamName);
        }

        [Fact]
        public void InMemoryCache_WithServices_NoService_ThrowsException_Tests()
        {
            CreateCca();
            var ex = Assert.Throws<ArgumentNullException>(() => _confidentialApp.AddInMemoryTokenCache(null!));

            Assert.Equal("initializeMemoryCache", ex.ParamName);
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
            var ex = Assert.Throws<ArgumentNullException>(() => _confidentialApp.AddDistributedTokenCache(null!));

            Assert.Equal("initializeDistributedCache", ex.ParamName);
        }

        private enum CacheType
        {
            InMemory,
            DistributedInMemory,
        }

        private static async Task<AuthenticationResult> CreateAppAndGetTokenAsync(
            CacheType cacheType,
            bool addTokenMock = true,
            bool addInstanceMock = false)
        {
            using MockHttpClientFactory mockHttp = new MockHttpClientFactory();
            using var discoveryHandler = MockHttpCreator.CreateInstanceDiscoveryMockHandler();
            using var tokenHandler = MockHttpCreator.CreateClientCredentialTokenHandler();
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
