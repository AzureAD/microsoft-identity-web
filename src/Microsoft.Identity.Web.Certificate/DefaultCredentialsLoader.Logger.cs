// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using Microsoft.Identity.Abstractions;
using System;

namespace Microsoft.Identity.Web
{
    // Log messages for DefaultCredentialsLoader
    public partial class DefaultCredentialsLoader
    {
        /// <summary>
        /// Logging infrastructure
        /// </summary>
        private static class Logger
        {
            private static readonly Action<ILogger, string, string, bool, Exception?> s_credentialLoadingFailure =
                LoggerMessage.Define<string, string, bool>(
                    LogLevel.Information,
                    new EventId(
                        7,
                        nameof(CredentialLoadingFailure)),
            "Failed to load credential {id} from source {sourceType}. Will it be skipped in the future ? {skip}.");

            public static void CredentialLoadingFailure(ILogger logger, CredentialDescription cd, Exception? ex)
                => s_credentialLoadingFailure(logger, cd.Id, cd.SourceType.ToString(), cd.Skip, ex);

            private static readonly Action<ILogger, string, string, bool, Exception?> s_customSignedAssertionProviderLoadingFailure =
                LoggerMessage.Define<string, string, bool>(
                    LogLevel.Information,
                    new EventId(
                        7,
                        nameof(CustomSignedAssertionProviderLoadingFailure)),
            "Failed to find custom signed assertion provider {name} from source {sourceType}. Will it be skipped in the future ? {skip}.");

            public static void CustomSignedAssertionProviderLoadingFailure(
                ILogger logger,
                CredentialDescription cd,
                Exception ex
                ) => s_customSignedAssertionProviderLoadingFailure(logger, cd.CustomSignedAssertionProviderName ?? "NameMissing", cd.SourceType.ToString(), cd.Skip, ex);
        }
    }
}
