// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Identity.Client;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Interface for the token acquisition service (encapsulating MSAL.NET).
    /// </summary>
    public interface ITokenAcquirer
    {
        /// <summary>
        /// Typically used from an ASP.NET Core web app or web API controller. This method gets an access token
        /// for a downstream API on behalf of the user account for which the claims are provided in the <see cref="HttpContext.User"/>
        /// member of the controller's <see cref="HttpContext"/> parameter.
        /// </summary>
        /// <param name="scopes">Scopes to request for the downstream API to call.</param>
        /// <param name="tokenAcquisitionOptions">Options passed-in to create the token acquisition object which calls into MSAL .NET.</param>
        /// <param name="authenticationScheme">Authentication scheme. If null, will use OpenIdConnectDefault.AuthenticationScheme
        /// if called from a web app, and JwtBearerDefault.AuthenticationScheme if called from a web APIs.</param>
        /// <param name="user">Optional claims principal representing the user. If not provided, will use the signed-in
        /// user (in a web app), or the user for which the token was received (in a web API)
        /// cases where a given account is a guest in other tenants, and you want to acquire tokens for a specific tenant, like where the user is a guest in.</param>
        /// <returns>An <see cref="AuthenticationResult"/> to call on behalf of the user, the downstream API characterized by its scopes.</returns>
        Task<AuthenticationResult> GetAuthenticationResultForUserAsync(
            IEnumerable<string> scopes,
            TokenAcquisitionOptions? tokenAcquisitionOptions = null,
            string? authenticationScheme = null,
            ClaimsPrincipal? user = null);

        /// <summary>
        /// Acquires an authentication result from the authority configured in the app, for the confidential client itself (not on behalf of a user)
        /// using the client credentials flow. See https://aka.ms/msal-net-client-credentials.
        /// </summary>
        /// <param name="scope">The scope requested to access a protected API. For this flow (client credentials), the scope
        /// should be of the form "{ResourceIdUri/.default}" for instance <c>https://management.azure.net/.default</c> or, for Microsoft
        /// Graph, <c>https://graph.microsoft.com/.default</c> as the requested scopes are defined statically with the application registration
        /// in the portal, and cannot be overridden in the application, as you can request a token for only one resource at a time (use
        /// several calls to get tokens for other resources).</param>
        /// <param name="tokenAcquisitionOptions">Options passed-in to create the token acquisition object which calls into MSAL .NET.</param>
        /// <param name="authenticationScheme">Authentication scheme. If null, will use OpenIdConnectDefault.AuthenticationScheme
        /// if called from a web app, and JwtBearerDefault.AuthenticationScheme if called from a web API.</param>
        /// <returns>An authentication result for the app itself, based on its scopes.</returns>
        Task<AuthenticationResult> GetAuthenticationResultForAppAsync(
            string scope,
            TokenAcquisitionOptions? tokenAcquisitionOptions = null,
            string? authenticationScheme = null);
    }
}
