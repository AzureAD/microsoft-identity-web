// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// LoggingMessage class for TokenAcquisition.
    /// </summary>
    internal partial class TokenAcquisition
    {
        internal static class Logger
        {
            private static readonly Action<ILogger, string, Exception?> s_tokenAcquisitionError =
                LoggerMessage.Define<string>(
                    LogLevel.Information,
                    LoggingEventId.TokenAcquisitionError,
                    "[MsIdWeb] An error occured during token acquisition: {0}");

            private static readonly Action<ILogger, long, long, long, string, string, string, Exception?> s_tokenAcquisitionMsalAuthenticationResultTime =
                LoggerMessage.Define<long, long, long, string, string, string>(
                    LogLevel.Debug,
                    LoggingEventId.TokenAcquisitionMsalAuthenticationResultTime,
                    "[MsIdWeb] Time to get token with MSAL: " +
                    "DurationTotalInMs: {0} " +
                    "DurationInHttpInMs: {1} " +
                    "DurationInCacheInMs: {2} " +
                    "TokenSource: {3} " +
                    "CorrelationId: {4} " +
                    "CacheRefreshReason: {5} ");

            /// <summary>
            /// Logger for handling MSAL exceptions in TokenAcquisition.
            /// </summary>
            /// <param name="logger">ILogger.</param>
            /// <param name="msalErrorMessage">Specific log message from TokenAcquisition.</param>
            /// <param name="ex">Exception from MSAL.NET.</param>
            public static void TokenAcquisitionError(
                ILogger logger,
                string msalErrorMessage,
                Exception? ex) => s_tokenAcquisitionError(logger, msalErrorMessage, ex);

            /// <summary>
            /// Logger for handling information specific to MSAL in token acquisition.
            /// </summary>
            /// <param name="logger">ILogger.</param>
            /// <param name="durationTotalInMs">durationTotalInMs.</param>
            /// <param name="durationInHttpInMs">durationInHttpInMs.</param>
            /// <param name="durationInCacheInMs">durationInCacheInMs.</param>
            /// <param name="tokenSource">cache or IDP.</param>
            /// <param name="correlationId">correlationId.</param>
            /// <param name="cacheRefreshReason">cacheRefreshReason.</param>
            /// <param name="ex">Exception from MSAL.NET.</param>
            public static void TokenAcquisitionMsalAuthenticationResultTime(
                ILogger logger,
                long durationTotalInMs,
                long durationInHttpInMs,
                long durationInCacheInMs,
                string tokenSource,
                string correlationId,
                string cacheRefreshReason,
                Exception? ex) => s_tokenAcquisitionMsalAuthenticationResultTime(
                    logger,
                    durationTotalInMs,
                    durationInHttpInMs,
                    durationInCacheInMs,
                    tokenSource,
                    correlationId,
                    cacheRefreshReason,
                    ex);
        }
    }
}
