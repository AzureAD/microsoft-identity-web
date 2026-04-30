// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Web
{
    /*
     * Used by Microsoft.Identity.Web
     * Any changes to this member (including removal) can cause runtime failures.
     * Treat as a public member.
     */
    internal static class ErrorCodes
    {
        // AzureAD B2C
        public const string B2CPasswordResetErrorCode = "AADSTS50013";
        public const string B2CForgottenPassword = "AADB2C90118";
        public const string AccessDenied = "access_denied";
    }
}
