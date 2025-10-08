// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Implementation for the downstream web API.
    /// </summary>
#pragma warning disable CS0618 // Type or member is obsolete
    [Obsolete("Use DownstreamApi in Microsoft.Identity.Abstractions, implemented in Microsoft.Identity.Web.DownstreamApi." +
        "See aka.ms/id-web-downstream-api-v2 for migration details.", false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class DownstreamWebApi : IDownstreamWebApi
#pragma warning restore CS0618 // Type or member is obsolete
    {
        private readonly ITokenAcquisition _tokenAcquisition;
        private readonly HttpClient _httpClient;
        private readonly IOptionsMonitor<DownstreamWebApiOptions> _namedDownstreamWebApiOptions;
        private readonly IOptionsMonitor<MicrosoftIdentityOptions> _microsoftIdentityOptionsMonitor;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="tokenAcquisition">Token acquisition service.</param>
        /// <param name="namedDownstreamWebApiOptions">Named options provider.</param>
        /// <param name="httpClient">HTTP client.</param>
        /// <param name="microsoftIdentityOptionsMonitor">Configuration options.</param>
        public DownstreamWebApi(
            ITokenAcquisition tokenAcquisition,
            IOptionsMonitor<DownstreamWebApiOptions> namedDownstreamWebApiOptions,
            HttpClient httpClient,
            IOptionsMonitor<MicrosoftIdentityOptions> microsoftIdentityOptionsMonitor)
        {
            _tokenAcquisition = tokenAcquisition;
            _namedDownstreamWebApiOptions = namedDownstreamWebApiOptions;
            _httpClient = httpClient;
            _microsoftIdentityOptionsMonitor = microsoftIdentityOptionsMonitor;
        }

        /// <inheritdoc/>
        public async Task<HttpResponseMessage> CallWebApiForUserAsync(
            string serviceName,
            string? authenticationScheme,
            Action<DownstreamWebApiOptions>? calledDownstreamWebApiOptionsOverride = null,
            ClaimsPrincipal? user = null,
            StringContent? content = null)
        {
            DownstreamWebApiOptions effectiveOptions = MergeOptions(serviceName, calledDownstreamWebApiOptionsOverride);

            if (string.IsNullOrEmpty(effectiveOptions.Scopes))
            {
                throw new ArgumentException(IDWebErrorMessage.ScopesNotConfiguredInConfigurationOrViaDelegate);
            }

            MicrosoftIdentityOptions microsoftIdentityOptions = _microsoftIdentityOptionsMonitor
                .Get(_tokenAcquisition.GetEffectiveAuthenticationScheme(authenticationScheme));

            string apiUrl = effectiveOptions.GetApiUrl();

            CreateProofOfPossessionConfiguration(effectiveOptions, apiUrl);

            string? userflow;
            if (microsoftIdentityOptions.IsB2C && string.IsNullOrEmpty(effectiveOptions.UserFlow))
            {
                userflow = microsoftIdentityOptions.DefaultUserFlow;
            }
            else
            {
                userflow = effectiveOptions.UserFlow;
            }

            AuthenticationResult authResult = await _tokenAcquisition.GetAuthenticationResultForUserAsync(
                effectiveOptions.GetScopes(),
                authenticationScheme,
                effectiveOptions.Tenant,
                userflow,
                user,
                effectiveOptions.TokenAcquisitionOptions)
                .ConfigureAwait(false);

            using (HttpRequestMessage httpRequestMessage = new HttpRequestMessage(
                effectiveOptions.HttpMethod,
                apiUrl))
            {
                if (content != null)
                {
                    httpRequestMessage.Content = content;
                }

                httpRequestMessage.Headers.Add(
                    Constants.Authorization,
                    authResult.CreateAuthorizationHeader());
                effectiveOptions.CustomizeHttpRequestMessage?.Invoke(httpRequestMessage);
                return await _httpClient.SendAsync(httpRequestMessage).ConfigureAwait(false);
            }
        }

        /// <inheritdoc/>
#if NET6_0_OR_GREATER
        [RequiresUnreferencedCode("Calls System.Text.Json.JsonSerializer.Serialize<TValue>(TValue, JsonSerializerOptions).")]
#endif
        public async Task<TOutput?> CallWebApiForUserAsync<TInput, TOutput>(
            string serviceName,
            TInput input,
            string? authenticationScheme,
            Action<DownstreamWebApiOptions>? downstreamWebApiOptionsOverride = null,
            ClaimsPrincipal? user = null)
            where TOutput : class
        {
            HttpResponseMessage response = await CallWebApiForUserAsync(
                serviceName,
                authenticationScheme,
                downstreamWebApiOptionsOverride,
                user,
                new StringContent(JsonSerializer.Serialize(input), Encoding.UTF8, "application/json")).ConfigureAwait(false);

            try
            {
                response.EnsureSuccessStatusCode();
            }
            catch
            {
                string error = await ReadErrorResponseContentAsync(response).ConfigureAwait(false);

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
        public async Task<HttpResponseMessage> CallWebApiForAppAsync(
            string serviceName,
            string? authenticationScheme,
            Action<DownstreamWebApiOptions>? downstreamWebApiOptionsOverride = null,
            StringContent? content = null)
        {
            DownstreamWebApiOptions effectiveOptions = MergeOptions(serviceName, downstreamWebApiOptionsOverride);

            if (effectiveOptions.Scopes == null)
            {
                throw new ArgumentException(IDWebErrorMessage.ScopesNotConfiguredInConfigurationOrViaDelegate);
            }

            string apiUrl = effectiveOptions.GetApiUrl();

            CreateProofOfPossessionConfiguration(effectiveOptions, apiUrl);

            AuthenticationResult authResult = await _tokenAcquisition.GetAuthenticationResultForAppAsync(
                effectiveOptions.Scopes,
                authenticationScheme,
                effectiveOptions.Tenant,
                effectiveOptions.TokenAcquisitionOptions)
                .ConfigureAwait(false);

            HttpResponseMessage response;
            using (HttpRequestMessage httpRequestMessage = new HttpRequestMessage(
                effectiveOptions.HttpMethod,
                apiUrl))
            {
                if (content != null)
                {
                    httpRequestMessage.Content = content;
                }

                httpRequestMessage.Headers.Add(
                    Constants.Authorization,
                    authResult.CreateAuthorizationHeader());
                effectiveOptions.CustomizeHttpRequestMessage?.Invoke(httpRequestMessage);
                response = await _httpClient.SendAsync(httpRequestMessage).ConfigureAwait(false);
            }

            return response;
        }

        /// <summary>
        /// Merge the options from configuration and override from caller.
        /// </summary>
        /// <param name="optionsInstanceName">Named configuration.</param>
        /// <param name="calledApiOptionsOverride">Delegate to override the configuration.</param>
        internal /* for tests */ DownstreamWebApiOptions MergeOptions(
            string optionsInstanceName,
            Action<DownstreamWebApiOptions>? calledApiOptionsOverride)
        {
            // Gets the options from configuration (or default value)
            DownstreamWebApiOptions options;
            if (optionsInstanceName != null)
            {
                options = _namedDownstreamWebApiOptions.Get(optionsInstanceName);
            }
            else
            {
                options = _namedDownstreamWebApiOptions.CurrentValue;
            }

            DownstreamWebApiOptions clonedOptions = options.Clone();
            calledApiOptionsOverride?.Invoke(clonedOptions);
            return clonedOptions;
        }

        private static void CreateProofOfPossessionConfiguration(DownstreamWebApiOptions effectiveOptions, string apiUrl)
        {
            if (effectiveOptions.IsProofOfPossessionRequest && effectiveOptions.TokenAcquisitionOptions?.PoPConfiguration != null)
            {
                if (effectiveOptions.TokenAcquisitionOptions == null)
                {
                    effectiveOptions.TokenAcquisitionOptions = new TokenAcquisitionOptions();
                }

                effectiveOptions.TokenAcquisitionOptions.PoPConfiguration = new PoPAuthenticationConfiguration(new Uri(apiUrl))
                {
                    HttpMethod = effectiveOptions.HttpMethod,
                };
            }
        }

        /// <summary>
        /// Safely reads error response content with size limits to avoid performance issues with large payloads.
        /// </summary>
        /// <param name="response">The HTTP response message.</param>
        /// <returns>The error response content, truncated if necessary.</returns>
        private static async Task<string> ReadErrorResponseContentAsync(HttpResponseMessage response)
        {
            const int maxErrorContentLength = 4096;
            
            long? contentLength = response.Content.Headers.ContentLength;
            
            if (contentLength.HasValue && contentLength.Value > maxErrorContentLength)
            {
                return $"[Error response too large: {contentLength.Value} bytes, not captured]";
            }
            
            string errorResponseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            
            if (errorResponseContent.Length > maxErrorContentLength)
            {
                errorResponseContent = errorResponseContent.Substring(0, maxErrorContentLength) + "... (truncated)";
            }
            
            return errorResponseContent;
        }
    }
}
