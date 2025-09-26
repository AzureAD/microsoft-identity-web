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
}
