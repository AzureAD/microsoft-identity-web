// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Microsoft.Identity.Web.Resource
{

    internal class RequiredScopeFilter : IAuthorizationFilter
    {
        private readonly string[] _acceptedScopes;

        /// <summary>
        /// If the authenticated user does not have any of these <paramref name="acceptedScopes"/>, the
        /// method updates the HTTP response providing a status code 403 (Forbidden)
        /// and writes to the response body a message telling which scopes are expected in the token.
        /// </summary>
        /// <param name="acceptedScopes">Scopes accepted by this web API.</param>
        /// <remarks>When the scopes don't match, the response is a 403 (Forbidden),
        /// because the user is authenticated (hence not 401), but not authorized.</remarks>
        public RequiredScopeFilter(params string[] acceptedScopes)
        {
            _acceptedScopes = acceptedScopes;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            if (_acceptedScopes == null)
            {
                throw new ArgumentNullException(nameof(_acceptedScopes));
            }

            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            else if (context.HttpContext.User == null || context.HttpContext.User.Claims == null || !context.HttpContext.User.Claims.Any())
            {
                context.HttpContext.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                throw new UnauthorizedAccessException(IDWebErrorMessage.UnauthenticatedUser);
            }
            else
            {
                // Attempt with Scp claim
                Claim? scopeClaim = context.HttpContext.User.FindFirst(ClaimConstants.Scp);

                // Fallback to Scope claim name
                if (scopeClaim == null)
                {
                    scopeClaim = context.HttpContext.User.FindFirst(ClaimConstants.Scope);
                }

                if (scopeClaim == null || !scopeClaim.Value.Split(' ').Intersect(_acceptedScopes).Any())
                {
                    context.HttpContext.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                    string message = string.Format(CultureInfo.InvariantCulture, IDWebErrorMessage.MissingScopes, string.Join(",", _acceptedScopes));
                    context.HttpContext.Response.WriteAsync(message);
                    context.HttpContext.Response.CompleteAsync();
                    throw new UnauthorizedAccessException(message);
                }
            }
        }
    }
}
