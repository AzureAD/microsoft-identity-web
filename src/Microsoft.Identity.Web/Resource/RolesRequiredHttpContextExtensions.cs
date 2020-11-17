// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.Http;

namespace Microsoft.Identity.Web.Resource
{
    /// <summary>
    /// Extension class providing the extension methods for <see cref="HttpContent"/> that
    /// can be used in web APIs to validate the roles in controller actions.
    /// </summary>
    public static class RolesRequiredHttpContextExtensions
    {
        /// <summary>
        /// When applied to an <see cref="HttpContext"/>, verifies that the application
        /// has the expected roles.
        /// </summary>
        /// <param name="context">HttpContext (from the controller).</param>
        /// <param name="acceptedRoles">Roles accepted by this web API.</param>
        /// <remarks>When the roles don't match, the response is a 403 (Forbidden),
        /// because the app does not have the expected roles.</remarks>
        public static void ValidateAppRole(this HttpContext context, params string[] acceptedRoles)
        {
            if (acceptedRoles == null)
            {
                throw new ArgumentNullException(nameof(acceptedRoles));
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
                // Attempt with Roles claim
                IEnumerable<string> rolesClaim = context.User.Claims.Where(
                    c => c.Type == ClaimConstants.Roles || c.Type == ClaimConstants.Role)
                    .SelectMany(c => c.Value.Split(' '));

                if (!rolesClaim.Intersect(acceptedRoles).Any())
                {
                    context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                    string message = string.Format(CultureInfo.InvariantCulture, IDWebErrorMessage.MissingRoles, string.Join(", ", acceptedRoles));
                    context.Response.WriteAsync(message);
                    context.Response.CompleteAsync();
                    throw new UnauthorizedAccessException(message);
                }
            }
        }
    }
}
