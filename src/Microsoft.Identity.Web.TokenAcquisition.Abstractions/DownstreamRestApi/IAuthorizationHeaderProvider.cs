// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Identity.Abstractions
{
    /// <summary>
    /// Creates the value of an authorization header that the caller can use to call a protected web API.
    /// </summary>
    public interface IAuthorizationHeaderProvider
    {
        /// <summary>
        /// Creates the authorization header used to call a protected web API on behalf
        /// of a user.
        /// </summary>
        /// <param name="scopes">Scopes for which to request the authorization header.</param>
        /// <param name="downstreamApiOptions">Information about the API that will be called (for some
        /// protocols like Pop), and token acquisition options.</param>
        /// <param name="claimsPrincipal">Inbound authentication elements.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A string containing the authorization request, that is protocol and tokens
        /// (for instance: "Bearer token", "PoP token", etc ...).
        /// </returns>
        Task<string> CreateAuthorizationHeaderForUserAsync(
            IEnumerable<string> scopes, 
            DownstreamRestApiOptions? downstreamApiOptions=null, 
            ClaimsPrincipal? claimsPrincipal=null,
            CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Creates the authorization header used to call a protected web API on behalf
        /// of the application itself.
        /// </summary>
        /// <param name="scopes">Scopes for which to request the authorization header.</param>
        /// <param name="downstreamApiOptions">Information about the API that will be called (for some
        /// protocols like Pop), and token acquisition options.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A string containing the authorization request, that is protocol and tokens
        /// (for instance: "Bearer token", "PoP token", etc ...).
        /// </returns>
        Task<string> CreateAuthorizationHeaderForAppAsync(
            string scopes,
            DownstreamRestApiOptions? downstreamApiOptions = null,
            CancellationToken cancellationToken = default(CancellationToken));
    }
}
