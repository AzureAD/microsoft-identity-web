// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
#if !NETSTANDARD2_0 && !NET462 && !NET472
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
#endif

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Interface for the internal operations of token acquisition service (encapsulating MSAL.NET).
    /// </summary>
    internal interface ITokenAcquisitionInternal : ITokenAcquisition
    {
#if !NETSTANDARD2_0 && !NET462 && !NET472
        /// <summary>
        /// In a web app, adds, to the MSAL.NET cache, the account of the user authenticating to the web app, when the authorization code is received (after the user
        /// signed-in and consented)
        /// An On-behalf-of token contained in the <see cref="AuthorizationCodeReceivedContext"/> is added to the cache, so that it can then be used to acquire another token on-behalf-of the
        /// same user in order to call to downstream APIs.
        /// </summary>
        /// <param name="context">The context used when an 'AuthorizationCode' is received over the OpenIdConnect protocol.</param>
        /// <param name="scopes">Scopes to request.</param>
        /// <param name="authenticationScheme">Authentication scheme to use.</param>
        /// <returns>A <see cref="Task"/> that represents a completed add to cache operation.</returns>
        /// <example>
        /// From the configuration of the Authentication of the ASP.NET Core web API:
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
        Task AddAccountToCacheFromAuthorizationCodeAsync(
            AuthorizationCodeReceivedContext context,
            IEnumerable<string> scopes,
            string authenticationScheme = OpenIdConnectDefaults.AuthenticationScheme);
#else
        /// <summary>
        /// In a web app, adds, to the MSAL.NET cache, the account of the user authenticating to the web app, when the authorization code is received (after the user
        /// signed-in and consented)
        /// the token redeemed from the <paramref name="authCode"/>"/> is added to the cache, so that it can then be used to acquire another token on-behalf-of the
        /// same user in order to call to downstream APIs.
        /// </summary>
        /// <param name="scopes">Scopes to request. Can be empty</param>
        /// <param name="authCode">Authorization code</param>
        /// <param name="authenticationScheme">Authentication scheme to use (config section)</param>
        /// <param name="clientInfo">Client Info obtained with the code</param>
        /// <param name="codeVerifier">PKCE code verifier</param>
        /// <param name="userFlow">User flow in the case of B2C</param>
        /// <returns>The ID Token.</returns>
        Task<string> AddAccountToCacheFromAuthorizationCodeAsync(
            IEnumerable<string> scopes, 
            string authCode, 
            string authenticationScheme, 
            string? clientInfo, 
            string? codeVerifier, 
            string? userFlow);
#endif

        /// <summary>
        /// Removes the account associated with context.HttpContext.User from the MSAL.NET cache.
        /// </summary>
        /// <param name="user">Signed in user</param>
        /// <param name="authenticationScheme">Authentication scheme. If null, will use OpenIdConnectDefault.AuthenticationScheme
        /// if called from a web app, and JwtBearerDefault.AuthenticationScheme if called from a web APIs.</param>
        /// <returns>A <see cref="Task"/> that represents a completed remove from cache operation.</returns>
        Task RemoveAccountAsync(ClaimsPrincipal user, string? authenticationScheme = null);
    }
}
