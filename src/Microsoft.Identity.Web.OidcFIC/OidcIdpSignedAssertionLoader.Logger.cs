// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.Identity.Web.OidcFic
{
    // Log messages for OidcIdpSignedAssertionLoader
    internal partial class OidcIdpSignedAssertionLoader
    {
        /// <summary>
        /// Performant logging messages.
        /// </summary>
        private static class Logger
        {
            private static readonly Action<ILogger, Exception?> s_customSignedAssertionProviderDataIsNull =
                LoggerMessage.Define(
                    LogLevel.Error,
                    new EventId(1, nameof(CustomSignedAssertionProviderDataIsNull)),
                    "CustomSignedAssertionProviderData is null"
                );

            public static void CustomSignedAssertionProviderDataIsNull(ILogger? logger)
            {
                if (logger != null && logger.IsEnabled(LogLevel.Error))
                {
                    s_customSignedAssertionProviderDataIsNull(logger, null);
                }
            }

            private static readonly Action<ILogger, Exception?> s_configurationSectionIsNull =
                LoggerMessage.Define(
                    LogLevel.Error,
                    new EventId(2, nameof(ConfigurationSectionIsNull)),
                    "ConfigurationSection is null"
                );

            public static void ConfigurationSectionIsNull(ILogger? logger)
            {
                if (logger != null && logger.IsEnabled(LogLevel.Error))
                {
                    s_configurationSectionIsNull(logger, null);
                }
            }

            private static readonly Action<ILogger, string?, string, Exception?> s_failedToGetSignedAssertion =
                LoggerMessage.Define<string?, string>(
                    LogLevel.Error,
                    new EventId(3, nameof(FailedToGetSignedAssertion)),
                    "Failed to get signed assertion from {ProviderName}. exception occurred: {Message}. Setting skip to true."
                );

            public static void FailedToGetSignedAssertion(ILogger? logger, string? providerName, string message, Exception? ex)
            {
                if (logger != null && logger.IsEnabled(LogLevel.Error))
                {
                    s_failedToGetSignedAssertion(logger, providerName, message, ex);
                }
            }
        }
    }
}