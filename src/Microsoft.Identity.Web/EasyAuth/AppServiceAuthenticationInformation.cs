// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

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
    }
}
