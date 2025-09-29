// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.Identity.Web.OidcFic;

internal static partial class LoggerExtensions
{

    private static readonly Action<ILogger, Exception?> s_postponingToFirstCall =
        LoggerMessage.Define(
            LogLevel.Debug,
            new EventId(1, nameof(PostponingToFirstCall)),
            "[MsIdWeb] OidcIdpSignedAssertionProvider: RequiresSignedAssertionFmiPath is true, but assertionRequestOptions is null. Postponing to first call");

    private static readonly Action<ILogger, string, string?, Exception?> s_acquiringToken =
        LoggerMessage.Define<string, string?>(
            LogLevel.Debug,
            new EventId(2, nameof(AcquiringToken)),
            "[MsIdWeb] OidcIdpSignedAssertionProvider: Acquiring token for {tokenExchangeUrl} with FmiPath: {fmiPath}");

    private static readonly Action<ILogger, string?, Exception?> s_acquiredToken =
        LoggerMessage.Define<string?>(
            LogLevel.Debug,
            new EventId(3, nameof(AcquiredToken)),
            "[MsIdWeb] OidcIdpSignedAssertionProvider: Acquired token with FmiPath: {fmiPath}");

    /// <summary>
    /// Logger for when RequiresSignedAssertionFmiPath is true but assertionRequestOptions is null.
    /// </summary>
    /// <param name="logger">ILogger.</param>
    public static void PostponingToFirstCall(this ILogger logger) => s_postponingToFirstCall(logger, default!);

    /// <summary>
    /// Logger for when acquiring token.
    /// </summary>
    /// <param name="logger">ILogger.</param>
    /// <param name="tokenExchangeUrl">Token exchange URL.</param>
    /// <param name="fmiPath">FMI path.</param>
    public static void AcquiringToken(this ILogger logger, string tokenExchangeUrl, string? fmiPath) => 
        s_acquiringToken(logger, tokenExchangeUrl, fmiPath, default!);

    /// <summary>
    /// Logger for when token is acquired.
    /// </summary>
    /// <param name="logger">ILogger.</param>
    /// <param name="fmiPath">FMI path.</param>
    public static void AcquiredToken(this ILogger logger, string? fmiPath) => 
        s_acquiredToken(logger, fmiPath, default!);
    
}
