// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
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
        private const string AuthorizationHeaderKey = "Authorization";
        readonly IAuthorizationHeaderProvider _authorizationHeaderProvider;
        readonly GraphServiceClientOptions _defaultAuthenticationOptions;
        private readonly string[] _graphUris = ["graph.microsoft.com", "graph.microsoft.us", "dod-graph.microsoft.us", "graph.microsoft.de", "microsoftgraph.chinacloudapi.cn", "canary.graph.microsoft.com", "graph.microsoft-ppe.com"];

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
            ClaimsPrincipal? user = authenticationOptions?.User;

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

            AllowedHostsValidator allowedHostsValidator = new(_graphUris);
            // Add the authorization header
            if (allowedHostsValidator.IsUrlHostValid(request.URI) && !request.Headers.ContainsKey(AuthorizationHeaderKey))
            {
                string authorizationHeader;
                if (authorizationHeaderProviderOptions!.RequestAppToken)
                {
                    authorizationHeader = await _authorizationHeaderProvider.CreateAuthorizationHeaderForAppAsync("https://graph.microsoft.com/.default",
                        authorizationHeaderProviderOptions,
                        cancellationToken);
                }
                else
                {
                    authorizationHeader = await _authorizationHeaderProvider.CreateAuthorizationHeaderForUserAsync(
                         scopes!,
                         authorizationHeaderProviderOptions,
                         claimsPrincipal: user,
                         cancellationToken);
                }
                request.Headers.Add(AuthorizationHeaderKey, authorizationHeader);
            }
        }

        /// <summary>
        /// Transforms the Kiota HTTP Method (enum) into a string representing a .NET HttpMethod (static members).
        /// </summary>
        /// <param name="httpMethod">Kiota Http method</param>
        /// <returns>string</returns>
        private string GetHttpMethod(Method httpMethod)
        {
            switch (httpMethod)
            {
                case Method.GET:
                    return HttpMethod.Get.ToString();
                case Method.POST:
                    return HttpMethod.Post.ToString();
                case Method.PUT:
                    return HttpMethod.Put.ToString();
                case Method.PATCH:
#if NETFRAMEWORK || NETSTANDARD2_0
                    return HttpMethod.Put.ToString();
#else
                    return HttpMethod.Patch.ToString();
#endif
                case Method.DELETE:
                    return HttpMethod.Delete.ToString();
                case Method.OPTIONS:
                    return HttpMethod.Options.ToString();
                case Method.TRACE:
                    return HttpMethod.Trace.ToString();
                case Method.HEAD:
                    return HttpMethod.Head.ToString();
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
