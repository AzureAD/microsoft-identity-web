﻿// Copyright (c) Microsoft Corporation. All rights reserved.
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

namespace Microsoft.Identity.Web
{
    /// <inheritdoc/>
    public class DownstreamRestApi : IDownstreamRestApi
    {
        private readonly ITokenAcquirerFactory _tokenAcquirerFactory;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IOptionsMonitor<DownstreamRestApiOptions> _namedDownstreamRestApiOptions;
        private const string ScopesNotConfiguredInConfigurationOrViaDelegate = "IDW10107: Scopes need to be passed-in either by configuration or by the delegate overriding it. ";
        private const string Authorization = "Authorization";

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="tokenAcquirerFactory">Token acquirer factory.</param>
        /// <param name="namedDownstreamRestApiOptions">Named options provider.</param>
        /// <param name="httpClientFactory">HTTP client.</param>
        public DownstreamRestApi(
            ITokenAcquirerFactory tokenAcquirerFactory,
            IOptionsMonitor<DownstreamRestApiOptions> namedDownstreamRestApiOptions,
            IHttpClientFactory httpClientFactory)
        {
            _tokenAcquirerFactory = tokenAcquirerFactory;
            _namedDownstreamRestApiOptions = namedDownstreamRestApiOptions;
            _httpClientFactory = httpClientFactory;
        }

        /// <inheritdoc/>
        public async Task<HttpResponseMessage> CallRestApiForUserAsync(
            string serviceName,
            Action<DownstreamRestApiOptions>? calledDownstreamRestApiOptionsOverride = null,
            ClaimsPrincipal? user = null,
            HttpContent? content = null,
            CancellationToken cancellationToken = default)
        {
            DownstreamRestApiOptions effectiveOptions = MergeOptions(serviceName, calledDownstreamRestApiOptionsOverride);

            if (effectiveOptions.Scopes == null)
            {
                throw new ArgumentException(ScopesNotConfiguredInConfigurationOrViaDelegate);
            }

            string apiUrl = effectiveOptions.GetApiUrl();

            CreateProofOfPossessionConfiguration(effectiveOptions, apiUrl);

            ITokenAcquirer tokenAcquirer = _tokenAcquirerFactory.GetTokenAcquirer(effectiveOptions.TokenAcquirerOptions.AuthenticationScheme ?? string.Empty);
            AcquireTokenResult result = await tokenAcquirer.GetTokenForUserAsync(
                effectiveOptions.Scopes,
                effectiveOptions.TokenAcquirerOptions,
                user,
                cancellationToken).ConfigureAwait(false);

            using HttpRequestMessage httpRequestMessage = new HttpRequestMessage(
                effectiveOptions.HttpMethod,
                apiUrl);
            if (content != null)
            {
                httpRequestMessage.Content = content;
            }

            httpRequestMessage.Headers.Add(
                Authorization,
                CreateAuthorizationHeader(result));
            effectiveOptions.CustomizeHttpRequestMessage?.Invoke(httpRequestMessage);
            using var client = _httpClientFactory.CreateClient(serviceName);
            return await client.SendAsync(httpRequestMessage, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<HttpResponseMessage> CallRestApiForAppAsync(
            string serviceName,
            Action<DownstreamRestApiOptions>? downstreamRestApiOptionsOverride = null,
            HttpContent? content = null,
            CancellationToken cancellationToken = default)
        {
            DownstreamRestApiOptions effectiveOptions = MergeOptions(serviceName, downstreamRestApiOptionsOverride);

            if (effectiveOptions.Scopes == null)
            {
                throw new ArgumentException(ScopesNotConfiguredInConfigurationOrViaDelegate);
            }

            string apiUrl = effectiveOptions.GetApiUrl();

            CreateProofOfPossessionConfiguration(effectiveOptions, apiUrl);
            ITokenAcquirer tokenAcquirer = _tokenAcquirerFactory.GetTokenAcquirer(effectiveOptions.TokenAcquirerOptions.AuthenticationScheme ?? string.Empty);
            AcquireTokenResult result = await tokenAcquirer.GetTokenForAppAsync(
                effectiveOptions.Scopes.FirstOrDefault(),
                effectiveOptions.TokenAcquirerOptions,
                cancellationToken).ConfigureAwait(false);

            using HttpRequestMessage httpRequestMessage = new HttpRequestMessage(
                effectiveOptions.HttpMethod,
                apiUrl);
            if (content != null)
            {
                httpRequestMessage.Content = content;
            }

            httpRequestMessage.Headers.Add(
                Authorization,
                CreateAuthorizationHeader(result));
            effectiveOptions.CustomizeHttpRequestMessage?.Invoke(httpRequestMessage);
            using var client = _httpClientFactory.CreateClient(serviceName);
            return await client.SendAsync(httpRequestMessage, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<TOutput?> CallRestApiForUserAsync<TInput, TOutput>(
            string serviceName,
            TInput input,
            Action<DownstreamRestApiOptions>? downstreamRestApiOptionsOverride = null,
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
                effectiveInput = new StringContent(JsonSerializer.Serialize(input), Encoding.UTF8, "application/json");
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

            DownstreamRestApiOptions clonedOptions = options.Clone();
            calledApiOptionsOverride?.Invoke(clonedOptions);
            return clonedOptions;
        }

        private static void CreateProofOfPossessionConfiguration(DownstreamRestApiOptions effectiveOptions, string apiUrl)
        {
            //if (effectiveOptions.IsProofOfPossessionRequest && effectiveOptions.TokenAcquisitionOptions?.PoPConfiguration != null)
            //{
            //    if (effectiveOptions.TokenAcquisitionOptions == null)
            //    {
            //        effectiveOptions.TokenAcquisitionOptions = new TokenAcquisitionOptions();
            //    }

            //    effectiveOptions.TokenAcquisitionOptions.PoPConfiguration = new PoPAuthenticationConfiguration(new Uri(apiUrl))
            //    {
            //        HttpMethod = effectiveOptions.HttpMethod,
            //    };
            //}
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

        // TODO: Update
        private static string CreateAuthorizationHeader(AcquireTokenResult result)
        {
            return $"Bearer {result.AccessToken}";
        }
    }
}
