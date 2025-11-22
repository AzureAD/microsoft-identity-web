// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// High-performance logging for MergedOptions operations.
    /// </summary>
    internal static partial class MergedOptionsLogging
    {
        private static readonly Action<ILogger, string, string, string, Exception?> s_authorityIgnored =
            LoggerMessage.Define<string, string, string>(
                LogLevel.Warning,
                LoggingEventId.AuthorityConflict,
                "[MsIdWeb] Authority '{Authority}' is being ignored because Instance '{Instance}' and/or TenantId '{TenantId}' are already configured. To use Authority, remove Instance and TenantId from the configuration.");

        /// <summary>
        /// Logs a warning when Authority is configured alongside Instance and/or TenantId.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <param name="authority">The Authority value that is being ignored.</param>
        /// <param name="instance">The Instance value that takes precedence.</param>
        /// <param name="tenantId">The TenantId value that takes precedence.</param>
        public static void AuthorityIgnored(
            ILogger logger,
            string authority,
            string instance,
            string tenantId)
        {
            s_authorityIgnored(logger, authority, instance, tenantId, null);
        }
    }
}
