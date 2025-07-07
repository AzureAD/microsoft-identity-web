// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.Identity.Web.OidcFic
{
    // Log messages for OidcIdpSignedAssertionProvider
    internal partial class OidcIdpSignedAssertionProvider
    {
        /// <summary>
        /// Performant logging messages.
        /// </summary>
        private static class Logger
        {
            private static readonly Action<ILogger, Exception?> s_postponingToFirstCall =
                LoggerMessage.Define(
                    LogLevel.Debug,
                    new EventId(1, nameof(PostponingToFirstCall)),
                    "OidcIdpSignedAssertionProvider: RequiresSignedAssertionFmiPath is true, but assertionRequestOptions is null. Postponing to first call"
                );

            public static void PostponingToFirstCall(ILogger? logger)
            {
                if (logger != null && logger.IsEnabled(LogLevel.Debug))
                {
                    s_postponingToFirstCall(logger, null);
                }
            }

            private static readonly Action<ILogger, string, string?, Exception?> s_acquiringToken =
                LoggerMessage.Define<string, string?>(
                    LogLevel.Debug,
                    new EventId(2, nameof(AcquiringToken)),
                    "OidcIdpSignedAssertionProvider: Acquiring token for {tokenExchangeUrl} with FmiPath: {fmiPath}"
                );

            public static void AcquiringToken(ILogger? logger, string tokenExchangeUrl, string? fmiPath)
            {
                if (logger != null && logger.IsEnabled(LogLevel.Debug))
                {
                    s_acquiringToken(logger, tokenExchangeUrl, fmiPath, null);
                }
            }

            private static readonly Action<ILogger, string?, Exception?> s_acquiredToken =
                LoggerMessage.Define<string?>(
                    LogLevel.Debug,
                    new EventId(3, nameof(AcquiredToken)),
                    "OidcIdpSignedAssertionProvider: Acquired token for with FmiPath: {fmiPath}"
                );

            public static void AcquiredToken(ILogger? logger, string? fmiPath)
            {
                if (logger != null && logger.IsEnabled(LogLevel.Debug))
                {
                    s_acquiredToken(logger, fmiPath, null);
                }
            }
        }
    }
}