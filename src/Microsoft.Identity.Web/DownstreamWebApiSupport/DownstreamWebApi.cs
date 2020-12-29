// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Implementation for the downstream web API.
    /// </summary>
    public class DownstreamWebApi : IDownstreamWebApi
    {
        private readonly ITokenAcquisition _tokenAcquisition;
        private readonly HttpClient _httpClient;
        private readonly IOptionsMonitor<DownstreamWebApiOptions> _namedDownstreamWebApiOptions;
        private readonly MicrosoftIdentityOptions _microsoftIdentityOptions;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="tokenAcquisition">Token acquisition service.</param>
        /// <param name="namedDownstreamWebApiOptions">Named options provider.</param>
        /// <param name="httpClient">HTTP client.</param>
        /// <param name="microsoftIdentityOptions">Configuration options.</param>
        public DownstreamWebApi(
            ITokenAcquisition tokenAcquisition,
            IOptionsMonitor<DownstreamWebApiOptions> namedDownstreamWebApiOptions,
            HttpClient httpClient,
            IOptions<MicrosoftIdentityOptions> microsoftIdentityOptions)
        {
            _tokenAcquisition = tokenAcquisition;
            _namedDownstreamWebApiOptions = namedDownstreamWebApiOptions;
            _httpClient = httpClient;
#pragma warning disable CA1062 // Validate arguments of public methods
            _microsoftIdentityOptions = microsoftIdentityOptions.Value;
#pragma warning restore CA1062 // Validate arguments of public methods
        }

        /// <inheritdoc/>
        public async Task<HttpResponseMessage> CallWebApiForUserAsync(
            string serviceName,
            Action<DownstreamWebApiOptions>? calledDownstreamWebApiOptionsOverride = null,
            ClaimsPrincipal? user = null,
            StringContent? content = null)
        {
            DownstreamWebApiOptions effectiveOptions = MergeOptions(serviceName, calledDownstreamWebApiOptionsOverride);

            if (string.IsNullOrEmpty(effectiveOptions.Scopes))
            {
                throw new ArgumentException(IDWebErrorMessage.ScopesNotConfiguredInConfigurationOrViaDelegate);
            }

            string? userflow;
            if (_microsoftIdentityOptions.IsB2C && string.IsNullOrEmpty(effectiveOptions.UserFlow))
            {
                // this may need to change if 'IsB2C' is changed to also allow for separate signup and signin user flows
                // B2C userflow would no longer necessarily be just DefualtUserFlow, which is SignUpSignInPolicyId
                userflow = _microsoftIdentityOptions.DefaultUserFlow;
            }
            else
            {
                userflow = effectiveOptions.UserFlow;
            }

            string accessToken = await _tokenAcquisition.GetAccessTokenForUserAsync(
                effectiveOptions.GetScopes(),
                effectiveOptions.Tenant,
                userflow,
                user,
                effectiveOptions.TokenAcquisitionOptions)
                .ConfigureAwait(false);

            HttpResponseMessage response;
            using (HttpRequestMessage httpRequestMessage = new HttpRequestMessage(
                effectiveOptions.HttpMethod,
                effectiveOptions.GetApiUrl()))
            {
                if (content != null)
                {
                    httpRequestMessage.Content = content;
                }

                httpRequestMessage.Headers.Add(
                    Constants.Authorization,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "{0} {1}",
                        Constants.Bearer,
                        accessToken));
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

        /// <inheritdoc/>
        public async Task<TOutput?> CallWebApiForUserAsync<TInput, TOutput>(
            string serviceName,
            TInput input,
            Action<DownstreamWebApiOptions>? downstreamWebApiOptionsOverride = null,
            ClaimsPrincipal? user = null)
            where TOutput : class
        {
            HttpResponseMessage response = await CallWebApiForUserAsync(
                serviceName,
                downstreamWebApiOptionsOverride,
                user,
                new StringContent(JsonSerializer.Serialize(input), Encoding.UTF8, "application/json")).ConfigureAwait(false);

            try
            {
                response.EnsureSuccessStatusCode();
            }
            catch
            {
                string error = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                throw new HttpRequestException($"{(int)response.StatusCode} {response.StatusCode} {error}");
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
            Action<DownstreamWebApiOptions>? downstreamWebApiOptionsOverride = null,
            StringContent? content = null)
        {
            DownstreamWebApiOptions effectiveOptions = MergeOptions(serviceName, downstreamWebApiOptionsOverride);

            if (effectiveOptions.Scopes == null)
            {
                throw new ArgumentException(IDWebErrorMessage.ScopesNotConfiguredInConfigurationOrViaDelegate);
            }

            string accessToken = await _tokenAcquisition.GetAccessTokenForAppAsync(
                effectiveOptions.Scopes,
                effectiveOptions.Tenant,
                effectiveOptions.TokenAcquisitionOptions)
                .ConfigureAwait(false);

            HttpResponseMessage response;
            using (HttpRequestMessage httpRequestMessage = new HttpRequestMessage(
                effectiveOptions.HttpMethod,
                effectiveOptions.GetApiUrl()))
            {
                if (content != null)
                {
                    httpRequestMessage.Content = content;
                }

                httpRequestMessage.Headers.Add(
                    Constants.Authorization,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "{0} {1}",
                        Constants.Bearer,
                        accessToken));
                response = await _httpClient.SendAsync(httpRequestMessage).ConfigureAwait(false);
            }

            return response;
        }
    }
}
