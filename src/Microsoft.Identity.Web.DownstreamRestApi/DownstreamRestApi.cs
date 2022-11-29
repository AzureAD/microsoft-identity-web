// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Abstractions;

namespace Microsoft.Identity.Web
{
    /// <inheritdoc/>
    internal class DownstreamRestApi : IDownstreamRestApi
    {
        private readonly IAuthorizationHeaderProvider _authorizationHeaderProvider;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IOptionsMonitor<DownstreamRestApiOptionsWithScopes> _namedDownstreamRestApiOptions;
        private const string ScopesNotConfiguredInConfigurationOrViaDelegate = "IDW10107: Scopes need to be passed-in either by configuration or by the delegate overriding it. ";
        private const string Authorization = "Authorization";

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="authorizationHeaderProvider">Authorization header provider.</param>
        /// <param name="namedDownstreamRestApiOptions">Named options provider.</param>
        /// <param name="httpClientFactory">HTTP client factory.</param>
        public DownstreamRestApi(
            IAuthorizationHeaderProvider authorizationHeaderProvider,
            IOptionsMonitor<DownstreamRestApiOptionsWithScopes> namedDownstreamRestApiOptions,
            IHttpClientFactory httpClientFactory)
        {
            _authorizationHeaderProvider = authorizationHeaderProvider;
            _namedDownstreamRestApiOptions = namedDownstreamRestApiOptions;
            _httpClientFactory = httpClientFactory;
        }

        /// <inheritdoc/>
        public async Task<HttpResponseMessage> CallRestApiForUserAsync(
            string serviceName,
            Action<DownstreamRestApiOptionsWithScopes>? calledDownstreamRestApiOptionsOverride = null,
            ClaimsPrincipal? user = null,
            HttpContent? content = null,
            CancellationToken cancellationToken = default)
        {
            DownstreamRestApiOptionsWithScopes effectiveOptions = MergeOptions(serviceName, calledDownstreamRestApiOptionsOverride);

            if (effectiveOptions.Scopes == null)
            {
                throw new ArgumentException(ScopesNotConfiguredInConfigurationOrViaDelegate);
            }

            string apiUrl = effectiveOptions.GetApiUrl();

            using HttpRequestMessage httpRequestMessage = new HttpRequestMessage(
                effectiveOptions.HttpMethod,
                apiUrl);
            if (content != null)
            {
                httpRequestMessage.Content = content;
            }

            string authorizationHeader = await
                _authorizationHeaderProvider.CreateAuthorizationHeaderForUserAsync(
                    effectiveOptions.Scopes,
                    effectiveOptions,
                    user,
                    cancellationToken).ConfigureAwait(false);

            httpRequestMessage.Headers.Add(Authorization, authorizationHeader);
            effectiveOptions.CustomizeHttpRequestMessage?.Invoke(httpRequestMessage);
            using HttpClient client = _httpClientFactory.CreateClient(serviceName);
            return await client.SendAsync(httpRequestMessage, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<HttpResponseMessage> CallRestApiForAppAsync(
            string serviceName,
            Action<DownstreamRestApiOptionsWithScopes>? downstreamRestApiOptionsOverride = null,
            HttpContent? content = null,
            CancellationToken cancellationToken = default)
        {
            DownstreamRestApiOptionsWithScopes effectiveOptions = MergeOptions(serviceName, downstreamRestApiOptionsOverride);

            if (effectiveOptions.Scopes == null)
            {
                throw new ArgumentException(ScopesNotConfiguredInConfigurationOrViaDelegate);
            }

            string apiUrl = effectiveOptions.GetApiUrl();

            using HttpRequestMessage httpRequestMessage = new HttpRequestMessage(
                effectiveOptions.HttpMethod,
                apiUrl);
            if (content != null)
            {
                httpRequestMessage.Content = content;
            }

            string authorizationHeader = await
                _authorizationHeaderProvider.CreateAuthorizationHeaderForAppAsync(
                    effectiveOptions.Scopes.FirstOrDefault(),
                    effectiveOptions,
                    cancellationToken).ConfigureAwait(false);

            httpRequestMessage.Headers.Add(Authorization, authorizationHeader);
            effectiveOptions.CustomizeHttpRequestMessage?.Invoke(httpRequestMessage);
            using HttpClient client = _httpClientFactory.CreateClient(serviceName);
            return await client.SendAsync(httpRequestMessage, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<TOutput?> CallRestApiForUserAsync<TInput, TOutput>(
            string serviceName,
            TInput input,
            Action<DownstreamRestApiOptionsWithScopes>? downstreamRestApiOptionsOverride = null,
            ClaimsPrincipal? user = null,
            CancellationToken cancellationToken = default) where TOutput : class
        {
            HttpContent? effectiveInput;
            if (input is HttpContent)
            {
                effectiveInput = input as HttpContent;
            }
            else
            {
#pragma warning disable CA2000 // Dispose objects before losing scope
                effectiveInput = new StringContent(JsonSerializer.Serialize(input), Encoding.UTF8, "application/json");
#pragma warning restore CA2000 // Dispose objects before losing scope
            }

            HttpResponseMessage response = await CallRestApiForUserAsync(
                serviceName,
                downstreamRestApiOptionsOverride,
                user,
                effectiveInput,
                cancellationToken).ConfigureAwait(false);

            if (input is not HttpContent)
            {
                effectiveInput?.Dispose();
            }

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

            string content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(content))
            {
                return default;
            }

            return JsonSerializer.Deserialize<TOutput>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        /// <inheritdoc/>
        public async Task<TOutput?> CallRestApiForUserAsync<TOutput>(
            string serviceName,
            Action<DownstreamRestApiOptionsWithScopes>? downstreamRestApiOptionsOverride = null,
            ClaimsPrincipal? user = null,
            CancellationToken cancellationToken = default) where TOutput : class
        {
            HttpResponseMessage response = await CallRestApiForUserAsync(
              serviceName,
              downstreamRestApiOptionsOverride,
              user,
              null,
              cancellationToken).ConfigureAwait(false);

            return await ConvertToOutput<TOutput>(response).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<TOutput?> GetForUserAsync<TOutput>(
            string serviceName,
            Action<DownstreamRestApiOptionsWithScopes>? downstreamRestApiOptionsOverride = null,
            ClaimsPrincipal? user = null,
            CancellationToken cancellationToken = default) where TOutput : class
        {
            HttpResponseMessage response = await CallRestApiForUserAsync(
                serviceName,
                PrepareOptions(downstreamRestApiOptionsOverride, HttpMethod.Get),
                user,
                null,
                cancellationToken).ConfigureAwait(false);

            return await ConvertToOutput<TOutput>(response).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task GetForUserAsync<TInput>(
            string serviceName,
            TInput inputData,
            Action<DownstreamRestApiOptionsWithScopes>? downstreamRestApiOptionsOverride = null,
            ClaimsPrincipal? user = null,
            CancellationToken cancellationToken = default)
        {
            using StringContent? input = ConvertFromInput(inputData);

            await CallRestApiForUserAsync(
             serviceName,
             downstreamRestApiOptionsOverride,
             user,
             input,
             cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<TOutput?> PostForUserAsync<TOutput, TInput>(
            string serviceName,
            string relativePath,
            TInput inputData,
            Action<DownstreamRestApiOptionsWithScopes>? downstreamRestApiOptionsOverride = null,
            ClaimsPrincipal? user = null,
            CancellationToken cancellationToken = default) where TOutput : class
        {
            using StringContent? input = ConvertFromInput(inputData);

            HttpResponseMessage response = await CallRestApiForUserAsync(
               serviceName,
               PrepareOptions(downstreamRestApiOptionsOverride, HttpMethod.Post),
               user,
               input,
               cancellationToken).ConfigureAwait(false);

            return await ConvertToOutput<TOutput>(response).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task PutForUserAsync<TInput>(
            string serviceName,
            string relativePath,
            TInput inputData,
            Action<DownstreamRestApiOptionsWithScopes>? downstreamRestApiOptionsOverride = null,
            ClaimsPrincipal? user = null,
            CancellationToken cancellationToken = default)
        {
            using StringContent? input = ConvertFromInput(inputData);

            await CallRestApiForUserAsync(
              serviceName,
              PrepareOptions(downstreamRestApiOptionsOverride, HttpMethod.Put),
              user,
              input,
              cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<TOutput?> PutForUserAsync<TOutput, TInput>(
            string serviceName,
            string relativePath,
            TInput inputData,
            Action<DownstreamRestApiOptionsWithScopes>? downstreamRestApiOptionsOverride = null,
            ClaimsPrincipal? user = null,
            CancellationToken cancellationToken = default) where TOutput : class
        {
            using StringContent? input = ConvertFromInput(inputData);

            HttpResponseMessage response = await CallRestApiForUserAsync(
               serviceName,
               PrepareOptions(downstreamRestApiOptionsOverride, HttpMethod.Put),
               user,
               input,
               cancellationToken).ConfigureAwait(false);

            return await ConvertToOutput<TOutput>(response).ConfigureAwait(false);
        }

        /// <summary>
        /// Merge the options from configuration and override from caller.
        /// </summary>
        /// <param name="optionsInstanceName">Named configuration.</param>
        /// <param name="calledApiOptionsOverride">Delegate to override the configuration.</param>
        internal /* for tests */ DownstreamRestApiOptionsWithScopes MergeOptions(
            string optionsInstanceName,
            Action<DownstreamRestApiOptionsWithScopes>? calledApiOptionsOverride)
        {
            // Gets the options from configuration (or default value)
            DownstreamRestApiOptionsWithScopes options;
            if (optionsInstanceName != null)
            {
                options = _namedDownstreamRestApiOptions.Get(optionsInstanceName);
            }
            else
            {
                options = _namedDownstreamRestApiOptions.CurrentValue;
            }

            DownstreamRestApiOptionsWithScopes clonedOptions = new DownstreamRestApiOptionsWithScopes(options);
            calledApiOptionsOverride?.Invoke(clonedOptions);
            return clonedOptions;
        }

        private static async Task<TOutput?> ConvertToOutput<TOutput>(HttpResponseMessage response)
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

            string content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(content))
            {
                return default;
            }

            return JsonSerializer.Deserialize<TOutput>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        private static Action<DownstreamRestApiOptionsWithScopes> PrepareOptions(
          Action<DownstreamRestApiOptionsWithScopes>? downstreamRestApiOptionsOverride,
          HttpMethod httpMethod)
        {
            Action<DownstreamRestApiOptionsWithScopes> downstreamRestApiOptions;

            if (downstreamRestApiOptionsOverride == null)
            {
                downstreamRestApiOptions = options =>
                {
                    options.HttpMethod = httpMethod;
                };
            }
            else
            {
                downstreamRestApiOptions = options =>
                {
                    downstreamRestApiOptionsOverride(options);
                    options.HttpMethod = httpMethod;
                };
            }

            return downstreamRestApiOptions;
        }

        private static StringContent ConvertFromInput<TInput>(TInput input)
        {
            return new StringContent(JsonSerializer.Serialize(input), Encoding.UTF8, "application/json");
        }
    }
}
