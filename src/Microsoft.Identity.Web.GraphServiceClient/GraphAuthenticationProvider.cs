// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Abstractions;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Authentication;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Authentication provider for Microsoft Graph, based on IAuthorizationHeaderProvider. This is richer
    /// than <see cref="BaseBearerTokenAuthenticationProvider"/> which only supports the bearer protocol.
    /// </summary>
    internal class GraphAuthenticationProvider : IAuthenticationProvider
    {
        const string ScopeKey = "scopes";
        private const string AuthorizationHeaderKey = "Authorization";
        private const string AuthorizationHeaderProviderOptionsKey = "authorizationHeaderProviderOptions";
        readonly IAuthorizationHeaderProvider _authorizationHeaderProvider;
        readonly GraphServiceClientOptions _defaultAuthenticationOptions;

        /// <summary>
        /// Constructor from the authorization header provider.
        /// </summary>
        /// <param name="authorizationHeaderProvider"></param>
        /// <param name="defaultAuthenticationOptions"></param>
        public GraphAuthenticationProvider(IAuthorizationHeaderProvider authorizationHeaderProvider,
            GraphServiceClientOptions defaultAuthenticationOptions)
        {
            _authorizationHeaderProvider = authorizationHeaderProvider;
            _defaultAuthenticationOptions = defaultAuthenticationOptions;
        }

        /// <summary>
        /// Method that performs the authentication, and adds the right headers to the request.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="additionalAuthenticationContext"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public async Task AuthenticateRequestAsync(
            RequestInformation request,
            Dictionary<string, object>? additionalAuthenticationContext = null,
            CancellationToken cancellationToken = default)
        {
            _ = Throws.IfNull(request);

            // Attempts to get the scopes
            IEnumerable<string>? scopes;
            GraphServiceClientOptions? graphServiceClientOptions;

            GraphAuthenticationOptions? authenticationOptions = request.RequestOptions.OfType<GraphAuthenticationOptions>().FirstOrDefault();

            scopes = authenticationOptions?.Scopes ?? _defaultAuthenticationOptions.Scopes;
            graphServiceClientOptions = authenticationOptions ?? _defaultAuthenticationOptions;

            // Remove the authorization header if it exists
            if (request.Headers.ContainsKey(AuthorizationHeaderKey))
            {
                request.Headers.Remove(AuthorizationHeaderKey);
            }

            // Data coming from the request (needed in protocols like "Pop")
            AuthorizationHeaderProviderOptions? authorizationHeaderProviderOptions;
            if (string.Compare(graphServiceClientOptions?.ProtocolScheme, "bearer", StringComparison.OrdinalIgnoreCase) == 0)
            {
                authorizationHeaderProviderOptions = new AuthorizationHeaderProviderOptions(graphServiceClientOptions!);
                authorizationHeaderProviderOptions.BaseUrl = request.URI.Host;
                authorizationHeaderProviderOptions.RelativePath = request.URI.LocalPath;
                authorizationHeaderProviderOptions.HttpMethod = GetHttpMethod(request.HttpMethod);
            }
            else
            {
                authorizationHeaderProviderOptions = graphServiceClientOptions;
            }

            // Add the authorization header
            if (!request.Headers.ContainsKey(AuthorizationHeaderKey))
            {
                string authorizationHeader;
                if (authorizationHeaderProviderOptions!.RequestAppToken)
                {
                    authorizationHeader = await _authorizationHeaderProvider.CreateAuthorizationHeaderForAppAsync("https://graph.microsoft.com/.default",
                        authorizationHeaderProviderOptions);
                }
                else
                {
                    authorizationHeader = await _authorizationHeaderProvider.CreateAuthorizationHeaderForUserAsync(
                         scopes!,
                         authorizationHeaderProviderOptions);
                }
                request.Headers.Add(AuthorizationHeaderKey, authorizationHeader);
            }
        }

        /// <summary>
        /// Transforms the Kiota HTTP Method (enum) into a .NET HttpMethod (static members).
        /// </summary>
        /// <param name="httpMethod">Kiota Http method</param>
        /// <returns>HttpMethod</returns>
        private HttpMethod GetHttpMethod(Method httpMethod)
        {
            switch (httpMethod)
            {
                case Method.GET:
                    return HttpMethod.Get;
                case Method.POST:
                    return HttpMethod.Post;
                case Method.PUT:
                    return HttpMethod.Put;
                case Method.PATCH:
#if NETFRAMEWORK || NETSTANDARD2_0
                    return HttpMethod.Put;
#else
                    return HttpMethod.Patch;
#endif
                case Method.DELETE:
                    return HttpMethod.Delete;
                case Method.OPTIONS:
                    return HttpMethod.Options;
                case Method.TRACE:
                    return HttpMethod.Trace;
                case Method.HEAD:
                    return HttpMethod.Head;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
