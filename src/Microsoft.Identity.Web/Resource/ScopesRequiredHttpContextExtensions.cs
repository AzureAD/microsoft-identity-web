// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

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
        /// If there is no authenticated user, the reponse is a 401 (Unauthenticated).
        /// If the authenticated user does not have any of these <paramref name="acceptedScopes"/>, the
        /// method updates the HTTP response providing a status code Forbidden (403)
        /// and writes to the response body a message telling which scopes are expected in the token.
        /// </summary>
        /// <param name="context">HttpContext (from the controller).</param>
        /// <param name="acceptedScopes">Scopes accepted by this web API.</param>
        /// <remarks>When the scopes don't match the response is a 403 (Forbidden), 
        /// because the user is authenticated (hence not 401), but not authorized.</remarks>
        public static void VerifyUserHasAnyAcceptedScope(this HttpContext context, params string[] acceptedScopes)
        {
            if (acceptedScopes == null)
            {
                throw new ArgumentNullException(nameof(acceptedScopes));
            }

            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            else if (context.User == null || context.User.Claims == null || !context.User.Claims.Any())
            {
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            }
            else
            {
                // Attempt with Scp claim
                Claim? scopeClaim = context.User.FindFirst(ClaimConstants.Scp);

                // Fallback to Scope claim name
                if (scopeClaim == null)
                {
                    scopeClaim = context?.User?.FindFirst(ClaimConstants.Scope);
                }

                if (scopeClaim == null || !scopeClaim.Value.Split(' ').Intersect(acceptedScopes).Any())
                {
                    context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                    string message = $"The 'scope' or 'scp' claim does not contain scopes '{string.Join(",", acceptedScopes)}' or was not found";
                    context.Response.WriteAsync(message);
                }
            }
        }
    }
}
