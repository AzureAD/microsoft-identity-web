// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// LoggingMessage class for TokenAcquisition.
    /// </summary>
    internal partial class TokenAcquisition
    {
        private static class Logger
        {
            private static readonly Action<ILogger, string, Exception> s_tokenAcquisitionError =
           LoggerMessage.Define<string>(
               LogLevel.Information,
               new EventId(1, "TokenAcquisitionError"),
               "[MsIdWeb] An error occured during token acquisition: {MsalErrorMessage}");

            /// <summary>
            /// Logger for handling MSAL exceptions in TokenAcquisition.
            /// </summary>
            /// <param name="logger">ILogger.</param>
            /// <param name="msalErrorMessage">Specific log message from TokenAcquisition.</param>
            /// <param name="ex">Exception from MSAL.NET.</param>
            public static void TokenAcquisitionError(
                ILogger logger,
                string msalErrorMessage,
                Exception ex) => s_tokenAcquisitionError(logger, msalErrorMessage, ex);
        }
    }
}
