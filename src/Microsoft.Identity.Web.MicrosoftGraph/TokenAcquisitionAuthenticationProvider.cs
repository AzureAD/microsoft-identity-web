// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Graph;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Authentication provider based on ITokenAcquisition.
    /// </summary>
    internal class TokenAcquisitionAuthenticationProvider : IAuthenticationProvider
    {
        public TokenAcquisitionAuthenticationProvider(ITokenAcquisition tokenAcquisition, TokenAcquisitionAuthenticationProviderOption options)
        {
            _tokenAcquisition = tokenAcquisition;
            _initialOptions = options;
        }

        private ITokenAcquisition _tokenAcquisition;
        private TokenAcquisitionAuthenticationProviderOption _initialOptions;

        /// <summary>
        /// Adds an authorization header to an HttpRequestMessage.
        /// </summary>
        /// <param name="request">HttpRequest message to authenticate.</param>
        /// <returns>A Task (as this is an async method).</returns>
        public async Task AuthenticateRequestAsync(HttpRequestMessage request)
        {
            // Default options to settings provided during intialization
            var scopes = _initialOptions.Scopes;
            bool appOnly = _initialOptions.AppOnly ?? false;
            // Extract per-request options from the request if present
            TokenAcquisitionAuthenticationProviderOption? msalAuthProviderOption = GetMsalAuthProviderOption(request);
            if (msalAuthProviderOption != null) {
                scopes = msalAuthProviderOption.Scopes ?? scopes;
                appOnly = msalAuthProviderOption.AppOnly ?? appOnly;
            }

            if (scopes == null)
            {
                throw new ArgumentNullException(
                    Constants.Scopes,
                    IDWebErrorMessage.ScopesRequiredToCallMicrosoftGraph);
            }

            string token;
            if (appOnly)
            {
                token = await _tokenAcquisition.GetAccessTokenForAppAsync(Constants.DefaultGraphScope).ConfigureAwait(false);
            }
            else
            {
                token = await _tokenAcquisition.GetAccessTokenForUserAsync(scopes).ConfigureAwait(false);
            } 

            request.Headers.Authorization = new AuthenticationHeaderValue(Constants.Bearer, token);
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
