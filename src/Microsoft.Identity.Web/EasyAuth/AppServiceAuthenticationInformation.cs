// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Information about the AppService configuration on the host.
    /// </summary>
    public static class AppServiceAuthenticationInformation
    {
        // Environment variables.
        private const string EasyAuthEnabledEnvironmentVariable = "WEBSITE_AUTH_ENABLED";            // True
        private const string EasyAuthOpenIdIssuerEnvironmentVariable = "WEBSITE_AUTH_OPENID_ISSUER"; // for instance https://sts.windows.net/<tenantId>/
        private const string EasyAuthClientIdEnvironmentVariable = "WEBSITE_AUTH_CLIENT_ID";         // A GUID
        private const string EasyAuthClientSecretEnvironmentVariable = "WEBSITE_AUTH_CLIENT_SECRET"; // A string
        private const string EasyAuthLogoutPathEnvironementVariable = "WEBSITE_AUTH_LOGOUT_PATH";    // /.auth/logout
        private const string EasyAuthIdentityProviderEnvironmentVariable = "WEBSITE_AUTH_DEFAULT_PROVIDER"; // AzureActiveDirectory
        private const string EasyAuthAzureActiveDirectory = "AzureActiveDirectory";

        // Artificially added by Microsoft.Identity.Web to help debugging Easy Auth. See the Debug controller of the test app
        private const string EasyAuthDebugHeadersEnvironementVariable = "EASY_AUTH_LOCAL_DEBUG";

        /// <summary>
        /// Is AppService authentication enabled?.
        /// </summary>
        public static bool IsAppServiceAadAuthenticationEnabled
        {
            get
            {
                return !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(EasyAuthEnabledEnvironmentVariable))
                    && Environment.GetEnvironmentVariable(EasyAuthIdentityProviderEnvironmentVariable) == EasyAuthAzureActiveDirectory;
            }
        }

        /// <summary>
        /// Logout URL for Easy Auth web sites.
        /// </summary>
        public static string? LogoutUrl
        {
            get
            {
                return Environment.GetEnvironmentVariable(EasyAuthLogoutPathEnvironementVariable);
            }
        }

        /// <summary>
        /// ClientID of the Easy Auth web site.
        /// </summary>
        internal static string? ClientId
        {
            get
            {
                return Environment.GetEnvironmentVariable(EasyAuthClientIdEnvironmentVariable);
            }
        }

        /// <summary>
        /// ClientID of the Easy Auth web site.
        /// </summary>
        internal static string? ClientSecret
        {
            get
            {
                return Environment.GetEnvironmentVariable(EasyAuthClientSecretEnvironmentVariable);
            }
        }

        /// <summary>
        /// ClientID of the Easy Auth web site.
        /// </summary>
        internal static string? Issuer
        {
            get
            {
                return Environment.GetEnvironmentVariable(EasyAuthOpenIdIssuerEnvironmentVariable);
            }
        }

        /// <summary>
        /// Get headers from environement to help debugging easy auth authentication.
        /// </summary>
        internal static string? GetDebugHeader(string header)
        {
            string? headerPlusValue = Environment.GetEnvironmentVariable(EasyAuthDebugHeadersEnvironementVariable)
                ?.Split(';')
                ?.FirstOrDefault(h => h.StartsWith(header));
            return headerPlusValue?.Substring(header.Length + 1);
        }
    }
}
