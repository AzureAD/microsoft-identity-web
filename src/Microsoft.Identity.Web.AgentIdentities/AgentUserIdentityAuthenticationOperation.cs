// Copyright (c) Mi'crosoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Security.Claims;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AuthScheme;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.Identity.Web.AgentIdentities
{
    internal class AgentUserIdentityAuthenticationOperation : IAuthenticationOperation
    {
        public AgentUserIdentityAuthenticationOperation(string userOid, IEnumerable<string> scopes, ITokenAcquirer tokenAcquirer, TokenAcquisitionOptions tokenAcquisitionOptions)
        {
            UserOid = userOid;
            Scopes = scopes;
            _tokenAcquirer = tokenAcquirer;
            TokenAcquisitionOptions = tokenAcquisitionOptions;
        }

        /// <inherit/>
        public int TelemetryTokenType => 5; // indicate extension token type

        /// <inherit/>
        public string AuthorizationHeaderPrefix => "Bearer"; // At MSAL level

        /// <inherit/>
        public string KeyId => string.Empty;

        /// <inherit/>
        public string AccessTokenType => "Bearer";

        /// <inherit/>
        private string UserOid { get; }
        private IEnumerable<string> Scopes { get; }
        private ITokenAcquirer _tokenAcquirer { get; }
        public TokenAcquisitionOptions TokenAcquisitionOptions { get; }

        /// <inherit/>
        public void FormatResult(AuthenticationResult authenticationResult)
        {
            if (authenticationResult == null)
            {
                throw new ArgumentNullException(nameof(authenticationResult));
            }

            // This method is used in the context of agent identities, which means that we'll already have
            // the FIC token representing the agent user identity in the authentication result.
            ClaimsPrincipal user = new ClaimsPrincipal(new CaseSensitiveClaimsIdentity(
                [
                    new Claim("oid", UserOid),
                    new Claim("tid", authenticationResult.TenantId)
                ]));

#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits
            AcquireTokenResult result =
                _tokenAcquirer.GetTokenForUserAsync(Scopes, TokenAcquisitionOptions, user).GetAwaiter().GetResult();
#pragma warning restore VSTHRD002 // Avoid problematic synchronous waits
            authenticationResult.AccessToken = result.AccessToken;
        }

        public IReadOnlyDictionary<string, string> GetTokenRequestParams()
        {
            return new Dictionary<string, string>
            {
                { "TokenType", "Bearer"},
            };
        }
    }
}
