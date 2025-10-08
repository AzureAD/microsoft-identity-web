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

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Extensions for the downstream web API.
    /// </summary>
    public static class DownstreamWebApiGenericExtensions
    {
        /// <summary>
        /// Get a strongly typed response from the web API.
        /// </summary>
        /// <typeparam name="TOutput">Output type.</typeparam>
        /// <param name="downstreamWebApi">The downstream web API.</param>
        /// <param name="serviceName">Name of the service describing the downstream web API. There can
        /// be several configuration named sections mapped to a <see cref="DownstreamWebApiOptions"/>,
        /// each for one downstream web API. You can pass-in null, but in that case <paramref name="downstreamWebApiOptionsOverride"/>
        /// needs to be set.</param>
        /// <param name="relativePath">Path to the API endpoint relative to the base URL specified in the configuration.</param>
        /// <param name="downstreamWebApiOptionsOverride">Overrides the options proposed in the configuration described
        /// by <paramref name="serviceName"/>.</param>
        /// <param name="user">[Optional] Claims representing a user. This is useful in platforms like Blazor
        /// or Azure Signal R, where the HttpContext is not available. In other platforms, the library
        /// will find the user from the HttpContext.</param>
        /// <param name="authenticationScheme">Authentication scheme. If null, will use OpenIdConnectDefault.AuthenticationScheme
        /// if called from a web app, and JwtBearerDefault.AuthenticationScheme if called from a web API.</param>
        /// <returns>A strongly typed response from the web API.</returns>
        [Obsolete("Use IDownstreamApi.GetForUserAsync in Microsoft.Identity.Abstractions, implemented in Microsoft.Identity.Web.DownstreamApi." +
        "See aka.ms/id-web-downstream-api-v2 for migration details.", false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
#if NET6_0_OR_GREATER
        [RequiresUnreferencedCode("Calls Microsoft.Identity.Web.DownstreamWebApiGenericExtensions.ConvertToOutput<TOutput>(TInput).")]
#endif
        public static async Task<TOutput?> GetForUserAsync<TOutput>(
            this IDownstreamWebApi downstreamWebApi,
            string serviceName,
            string relativePath,
            Action<DownstreamWebApiOptions>? downstreamWebApiOptionsOverride = null,
            ClaimsPrincipal? user = null,
            string? authenticationScheme = null)
            where TOutput : class
        {
            _ = Throws.IfNull(downstreamWebApi);

            HttpResponseMessage response = await downstreamWebApi.CallWebApiForUserAsync(
                serviceName,
                authenticationScheme,
                PrepareOptions(relativePath, downstreamWebApiOptionsOverride, HttpMethod.Get),
                user).ConfigureAwait(false);

            return await ConvertToOutputAsync<TOutput>(response).ConfigureAwait(false);
        }

        /// <summary>
        /// Call a web API with a strongly typed input, with an HttpGet.
        /// </summary>
        /// <typeparam name="TInput">Input type.</typeparam>
        /// <param name="downstreamWebApi">The downstream web API.</param>
        /// <param name="serviceName">Name of the service describing the downstream web API. There can
        /// be several configuration named sections mapped to a <see cref="DownstreamWebApiOptions"/>,
        /// each for one downstream web API. You can pass-in null, but in that case <paramref name="downstreamWebApiOptionsOverride"/>
        /// needs to be set.</param>
        /// <param name="inputData">Input data.</param>
        /// <param name="downstreamWebApiOptionsOverride">Overrides the options proposed in the configuration described
        /// by <paramref name="serviceName"/>.</param>
        /// <param name="user">[Optional] Claims representing a user. This is useful in platforms like Blazor
        /// or Azure Signal R, where the HttpContext is not available. In other platforms, the library
        /// will find the user from the HttpContext.</param>
        /// <param name="authenticationScheme">Authentication scheme. If null, will use OpenIdConnectDefault.AuthenticationScheme
        /// if called from a web app, and JwtBearerDefault.AuthenticationScheme if called from a web API.</param>
        /// <returns>The value returned by the downstream web API.</returns>
        [Obsolete("Use IDownstreamApi.GetForUserAsync in Microsoft.Identity.Abstractions, implemented in Microsoft.Identity.Web.DownstreamApi." +
        "See aka.ms/id-web-downstream-api-v2 for migration details.", false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
#if NET6_0_OR_GREATER
        [RequiresUnreferencedCode("Calls Microsoft.Identity.Web.DownstreamWebApiGenericExtensions.ConvertFromInput<TInput>(TInput).")]
#endif
        public static async Task GetForUserAsync<TInput>(
            this IDownstreamWebApi downstreamWebApi,
            string serviceName,
            TInput inputData,
            Action<DownstreamWebApiOptions>? downstreamWebApiOptionsOverride = null,
            ClaimsPrincipal? user = null,
            string? authenticationScheme = null)
        {
            _ = Throws.IfNull(downstreamWebApi);

            using StringContent? input = ConvertFromInput(inputData);

            await downstreamWebApi.CallWebApiForUserAsync(
             serviceName,
             authenticationScheme,
             downstreamWebApiOptionsOverride,
             user,
             input).ConfigureAwait(false);
        }

        /// <summary>
        /// Calls the web API with an HttpPost, providing strongly typed input and getting
        /// strongly typed output.
        /// </summary>
        /// <typeparam name="TOutput">Output type.</typeparam>
        /// <typeparam name="TInput">Input type.</typeparam>
        /// <param name="downstreamWebApi">The downstream web API.</param>
        /// <param name="serviceName">Name of the service describing the downstream web API. There can
        /// be several configuration named sections mapped to a <see cref="DownstreamWebApiOptions"/>,
        /// each for one downstream web API. You can pass-in null, but in that case <paramref name="downstreamWebApiOptionsOverride"/>
        /// needs to be set.</param>
        /// <param name="relativePath">Path to the API endpoint relative to the base URL specified in the configuration.</param>
        /// <param name="inputData">Input data sent to the API.</param>
        /// <param name="downstreamWebApiOptionsOverride">Overrides the options proposed in the configuration described
        /// by <paramref name="serviceName"/>.</param>
        /// <param name="user">[Optional] Claims representing a user. This is useful in platforms like Blazor
        /// or Azure Signal R, where the HttpContext is not available. In other platforms, the library
        /// will find the user from the HttpContext.</param>
        /// <param name="authenticationScheme">Authentication scheme. If null, will use OpenIdConnectDefault.AuthenticationScheme
        /// if called from a web app, and JwtBearerDefault.AuthenticationScheme if called from a web API.</param>
        /// <returns>A strongly typed response from the web API.</returns>
        [Obsolete("Use IDownstreamApi.PostForUserAsync in Microsoft.Identity.Abstractions, implemented in Microsoft.Identity.Web.DownstreamApi." +
        "See aka.ms/id-web-downstream-api-v2 for migration details.", false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
#if NET6_0_OR_GREATER
        [RequiresUnreferencedCode("Calls Microsoft.Identity.Web.DownstreamWebApiGenericExtensions.ConvertToOutput<TOutput>(TInput).")]
#endif
        public static async Task<TOutput?> PostForUserAsync<TOutput, TInput>(
            this IDownstreamWebApi downstreamWebApi,
            string serviceName,
            string relativePath,
            TInput inputData,
            Action<DownstreamWebApiOptions>? downstreamWebApiOptionsOverride = null,
            ClaimsPrincipal? user = null,
            string? authenticationScheme = null)
            where TOutput : class
        {
            _ = Throws.IfNull(downstreamWebApi);

            using StringContent? input = ConvertFromInput(inputData);

            HttpResponseMessage response = await downstreamWebApi.CallWebApiForUserAsync(
               serviceName,
               authenticationScheme,
               PrepareOptions(relativePath, downstreamWebApiOptionsOverride, HttpMethod.Post),
               user,
               input).ConfigureAwait(false);

            return await ConvertToOutputAsync<TOutput>(response).ConfigureAwait(false);
        }

        /// <summary>
        /// Calls the web API endpoint with an HttpPut, providing strongly typed input data.
        /// </summary>
        /// <typeparam name="TInput">Input type.</typeparam>
        /// <param name="downstreamWebApi">The downstream web API.</param>
        /// <param name="serviceName">Name of the service describing the downstream web API. There can
        /// be several configuration named sections mapped to a <see cref="DownstreamWebApiOptions"/>,
        /// each for one downstream web API. You can pass-in null, but in that case <paramref name="downstreamWebApiOptionsOverride"/>
        /// needs to be set.</param>
        /// <param name="relativePath">Path to the API endpoint relative to the base URL specified in the configuration.</param>
        /// <param name="inputData">Input data sent to the API.</param>
        /// <param name="downstreamWebApiOptionsOverride">Overrides the options proposed in the configuration described
        /// by <paramref name="serviceName"/>.</param>
        /// <param name="user">[Optional] Claims representing a user. This is useful in platforms like Blazor
        /// or Azure Signal R, where the HttpContext is not available. In other platforms, the library
        /// will find the user from the HttpContext.</param>
        /// <param name="authenticationScheme">Authentication scheme. If null, will use OpenIdConnectDefault.AuthenticationScheme
        /// if called from a web app, and JwtBearerDefault.AuthenticationScheme if called from a web API.</param>
        /// <returns>The value returned by the downstream web API.</returns>
        [Obsolete("Use IDownstreamApi.PutForUserAsync in Microsoft.Identity.Abstractions, implemented in Microsoft.Identity.Web.DownstreamApi." +
        "See aka.ms/id-web-downstream-api-v2 for migration details.", false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
#if NET6_0_OR_GREATER
        [RequiresUnreferencedCode("Calls Microsoft.Identity.Web.DownstreamWebApiGenericExtensions.ConvertFromInput<TInput>(TInput).")]
#endif
        public static async Task PutForUserAsync<TInput>(
            this IDownstreamWebApi downstreamWebApi,
            string serviceName,
            string relativePath,
            TInput inputData,
            Action<DownstreamWebApiOptions>? downstreamWebApiOptionsOverride = null,
            ClaimsPrincipal? user = null,
            string? authenticationScheme = null)
        {
            _ = Throws.IfNull(downstreamWebApi);

            using StringContent? input = ConvertFromInput(inputData);

            await downstreamWebApi.CallWebApiForUserAsync(
              serviceName,
              authenticationScheme,
              PrepareOptions(relativePath, downstreamWebApiOptionsOverride, HttpMethod.Put),
              user,
              input).ConfigureAwait(false);
        }

        /// <summary>
        /// Calls the web API endpoint with an HttpPut, provinding strongly typed input data
        /// and getting back strongly typed data.
        /// </summary>
        /// <typeparam name="TOutput">Output type.</typeparam>
        /// <typeparam name="TInput">Input type.</typeparam>
        /// <param name="downstreamWebApi">The downstream web API.</param>
        /// <param name="serviceName">Name of the service describing the downstream web API. There can
        /// be several configuration named sections mapped to a <see cref="DownstreamWebApiOptions"/>,
        /// each for one downstream web API. You can pass-in null, but in that case <paramref name="downstreamWebApiOptionsOverride"/>
        /// needs to be set.</param>
        /// <param name="relativePath">Path to the API endpoint relative to the base URL specified in the configuration.</param>
        /// <param name="inputData">Input data sent to the API.</param>
        /// <param name="downstreamWebApiOptionsOverride">Overrides the options proposed in the configuration described
        /// by <paramref name="serviceName"/>.</param>
        /// <param name="user">[Optional] Claims representing a user. This is useful in platforms like Blazor
        /// or Azure Signal R, where the HttpContext is not available. In other platforms, the library
        /// will find the user from the HttpContext.</param>
        /// <param name="authenticationScheme">Authentication scheme. If null, will use OpenIdConnectDefault.AuthenticationScheme
        /// if called from a web app, and JwtBearerDefault.AuthenticationScheme if called from a web API.</param>
        /// <returns>A strongly typed response from the web API.</returns>
        [Obsolete("Use IDownstreamApi.PutForUserAsync in Microsoft.Identity.Abstractions, implemented in Microsoft.Identity.Web.DownstreamApi." +
        "See aka.ms/id-web-downstream-api-v2 for migration details.", false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
#if NET6_0_OR_GREATER
        [RequiresUnreferencedCode("Calls Microsoft.Identity.Web.DownstreamWebApiGenericExtensions.ConvertToOutput<TOutput>(TInput).")]
#endif
        public static async Task<TOutput?> PutForUserAsync<TOutput, TInput>(
            this IDownstreamWebApi downstreamWebApi,
            string serviceName,
            string relativePath,
            TInput inputData,
            Action<DownstreamWebApiOptions>? downstreamWebApiOptionsOverride = null,
            ClaimsPrincipal? user = null,
            string? authenticationScheme = null)
            where TOutput : class
        {
            _ = Throws.IfNull(downstreamWebApi);

            using StringContent? input = ConvertFromInput(inputData);

            HttpResponseMessage response = await downstreamWebApi.CallWebApiForUserAsync(
               serviceName,
               authenticationScheme,
               PrepareOptions(relativePath, downstreamWebApiOptionsOverride, HttpMethod.Put),
               user,
               input).ConfigureAwait(false);

            return await ConvertToOutputAsync<TOutput>(response).ConfigureAwait(false);
        }

        /// <summary>
        /// Call a web API endpoint with an HttpGet,
        /// and return strongly typed data.
        /// </summary>
        /// <typeparam name="TOutput">Output type.</typeparam>
        /// <param name="downstreamWebApi">The downstream web API.</param>
        /// <param name="serviceName">Name of the service describing the downstream web API. There can
        /// be several configuration named sections mapped to a <see cref="DownstreamWebApiOptions"/>,
        /// each for one downstream web API. You can pass-in null, but in that case <paramref name="downstreamWebApiOptionsOverride"/>
        /// needs to be set.</param>
        /// <param name="downstreamWebApiOptionsOverride">Overrides the options proposed in the configuration described
        /// by <paramref name="serviceName"/>.</param>
        /// <param name="user">[Optional] Claims representing a user. This is useful in platforms like Blazor
        /// or Azure Signal R, where the HttpContext is not available. In other platforms, the library
        /// will find the user from the HttpContext.</param>
        /// <param name="authenticationScheme">Authentication scheme. If null, will use OpenIdConnectDefault.AuthenticationScheme
        /// if called from a web app, and JwtBearerDefault.AuthenticationScheme if called from a web API.</param>
        /// <returns>The value returned by the downstream web API.</returns>
        [Obsolete("Use IDownstreamApi.CallWebApiForUserAsync in Microsoft.Identity.Abstractions, implemented in Microsoft.Identity.Web.DownstreamApi." +
        "See aka.ms/id-web-downstream-api-v2 for migration details.", false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
#if NET6_0_OR_GREATER
        [RequiresUnreferencedCode("Calls Microsoft.Identity.Web.DownstreamWebApiGenericExtensions.ConvertToOutput<TOutput>(TInput).")]
#endif
        public static async Task<TOutput?> CallWebApiForUserAsync<TOutput>(
            this IDownstreamWebApi downstreamWebApi,
            string serviceName,
            Action<DownstreamWebApiOptions>? downstreamWebApiOptionsOverride = null,
            ClaimsPrincipal? user = null,
            string? authenticationScheme = null)
            where TOutput : class
        {
            _ = Throws.IfNull(downstreamWebApi);

            HttpResponseMessage response = await downstreamWebApi.CallWebApiForUserAsync(
              serviceName,
              authenticationScheme,
              downstreamWebApiOptionsOverride,
              user).ConfigureAwait(false);

            return await ConvertToOutputAsync<TOutput>(response).ConfigureAwait(false);
        }

#if NET6_0_OR_GREATER
        [RequiresUnreferencedCode("Calls System.Text.Json.JsonSerializer.Serialize<TValue>(TValue, JsonSerializerOptions).")]
#endif
        private static StringContent ConvertFromInput<TInput>(TInput input)
        {
            return new StringContent(JsonSerializer.Serialize(input), Encoding.UTF8, "application/json");
        }

#if NET6_0_OR_GREATER
        [RequiresUnreferencedCode("Calls System.Text.Json.JsonSerializer.Deserialize<TValue>(String, JsonSerializerOptions).")]
#endif
        private static async Task<TOutput?> ConvertToOutputAsync<TOutput>(HttpResponseMessage response)
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
