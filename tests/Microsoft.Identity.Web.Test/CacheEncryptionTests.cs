// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
        private const string DefaultCacheKey = "default-key";

        private TestDistributedCache EncryptCache
        {
            get { return _testCacheAdapter._distributedCache as TestDistributedCache; }
        }

        [Fact]
        public async Task InMemory_NoEncryption_TestAsync()
        {
            TestTokenCache tokenCache = new TestTokenCache();

            // Arrange
            byte[] cache = new byte[] { 1, 2, 3, 4 };
            BuildTheRequiredServices();
            _testCacheAdapter = _provider.GetService<IMsalTokenCacheProvider>() as TestMsalDistributedTokenCacheAdapter;
            TokenCacheNotificationArgs args = Activator.CreateInstance(typeof(TokenCacheNotificationArgs)) as TokenCacheNotificationArgs;
            _testCacheAdapter.Initialize(tokenCache);

            await tokenCache._beforeAccess(args).ConfigureAwait(false);
            
            await tokenCache._afterAccess(args).ConfigureAwait(false);
            var seralizedBytes = tokenCache.cache;


            // Act
            await _testCacheAdapter.TestWriteCacheBytesAsync(DefaultCacheKey, cache).ConfigureAwait(false);

            // Assert
            Assert.Equal(1, _testCacheAdapter._memoryCache.Count);
            Assert.Single(EncryptCache.dict);
            Assert.Equal(cache, await _testCacheAdapter.TestReadCacheBytesAsync(DefaultCacheKey).ConfigureAwait(false));
        }

        private void BuildTheRequiredServices()
        {
            IHostBuilder hostBuilder = Host.CreateDefaultBuilder()
               .ConfigureLogging(logger => { })
               .ConfigureServices(services =>
               {
#if DOTNET_472 || DOTNET_462
                     services.AddSingleton<IDataProtectionProvider, DpapiDataProtectionProvider>();
#endif
                   services.AddLogging();
                   services.AddDistributedTokenCaches();
                   services.AddSingleton<IMsalTokenCacheProvider, TestMsalDistributedTokenCacheAdapter>();
                   services.Configure<MsalDistributedTokenCacheAdapterOptions>(o =>
                   {
                       o.Encrypt = true;
                   });
               });
            _provider = hostBuilder.Build().Services;
        }

        private static IDistributedCache MakeMockDistributedCache()
        {
            return new TestDistributedCache();
        }
    }

    public class TestTokenCache : ITokenCache
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
