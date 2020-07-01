// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Interface for the internal operations of token acquisition service (encapsulating MSAL.NET).
    /// </summary>
    internal interface ITokenAcquisitionInternal : ITokenAcquisition
    {
        /// <summary>
        /// In a Web App, adds, to the MSAL.NET cache, the account of the user authenticating to the Web App, when the authorization code is received (after the user
        /// signed-in and consented)
        /// An On-behalf-of token contained in the <see cref="AuthorizationCodeReceivedContext"/> is added to the cache, so that it can then be used to acquire another token on-behalf-of the
        /// same user in order to call to downstream APIs.
        /// </summary>
        /// <param name="context">The context used when an 'AuthorizationCode' is received over the OpenIdConnect protocol.</param>
        /// <param name="scopes">Scopes to request.</param>
        /// <returns>A <see cref="Task"/> that represents a completed add to cache operation.</returns>
        /// <example>
        /// From the configuration of the Authentication of the ASP.NET Core Web API:
        /// <code>OpenIdConnectOptions options;</code>
        ///
        /// Subscribe to the authorization code received event:
        /// <code>
        ///  options.Events = new OpenIdConnectEvents();
        ///  options.Events.OnAuthorizationCodeReceived = OnAuthorizationCodeReceived;
        /// }
        /// </code>
        ///
        /// And then in the OnAuthorizationCodeRecieved method, call <see cref="AddAccountToCacheFromAuthorizationCodeAsync"/>:
        /// <code>
        /// private async Task OnAuthorizationCodeReceived(AuthorizationCodeReceivedContext context)
        /// {
        ///   var tokenAcquisition = context.HttpContext.RequestServices.GetRequiredService&lt;ITokenAcquisition&gt;();
        ///    await _tokenAcquisition.AddAccountToCacheFromAuthorizationCode(context, new string[] { "user.read" });
        /// }
        /// </code>
        /// </example>
        Task AddAccountToCacheFromAuthorizationCodeAsync(AuthorizationCodeReceivedContext context, IEnumerable<string> scopes);

        /// <summary>
        /// Removes the account associated with context.HttpContext.User from the MSAL.NET cache.
        /// </summary>
        /// <param name="context">RedirectContext passed-in to a <see cref="OpenIdConnectEvents.OnRedirectToIdentityProviderForSignOut"/>
        /// OpenID Connect event.</param>
        /// <returns>A <see cref="Task"/> that represents a completed remove from cache operation.</returns>
        Task RemoveAccountAsync(RedirectContext context);
    }
}
