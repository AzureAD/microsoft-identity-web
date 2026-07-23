// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Web.Test.Common;
using Microsoft.Identity.Web.Test.Common.TestHelpers;
using Microsoft.Identity.Web.TokenCacheProviders;
using Microsoft.Identity.Web.TokenCacheProviders.InMemory;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    public class MsalTokenCacheProviderTests
    {
        [Fact]
        public void CreateTokenCacheSerializerTest()
        {
            IMsalTokenCacheProvider tokenCacheProvider = CreateTokenCacheSerializer();
            Assert.NotNull(tokenCacheProvider);
        }

        [Theory, MemberData(nameof(CacheExpiryData), DisableDiscoveryEnumeration = true)]
        public void CacheEntryExpiry_SetCorrectly_Test(TimeSpan? optionsCacheExpiry, DateTimeOffset? suggestedCacheExpiry, TimeSpan expectedCacheExpiry)
        {
            // Arrange
            var msalCacheOptions = new MsalMemoryTokenCacheOptions();
            if (optionsCacheExpiry.HasValue)
            {
                msalCacheOptions.AbsoluteExpirationRelativeToNow = optionsCacheExpiry.Value;
            }

            var msalCacheProvider = new MsalMemoryTokenCacheProvider(GetMemoryCache(), Options.Create(msalCacheOptions));
            var cacheHints = new CacheSerializerHints()
            {
                SuggestedCacheExpiry = suggestedCacheExpiry,
            };

            // Act
            TimeSpan? calculatedCacheExpiry = msalCacheProvider.DetermineCacheEntryExpiry(cacheHints);

            // Assert
            Assert.NotNull(calculatedCacheExpiry);
            // Using InRange because internal logic uses current date/time, which is variable and not constant.
            Asserts.WithinVariance(calculatedCacheExpiry.Value, expectedCacheExpiry, TimeSpan.FromMinutes(2));
        }

        // TimeSpan? optionsCacheExpiry, DateTimeOffset? suggestedCacheExpiry, TimeSpan expectedCacheExpiry
        public static IEnumerable<object?[]> CacheExpiryData =>
            new List<object?[]>
            {
                new object?[] { null, null, MsalMemoryTokenCacheOptions.DefaultAbsoluteExpirationRelativeToNow }, // no user-provided expiry, no suggested expiry (e.g. user flows)
                new object?[] { null, DateTimeOffset.Now.AddHours(5), TimeSpan.FromHours(5) }, // no user-provided expiry, suggested expiry (e.g. app flows)
                new object?[] { TimeSpan.FromHours(1), null, TimeSpan.FromHours(1) }, // user-provided expiry, no suggested expiry (e.g. user flows)
                new object?[] { TimeSpan.FromHours(1), DateTimeOffset.Now.AddHours(5), TimeSpan.FromHours(1) }, // user-provided expiry shorter than suggested
                new object?[] { TimeSpan.FromHours(10), DateTimeOffset.Now.AddHours(5), TimeSpan.FromHours(5) }, // user-provided expiry longer than suggested
                new object?[] { null, DateTimeOffset.Now.AddHours(-5), TimeSpan.Zero }, // Negative suggested expiry
            };

        private IMemoryCache GetMemoryCache()
        {
            IOptions<MemoryCacheOptions> options = Options.Create(new MemoryCacheOptions());
            return new MemoryCache(options);
        }

        private IMsalTokenCacheProvider CreateTokenCacheSerializer()
        {
            IOptions<MsalMemoryTokenCacheOptions> msalCacheOptions = Options.Create(new MsalMemoryTokenCacheOptions());

            MsalMemoryTokenCacheProvider memoryTokenCacheProvider = new MsalMemoryTokenCacheProvider(GetMemoryCache(), msalCacheOptions);
            return memoryTokenCacheProvider;
        }
    }
}
