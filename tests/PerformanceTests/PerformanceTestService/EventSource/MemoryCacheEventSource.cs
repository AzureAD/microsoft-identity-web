// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Diagnostics.Tracing;
using System.Threading;

namespace PerformanceTestService
{
    /// <summary>
    /// Enables sending in-memory cache related counters.
    /// </summary>
    /// <remarks>
    /// https://github.com/dotnet/aspnetcore/blob/master/src/Hosting/Hosting/src/Internal/HostingEventSource.cs
    /// </remarks>
    internal sealed class MemoryCacheEventSource : System.Diagnostics.Tracing.EventSource
    {
        public static readonly MemoryCacheEventSource Log = new MemoryCacheEventSource();

        public const string EventSourceName = "Microsoft.Identity.Web.Caching";
        public const string CacheItemCounterName = "cache-item-count";
        public const string CacheWriteCounterName = "cache-write-count";
        public const string CacheReadCounterName = "cache-read-count";
        public const string CacheReadMissCounterName = "cache-read-miss-count";
        public const string CacheRemoveCounterName = "cache-remove-count";
        public const string CacheSizeCounterName = "cache-size";
        public const string CacheReadDurationCounterName = "cache-read-duration";
        public const string CacheWriteDurationCounterName = "cache-write-duration";

        private PollingCounter _cacheItemCounter;
        private PollingCounter _cacheWriteCounter;
        private PollingCounter _cacheReadCounter;
        private PollingCounter _cacheReadMissCounter;
        private PollingCounter _cacheRemoveCounter;
        private PollingCounter _cacheSizeCounter;
        private EventCounter _cacheReadDurationCounter;
        private EventCounter _cacheWriteDurationCounter;

        private long _totalItemCount;
        private long _writeCount;
        private long _readCount;
        private long _readMissCount;
        private long _removeCount;
        private long _cacheSizeInBytes;

        internal MemoryCacheEventSource()
            : this(EventSourceName)
        {
        }

        internal MemoryCacheEventSource(string eventSourceName)
            : base(eventSourceName)
        {
        }

        public void IncrementWriteCount()
        {
            Interlocked.Increment(ref _writeCount);
            Interlocked.Increment(ref _totalItemCount);
        }

        public void IncrementReadCount()
        {
            Interlocked.Increment(ref _readCount);
        }

        public void IncrementReadMissCount()
        {
            Interlocked.Increment(ref _readMissCount);
        }

        public void IncrementRemoveCount()
        {
            Interlocked.Increment(ref _removeCount);
            Interlocked.Decrement(ref _totalItemCount);
        }
        
        public void IncrementSize(int sizeInBytes)
        {
            Interlocked.Add(ref _cacheSizeInBytes, sizeInBytes);
        }

        public void DecrementSize(int sizeInBytes)
        {
            Interlocked.Add(ref _cacheSizeInBytes, sizeInBytes * -1);
        }

        public void AddReadDuration(double readDurationInMilliseconds)
        {
            _cacheReadDurationCounter.WriteMetric(readDurationInMilliseconds);
        }

        public void AddWriteDuration(double writeDurationInMilliseconds)
        {
            _cacheWriteDurationCounter.WriteMetric(writeDurationInMilliseconds);
        }

        /// <remarks>
        /// This is the convention for initializing counters in the RuntimeEventSource (lazily on the first enable command).
        /// They aren't disabled afterwards
        /// </remarks>
        protected override void OnEventCommand(EventCommandEventArgs command)
        {
            if (command.Command == EventCommand.Enable)
            {
                _cacheItemCounter ??= new PollingCounter(CacheItemCounterName, this, () => Interlocked.Read(ref _totalItemCount))
                {
                    DisplayName = "Total cache items (Write - Remove)"
                };
                _cacheWriteCounter ??= new PollingCounter(CacheWriteCounterName, this, () => Interlocked.Read(ref _writeCount))
                {
                    DisplayName = "Total cache write calls"
                };
                _cacheReadCounter ??= new PollingCounter(CacheReadCounterName, this, () => Interlocked.Read(ref _readCount))
                {
                    DisplayName = "Total cache read calls"
                };
                _cacheReadMissCounter ??= new PollingCounter(CacheReadMissCounterName, this, () => Interlocked.Read(ref _readMissCount))
                {
                    DisplayName = "Total cache read misses"
                };
                _cacheRemoveCounter ??= new PollingCounter(CacheRemoveCounterName, this, () => Interlocked.Read(ref _removeCount))
                {
                    DisplayName = "Total cache remove calls"
                };
                _cacheSizeCounter ??= new PollingCounter(CacheSizeCounterName, this, () => Interlocked.Read(ref _cacheSizeInBytes))
                {
                    DisplayName = "Total cache size",
                    DisplayUnits = "B"
                };
                _cacheReadDurationCounter ??= new EventCounter(CacheReadDurationCounterName, this)
                {
                    DisplayName = "Cache read duration",
                    DisplayUnits = "ms"
                };
                _cacheWriteDurationCounter ??= new EventCounter(CacheWriteDurationCounterName, this)
                {
                    DisplayName = "Cache write duration",
                    DisplayUnits = "ms"
                };
            }
        }
    }
}
