// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.Identity.Web
{
    internal partial class ConfidentialClientApplicationBuilderExtension
    {
        internal static class Logger
        {
            private static readonly Action<ILogger, string, Exception?> s_notManagedIdentity =
                LoggerMessage.Define<string>(
                    LogLevel.Information,
                    LoggingEventId.NotUsingManagedIdentity,
                    "[MsIdWeb] Not using Managed identity for client credentials: {ErrorMessage}. ");

            private static readonly Action<ILogger, Exception?> s_usingManagedIdentity =
                LoggerMessage.Define(
                    LogLevel.Information,
                    LoggingEventId.UsingManagedIdentity,
                    "[MsIdWeb] Using Managed identity for client credentials. ");

            private static readonly Action<ILogger, string, Exception?> s_usingPodIdentityFile =
                LoggerMessage.Define<string>(
                    LogLevel.Information,
                    LoggingEventId.UsingPodIdentityFile,
                    "[MsIdWeb] Using Pod identity file {signedAssertionFileDiskPath} as client credentials. ");

            private static readonly Action<ILogger, string, Exception?> s_usingCertThumbprint =
                LoggerMessage.Define<string>(
                    LogLevel.Information,
                    LoggingEventId.UsingCertThumbprint,
                    "[MsIdWeb] Using certificate Thumbprint={certThumbprint} as client credentials. ");

            /// <summary>
            /// Logger for handling information specific to ConfidentialClientApplicationBuilderExtension.
            /// </summary>
            /// <param name="logger">ILogger.</param>
            /// <param name="message">Exception message.</param>
            public static void NotUsingManagedIdentity(
                ILogger logger,
                string message) => s_notManagedIdentity(logger, message, default!);

            /// <summary>
            /// Logger for handling information specific to ConfidentialClientApplicationBuilderExtension.
            /// </summary>
            /// <param name="logger">ILogger.</param>
            public static void UsingManagedIdentity(
                ILogger logger) => s_usingManagedIdentity(logger, default!);

            /// <summary>
            /// Logger for handling information specific to ConfidentialClientApplicationBuilderExtension.
            /// </summary>
            /// <param name="logger">ILogger.</param>
            /// <param name="signedAssertionFileDiskPath"></param>
            public static void UsingPodIdentityFile(
                ILogger logger,
                string signedAssertionFileDiskPath) => s_usingPodIdentityFile(logger, signedAssertionFileDiskPath, default!);

            /// <summary>
            /// Logger for handling information specific to ConfidentialClientApplicationBuilderExtension.
            /// </summary>
            /// <param name="logger">ILogger.</param>
            /// <param name="certThumbprint"></param>
            public static void UsingCertThumbprint(
                ILogger logger,
                string certThumbprint) => s_usingCertThumbprint(logger, certThumbprint, default!);
        }
    }
}
