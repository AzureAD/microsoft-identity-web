// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;

namespace Microsoft.Identity.Web.TokenCacheProviders.Distributed
{
    /// <summary>
    /// An implementation of the token cache for both Confidential and Public clients backed by a Distributed Cache.
    /// The Distributed Cache (L2), by default creates a Memory Cache (L1), for faster look up, resulting in a two level cache.
    /// </summary>
    /// <seealso>https://aka.ms/msal-net-token-cache-serialization</seealso>
    public partial class MsalDistributedTokenCacheAdapter : MsalAbstractTokenCacheProvider
    {
        /// <summary>
        /// .NET Core Memory cache.
        /// </summary>
        internal /*for tests*/ readonly IDistributedCache _distributedCache;
        internal /*for tests*/ readonly MemoryCache? _memoryCache;
        private readonly ILogger<MsalDistributedTokenCacheAdapter> _logger;
        private readonly TimeSpan? _memoryCacheExpirationTime;
        private readonly string _distributedCacheType = "DistributedCache"; // for logging
        private readonly string _memoryCacheType = "MemoryCache"; // for logging
        private const string DefaultPurpose = "msal_cache";
        private readonly ConcurrentDictionary<string, object> _failedL2CacheRemovals =
            new ConcurrentDictionary<string, object>();

        /// <summary>
        /// MSAL distributed token cache options.
        /// </summary>
        private readonly MsalDistributedTokenCacheAdapterOptions _distributedCacheOptions;

        /// <summary>
        /// Initializes a new instance of the <see cref="MsalDistributedTokenCacheAdapter"/> class.
        /// </summary>
        /// <param name="distributedCache">Distributed cache instance to use.</param>
        /// <param name="distributedCacheOptions">Options for the token cache.</param>
        /// <param name="logger">MsalDistributedTokenCacheAdapter logger.</param>
        /// <param name="serviceProvider">Service provider. Can be null, in which case the token cache
        /// will not be encrypted. See https://aka.ms/ms-id-web/token-cache-encryption.</param>
        public MsalDistributedTokenCacheAdapter(
                                            IDistributedCache distributedCache,
                                            IOptions<MsalDistributedTokenCacheAdapterOptions> distributedCacheOptions,
                                            ILogger<MsalDistributedTokenCacheAdapter> logger,
                                            IServiceProvider? serviceProvider = null)
            : base(GetDataProtector(distributedCacheOptions, serviceProvider), logger)
        {
            _ = Throws.IfNull(distributedCacheOptions);

            _distributedCache = distributedCache;
            _distributedCacheOptions = distributedCacheOptions.Value;

            if (!_distributedCacheOptions.DisableL1Cache)
            {
                _memoryCache = new MemoryCache(_distributedCacheOptions.L1CacheOptions ?? new MemoryCacheOptions { SizeLimit = MsalDistributedTokenCacheAdapterOptions.FiveHundredMb });
            }

            _logger = logger;

            if (_distributedCacheOptions.AbsoluteExpirationRelativeToNow != null)
            {
                if (_distributedCacheOptions.L1ExpirationTimeRatio <= 0 || _distributedCacheOptions.L1ExpirationTimeRatio > 1)
                {
                    throw new ArgumentOutOfRangeException(nameof(_distributedCacheOptions.L1ExpirationTimeRatio), "L1ExpirationTimeRatio must be greater than 0, less than 1. ");
                }

                _memoryCacheExpirationTime = TimeSpan.FromMilliseconds(_distributedCacheOptions.AbsoluteExpirationRelativeToNow.Value.TotalMilliseconds * _distributedCacheOptions.L1ExpirationTimeRatio);
            }
        }

        private static IDataProtector? GetDataProtector(
            IOptions<MsalDistributedTokenCacheAdapterOptions> distributedCacheOptions,
            IServiceProvider? serviceProvider)
        {
            _ = Throws.IfNull(distributedCacheOptions);

            if (serviceProvider != null && distributedCacheOptions.Value.Encrypt)
            {
                IDataProtectionProvider? dataProtectionProvider = serviceProvider.GetService(typeof(IDataProtectionProvider)) as IDataProtectionProvider;
                return dataProtectionProvider?.CreateProtector(DefaultPurpose);
            }

            return null;
        }

        /// <summary>
        /// Removes a specific token cache, described by its cache key
        /// from the distributed cache.
        /// </summary>
        /// <param name="cacheKey">Key of the cache to remove.</param>
        /// <returns>A <see cref="Task"/> that completes when key removal has completed.</returns>
        protected override async Task RemoveKeyAsync(string cacheKey)
        {
            await RemoveKeyAsync(cacheKey, new CacheSerializerHints()).ConfigureAwait(false);
        }

        /// <summary>
        /// Removes a specific token cache, described by its cache key
        /// from the distributed cache.
        /// </summary>
        /// <param name="cacheKey">Key of the cache to remove.</param>
        /// <param name="cacheSerializerHints">Hints for the cache serialization implementation optimization.</param>
        /// <returns>A <see cref="Task"/> that completes when key removal has completed.</returns>
        protected override async Task RemoveKeyAsync(string cacheKey, CacheSerializerHints cacheSerializerHints)
        {
            const string remove = "Remove";

            if (!string.IsNullOrEmpty(cacheSerializerHints.ShouldNotUseDistributedCacheMessage))
            {
                throw new InvalidOperationException(TokenCacheErrorMessage.CannotUseDistributedCache + " " + cacheSerializerHints?.ShouldNotUseDistributedCacheMessage);
            }

            object removalMarker = new object();
            _failedL2CacheRemovals[cacheKey] = removalMarker;

            if (_memoryCache != null)
            {
                _memoryCache.Remove(cacheKey);

                Logger.MemoryCacheRemove(_logger, _memoryCacheType, remove, cacheKey, null);
            }

            await RemoveFromL2CacheAsync(cacheKey, cacheSerializerHints.CancellationToken).ConfigureAwait(false);
            RemoveFailedRemovalIfCurrent(cacheKey, removalMarker);
        }

        /// <summary>
        /// Read a specific token cache, described by its cache key, from the
        /// distributed cache.
        /// </summary>
        /// <param name="cacheKey">Key of the cache item to retrieve.</param>
        /// <returns>Read blob representing a token cache for the cache key
        /// (account or app).</returns>
        protected override async Task<byte[]?> ReadCacheBytesAsync(string cacheKey)
        {
            return await ReadCacheBytesAsync(cacheKey, new CacheSerializerHints()).ConfigureAwait(false);
        }

        /// <summary>
        /// Read a specific token cache, described by its cache key, from the
        /// distributed cache.
        /// </summary>
        /// <param name="cacheKey">Key of the cache item to retrieve.</param>
        /// <param name="cacheSerializerHints">Hints for the cache serialization implementation optimization.</param>
        /// <returns>Read blob representing a token cache for the cache key
        /// (account or app).</returns>
        protected override async Task<byte[]?> ReadCacheBytesAsync(string cacheKey, CacheSerializerHints cacheSerializerHints)
        {
            const string read = "Read";
            byte[]? result = null;
            var telemetryData = cacheSerializerHints.TelemetryData;

            if (!string.IsNullOrEmpty(cacheSerializerHints.ShouldNotUseDistributedCacheMessage))
            {
                throw new InvalidOperationException(TokenCacheErrorMessage.CannotUseDistributedCache + " " + cacheSerializerHints?.ShouldNotUseDistributedCacheMessage);
            }

            if (_failedL2CacheRemovals.TryGetValue(cacheKey, out _))
            {
                _memoryCache?.Remove(cacheKey);
                return null;
            }

            if (_memoryCache != null)
            {
                // check memory cache first
                result = (byte[]?)_memoryCache.Get(cacheKey);
                Logger.MemoryCacheRead(_logger, _memoryCacheType, read, cacheKey, result?.Length ?? 0);
            }

            if (result == null)
            {
                var measure = await Task.Run(
                    async () =>
                {
                    // not found in memory, check distributed cache
                    result = await L2OperationWithRetryOnFailureAsync(
                        read,
                        (cacheKey) => _distributedCache.GetAsync(cacheKey, cacheSerializerHints.CancellationToken),
                        cacheKey).ConfigureAwait(false);
#pragma warning disable CA1062 // Validate arguments of public methods
                }, cacheSerializerHints.CancellationToken).MeasureAsync().ConfigureAwait(false);
#pragma warning restore CA1062 // Validate arguments of public methods

                if (result != null && telemetryData != null)
                {
                    telemetryData.CacheLevel = Client.Cache.CacheLevel.L2Cache;
                }

                Logger.DistributedCacheReadTime(_logger, _distributedCacheType, read, measure.MilliSeconds);

                if (_memoryCache != null)
                {
                    // back propagate to memory cache
                    if (result != null)
                    {
                        if (_failedL2CacheRemovals.TryGetValue(cacheKey, out _))
                        {
                            _memoryCache.Remove(cacheKey);
                            return null;
                        }

                        MemoryCacheEntryOptions memoryCacheEntryOptions = new MemoryCacheEntryOptions
                        {
                            AbsoluteExpirationRelativeToNow = _memoryCacheExpirationTime,
                            Size = result?.Length,
                        };

                        Logger.BackPropagateL2toL1(_logger, memoryCacheEntryOptions.Size ?? 0);
                        _memoryCache.Set(cacheKey, result, memoryCacheEntryOptions);
                        if (_failedL2CacheRemovals.TryGetValue(cacheKey, out _))
                        {
                            _memoryCache.Remove(cacheKey);
                            return null;
                        }

                        Logger.MemoryCacheCount(_logger, _memoryCacheType, read, _memoryCache.Count);
                    }
                }

                if (_failedL2CacheRemovals.TryGetValue(cacheKey, out _))
                {
                    _memoryCache?.Remove(cacheKey);
                    return null;
                }
            }
            else
            {
                await L2OperationWithRetryOnFailureAsync(
                       "Refresh",
                       (cacheKey) => _distributedCache.RefreshAsync(cacheKey, cacheSerializerHints.CancellationToken),
                       cacheKey,
                       result!).ConfigureAwait(false);

                if (_failedL2CacheRemovals.TryGetValue(cacheKey, out _))
                {
                    _memoryCache?.Remove(cacheKey);
                    return null;
                }

                if (telemetryData != null)
                {
                    telemetryData.CacheLevel = Client.Cache.CacheLevel.L1Cache;
                }
            }

#pragma warning disable CS8603 // Possible null reference return.
            return result;
#pragma warning restore CS8603 // Possible null reference return.
        }

        /// <summary>
        /// Writes a token cache blob to the serialization cache (by key).
        /// </summary>
        /// <param name="cacheKey">Cache key.</param>
        /// <param name="bytes">blob to write.</param>
        /// <returns>A <see cref="Task"/> that completes when a write operation has completed.</returns>
        protected override async Task WriteCacheBytesAsync(string cacheKey, byte[] bytes)
        {
            await WriteCacheBytesAsync(cacheKey, bytes, new CacheSerializerHints()).ConfigureAwait(false);
        }

        /// <summary>
        /// Writes a token cache blob to the serialization cache (by key).
        /// </summary>
        /// <param name="cacheKey">Cache key.</param>
        /// <param name="bytes">blob to write.</param>
        /// <param name="cacheSerializerHints">Hints for the cache serialization implementation optimization.</param>
        /// <returns>A <see cref="Task"/> that completes when a write operation has completed.</returns>
        protected override async Task WriteCacheBytesAsync(
            string cacheKey,
            byte[] bytes,
            CacheSerializerHints? cacheSerializerHints)
        {
            const string write = "Write";
            _failedL2CacheRemovals.TryGetValue(cacheKey, out object? failedRemovalAtInvocation);

            DateTimeOffset? cacheExpiry = cacheSerializerHints?.SuggestedCacheExpiry;

            if (!string.IsNullOrEmpty(cacheSerializerHints?.ShouldNotUseDistributedCacheMessage))
            {
                throw new InvalidOperationException(TokenCacheErrorMessage.CannotUseDistributedCache + " " + cacheSerializerHints?.ShouldNotUseDistributedCacheMessage);
            }

            if (_memoryCache != null)
            {
                MemoryCacheEntryOptions memoryCacheEntryOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpiration = cacheExpiry ?? _distributedCacheOptions.AbsoluteExpiration,
                    AbsoluteExpirationRelativeToNow = _memoryCacheExpirationTime,
                    Size = bytes?.Length,
                };

                // write in both
                _memoryCache.Set(cacheKey, bytes, memoryCacheEntryOptions);
                Logger.MemoryCacheRead(_logger, _memoryCacheType, write, cacheKey, bytes?.Length ?? 0);
                Logger.MemoryCacheCount(_logger, _memoryCacheType, write, _memoryCache.Count);
            }

            if ((cacheExpiry != null && _distributedCacheOptions.AbsoluteExpiration != null && _distributedCacheOptions.AbsoluteExpiration < cacheExpiry)
               || (cacheExpiry == null && _distributedCacheOptions.AbsoluteExpiration != null))
            {
                cacheExpiry = _distributedCacheOptions.AbsoluteExpiration;
            }

            DistributedCacheEntryOptions distributedCacheEntryOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpiration = cacheExpiry,
                AbsoluteExpirationRelativeToNow = _distributedCacheOptions.AbsoluteExpirationRelativeToNow,
                SlidingExpiration = _distributedCacheOptions.SlidingExpiration,
            };

            if (_distributedCacheOptions.DisableL1Cache || !_distributedCacheOptions.EnableAsyncL2Write)
            {
                var writeMeasure = await L2OperationWithRetryOnFailureAsync(
                    write,
                    (cacheKey) => _distributedCache.SetAsync(
                        cacheKey,
                        bytes!, // We know that in the Write case, the bytes won't be null 
                                // the parent class 
                        distributedCacheEntryOptions,
                        cacheSerializerHints?.CancellationToken ?? CancellationToken.None),
                    cacheKey,
                    bytes!).MeasureAsync().ConfigureAwait(false);
                ClearFailedRemovalAfterConfirmedWrite(cacheKey, failedRemovalAtInvocation, writeMeasure.Result);
            }
            else
            {
                _ = Task.Run(async () =>
                {
                    var writeMeasure = await L2OperationWithRetryOnFailureAsync(
                        write,
                        (cacheKey) => _distributedCache.SetAsync(
                            cacheKey,
                            bytes!,
                            distributedCacheEntryOptions,
                            cacheSerializerHints?.CancellationToken ?? CancellationToken.None),
                        cacheKey,
                        bytes!).MeasureAsync().ConfigureAwait(false);
                    ClearFailedRemovalAfterConfirmedWrite(cacheKey, failedRemovalAtInvocation, writeMeasure.Result);
                });
            }
        }

        private async Task<bool> L2OperationWithRetryOnFailureAsync(
            string operation,
            Func<string, Task> cacheOperation,
            string cacheKey,
            byte[]? bytes = null,
            bool inRetry = false)
        {
            try
            {
                var measure = await cacheOperation(cacheKey).MeasureAsync().ConfigureAwait(false);
                Logger.DistributedCacheStateWithTime(
                    _logger,
                    _distributedCacheType,
                    operation,
                    cacheKey,
                    bytes?.Length ?? 0,
                    inRetry,
                    measure.MilliSeconds);
                return true;
            }
            catch (Exception ex)
            {
                Logger.DistributedCacheConnectionError(
                    _logger,
                    _distributedCacheType,
                    operation,
                    inRetry,
                    ex.Message,
                    ex);

                if (_distributedCacheOptions.OnL2CacheFailure != null && _distributedCacheOptions.OnL2CacheFailure(ex) && !inRetry)
                {
                    Logger.DistributedCacheRetry(_logger, _distributedCacheType, operation, cacheKey, null);
                    return await L2OperationWithRetryOnFailureAsync(
                        operation,
                        cacheOperation,
                        cacheKey,
                        bytes,
                        true).ConfigureAwait(false);
                }

                return false;
            }
        }

        private async Task RemoveFromL2CacheAsync(string cacheKey, CancellationToken cancellationToken)
        {
            const string remove = "Remove";

            try
            {
                await RemoveFromL2CacheAsync(cacheKey, false, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception initialException)
            {
                Logger.DistributedCacheConnectionError(
                    _logger,
                    _distributedCacheType,
                    remove,
                    false,
                    initialException.Message,
                    initialException);

                if (_distributedCacheOptions.OnL2CacheFailure?.Invoke(initialException) != true)
                {
                    throw CreateRemovalFailure(initialException);
                }

                Logger.DistributedCacheRetry(_logger, _distributedCacheType, remove, cacheKey, null);
                try
                {
                    await RemoveFromL2CacheAsync(cacheKey, true, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception retryException)
                {
                    Logger.DistributedCacheConnectionError(
                        _logger,
                        _distributedCacheType,
                        remove,
                        true,
                        retryException.Message,
                        retryException);
                    throw CreateRemovalFailure(retryException);
                }
            }
        }

        private async Task RemoveFromL2CacheAsync(
            string cacheKey,
            bool inRetry,
            CancellationToken cancellationToken)
        {
            var measure = await _distributedCache.RemoveAsync(cacheKey, cancellationToken).MeasureAsync().ConfigureAwait(false);
            Logger.DistributedCacheStateWithTime(
                _logger,
                _distributedCacheType,
                "Remove",
                cacheKey,
                0,
                inRetry,
                measure.MilliSeconds);
        }

        private static MsalClientException CreateRemovalFailure(Exception innerException)
        {
            return new MsalClientException(
                TokenCacheErrorMessage.L2CacheRemovalFailedErrorCode,
                TokenCacheErrorMessage.L2CacheRemovalFailed,
                innerException);
        }

        private void ClearFailedRemovalAfterConfirmedWrite(
            string cacheKey,
            object? failedRemovalAtInvocation,
            bool writeConfirmed)
        {
            if (writeConfirmed && failedRemovalAtInvocation != null)
            {
                RemoveFailedRemovalIfCurrent(cacheKey, failedRemovalAtInvocation);
            }
        }

        private void RemoveFailedRemovalIfCurrent(string cacheKey, object failedRemoval)
        {
            ((ICollection<KeyValuePair<string, object>>)_failedL2CacheRemovals)
                .Remove(new KeyValuePair<string, object>(cacheKey, failedRemoval));
        }

        private async Task<byte[]?> L2OperationWithRetryOnFailureAsync(
            string operation,
            Func<string, Task<byte[]?>> cacheOperation,
            string cacheKey,
            bool inRetry = false)
        {
            byte[]? result = null;
            try
            {
                result = await cacheOperation(cacheKey).ConfigureAwait(false);
                Logger.DistributedCacheState(
                    _logger,
                    _distributedCacheType,
                    operation,
                    cacheKey,
                    result?.Length ?? 0,
                    inRetry);
            }
            catch (Exception ex)
            {
                Logger.DistributedCacheConnectionError(
                    _logger,
                    _distributedCacheType,
                    operation,
                    inRetry,
                    ex.Message,
                    ex);

                if (_distributedCacheOptions.OnL2CacheFailure != null && _distributedCacheOptions.OnL2CacheFailure(ex) && !inRetry)
                {
                    Logger.DistributedCacheRetry(_logger, _distributedCacheType, operation, cacheKey, null);
                    result = await L2OperationWithRetryOnFailureAsync(
                        operation,
                        cacheOperation,
                        cacheKey,
                        true).ConfigureAwait(false);
                }
            }

            return result;
        }
    }
}
