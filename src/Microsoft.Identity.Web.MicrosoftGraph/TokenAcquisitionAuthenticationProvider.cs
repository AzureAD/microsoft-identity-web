// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Claims;
using System.Threading;
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

        internal TokenAcquisitionAuthenticationProvider(
            IAuthorizationHeaderProvider authorizationHeaderProvider,
            TokenAcquisitionAuthenticationProviderOption options,
            string baseUrl)
            : this(authorizationHeaderProvider, options)
        {
            BindBaseUrl(baseUrl);
        }

        private readonly IAuthorizationHeaderProvider _authorizationHeaderProvider;
        private readonly TokenAcquisitionAuthenticationProviderOption _initialOptions;
        private readonly IEnumerable<string> _defaultGraphScope = ["https://graph.microsoft.com/.default"];
        private GraphOrigin? _graphOrigin;

        /// <summary>
        /// Adds an authorization header to an HttpRequestMessage.
        /// </summary>
        /// <param name="request">HttpRequest message to authenticate.</param>
        /// <returns>A Task (as this is an async method).</returns>
        public async Task AuthenticateRequestAsync(HttpRequestMessage request)
        {
            _ = Throws.IfNull(request);

            if (request.Headers.Contains(Constants.Authorization))
            {
                request.Headers.Remove(Constants.Authorization);
            }

            if (!IsRequestUriAllowed(request.RequestUri))
            {
                return;
            }

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

            request.Headers.Add(
                Constants.Authorization, authorizationHeader);

            try
            {
                downstreamOptions.CustomizeHttpRequestMessage?.Invoke(request);
                if (!IsRequestUriAllowed(request.RequestUri))
                {
                    request.Headers.Remove(Constants.Authorization);
                }
            }
            catch
            {
                request.Headers.Remove(Constants.Authorization);
                throw;
            }
        }

        internal void BindBaseUrl(string? baseUrl)
        {
            GraphOrigin graphOrigin = GraphOrigin.Create(baseUrl);
            if (Interlocked.CompareExchange(ref _graphOrigin, graphOrigin, null) is not null)
            {
                throw new InvalidOperationException("The Microsoft Graph base URL has already been bound.");
            }
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

        private bool IsRequestUriAllowed(Uri? requestUri)
        {
            GraphOrigin? graphOrigin = Volatile.Read(ref _graphOrigin);
            return graphOrigin is not null && graphOrigin.Matches(requestUri);
        }

        private sealed class GraphOrigin
        {
            private GraphOrigin(string host, int port)
            {
                Host = host;
                Port = port;
            }

            private string Host { get; }

            private int Port { get; }

            internal static GraphOrigin Create(string? baseUrl)
            {
                if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out Uri? uri)
                    || !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
                    || string.IsNullOrEmpty(uri.Host))
                {
                    throw new ArgumentException(
                        "The Microsoft Graph base URL must be an absolute HTTPS URI.",
                        nameof(baseUrl));
                }

                return new GraphOrigin(uri.IdnHost, uri.Port);
            }

            internal bool Matches(Uri? uri)
            {
                return uri is not null
                    && uri.IsAbsoluteUri
                    && string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(uri.IdnHost, Host, StringComparison.OrdinalIgnoreCase)
                    && uri.Port == Port;
            }
        }
    }
}
