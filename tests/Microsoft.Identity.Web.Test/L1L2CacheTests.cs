// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.TelemetryCore.TelemetryClient;
using Microsoft.Identity.Web.Test.Common.TestHelpers;
using Microsoft.Identity.Web.TokenCacheProviders;
using Microsoft.Identity.Web.TokenCacheProviders.Distributed;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    [CollectionDefinition(nameof(L1L2CacheTests), DisableParallelization = true)]
    public class L1L2CacheTests
    {
        private const string DefaultCacheKey = "default-key";
        private const string AnotherCacheKey = "another-key";
        private ServiceProvider? _provider;
        private ServiceProvider Provider { get { return _provider!; } }
        private readonly TestMsalDistributedTokenCacheAdapter _testCacheAdapter;

        private TestDistributedCache L2Cache
        {
            get { return (_testCacheAdapter._distributedCache as TestDistributedCache)!; }
        }

        public L1L2CacheTests()
        {
            BuildTheRequiredServices();
            _testCacheAdapter = new TestMsalDistributedTokenCacheAdapter(
                MakeMockDistributedCache(),
                Provider.GetService<IOptions<MsalDistributedTokenCacheAdapterOptions>>()!,
                Provider.GetService<ILogger<MsalDistributedTokenCacheAdapter>>()!);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task WriteCache_WritesInL1L2_TestAsync(bool enableAsyncL2Write)
        {
            // Arrange
            Provider.GetService<IOptions<MsalDistributedTokenCacheAdapterOptions>>()!.Value.EnableAsyncL2Write = enableAsyncL2Write;
            byte[] cache = new byte[3];
            AssertCacheValues(_testCacheAdapter);
            Assert.Equal(0, _testCacheAdapter._memoryCache!.Count);
            Assert.Empty(L2Cache._dict);

            // Act
            L2Cache.ResetEvent.Reset();
            await _testCacheAdapter.TestWriteCacheBytesAsync(DefaultCacheKey, cache);

            // Assert
            Assert.Equal(1, _testCacheAdapter._memoryCache.Count);
            L2Cache.ResetEvent.Wait();
            Assert.Single(L2Cache._dict);
        }

        [Fact]
        public async Task WriteCache_NegativeExpiry_TestAsync()
        {
            // Arrange & Act
            await CreateL1L2TestWithSerializerHintsAsync(System.DateTimeOffset.Now - System.TimeSpan.FromHours(1), 0);

            // Assert
            Assert.NotNull(_testCacheAdapter._memoryCache);
            Assert.Null(_testCacheAdapter._memoryCache.Get(DefaultCacheKey));

            await _testCacheAdapter.TestReadCacheBytesAsync(DefaultCacheKey);
            Assert.Equal(1, _testCacheAdapter._memoryCache.Count);
        }

        [Fact]
        public async Task WriteCacheL1L2_NegativeExpiry_TestAsync()
        {
            // Arrange & Act
            await CreateL1L2TestWithSerializerHintsAsync(System.DateTimeOffset.Now - System.TimeSpan.FromHours(1), 0);

            // Assert
            Assert.NotNull(_testCacheAdapter._memoryCache);
            Assert.Null(_testCacheAdapter._memoryCache.Get(DefaultCacheKey));
            var options = (_testCacheAdapter._distributedCache as TestDistributedCache)!.GetDistributedCacheEntryOptions(DefaultCacheKey);
            Assert.NotNull(options);
            Assert.NotNull(options.AbsoluteExpiration);
            await _testCacheAdapter.TestReadCacheBytesAsync(DefaultCacheKey);
            Assert.Equal(1, _testCacheAdapter._memoryCache.Count);
        }

        [Fact]
        public async Task WriteCacheL1L2_PositiveExpiry_TestAsync()
        {
            // Arrange & Act
            var timespan = System.TimeSpan.FromHours(1);
            var expiry = System.DateTimeOffset.UtcNow + timespan;
            await CreateL1L2TestWithSerializerHintsAsync(expiry, 1);

            // Assert
            Assert.NotNull(_testCacheAdapter._memoryCache);
            Assert.NotNull(_testCacheAdapter._memoryCache.Get(DefaultCacheKey));
            TestDistributedCache? testDistributedCache = _testCacheAdapter._distributedCache as TestDistributedCache;
            Assert.NotNull(testDistributedCache);
            var options = testDistributedCache.GetDistributedCacheEntryOptions(DefaultCacheKey);
            Assert.NotNull(options);
            Assert.NotNull(options.AbsoluteExpiration);
            Assert.Equal(expiry, options.AbsoluteExpiration.Value);
            await _testCacheAdapter.TestReadCacheBytesAsync(DefaultCacheKey);
            Assert.Equal(1, _testCacheAdapter._memoryCache.Count);
        }

        [Fact]
        public async Task WriteCacheL1L2_PositiveExpiryAndAbsoluteOptions_TestAsync()
        {
            await CreateL1L2TestWithAbsoluteOptionsAsync(1.5);
        }

        [Fact]
        public async Task WriteCacheL1L2_PositiveExpiryAndAbsoluteOptionsLessThanSuggestedExpiry_TestAsync()
        {
            await CreateL1L2TestWithAbsoluteOptionsAsync(.5);
        }

        private async Task CreateL1L2TestWithAbsoluteOptionsAsync(double time)
        {
            // Arrange & Act
            var timespan = System.TimeSpan.FromHours(1);
            var suggestedExpiry = System.DateTimeOffset.UtcNow + timespan;
            var absoluteOptions = Provider.GetService<IOptions<MsalDistributedTokenCacheAdapterOptions>>();
            Assert.NotNull(absoluteOptions);
            absoluteOptions.Value.AbsoluteExpiration = System.DateTimeOffset.Now + System.TimeSpan.FromHours(time);
            await CreateL1L2TestWithSerializerHintsAsync(suggestedExpiry, 1);

            // Assert
            Assert.NotNull(_testCacheAdapter._memoryCache);
            Assert.NotNull(_testCacheAdapter._memoryCache.Get(DefaultCacheKey));
            Assert.NotNull(_testCacheAdapter._distributedCache as TestDistributedCache);
            var options = (_testCacheAdapter._distributedCache as TestDistributedCache)!.GetDistributedCacheEntryOptions(DefaultCacheKey);
            Assert.NotNull(options);
            Assert.NotNull(options.AbsoluteExpiration);
            if (time < 1)
            {
                Assert.Equal(absoluteOptions.Value.AbsoluteExpiration, options.AbsoluteExpiration.Value);
            }
            else
            {
                Assert.Equal(suggestedExpiry, options.AbsoluteExpiration.Value);
            }

            absoluteOptions.Value.AbsoluteExpiration = null;
            await _testCacheAdapter.TestReadCacheBytesAsync(DefaultCacheKey);
            Assert.Equal(1, _testCacheAdapter._memoryCache.Count);
        }

        private async Task CreateL1L2TestWithSerializerHintsAsync(
            System.DateTimeOffset dateTimeOffset,
            int memoryCacheExpectedCount)
        {
            // Arrange
            byte[] cache = new byte[3];
            AssertCacheValues(_testCacheAdapter);
            Assert.NotNull(_testCacheAdapter._memoryCache);
            Assert.Equal(0, _testCacheAdapter._memoryCache.Count);
            Assert.Empty(L2Cache._dict);
            CacheSerializerHints cacheSerializerHints = new CacheSerializerHints();
            cacheSerializerHints.SuggestedCacheExpiry = dateTimeOffset;

            // Act
            L2Cache.ResetEvent.Reset();
            await _testCacheAdapter.TestWriteCacheBytesAsync(DefaultCacheKey, cache, cacheSerializerHints);

            // Assert
            Assert.Equal(memoryCacheExpectedCount, _testCacheAdapter._memoryCache.Count);
            L2Cache.ResetEvent.Wait();
            Assert.Single(L2Cache._dict);
        }

        [Fact]
        public async Task SetL1Cache_ReadL1_TestAsync()
        {
            // Arrange
            byte[] cache = new byte[3];
            cache[0] = 4;
            TelemetryData telemetryData = new TelemetryData();
            AssertCacheValues(_testCacheAdapter);
            Assert.NotNull(_testCacheAdapter._memoryCache);
            Assert.Equal(0, _testCacheAdapter._memoryCache.Count);
            _testCacheAdapter._memoryCache.Set(DefaultCacheKey, cache, new MemoryCacheEntryOptions { Size = cache.Length });
            Assert.Equal(1, _testCacheAdapter._memoryCache.Count);
            Assert.Empty(L2Cache._dict);

            // Act
            byte[]? result = await _testCacheAdapter.TestReadCacheBytesAsync(DefaultCacheKey, telemetryData);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(4, result[0]);
            Assert.Equal(CacheLevel.L1Cache, telemetryData.CacheLevel);
        }

        [Fact]
        public async Task EmptyL1Cache_ReadL2AndSetL1_TestAsync()
        {
            // Arrange
            byte[] cache = new byte[3];
            cache[0] = 4;
            TelemetryData telemetryData = new TelemetryData();
            AssertCacheValues(_testCacheAdapter);
            _testCacheAdapter._distributedCache.Set(DefaultCacheKey, cache);
            Assert.Single(L2Cache._dict);
            Assert.NotNull(_testCacheAdapter._memoryCache);
            Assert.Equal(0, _testCacheAdapter._memoryCache.Count);

            // Act
            byte[]? result = await _testCacheAdapter.TestReadCacheBytesAsync(DefaultCacheKey, telemetryData);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(4, result[0]);
            Assert.Equal(1, _testCacheAdapter._memoryCache.Count);
            Assert.Single(L2Cache._dict);
            Assert.Equal(CacheLevel.L2Cache, telemetryData.CacheLevel);
        }

        [Fact]
        public async Task EmptyL1Cache_ReadL2AndSetL1_ForTelemetryTestAsync()
        {
            // Arrange
            byte[] cache = new byte[3];
            cache[0] = 4;
            AssertCacheValues(_testCacheAdapter);
            _testCacheAdapter._distributedCache.Set(DefaultCacheKey, cache);
            TelemetryData telemetryData = new TelemetryData();
            Assert.Single(L2Cache._dict);
            Assert.NotNull(_testCacheAdapter._memoryCache);
            Assert.Equal(0, _testCacheAdapter._memoryCache.Count);

            // Act
            byte[]? result = await _testCacheAdapter.TestReadCacheBytesAsync(DefaultCacheKey, telemetryData);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(4, result[0]);
            Assert.Equal(1, _testCacheAdapter._memoryCache.Count);
            Assert.Single(L2Cache._dict);
            Assert.Equal(CacheLevel.L2Cache, telemetryData.CacheLevel);

            // Act
            result = await _testCacheAdapter.TestReadCacheBytesAsync(DefaultCacheKey, telemetryData);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(4, result[0]);
            Assert.Equal(1, _testCacheAdapter._memoryCache.Count);
            Assert.Single(L2Cache._dict);
            Assert.Equal(CacheLevel.L1Cache, telemetryData.CacheLevel);
        }

        [Fact]
        public async Task EmptyL1L2Cache_ReturnNullCacheResult_TestAsync()
        {
            // Arrange
            byte[] cache = new byte[3];
            cache[0] = 4;
            TelemetryData telemetryData = new TelemetryData();
            AssertCacheValues(_testCacheAdapter);
            Assert.NotNull(_testCacheAdapter._memoryCache);
            Assert.Equal(0, _testCacheAdapter._memoryCache.Count);

            // Act
            byte[]? result = await _testCacheAdapter.TestReadCacheBytesAsync(DefaultCacheKey, telemetryData);

            // Assert
            Assert.Null(result);
            Assert.Equal(0, _testCacheAdapter._memoryCache.Count);
            Assert.Empty(L2Cache._dict);
            Assert.Equal(CacheLevel.None, telemetryData.CacheLevel);
        }

        [Fact]
        public async Task SetL1Cache_ReadL1WithDifferentCacheKey__ReturnNullCacheResult_TestAsync()
        {
            // Arrange
            byte[] cache = new byte[3];
            cache[0] = 4;
            AssertCacheValues(_testCacheAdapter);
            Assert.NotNull(_testCacheAdapter._memoryCache);
            Assert.Equal(0, _testCacheAdapter._memoryCache.Count);
            Assert.Empty(L2Cache._dict);
            _testCacheAdapter._memoryCache.Set(AnotherCacheKey, cache, new MemoryCacheEntryOptions { Size = cache.Length });
            Assert.Equal(1, _testCacheAdapter._memoryCache.Count);

            // Act
            byte[]? result = await _testCacheAdapter.TestReadCacheBytesAsync(DefaultCacheKey);

            // Assert
            Assert.Null(result);
            Assert.Empty(L2Cache._dict);
        }

        [Fact]
        public async Task SetL1CacheAndL2CacheWithDifferentCache_ReadL1WithCacheKey__ReturnL2CacheResult_TestAsync()
        {
            // Arrange
            byte[] cacheL1 = new byte[3];
            cacheL1[0] = 4;
            byte[] cacheL2 = new byte[2];
            cacheL2[0] = 9;
            AssertCacheValues(_testCacheAdapter);
            Assert.NotNull(_testCacheAdapter._memoryCache);
            Assert.Equal(0, _testCacheAdapter._memoryCache.Count);
            _testCacheAdapter._memoryCache.Set(AnotherCacheKey, cacheL1, new MemoryCacheEntryOptions { Size = cacheL1.Length });
            _testCacheAdapter._distributedCache.Set(DefaultCacheKey, cacheL2);
            Assert.Equal(1, _testCacheAdapter._memoryCache.Count);
            Assert.Single(L2Cache._dict);

            // Act & Assert
            byte[]? result = await _testCacheAdapter.TestReadCacheBytesAsync(DefaultCacheKey);
            Assert.NotNull(result);
            Assert.Equal(9, result[0]);
            Assert.Equal(2, _testCacheAdapter._memoryCache.Count);
            Assert.Single(L2Cache._dict);

            byte[]? result2 = await _testCacheAdapter.TestReadCacheBytesAsync(AnotherCacheKey);
            Assert.NotNull(result2);
            Assert.Equal(4, result2[0]);
            Assert.Equal(2, _testCacheAdapter._memoryCache.Count);
            Assert.Single(L2Cache._dict);
        }

        [Fact]
        public async Task RemoveL1CacheItem_TestAsync()
        {
            // Arrange
            byte[] cacheL1 = new byte[3];
            cacheL1[0] = 4;
            AssertCacheValues(_testCacheAdapter);
            Assert.NotNull(_testCacheAdapter._memoryCache);
            Assert.Equal(0, _testCacheAdapter._memoryCache.Count);
            _testCacheAdapter._memoryCache.Set(DefaultCacheKey, cacheL1, new MemoryCacheEntryOptions { Size = cacheL1.Length });
            Assert.Equal(1, _testCacheAdapter._memoryCache.Count);

            // Act
            await _testCacheAdapter.TestRemoveKeyAsync(DefaultCacheKey);

            // Assert
            Assert.Equal(0, _testCacheAdapter._memoryCache.Count);
        }

        [Fact]
        public async Task RemoveL2CacheItem_TestAsync()
        {
            // Arrange
            byte[] cacheL2 = new byte[3];
            cacheL2[0] = 4;
            AssertCacheValues(_testCacheAdapter);
            Assert.NotNull(_testCacheAdapter._memoryCache);
            Assert.Equal(0, _testCacheAdapter._memoryCache.Count);
            _testCacheAdapter._distributedCache.Set(DefaultCacheKey, cacheL2);
            Assert.Single(L2Cache._dict);

            // Act
            await _testCacheAdapter.TestRemoveKeyAsync(DefaultCacheKey);

            // Assert
            Assert.Equal(0, _testCacheAdapter._memoryCache.Count);
            Assert.Empty(L2Cache._dict);
        }

        [Fact]
        public async Task RemoveOneCacheItem_OneCacheItemsRemains_TestAsync()
        {
            // Arrange
            byte[] cacheL1 = new byte[3];
            byte[] cacheL2 = new byte[2];
            AssertCacheValues(_testCacheAdapter);
            Assert.NotNull(_testCacheAdapter._memoryCache);
            Assert.Equal(0, _testCacheAdapter._memoryCache.Count);
            _testCacheAdapter._memoryCache.Set(AnotherCacheKey, cacheL1, new MemoryCacheEntryOptions { Size = cacheL1.Length });
            _testCacheAdapter._distributedCache.Set(DefaultCacheKey, cacheL2);

            // Act & Assert
            await _testCacheAdapter.TestRemoveKeyAsync(DefaultCacheKey);
            Assert.Equal(1, _testCacheAdapter._memoryCache.Count);
            Assert.Empty(L2Cache._dict);
            await _testCacheAdapter.TestRemoveKeyAsync(AnotherCacheKey);
            Assert.Equal(0, _testCacheAdapter._memoryCache.Count);
            Assert.Empty(L2Cache._dict);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task RemoveFailure_ThrowsAndBlocksReadsAsync(bool disableL1Cache)
        {
            // Arrange
            InvalidOperationException removalException = new InvalidOperationException("remove failed");
            var (adapter, cache, _) = CreateAdapter(disableL1Cache);
            cache.Set(DefaultCacheKey, new byte[] { 1 });
            cache.RemoveAsyncOverride = (_, _) => Task.FromException(removalException);

            // Act
            MsalClientException exception = await Assert.ThrowsAsync<MsalClientException>(
                () => adapter.TestRemoveKeyAsync(DefaultCacheKey));
            cache.GetAsyncOverride = (_, _) => throw new Xunit.Sdk.XunitException("L2 read should be blocked.");
            byte[]? result = await adapter.TestReadCacheBytesAsync(DefaultCacheKey);

            // Assert
            Assert.Equal(TokenCacheErrorMessage.L2CacheRemovalFailedErrorCode, exception.ErrorCode);
            Assert.Same(removalException, exception.InnerException);
            Assert.Null(result);
            Assert.Null(adapter._memoryCache?.Get(DefaultCacheKey));
        }

        [Fact]
        public async Task RemoveFailure_CallbackFalseIsCalledOnceWithoutRetryAsync()
        {
            // Arrange
            int attempts = 0;
            int callbackCalls = 0;
            var (adapter, cache, options) = CreateAdapter();
            options.OnL2CacheFailure = _ =>
            {
                callbackCalls++;
                return false;
            };
            cache.RemoveAsyncOverride = (_, _) =>
            {
                attempts++;
                return Task.FromException(new InvalidOperationException("remove failed"));
            };

            // Act
            MsalClientException exception = await Assert.ThrowsAsync<MsalClientException>(
                () => adapter.TestRemoveKeyAsync(DefaultCacheKey));

            // Assert
            Assert.Equal(TokenCacheErrorMessage.L2CacheRemovalFailedErrorCode, exception.ErrorCode);
            Assert.Equal(1, attempts);
            Assert.Equal(1, callbackCalls);
        }

        [Fact]
        public async Task RemoveFailure_CallbackTrueRetriesOnceAsync()
        {
            // Arrange
            int attempts = 0;
            int callbackCalls = 0;
            var (adapter, cache, options) = CreateAdapter();
            cache.Set(DefaultCacheKey, new byte[] { 1 });
            options.OnL2CacheFailure = _ =>
            {
                callbackCalls++;
                return true;
            };
            cache.RemoveAsyncOverride = (key, _) =>
            {
                attempts++;
                if (attempts == 1)
                {
                    return Task.FromException(new InvalidOperationException("remove failed"));
                }

                cache.Remove(key);
                return Task.CompletedTask;
            };

            // Act
            await adapter.TestRemoveKeyAsync(DefaultCacheKey);

            // Assert
            Assert.Equal(2, attempts);
            Assert.Equal(1, callbackCalls);
            Assert.Null(await adapter.TestReadCacheBytesAsync(DefaultCacheKey));
        }

        [Fact]
        public async Task RemoveRetryFailure_ThrowsFinalFailureAsync()
        {
            // Arrange
            int attempts = 0;
            int callbackCalls = 0;
            InvalidOperationException retryException = new InvalidOperationException("retry failed");
            var (adapter, cache, options) = CreateAdapter();
            options.OnL2CacheFailure = _ =>
            {
                callbackCalls++;
                return true;
            };
            cache.RemoveAsyncOverride = (_, _) =>
            {
                attempts++;
                return Task.FromException(
                    attempts == 1 ? new InvalidOperationException("remove failed") : retryException);
            };

            // Act
            MsalClientException exception = await Assert.ThrowsAsync<MsalClientException>(
                () => adapter.TestRemoveKeyAsync(DefaultCacheKey));

            // Assert
            Assert.Equal(2, attempts);
            Assert.Equal(1, callbackCalls);
            Assert.Same(retryException, exception.InnerException);
        }

        [Fact]
        public async Task RemoveFailure_CallbackExceptionPropagatesAsync()
        {
            // Arrange
            ApplicationException callbackException = new ApplicationException("callback failed");
            var (adapter, cache, options) = CreateAdapter();
            options.OnL2CacheFailure = _ => throw callbackException;
            cache.RemoveAsyncOverride = (_, _) => Task.FromException(new InvalidOperationException("remove failed"));

            // Act
            ApplicationException exception = await Assert.ThrowsAsync<ApplicationException>(
                () => adapter.TestRemoveKeyAsync(DefaultCacheKey));

            // Assert
            Assert.Same(callbackException, exception);
            cache.GetAsyncOverride = (_, _) => throw new Xunit.Sdk.XunitException("L2 read should be blocked.");
            Assert.Null(await adapter.TestReadCacheBytesAsync(DefaultCacheKey));
        }

        [Fact]
        public async Task RemoveCancellation_PropagatesWithoutCallbackOrRetryAsync()
        {
            // Arrange
            int attempts = 0;
            int callbackCalls = 0;
            var (adapter, cache, options) = CreateAdapter();
            using CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            await cancellationTokenSource.CancelAsync();
            OperationCanceledException expectedException = new OperationCanceledException(
                "caller canceled",
                innerException: null,
                cancellationTokenSource.Token);
            options.OnL2CacheFailure = _ =>
            {
                callbackCalls++;
                return true;
            };
            cache.RemoveAsyncOverride = (_, _) =>
            {
                attempts++;
                return Task.FromException(expectedException);
            };
            CacheSerializerHints hints = new CacheSerializerHints
            {
                CancellationToken = cancellationTokenSource.Token,
            };

            // Act
            OperationCanceledException exception = await Assert.ThrowsAsync<OperationCanceledException>(
                () => adapter.TestRemoveKeyAsync(DefaultCacheKey, hints));

            // Assert
            Assert.Same(expectedException, exception);
            Assert.Equal(1, attempts);
            Assert.Equal(0, callbackCalls);
            cache.GetAsyncOverride = (_, _) => throw new Xunit.Sdk.XunitException("L2 read should be blocked.");
            Assert.Null(await adapter.TestReadCacheBytesAsync(DefaultCacheKey));
        }

#pragma warning disable VSTHRD003 // The tasks are deliberately coordinated to exercise overlapping removals.
        [Fact]
        public async Task OlderSuccessfulRemoval_DoesNotClearNewerFailedRemovalMarkerAsync()
        {
            // Arrange
            var (adapter, cache, _) = CreateAdapter();
            int attempts = 0;
            TaskCompletionSource<object?> firstAttemptStarted = new TaskCompletionSource<object?>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            TaskCompletionSource<object?> releaseFirstAttempt = new TaskCompletionSource<object?>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            cache.RemoveAsyncOverride = async (key, _) =>
            {
                if (Interlocked.Increment(ref attempts) == 1)
                {
                    firstAttemptStarted.SetResult(null);
                    await releaseFirstAttempt.Task;
                    cache.Remove(key);
                    return;
                }

                throw new InvalidOperationException("newer removal failed");
            };

            // Act
            Task olderRemoval = adapter.TestRemoveKeyAsync(DefaultCacheKey);
            await firstAttemptStarted.Task;
            await Assert.ThrowsAsync<MsalClientException>(() => adapter.TestRemoveKeyAsync(DefaultCacheKey));
            releaseFirstAttempt.SetResult(null);
            await olderRemoval;
            cache.GetAsyncOverride = (_, _) => throw new Xunit.Sdk.XunitException("L2 read should be blocked.");
            byte[]? result = await adapter.TestReadCacheBytesAsync(DefaultCacheKey);

            // Assert
            Assert.Equal(2, attempts);
            Assert.Null(result);
            Assert.Equal(1, GetFailedRemovalCount(adapter));
        }

        [Fact]
        public async Task OlderFailedRemoval_DoesNotPublishAfterNewerSuccessfulRemovalAsync()
        {
            // Arrange
            var (adapter, cache, _) = CreateAdapter();
            int attempts = 0;
            TaskCompletionSource<object?> firstAttemptStarted = new TaskCompletionSource<object?>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            TaskCompletionSource<object?> releaseFirstAttempt = new TaskCompletionSource<object?>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            cache.RemoveAsyncOverride = async (key, _) =>
            {
                if (Interlocked.Increment(ref attempts) == 1)
                {
                    firstAttemptStarted.SetResult(null);
                    await releaseFirstAttempt.Task;
                    throw new InvalidOperationException("older removal failed");
                }

                cache.Remove(key);
            };

            // Act
            Task olderRemoval = adapter.TestRemoveKeyAsync(DefaultCacheKey);
            await firstAttemptStarted.Task;
            await adapter.TestRemoveKeyAsync(DefaultCacheKey);
            releaseFirstAttempt.SetResult(null);
            await Assert.ThrowsAsync<MsalClientException>(() => olderRemoval);
            cache.Set(DefaultCacheKey, new byte[] { 6 });
            byte[]? result = await adapter.TestReadCacheBytesAsync(DefaultCacheKey);

            // Assert
            Assert.Equal(2, attempts);
            Assert.NotNull(result);
            Assert.Equal(6, result[0]);
            Assert.Equal(0, GetFailedRemovalCount(adapter));
        }
#pragma warning restore VSTHRD003

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ReadStartedBeforeFailedRemoval_DoesNotReturnOrBackfillAsync(bool useL1)
        {
            // Arrange
            var (adapter, cache, _) = CreateAdapter();
            SemaphoreSlim readStarted = new SemaphoreSlim(0, 1);
            SemaphoreSlim releaseRead = new SemaphoreSlim(0, 1);
            if (useL1)
            {
                adapter._memoryCache!.Set(
                    DefaultCacheKey,
                    new byte[] { 1 },
                    new MemoryCacheEntryOptions { Size = 1 });
                cache.RefreshAsyncOverride = async (_, _) =>
                {
                    readStarted.Release();
                    await releaseRead.WaitAsync();
                };
            }
            else
            {
                cache.GetAsyncOverride = async (_, _) =>
                {
                    readStarted.Release();
                    await releaseRead.WaitAsync();
                    return new byte[] { 1 };
                };
            }

            // Act
            Task<byte[]?> read = adapter.TestReadCacheBytesAsync(DefaultCacheKey);
            await readStarted.WaitAsync();
            cache.RemoveAsyncOverride = (_, _) => Task.FromException(new InvalidOperationException("remove failed"));
            await Assert.ThrowsAsync<MsalClientException>(() => adapter.TestRemoveKeyAsync(DefaultCacheKey));
            releaseRead.Release();
            byte[]? result = await read;

            // Assert
            Assert.Null(result);
            Assert.Null(adapter._memoryCache!.Get(DefaultCacheKey));
        }

        [Fact]
        public async Task WriteStartedBeforeFailedRemoval_DoesNotClearNewerRemovalMarkerAsync()
        {
            // Arrange
            var (adapter, cache, options) = CreateAdapter();
            options.EnableAsyncL2Write = false;
            SemaphoreSlim writeStarted = new SemaphoreSlim(0, 1);
            SemaphoreSlim releaseWrite = new SemaphoreSlim(0, 1);
            cache.SetAsyncOverride = async (key, value, entryOptions, _) =>
            {
                writeStarted.Release();
                await releaseWrite.WaitAsync();
                cache.Set(key, value, entryOptions);
            };

            // Act
            Task write = adapter.TestWriteCacheBytesAsync(DefaultCacheKey, new byte[] { 2 });
            await writeStarted.WaitAsync();
            cache.RemoveAsyncOverride = (_, _) => Task.FromException(new InvalidOperationException("remove failed"));
            await Assert.ThrowsAsync<MsalClientException>(() => adapter.TestRemoveKeyAsync(DefaultCacheKey));
            releaseWrite.Release();
            await write;
            cache.GetAsyncOverride = (_, _) => throw new Xunit.Sdk.XunitException("L2 read should be blocked.");
            byte[]? result = await adapter.TestReadCacheBytesAsync(DefaultCacheKey);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task ConfirmedWrite_ClearsOnlyMarkerObservedAtInvocationAsync()
        {
            // Arrange
            var (adapter, cache, options) = CreateAdapter();
            options.EnableAsyncL2Write = false;
            cache.RemoveAsyncOverride = (_, _) => Task.FromException(new InvalidOperationException("remove failed"));
            await Assert.ThrowsAsync<MsalClientException>(() => adapter.TestRemoveKeyAsync(DefaultCacheKey));
            SemaphoreSlim writeStarted = new SemaphoreSlim(0, 1);
            SemaphoreSlim releaseWrite = new SemaphoreSlim(0, 1);
            cache.SetAsyncOverride = async (key, value, entryOptions, _) =>
            {
                writeStarted.Release();
                await releaseWrite.WaitAsync();
                cache.Set(key, value, entryOptions);
            };

            // Act
            Task write = adapter.TestWriteCacheBytesAsync(DefaultCacheKey, new byte[] { 3 });
            await writeStarted.WaitAsync();
            await Assert.ThrowsAsync<MsalClientException>(() => adapter.TestRemoveKeyAsync(DefaultCacheKey));
            releaseWrite.Release();
            await write;
            cache.GetAsyncOverride = (_, _) => throw new Xunit.Sdk.XunitException("L2 read should be blocked.");
            byte[]? result = await adapter.TestReadCacheBytesAsync(DefaultCacheKey);

            // Assert
            Assert.Null(result);
            Assert.Equal(1, GetFailedRemovalCount(adapter));
        }

        [Fact]
        public async Task ConfirmedWriteAndLaterRemoval_ClearFailedRemovalMarkerAsync()
        {
            // Arrange
            var (adapter, cache, options) = CreateAdapter();
            options.EnableAsyncL2Write = false;
            cache.RemoveAsyncOverride = (_, _) => Task.FromException(new InvalidOperationException("remove failed"));
            await Assert.ThrowsAsync<MsalClientException>(() => adapter.TestRemoveKeyAsync(DefaultCacheKey));

            // Act and assert confirmed write recovery
            await adapter.TestWriteCacheBytesAsync(DefaultCacheKey, new byte[] { 4 });
            Assert.Equal(4, (await adapter.TestReadCacheBytesAsync(DefaultCacheKey))![0]);
            Assert.Equal(0, GetFailedRemovalCount(adapter));

            // Act and assert confirmed later removal recovery
            cache.RemoveAsyncOverride = (_, _) => Task.FromException(new InvalidOperationException("remove failed"));
            await Assert.ThrowsAsync<MsalClientException>(() => adapter.TestRemoveKeyAsync(DefaultCacheKey));
            Assert.Equal(1, GetFailedRemovalCount(adapter));
            cache.RemoveAsyncOverride = null;
            await adapter.TestRemoveKeyAsync(DefaultCacheKey);
            cache.Set(DefaultCacheKey, new byte[] { 5 });

            // Assert
            Assert.Equal(5, (await adapter.TestReadCacheBytesAsync(DefaultCacheKey))![0]);
            Assert.Equal(0, GetFailedRemovalCount(adapter));
        }

#pragma warning disable VSTHRD003 // The fire-and-forget L2 write is deliberately coordinated by the test.
        [Fact]
        public async Task AsyncL2Write_ConfirmedCompletionClearsFailedRemovalMarkerAsync()
        {
            // Arrange
            var (adapter, cache, options) = CreateAdapter();
            options.EnableAsyncL2Write = true;
            cache.RemoveAsyncOverride = (_, _) => Task.FromException(new InvalidOperationException("remove failed"));
            await Assert.ThrowsAsync<MsalClientException>(() => adapter.TestRemoveKeyAsync(DefaultCacheKey));
            Assert.Equal(1, GetFailedRemovalCount(adapter));

            int getCalls = 0;
            cache.GetAsyncOverride = (key, _) =>
            {
                Interlocked.Increment(ref getCalls);
                return Task.FromResult(cache.Get(key));
            };
            TaskCompletionSource<object?> setStarted = new TaskCompletionSource<object?>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            TaskCompletionSource<object?> releaseSet = new TaskCompletionSource<object?>();
            TaskCompletionSource<object?> setCompleted = new TaskCompletionSource<object?>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            cache.SetAsyncOverride = async (key, value, entryOptions, _) =>
            {
                setStarted.SetResult(null);
                await releaseSet.Task;
                cache.Set(key, value, entryOptions);
                setCompleted.SetResult(null);
            };

            // Act
            Task writeCall = adapter.TestWriteCacheBytesAsync(DefaultCacheKey, new byte[] { 8 });
            await writeCall;
            await setStarted.Task;

            // Assert while L2 write is blocked
            Assert.False(setCompleted.Task.IsCompleted);
            Assert.Null(await adapter.TestReadCacheBytesAsync(DefaultCacheKey));
            Assert.Equal(0, getCalls);
            Assert.Equal(1, GetFailedRemovalCount(adapter));

            // Act after confirmed L2 completion
            releaseSet.SetResult(null);
            await setCompleted.Task;
            await WaitForFailedRemovalCountAsync(adapter, 0);
            byte[]? result = await adapter.TestReadCacheBytesAsync(DefaultCacheKey);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(8, result[0]);
            Assert.Equal(1, getCalls);
        }
#pragma warning restore VSTHRD003

        [Fact]
        public async Task OrdinaryReadAndWrite_DoNotCreateFailedRemovalMarkersAsync()
        {
            // Arrange
            var (adapter, _, _) = CreateAdapter();

            // Act
            Assert.Null(await adapter.TestReadCacheBytesAsync(DefaultCacheKey));
            await adapter.TestWriteCacheBytesAsync(AnotherCacheKey, new byte[] { 1 });

            // Assert
            Assert.Equal(0, GetFailedRemovalCount(adapter));
        }


        [Fact]
        public async Task SetLCache_ThrowIf_ShouldNotUseDistributedCache_TestAsync()
        {
            // Arrange
            byte[] cache = new byte[3];
            AssertCacheValues(_testCacheAdapter);
            Assert.NotNull(_testCacheAdapter._memoryCache);
            Assert.Equal(0, _testCacheAdapter._memoryCache.Count);
            Assert.Empty(L2Cache._dict);
            CacheSerializerHints cacheSerializerHints = new CacheSerializerHints();
            cacheSerializerHints.SuggestedCacheExpiry = System.DateTimeOffset.Now - System.TimeSpan.FromHours(1);
            cacheSerializerHints.ShouldNotUseDistributedCacheMessage = "DoNotUseDistCache";

            // Act
            var ex1 = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await _testCacheAdapter.TestWriteCacheBytesAsync(DefaultCacheKey, cache, cacheSerializerHints));

            var ex2 = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await _testCacheAdapter.TestReadCacheBytesAsync(DefaultCacheKey, cacheSerializerHints));

            var ex3 = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await _testCacheAdapter.TestRemoveKeyAsync(DefaultCacheKey, cacheSerializerHints));

            // Assert
            Assert.Equal(TokenCacheErrorMessage.CannotUseDistributedCache + " DoNotUseDistCache", ex1.Message);
            Assert.Equal(TokenCacheErrorMessage.CannotUseDistributedCache + " DoNotUseDistCache", ex2.Message);
            Assert.Equal(TokenCacheErrorMessage.CannotUseDistributedCache + " DoNotUseDistCache", ex3.Message);
            Assert.Equal(0, _testCacheAdapter._memoryCache.Count);
        }

        private static void AssertCacheValues(TestMsalDistributedTokenCacheAdapter testCache)
        {
            Assert.NotNull(testCache);
            Assert.NotNull(testCache._distributedCache);
            Assert.NotNull(testCache._memoryCache);
        }

        private void BuildTheRequiredServices()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddDistributedTokenCaches();
            _provider = services.BuildServiceProvider();
        }

        private static IDistributedCache MakeMockDistributedCache()
        {
            return new TestDistributedCache();
        }

        private static int GetFailedRemovalCount(TestMsalDistributedTokenCacheAdapter adapter)
        {
            FieldInfo field = typeof(MsalDistributedTokenCacheAdapter).GetField(
                "_failedL2CacheRemovals",
                BindingFlags.Instance | BindingFlags.NonPublic)!;
            return ((ConcurrentDictionary<string, object>)field.GetValue(adapter)!).Count;
        }

        private static async Task WaitForFailedRemovalCountAsync(
            TestMsalDistributedTokenCacheAdapter adapter,
            int expectedCount)
        {
            for (int attempt = 0; attempt < 1000; attempt++)
            {
                if (GetFailedRemovalCount(adapter) == expectedCount)
                {
                    return;
                }

                await Task.Yield();
            }

            Assert.Equal(expectedCount, GetFailedRemovalCount(adapter));
        }

        private static (
            TestMsalDistributedTokenCacheAdapter Adapter,
            TestDistributedCache Cache,
            MsalDistributedTokenCacheAdapterOptions Options) CreateAdapter(bool disableL1Cache = false)
        {
            TestDistributedCache cache = new TestDistributedCache();
            MsalDistributedTokenCacheAdapterOptions options = new MsalDistributedTokenCacheAdapterOptions
            {
                DisableL1Cache = disableL1Cache,
            };
            ServiceProvider provider = new ServiceCollection().AddLogging().BuildServiceProvider();
            TestMsalDistributedTokenCacheAdapter adapter = new TestMsalDistributedTokenCacheAdapter(
                cache,
                Microsoft.Extensions.Options.Options.Create(options),
                provider.GetRequiredService<ILogger<MsalDistributedTokenCacheAdapter>>());
            return (adapter, cache, options);
        }
    }
}
