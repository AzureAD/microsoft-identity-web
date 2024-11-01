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
        internal readonly ConcurrentDictionary<string, Entry> _dict = new ConcurrentDictionary<string, Entry>();
        internal ManualResetEventSlim ResetEvent { get; set; } = new ManualResetEventSlim(initialState: false);

        public byte[]? Get(string key)
        {
            if (_dict.TryGetValue(key, out var value))
            {
                return _dict[key].Value;
            }

            return null;
        }

        public DistributedCacheEntryOptions? GetDistributedCacheEntryOptions(string key)
        {
            if (_dict.TryGetValue(key, out var value))
            {
                return _dict[key].DistributedCacheEntryOptions;
            }

            return null;
        }

        public Task<byte[]?> GetAsync(string key, CancellationToken token = default)
        {
            return Task.FromResult(Get(key));
        }

        public void Refresh(string key)
        {
            // Don't process anything
        }

        public Task RefreshAsync(string key, CancellationToken token = default)
        {
            Refresh(key);
            return Task.CompletedTask;
        }

        public void Remove(string key)
        {
            _dict.TryRemove(key, out var _);
        }

        public Task RemoveAsync(string key, CancellationToken token = default)
        {
            Remove(key);
            return Task.CompletedTask;
        }

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
        {
            _dict[key] = new Entry(value, options);
            ResetEvent.Set();
        }

        public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
        {
            Set(key, value, options);
            return Task.CompletedTask;
        }

        internal class Entry
        {
            public byte[] Value { get; set; }
            public DistributedCacheEntryOptions DistributedCacheEntryOptions { get; set; }

            public Entry(byte[] value, DistributedCacheEntryOptions options)
            {
                Value = value;
                DistributedCacheEntryOptions = options;
            }
        }
    }
}
