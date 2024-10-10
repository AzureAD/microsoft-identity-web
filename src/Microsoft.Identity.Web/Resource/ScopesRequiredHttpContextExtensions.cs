// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Microsoft.Identity.Web.Resource
{
    /// <summary>
    /// Extension class providing the extension
    /// methods for <see cref="HttpContent"/> that
    /// can be used in web APIs to validate scopes in controller actions.
    /// We recommend using instead the RequiredScope Attribute on the controller, the page or the action.
    /// See https://aka.ms/ms-id-web/required-scope-attribute.
    /// </summary>
    public static class ScopesRequiredHttpContextExtensions
    {
        /// <summary>
        /// When applied to an <see cref="HttpContext"/>, verifies that the user authenticated in the
        /// web API has any of the accepted scopes.
        /// If there is no authenticated user, the response is a 401 (Unauthenticated).
        /// If the authenticated user does not have any of these <paramref name="acceptedScopes"/>, the
        /// method updates the HTTP response providing a status code 403 (Forbidden)
        /// and writes to the response body a message telling which scopes are expected in the token.
        /// We recommend using instead the RequiredScope Attribute on the controller, the page or the action.
        /// See https://aka.ms/ms-id-web/required-scope-attribute.
        /// </summary>
        /// <param name="context">HttpContext (from the controller).</param>
        /// <param name="acceptedScopes">Scopes accepted by this web API.</param>
        public static void VerifyUserHasAnyAcceptedScope(this HttpContext context, params string[] acceptedScopes)
        {
            _ = Throws.IfNull(acceptedScopes);

            _ = Throws.IfNull(context);

            IEnumerable<Claim> userClaims;
            ClaimsPrincipal user;

            // Need to lock due to https://docs.microsoft.com/en-us/aspnet/core/performance/performance-best-practices?#do-not-access-httpcontext-from-multiple-threads
            lock (context)
            {
                user = context.User;
                userClaims = user.Claims;
            }

            if (user == null || userClaims == null || !userClaims.Any())
            {
                lock (context)
                {
                    context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                }

                throw new UnauthorizedAccessException(IDWebErrorMessage.UnauthenticatedUser);
            }
            else
            {
                // Attempt with Scp claim
                Claim? scopeClaim = user.FindFirst(ClaimConstants.Scp);

                // Fallback to Scope claim name
                if (scopeClaim == null)
                {
                    scopeClaim = user.FindFirst(ClaimConstants.Scope);
                }

                if (scopeClaim == null || !scopeClaim.Value.Split(' ').Intersect(acceptedScopes).Any())
                {
                    string message = string.Format(CultureInfo.InvariantCulture, IDWebErrorMessage.MissingScopes, string.Join(",", acceptedScopes));
                    
                    lock (context)
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                        _ = context.Response.WriteAsync(message);
                        _ = context.Response.CompleteAsync();
                    }
                    
                    throw new UnauthorizedAccessException(message);
                }
            }
        }
    }
}
