// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.Identity.Web.TokenCacheProviders.Session
{
    /// <summary>
    /// LoggingMessage class for MsalSessionTokenCacheProvider.
    /// </summary>
    public partial class MsalSessionTokenCacheProvider
    {
        /// <summary>
        /// LoggingMessage class for MsalSessionTokenCacheProvider.
        /// </summary>
        private static class Logger
        {
            private static readonly Action<ILogger, string, string, string, Exception?> s_sessionCache =
                LoggerMessage.Define<string, string, string>(
                    LogLevel.Information,
                    LoggingEventId.SessionCache,
                    "[MsIdWeb] {Operation} session {SessionId}, cache key {CacheKey} ");

            private static readonly Action<ILogger, string, string, Exception?> s_sessionCacheKeyNotfound =
                LoggerMessage.Define<string, string>(
                    LogLevel.Information,
                    LoggingEventId.SessionCacheKeyNotFound,
                    "[MsIdWeb] Session cache key {CacheKey} not found in session {SessionId} ");

            /// <summary>
            /// Session cache logging.
            /// </summary>
            /// <param name="logger">ILogger.</param>
            /// /// <param name="operation">Cache operation (Read, Write, etc...).</param>
            /// <param name="sessionId">Session Id.</param>
            /// <param name="cacheKey">MSAL.NET cache key.</param>
            /// <param name="ex">Exception.</param>
            public static void SessionCache(
                ILogger logger,
                string operation,
                string sessionId,
                string cacheKey,
                Exception? ex) => s_sessionCache(
                    logger,
                    operation,
                    sessionId,
                    cacheKey,
                    ex);

            /// <summary>
            /// Session cache deserialized.
            /// </summary>
            /// <param name="logger">ILogger.</param>
            /// <param name="cacheKey">MSAL.NET cache key.</param>
            /// <param name="sessionId">Session Id.</param>
            /// <param name="ex">Exception.</param>
            public static void SessionCacheKeyNotFound(
                ILogger logger,
                string cacheKey,
                string sessionId,
                Exception? ex) => s_sessionCacheKeyNotfound(
                    logger,
                    cacheKey,
                    sessionId,
                    ex);
        }
    }
}
