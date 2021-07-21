// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Web.Test.Common.TestHelpers;
using Microsoft.Identity.Web.TokenCacheProviders;
using Microsoft.Identity.Web.TokenCacheProviders.Distributed;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    public class CacheEncryptionTests
    {
        private TestMsalDistributedTokenCacheAdapter _testCacheAdapter;
        private IServiceProvider _provider;

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task EncryptionTestAsync(bool isEncrypted)
        {
            // Arrange
            byte[] cache = new byte[] { 1, 2, 3, 4 };
            BuildTheRequiredServices(isEncrypted);
            _testCacheAdapter = _provider.GetService<IMsalTokenCacheProvider>() as TestMsalDistributedTokenCacheAdapter;
            TestTokenCache tokenCache = new TestTokenCache();
            TokenCacheNotificationArgs args = InstantiateTokenCacheNotificationArgs(tokenCache);
            _testCacheAdapter.Initialize(tokenCache);

            // Act
            await tokenCache._beforeAccess(args).ConfigureAwait(false);
            tokenCache.cache = cache;
            await tokenCache._afterAccess(args).ConfigureAwait(false);

            // Assert
            Assert.Equal(1, _testCacheAdapter._memoryCache.Count);
            Assert.NotEqual(cache.SequenceEqual(GetFirstCacheValue()), isEncrypted);
        }

        private byte[] GetFirstCacheValue()
        {
            dynamic content = _testCacheAdapter._memoryCache
                .GetType()
                .GetField("_entries", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .GetValue(_testCacheAdapter._memoryCache);
            System.Collections.IDictionary dictionary = content as System.Collections.IDictionary;
            var firstEntry = dictionary.Values.OfType<object>().First();
            var firstEntryValue = firstEntry.GetType()
                .GetProperty("Value")
                .GetValue(firstEntry);
            return firstEntryValue as byte[];
        }

        private void BuildTheRequiredServices(bool isEncrypted)
        {
            IHostBuilder hostBuilder = Host.CreateDefaultBuilder()
               .ConfigureLogging(logger => { })
               .ConfigureServices(services =>
               {
                   services.AddDataProtection();
                   services.AddLogging();
                   services.AddDistributedTokenCaches();
                   services.AddSingleton<IMsalTokenCacheProvider, TestMsalDistributedTokenCacheAdapter>();
                   services.Configure<MsalDistributedTokenCacheAdapterOptions>(o =>
                   {
                       o.Encrypt = isEncrypted;
                   });
               });
            _provider = hostBuilder.Build().Services;
        }

        private static TokenCacheNotificationArgs InstantiateTokenCacheNotificationArgs(TestTokenCache tokenCache)
        {
            ITokenCacheSerializer tokenCacheSerializer = tokenCache;
            string clientId = string.Empty;
            IAccount account = null;
            bool hasStateChanged = true;
            bool isAppCache = false;
            bool hasTokens = true;
            CancellationToken cancellationToken = CancellationToken.None;
            string suggestedCacheKey = "key";
            DateTimeOffset? suggestedCacheExpiry = null;
            TokenCacheNotificationArgs args = Activator.CreateInstance(
                typeof(TokenCacheNotificationArgs),
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
                null,
                new object[]
                {
                    tokenCacheSerializer,
                    clientId,
                    account,
                    hasStateChanged,
                    isAppCache,
                    hasTokens,
                    cancellationToken,
                    suggestedCacheKey,
                    suggestedCacheExpiry,
                },
                null) as TokenCacheNotificationArgs;
            return args;
        }
    }

    public class TestTokenCache : ITokenCache, ITokenCacheSerializer
    {
        public byte[] cache;
        public Func<TokenCacheNotificationArgs, Task> _beforeAccess;
        public Func<TokenCacheNotificationArgs, Task> _afterAccess;

        public void Deserialize(byte[] msalV2State)
        {
            throw new NotImplementedException();
        }

        public void DeserializeAdalV3(byte[] adalV3State)
        {
            throw new NotImplementedException();
        }

        public void DeserializeMsalV2(byte[] msalV2State)
        {
            throw new NotImplementedException();
        }

        public void DeserializeMsalV3(byte[] msalV3State, bool shouldClearExistingCache = false)
        {
            cache = msalV3State;
        }

        [Obsolete("Obsolete")]
        public void DeserializeUnifiedAndAdalCache(CacheData cacheData)
        {
            throw new NotImplementedException();
        }

        public byte[] Serialize()
        {
            throw new NotImplementedException();
        }

        public byte[] SerializeAdalV3()
        {
            throw new NotImplementedException();
        }

        public byte[] SerializeMsalV2()
        {
            throw new NotImplementedException();
        }

        public byte[] SerializeMsalV3()
        {
            return cache;
        }

        [Obsolete("Obsolete")]
        public CacheData SerializeUnifiedAndAdalCache()
        {
            throw new NotImplementedException();
        }

        public void SetAfterAccess(TokenCacheCallback afterAccess)
        {
            throw new NotImplementedException();
        }

        public void SetAfterAccessAsync(Func<TokenCacheNotificationArgs, Task> afterAccess)
        {
            _afterAccess = afterAccess;
        }

        public void SetBeforeAccess(TokenCacheCallback beforeAccess)
        {
            throw new NotImplementedException();
        }

        public void SetBeforeAccessAsync(Func<TokenCacheNotificationArgs, Task> beforeAccess)
        {
            _beforeAccess = beforeAccess;
        }

        public void SetBeforeWrite(TokenCacheCallback beforeWrite)
        {
            throw new NotImplementedException();
        }

        public void SetBeforeWriteAsync(Func<TokenCacheNotificationArgs, Task> beforeWrite)
        {
        }
    }
}
