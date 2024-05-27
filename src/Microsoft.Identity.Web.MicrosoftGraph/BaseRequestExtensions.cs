// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Security.Claims;
using Microsoft.Graph;
using Microsoft.Identity.Abstractions;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Extension methods for Graph requests
    /// </summary>
    public static class BaseRequestExtensions
    {
        /// <summary>
        /// Sets Microsoft Graph's scopes that will be used by <see cref="IAuthenticationProvider"/> to authenticate this request
        /// and can be used to perform incremental scope consent.
        /// This only works with the default authentication handler and default set of Microsoft Graph authentication providers.
        /// If you use a custom authentication handler or authentication provider, you have to handle its retrieval in your implementation.
        /// </summary>
        /// <param name="baseRequest">The <see cref="IBaseRequest"/>.</param>
        /// <param name="scopes">Microsoft Graph scopes used to authenticate this request.</param>
        public static T WithScopes<T>(this T baseRequest, params string[] scopes) where T : IBaseRequest
        {
            return SetParameter(baseRequest, options => options.Scopes = scopes);
        }

        /// <summary>
        /// Applied to a request, expresses to use app only permissions for Graph.
        /// </summary>
        /// <typeparam name="T">Type of the request.</typeparam>
        /// <param name="baseRequest">Request.</param>
        /// <param name="appOnly">Should the permissions be app only or not.</param>
        /// <param name="tenant">Tenant ID or domain for which we want to make the call..</param>
        /// <returns></returns>
        public static T WithAppOnly<T>(this T baseRequest, bool appOnly = true, string? tenant = null) where T : IBaseRequest
        {
            return SetParameter(baseRequest, options =>
            {
                options.AppOnly = appOnly;
                options.Tenant = tenant;
            });
        }

        /// <summary>
        /// Sets the authentication scheme that will be used by <see cref="IAuthenticationProvider"/> to authenticate this request.
        /// This only works with the default authentication handler and default set of Microsoft Graph authentication providers.
        /// If you use a custom authentication handler or authentication provider, you have to handle its retrieval in your implementation.
        /// </summary>
        /// <param name="baseRequest">The <see cref="IBaseRequest"/>.</param>
        /// <param name="authenticationScheme">Authentication scheme used to authenticate this request.</param>
        public static T WithAuthenticationScheme<T>(this T baseRequest, string authenticationScheme) where T : IBaseRequest
        {
            return SetParameter(baseRequest, options => options.AuthenticationScheme = authenticationScheme);
        }

        /// <summary>
        /// Overrides authentication options for a given request.
        /// </summary>
        /// <typeparam name="T">Request</typeparam>
        /// <param name="baseRequest">Request.</param>
        /// <param name="overrideAuthenticationOptions">Delegate to override
        /// the authentication options</param>
        /// <returns>Base request</returns>
        public static T WithAuthenticationOptions<T>(this T baseRequest, 
            Action<AuthorizationHeaderProviderOptions> overrideAuthenticationOptions) where T : IBaseRequest
        {
            return SetParameter(baseRequest, options => options.AuthorizationHeaderProviderOptions = overrideAuthenticationOptions);
        }

        private static T SetParameter<T>(T baseRequest, Action<TokenAcquisitionAuthenticationProviderOption> action) where T : IBaseRequest
        {
            string authHandlerOptionKey = typeof(AuthenticationHandlerOption).FullName!;
            AuthenticationHandlerOption authHandlerOptions;

            try
            {
                authHandlerOptionKey = typeof(AuthenticationHandlerOption).Name!;
                authHandlerOptions = baseRequest.MiddlewareOptions[authHandlerOptionKey] as AuthenticationHandlerOption ?? new AuthenticationHandlerOption();
            }
            catch (Exception)
            {
                // This flow only exists with very old versions of the Microsoft Graph SDK
                authHandlerOptions = baseRequest.MiddlewareOptions[authHandlerOptionKey] as AuthenticationHandlerOption ?? new AuthenticationHandlerOption();
            }

            TokenAcquisitionAuthenticationProviderOption msalAuthProviderOption = authHandlerOptions?.AuthenticationProviderOption as TokenAcquisitionAuthenticationProviderOption ?? new TokenAcquisitionAuthenticationProviderOption();

            action(msalAuthProviderOption);

#pragma warning disable CS8602 // Dereference of a possibly null reference. The Graph SDK ensures it exists
            authHandlerOptions.AuthenticationProviderOption = msalAuthProviderOption;
#pragma warning restore CS8602 // Dereference of a possibly null reference.
            baseRequest.MiddlewareOptions[authHandlerOptionKey] = authHandlerOptions;

            return baseRequest;
        }

        /// <summary>
        /// Overrides authentication options for a given request.
        /// </summary>
        /// <typeparam name="T">Request</typeparam>
        /// <param name="baseRequest">Request.</param>
        /// <param name="user">Delegate to override
        /// the authentication options</param>
        /// <returns>Base request</returns>
        public static T WithUser<T>(this T baseRequest,
            ClaimsPrincipal user) where T : IBaseRequest
        {
            return SetParameter(baseRequest, options => options.User = user );
        }
    }
}
