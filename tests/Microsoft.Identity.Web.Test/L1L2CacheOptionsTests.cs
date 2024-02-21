// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Web.Test.Common.TestHelpers;
using Microsoft.Identity.Web.TokenCacheProviders.Distributed;
using Microsoft.Identity.Web.TokenCacheProviders.InMemory;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    [CollectionDefinition(nameof(UiTestNoParallelization), DisableParallelization = true)]
    public class L1L2CacheOptionsTests
    {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        // _provider is initialized by BuildTheRequiredServices() called in all tests.
        private ServiceProvider _provider;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(2)]
        public void MsalDistributedTokenCacheAdapterOptions_L1ExpirationTimeRatio_ThrowsException(double expirationRatio)
        {
            // Arrange
            var msalDistributedTokenOptions = Options.Create(
                new MsalDistributedTokenCacheAdapterOptions()
                {
                    L1ExpirationTimeRatio = expirationRatio,
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(3),
                });
            BuildTheRequiredServices();

            // Act & Assert Exception
            Assert.Throws<ArgumentOutOfRangeException>(() => new TestMsalDistributedTokenCacheAdapter(
                new TestDistributedCache(),
                msalDistributedTokenOptions,
                _provider.GetService<ILogger<MsalDistributedTokenCacheAdapter>>()!));
        }

        [Theory]
        [InlineData(1)]
        [InlineData(.23)]
        public void MsalDistributedTokenCacheAdapterOptions_L1ExpirationTimeRatio(double expirationRatio)
        {
            // Arrange
            var msalDistributedTokenOptions = Options.Create(
                new MsalDistributedTokenCacheAdapterOptions()
                {
                    L1ExpirationTimeRatio = expirationRatio,
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(3),
                });
            BuildTheRequiredServices();

            // Act
            var testCache = new TestMsalDistributedTokenCacheAdapter(
                new TestDistributedCache(),
                msalDistributedTokenOptions,
                _provider.GetService<ILogger<MsalDistributedTokenCacheAdapter>>()!);

            // Assert
            Assert.NotNull(testCache);
            Assert.NotNull(testCache._distributedCache);
            Assert.NotNull(testCache._memoryCache);
            Assert.Equal(0, testCache._memoryCache.Count);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void MsalDistributedTokenCacheAdapterOptions_DisableL1Cache(bool disableL1Cache)
        {
            // Arrange
            var msalDistributedTokenOptions = Options.Create(
                new MsalDistributedTokenCacheAdapterOptions()
                {
                    DisableL1Cache = disableL1Cache,
                });
            BuildTheRequiredServices();

            // Act
            var testCache = new TestMsalDistributedTokenCacheAdapter(
                new TestDistributedCache(),
                msalDistributedTokenOptions,
                _provider.GetService<ILogger<MsalDistributedTokenCacheAdapter>>()!);

            // Assert
            Assert.NotNull(testCache);
            Assert.NotNull(testCache._distributedCache);

            if (!disableL1Cache)
            {
                Assert.NotNull(testCache._memoryCache);
                Assert.Equal(0, testCache._memoryCache.Count);
            }
            else
            {
                Assert.Null(testCache._memoryCache);
            }
        }

        [Fact]
        public void MsalMemoryTokenCacheOptions_SetsDefault_Test()
        {
            var options = new MsalMemoryTokenCacheOptions();
            Assert.Equal(MsalMemoryTokenCacheOptions.DefaultAbsoluteExpirationRelativeToNow, options.AbsoluteExpirationRelativeToNow);
        }

        private void BuildTheRequiredServices()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddDistributedTokenCaches();
            _provider = services.BuildServiceProvider();
        }
    }
}
