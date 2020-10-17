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
    public partial class DownstreamWebApi : IDownstreamWebApi
    {
        private readonly ITokenAcquisition _tokenAcquisition;
        private readonly HttpClient _httpClient;
        private readonly IOptionsMonitor<DownstreamWebApiOptions> _namedDownstreamWebApiOptions;
        private readonly MicrosoftIdentityOptions _microsoftIdentityOptions;
        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        };

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
            string optionsInstanceName,
            Action<DownstreamWebApiOptions>? calledDownstreamApiOptionsOverride,
            ClaimsPrincipal? user,
            StringContent? requestContent)
        {
            DownstreamWebApiOptions effectiveOptions = MergeOptions(optionsInstanceName, calledDownstreamApiOptionsOverride);

            if (string.IsNullOrEmpty(effectiveOptions.Scopes))
            {
                throw new ArgumentException(IDWebErrorMessage.ScopesNotConfiguredInConfigurationOrViaDelegate);
            }

            string? userflow;
            if (_microsoftIdentityOptions.IsB2C && string.IsNullOrEmpty(effectiveOptions.UserFlow))
            {
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
                if (requestContent != null)
                {
                    httpRequestMessage.Content = requestContent;
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
        public async Task<HttpResponseMessage> CallWebApiForAppAsync(
            string optionsInstanceName,
            Action<DownstreamWebApiOptions>? downstreamApiOptionsOverride = null,
            StringContent? requestContent = null)
        {
            DownstreamWebApiOptions effectiveOptions = MergeOptions(optionsInstanceName, downstreamApiOptionsOverride);

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
                if (requestContent != null)
                {
                    httpRequestMessage.Content = requestContent;
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

        /// <inheritdoc/>
        public async Task PostWebApiForUserAsync(
            string serviceName,
            string relativePath,
            Action<DownstreamWebApiOptions>? downstreamWebApiOptionsOverride = null,
            ClaimsPrincipal? user = null)
        {
            await CallWebApiForUserAsync(
                serviceName,
                PrepareOptions(relativePath, downstreamWebApiOptionsOverride, HttpMethod.Post),
                user,
                null).ConfigureAwait(false);
        }

        private static Action<DownstreamWebApiOptions> PrepareOptions(
           string relativePath,
           Action<DownstreamWebApiOptions>? downstreamWebApiOptionsOverride,
           HttpMethod httpMethod)
        {
            Action<DownstreamWebApiOptions> downstreamWebApiOptions;

            if (downstreamWebApiOptionsOverride == null)
            {
                downstreamWebApiOptions = options =>
                {
                    options.HttpMethod = httpMethod;
                    options.RelativePath = relativePath;
                };
            }
            else
            {
                downstreamWebApiOptions = options =>
                {
                    downstreamWebApiOptionsOverride(options);
                    options.HttpMethod = httpMethod;
                    options.RelativePath = relativePath;
                };
            }

            return downstreamWebApiOptions;
        }
    }
}
