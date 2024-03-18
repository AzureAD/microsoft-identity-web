// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.Identity.Web
{
    public partial class AzureIdentityForKubernetesClientAssertion
    {
    /*       
    // High performance logger messages (before generation).
    #pragma warning disable SYSLIB1009 // Logging methods must be static
            [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "SignedAssertionFileDiskPath not provided. Falling back to the content of the AZURE_FEDERATED_TOKEN_FILE environment variable. ")]
            partial void SignedAssertionFileDiskPathNotProvided(ILogger logger);

            [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "The `{environmentVariableName}` environment variable not provided. ")]
            partial void SignedAssertionEnvironmentVariableNotProvided(ILogger logger, string environmentVariableName);

            [LoggerMessage(EventId = 3, Level = LogLevel.Error, Message = "The environment variable AZURE_FEDERATED_TOKEN_FILE or AZURE_ACCESS_TOKEN_FILE or the 'SignedAssertionFileDiskPath' must be set to the path of the file containing the signed assertion. ")]
            partial void NoSignedAssertionParameterProvided(ILogger logger);

            [LoggerMessage(EventId = 4, Level = LogLevel.Error, Message = "The file `{filePath}` containing the signed assertion was not found. ")]
            partial void FileAssertionPathNotFound(ILogger logger, string filePath);

            [LoggerMessage(EventId = 5, Level = LogLevel.Information, Message = "Successfully read the signed assertion for `{filePath}`. Expires at {expiry}. ")]
            partial void SuccessFullyReadSignedAssertion(ILogger logger, string filePath, DateTime expiry);

            [LoggerMessage(EventId = 6, Level = LogLevel.Error, Message = "The file `{filePath} does not contain a valid signed assertion. {message}. ")]
            partial void FileDoesNotContainValidAssertion(ILogger logger, string filePath, string message);
    #pragma warning restore SYSLIB1009 // Logging methods must be static
    */

        /// <summary>
        /// Performant logging messages.
        /// </summary>
        static class Logger
        {
            public static void SignedAssertionFileDiskPathNotProvided(ILogger? logger)
            {
                if (logger != null && logger.IsEnabled(LogLevel.Information))
                {
                    __SignedAssertionFileDiskPathNotProvidedCallback(logger, null);
                }
            }

            public static void SignedAssertionEnvironmentVariableNotProvided(ILogger? logger, string environmentVariableName)
            {
                if (logger != null && logger.IsEnabled(LogLevel.Information))
                {
                    __SignedAssertionEnvironmentVariableNotProvidedCallback(logger, environmentVariableName, null);
                }
            }

            public static void NoSignedAssertionParameterProvided(ILogger? logger)
            {
                if (logger != null && logger.IsEnabled(LogLevel.Error))
                {
                    __NoSignedAssertionParameterProvidedCallback(logger, null);
                }
            }

            public static void FileAssertionPathNotFound(ILogger? logger, string filePath)
            {
                if (logger != null && logger.IsEnabled(LogLevel.Error))
                {
                    __FileAssertionPathNotFoundCallback(logger, filePath, null);
                }
            }

            public static void SuccessFullyReadSignedAssertion(ILogger? logger, string filePath, DateTime expiry)
            {
                if (logger != null && logger.IsEnabled(LogLevel.Information))
                {
                    __SuccessFullyReadSignedAssertionCallback(logger, filePath, expiry, null);
                }
            }

            public static void FileDoesNotContainValidAssertion(ILogger? logger, string filePath, string message)
            {
                if (logger != null && logger.IsEnabled(LogLevel.Error))
                {
                    __FileDoesNotContainValidAssertionCallback(logger, filePath, message, null);
                }
            }

            private static readonly Action<ILogger, Exception?> __SignedAssertionFileDiskPathNotProvidedCallback =
                LoggerMessage.Define(LogLevel.Information, new EventId(1, nameof(SignedAssertionFileDiskPathNotProvided)), "SignedAssertionFileDiskPath not provided. Falling back to the content of the AZURE_FEDERATED_TOKEN_FILE environment variable. ");
            private static readonly Action<ILogger, string, Exception?> __SignedAssertionEnvironmentVariableNotProvidedCallback =
                LoggerMessage.Define<string>(LogLevel.Information, new EventId(2, nameof(SignedAssertionEnvironmentVariableNotProvided)), "The `{environmentVariableName}` environment variable not provided. ");

            private static readonly Action<ILogger, Exception?> __NoSignedAssertionParameterProvidedCallback =
                LoggerMessage.Define(LogLevel.Error, new EventId(3, nameof(NoSignedAssertionParameterProvided)), "The environment variable AZURE_FEDERATED_TOKEN_FILE or AZURE_ACCESS_TOKEN_FILE or the 'SignedAssertionFileDiskPath' must be set to the path of the file containing the signed assertion. ");

            private static readonly Action<ILogger, string, Exception?> __FileAssertionPathNotFoundCallback =
                LoggerMessage.Define<string>(LogLevel.Error, new EventId(4, nameof(FileAssertionPathNotFound)), "The file `{filePath}` containing the signed assertion was not found. ");

            private static readonly Action<ILogger, string, DateTime, Exception?> __SuccessFullyReadSignedAssertionCallback =
                LoggerMessage.Define<string, DateTime>(LogLevel.Information, new EventId(5, nameof(SuccessFullyReadSignedAssertion)), "Successfully read the signed assertion for `{filePath}`. Expires at {expiry}. ");

            private static readonly Action<ILogger, string, string, Exception?> __FileDoesNotContainValidAssertionCallback =
                LoggerMessage.Define<string, string>(LogLevel.Error, new EventId(6, nameof(FileDoesNotContainValidAssertion)), "The file `{filePath} does not contain a valid signed assertion. {message}. ");
        }
    }
}
