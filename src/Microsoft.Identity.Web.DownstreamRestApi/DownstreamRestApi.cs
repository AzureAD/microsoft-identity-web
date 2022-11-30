// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
        private readonly IOptionsMonitor<DownstreamRestApiOptions> _namedDownstreamRestApiOptions;
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
            IOptionsMonitor<DownstreamRestApiOptions> namedDownstreamRestApiOptions,
            IHttpClientFactory httpClientFactory)
        {
            _authorizationHeaderProvider = authorizationHeaderProvider;
            _namedDownstreamRestApiOptions = namedDownstreamRestApiOptions;
            _httpClientFactory = httpClientFactory;
        }

        /// <inheritdoc/>
        public Task<HttpResponseMessage> CallRestApiForUserAsync(
            string serviceName,
            Action<DownstreamRestApiOptions>? calledDownstreamRestApiOptionsOverride = null,
            ClaimsPrincipal? user = null,
            HttpContent? content = null,
            CancellationToken cancellationToken = default)
        {
            DownstreamRestApiOptions effectiveOptions = MergeOptions(serviceName, calledDownstreamRestApiOptionsOverride);
            return CallRestApiInternalAsync(serviceName,
                effectiveOptions,
                   async (scopes, options) =>
                   {
                       return await _authorizationHeaderProvider.CreateAuthorizationHeaderForUserAsync(
                        scopes,
                        options,
                        user,
                        cancellationToken).ConfigureAwait(false);
                   },
                content,
                cancellationToken);
        }

        /// <inheritdoc/>
        public Task<HttpResponseMessage> CallRestApiForAppAsync(
           string serviceName,
           Action<DownstreamRestApiOptions>? downstreamRestApiOptionsOverride = null,
           HttpContent? content = null,
           CancellationToken cancellationToken = default)
        {
            DownstreamRestApiOptions effectiveOptions = MergeOptions(serviceName, downstreamRestApiOptionsOverride);
            return CallRestApiInternalAsync(serviceName, effectiveOptions,
                async (scopes, options) =>
                {
                    return await _authorizationHeaderProvider.CreateAuthorizationHeaderForAppAsync(
                     scopes.FirstOrDefault(),
                     options,
                     cancellationToken).ConfigureAwait(false);
                },
                content, cancellationToken);
        }


        private delegate StringContent Serializer<T>(T objectToSerialize);

        /// <inheritdoc/>
        public async Task<TOutput?> CallRestApiForUserAsync<TInput, TOutput>(
            string serviceName,
            TInput input,
            Action<DownstreamRestApiOptions>? downstreamRestApiOptionsOverride = null,
            ClaimsPrincipal? user = null,
            CancellationToken cancellationToken = default) where TOutput : class
        {
            DownstreamRestApiOptions effectiveOptions = MergeOptions(serviceName, downstreamRestApiOptionsOverride);

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

            if (effectiveOptions.Deserializer != null)
            {
                return effectiveOptions.Deserializer(content) as TOutput;
            }
            else
            {
                return JsonSerializer.Deserialize<TOutput>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
        }

        /// <inheritdoc/>
        public async Task<TOutput?> CallRestApiForUserAsync<TOutput>(
            string serviceName,
            Action<DownstreamRestApiOptions>? downstreamRestApiOptionsOverride = null,
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
            Action<DownstreamRestApiOptions>? downstreamRestApiOptionsOverride = null,
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
            Action<DownstreamRestApiOptions>? downstreamRestApiOptionsOverride = null,
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
            Action<DownstreamRestApiOptions>? downstreamRestApiOptionsOverride = null,
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
            Action<DownstreamRestApiOptions>? downstreamRestApiOptionsOverride = null,
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
            Action<DownstreamRestApiOptions>? downstreamRestApiOptionsOverride = null,
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
        internal /* for tests */ DownstreamRestApiOptions MergeOptions(
            string optionsInstanceName,
            Action<DownstreamRestApiOptions>? calledApiOptionsOverride)
        {
            // Gets the options from configuration (or default value)
            DownstreamRestApiOptions options;
            if (optionsInstanceName != null)
            {
                options = _namedDownstreamRestApiOptions.Get(optionsInstanceName);
            }
            else
            {
                options = _namedDownstreamRestApiOptions.CurrentValue;
            }

            DownstreamRestApiOptions clonedOptions = new DownstreamRestApiOptions(options);
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

        private static Action<DownstreamRestApiOptions> PrepareOptions(
          Action<DownstreamRestApiOptions>? downstreamRestApiOptionsOverride,
          HttpMethod httpMethod)
        {
            Action<DownstreamRestApiOptions> downstreamRestApiOptions;

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

        private delegate Task<string> AuthorizationHeaderCreator(IEnumerable<string> scopes, DownstreamRestApiOptions options);

        private async Task<HttpResponseMessage> CallRestApiInternalAsync(
            string serviceName,
            DownstreamRestApiOptions effectiveOptions,
            AuthorizationHeaderCreator authorizationHeaderCreator,
            HttpContent? content = null,

            CancellationToken cancellationToken = default)
        {
            if (effectiveOptions.Scopes == null)
            {
                throw new ArgumentException(ScopesNotConfiguredInConfigurationOrViaDelegate);
            }

            // Downstream API URI
            string apiUrl = effectiveOptions.GetApiUrl();

            // Creation of the Http Request message with the right HTTP Method
            using HttpRequestMessage httpRequestMessage = new HttpRequestMessage(
                effectiveOptions.HttpMethod,
                apiUrl);
            if (content != null)
            {
                httpRequestMessage.Content = content;
            }

            // Obtention of the authorization header
            string authorizationHeader = await authorizationHeaderCreator(effectiveOptions.Scopes, effectiveOptions);
            httpRequestMessage.Headers.Add(Authorization, authorizationHeader);

            // Opportunity to change the request message
            effectiveOptions.CustomizeHttpRequestMessage?.Invoke(httpRequestMessage);

            // Send the HTTP message
            using HttpClient client = _httpClientFactory.CreateClient(serviceName);
            return await client.SendAsync(httpRequestMessage, cancellationToken).ConfigureAwait(false);
        }
    }
}
