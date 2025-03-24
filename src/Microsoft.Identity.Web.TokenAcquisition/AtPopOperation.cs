// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.Identity.Client.AuthScheme;
using Microsoft.Identity.Client;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.Identity.Web
{
    internal class AtPopOperation : IAuthenticationOperation
    {
        private readonly string _reqCnf;

        public AtPopOperation(string keyId, string reqCnf)
        {
            KeyId = keyId;
            _reqCnf = reqCnf;
        }

        public int TelemetryTokenType => 4; // as per TelemetryTokenTypeConstants

        public string AuthorizationHeaderPrefix => "Bearer"; // these tokens go over bearer

        public string KeyId { get; }

        public string AccessTokenType => "pop"; // eSTS returns token_type=pop and MSAL needs to know

        public void FormatResult(AuthenticationResult authenticationResult)
        {
            // no-op, adding the SHR is done by the caller
        }

        public IReadOnlyDictionary<string, string> GetTokenRequestParams()
        {
            return new Dictionary<string, string>()
            {
                {"req_cnf", Base64UrlEncoder.Encode(_reqCnf) },
                {"token_type", "pop" }
            };
        }
    }
}
