// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License

using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Microsoft.Identity.Web.Resource
{
    /// <summary>
    /// Extension class providing the extension methods for <see cref="HttpContent"/> that
    /// can be used in Web APIs to validate scopes in controller actions.
    /// </summary>
    public static class ScopesRequiredHttpContextExtensions
    {
        /// <summary>
        /// When applied to an <see cref="HttpContext"/>, verifies that the user authenticated in the
        /// web API has any of the accepted scopes.
        /// If the authenticated user does not have any of these <paramref name="acceptedScopes"/>, the
        /// method throws an HTTP Unauthorized with the message telling which scopes are expected in the token.
        /// </summary>
        /// <param name="context">HttpContext (from the controller).</param>
        /// <param name="acceptedScopes">Scopes accepted by this web API.</param>
        /// <exception cref="HttpRequestException"/> with a <see cref="HttpResponse.StatusCode"/> set to
        /// <see cref="HttpStatusCode.Unauthorized"/>
        public static void VerifyUserHasAnyAcceptedScope(this HttpContext context, params string[] acceptedScopes)
        {
            if (acceptedScopes == null)
            {
                throw new ArgumentNullException(nameof(acceptedScopes));
            }

            Claim scopeClaim = context?.User?.FindFirst("http://schemas.microsoft.com/identity/claims/scope");

            if (scopeClaim == null || !scopeClaim.Value.Split(' ').Intersect(acceptedScopes).Any())
            {
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                string message = $"The 'scope' claim does not contain scopes '{string.Join(",", acceptedScopes)}' or was not found";
                throw new HttpRequestException(message);
            }
        }
    }
}
