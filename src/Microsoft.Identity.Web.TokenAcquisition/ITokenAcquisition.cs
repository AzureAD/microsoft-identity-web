// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
#if !NETSTANDARD2_0 && !NET462 && !NET472
using Microsoft.AspNetCore.Http;
#endif
using Microsoft.Identity.Client;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Interface for the token acquisition service (encapsulating MSAL.NET).
    /// </summary>
    public interface ITokenAcquisition
    {
#if !NETSTANDARD2_0 && !NET462 && !NET472
        /// <summary>
        /// Typically used from an ASP.NET Core web app or web API controller. This method gets an access token
        /// for a downstream API on behalf of the user account for which the claims are provided in the <see cref="HttpContext.User"/>
        /// member of the controller's <see cref="HttpContext"/> parameter.
        /// </summary>
        /// <param name="scopes">Scopes to request for the downstream API to call.</param>
        /// <param name="tenantId">Enables to override the tenant/account for the same identity. This is useful in the
        /// cases where a given account is guest in other tenants, and you want to acquire tokens for a specific tenant.</param>
        /// <param name="userFlow">Azure AD B2C UserFlow to target.</param>
        /// <param name="user">Optional claims principal representing the user. If not provided, will use the signed-in
        /// user (in a web app), or the user for which the token was received (in a web API)
        /// cases where a given account is guest in other tenants, and you want to acquire tokens for a specific tenant, like where the user is a guest in.</param>
        /// <param name="tokenAcquisitionOptions">Options passed-in to create the token acquisition object which calls into MSAL .NET.</param>
        /// <returns>An access token to call on behalf of the user, the downstream API characterized by its scopes.</returns>
        Task<string> GetAccessTokenForUserAsync(
            IEnumerable<string> scopes,
            string? tenantId = null,
            string? userFlow = null,
            ClaimsPrincipal? user = null,
            TokenAcquisitionOptions? tokenAcquisitionOptions = null)
        {
            return GetAccessTokenForUserAsync(
                scopes,
                null,
                tenantId,
                userFlow,
                user,
                tokenAcquisitionOptions);
        }
#endif

        /// <summary>
        /// Typically used from an ASP.NET Core web app or web API controller. This method gets an access token
        /// for a downstream API on behalf of the user account for which the claims are provided in the current user
        /// </summary>
        /// <param name="scopes">Scopes to request for the downstream API to call.</param>
        /// <param name="authenticationScheme">Authentication scheme. If null, will use OpenIdConnectDefault.AuthenticationScheme
        /// if called from a web app, and JwtBearerDefault.AuthenticationScheme if called from a web API.</param>
        /// <param name="tenantId">Enables to override the tenant/account for the same identity. This is useful in the
        /// cases where a given account is guest in other tenants, and you want to acquire tokens for a specific tenant.</param>
        /// <param name="userFlow">Azure AD B2C UserFlow to target.</param>
        /// <param name="user">Optional claims principal representing the user. If not provided, will use the signed-in
        /// user (in a web app), or the user for which the token was received (in a web API)
        /// cases where a given account is guest in other tenants, and you want to acquire tokens for a specific tenant, like where the user is a guest in.</param>
        /// <param name="tokenAcquisitionOptions">Options passed-in to create the token acquisition object which calls into MSAL .NET.</param>
        /// <returns>An access token to call on behalf of the user, the downstream API characterized by its scopes.</returns>
        Task<string> GetAccessTokenForUserAsync(
            IEnumerable<string> scopes,
            string? authenticationScheme,
            string? tenantId = null,
            string? userFlow = null,
            ClaimsPrincipal? user = null,
            TokenAcquisitionOptions? tokenAcquisitionOptions = null);

#if !NETSTANDARD2_0 && !NET462 && !NET472
        /// <summary>
        /// Typically used from an ASP.NET Core web app or web API controller. This method gets an access token
        /// for a downstream API on behalf of the user account for which the claims are provided in the current user
        /// </summary>
        /// <param name="scopes">Scopes to request for the downstream API to call.</param>
        /// <param name="tenantId">Enables to override the tenant/account for the same identity. This is useful in the
        /// cases where a given account is a guest in other tenants, and you want to acquire tokens for a specific tenant.</param>
        /// <param name="userFlow">Azure AD B2C UserFlow to target.</param>
        /// <param name="user">Optional claims principal representing the user. If not provided, will use the signed-in
        /// user (in a web app), or the user for which the token was received (in a web API)
        /// cases where a given account is a guest in other tenants, and you want to acquire tokens for a specific tenant, like where the user is a guest in.</param>
        /// <param name="tokenAcquisitionOptions">Options passed-in to create the token acquisition object which calls into MSAL .NET.</param>
        /// <returns>An <see cref="AuthenticationResult"/> to call on behalf of the user, the downstream API characterized by its scopes.</returns>
        Task<AuthenticationResult> GetAuthenticationResultForUserAsync(
            IEnumerable<string> scopes,
            string? tenantId = null,
            string? userFlow = null,
            ClaimsPrincipal? user = null,
            TokenAcquisitionOptions? tokenAcquisitionOptions = null)
        {
            return GetAuthenticationResultForUserAsync(
                scopes,
                null,
                tenantId,
                userFlow,
                user,
                tokenAcquisitionOptions);
        }
#endif

        /// <summary>
        /// Typically used from an ASP.NET Core web app or web API controller. This method gets an access token
        /// for a downstream API on behalf of the user account for which the claims are provided in the current user
        /// </summary>
        /// <param name="scopes">Scopes to request for the downstream API to call.</param>
        /// <param name="authenticationScheme">Authentication scheme. If null, will use OpenIdConnectDefault.AuthenticationScheme
        /// if called from a web app, and JwtBearerDefault.AuthenticationScheme if called from a web APIs.</param>
        /// <param name="tenantId">Enables to override the tenant/account for the same identity. This is useful in the
        /// cases where a given account is a guest in other tenants, and you want to acquire tokens for a specific tenant.</param>
        /// <param name="userFlow">Azure AD B2C UserFlow to target.</param>
        /// <param name="user">Optional claims principal representing the user. If not provided, will use the signed-in
        /// user (in a web app), or the user for which the token was received (in a web API)
        /// cases where a given account is a guest in other tenants, and you want to acquire tokens for a specific tenant, like where the user is a guest in.</param>
        /// <param name="tokenAcquisitionOptions">Options passed-in to create the token acquisition object which calls into MSAL .NET.</param>
        /// <returns>An <see cref="AuthenticationResult"/> to call on behalf of the user, the downstream API characterized by its scopes.</returns>
        Task<AuthenticationResult> GetAuthenticationResultForUserAsync(
            IEnumerable<string> scopes,
            string? authenticationScheme,
            string? tenantId = null,
            string? userFlow = null,
            ClaimsPrincipal? user = null,
            TokenAcquisitionOptions? tokenAcquisitionOptions = null);

#if !NETSTANDARD2_0 && !NET462 && !NET472
        /// <summary>
        /// Acquires a token from the authority configured in the app, for the confidential client itself (not on behalf of a user)
        /// using the client credentials flow. See https://aka.ms/msal-net-client-credentials.
        /// </summary>
        /// <param name="scope">The scope requested to access a protected API. For this flow (client credentials), the scope
        /// should be of the form "{ResourceIdUri/.default}" for instance <c>https://management.azure.net/.default</c> or, for Microsoft
        /// Graph, <c>https://graph.microsoft.com/.default</c> as the requested scopes are defined statically with the application registration
        /// in the portal, cannot be overridden in the application, as you can request a token for only one resource at a time (use
        /// several calls to get tokens for other resources).</param>
        /// <param name="tenant">Enables overriding of the tenant/account for the same identity. This is useful in the
        /// cases where a given account is a guest in other tenants, and you want to acquire tokens for a specific tenant.</param>
        /// <param name="tokenAcquisitionOptions">Options passed-in to create the token acquisition object which calls into MSAL .NET.</param>
        /// <returns>An access token for the app itself, based on its scopes.</returns>
        Task<string> GetAccessTokenForAppAsync(
            string scope,
            string? tenant = null,
            TokenAcquisitionOptions? tokenAcquisitionOptions = null)
        {
            return GetAccessTokenForAppAsync(
                scope,
                null,
                tenant,
                tokenAcquisitionOptions);
        }
#endif

        /// <summary>
        /// Acquires a token from the authority configured in the app, for the confidential client itself (not on behalf of a user)
        /// using the client credentials flow. See https://aka.ms/msal-net-client-credentials.
        /// </summary>
        /// <param name="scope">The scope requested to access a protected API. For this flow (client credentials), the scope
        /// should be of the form "{ResourceIdUri/.default}" for instance <c>https://management.azure.net/.default</c> or, for Microsoft
        /// Graph, <c>https://graph.microsoft.com/.default</c> as the requested scopes are defined statically with the application registration
        /// in the portal, cannot be overridden in the application, as you can request a token for only one resource at a time (use
        /// several calls to get tokens for other resources).</param>
        /// <param name="authenticationScheme">Authentication scheme. If null, will use OpenIdConnectDefault.AuthenticationScheme
        /// if called from a web app, and JwtBearerDefault.AuthenticationScheme if called from a web API.</param>
        /// <param name="tenant">Enables overriding of the tenant/account for the same identity. This is useful in the
        /// cases where a given account is a guest in other tenants, and you want to acquire tokens for a specific tenant.</param>
        /// <param name="tokenAcquisitionOptions">Options passed-in to create the token acquisition object which calls into MSAL .NET.</param>
        /// <returns>An access token for the app itself, based on its scopes.</returns>
        Task<string> GetAccessTokenForAppAsync(
            string scope,
            string? authenticationScheme,
            string? tenant = null,
            TokenAcquisitionOptions? tokenAcquisitionOptions = null);

#if !NETSTANDARD2_0 && !NET462 && !NET472
        /// <summary>
        /// Acquires an authentication result from the authority configured in the app, for the confidential client itself (not on behalf of a user)
        /// using the client credentials flow. See https://aka.ms/msal-net-client-credentials.
        /// </summary>
        /// <param name="scope">The scope requested to access a protected API. For this flow (client credentials), the scope
        /// should be of the form "{ResourceIdUri/.default}" for instance <c>https://management.azure.net/.default</c> or, for Microsoft
        /// Graph, <c>https://graph.microsoft.com/.default</c> as the requested scopes are defined statically with the application registration
        /// in the portal, and cannot be overridden in the application, as you can request a token for only one resource at a time (use
        /// several calls to get tokens for other resources).</param>
        /// <param name="tenant">Enables overriding of the tenant/account for the same identity. This is useful
        /// for multi tenant apps or daemons.</param>
        /// <param name="tokenAcquisitionOptions">Options passed-in to create the token acquisition object which calls into MSAL .NET.</param>
        /// <returns>An authentication result for the app itself, based on its scopes.</returns>
        Task<AuthenticationResult> GetAuthenticationResultForAppAsync(
            string scope,
            string? tenant = null,
            TokenAcquisitionOptions? tokenAcquisitionOptions = null)
        {
            return GetAuthenticationResultForAppAsync(
                scope,
                null,
                tenant,
                tokenAcquisitionOptions);
        }
#endif

        /// <summary>
        /// Acquires an authentication result from the authority configured in the app, for the confidential client itself (not on behalf of a user)
        /// using the client credentials flow. See https://aka.ms/msal-net-client-credentials.
        /// </summary>
        /// <param name="scope">The scope requested to access a protected API. For this flow (client credentials), the scope
        /// should be of the form "{ResourceIdUri/.default}" for instance <c>https://management.azure.net/.default</c> or, for Microsoft
        /// Graph, <c>https://graph.microsoft.com/.default</c> as the requested scopes are defined statically with the application registration
        /// in the portal, and cannot be overridden in the application, as you can request a token for only one resource at a time (use
        /// several calls to get tokens for other resources).</param>
        /// <param name="authenticationScheme">Authentication scheme. If null, will use OpenIdConnectDefault.AuthenticationScheme
        /// if called from a web app, and JwtBearerDefault.AuthenticationScheme if called from a web API.</param>
        /// <param name="tenant">Enables overriding of the tenant/account for the same identity. This is useful
        /// for multi tenant apps or daemons.</param>
        /// <param name="tokenAcquisitionOptions">Options passed-in to create the token acquisition object which calls into MSAL .NET.</param>
        /// <returns>An authentication result for the app itself, based on its scopes.</returns>
        Task<AuthenticationResult> GetAuthenticationResultForAppAsync(
            string scope,
            string? authenticationScheme,
            string? tenant = null,
            TokenAcquisitionOptions? tokenAcquisitionOptions = null);

#if !NETSTANDARD2_0 && !NET462 && !NET472
        /// <summary>
        /// Used in web APIs (which therefore cannot have an interaction with the user).
        /// Replies to the client through the HttpResponse by sending a 403 (forbidden) and populating wwwAuthenticateHeaders so that
        /// the client can trigger an interaction with the user so the user can consent to more scopes.
        /// </summary>
        /// <param name="scopes">Scopes to consent to.</param>
        /// <param name="msalServiceException"><see cref="MsalUiRequiredException"/> triggering the challenge.</param>
        /// <param name="httpResponse">The <see cref="HttpResponse"/> to update.</param>
        [Obsolete(IDWebErrorMessage.ReplyForbiddenWithWwwAuthenticateHeaderIsObsolete, false)]
        void ReplyForbiddenWithWwwAuthenticateHeader(
        IEnumerable<string> scopes,
        MsalUiRequiredException msalServiceException,
        HttpResponse? httpResponse = null)
        {
            ReplyForbiddenWithWwwAuthenticateHeader(
                scopes,
                msalServiceException,
                null,
                httpResponse);
        }

        /// <summary>
        /// Used in web APIs (which therefore cannot have an interaction with the user).
        /// Replies to the client through the HttpResponse by sending a 403 (forbidden) and populating wwwAuthenticateHeaders so that
        /// the client can trigger an interaction with the user so the user can consent to more scopes.
        /// </summary>
        /// <param name="scopes">Scopes to consent to.</param>
        /// <param name="msalServiceException"><see cref="MsalUiRequiredException"/> triggering the challenge.</param>
        /// <param name="authenticationScheme">Authentication scheme. If null, will use OpenIdConnectDefault.AuthenticationScheme
        /// if called from a web app, and JwtBearerDefault.AuthenticationScheme if called from a web API.</param>
        /// <param name="httpResponse">The <see cref="HttpResponse"/> to update.</param>
        [Obsolete(IDWebErrorMessage.ReplyForbiddenWithWwwAuthenticateHeaderIsObsolete, false)]
        void ReplyForbiddenWithWwwAuthenticateHeader(
        IEnumerable<string> scopes,
        MsalUiRequiredException msalServiceException,
        string? authenticationScheme,
        HttpResponse? httpResponse = null);
#endif

        /// <summary>
        /// Get the effective authentication scheme based on the context.
        /// </summary>
        /// <param name="authenticationScheme">Proposed authentication scheme.</param>
        /// <returns>Effective authenticationScheme which is the authentication scheme
        /// if it's not null, or otherwise OpenIdConnectDefault.AuthenticationScheme
        /// if called from a web app, and JwtBearerDefault.AuthenticationScheme if called from a web API.</returns>
        string GetEffectiveAuthenticationScheme(string? authenticationScheme);

#if !NETSTANDARD2_0 && !NET462 && !NET472
        /// <summary>
        /// Used in web APIs (which therefore cannot have an interaction with the user).
        /// Replies to the client through the HttpResponse by sending a 403 (forbidden) and populating wwwAuthenticateHeaders so that
        /// the client can trigger an interaction with the user so the user can consent to more scopes.
        /// </summary>
        /// <param name="scopes">Scopes to consent to.</param>
        /// <param name="msalServiceException"><see cref="MsalUiRequiredException"/> triggering the challenge.</param>
        /// <param name="httpResponse">The <see cref="HttpResponse"/> to update.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task ReplyForbiddenWithWwwAuthenticateHeaderAsync(
        IEnumerable<string> scopes,
        MsalUiRequiredException msalServiceException,
        HttpResponse? httpResponse = null);
        
        /// <summary>
        /// Used in web APIs (which therefore cannot have an interaction with the user).
        /// Replies to the client through the HttpResponse by sending a 403 (forbidden) and populating wwwAuthenticateHeaders so that
        /// the client can trigger an interaction with the user so the user can consent to more scopes.
        /// </summary>
        /// <param name="scopes">Scopes to consent to.</param>
        /// <param name="msalServiceException"><see cref="MsalUiRequiredException"/> triggering the challenge.</param>
        /// <param name="authenticationScheme">Authentication scheme. If null, will use OpenIdConnectDefault.AuthenticationScheme
        /// if called from a web app, and JwtBearerDefault.AuthenticationScheme if called from a web API.</param>
        /// <param name="httpResponse">The <see cref="HttpResponse"/> to update.</param>
        Task ReplyForbiddenWithWwwAuthenticateHeaderAsync(
            IEnumerable<string> scopes,
            MsalUiRequiredException msalServiceException,
            string? authenticationScheme,
            HttpResponse? httpResponse = null);
#endif
    }
}
