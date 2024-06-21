// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Client;

namespace Microsoft.Identity.Web
{
    /// <inheritdoc/>
    internal partial class DownstreamApi : IDownstreamApi
    {
        private readonly IAuthorizationHeaderProvider _authorizationHeaderProvider;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IOptionsMonitor<DownstreamApiOptions> _namedDownstreamApiOptions;
        private const string Authorization = "Authorization";
        protected readonly ILogger<DownstreamApi> _logger;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="authorizationHeaderProvider">Authorization header provider.</param>
        /// <param name="namedDownstreamApiOptions">Named options provider.</param>
        /// <param name="httpClientFactory">HTTP client factory.</param>
        /// <param name="logger">Logger.</param>
        public DownstreamApi(
            IAuthorizationHeaderProvider authorizationHeaderProvider,
            IOptionsMonitor<DownstreamApiOptions> namedDownstreamApiOptions,
            IHttpClientFactory httpClientFactory,
            ILogger<DownstreamApi> logger)
        {
            _authorizationHeaderProvider = authorizationHeaderProvider;
            _namedDownstreamApiOptions = namedDownstreamApiOptions;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Task<HttpResponseMessage> CallApiAsync(
            string? serviceName,
            Action<DownstreamApiOptions>? downstreamApiOptionsOverride = null,
            ClaimsPrincipal? user = null,
            HttpContent? content = null,
            CancellationToken cancellationToken = default)
        {
            DownstreamApiOptions effectiveOptions = MergeOptions(serviceName, downstreamApiOptionsOverride);
            return CallApiInternalAsync(serviceName, effectiveOptions, effectiveOptions.RequestAppToken, content,
                                            user, cancellationToken);
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Task<HttpResponseMessage> CallApiAsync(
            DownstreamApiOptions downstreamApiOptions,
            ClaimsPrincipal? user = null,
            HttpContent? content = null,
            CancellationToken cancellationToken = default)
        {
            return CallApiInternalAsync(null, downstreamApiOptions, downstreamApiOptions.RequestAppToken, content,
                                user, cancellationToken);
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Task<HttpResponseMessage> CallApiForUserAsync(
            string? serviceName,
            Action<DownstreamApiOptions>? downstreamApiOptionsOverride = null,
            ClaimsPrincipal? user = null,
            HttpContent? content = null,
            CancellationToken cancellationToken = default)
        {
            DownstreamApiOptions effectiveOptions = MergeOptions(serviceName, downstreamApiOptionsOverride);
            return CallApiInternalAsync(serviceName, effectiveOptions, false, content, user, cancellationToken);
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Task<HttpResponseMessage> CallApiForAppAsync(
           string? serviceName,
           Action<DownstreamApiOptions>? downstreamApiOptionsOverride = null,
           HttpContent? content = null,
           CancellationToken cancellationToken = default)
        {
            DownstreamApiOptions effectiveOptions = MergeOptions(serviceName, downstreamApiOptionsOverride);
            return CallApiInternalAsync(serviceName, effectiveOptions, true, content, null, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<TOutput?> CallApiForUserAsync<TInput, TOutput>(
            string? serviceName,
            TInput input,
            Action<DownstreamApiOptions>? downstreamApiOptionsOverride = null,
            ClaimsPrincipal? user = null,
            CancellationToken cancellationToken = default) where TOutput : class
        {
            DownstreamApiOptions effectiveOptions = MergeOptions(serviceName, downstreamApiOptionsOverride);
            HttpContent? effectiveInput = SerializeInput(input, effectiveOptions);

            HttpResponseMessage response = await CallApiInternalAsync(serviceName, effectiveOptions, false,
                                                                          effectiveInput, user, cancellationToken).ConfigureAwait(false);

            // Only dispose the HttpContent if was created here, not provided by the caller.
            if (input is not HttpContent)
            {
                effectiveInput?.Dispose();
            }

            return await DeserializeOutput<TOutput>(response, effectiveOptions).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async Task<TOutput?> CallApiForAppAsync<TInput, TOutput>(
            string? serviceName,
            TInput input,
            Action<DownstreamApiOptions>? downstreamApiOptionsOverride = null,
            CancellationToken cancellationToken = default) where TOutput : class
        {
            DownstreamApiOptions effectiveOptions = MergeOptions(serviceName, downstreamApiOptionsOverride);
            HttpContent? effectiveInput = SerializeInput(input, effectiveOptions);
            HttpResponseMessage response = await CallApiInternalAsync(serviceName, effectiveOptions, true,
                                                                          effectiveInput, null, cancellationToken).ConfigureAwait(false);

            // Only dispose the HttpContent if was created here, not provided by the caller.
            if (input is not HttpContent)
            {
                effectiveInput?.Dispose();
            }

            return await DeserializeOutput<TOutput>(response, effectiveOptions).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async Task<TOutput?> CallApiForAppAsync<TOutput>(string serviceName,
            Action<DownstreamApiOptions>? downstreamApiOptionsOverride = null,
            CancellationToken cancellationToken = default) where TOutput : class
        {
            DownstreamApiOptions effectiveOptions = MergeOptions(serviceName, downstreamApiOptionsOverride);
            HttpResponseMessage response = await CallApiInternalAsync(serviceName, effectiveOptions, true,
                                                                          null, null, cancellationToken).ConfigureAwait(false);

            return await DeserializeOutput<TOutput>(response, effectiveOptions).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<TOutput?> CallApiForUserAsync<TOutput>(
            string? serviceName,
            Action<DownstreamApiOptions>? downstreamApiOptionsOverride = null,
            ClaimsPrincipal? user = null,
            CancellationToken cancellationToken = default) where TOutput : class
        {
            DownstreamApiOptions effectiveOptions = MergeOptions(serviceName, downstreamApiOptionsOverride);
            HttpResponseMessage response = await CallApiInternalAsync(serviceName, effectiveOptions, false,
                                                                          null, user, cancellationToken).ConfigureAwait(false);
            return await DeserializeOutput<TOutput>(response, effectiveOptions).ConfigureAwait(false);
        }

        /// <summary>
        /// Merge the options from configuration and override from caller.
        /// </summary>
        /// <param name="optionsInstanceName">Named configuration.</param>
        /// <param name="calledApiOptionsOverride">Delegate to override the configuration.</param>
        internal /* for tests */ DownstreamApiOptions MergeOptions(
            string? optionsInstanceName,
            Action<DownstreamApiOptions>? calledApiOptionsOverride)
        {
            // Gets the options from configuration (or default value)
            DownstreamApiOptions options;
            if (optionsInstanceName != null)
            {
                options = _namedDownstreamApiOptions.Get(optionsInstanceName);
            }
            else
            {
                options = _namedDownstreamApiOptions.CurrentValue;
            }

            DownstreamApiOptions clonedOptions = new DownstreamApiOptions(options);
            calledApiOptionsOverride?.Invoke(clonedOptions);
            return clonedOptions;
        }

        /// <summary>
        /// Merge the options from configuration and override from caller.
        /// </summary>
        /// <param name="optionsInstanceName">Named configuration.</param>
        /// <param name="calledApiOptionsOverride">Delegate to override the configuration.</param>
        /// <param name="httpMethod">Http method overriding the configuration options.</param>
        internal /* for tests */ DownstreamApiOptions MergeOptions(
            string? optionsInstanceName,
            Action<DownstreamApiOptionsReadOnlyHttpMethod>? calledApiOptionsOverride, HttpMethod httpMethod)
        {
            // Gets the options from configuration (or default value)
            DownstreamApiOptions options;
            if (optionsInstanceName != null)
            {
                options = _namedDownstreamApiOptions.Get(optionsInstanceName);
            }
            else
            {
                options = _namedDownstreamApiOptions.CurrentValue;
            }

            DownstreamApiOptionsReadOnlyHttpMethod clonedOptions = new DownstreamApiOptionsReadOnlyHttpMethod(options, httpMethod.ToString());
            calledApiOptionsOverride?.Invoke(clonedOptions);
            return clonedOptions;
        }

        private static HttpContent? SerializeInput<TInput>(TInput input, DownstreamApiOptions effectiveOptions)
        {
            HttpContent? effectiveInput;
            if (input is HttpContent)
            {
                effectiveInput = input as HttpContent;
            }
            else if (effectiveOptions.Serializer != null)
            {
                effectiveInput = effectiveOptions.Serializer(input);
            }
            else
            {
#pragma warning disable CA2000 // Dispose objects before losing scope
                effectiveInput = new StringContent(JsonSerializer.Serialize(input), Encoding.UTF8, "application/json");
#pragma warning restore CA2000 // Dispose objects before losing scope
            }

            return effectiveInput;
        }

        private static async Task<TOutput?> DeserializeOutput<TOutput>(HttpResponseMessage response, DownstreamApiOptions effectiveOptions)
             where TOutput : class
        {
            try
            {
                response.EnsureSuccessStatusCode();
            }
            catch
            {
                string error = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

#if NET5_0_OR_GREATER
                throw new HttpRequestException($"{(int)response.StatusCode} {response.StatusCode} {error}", null, response.StatusCode);
#else
                throw new HttpRequestException($"{(int)response.StatusCode} {response.StatusCode} {error}");
#endif
            }

            HttpContent content = response.Content;

            if (content == null)
            {
                return default;
            }

            if (effectiveOptions.Deserializer != null)
            {
                return effectiveOptions.Deserializer(content) as TOutput;
            }
            else
            {
                string stringContent = await content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<TOutput>(stringContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
        }

        private async Task<HttpResponseMessage> CallApiInternalAsync(
            string? serviceName,
            DownstreamApiOptions effectiveOptions,
            bool appToken,
            HttpContent? content = null,
            ClaimsPrincipal? user = null,
            CancellationToken cancellationToken = default)
        {
            // Downstream API URI
            string apiUrl = effectiveOptions.GetApiUrl();

            using HttpClient client = string.IsNullOrEmpty(serviceName) ? _httpClientFactory.CreateClient() : _httpClientFactory.CreateClient(serviceName);

            // Create an HTTP request message
            using HttpRequestMessage httpRequestMessage = new(
                new HttpMethod(effectiveOptions.HttpMethod),
                apiUrl);

            await UpdateRequestAsync(httpRequestMessage, content, effectiveOptions, appToken, user, cancellationToken);

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

        private async Task UpdateRequestAsync(
            HttpRequestMessage httpRequestMessage,
            HttpContent? content,
            DownstreamApiOptions effectiveOptions,
            bool appToken,
            ClaimsPrincipal? user,
            CancellationToken cancellationToken)
        {
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
                
                httpRequestMessage.Headers.Add(Authorization, authorizationHeader);
            }
            else
            {
                Logger.UnauthenticatedApiCall(_logger, null);
            }

            // Opportunity to change the request message
            effectiveOptions.CustomizeHttpRequestMessage?.Invoke(httpRequestMessage);
        }
    }
}
