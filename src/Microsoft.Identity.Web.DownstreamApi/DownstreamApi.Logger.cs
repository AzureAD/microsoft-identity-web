// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// LoggerMessage class for DownstreamApi.
    /// </summary>
    internal partial class DownstreamApi
    {
        internal static class Logger
        {
            private static readonly Action<ILogger, string, string, string, int, string, Exception?> s_httpRequestError =
                LoggerMessage.Define<string, string, string, int, string>(
                    LogLevel.Debug,
                    DownstreamApiLoggingEventId.HttpRequestError,
                    "[MsIdWeb] An error occurred during HTTP Request. " +
                    "ServiceName: {serviceName}, " +
                    "BaseUrl: {BaseUrl}, " +
                    "RelativePath: {RelativePath}, " +
                    "StatusCode: {statusCode}, " +
                    "ResponseContent: {responseContent}");

            private static readonly Action<ILogger, Exception?> s_unauthenticatedApiCall =
                LoggerMessage.Define(
                    LogLevel.Information,
                    DownstreamApiLoggingEventId.UnauthenticatedApiCall,
                    "[MsIdWeb] An unauthenticated call was made to the Api with null Scopes");

            private static readonly Action<ILogger, string, Exception?> s_reservedHeaderIgnored =
                LoggerMessage.Define<string>(
                    LogLevel.Warning,
                    DownstreamApiLoggingEventId.ReservedHeaderIgnored,
                    "[MsIdWeb] Header '{HeaderName}' supplied through ExtraHeaderParameters was ignored because the name is reserved for the library.");

            private static readonly Action<ILogger, string, Exception?> s_duplicateHeaderIgnored =
                LoggerMessage.Define<string>(
                    LogLevel.Warning,
                    DownstreamApiLoggingEventId.DuplicateHeaderIgnored,
                    "[MsIdWeb] Header '{HeaderName}' supplied through ExtraHeaderParameters was ignored because the request already carries a value for it.");

            /// <summary>
            /// Logger for handling options exceptions in DownstreamApi.
            /// </summary>
            /// <param name="logger">ILogger.</param>
            /// <param name="ServiceName">Name of API receiving request.</param>
            /// <param name="BaseUrl">Base url from appsettings.</param>
            /// <param name="RelativePath">Relative path from appsettings.</param>
            /// <param name="statusCode">HTTP status code from the response.</param>
            /// <param name="responseContent">Error content returned by the downstream API</param>
            /// <param name="ex">Exception.</param>
            public static void HttpRequestError(
                ILogger logger,
                string ServiceName,
                string BaseUrl,
                string RelativePath,
                int statusCode,
                string responseContent,
                Exception? ex) => s_httpRequestError(logger, ServiceName, BaseUrl, RelativePath, statusCode, responseContent, ex);

            /// <summary>
            /// Logger for unauthenticated internal API call in DownstreamApi.
            /// </summary>
            /// <param name="logger">Logger.</param>
            /// <param name="ex">Exception.</param>
            public static void UnauthenticatedApiCall(
                ILogger logger,
                Exception? ex) => s_unauthenticatedApiCall(logger, ex);

            /// <summary>
            /// Logs that an ExtraHeaderParameters entry was skipped because its name is reserved.
            /// </summary>
            /// <param name="logger">Logger.</param>
            /// <param name="headerName">Header name that was ignored.</param>
            public static void ReservedHeaderIgnored(
                ILogger logger,
                string headerName) => s_reservedHeaderIgnored(logger, headerName, null);

            /// <summary>
            /// Logs that an ExtraHeaderParameters entry was skipped because the request already
            /// carries a value for the same header name.
            /// </summary>
            /// <param name="logger">Logger.</param>
            /// <param name="headerName">Header name that was ignored.</param>
            public static void DuplicateHeaderIgnored(
                ILogger logger,
                string headerName) => s_duplicateHeaderIgnored(logger, headerName, null);
        }
    }
}
