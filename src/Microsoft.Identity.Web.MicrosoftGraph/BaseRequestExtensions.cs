using System;
using Microsoft.Graph;

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
        /// This only works with the default authentication handler and default set of Microsoft graph authentication providers.
        /// If you use a custom authentication handler or authentication provider, you have to handle it's retrieval in your implementation.
        /// </summary>
        /// <param name="baseRequest">The <see cref="IBaseRequest"/>.</param>
        /// <param name="scopes">Microsoft graph scopes used to authenticate this request.</param>
        public static T WithScopes<T>(this T baseRequest, params string[] scopes) where T : IBaseRequest
        {
            return SetParameter(baseRequest, options => options.Scopes = scopes);
        }

        /// <summary>
        /// Applied to a request, expresses to use App only permissions for Graph
        /// </summary>
        /// <typeparam name="T">Type of the request</typeparam>
        /// <param name="baseRequest">Request</param>
        /// <param name="appOnly">Should the permissions be app only or not</param>
        /// <returns></returns>
        public static T WithAppOnly<T>(this T baseRequest, bool appOnly = true) where T : IBaseRequest
        {
            return SetParameter(baseRequest, options => options.AppOnly = appOnly);
        }

        private static T SetParameter<T>(T baseRequest, Action<TokenAcquisitionAuthenticationProviderOption> action) where T : IBaseRequest
        {
            string authHandlerOptionKey = typeof(AuthenticationHandlerOption).ToString();
            AuthenticationHandlerOption authHandlerOptions = baseRequest.MiddlewareOptions[authHandlerOptionKey] as AuthenticationHandlerOption ?? new AuthenticationHandlerOption();
            TokenAcquisitionAuthenticationProviderOption msalAuthProviderOption = authHandlerOptions?.AuthenticationProviderOption as TokenAcquisitionAuthenticationProviderOption ?? new TokenAcquisitionAuthenticationProviderOption();

            action(msalAuthProviderOption);

#pragma warning disable CS8602 // Dereference of a possibly null reference. The Graph SDK ensures it exists
            authHandlerOptions.AuthenticationProviderOption = msalAuthProviderOption;
#pragma warning restore CS8602 // Dereference of a possibly null reference.
            baseRequest.MiddlewareOptions[authHandlerOptionKey] = authHandlerOptions;

            return baseRequest;
        }
    }
}
