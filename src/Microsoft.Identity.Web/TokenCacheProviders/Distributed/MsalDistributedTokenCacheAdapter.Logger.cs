// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.Identity.Web.TokenCacheProviders.Distributed
{
    /// <summary>
    /// LoggingMessage class for MsalDistributedTokenCacheAdapter.
    /// </summary>
    public partial class MsalDistributedTokenCacheAdapter
    {
        /// <summary>
        /// LoggingMessage class for MsalDistributedTokenCacheAdapter.
        /// </summary>
        private static class Logger
        {
            private static readonly Action<ILogger, string, string, string, int, bool, Exception?> s_l2CacheState =
                LoggerMessage.Define<string, string, string, int, bool>(
                    LogLevel.Debug,
                    LoggingEventId.DistributedCacheState,
                    "[MsIdWeb] {CacheType}: {Operation} cacheKey {CacheKey} cache size {Size} InRetry? {InRetry} ");

            private static readonly Action<ILogger, string, string, string, int, bool, double, Exception?> s_l2CacheStateWithTime =
                LoggerMessage.Define<string, string, string, int, bool, double>(
                    LogLevel.Debug,
                    LoggingEventId.DistributedCacheStateWithTime,
                    "[MsIdWeb] {CacheType}: {Operation} cacheKey {CacheKey} cache size {Size} InRetry? {InRetry} Time in MilliSeconds: {Time} ");

            private static readonly Action<ILogger, string, string, double, Exception?> s_l2CacheReadTime =
                LoggerMessage.Define<string, string, double>(
                    LogLevel.Debug,
                    LoggingEventId.DistributedCacheReadTime,
                    "[MsIdWeb] {CacheType}: {Operation} Time in MilliSeconds {Time} ");

            private static readonly Action<ILogger, string, string, bool, string, Exception> s_l2CacheConnectionError =
                LoggerMessage.Define<string, string, bool, string>(
                    LogLevel.Error,
                    LoggingEventId.DistributedCacheConnectionError,
                    "[MsIdWeb] {CacheType}: {Operation} Connection issue. InRetry? {InRetry} Error message: {ErrorMessage} ");

            private static readonly Action<ILogger, string, string, string, Exception?> s_l2CacheRetry =
                LoggerMessage.Define<string, string, string>(
                    LogLevel.Debug,
                    LoggingEventId.DistributedCacheRetry,
                    "[MsIdWeb] {CacheType}: Retrying {Operation} cacheKey {CacheKey} ");

            private static readonly Action<ILogger, string, string, string, Exception?> s_l1CacheRemove =
                LoggerMessage.Define<string, string, string>(
                    LogLevel.Debug,
                    LoggingEventId.MemoryCacheRemove,
                    "[MsIdWeb] {CacheType}: {Operation} cacheKey {CacheKey} ");

            private static readonly Action<ILogger, string, string, string, int, Exception?> s_l1CacheRead =
                LoggerMessage.Define<string, string, string, int>(
                    LogLevel.Debug,
                    LoggingEventId.MemoryCacheRead,
                    "[MsIdWeb] {CacheType}: {Operation} cacheKey {CacheKey} cache size {Size} ");

            private static readonly Action<ILogger, string, string, Exception?> s_l1CacheNegativeExpiry =
                LoggerMessage.Define<string, string>(
                    LogLevel.Debug,
                    LoggingEventId.MemoryCacheNegativeExpiry,
                    "[MsIdWeb] {CacheType}: {Operation} The SuggestedCacheExpiry from MSAL was negative. ");

            private static readonly Action<ILogger, string, string, int, Exception?> s_l1CacheCount =
                LoggerMessage.Define<string, string, int>(
                    LogLevel.Debug,
                    LoggingEventId.MemoryCacheCount,
                    "[MsIdWeb] {CacheType}: {Operation} Count: {Count} ");

            private static readonly Action<ILogger, long, Exception?> s_backPropagateL2toL1 =
                LoggerMessage.Define<long>(
                    LogLevel.Debug,
                    LoggingEventId.BackPropagateL2toL1,
                    "[MsIdWeb] Back propagate from Distributed to Memory, cache size {Size} ");

            /// <summary>
            /// Memory cache read.
            /// </summary>
            /// <param name="logger">ILogger.</param>
            /// <param name="cacheType">Distributed or Memory.</param>
            /// <param name="operation">Cache operation (Read, Write, etc...).</param>
            /// <param name="cacheKey">MSAL.NET cache key.</param>
            /// <param name="cacheSize">Cache size in bytes, or 0 if empty.</param>
            /// <param name="ex">Exception.</param>
            public static void MemoryCacheRead(
                ILogger logger,
                string cacheType,
                string operation,
                string cacheKey,
                int cacheSize,
                Exception? ex) => s_l1CacheRead(
                    logger,
                    cacheType,
                    operation,
                    cacheKey,
                    cacheSize,
                    ex);

            /// <summary>
            /// Memory cache negative expiry.
            /// </summary>
            /// <param name="logger">ILogger.</param>
            /// <param name="cacheType">Distributed or Memory.</param>
            /// <param name="operation">Cache operation (Read, Write, etc...).</param>
            /// <param name="ex">Exception.</param>
            public static void MemoryCacheNegativeExpiry(
                ILogger logger,
                string cacheType,
                string operation,
                Exception? ex) => s_l1CacheNegativeExpiry(
                    logger,
                    cacheType,
                    operation,
                    ex);

            /// <summary>
            /// Memory cache remove.
            /// </summary>
            /// <param name="logger">ILogger.</param>
            /// <param name="cacheType">Distributed or Memory.</param>
            /// <param name="operation">Cache operation (Read, Write, etc...).</param>
            /// <param name="cacheKey">MSAL.NET cache key.</param>
            /// <param name="ex">Exception.</param>
            public static void MemoryCacheRemove(
                ILogger logger,
                string cacheType,
                string operation,
                string cacheKey,
                Exception? ex) => s_l1CacheRemove(
                    logger,
                    cacheType,
                    operation,
                    cacheKey,
                    ex);

            /// <summary>
            /// Memory cache count.
            /// </summary>
            /// <param name="logger">ILogger.</param>
            /// <param name="cacheType">Distributed or Memory.</param>
            /// <param name="operation">Cache operation (Read, Write, etc...).</param>
            /// <param name="count">L1 cache count.</param>
            /// <param name="ex">Exception.</param>
            public static void MemoryCacheCount(
                ILogger logger,
                string cacheType,
                string operation,
                int count,
                Exception? ex) => s_l1CacheCount(
                    logger,
                    cacheType,
                    operation,
                    count,
                    ex);

            /// <summary>
            /// L2 cache state logging.
            /// </summary>
            /// <param name="logger">ILogger.</param>
            /// <param name="cacheType">Distributed or Memory.</param>
            /// <param name="operation">Cache operation (Read, Write, etc...).</param>
            /// <param name="cacheKey">MSAL.NET cache key.</param>
            /// <param name="cacheSize">Cache size in bytes, or 0 if empty.</param>
            /// <param name="inRetry">L2 cache retry due to possible connection issue.</param>
            /// <param name="ex">Exception.</param>
            public static void DistributedCacheState(
                ILogger logger,
                string cacheType,
                string operation,
                string cacheKey,
                int cacheSize,
                bool inRetry,
                Exception? ex) => s_l2CacheState(
                    logger,
                    cacheType,
                    operation,
                    cacheKey,
                    cacheSize,
                    inRetry,
                    ex);

            /// <summary>
            /// L2 cache state logging.
            /// </summary>
            /// <param name="logger">ILogger.</param>
            /// <param name="cacheType">Distributed or Memory.</param>
            /// <param name="operation">Cache operation (Read, Write, etc...).</param>
            /// <param name="cacheKey">MSAL.NET cache key.</param>
            /// <param name="cacheSize">Cache size in bytes, or 0 if empty.</param>
            /// <param name="inRetry">L2 cache retry due to possible connection issue.</param>
            /// <param name="time">Time in milliseconds.</param>
            /// <param name="ex">Exception.</param>
            public static void DistributedCacheStateWithTime(
                ILogger logger,
                string cacheType,
                string operation,
                string cacheKey,
                int cacheSize,
                bool inRetry,
                double time,
                Exception? ex) => s_l2CacheStateWithTime(
                    logger,
                    cacheType,
                    operation,
                    cacheKey,
                    cacheSize,
                    inRetry,
                    time,
                    ex);

            /// <summary>
            /// L2 cache retry.
            /// </summary>
            /// <param name="logger">ILogger.</param>
            /// <param name="cacheType">Distributed or Memory.</param>
            /// <param name="operation">Cache operation (Read, Write, etc...).</param>
            /// <param name="cacheKey">MSAL.NET cache key.</param>
            /// <param name="ex">Exception.</param>
            public static void DistributedCacheRetry(
                ILogger logger,
                string cacheType,
                string operation,
                string cacheKey,
                Exception? ex) => s_l2CacheRetry(
                    logger,
                    cacheType,
                    operation,
                    cacheKey,
                    ex);

            /// <summary>
            /// L2 cache retry.
            /// </summary>
            /// <param name="logger">ILogger.</param>
            /// <param name="cacheType">Distributed or Memory.</param>
            /// <param name="operation">Cache operation (Read, Write, etc...).</param>
            /// <param name="time">Time in milliseconds.</param>
            /// <param name="ex">Exception.</param>
            public static void DistributedCacheReadTime(
                ILogger logger,
                string cacheType,
                string operation,
                double time,
                Exception? ex) => s_l2CacheReadTime(
                    logger,
                    cacheType,
                    operation,
                    time,
                    ex);

            /// <summary>
            /// L2 cache error.
            /// </summary>
            /// <param name="logger">ILogger.</param>
            /// <param name="cacheType">Distributed or Memory.</param>
            /// <param name="operation">Cache operation (Read, Write, etc...).</param>
            /// <param name="inRetry">L2 cache retry due to possible connection issue.</param>
            /// <param name="errorMessage">Error message.</param>
            /// <param name="ex">Exception.</param>
            public static void DistributedCacheConnectionError(
                ILogger logger,
                string cacheType,
                string operation,
                bool inRetry,
                string errorMessage,
                Exception ex) => s_l2CacheConnectionError(
                    logger,
                    cacheType,
                    operation,
                    inRetry,
                    errorMessage,
                    ex);

            /// <summary>
            /// Back propagate L2 to L1.
            /// </summary>
            /// <param name="logger">ILogger.</param>
            /// <param name="cacheSize">Cache size in bytes, or 0 if empty.</param>
            /// <param name="ex">Exception.</param>
            public static void BackPropagateL2toL1(
                ILogger logger,
                long cacheSize,
                Exception? ex) => s_backPropagateL2toL1(
                    logger,
                    cacheSize,
                    ex);
        }
    }
}
