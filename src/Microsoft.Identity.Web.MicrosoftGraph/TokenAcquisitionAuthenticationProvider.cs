// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net.Http;
using Microsoft.Graph;
using Microsoft.Identity.Client;

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

        private readonly ITokenAcquisition _tokenAcquisition;
        private readonly TokenAcquisitionAuthenticationProviderOption _initialOptions;

        /// <summary>
        /// Adds an authorization header to an HttpRequestMessage.
        /// </summary>
        /// <param name="request">HttpRequest message to authenticate.</param>
        /// <returns>A Task (as this is an async method).</returns>
        public async System.Threading.Tasks.Task AuthenticateRequestAsync(HttpRequestMessage request)
        {
            // Default options to settings provided during intialization
            var scopes = _initialOptions.Scopes;
            bool appOnly = _initialOptions.AppOnly ?? false;
            string? tenant = _initialOptions.Tenant ?? null;
            string? scheme = _initialOptions.AuthenticationScheme ?? null;
            // Extract per-request options from the request if present
            TokenAcquisitionAuthenticationProviderOption? msalAuthProviderOption = GetMsalAuthProviderOption(request);
            if (msalAuthProviderOption != null) {
                scopes = msalAuthProviderOption.Scopes ?? scopes;
                appOnly = msalAuthProviderOption.AppOnly ?? appOnly;
                tenant = msalAuthProviderOption.Tenant ?? tenant;
                scheme = msalAuthProviderOption.AuthenticationScheme ?? scheme;
            }

            if (!appOnly && scopes == null)
            {
                throw new InvalidOperationException(IDWebErrorMessage.ScopesRequiredToCallMicrosoftGraph);
            }

            AuthenticationResult authenticationResult;
            if (appOnly)
            {
                authenticationResult = await _tokenAcquisition.GetAuthenticationResultForAppAsync(
                    Constants.DefaultGraphScope,
                    authenticationScheme: scheme,
                    tenant: tenant).ConfigureAwait(false);
            }
            else
            {
                authenticationResult = await _tokenAcquisition.GetAuthenticationResultForUserAsync(
                    scopes!,
                    tenantId: tenant,
                    authenticationScheme: scheme).ConfigureAwait(false);
            }

            // add or replace authorization header
            if (request.Headers.Contains(Constants.Authorization))
            {
                request.Headers.Remove(Constants.Authorization);
            }

            request.Headers.Add(
                Constants.Authorization,
                authenticationResult.CreateAuthorizationHeader());
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
