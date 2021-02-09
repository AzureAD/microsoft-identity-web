// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Microsoft.Identity.Web.Resource
{
    /// <summary>
    /// This extension class is now Obsolete.
    /// Use instead the RequiredScope Attribute on the controller, the page or the action.
    /// See https://aka.ms/ms-id-web/required-scope-attribute.
    /// </summary>
    [Obsolete(IDWebErrorMessage.VerifyUserHasAnyAcceptedScopeIsObsolete, false)]
    public static class ScopesRequiredHttpContextExtensions
    {
        /// <summary>
        /// This method is now Obsolete.
        /// Use instead the RequiredScope Attribute on the controller, the page or the action.
        /// See https://aka.ms/ms-id-web/required-scope-attribute.
        /// </summary>
        /// <param name="context">HttpContext (from the controller).</param>
        /// <param name="acceptedScopes">Scopes accepted by this web API.</param>
        [Obsolete(IDWebErrorMessage.VerifyUserHasAnyAcceptedScopeIsObsolete, false)]
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
                throw new UnauthorizedAccessException(IDWebErrorMessage.UnauthenticatedUser);
            }
            else
            {
                // Attempt with Scp claim
                Claim? scopeClaim = context.User.FindFirst(ClaimConstants.Scp);

                // Fallback to Scope claim name
                if (scopeClaim == null)
                {
                    scopeClaim = context.User.FindFirst(ClaimConstants.Scope);
                }

                if (scopeClaim == null || !scopeClaim.Value.Split(' ').Intersect(acceptedScopes).Any())
                {
                    context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                    string message = string.Format(CultureInfo.InvariantCulture, IDWebErrorMessage.MissingScopes, string.Join(",", acceptedScopes));
                    context.Response.WriteAsync(message);
                    context.Response.CompleteAsync();
                    throw new UnauthorizedAccessException(message);
                }
            }
        }
    }
}
