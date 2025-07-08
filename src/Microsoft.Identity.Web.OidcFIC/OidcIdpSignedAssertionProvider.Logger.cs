// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.Identity.Web.OidcFic
{
    /// <summary>
    /// High-performance logger extensions for OidcIdpSignedAssertionProvider.
    /// </summary>
    internal static partial class OidcIdpSignedAssertionProviderLoggerExtensions
    {
        [LoggerMessage(
            EventId = 1,
            Level = LogLevel.Debug,
            Message = "OidcIdpSignedAssertionProvider: RequiresSignedAssertionFmiPath is true, but assertionRequestOptions is null. Postponing to first call")]
        public static partial void PostponingToFirstCall(this ILogger? logger);

        [LoggerMessage(
            EventId = 2,
            Level = LogLevel.Debug,
            Message = "OidcIdpSignedAssertionProvider: Acquiring token for {tokenExchangeUrl} with FmiPath: {fmiPath}")]
        public static partial void AcquiringToken(this ILogger? logger, string tokenExchangeUrl, string? fmiPath);

        [LoggerMessage(
            EventId = 3,
            Level = LogLevel.Debug,
            Message = "OidcIdpSignedAssertionProvider: Acquired token for with FmiPath: {fmiPath}")]
        public static partial void AcquiredToken(this ILogger? logger, string? fmiPath);
    }
}