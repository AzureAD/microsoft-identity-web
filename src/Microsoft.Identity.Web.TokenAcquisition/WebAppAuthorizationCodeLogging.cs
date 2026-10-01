// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// High-performance logging for the automatic MSAL-based authorization code redemption that
    /// Microsoft.Identity.Web enables when a web app is configured with a complex client credential
    /// (not a plain client secret) and ResponseType=code, without calling
    /// EnableTokenAcquisitionToCallDownstreamApi().
    /// </summary>
    internal static partial class WebAppAuthorizationCodeLogging
    {
        private static readonly Action<ILogger, string, Exception?> s_automaticRedemptionEnabled =
            LoggerMessage.Define<string>(
                LogLevel.Information,
                LoggingEventId.AutomaticAuthorizationCodeRedemptionEnabled,
                "[MsIdWeb] Authentication scheme '{Scheme}' is configured with a complex client credential (not a plain client secret) " +
                "and ResponseType=code. Microsoft.Identity.Web automatically redeems the authorization code using MSAL.NET so that Azure AD " +
                "does not reject the request with AADSTS7000218. Call EnableTokenAcquisitionToCallDownstreamApi() if you also need to call downstream APIs.");

        /// <summary>
        /// Logs that automatic MSAL-based authorization code redemption was enabled for the given scheme.
        /// </summary>
        public static void AutomaticRedemptionEnabled(ILogger logger, string scheme)
        {
            s_automaticRedemptionEnabled(logger, scheme, null);
        }

        private static readonly Action<ILogger, string, Exception?> s_automaticRedemptionNotAvailable =
            LoggerMessage.Define<string>(
                LogLevel.Warning,
                LoggingEventId.AutomaticAuthorizationCodeRedemptionNotAvailable,
                "[MsIdWeb] Authentication scheme '{Scheme}' is configured with a complex client credential (not a plain client secret) and " +
                "ResponseType=code, but Microsoft.Identity.Web could not automatically redeem the authorization code using MSAL.NET because " +
                "the token acquisition services are not available. The default OpenID Connect handler will attempt to redeem the code itself " +
                "and Azure AD will reject it with AADSTS7000218 because it is unaware of the configured credential. Call " +
                "EnableTokenAcquisitionToCallDownstreamApi() on the authentication builder to fix this.");

        /// <summary>
        /// Logs that automatic MSAL-based authorization code redemption could not be performed for the given scheme.
        /// </summary>
        public static void AutomaticRedemptionNotAvailable(ILogger logger, string scheme)
        {
            s_automaticRedemptionNotAvailable(logger, scheme, null);
        }
    }
}
