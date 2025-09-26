// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.Identity.Web
{
    // Log messages for ManagedIdentityClientAssertion
    public partial class ManagedIdentityClientAssertion
    {
        /// <summary>
        /// Logging infrastructure for ManagedIdentityClientAssertion.
        /// </summary>
        private static class Logger
        {
            private static readonly Action<ILogger, string, Exception?> s_managedIdentityClientAssertionInitialized =
                LoggerMessage.Define<string>(
                    LogLevel.Information,
                    new EventId(1, nameof(ManagedIdentityClientAssertionInitialized)),
                    "ManagedIdentityClientAssertion with tokenExchangeUrl={TokenExchangeUrl}"
                );

            /// <summary>
            /// Log when ManagedIdentityClientAssertion is initialized.
            /// </summary>
            /// <param name="logger">ILogger.</param>
            /// <param name="tokenExchangeUrl">Token exchange URL.</param>
            public static void ManagedIdentityClientAssertionInitialized(ILogger logger, string tokenExchangeUrl)
                => s_managedIdentityClientAssertionInitialized(logger, tokenExchangeUrl, default!);
        }
    }
}