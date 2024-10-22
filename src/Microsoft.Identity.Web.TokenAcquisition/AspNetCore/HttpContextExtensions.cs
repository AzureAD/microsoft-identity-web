// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.Identity.Web
{
    internal static class HttpContextExtensions
    {
        /// <summary>
        /// Keep the validated token associated with the HTTP request.
        /// </summary>
        /// <param name="httpContext">HTTP context.</param>
        /// <param name="token">Token to preserve after the token is validated so that
        /// it can be used in the actions.</param>
        internal static void StoreTokenUsedToCallWebAPI(this HttpContext httpContext, SecurityToken? token)
        {
            // lock due to https://learn.microsoft.com/aspnet/core/performance/performance-best-practices?#do-not-access-httpcontext-from-multiple-threads
            lock (httpContext)
            {
                httpContext.Items[Constants.JwtSecurityTokenUsedToCallWebApi] = token;
            }
        }

        /// <summary>
        /// Get the parsed information about the token used to call the web API.
        /// </summary>
        /// <param name="httpContext">HTTP context associated with the current request.</param>
        /// <returns><see cref="SecurityToken"/> used to call the web API.</returns>
        internal static SecurityToken? GetTokenUsedToCallWebAPI(this HttpContext httpContext)
        {
            // lock due to https://learn.microsoft.com/aspnet/core/performance/performance-best-practices?#do-not-access-httpcontext-from-multiple-threads
            lock (httpContext)
            {
                return httpContext.Items[Constants.JwtSecurityTokenUsedToCallWebApi] as SecurityToken;
            }
        }
    }
}
