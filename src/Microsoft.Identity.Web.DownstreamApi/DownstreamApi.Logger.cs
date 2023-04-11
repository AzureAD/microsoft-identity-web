// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
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
                    new EventId(400, "EffectiveOptions"), 
                    "[MsIdWeb] An error occurred during Get: " + 
                    "BaseUrl: {BaseUrl} " +
                    "RelativePath: {RelativePath} ");


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
        }
    }
}
