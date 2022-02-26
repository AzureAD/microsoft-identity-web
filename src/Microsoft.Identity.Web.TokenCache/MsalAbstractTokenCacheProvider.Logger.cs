// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.Identity.Web.TokenCacheProviders
{
    /// <summary>
    /// LoggingMessage class for MsalAbstractTokenCacheProvider.
    /// </summary>
    public abstract partial class MsalAbstractTokenCacheProvider
    {
        /// <summary>
        /// LoggingMessage class for MsalAbstractTokenCacheProvider.
        /// </summary>
        private static class Logger
        {
            private static readonly Action<ILogger, string, string, Exception> s_cacheDeserializationError =
                LoggerMessage.Define<string, string>(
                    LogLevel.Warning,
                    LoggingEventId.DistributedCacheConnectionError,
                    "[MsIdWeb] Unable to deserialize cache entry. Cache key : {CacheKey} Error message: {ErrorMessage} ");

            /// <summary>
            /// Cache deserialization error.
            /// </summary>
            /// <param name="logger">ILogger.</param>
            /// <param name="cacheKey">MSAL.NET cache key.</param>
            /// <param name="errorMessage">Error message.</param>
            /// <param name="ex">Exception.</param>
            public static void CacheDeserializationError(
                ILogger logger,
                string cacheKey,
                string errorMessage,
                Exception ex) => s_cacheDeserializationError(
                    logger,
                    cacheKey,
                    errorMessage,
                    ex);
        }
    }
}
