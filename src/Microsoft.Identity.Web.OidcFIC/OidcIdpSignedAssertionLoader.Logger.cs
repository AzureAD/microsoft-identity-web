// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.Identity.Web.OidcFic
{
    /// <summary>
    /// High-performance logger extensions for OidcIdpSignedAssertionLoader.
    /// </summary>
    internal static partial class OidcIdpSignedAssertionLoaderLoggerExtensions
    {
        [LoggerMessage(
            EventId = 1,
            Level = LogLevel.Error,
            Message = "CustomSignedAssertionProviderData is null")]
        public static partial void CustomSignedAssertionProviderDataIsNull(this ILogger? logger);

        [LoggerMessage(
            EventId = 2,
            Level = LogLevel.Error,
            Message = "ConfigurationSection is null")]
        public static partial void ConfigurationSectionIsNull(this ILogger? logger);

        [LoggerMessage(
            EventId = 3,
            Level = LogLevel.Error,
            Message = "Failed to get signed assertion from {providerName}. exception occurred: {message}. Setting skip to true.")]
        public static partial void FailedToGetSignedAssertion(this ILogger? logger, string? providerName, string message, Exception? ex);
    }
}