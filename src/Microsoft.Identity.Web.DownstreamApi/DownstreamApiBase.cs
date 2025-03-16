// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Client;

namespace Microsoft.Identity.Web
{
    public abstract class DownstreamApiBase
    {
        private readonly IAuthorizationHeaderProvider _authorizationHeaderProvider;
        private readonly IHttpClientFactory _httpClientFactory;
        protected readonly ILogger _logger;
        private const string Authorization = "Authorization";
        private const string AuthSchemeDstsSamlBearer = "http://schemas.microsoft.com/dsts/saml2-bearer";

        /// <summary>
        /// Initializes a new instance of the <see cref="DownstreamApiBase"/> class.
        /// </summary>
        /// <param name="authorizationHeaderProvider">The provider for authorization headers.</param>
        /// <param name="httpClientFactory">The factory for creating HTTP clients.</param>
        /// <param name="logger">The logger instance.</param>
        protected DownstreamApiBase(
            IAuthorizationHeaderProvider authorizationHeaderProvider,
            IHttpClientFactory httpClientFactory,
            ILogger logger)
        {
            _authorizationHeaderProvider = authorizationHeaderProvider;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        /// <summary>
        /// Calls the downstream API asynchronously.
        /// </summary>
        /// <param name="serviceName">The name of the service.</param>
        /// <param name="effectiveOptions">The options for the downstream API call.</param>
        /// <param name="appToken">Indicates whether to use an app token.</param>
        /// <param name="content">The HTTP content to send with the request.</param>
        /// <param name="user">The claims principal representing the user.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The HTTP response message.</returns>
        protected virtual async Task<HttpResponseMessage> CallApiInternalAsync(
            string? serviceName,
            DownstreamApiOptions effectiveOptions,
            bool appToken,
            HttpContent? content = null,
            ClaimsPrincipal? user = null,
            CancellationToken cancellationToken = default)
        {
            // Downstream API URI
            string apiUrl = effectiveOptions.GetApiUrl();

            // Create an HTTP request message
            using HttpRequestMessage httpRequestMessage = new(
                new HttpMethod(effectiveOptions.HttpMethod),
                apiUrl);

            await UpdateRequestAsync(httpRequestMessage, content, effectiveOptions, appToken, user, cancellationToken);

            using HttpClient client = string.IsNullOrEmpty(serviceName) ? _httpClientFactory.CreateClient() : _httpClientFactory.CreateClient(serviceName);

            // Send the HTTP message
            var downstreamApiResult = await client.SendAsync(httpRequestMessage, cancellationToken).ConfigureAwait(false);

            // Retry only if the resource sent 401 Unauthorized with WWW-Authenticate header and claims
            if (downstreamApiResult.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                effectiveOptions.AcquireTokenOptions.Claims = WwwAuthenticateParameters.GetClaimChallengeFromResponseHeaders(downstreamApiResult.Headers);

                if (!string.IsNullOrEmpty(effectiveOptions.AcquireTokenOptions.Claims))
                {
                    using HttpRequestMessage retryHttpRequestMessage = new(
                        new HttpMethod(effectiveOptions.HttpMethod),
                        apiUrl);

                    await UpdateRequestAsync(retryHttpRequestMessage, content, effectiveOptions, appToken, user, cancellationToken);

                    return await client.SendAsync(retryHttpRequestMessage, cancellationToken).ConfigureAwait(false);
                }
            }

            return downstreamApiResult;
        }

        /// <summary>
        /// Updates the HTTP request message asynchronously.
        /// </summary>
        /// <param name="httpRequestMessage">The HTTP request message to update.</param>
        /// <param name="content">The HTTP content to send with the request.</param>
        /// <param name="effectiveOptions">The options for the downstream API call.</param>
        /// <param name="appToken">Indicates whether to use an app token.</param>
        /// <param name="user">The claims principal representing the user.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        internal async Task UpdateRequestAsync(
            HttpRequestMessage httpRequestMessage,
            HttpContent? content,
            DownstreamApiOptions effectiveOptions,
            bool appToken,
            ClaimsPrincipal? user,
            CancellationToken cancellationToken)
        {
            AddCallerSDKTelemetry(effectiveOptions);

            if (content != null)
            {
                httpRequestMessage.Content = content;
            }

            effectiveOptions.RequestAppToken = appToken;

            // Obtention of the authorization header (except when calling an anonymous endpoint
            // which is done by not specifying any scopes
            if (effectiveOptions.Scopes != null && effectiveOptions.Scopes.Any())
            {
                string authorizationHeader = await _authorizationHeaderProvider.CreateAuthorizationHeaderAsync(
                       effectiveOptions.Scopes,
                       effectiveOptions,
                       user,
                       cancellationToken).ConfigureAwait(false);

                if (authorizationHeader.StartsWith(AuthSchemeDstsSamlBearer, StringComparison.OrdinalIgnoreCase))
                {
                    // TryAddWithoutValidation method bypasses strict validation, allowing non-standard headers to be added for custom Header schemes that cannot be parsed.
                    httpRequestMessage.Headers.TryAddWithoutValidation(Authorization, authorizationHeader);
                }
                else
                {
                    httpRequestMessage.Headers.Add(Authorization, authorizationHeader);
                }
            }
            else
            {
                DownstreamApi.Logger.UnauthenticatedApiCall(_logger, null);
            }
            if (!string.IsNullOrEmpty(effectiveOptions.AcceptHeader))
            {
                httpRequestMessage.Headers.Accept.ParseAdd(effectiveOptions.AcceptHeader);
            }
            // Opportunity to change the request message
            effectiveOptions.CustomizeHttpRequestMessage?.Invoke(httpRequestMessage);
        }

        /// <summary>
        /// Gets the caller SDK details.
        /// </summary>
        internal static Dictionary<string, string> CallerSDKDetails { get; } = new()
        {
            { "caller-sdk-id", "IdWeb_1" },
            { "caller-sdk-ver", IdHelper.GetIdWebVersion() }
        };

        private static void AddCallerSDKTelemetry(DownstreamApiOptions effectiveOptions)
        {
            if (effectiveOptions.AcquireTokenOptions.ExtraQueryParameters == null)
            {
                effectiveOptions.AcquireTokenOptions.ExtraQueryParameters = CallerSDKDetails;
            }
            else
            {
                effectiveOptions.AcquireTokenOptions.ExtraQueryParameters["caller-sdk-id"] =
                    CallerSDKDetails["caller-sdk-id"];
                effectiveOptions.AcquireTokenOptions.ExtraQueryParameters["caller-sdk-ver"] =
                    CallerSDKDetails["caller-sdk-ver"];
            }
        }
    }
}
