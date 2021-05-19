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
        private static class Logger
        {
            private static readonly Action<ILogger, string, Exception?> s_tokenAcquisitionError =
                LoggerMessage.Define<string>(
                    LogLevel.Information,
                    LoggingEventId.TokenAcquisitionError,
                    "[MsIdWeb] An error occured during token acquisition: {MsalErrorMessage}");

            private static readonly Action<ILogger, long, long, long, Exception?> s_tokenAcquisitionMsalAuthenticationResultTime =
                LoggerMessage.Define<long, long, long>(
                    LogLevel.Debug,
                    LoggingEventId.TokenAcquisitionMsalAuthenticationResultTime,
                    "[MsIdWeb] Time to get token with MSAL: DurationTotalInMs: {DurationTotalInMs} DurationInHttpInMs: {DurationInHttpInMs} DurationInCacheInMs: {DurationInCacheInMs} ");

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
            /// <param name="ex">Exception from MSAL.NET.</param>
            public static void TokenAcquisitionMsalAuthenticationResultTime(
                ILogger logger,
                long durationTotalInMs,
                long durationInHttpInMs,
                long durationInCacheInMs,
                Exception? ex) => s_tokenAcquisitionMsalAuthenticationResultTime(
                    logger,
                    durationTotalInMs,
                    durationInHttpInMs,
                    durationInCacheInMs,
                    ex);
        }
    }
}
