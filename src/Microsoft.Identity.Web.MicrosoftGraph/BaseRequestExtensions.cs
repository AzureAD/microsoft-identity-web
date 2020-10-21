using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Graph;

namespace Microsoft.Identity.Web
{
    public static class BaseRequestExtensions
    {
        /// <summary>
        /// Sets Microsoft Graph's scopes that will be used by <see cref="IAuthenticationProvider"/> to authenticate this request
        /// and can be used to perform incremental scope consent.
        /// This only works with the default authentication handler and default set of Microsoft graph authentication providers.
        /// If you use a custom authentication handler or authentication provider, you have to handle it's retrieval in your implementation.
        /// </summary>
        /// <param name="baseRequest">The <see cref="IBaseRequest"/>.</param>
        /// <param name="scopes">Microsoft graph scopes used to authenticate this request.</param>
        public static T WithScopes<T>(this T baseRequest, string[] scopes) where T : IBaseRequest
        {
            string authHandlerOptionKey = typeof(TokenAcquisitionAuthenticationProviderOption).ToString();
            AuthenticationHandlerOption authHandlerOptions = baseRequest.MiddlewareOptions[authHandlerOptionKey] as AuthenticationHandlerOption ?? new AuthenticationHandlerOption();
            TokenAcquisitionAuthenticationProviderOption msalAuthProviderOption = authHandlerOptions?.AuthenticationProviderOption as TokenAcquisitionAuthenticationProviderOption ?? new TokenAcquisitionAuthenticationProviderOption();

            msalAuthProviderOption.Scopes = scopes;

            authHandlerOptions.AuthenticationProviderOption = msalAuthProviderOption;
            baseRequest.MiddlewareOptions[authHandlerOptionKey] = authHandlerOptions;

            return baseRequest;
        }

        public static T WithAppOnly<T>(this T baseRequest, bool appOnly = true) where T : IBaseRequest
        {
            string authHandlerOptionKey = typeof(TokenAcquisitionAuthenticationProviderOption).ToString();
            AuthenticationHandlerOption authHandlerOptions = baseRequest.MiddlewareOptions[authHandlerOptionKey] as AuthenticationHandlerOption ?? new AuthenticationHandlerOption();
            TokenAcquisitionAuthenticationProviderOption msalAuthProviderOption = authHandlerOptions?.AuthenticationProviderOption as TokenAcquisitionAuthenticationProviderOption ?? new TokenAcquisitionAuthenticationProviderOption();

            msalAuthProviderOption.AppOnly = appOnly;

            authHandlerOptions.AuthenticationProviderOption = msalAuthProviderOption;
            baseRequest.MiddlewareOptions[authHandlerOptionKey] = authHandlerOptions;

            return baseRequest;
        }
    }
}
