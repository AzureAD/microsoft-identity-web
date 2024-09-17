// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Abstractions;

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

            private static readonly Action<ILogger, string, Exception?> s_usingSignedAssertionFromVault =
                LoggerMessage.Define<string>(
                    LogLevel.Information,
                    LoggingEventId.UsingSignedAssertionFromVault,
                    "[MsIdWeb] Using signed assertion from {signedAssertionUri} as client credentials. ");

            private static readonly Action<ILogger, string, Exception?> s_usingCertThumbprint =
                LoggerMessage.Define<string>(
                    LogLevel.Information,
                    LoggingEventId.UsingCertThumbprint,
                    "[MsIdWeb] Using certificate Thumbprint={certThumbprint} as client credentials. ");

            private static readonly Action<ILogger, string, string, Exception?> s_credentialAttempt =
                LoggerMessage.Define<string, string>(
                    LogLevel.Information,
                    LoggingEventId.CredentialLoadAttempt,
                    "[MsIdWeb] Attempting to load the credential from the CredentialDescription with Id={Id} and Skip={Skip} . ");

            private static readonly Action<ILogger, string, string, Exception?> s_credentialAttemptFailed =
                LoggerMessage.Define<string, string>(
                LogLevel.Information,
                LoggingEventId.CredentialLoadAttemptFailed,
                "[MsIdWeb] Loading the credential from CredentialDescription Id={Id} failed. Will the credential be re-attempted? - {Skip}.");

            /// <summary>
            /// Logger for attempting to use a CredentialDescription with MSAL
            /// </summary>
            /// <param name="logger"></param>
            /// <param name="certificateDescription"></param>
            /// <param name="ex"></param>
            public static void AttemptToLoadCredentialsFailed(
                ILogger logger,
                CredentialDescription certificateDescription, 
                Exception ex) =>
                    s_credentialAttemptFailed(
                        logger,
                        certificateDescription.Id,
                        certificateDescription.Skip.ToString(),
                        ex);

            /// <summary>
            /// Logger for attempting to use a CredentialDescription with MSAL
            /// </summary>
            /// <param name="logger"></param>
            /// <param name="certificateDescription"></param>
            public static void AttemptToLoadCredentials(
                ILogger logger,
                CredentialDescription certificateDescription) => 
                    s_credentialAttempt(
                        logger, 
                        certificateDescription.Id, 
                        certificateDescription.Skip.ToString(), 
                        default!);

            /// <summary>
            /// Logger for attempting to use a CredentialDescription with MSAL
            /// </summary>
            /// <param name="logger"></param>
            /// <param name="certificateDescription"></param>
            public static void FailedToLoadCredentials(
                ILogger logger,
                CredentialDescription certificateDescription) =>
                    s_credentialAttemptFailed(
                        logger,
                        certificateDescription.Id,
                        certificateDescription.Skip.ToString(),
                        default!);

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
            /// <param name="signedAssertionUri"></param>
            public static void UsingSignedAssertionFromVault(
                ILogger logger,
                string signedAssertionUri) => s_usingSignedAssertionFromVault(logger, signedAssertionUri, default!);


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
