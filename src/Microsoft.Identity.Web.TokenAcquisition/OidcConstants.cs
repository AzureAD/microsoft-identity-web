// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Web
{
    /*
     * Used by Microsoft.Identity.Web
     * Any changes to this member (including removal) can cause runtime failures.
     * Treat as a public member.
     */
    internal static class OidcConstants
    {
        public const string AdditionalClaims = "claims";
        public const string ScopeOfflineAccess = "offline_access";
        public const string ScopeProfile = "profile";
        public const string ScopeOpenId = "openid";
        public const string PolicyKey = "policy";
    }
}