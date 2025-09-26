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
        /// Logging infrastructure for OidcIdpSignedAssertionProvider.
        /// </summary>
        private static class Logger
        {
            private static readonly Action<ILogger, Exception?> s_postponingSignedAssertionAcquisition =
                LoggerMessage.Define(
                    LogLevel.Debug,
                    new EventId(1, nameof(PostponingSignedAssertionAcquisition)),
                    "OidcIdpSignedAssertionProvider: RequiresSignedAssertionFmiPath is true, but assertionRequestOptions is null. Postponing to first call"
                );

            private static readonly Action<ILogger, string, string?, Exception?> s_acquiringTokenForTokenExchange =
                LoggerMessage.Define<string, string?>(
                    LogLevel.Debug,
                    new EventId(2, nameof(AcquiringTokenForTokenExchange)),
                    "OidcIdpSignedAssertionProvider: Acquiring token for {TokenExchangeUrl} with FmiPath: {FmiPath}"
                );

            private static readonly Action<ILogger, string?, Exception?> s_acquiredTokenForTokenExchange =
                LoggerMessage.Define<string?>(
                    LogLevel.Debug,
                    new EventId(3, nameof(AcquiredTokenForTokenExchange)),
                    "OidcIdpSignedAssertionProvider: Acquired token for with FmiPath: {FmiPath}"
                );

            /// <summary>
            /// Log when RequiresSignedAssertionFmiPath is true but assertionRequestOptions is null.
            /// </summary>
            /// <param name="logger">ILogger.</param>
            public static void PostponingSignedAssertionAcquisition(ILogger logger)
                => s_postponingSignedAssertionAcquisition(logger, default!);

            /// <summary>
            /// Log when acquiring token for token exchange.
            /// </summary>
            /// <param name="logger">ILogger.</param>
            /// <param name="tokenExchangeUrl">Token exchange URL.</param>
            /// <param name="fmiPath">FMI path if available.</param>
            public static void AcquiringTokenForTokenExchange(ILogger logger, string tokenExchangeUrl, string? fmiPath)
                => s_acquiringTokenForTokenExchange(logger, tokenExchangeUrl, fmiPath, default!);

            /// <summary>
            /// Log when token acquisition is completed.
            /// </summary>
            /// <param name="logger">ILogger.</param>
            /// <param name="fmiPath">FMI path if available.</param>
            public static void AcquiredTokenForTokenExchange(ILogger logger, string? fmiPath)
                => s_acquiredTokenForTokenExchange(logger, fmiPath, default!);
        }
    }
}