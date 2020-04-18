// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Interface for the token acquisition service (encapsultating MSAL.NET).
    /// </summary>
    public interface ITokenAcquisition
    {
        /// <summary>
        /// Typically used from an ASP.NET Core Web App or Web API controller, this method gets an access token
        /// for a downstream API on behalf of the user account which claims are provided in the <see cref="HttpContext.User"/>
        /// member of the <paramref name="context"/> parameter.
        /// </summary>
        /// <param name="context">HttpContext associated with the Controller or auth operation.</param>
        /// <param name="scopes">Scopes to request for the downstream API to call.</param>
        /// <param name="tenantId">Enables to override the tenant/account for the same identity. This is useful in the
        /// cases where a given account is guest in other tenants, and you want to acquire tokens for a specific tenant.</param>
        /// <returns>An access token to call on behalf of the user, the downstream API characterized by its scopes.</returns>
        Task<string> GetAccessTokenForUserAsync(IEnumerable<string> scopes, string tenantId = null);

        /// <summary>
        /// Acquires a token from the authority configured in the app, for the confidential client itself (not on behalf of a user)
        /// using the client credentials flow. See https://aka.ms/msal-net-client-credentials.
        /// </summary>
        /// <param name="scopes">scopes requested to access a protected API. For this flow (client credentials), the scopes
        /// should be of the form "{ResourceIdUri/.default}" for instance <c>https://management.azure.net/.default</c> or, for Microsoft
        /// Graph, <c>https://graph.microsoft.com/.default</c> as the requested scopes are defined statically with the application registration
        /// in the portal, and cannot be overriden in the application.</param>
        /// <returns>An access token for the app itself, based on its scopes.</returns>
        Task<string> GetAccessTokenForAppAsync(IEnumerable<string> scopes);

        /// <summary>
        /// Used in Web APIs (which therefore cannot have an interaction with the user).
        /// Replies to the client through the HttpResponse by sending a 403 (forbidden) and populating wwwAuthenticateHeaders so that
        /// the client can trigger an interaction with the user so the user can consent to more scopes.
        /// </summary>
        /// <param name="scopes">Scopes to consent to.</param>
        /// <param name="msalSeviceException"><see cref="MsalUiRequiredException"/> triggering the challenge.</param>
        void ReplyForbiddenWithWwwAuthenticateHeader(
            IEnumerable<string> scopes,
            MsalUiRequiredException msalSeviceException);
    }
}