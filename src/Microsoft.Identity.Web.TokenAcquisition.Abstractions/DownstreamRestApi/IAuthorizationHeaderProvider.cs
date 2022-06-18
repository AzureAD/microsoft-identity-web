using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Creates the value of an authorization header that the caller can use to call a protected web API
    /// </summary>
    internal interface IAuthorizationHeaderProvider
    {
        /// <summary>
        /// Creates the authorization header used to call a protected web API.
        /// </summary>
        /// <param name="scopes">Scopes for which to request the authorization header.</param>
        /// <param name="downstreamApiOptions">Information about the API that will be called (for some
        /// protocols like Pop), and token acquisition options.</param>
        /// <param name="claimsPrincipal">Inbound authentication elements.</param>
        /// <returns>A string containing the protocols (for instance: "Bearer token", "PoP token", etc ...)
        /// and tokens.</returns>
        Task<string> CreateAuthorizationHeaderAsync(IEnumerable<string> scopes, DownstreamRestApiOptions? downstreamApiOptions=null, ClaimsPrincipal? claimsPrincipal=null);
    }
}
