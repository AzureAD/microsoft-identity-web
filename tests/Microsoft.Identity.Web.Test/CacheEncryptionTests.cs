// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
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
        // These fields won't be null in tests, as tests call BuildTheRequiredServices()
        private TestMsalDistributedTokenCacheAdapter? _testCacheAdapter;
        private IServiceProvider? _provider;

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task EncryptionTestAsync(bool isEncrypted)
        {
            // Arrange
            byte[] cache = [1, 2, 3, 4];
            BuildTheRequiredServices(isEncrypted);
            _testCacheAdapter = (_provider!.GetRequiredService<IMsalTokenCacheProvider>() as TestMsalDistributedTokenCacheAdapter)!;
            TestTokenCache tokenCache = new TestTokenCache();
            TokenCacheNotificationArgs args = InstantiateTokenCacheNotificationArgs(tokenCache);
            await _testCacheAdapter.InitializeAsync(tokenCache);

            // Act
            await tokenCache._beforeAccess(args);
            tokenCache.cache = cache;
            await tokenCache._afterAccess(args);

            // Assert
            Assert.NotNull(_testCacheAdapter._memoryCache);
            Assert.Equal(1, _testCacheAdapter._memoryCache.Count);
            Assert.NotEqual(cache.SequenceEqual(GetFirstCacheValue(_testCacheAdapter._memoryCache)), isEncrypted);
        }

        private byte[] GetFirstCacheValue(MemoryCache memoryCache)
        {
            IDictionary memoryCacheContent;
# if NET6_0 
            memoryCacheContent = (memoryCache
                .GetType()
                .GetProperty("StringKeyEntriesCollection", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                .GetValue(_testCacheAdapter!._memoryCache) as IDictionary)!;            
#elif NET7_0
            dynamic content1 = memoryCache
                .GetType()
                .GetField("_coherentState", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                .GetValue(memoryCache)!;
            memoryCacheContent = (content1?
                .GetType()
                .GetField("_entries", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .GetValue(content1) as IDictionary)!;
#elif NET8_0
            dynamic content1 = memoryCache
                .GetType()
                .GetField("_coherentState", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                .GetValue(memoryCache)!;
            memoryCacheContent = (content1?
                .GetType()
                .GetField("_stringEntries", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .GetValue(content1) as IDictionary)!;
#elif NET9_0_OR_GREATER
            dynamic content1 = memoryCache
                .GetType()
                .GetField("_coherentState", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                .GetValue(memoryCache)!;
            memoryCacheContent = (content1?
                .GetType()
                .GetProperty("EntriesCollection", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .GetValue(content1) as IDictionary)!;
#else
            memoryCacheContent = (memoryCache
                .GetType()
                .GetField("_entries", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                .GetValue(_testCacheAdapter!._memoryCache) as IDictionary)!;
#endif
            var firstEntry = memoryCacheContent.Values.OfType<object>().First();
            var firstEntryValue = firstEntry.GetType()
                .GetProperty("Value")!
                .GetValue(firstEntry);
            return (firstEntryValue as byte[])!;
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
            IAccount? account = null;
            bool hasStateChanged = true;
            bool isAppCache = false;
            bool hasTokens = true;
            CancellationToken cancellationToken = CancellationToken.None;
            string suggestedCacheKey = "key";
            DateTimeOffset? suggestedCacheExpiry = null;
            TokenCacheNotificationArgs args = new TokenCacheNotificationArgs(
                    tokenCacheSerializer,
                    clientId,
                    account,
                    hasStateChanged,
                    isAppCache,
                    suggestedCacheKey,
                    hasTokens,
                    suggestedCacheExpiry,
                    cancellationToken);

            return args;
        }
    }

    public class TestTokenCache : ITokenCache, ITokenCacheSerializer
    {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public byte[] cache;
        public Func<TokenCacheNotificationArgs, Task> _beforeAccess;
        public Func<TokenCacheNotificationArgs, Task> _afterAccess;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

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
