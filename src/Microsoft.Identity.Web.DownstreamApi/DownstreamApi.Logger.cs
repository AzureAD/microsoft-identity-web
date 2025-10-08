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
        }
    }
}
