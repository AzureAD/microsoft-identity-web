// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Identity.Web;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Implementation for the downstream API.
    /// </summary>
    public class DownstreamWebApi : IDownstreamWebApi
    {
        private readonly ITokenAcquisition _tokenAcquisition;
        private readonly HttpClient _httpClient;
        private readonly IOptionsMonitor<CalledApiOptions> _namedOptions;
        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        };

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="tokenAcquisition">Token acquisition service.</param>
        /// <param name="namedOptions">Named options provider.</param>
        /// <param name="httpClient">Http client.</param>
        public DownstreamWebApi(
            ITokenAcquisition tokenAcquisition,
            IOptionsMonitor<CalledApiOptions> namedOptions,
            HttpClient httpClient)
        {
            _tokenAcquisition = tokenAcquisition;
            _namedOptions = namedOptions;
            _httpClient = httpClient;
        }

        /// <inheritdoc/>
        public async Task<HttpResponseMessage> CallWebApiForUserAsync(
            string optionsInstanceName,
            Action<CalledApiOptions>? calledApiOptionsOverride,
            ClaimsPrincipal? user,
            StringContent? content)
        {
            CalledApiOptions effectiveOptions = MergeOptions(optionsInstanceName, calledApiOptionsOverride);

            // verify scopes is not null
            if (string.IsNullOrEmpty(effectiveOptions.Scopes))
            {
                throw new ArgumentException("Scopes need to be passed-in either by configuration or by the delegate overring it.");
            }

            string accessToken = await _tokenAcquisition.GetAccessTokenForUserAsync(effectiveOptions.GetScopes(), effectiveOptions.Tenant)
                .ConfigureAwait(false);

            HttpResponseMessage response;
            using (HttpRequestMessage httpRequestMessage = new HttpRequestMessage(effectiveOptions.HttpMethod, effectiveOptions.GetApiUrl()))
            {
                if (content != null)
                {
                    httpRequestMessage.Content = content;
                }

                httpRequestMessage.Headers.Add("Authorization", $"bearer {accessToken}");
                response = await _httpClient.SendAsync(httpRequestMessage).ConfigureAwait(false);
            }

            return response;
        }

        /// <summary>
        /// Merge the options from configuration and override from caller.
        /// </summary>
        /// <param name="optionsInstanceName">Named configuration.</param>
        /// <param name="calledApiOptionsOverride">Delegate to override the configuration.</param>
        internal /* for tests */ CalledApiOptions MergeOptions(
            string optionsInstanceName,
            Action<CalledApiOptions>? calledApiOptionsOverride)
        {
            // Gets the options from configuration (or default value)
            CalledApiOptions options;
            if (optionsInstanceName != null)
            {
                options = _namedOptions.Get(optionsInstanceName);
            }
            else
            {
                options = _namedOptions.CurrentValue;
            }

            // Give a chance to the called to override defaults for this call
            CalledApiOptions clonedOptions = options.Clone();
            calledApiOptionsOverride?.Invoke(clonedOptions);
            return clonedOptions;
        }

        /// <inheritdoc/>
        public async Task<TOutput> CallWebApiForUserAsync<TInput, TOutput>(
            string optionsInstanceName,
            TInput input,
            Action<CalledApiOptions>? calledApiOptionsOverride,
            ClaimsPrincipal? user)
            where TOutput : class
        {
            StringContent? jsoncontent;
            if (input != null)
            {
                var jsonRequest = JsonSerializer.Serialize(input);
                jsoncontent = new StringContent(jsonRequest, Encoding.UTF8, "application/json");
// case of patch?               jsoncontent = new StringContent(jsonRequest, Encoding.UTF8, "application/json-patch+json");
            }
            else
            {
                jsoncontent = null;
            }

            HttpResponseMessage response = await CallWebApiForUserAsync(
                optionsInstanceName,
                calledApiOptionsOverride,
                user,
                jsoncontent).ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                string content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                TOutput? output = JsonSerializer.Deserialize<TOutput>(content, _jsonOptions);
                return output;
            }
            else
            {
                string error = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                throw new HttpRequestException($"Invalid status code in the HttpResponseMessage: {response.StatusCode}: {error}");
            }
        }

        /// <inheritdoc/>
        public async Task<HttpResponseMessage> CallWebApiForAppAsync(
            string optionsInstanceName,
            Action<CalledApiOptions>? calledApiOptionsOverride = null,
            StringContent? content = null)
        {
            CalledApiOptions effectiveOptions = MergeOptions(optionsInstanceName, calledApiOptionsOverride);

            if (effectiveOptions.Scopes == null)
            {
                throw new ArgumentException("Scopes need to be passed-in either by configuration or by the delegate overring it.");
            }

            string accessToken = await _tokenAcquisition.GetAccessTokenForAppAsync(effectiveOptions.Scopes, effectiveOptions.Tenant)
                .ConfigureAwait(false);

            HttpResponseMessage response;
            using (HttpRequestMessage httpRequestMessage = new HttpRequestMessage(effectiveOptions.HttpMethod, effectiveOptions.GetApiUrl()))
            {
                httpRequestMessage.Headers.Add("Authorization", $"bearer {accessToken}");
                response = await _httpClient.SendAsync(httpRequestMessage).ConfigureAwait(false);
            }

            return response;
        }
    }
}
