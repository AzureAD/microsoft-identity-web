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
    /// 
    /// </summary>
    public partial class DownstreamWebApi
    {
        /// <inheritdoc/>
        public async Task<TOutput?> CallWebApiForUserAsync<TInput, TOutput>(
            string optionsInstanceName,
            TInput input,
            Action<DownstreamWebApiOptions>? downstreamWebApiOptionsOverride = null,
            ClaimsPrincipal? user = null)
            where TOutput : class
        {
            HttpResponseMessage response = await CallWebApiForUserAsync(
                optionsInstanceName,
                downstreamWebApiOptionsOverride,
                user,
                ConvertFromInput(input)).ConfigureAwait(false);

            return await ConvertToOutput<TOutput>(response).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<TOutput?> GetWebApiForUserAsync<TOutput>(
            string serviceName,
            string relativePath,
            Action<DownstreamWebApiOptions>? downstreamWebApiOptionsOverride = null,
            ClaimsPrincipal? user = null)
            where TOutput : class
        {
            HttpResponseMessage response = await CallWebApiForUserAsync(
                serviceName,
                PrepareOptions(relativePath, downstreamWebApiOptionsOverride, HttpMethod.Get),
                user,
                null).ConfigureAwait(false);

            return await ConvertToOutput<TOutput>(response).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<TOutput?> PostWebApiForUserAsync<TOutput, TInput>(
            string serviceName,
            string relativePath,
            TInput data,
            Action<DownstreamWebApiOptions>? downstreamWebApiOptionsOverride = null,
            ClaimsPrincipal? user = null)
            where TOutput : class
        {
            HttpResponseMessage response = await CallWebApiForUserAsync(
               serviceName,
               PrepareOptions(relativePath, downstreamWebApiOptionsOverride, HttpMethod.Post),
               user,
               ConvertFromInput(data)).ConfigureAwait(false);

            return await ConvertToOutput<TOutput>(response).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task PutWebApiForUserAsync<TInput>(
            string serviceName,
            string relativePath,
            TInput data,
            Action<DownstreamWebApiOptions>? downstreamWebApiOptionsOverride = null,
            ClaimsPrincipal? user = null)
        {
            await CallWebApiForUserAsync(
              serviceName,
              PrepareOptions(relativePath, downstreamWebApiOptionsOverride, HttpMethod.Put),
              user,
              ConvertFromInput(data)).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<TOutput?> PutWebApiForUserAsync<TOutput, TInput>(
            string serviceName,
            string relativePath,
            TInput data,
            Action<DownstreamWebApiOptions>? downstreamWebApiOptionsOverride = null,
            ClaimsPrincipal? user = null)
            where TOutput : class
        {
            HttpResponseMessage response = await CallWebApiForUserAsync(
               serviceName,
               PrepareOptions(relativePath, downstreamWebApiOptionsOverride, HttpMethod.Put),
               user,
               ConvertFromInput(data)).ConfigureAwait(false);

            return await ConvertToOutput<TOutput>(response).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<TOutput?> CallWebApiForUserAsync<TOutput>(
            string serviceName,
            Action<DownstreamWebApiOptions>? downstreamWebApiOptionsOverride = null,
            ClaimsPrincipal? user = null)
            where TOutput : class
        {
            HttpResponseMessage response = await CallWebApiForUserAsync(
              serviceName,
              downstreamWebApiOptionsOverride,
              user,
              null).ConfigureAwait(false);

            return await ConvertToOutput<TOutput>(response).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task GetWebApiForUserAsync<TInput>(
            string serviceName,
            TInput input,
            Action<DownstreamWebApiOptions>? downstreamWebApiOptionsOverride = null,
            ClaimsPrincipal? user = null)
        {
            await CallWebApiForUserAsync(
             serviceName,
             downstreamWebApiOptionsOverride,
             user,
             ConvertFromInput(input)).ConfigureAwait(false);
        }

        private static StringContent ConvertFromInput<TInput>(TInput input)
        {
            return new StringContent(JsonSerializer.Serialize(input), Encoding.UTF8, "application/json");
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

                throw new HttpRequestException($"{(int)response.StatusCode} {response.StatusCode} {error}");
            }

            string content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(content))
            {
                return default;
            }

            return JsonSerializer.Deserialize<TOutput>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

    }
}
