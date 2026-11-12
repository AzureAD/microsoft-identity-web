// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web.TokenCacheProviders;
using Microsoft.Identity.Web.TokenCacheProviders.Distributed;
using Microsoft.Identity.Web.TokenCacheProviders.InMemory;

namespace Microsoft.Identity.Web.TokenCache.SnapshotPoc
{
    // Test-only opt-in experiment. Existing providers remain the default/control.
    internal sealed class SnapshotTokenCacheProvider : IDisposable
    {
        private readonly bool _enableSnapshots;
        private readonly MsalAbstractTokenCacheProvider _jsonProvider;
        private readonly DistributedStore? _l2;
        private readonly MemoryCache _jsonMemory = new MemoryCache(new MemoryCacheOptions());
        private readonly TimeSpan _retention = TimeSpan.FromMinutes(5);

        internal MemoryCache L1 { get; } = new MemoryCache(new MemoryCacheOptions { SizeLimit = 128 });
        internal int JsonSerializations { get; private set; }
        internal int JsonDeserializations { get; private set; }
        internal int Captures { get; private set; }
        internal int Restores { get; private set; }
        internal string? LastKey { get; private set; }

        internal SnapshotTokenCacheProvider(IDistributedCache? distributedCache = null, bool enableSnapshots = false)
        {
            _enableSnapshots = enableSnapshots;
            if (distributedCache is null)
            {
                _jsonProvider = new MsalMemoryTokenCacheProvider(
                    _jsonMemory, Options.Create(new MsalMemoryTokenCacheOptions()));
            }
            else
            {
                _l2 = new DistributedStore(distributedCache, disableByteL1: enableSnapshots);
                _jsonProvider = _l2;
            }
        }

        internal void Initialize(ITokenCache cache)
        {
            ArgumentNullException.ThrowIfNull(cache);
            if (!_enableSnapshots)
            {
                _jsonProvider.Initialize(cache);
                return;
            }

            cache.SetBeforeAccessAsync(BeforeAccessAsync);
            cache.SetAfterAccessAsync(AfterAccessAsync);
        }

        private async Task BeforeAccessAsync(TokenCacheNotificationArgs args)
        {
            string key = args.SuggestedCacheKey;
            if (string.IsNullOrEmpty(key))
            {
                return;
            }

            LastKey = key;
            var provider = (ITokenCacheSnapshotProvider)args.TokenCache;
            if (L1.TryGetValue(key, out TokenCacheSnapshot? snapshot))
            {
                provider.RestoreSnapshot(snapshot);
                Restores++;
                return;
            }

            byte[]? bytes = _l2 is null ? null : await _l2.ReadAsync(key, Hints(args)).ConfigureAwait(false);
            if (bytes is null)
            {
                provider.RestoreSnapshot(null);
                return;
            }

            args.TokenCache.DeserializeMsalV3(bytes, shouldClearExistingCache: true);
            JsonDeserializations++;
            Store(key, Capture(provider), args.SuggestedCacheExpiry);
        }

        private async Task AfterAccessAsync(TokenCacheNotificationArgs args)
        {
            if (!args.HasStateChanged || string.IsNullOrEmpty(args.SuggestedCacheKey))
            {
                return;
            }

            string key = args.SuggestedCacheKey;
            LastKey = key;
            if (!args.HasTokens)
            {
                await ClearAsync(key).ConfigureAwait(false);
                return;
            }

            var provider = (ITokenCacheSnapshotProvider)args.TokenCache;
            Store(key, Capture(provider), args.SuggestedCacheExpiry);
            if (_l2 is not null)
            {
                byte[] bytes = args.TokenCache.SerializeMsalV3();
                JsonSerializations++;
                await _l2.WriteAsync(key, bytes, Hints(args)).ConfigureAwait(false);
            }
        }

        private TokenCacheSnapshot Capture(ITokenCacheSnapshotProvider provider)
        {
            TokenCacheSnapshot snapshot = provider.CreateSnapshot();
            Captures++;
            return snapshot;
        }

        private void Store(string key, TokenCacheSnapshot snapshot, DateTimeOffset? suggestedExpiry)
        {
            DateTimeOffset expiry = DateTimeOffset.UtcNow + _retention;
            if (suggestedExpiry.HasValue && suggestedExpiry.Value < expiry)
            {
                expiry = suggestedExpiry.Value;
            }

            // POC budget is a partition count, not a claim about retained bytes.
            L1.Set(key, snapshot, new MemoryCacheEntryOptions { Size = 1, AbsoluteExpiration = expiry });
        }

        internal async Task ClearAsync(string key)
        {
            L1.Remove(key);
            await _jsonProvider.ClearAsync(key).ConfigureAwait(false);
        }

        internal object? GetMemoryOnlyControlEntry(string key)
        {
            return _jsonMemory.Get(key);
        }

        private static CacheSerializerHints Hints(TokenCacheNotificationArgs args)
        {
            return new CacheSerializerHints
            {
                CancellationToken = args.CancellationToken,
                SuggestedCacheExpiry = args.SuggestedCacheExpiry
            };
        }

        public void Dispose()
        {
            L1.Dispose();
            _jsonMemory.Dispose();
        }

        // Reuse Identity.Web's actual distributed read/write/expiry behavior.
        // Snapshot mode disables its byte-based L1; only JSON reaches this adapter.
        private sealed class DistributedStore : MsalDistributedTokenCacheAdapter
        {
            internal DistributedStore(IDistributedCache cache, bool disableByteL1)
                : base(cache, Options.Create(new MsalDistributedTokenCacheAdapterOptions
                {
                    DisableL1Cache = disableByteL1,
                    EnableAsyncL2Write = false,
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
                }), NullLogger<MsalDistributedTokenCacheAdapter>.Instance)
            {
            }

            internal Task<byte[]?> ReadAsync(string key, CacheSerializerHints hints)
            {
                return ReadCacheBytesAsync(key, hints);
            }

            internal Task WriteAsync(string key, byte[] bytes, CacheSerializerHints hints)
            {
                return WriteCacheBytesAsync(key, bytes, hints);
            }
        }
    }
}
