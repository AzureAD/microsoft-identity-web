// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;

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

            using (HttpRequestMessage httpRequestMessage = new HttpRequestMessage(
                effectiveOptions.HttpMethod,
                apiUrl))
            {
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
        }

        /// <inheritdoc/>
        public Task<HttpResponseMessage> CallWebApiForAppAsync(string serviceName, Action<DownstreamRestApiOptions>? DownstreamRestApiOptionsOverride = null, HttpContent? content = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<TOutput?> CallWebApiForUserAsync<TOutput>(string serviceName, Action<DownstreamRestApiOptions>? DownstreamRestApiOptionsOverride = null, ClaimsPrincipal? user = null, CancellationToken cancellationToken = default) where TOutput : class
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<TOutput?> GetForUserAsync<TOutput>(string serviceName, Action<DownstreamRestApiOptions>? DownstreamRestApiOptionsOverride = null, ClaimsPrincipal? user = null, CancellationToken cancellationToken = default) where TOutput : class
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task GetForUserAsync<TInput>(string serviceName, TInput inputData, Action<DownstreamRestApiOptions>? DownstreamRestApiOptionsOverride = null, ClaimsPrincipal? user = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<TOutput?> PostForUserAsync<TOutput, TInput>(string serviceName, string relativePath, TInput inputData, Action<DownstreamRestApiOptions>? DownstreamRestApiOptionsOverride = null, ClaimsPrincipal? user = null, CancellationToken cancellationToken = default) where TOutput : class
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task PutForUserAsync<TInput>(string serviceName, string relativePath, TInput inputData, Action<DownstreamRestApiOptions>? DownstreamRestApiOptionsOverride = null, ClaimsPrincipal? user = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<TOutput?> PutForUserAsync<TOutput, TInput>(string serviceName, string relativePath, TInput inputData, Action<DownstreamRestApiOptions>? DownstreamRestApiOptionsOverride = null, ClaimsPrincipal? user = null, CancellationToken cancellationToken = default) where TOutput : class
        {
            throw new NotImplementedException();
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

        // TODO: Update
        private static string CreateAuthorizationHeader(AcquireTokenResult result)
        {
            return $"Bearer {result.AccessToken}";
        }
    }
}
