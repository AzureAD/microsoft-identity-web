// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace Microsoft.Identity.Web
{
    internal class AuthCodeRedemptionParameters
    {
        public AuthCodeRedemptionParameters(
            IEnumerable<string> scopes,
            string authCode,
            string authScheme,
            string? clientInfo,
            string? codeVerifier,
            string? userFlow,
            string? tenant)
        {
            Scopes = scopes;
            AuthCode = authCode;
            AuthenticationScheme = authScheme;
            ClientInfo = clientInfo;
            CodeVerifier = codeVerifier;
            UserFlow = userFlow;
            Tenant = tenant;
        }

        public IEnumerable<string> Scopes { get; set; }
        public string AuthCode { get; set; }
        public string AuthenticationScheme { get; set; }
        public string? ClientInfo { get; set; }
        public string? CodeVerifier { get; set; }
        public string? UserFlow { get; set; }
        public string? Tenant { get; set; }
    }
}
