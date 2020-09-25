using System.Diagnostics.Tracing;
using System.Threading;

namespace IntegrationTestService
{
    /// <summary>
    /// Enables sending in-memory cache related counters.
    /// </summary>
    /// <remarks>
    /// https://github.com/dotnet/aspnetcore/blob/master/src/Hosting/Hosting/src/Internal/HostingEventSource.cs
    /// </remarks>
    internal sealed class MemoryCacheEventSource : EventSource
    {
        public static readonly MemoryCacheEventSource Log = new MemoryCacheEventSource();

        private PollingCounter _cacheItemCounter;
        private PollingCounter _cacheWriteCounter;
        private PollingCounter _cacheReadCounter;
        private PollingCounter _cacheRemoveCounter;

        private int _totalItemCount;
        private int _writeCount;
        private int _readCount;
        private int _removeCount;


        internal MemoryCacheEventSource()
            : this("Microsoft.Identity.Web.MemoryCache")
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

        public void IncrementRemoveCount()
        {
            Interlocked.Increment(ref _removeCount);
            Interlocked.Decrement(ref _totalItemCount);
        }

        protected override void OnEventCommand(EventCommandEventArgs command)
        {
            if (command.Command == EventCommand.Enable)
            {
                _cacheItemCounter ??= new PollingCounter("cache-item-count", this, () => _totalItemCount)
                {
                    DisplayName = "Total cache items (Write - Remove)"
                };
                _cacheWriteCounter ??= new PollingCounter("cache-write-count", this, () => _writeCount)
                {
                    DisplayName = "Total cache write calls"
                };
                _cacheReadCounter ??= new PollingCounter("cache-read-count", this, () => _readCount)
                {
                    DisplayName = "Total cache read calls"
                };
                _cacheRemoveCounter ??= new PollingCounter("cache-remove-count", this, () => _removeCount)
                {
                    DisplayName = "Total cache remove calls"
                };
            }
        }
    }
}
