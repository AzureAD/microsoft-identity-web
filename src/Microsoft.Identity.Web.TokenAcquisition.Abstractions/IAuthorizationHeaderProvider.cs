using System.Collections.Generic;
using System.Security.Claims;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Creates the authorization header used to call a protected web API
    /// </summary>
    internal interface IAuthorizationHeaderProvider
    {
        /// <summary>
        /// Creates the authorization header used to call a protected web API.
        /// </summary>
        /// <param name="scopes">Scopes for which to request the authorization header.</param>
        /// <param name="tokenAcquisitionOptions">Token acquisition options.</param>
        /// <param name="protocolScheme">Protocol for which to create an authorization header.</param>
        /// <param name="claimsPrincipal">inbound authentication elements. (for the moment).</param>
        /// <returns>A string containing the protocols (for instance: "Bearer token", "PoP token", etc ...)
        /// and tokens.</returns>
        string CreateAuthorizationHeader(IEnumerable<string> scopes, TokenAcquirerOptions? tokenAcquisitionOptions=null, ClaimsPrincipal claimsPrincipal=null, string protocolScheme = "Bearer");
    }
}
