using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Microsoft.Identity.Web
{
    // TODO: replace ClaimsPrincipal by an abstraction of the AuthenticationTicket.

    /// <summary>
    /// Creates the authorization header used to call a protected web API
    /// </summary>
    internal interface IAuthorizationHeaderProvider
    {
        /// <summary>
        /// Creates the authorization header used to call a protected web API.
        /// </summary>
        /// <param name="scopes">Scopes for which to request the authorization header.</param>
        /// <param name="downstreamApiOptions">Information about the API that will be called (for some
        /// protocols like Pop), and token acquisition options.</param>
        /// <param name="protocolScheme">Protocol for which to create an authorization header.</param>
        /// <param name="claimsPrincipal">inbound authentication elements. (for the moment).</param>
        /// <returns>A string containing the protocols (for instance: "Bearer token", "PoP token", etc ...)
        /// and tokens.</returns>
        Task<string> CreateAuthorizationHeaderAsync(IEnumerable<string> scopes, DownstreamRestApiOptions? downstreamApiOptions=null, ClaimsPrincipal? claimsPrincipal=null, string protocolScheme = "Bearer");
    }
}
