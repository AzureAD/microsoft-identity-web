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
using Microsoft.Extensions.Options;
using Microsoft.Identity.Abstractions;

namespace Microsoft.Identity.Web
{
    /// <inheritdoc/>
    internal partial class DownstreamRestApi : IDownstreamRestApi
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Task<HttpResponseMessage> CallRestApiAsync(
            string? serviceName,
            Action<DownstreamRestApiOptions>? downstreamRestApiOptionsOverride = null,
            ClaimsPrincipal? user = null,
            HttpContent? content = null,
            CancellationToken cancellationToken = default)
        {
            DownstreamRestApiOptions effectiveOptions = MergeOptions(serviceName, downstreamRestApiOptionsOverride);
            return CallRestApiInternalAsync(serviceName, effectiveOptions, effectiveOptions.RequestAppToken, content,
                                            user, cancellationToken);
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Task<HttpResponseMessage> CallRestApiAsync(
            DownstreamRestApiOptions downstreamRestApiOptions,
            ClaimsPrincipal? user = null,
            HttpContent? content = null,
            CancellationToken cancellationToken = default)
        {
            return CallRestApiInternalAsync(null, downstreamRestApiOptions, downstreamRestApiOptions.RequestAppToken, content,
                                user, cancellationToken);
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Task<HttpResponseMessage> CallRestApiForUserAsync(
            string? serviceName,
            Action<DownstreamRestApiOptions>? downstreamRestApiOptionsOverride = null,
            ClaimsPrincipal? user = null,
            HttpContent? content = null,
            CancellationToken cancellationToken = default)
        {
            DownstreamRestApiOptions effectiveOptions = MergeOptions(serviceName, downstreamRestApiOptionsOverride);
            return CallRestApiInternalAsync(serviceName, effectiveOptions, false, content, user, cancellationToken);
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Task<HttpResponseMessage> CallRestApiForAppAsync(
           string? serviceName,
           Action<DownstreamRestApiOptions>? downstreamRestApiOptionsOverride = null,
           HttpContent? content = null,
           CancellationToken cancellationToken = default)
        {
            DownstreamRestApiOptions effectiveOptions = MergeOptions(serviceName, downstreamRestApiOptionsOverride);
            return CallRestApiInternalAsync(serviceName, effectiveOptions, true, content, null, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<TOutput?> CallRestApiForUserAsync<TInput, TOutput>(
            string? serviceName,
            TInput input,
            Action<DownstreamRestApiOptions>? downstreamRestApiOptionsOverride = null,
            ClaimsPrincipal? user = null,
            CancellationToken cancellationToken = default) where TOutput : class
        {
            DownstreamRestApiOptions effectiveOptions = MergeOptions(serviceName, downstreamRestApiOptionsOverride);
            HttpContent? effectiveInput = SerializeInput(input, effectiveOptions);

            HttpResponseMessage response = await CallRestApiInternalAsync(serviceName, effectiveOptions, false,
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
        public async Task<TOutput?> CallRestApiForAppAsync<TInput, TOutput>(string? serviceName, TInput input, Action<DownstreamRestApiOptions>? downstreamRestApiOptionsOverride = null, CancellationToken cancellationToken = default) where TOutput : class
        {
            DownstreamRestApiOptions effectiveOptions = MergeOptions(serviceName, downstreamRestApiOptionsOverride);
            HttpContent? effectiveInput = SerializeInput(input, effectiveOptions);
            HttpResponseMessage response = await CallRestApiInternalAsync(serviceName, effectiveOptions, true,
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
        public async Task<TOutput?> CallRestApiForAppAsync<TOutput>(string serviceName, Action<DownstreamRestApiOptions>? downstreamRestApiOptionsOverride = null, CancellationToken cancellationToken = default) where TOutput : class
        {
            DownstreamRestApiOptions effectiveOptions = MergeOptions(serviceName, downstreamRestApiOptionsOverride);
            HttpResponseMessage response = await CallRestApiInternalAsync(serviceName, effectiveOptions, true,
                                                                          null, null, cancellationToken).ConfigureAwait(false);

            return await DeserializeOutput<TOutput>(response, effectiveOptions).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<TOutput?> CallRestApiForUserAsync<TOutput>(
            string? serviceName,
            Action<DownstreamRestApiOptions>? downstreamRestApiOptionsOverride = null,
            ClaimsPrincipal? user = null,
            CancellationToken cancellationToken = default) where TOutput : class
        {
            DownstreamRestApiOptions effectiveOptions = MergeOptions(serviceName, downstreamRestApiOptionsOverride);
            HttpResponseMessage response = await CallRestApiInternalAsync(serviceName, effectiveOptions, false,
                                                                          null, user, cancellationToken).ConfigureAwait(false);
            return await DeserializeOutput<TOutput>(response, effectiveOptions).ConfigureAwait(false);
        }

        /// <summary>
        /// Merge the options from configuration and override from caller.
        /// </summary>
        /// <param name="optionsInstanceName">Named configuration.</param>
        /// <param name="calledApiOptionsOverride">Delegate to override the configuration.</param>
        internal /* for tests */ DownstreamRestApiOptions MergeOptions(
            string? optionsInstanceName,
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

        /// <summary>
        /// Merge the options from configuration and override from caller.
        /// </summary>
        /// <param name="optionsInstanceName">Named configuration.</param>
        /// <param name="calledApiOptionsOverride">Delegate to override the configuration.</param>
        /// <param name="httpMethod">Http method overriding the configuration options.</param>
        internal /* for tests */ DownstreamRestApiOptions MergeOptions(
            string? optionsInstanceName,
            Action<DownstreamRestApiOptionsReadOnlyHttpMethod>? calledApiOptionsOverride, HttpMethod httpMethod)
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

            DownstreamRestApiOptionsReadOnlyHttpMethod clonedOptions = new DownstreamRestApiOptionsReadOnlyHttpMethod(options, httpMethod);
            calledApiOptionsOverride?.Invoke(clonedOptions);
            return clonedOptions;
        }

        private static HttpContent? SerializeInput<TInput>(TInput input, DownstreamRestApiOptions effectiveOptions)
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

        private static async Task<TOutput?> DeserializeOutput<TOutput>(HttpResponseMessage response, DownstreamRestApiOptions effectiveOptions)
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

        private async Task<HttpResponseMessage> CallRestApiInternalAsync(
            string? serviceName,
            DownstreamRestApiOptions effectiveOptions,
            bool appToken,
            HttpContent? content = null,
            ClaimsPrincipal? user = null,
            CancellationToken cancellationToken = default)
        {
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

            // Obtention of the authorization header (except when calling an anonymous endpoint
            // which is done by not specifying any scopes
            if (effectiveOptions.Scopes != null && effectiveOptions.Scopes.Any())
            {
                string authorizationHeader = appToken ?
                    await _authorizationHeaderProvider.CreateAuthorizationHeaderForAppAsync(
                                            effectiveOptions.Scopes.FirstOrDefault()!,
                                            effectiveOptions,
                                            cancellationToken).ConfigureAwait(false) :
                    await _authorizationHeaderProvider.CreateAuthorizationHeaderForUserAsync(
                                            effectiveOptions.Scopes,
                                            effectiveOptions,
                                            user,
                                            cancellationToken).ConfigureAwait(false);
                httpRequestMessage.Headers.Add(Authorization, authorizationHeader);
            }
            // Opportunity to change the request message
            effectiveOptions.CustomizeHttpRequestMessage?.Invoke(httpRequestMessage);

            // Send the HTTP message
            using HttpClient client = string.IsNullOrEmpty(serviceName) ? _httpClientFactory.CreateClient() : _httpClientFactory.CreateClient(serviceName);
            return await client.SendAsync(httpRequestMessage, cancellationToken).ConfigureAwait(false);
        }
    }
}
