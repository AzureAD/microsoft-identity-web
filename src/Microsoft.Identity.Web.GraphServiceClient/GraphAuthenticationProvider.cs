// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
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
            GraphServiceClientOptions? authorizationHeaderProviderOptions;

            if (additionalAuthenticationContext != null)
            {
                scopes = additionalAuthenticationContext.ContainsKey(ScopeKey) ? (string[])additionalAuthenticationContext[ScopeKey] : _defaultAuthenticationOptions?.Scopes;
                authorizationHeaderProviderOptions = additionalAuthenticationContext.ContainsKey(AuthorizationHeaderProviderOptionsKey) ?
                 (GraphServiceClientOptions)additionalAuthenticationContext[AuthorizationHeaderProviderOptionsKey] :
                 _defaultAuthenticationOptions;
            }
            else
            {
                scopes = _defaultAuthenticationOptions.Scopes;
                authorizationHeaderProviderOptions = _defaultAuthenticationOptions;
            }

            if (request.Headers.ContainsKey(AuthorizationHeaderKey))
            {
                request.Headers.Remove(AuthorizationHeaderKey);
            }

            
            if (!request.Headers.ContainsKey(AuthorizationHeaderKey))
            {
                string authorizationHeader;
                if (authorizationHeaderProviderOptions!.RequestAppToken)
                {
                    authorizationHeader = await _authorizationHeaderProvider.CreateAuthorizationHeaderForAppAsync(scopes!.FirstOrDefault()!,
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
    }
}
