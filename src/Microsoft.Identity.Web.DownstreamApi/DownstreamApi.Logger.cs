// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections;
using Microsoft.Extensions.Logging;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// LoggerMessage class for DownstreamApi
    /// </summary>
    internal partial class DownstreamApi
    {
        internal static class Logger
        {
            private static readonly Action<ILogger, string, string, Exception?> s_effectiveOptionsError =
                LoggerMessage.Define<string, string>(
                    LogLevel.Debug, 
                    DownstreamApiLoggingEventId.EffectiveOptionsError, 
                    "[MsIdWeb] An error occurred during Get. " + 
                    "BaseUrl: {BaseUrl} " +
                    "RelativePath: {RelativePath} ");

            private static readonly Action<ILogger, Exception?> s_unauthenticatedApiCall =
                LoggerMessage.Define(
                    LogLevel.Information,
                    DownstreamApiLoggingEventId.UnauthenticatedApiCall, 
                    "[MsIdWeb] An unauthenticated call was made to the Api with null Scopes");


            /// <summary>
            /// Logger for handling options exceptions in DownstreamApi
            /// </summary>
            /// <param name="logger">ILogger</param>
            /// <param name="BaseUrl">Base url from appsettings.</param>
            /// <param name="RelativePath">Relative path from appsettings.</param>
            /// <param name="ex">Exception</param>
            public static void EffectiveOptionsError(
                ILogger logger, 
                string BaseUrl, 
                string RelativePath, 
                Exception? ex) => s_effectiveOptionsError(logger, BaseUrl, RelativePath, ex);

            /// <summary>
            /// Logger for unauthenticated internal Api call in DownstreamApi
            /// </summary>
            /// <param name="logger">Logger</param>
            /// <param name="ex">Exception</param>
            public static void UnauthenticatedApiCall(
                ILogger logger,
                Exception? ex) => s_unauthenticatedApiCall(logger, ex);
        }
    }
}
