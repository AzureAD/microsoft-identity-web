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
                LoggingEventId.AuthorityIgnored,
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

        private static readonly Action<ILogger, string, Exception?> s_authorityUsedConsiderInstanceTenantId =
            LoggerMessage.Define<string>(
                LogLevel.Warning,
                LoggingEventId.AuthorityUsedConsiderInstanceTenantId,
                "[MsIdWeb] The 'Authority' option ('{Authority}') is configured. " +
                "'Authority' is intended for vanilla OIDC / CIAM scenarios (3P) and routes through MSAL.WithOidcAuthority(). " +
                "First-party (1P) callers — e.g. services using Microsoft Identity Service Essentials (MISE) — should NOT use 'Authority'; " +
                "configure 'Instance' (e.g. \"https://login.microsoftonline.com\" or \"https://{{host}}/dstsv2\") and 'TenantId' separately, " +
                "which routes through MSAL.WithAuthority() and works correctly with eSTS, dSTS, and B2C. " +
                "Third-party (3P) callers using CIAM, ADFS, or generic OIDC issuers can safely ignore this warning.");

        /// <summary>
        /// Logs a warning when an application configures the single-string <c>Authority</c> option,
        /// hinting that first-party (1P) callers (e.g. MISE) should use <c>Instance</c> + <c>TenantId</c> instead.
        /// Third-party (3P) callers using CIAM / ADFS / generic OIDC can safely ignore the warning.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <param name="authority">The Authority value being parsed.</param>
        public static void AuthorityUsedConsiderInstanceTenantId(
            ILogger logger,
            string authority)
        {
            s_authorityUsedConsiderInstanceTenantId(logger, authority, null);
        }
    }
}
