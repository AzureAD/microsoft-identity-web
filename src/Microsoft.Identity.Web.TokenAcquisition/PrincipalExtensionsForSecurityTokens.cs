// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Security.Claims;
using System.Security.Principal;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Extensions to retrive a <see cref="SecurityToken"/> from <see cref="ClaimsPrincipal"/>.
    /// </summary>
    public static class PrincipalExtensionsForSecurityTokens
    {
        /// <summary>
        /// Get the <see cref="SecurityToken"/> used to call a protected web API.
        /// </summary>
        /// <param name="claimsPrincipal"></param>
        /// <returns></returns>
        public static SecurityToken? GetBootstrapToken(this IPrincipal claimsPrincipal)
        {
            object? o = (claimsPrincipal?.Identity as ClaimsIdentity)?.BootstrapContext;
            if (o is SecurityToken securityToken)
            {
                return securityToken;
            }
            if (o is string s)
            {
                return new JsonWebToken(s);
            }
            return (o != null) ? new JsonWebToken(o.ToString()) : null;
        }
    }
}
