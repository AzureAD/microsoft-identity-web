// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
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
        private const string AuthSchemeDstsSamlBearer = "http://schemas.microsoft.com/dsts/saml2-bearer";

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
#if NET8_0_OR_GREATER
        [RequiresUnreferencedCode("Calls JsonSerializer.Serialize<TInput>")]
        [RequiresDynamicCode("Calls JsonSerializer.Serialize<TInput>")]
#endif
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

            return await DeserializeOutputAsync<TOutput>(response, effectiveOptions).ConfigureAwait(false);
        }

        /// <inheritdoc/>
#if NET8_0_OR_GREATER
        [RequiresUnreferencedCode("Calls JsonSerializer.Serialize<TInput>")]
        [RequiresDynamicCode("Calls JsonSerializer.Serialize<TInput>")]
#endif
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

            return await DeserializeOutputAsync<TOutput>(response, effectiveOptions).ConfigureAwait(false);
        }

        /// <inheritdoc/>
#if NET8_0_OR_GREATER
        [RequiresUnreferencedCode("Calls JsonSerializer.Serialize<TInput>")]
        [RequiresDynamicCode("Calls JsonSerializer.Serialize<TInput>")]
#endif
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async Task<TOutput?> CallApiForAppAsync<TOutput>(string serviceName,
            Action<DownstreamApiOptions>? downstreamApiOptionsOverride = null,
            CancellationToken cancellationToken = default) where TOutput : class
        {
            DownstreamApiOptions effectiveOptions = MergeOptions(serviceName, downstreamApiOptionsOverride);
            HttpResponseMessage response = await CallApiInternalAsync(serviceName, effectiveOptions, true,
                                                                          null, null, cancellationToken).ConfigureAwait(false);

            return await DeserializeOutputAsync<TOutput>(response, effectiveOptions).ConfigureAwait(false);
        }

        /// <inheritdoc/>
#if NET8_0_OR_GREATER
        [RequiresUnreferencedCode("Calls JsonSerializer.Serialize<TInput>")]
        [RequiresDynamicCode("Calls JsonSerializer.Serialize<TInput>")]
#endif
        public async Task<TOutput?> CallApiForUserAsync<TOutput>(
            string? serviceName,
            Action<DownstreamApiOptions>? downstreamApiOptionsOverride = null,
            ClaimsPrincipal? user = null,
            CancellationToken cancellationToken = default) where TOutput : class
        {
            DownstreamApiOptions effectiveOptions = MergeOptions(serviceName, downstreamApiOptionsOverride);
            HttpResponseMessage response = await CallApiInternalAsync(serviceName, effectiveOptions, false,
                                                                          null, user, cancellationToken).ConfigureAwait(false);
            return await DeserializeOutputAsync<TOutput>(response, effectiveOptions).ConfigureAwait(false);
        }

#if NET8_0_OR_GREATER
        /// <inheritdoc/>
        public async Task<TOutput?> CallApiForUserAsync<TInput, TOutput>(
            string? serviceName,
            TInput input,
            JsonTypeInfo<TInput> inputJsonTypeInfo,
            JsonTypeInfo<TOutput> outputJsonTypeInfo,
            Action<DownstreamApiOptions>? downstreamApiOptionsOverride = null,
            ClaimsPrincipal? user = default,
            CancellationToken cancellationToken = default)
            where TOutput : class
        {
            DownstreamApiOptions effectiveOptions = MergeOptions(serviceName, downstreamApiOptionsOverride);
            HttpContent? effectiveInput = SerializeInput(input, effectiveOptions, inputJsonTypeInfo);

            HttpResponseMessage response = await CallApiInternalAsync(serviceName, effectiveOptions, false,
                                                                          effectiveInput, user, cancellationToken).ConfigureAwait(false);

            // Only dispose the HttpContent if was created here, not provided by the caller.
            if (input is not HttpContent)
            {
                effectiveInput?.Dispose();
            }

            return await DeserializeOutputAsync<TOutput>(response, effectiveOptions, outputJsonTypeInfo).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<TOutput?> CallApiForUserAsync<TOutput>(
            string serviceName,
            JsonTypeInfo<TOutput> outputJsonTypeInfo,
            Action<DownstreamApiOptions>? downstreamApiOptionsOverride = null,
            ClaimsPrincipal? user = default,
            CancellationToken cancellationToken = default)
            where TOutput : class
        {
            DownstreamApiOptions effectiveOptions = MergeOptions(serviceName, downstreamApiOptionsOverride);
            HttpResponseMessage response = await CallApiInternalAsync(serviceName, effectiveOptions, false,
                                                                          null, user, cancellationToken).ConfigureAwait(false);
            return await DeserializeOutputAsync<TOutput>(response, effectiveOptions, outputJsonTypeInfo).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<TOutput?> CallApiForAppAsync<TInput, TOutput>(
            string? serviceName,
            TInput input,
            JsonTypeInfo<TInput> inputJsonTypeInfo,
            JsonTypeInfo<TOutput> outputJsonTypeInfo,
            Action<DownstreamApiOptions>? downstreamApiOptionsOverride = null,
            CancellationToken cancellationToken = default)
            where TOutput : class
        {
            DownstreamApiOptions effectiveOptions = MergeOptions(serviceName, downstreamApiOptionsOverride);
            HttpContent? effectiveInput = SerializeInput(input, effectiveOptions, inputJsonTypeInfo);
            HttpResponseMessage response = await CallApiInternalAsync(serviceName, effectiveOptions, true,
                                                                          effectiveInput, null, cancellationToken).ConfigureAwait(false);

            // Only dispose the HttpContent if was created here, not provided by the caller.
            if (input is not HttpContent)
            {
                effectiveInput?.Dispose();
            }

            return await DeserializeOutputAsync<TOutput>(response, effectiveOptions, outputJsonTypeInfo).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<TOutput?> CallApiForAppAsync<TOutput>(
            string serviceName,
            JsonTypeInfo<TOutput> outputJsonTypeInfo,
            Action<DownstreamApiOptions>? downstreamApiOptionsOverride = null,
            CancellationToken cancellationToken = default)
            where TOutput : class
        {
            DownstreamApiOptions effectiveOptions = MergeOptions(serviceName, downstreamApiOptionsOverride);
            HttpResponseMessage response = await CallApiInternalAsync(serviceName, effectiveOptions, true,
                                                                          null, null, cancellationToken).ConfigureAwait(false);

            return await DeserializeOutputAsync<TOutput>(response, effectiveOptions, outputJsonTypeInfo).ConfigureAwait(false);
        }

        internal static HttpContent? SerializeInput<TInput>(TInput input, DownstreamApiOptions effectiveOptions, JsonTypeInfo<TInput> inputJsonTypeInfo)
        {
            HttpContent? httpContent;

            if (effectiveOptions.Serializer != null)
            {
                httpContent = effectiveOptions.Serializer(input);
            }
            else
            {
                // if the input is already an HttpContent, it's used as is, and should already contain a ContentType.
                httpContent = input switch
                {
                    HttpContent content => content,
                    string str when !string.IsNullOrEmpty(effectiveOptions.ContentType) && effectiveOptions.ContentType.StartsWith("text", StringComparison.OrdinalIgnoreCase) => new StringContent(str),
                    string str => new StringContent(JsonSerializer.Serialize(str, inputJsonTypeInfo),
                        Encoding.UTF8,
                        "application/json"),
                    byte[] bytes => new ByteArrayContent(bytes),
                    Stream stream => new StreamContent(stream),
                    null => null,
                    _ => new StringContent(
                        JsonSerializer.Serialize(input, inputJsonTypeInfo),
                        Encoding.UTF8,
                        "application/json"),
                };
            }
            return httpContent;
        }

        internal static async Task<TOutput?> DeserializeOutputAsync<TOutput>(HttpResponseMessage response, DownstreamApiOptions effectiveOptions, JsonTypeInfo<TOutput> outputJsonTypeInfo)
             where TOutput : class
        {
            return await DeserializeOutputImplAsync<TOutput>(response, effectiveOptions, outputJsonTypeInfo);
        }
#endif

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

#if NET7_0_OR_GREATER
        [RequiresUnreferencedCode("Calls JsonSerializer.Serialize<TInput>")]
        [RequiresDynamicCode("Calls JsonSerializer.Serialize<TInput>")]
#endif
        internal static HttpContent? SerializeInput<TInput>(TInput input, DownstreamApiOptions effectiveOptions)
        {
            HttpContent? httpContent;

            if (effectiveOptions.Serializer != null)
            {
                httpContent = effectiveOptions.Serializer(input);
            }
            else
            {
                // if the input is already an HttpContent, it's used as is, and should already contain a ContentType.
                httpContent = input switch
                {
                    HttpContent content => content,
                    string str when !string.IsNullOrEmpty(effectiveOptions.ContentType) && effectiveOptions.ContentType.StartsWith("text", StringComparison.OrdinalIgnoreCase) => new StringContent(str),
                    string str => new StringContent(
                        JsonSerializer.Serialize(str),
                        Encoding.UTF8,
                        "application/json"),
                    byte[] bytes => new ByteArrayContent(bytes),
                    Stream stream => new StreamContent(stream),
                    null => null,
                    _ => new StringContent(
                        JsonSerializer.Serialize(input),
                        Encoding.UTF8,
                        "application/json"),
                };
            }
            return httpContent;
        }

#if NET7_0_OR_GREATER
        [RequiresUnreferencedCode("Calls JsonSerializer.Serialize<TInput>")]
        [RequiresDynamicCode("Calls JsonSerializer.Serialize<TInput>")]
#endif
        internal static async Task<TOutput?> DeserializeOutputAsync<TOutput>(HttpResponseMessage response, DownstreamApiOptions effectiveOptions)
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

            string? mediaType = content.Headers.ContentType?.MediaType;

            if (effectiveOptions.Deserializer != null)
            {
                return effectiveOptions.Deserializer(content) as TOutput;
            }
            else if (typeof(TOutput).IsAssignableFrom(typeof(HttpContent)))
            {
                return content as TOutput;
            }
            else
            {
                string stringContent = await content.ReadAsStringAsync();
                if (mediaType == "application/json")
                {
                    return JsonSerializer.Deserialize<TOutput>(stringContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });                    
                }
                if (mediaType != null && !mediaType.StartsWith("text/", StringComparison.OrdinalIgnoreCase))
                {
                    // Handle other content types here
                    throw new NotSupportedException("Content type not supported. Provide your own deserializer. ");
                }
                return stringContent as TOutput;
            }
        }

        private static async Task<TOutput?> DeserializeOutputImplAsync<TOutput>(HttpResponseMessage response, DownstreamApiOptions effectiveOptions, JsonTypeInfo<TOutput> outputJsonTypeInfo)
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

            string? mediaType = content.Headers.ContentType?.MediaType;

            if (effectiveOptions.Deserializer != null)
            {
                return effectiveOptions.Deserializer(content) as TOutput;
            }
            else if (typeof(TOutput).IsAssignableFrom(typeof(HttpContent)))
            {
                return content as TOutput;
            }
            else
            {
                string stringContent = await content.ReadAsStringAsync();
                if (mediaType == "application/json")
                {
                    return JsonSerializer.Deserialize<TOutput>(stringContent, outputJsonTypeInfo);
                }
                if (mediaType != null && !mediaType.StartsWith("text/", StringComparison.OrdinalIgnoreCase))
                {
                    // Handle other content types here
                    throw new NotSupportedException("Content type not supported. Provide your own deserializer. ");
                }
                return stringContent as TOutput;
            }
        }

        internal /* for tests */ async Task<HttpResponseMessage> CallApiInternalAsync(
            string? serviceName,
            DownstreamApiOptions effectiveOptions,
            bool appToken,
            HttpContent? content = null,
            ClaimsPrincipal? user = null,
            CancellationToken cancellationToken = default)
        {
            // Downstream API URI
            string apiUrl = effectiveOptions.GetApiUrl();

            // Create an HTTP request message
            using HttpRequestMessage httpRequestMessage = new(
                new HttpMethod(effectiveOptions.HttpMethod),
                apiUrl);

            await UpdateRequestAsync(httpRequestMessage, content, effectiveOptions, appToken, user, cancellationToken);

            using HttpClient client = string.IsNullOrEmpty(serviceName) ? _httpClientFactory.CreateClient() : _httpClientFactory.CreateClient(serviceName);

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

        internal /* internal for test */ async Task UpdateRequestAsync(
            HttpRequestMessage httpRequestMessage,
            HttpContent? content,
            DownstreamApiOptions effectiveOptions,
            bool appToken,
            ClaimsPrincipal? user,
            CancellationToken cancellationToken)
        {
            AddCallerSDKTelemetry(effectiveOptions);

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

                if (authorizationHeader.StartsWith(AuthSchemeDstsSamlBearer, StringComparison.OrdinalIgnoreCase))
                {
                    // TryAddWithoutValidation method bypasses strict validation, allowing non-standard headers to be added for custom Header schemes that cannot be parsed.
                    httpRequestMessage.Headers.TryAddWithoutValidation(Authorization, authorizationHeader);
                }
                else
                {
                    httpRequestMessage.Headers.Add(Authorization, authorizationHeader);
                }
            }
            else
            {
                Logger.UnauthenticatedApiCall(_logger, null);
            }
            if (!string.IsNullOrEmpty(effectiveOptions.AcceptHeader))
            {
                httpRequestMessage.Headers.Accept.ParseAdd(effectiveOptions.AcceptHeader);
            }

            // Add extra headers if specified directly on DownstreamApiOptions
            var extraHeaders = GetExtraHeaderParameters(effectiveOptions);
            if (extraHeaders != null)
            {
                foreach (var header in extraHeaders)
                {
                    httpRequestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }

            // Add extra query parameters if specified directly on DownstreamApiOptions
            var extraQueryParams = GetExtraQueryParameters(effectiveOptions);
            if (extraQueryParams != null && extraQueryParams.Count > 0)
            {
                var uriBuilder = new UriBuilder(httpRequestMessage.RequestUri!);
                var existingQuery = uriBuilder.Query;
                var queryString = new StringBuilder(existingQuery);
                
                foreach (var queryParam in extraQueryParams)
                {
                    if (queryString.Length > 1) // if there are existing query parameters
                    {
                        queryString.Append('&');
                    }
                    else if (queryString.Length == 0)
                    {
                        queryString.Append('?');
                    }
                    
                    queryString.Append(Uri.EscapeDataString(queryParam.Key));
                    queryString.Append('=');
                    queryString.Append(Uri.EscapeDataString(queryParam.Value));
                }
                
                uriBuilder.Query = queryString.ToString().TrimStart('?');
                httpRequestMessage.RequestUri = uriBuilder.Uri;
            }

            // Opportunity to change the request message
            effectiveOptions.CustomizeHttpRequestMessage?.Invoke(httpRequestMessage);
        }

        internal /* for test */ static Dictionary<string, string> CallerSDKDetails { get; } = new()
          {
              { "caller-sdk-id", "IdWeb_1" },  
              { "caller-sdk-ver", IdHelper.GetIdWebVersion() }
          };

        private static void AddCallerSDKTelemetry(DownstreamApiOptions effectiveOptions)
        {
            if (effectiveOptions.AcquireTokenOptions.ExtraQueryParameters == null)
            {
                effectiveOptions.AcquireTokenOptions.ExtraQueryParameters = CallerSDKDetails;
            }
            else
            {
                effectiveOptions.AcquireTokenOptions.ExtraQueryParameters["caller-sdk-id"] =
                    CallerSDKDetails["caller-sdk-id"];
                effectiveOptions.AcquireTokenOptions.ExtraQueryParameters["caller-sdk-ver"] =
                    CallerSDKDetails["caller-sdk-ver"];
            }
        }

        /// <summary>
        /// Gets the extra header parameters from DownstreamApiOptions if they exist.
        /// This method uses reflection to check if the property exists to maintain compatibility
        /// with different versions of Microsoft.Identity.Abstractions package.
        /// Checks for both "ExtraHeaderParameters" and "ExtraHeadersParameters" for compatibility.
        /// </summary>
        /// <param name="options">The DownstreamApiOptions instance.</param>
        /// <returns>Extra header parameters if they exist, null otherwise.</returns>
        private static IDictionary<string, string>? GetExtraHeaderParameters(DownstreamApiOptions options)
        {
            try
            {
                // First try "ExtraHeaderParameters" (preferred naming)
                var propertyInfo = options.GetType().GetProperty("ExtraHeaderParameters");
                if (propertyInfo != null)
                {
                    var value = propertyInfo.GetValue(options) as IDictionary<string, string>;
                    if (value != null && value.Count > 0)
                    {
                        return value;
                    }
                }

                // Fallback to "ExtraHeadersParameters" (for compatibility)
                propertyInfo = options.GetType().GetProperty("ExtraHeadersParameters");
                if (propertyInfo != null)
                {
                    var value = propertyInfo.GetValue(options) as IDictionary<string, string>;
                    if (value != null && value.Count > 0)
                    {
                        return value;
                    }
                }
            }
            catch
            {
                // If property doesn't exist or any error occurs, return null
            }
            return null;
        }

        /// <summary>
        /// Gets the extra query parameters from DownstreamApiOptions if they exist.
        /// This method uses reflection to check if the property exists to maintain compatibility
        /// with different versions of Microsoft.Identity.Abstractions package.
        /// </summary>
        /// <param name="options">The DownstreamApiOptions instance.</param>
        /// <returns>Extra query parameters if they exist, null otherwise.</returns>
        private static IDictionary<string, string>? GetExtraQueryParameters(DownstreamApiOptions options)
        {
            try
            {
                var propertyInfo = options.GetType().GetProperty("ExtraQueryParameters");
                if (propertyInfo != null)
                {
                    var directValue = propertyInfo.GetValue(options) as IDictionary<string, string>;
                    if (directValue != null && directValue.Count > 0)
                    {
                        return directValue;
                    }
                }
            }
            catch
            {
                // If property doesn't exist or any error occurs, return null
            }
            
            return null;
        }
    }
}
