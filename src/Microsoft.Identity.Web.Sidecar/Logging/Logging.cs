// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Web.Sidecar.Logging;

public static partial class LoggerMessageExtensions
{
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Error,
        Message = "An error occurred while creating an authorization header.",
        EventName = "AuthorizationHeaderAsyncError_CreateAuthorizationHeaderAsync")]
    public static partial void AuthorizationHeaderAsyncError(this ILogger logger, Exception exception);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Error,
        Message = "An error occurred while parsing the token.",
        EventName = "ValidateRequest_UnableToParseToken")]
    public static partial void UnableToParseToken(this ILogger logger, Exception? exception);

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Warning,
        Message = "Caller-supplied 'optionsOverride.*' parameters were ignored on route '{RouteName}' because overrides are not allowed for it by configuration.",
        EventName = "OverridesIgnored")]
    public static partial void OverridesIgnored(this ILogger logger, string routeName);

    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Warning,
        Message = "Caller-supplied 'optionsOverride.BaseUrl' was ignored. The downstream BaseUrl is fixed by the host configuration and cannot be overridden by the caller.",
        EventName = "BaseUrlOverrideIgnored")]
    public static partial void BaseUrlOverrideIgnored(this ILogger logger);
}
