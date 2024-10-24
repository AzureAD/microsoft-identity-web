// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Graph;
using Microsoft.Identity.Abstractions;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Authentication provider based on ITokenAcquisition.
    /// </summary>
    internal class TokenAcquisitionAuthenticationProvider : IAuthenticationProvider
    {
        public TokenAcquisitionAuthenticationProvider(IAuthorizationHeaderProvider authorizationHeaderProvider, TokenAcquisitionAuthenticationProviderOption options)
        { 
            _authorizationHeaderProvider = authorizationHeaderProvider;
            _initialOptions = options;
        }

        private readonly IAuthorizationHeaderProvider _authorizationHeaderProvider;
        private readonly TokenAcquisitionAuthenticationProviderOption _initialOptions;
        private readonly IEnumerable<string> _defaultGraphScope = ["https://graph.microsoft.com/.default"];

        /// <summary>
        /// Adds an authorization header to an HttpRequestMessage.
        /// </summary>
        /// <param name="request">HttpRequest message to authenticate.</param>
        /// <returns>A Task (as this is an async method).</returns>
        public async Task AuthenticateRequestAsync(HttpRequestMessage request)
        {
            // Default options to settings provided during initialization
            var scopes = _initialOptions.Scopes;
            bool appOnly = _initialOptions.AppOnly ?? false;
            string? tenant = _initialOptions.Tenant ?? null;
            string? scheme = _initialOptions.AuthenticationScheme ?? null;
            ClaimsPrincipal? user = null;
            // Extract per-request options from the request if present
            TokenAcquisitionAuthenticationProviderOption? msalAuthProviderOption = GetMsalAuthProviderOption(request);
            if (msalAuthProviderOption != null) {
                scopes = msalAuthProviderOption.Scopes ?? scopes;
                appOnly = msalAuthProviderOption.AppOnly ?? appOnly;
                tenant = msalAuthProviderOption.Tenant ?? tenant;
                scheme = msalAuthProviderOption.AuthenticationScheme ?? scheme;
                user = msalAuthProviderOption.User ?? user;
            }

            if (!appOnly && scopes == null)
            {
                throw new InvalidOperationException(IDWebErrorMessage.ScopesRequiredToCallMicrosoftGraph);
            }

            DownstreamApiOptions? downstreamOptions = new DownstreamApiOptions() { BaseUrl = "https://graph.microsoft.com", Scopes = scopes };
            downstreamOptions.AcquireTokenOptions.AuthenticationOptionsName = scheme;
            downstreamOptions.AcquireTokenOptions.Tenant = tenant;
            downstreamOptions.RequestAppToken = appOnly;

            if (msalAuthProviderOption?.AuthorizationHeaderProviderOptions != null)
            {
                msalAuthProviderOption.AuthorizationHeaderProviderOptions(downstreamOptions);
            }

            string authorizationHeader = await _authorizationHeaderProvider.CreateAuthorizationHeaderAsync(
                    appOnly ? _defaultGraphScope : scopes!,
                    downstreamOptions,
                    user).ConfigureAwait(false);

            // add or replace authorization header
            if (request.Headers.Contains(Constants.Authorization))
            {
                request.Headers.Remove(Constants.Authorization);
            }

            request.Headers.Add(
                Constants.Authorization, authorizationHeader);

            downstreamOptions?.CustomizeHttpRequestMessage?.Invoke(request);
        }

        /// <summary>
        /// Extract TokenAcquisitionAuthenticationProviderOption from request.Properties if it is present
        /// </summary>
        /// <param name="httpRequestMessage">Current request message</param>
        /// <returns>Options set for just this request.</returns>
        private TokenAcquisitionAuthenticationProviderOption? GetMsalAuthProviderOption(HttpRequestMessage httpRequestMessage)
        {
            AuthenticationHandlerOption authHandlerOption = httpRequestMessage.GetMiddlewareOption<AuthenticationHandlerOption>();

            return authHandlerOption?.AuthenticationProviderOption as TokenAcquisitionAuthenticationProviderOption;
        }
    }
}
