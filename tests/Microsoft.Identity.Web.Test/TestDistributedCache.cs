// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;

namespace Microsoft.Identity.Web.Test
{
    public class TestDistributedCache : IDistributedCache
    {
        public readonly ConcurrentDictionary<string, byte[]> dict = new ConcurrentDictionary<string, byte[]>();

        public byte[] Get(string key)
        {
            if (dict.TryGetValue(key, out var value))
            {
                return dict[key];
            }

            return null;
        }

        public Task<byte[]> GetAsync(string key, CancellationToken token = default)
        {
            return Task.FromResult(Get(key));
        }

        public void Refresh(string key)
        {
            throw new System.NotImplementedException();
        }

        public Task RefreshAsync(string key, CancellationToken token = default)
        {
            throw new System.NotImplementedException();
        }

        public void Remove(string key)
        {
            dict.TryRemove(key, out var _);
        }

        public Task RemoveAsync(string key, CancellationToken token = default)
        {
            Remove(key);
            return Task.CompletedTask;
        }

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
        {
            dict[key] = value;
        }

        public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
        {
            Set(key, value, options);
            return Task.CompletedTask;
        }
    }
}
