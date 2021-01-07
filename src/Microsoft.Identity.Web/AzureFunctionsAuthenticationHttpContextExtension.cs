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
        /// Enables Bearer authentication for an API for use in Azure Functions.
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

            AuthenticateResult? result =
                await httpContext.AuthenticateAsync(Constants.Bearer).ConfigureAwait(false);
            if (result != null && result.Succeeded)
            {
                httpContext.User = result.Principal;
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
