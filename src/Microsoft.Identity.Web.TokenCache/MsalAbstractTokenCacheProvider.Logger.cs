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
            private static readonly Action<ILogger, string, bool, string, Exception> s_cacheDeserializationError =
                LoggerMessage.Define<string, bool, string>(
                    LogLevel.Warning,
                    LoggingEventId.DistributedCacheConnectionError,
                    "[MsIdWeb] Unable to deserialize cache entry. Cache key : {CacheKey}. Encryption enabled: {EncryptionEnabled}. Error message: {ErrorMessage} ");

            /// <summary>
            /// Cache deserialization error.
            /// </summary>
            /// <param name="logger">ILogger.</param>
            /// <param name="cacheKey">MSAL.NET cache key.</param>
            /// <param name="encryptionEnabled">Whether cache is encrypted or not.</param>
            /// <param name="errorMessage">Error message.</param>
            /// <param name="ex">Exception.</param>
            public static void CacheDeserializationError(
                ILogger logger,
                string cacheKey,
                bool encryptionEnabled,
                string errorMessage,
                Exception ex) => s_cacheDeserializationError(
                    logger,
                    cacheKey,
                    encryptionEnabled,
                    errorMessage,
                    ex);
        }
    }
}
