// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Extensions for <see cref="AzureFunctionsAuthenticationHttpContextExtension"/>.
    /// </summary>
    public static class AzureFunctionsAuthenticationHttpContextExtension
    {
        /// <summary>
        /// Enables an Azure Function to act as/expose a protected web API, enabling bearer token authentication. Calling this method from your Azure function validates the token and exposes the identity of the user or app on behalf of which your function is called, in the HttpContext.User member, where your function can make use of it.
        /// </summary>
        /// <param name="httpContext">The current HTTP Context, such as req.HttpContext.</param>
        /// <returns>A task indicating success or failure. In case of failure <see cref="Microsoft.AspNetCore.Mvc.UnauthorizedObjectResult"/>.</returns>
        public static async Task<(bool, IActionResult?)> AuthenticateAzureFunctionAsync(
            this HttpContext httpContext)
        {
            if (httpContext == null)
            {
                throw new ArgumentNullException(nameof(httpContext));
            }

            AuthenticateResult result =
                await httpContext.AuthenticateAsync(Constants.Bearer).ConfigureAwait(false);

            if (result != null && result.Succeeded)
            {
#pragma warning disable CS8601 // Possible null reference assignment.
                httpContext.User = result.Principal;
#pragma warning restore CS8601 // Possible null reference assignment.
                return (true, null);
            }
            else
            {
                return (false, new UnauthorizedObjectResult(new ProblemDetails
                {
                    Title = "Authorization failed.",
                    Detail = result?.Failure?.Message,
                }));
            }
        }
    }
}
