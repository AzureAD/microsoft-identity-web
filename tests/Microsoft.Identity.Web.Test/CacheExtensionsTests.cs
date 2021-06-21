// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web.Test.Common;
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
            _confidentialApp.AddInMemoryTokenCaches();

            Assert.NotNull(_confidentialApp.UserTokenCache);
            Assert.NotNull(_confidentialApp.AppTokenCache);
        }

        [Fact]
        public void InMemoryCacheExtensions_NoCca_ThrowsException_Tests()
        {
            var ex = Assert.Throws<ArgumentNullException>(() => _confidentialApp.AddInMemoryTokenCaches());

            Assert.Equal("confidentialClientApp", ex.ParamName);
        }

        [Fact]
        public void DistributedCacheExtensionsTests()
        {
            CreateCca();
            _confidentialApp.AddDistributedTokenCaches(services =>
            {
                services.AddDistributedMemoryCache();
            });

            Assert.NotNull(_confidentialApp.UserTokenCache);
            Assert.NotNull(_confidentialApp.AppTokenCache);
        }

        [Fact]
        public void DistributedCacheExtensions_NoCca_ThrowsException_Tests()
        {
            var ex = Assert.Throws<ArgumentNullException>(() => _confidentialApp.AddDistributedTokenCaches(services =>
            {
                services.AddDistributedMemoryCache();
            }));

            Assert.Equal("confidentialClientApp", ex.ParamName);
        }

        [Fact]
        public void DistributedCacheExtensions_NoService_ThrowsException_Tests()
        {
            CreateCca();

            var ex = Assert.Throws<ArgumentNullException>(() => _confidentialApp.AddDistributedTokenCaches(null));

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
