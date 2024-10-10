// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Abstractions;

#if NET8_0_OR_GREATER
using System.Text.Json.Serialization.Metadata;
#endif

namespace Microsoft.Identity.Web
{
    /// <inheritdoc/>
    internal partial class DownstreamApi : IDownstreamApi
    {
        /// <inheritdoc/>
        public async Task<TOutput?> GetForUserAsync<TOutput>(
            string? serviceName,
            Action<DownstreamApiOptionsReadOnlyHttpMethod>? downstreamApiOptionsOverride = null,
            ClaimsPrincipal? user = null,
            CancellationToken cancellationToken = default)
            where TOutput : class
        {
            DownstreamApiOptions effectiveOptions = MergeOptions(serviceName, downstreamApiOptionsOverride, HttpMethod.Get);
            try
            {
                HttpResponseMessage response = await CallApiInternalAsync(serviceName, effectiveOptions, false, null, user, cancellationToken).ConfigureAwait(false);
                return await DeserializeOutputAsync<TOutput>(response, effectiveOptions).ConfigureAwait(false);
            }
            catch(Exception ex) when (
                ex is InvalidOperationException
                || ex is HttpRequestException)
            {
                Logger.HttpRequestError(
                    _logger, 
                    serviceName!,
                    effectiveOptions.BaseUrl!, 
                    effectiveOptions.RelativePath!, ex);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<TOutput?> GetForUserAsync<TInput, TOutput>(
            string? serviceName,
            TInput input,
            Action<DownstreamApiOptionsReadOnlyHttpMethod>? downstreamApiOptionsOverride = null,
            ClaimsPrincipal? user = null,
            CancellationToken cancellationToken = default)
            where TOutput : class
        {
            DownstreamApiOptions effectiveOptions = MergeOptions(serviceName, downstreamApiOptionsOverride, HttpMethod.Get);
            HttpContent? effectiveInput = SerializeInput(input, effectiveOptions);
            try
            {
                HttpResponseMessage response = await CallApiInternalAsync(serviceName, effectiveOptions, false, effectiveInput, user, cancellationToken).ConfigureAwait(false);

                // Only dispose the HttpContent if was created here, not provided by the caller.
                if (input is not HttpContent)
                {
                    effectiveInput?.Dispose();
                }
                response.EnsureSuccessStatusCode();
                return await DeserializeOutputAsync<TOutput>(response, effectiveOptions).ConfigureAwait(false);
            }
            catch(Exception ex) when (
                ex is InvalidOperationException
                || ex is HttpRequestException)
            {
                Logger.HttpRequestError(
                    _logger, 
                    serviceName!,
                    effectiveOptions.BaseUrl!, 
                    effectiveOptions.RelativePath!, ex);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<TOutput?> GetForAppAsync<TOutput>(
            string? serviceName,
            Action<DownstreamApiOptionsReadOnlyHttpMethod>? downstreamApiOptionsOverride = null,
            CancellationToken cancellationToken = default)
            where TOutput : class
        {
            DownstreamApiOptions effectiveOptions = MergeOptions(serviceName, downstreamApiOptionsOverride, HttpMethod.Get);
            try
            {
                HttpResponseMessage response = await CallApiInternalAsync(serviceName, effectiveOptions, true, null, null, cancellationToken).ConfigureAwait(false);
                return await DeserializeOutputAsync<TOutput>(response, effectiveOptions).ConfigureAwait(false);
            }
            catch(Exception ex) when (
                ex is InvalidOperationException
                || ex is HttpRequestException)
            {
                Logger.HttpRequestError(
                    _logger, 
                    serviceName!,
                    effectiveOptions.BaseUrl!, 
                    effectiveOptions.RelativePath!, ex);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<TOutput?> GetForAppAsync<TInput, TOutput>(
            string? serviceName,
            TInput input,
            Action<DownstreamApiOptionsReadOnlyHttpMethod>? downstreamApiOptionsOverride = null,
            CancellationToken cancellationToken = default)
            where TOutput : class
        {
            DownstreamApiOptions effectiveOptions = MergeOptions(serviceName, downstreamApiOptionsOverride, HttpMethod.Get);
            HttpContent? effectiveInput = SerializeInput(input, effectiveOptions);
            try
            {
                HttpResponseMessage response = await CallApiInternalAsync(serviceName, effectiveOptions, true, effectiveInput, null, cancellationToken).ConfigureAwait(false);

                // Only dispose the HttpContent if was created here, not provided by the caller.
                if (input is not HttpContent)
                {
                    effectiveInput?.Dispose();
                }
                response.EnsureSuccessStatusCode();
                return await DeserializeOutputAsync<TOutput>(response, effectiveOptions).ConfigureAwait(false);
            }
            catch(Exception ex) when (
                ex is InvalidOperationException
                || ex is HttpRequestException)
            {
                Logger.HttpRequestError(
                    _logger, 
                    serviceName!,
                    effectiveOptions.BaseUrl!, 
                    effectiveOptions.RelativePath!, ex);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task PostForUserAsync<TInput>(
            string? serviceName,
            TInput input,
            Action<DownstreamApiOptionsReadOnlyHttpMethod>? downstreamApiOptionsOverride = null,
            ClaimsPrincipal? user = null,
            CancellationToken cancellationToken = default)
        {
            DownstreamApiOptions effectiveOptions = MergeOptions(serviceName, downstreamApiOptionsOverride, HttpMethod.Post);
            HttpContent? effectiveInput = SerializeInput(input, effectiveOptions);
            try
            {
                HttpResponseMessage response = await CallApiInternalAsync(serviceName, effectiveOptions, false, effectiveInput, user, cancellationToken).ConfigureAwait(false);

                // Only dispose the HttpContent if was created here, not provided by the caller.
                if (input is not HttpContent)
                {
                    effectiveInput?.Dispose();
                }
                response.EnsureSuccessStatusCode();
            }
            catch(Exception ex) when (
                ex is InvalidOperationException
                || ex is HttpRequestException)
            {
                Logger.HttpRequestError(
                    _logger, 
                    serviceName!,
                    effectiveOptions.BaseUrl!, 
                    effectiveOptions.RelativePath!, ex);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<TOutput?> PostForUserAsync<TInput, TOutput>(
            string? serviceName,
            TInput input,
            Action<DownstreamApiOptionsReadOnlyHttpMethod>? downstreamApiOptionsOverride = null,
            ClaimsPrincipal? user = null,
            CancellationToken cancellationToken = default)
            where TOutput : class
        {
            DownstreamApiOptions effectiveOptions = MergeOptions(serviceName, downstreamApiOptionsOverride, HttpMethod.Post);
            HttpContent? effectiveInput = SerializeInput(input, effectiveOptions);
            try
            {
                HttpResponseMessage response = await CallApiInternalAsync(serviceName, effectiveOptions, false, effectiveInput, user, cancellationToken).ConfigureAwait(false);

                // Only dispose the HttpContent if was created here, not provided by the caller.
                if (input is not HttpContent)
                {
                    effectiveInput?.Dispose();
                }
                response.EnsureSuccessStatusCode();
                return await DeserializeOutputAsync<TOutput>(response, effectiveOptions).ConfigureAwait(false);
            }
            catch(Exception ex) when (
                ex is InvalidOperationException
                || ex is HttpRequestException)
            {
                Logger.HttpRequestError(
                    _logger, 
                    serviceName!,
                    effectiveOptions.BaseUrl!, 
                    effectiveOptions.RelativePath!, ex);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task PostForAppAsync<TInput>(
            string? serviceName,
            TInput input,
            Action<DownstreamApiOptionsReadOnlyHttpMethod>? downstreamApiOptionsOverride = null,
            CancellationToken cancellationToken = default)
        {
            DownstreamApiOptions effectiveOptions = MergeOptions(serviceName, downstreamApiOptionsOverride, HttpMethod.Post);
            HttpContent? effectiveInput = SerializeInput(input, effectiveOptions);
            try
            {
                HttpResponseMessage response = await CallApiInternalAsync(serviceName, effectiveOptions, true, effectiveInput, null, cancellationToken).ConfigureAwait(false);

                // Only dispose the HttpContent if was created here, not provided by the caller.
                if (input is not HttpContent)
                {
                    effectiveInput?.Dispose();
                }
                response.EnsureSuccessStatusCode();
            }
            catch(Exception ex) when (
                ex is InvalidOperationException
                || ex is HttpRequestException)
            {
                Logger.HttpRequestError(
                    _logger, 
                    serviceName!,
                    effectiveOptions.BaseUrl!, 
                    effectiveOptions.RelativePath!, ex);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<TOutput?> PostForAppAsync<TInput, TOutput>(
            string? serviceName,
            TInput input,
            Action<DownstreamApiOptionsReadOnlyHttpMethod>? downstreamApiOptionsOverride = null,
            CancellationToken cancellationToken = default)
            where TOutput : class
        {
            DownstreamApiOptions effectiveOptions = MergeOptions(serviceName, downstreamApiOptionsOverride, HttpMethod.Post);
            HttpContent? effectiveInput = SerializeInput(input, effectiveOptions);
            try
            {
                HttpResponseMessage response = await CallApiInternalAsync(serviceName, effectiveOptions, true, effectiveInput, null, cancellationToken).ConfigureAwait(false);

                // Only dispose the HttpContent if was created here, not provided by the caller.
                if (input is not HttpContent)
                {
                    effectiveInput?.Dispose();
                }
                response.EnsureSuccessStatusCode();
                return await DeserializeOutputAsync<TOutput>(response, effectiveOptions).ConfigureAwait(false);
            }
            catch(Exception ex) when (
                ex is InvalidOperationException
                || ex is HttpRequestException)
            {
                Logger.HttpRequestError(
                    _logger, 
                    serviceName!,
                    effectiveOptions.BaseUrl!, 
                    effectiveOptions.RelativePath!, ex);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task PutForUserAsync<TInput>(
            string? serviceName,
            TInput input,
            Action<DownstreamApiOptionsReadOnlyHttpMethod>? downstreamApiOptionsOverride = null,
            ClaimsPrincipal? user = null,
            CancellationToken cancellationToken = default)
        {
            DownstreamApiOptions effectiveOptions = MergeOptions(serviceName, downstreamApiOptionsOverride, HttpMethod.Put);
            HttpContent? effectiveInput = SerializeInput(input, effectiveOptions);
            try
            {
                HttpResponseMessage response = await CallApiInternalAsync(serviceName, effectiveOptions, false, effectiveInput, user, cancellationToken).ConfigureAwait(false);

                // Only dispose the HttpContent if was created here, not provided by the caller.
                if (input is not HttpContent)
                {
                    effectiveInput?.Dispose();
                }
                response.EnsureSuccessStatusCode();
            }
            catch(Exception ex) when (
                ex is InvalidOperationException
                || ex is HttpRequestException)
            {
                Logger.HttpRequestError(
                    _logger, 
                    serviceName!,
                    effectiveOptions.BaseUrl!, 
                    effectiveOptions.RelativePath!, ex);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<TOutput?> PutForUserAsync<TInput, TOutput>(
            string? serviceName,
            TInput input,
            Action<DownstreamApiOptionsReadOnlyHttpMethod>? downstreamApiOptionsOverride = null,
            ClaimsPrincipal? user = null,
            CancellationToken cancellationToken = default)
            where TOutput : class
        {
            DownstreamApiOptions effectiveOptions = MergeOptions(serviceName, downstreamApiOptionsOverride, HttpMethod.Put);
            HttpContent? effectiveInput = SerializeInput(input, effectiveOptions);
            try
            {
                HttpResponseMessage response = await CallApiInternalAsync(serviceName, effectiveOptions, false, effectiveInput, user, cancellationToken).ConfigureAwait(false);

                // Only dispose the HttpContent if was created here, not provided by the caller.
                if (input is not HttpContent)
                {
                    effectiveInput?.Dispose();
                }
                response.EnsureSuccessStatusCode();
                return await DeserializeOutputAsync<TOutput>(response, effectiveOptions).ConfigureAwait(false);
            }
            catch(Exception ex) when (
                ex is InvalidOperationException
                || ex is HttpRequestException)
            {
                Logger.HttpRequestError(
                    _logger, 
                    serviceName!,
                    effectiveOptions.BaseUrl!, 
                    effectiveOptions.RelativePath!, ex);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task PutForAppAsync<TInput>(
            string? serviceName,
            TInput input,
            Action<DownstreamApiOptionsReadOnlyHttpMethod>? downstreamApiOptionsOverride = null,
            CancellationToken cancellationToken = default)
        {
            DownstreamApiOptions effectiveOptions = MergeOptions(serviceName, downstreamApiOptionsOverride, HttpMethod.Put);
            HttpContent? effectiveInput = SerializeInput(input, effectiveOptions);
            try
            {
                HttpResponseMessage response = await CallApiInternalAsync(serviceName, effectiveOptions, true, effectiveInput, null, cancellationToken).ConfigureAwait(false);

                // Only dispose the HttpContent if was created here, not provided by the caller.
                if (input is not HttpContent)
                {
                    effectiveInput?.Dispose();
                }
                response.EnsureSuccessStatusCode();
            }
            catch(Exception ex) when (
                ex is InvalidOperationException
                || ex is HttpRequestException)
            {
                Logger.HttpRequestError(
                    _logger, 
                    serviceName!,
                    effectiveOptions.BaseUrl!, 
                    effectiveOptions.RelativePath!, ex);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<TOutput?> PutForAppAsync<TInput, TOutput>(
            string? serviceName,
            TInput input,
            Action<DownstreamApiOptionsReadOnlyHttpMethod>? downstreamApiOptionsOverride = null,
            CancellationToken cancellationToken = default)
            where TOutput : class
        {
            DownstreamApiOptions effectiveOptions = MergeOptions(serviceName, downstreamApiOptionsOverride, HttpMethod.Put);
            HttpContent? effectiveInput = SerializeInput(input, effectiveOptions);
            try
            {
                HttpResponseMessage response = await CallApiInternalAsync(serviceName, effectiveOptions, true, effectiveInput, null, cancellationToken).ConfigureAwait(false);

                // Only dispose the HttpContent if was created here, not provided by the caller.
                if (input is not HttpContent)
                {
                    effectiveInput?.Dispose();
                }
                response.EnsureSuccessStatusCode();
                return await DeserializeOutputAsync<TOutput>(response, effectiveOptions).ConfigureAwait(false);
            }
            catch(Exception ex) when (
                ex is InvalidOperationException
                || ex is HttpRequestException)
            {
                Logger.HttpRequestError(
                    _logger, 
                    serviceName!,
                    effectiveOptions.BaseUrl!, 
                    effectiveOptions.RelativePath!, ex);
                throw;
            }
        }

#if !NETFRAMEWORK && !NETSTANDARD2_0

        /// <inheritdoc/>
        public async Task PatchForUserAsync<TInput>(
            string? serviceName,
            TInput input,
            Action<DownstreamApiOptionsReadOnlyHttpMethod>? downstreamApiOptionsOverride = null,
            ClaimsPrincipal? user = null,
            CancellationToken cancellationToken = default)
        {
            DownstreamApiOptions effectiveOptions = MergeOptions(serviceName, downstreamApiOptionsOverride, HttpMethod.Patch);
            HttpContent? effectiveInput = SerializeInput(input, effectiveOptions);
            try
            {
                HttpResponseMessage response = await CallApiInternalAsync(serviceName, effectiveOptions, false, effectiveInput, user, cancellationToken).ConfigureAwait(false);

                // Only dispose the HttpContent if was created here, not provided by the caller.
                if (input is not HttpContent)
                {
                    effectiveInput?.Dispose();
                }
                response.EnsureSuccessStatusCode();
            }
            catch(Exception ex) when (
                ex is InvalidOperationException
                || ex is HttpRequestException)
            {
                Logger.HttpRequestError(
                    _logger, 
                    serviceName!,
                    effectiveOptions.BaseUrl!, 
                    effectiveOptions.RelativePath!, ex);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<TOutput?> PatchForUserAsync<TInput, TOutput>(
            string? serviceName,
            TInput input,
            Action<DownstreamApiOptionsReadOnlyHttpMethod>? downstreamApiOptionsOverride = null,
            ClaimsPrincipal? user = null,
            CancellationToken cancellationToken = default)
            where TOutput : class
        {
            DownstreamApiOptions effectiveOptions = MergeOptions(serviceName, downstreamApiOptionsOverride, HttpMethod.Patch);
            HttpContent? effectiveInput = SerializeInput(input, effectiveOptions);
            try
            {
                HttpResponseMessage response = await CallApiInternalAsync(serviceName, effectiveOptions, false, effectiveInput, user, cancellationToken).ConfigureAwait(false);

                // Only dispose the HttpContent if was created here, not provided by the caller.
                if (input is not HttpContent)
                {
                    effectiveInput?.Dispose();
                }
                response.EnsureSuccessStatusCode();
                return await DeserializeOutputAsync<TOutput>(response, effectiveOptions).ConfigureAwait(false);
            }
            catch(Exception ex) when (
                ex is InvalidOperationException
                || ex is HttpRequestException)
            {
                Logger.HttpRequestError(
                    _logger, 
                    serviceName!,
                    effectiveOptions.BaseUrl!, 
                    effectiveOptions.RelativePath!, ex);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task PatchForAppAsync<TInput>(
            string? serviceName,
            TInput input,
            Action<DownstreamApiOptionsReadOnlyHttpMethod>? downstreamApiOptionsOverride = null,
            CancellationToken cancellationToken = default)
        {
            DownstreamApiOptions effectiveOptions = MergeOptions(serviceName, downstreamApiOptionsOverride, HttpMethod.Patch);
            HttpContent? effectiveInput = SerializeInput(input, effectiveOptions);
            try
            {
                HttpResponseMessage response = await CallApiInternalAsync(serviceName, effectiveOptions, true, effectiveInput, null, cancellationToken).ConfigureAwait(false);

                // Only dispose the HttpContent if was created here, not provided by the caller.
                if (input is not HttpContent)
                {
                    effectiveInput?.Dispose();
                }
                response.EnsureSuccessStatusCode();
            }
            catch(Exception ex) when (
                ex is InvalidOperationException
                || ex is HttpRequestException)
            {
                Logger.HttpRequestError(
                    _logger, 
                    serviceName!,
                    effectiveOptions.BaseUrl!, 
                    effectiveOptions.RelativePath!, ex);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<TOutput?> PatchForAppAsync<TInput, TOutput>(
            string? serviceName,
            TInput input,
            Action<DownstreamApiOptionsReadOnlyHttpMethod>? downstreamApiOptionsOverride = null,
            CancellationToken cancellationToken = default)
            where TOutput : class
        {
            DownstreamApiOptions effectiveOptions = MergeOptions(serviceName, downstreamApiOptionsOverride, HttpMethod.Patch);
            HttpContent? effectiveInput = SerializeInput(input, effectiveOptions);
            try
            {
                HttpResponseMessage response = await CallApiInternalAsync(serviceName, effectiveOptions, true, effectiveInput, null, cancellationToken).ConfigureAwait(false);

                // Only dispose the HttpContent if was created here, not provided by the caller.
                if (input is not HttpContent)
                {
                    effectiveInput?.Dispose();
                }
                response.EnsureSuccessStatusCode();
                return await DeserializeOutputAsync<TOutput>(response, effectiveOptions).ConfigureAwait(false);
            }
            catch(Exception ex) when (
                ex is InvalidOperationException
                || ex is HttpRequestException)
            {
                Logger.HttpRequestError(
                    _logger, 
                    serviceName!,
                    effectiveOptions.BaseUrl!, 
                    effectiveOptions.RelativePath!, ex);
                throw;
            }
        }

#endif // !NETFRAMEWORK && !NETSTANDARD2_0

        /// <inheritdoc/>
        public async Task DeleteForUserAsync<TInput>(
            string? serviceName,
            TInput input,
            Action<DownstreamApiOptionsReadOnlyHttpMethod>? downstreamApiOptionsOverride = null,
            ClaimsPrincipal? user = null,
            CancellationToken cancellationToken = default)
        {
            DownstreamApiOptions effectiveOptions = MergeOptions(serviceName, downstreamApiOptionsOverride, HttpMethod.Delete);
            HttpContent? effectiveInput = SerializeInput(input, effectiveOptions);
            try
            {
                HttpResponseMessage response = await CallApiInternalAsync(serviceName, effectiveOptions, false, effectiveInput, user, cancellationToken).ConfigureAwait(false);

                // Only dispose the HttpContent if was created here, not provided by the caller.
                if (input is not HttpContent)
                {
                    effectiveInput?.Dispose();
                }
                response.EnsureSuccessStatusCode();
            }
            catch(Exception ex) when (
                ex is InvalidOperationException
                || ex is HttpRequestException)
            {
                Logger.HttpRequestError(
                    _logger, 
                    serviceName!,
                    effectiveOptions.BaseUrl!, 
                    effectiveOptions.RelativePath!, ex);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<TOutput?> DeleteForUserAsync<TInput, TOutput>(
            string? serviceName,
            TInput input,
            Action<DownstreamApiOptionsReadOnlyHttpMethod>? downstreamApiOptionsOverride = null,
            ClaimsPrincipal? user = null,
            CancellationToken cancellationToken = default)
            where TOutput : class
        {
            DownstreamApiOptions effectiveOptions = MergeOptions(serviceName, downstreamApiOptionsOverride, HttpMethod.Delete);
            HttpContent? effectiveInput = SerializeInput(input, effectiveOptions);
            try
            {
                HttpResponseMessage response = await CallApiInternalAsync(serviceName, effectiveOptions, false, effectiveInput, user, cancellationToken).ConfigureAwait(false);

                // Only dispose the HttpContent if was created here, not provided by the caller.
                if (input is not HttpContent)
                {
                    effectiveInput?.Dispose();
                }
                response.EnsureSuccessStatusCode();
                return await DeserializeOutputAsync<TOutput>(response, effectiveOptions).ConfigureAwait(false);
            }
            catch(Exception ex) when (
                ex is InvalidOperationException
                || ex is HttpRequestException)
            {
                Logger.HttpRequestError(
                    _logger, 
                    serviceName!,
                    effectiveOptions.BaseUrl!, 
                    effectiveOptions.RelativePath!, ex);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task DeleteForAppAsync<TInput>(
            string? serviceName,
            TInput input,
            Action<DownstreamApiOptionsReadOnlyHttpMethod>? downstreamApiOptionsOverride = null,
            CancellationToken cancellationToken = default)
        {
            DownstreamApiOptions effectiveOptions = MergeOptions(serviceName, downstreamApiOptionsOverride, HttpMethod.Delete);
            HttpContent? effectiveInput = SerializeInput(input, effectiveOptions);
            try
            {
                HttpResponseMessage response = await CallApiInternalAsync(serviceName, effectiveOptions, true, effectiveInput, null, cancellationToken).ConfigureAwait(false);

                // Only dispose the HttpContent if was created here, not provided by the caller.
                if (input is not HttpContent)
                {
                    effectiveInput?.Dispose();
                }
                response.EnsureSuccessStatusCode();
            }
            catch(Exception ex) when (
                ex is InvalidOperationException
                || ex is HttpRequestException)
            {
                Logger.HttpRequestError(
                    _logger, 
                    serviceName!,
                    effectiveOptions.BaseUrl!, 
                    effectiveOptions.RelativePath!, ex);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<TOutput?> DeleteForAppAsync<TInput, TOutput>(
            string? serviceName,
            TInput input,
            Action<DownstreamApiOptionsReadOnlyHttpMethod>? downstreamApiOptionsOverride = null,
            CancellationToken cancellationToken = default)
            where TOutput : class
        {
            DownstreamApiOptions effectiveOptions = MergeOptions(serviceName, downstreamApiOptionsOverride, HttpMethod.Delete);
            HttpContent? effectiveInput = SerializeInput(input, effectiveOptions);
            try
            {
                HttpResponseMessage response = await CallApiInternalAsync(serviceName, effectiveOptions, true, effectiveInput, null, cancellationToken).ConfigureAwait(false);

                // Only dispose the HttpContent if was created here, not provided by the caller.
                if (input is not HttpContent)
                {
                    effectiveInput?.Dispose();
                }
                response.EnsureSuccessStatusCode();
                return await DeserializeOutputAsync<TOutput>(response, effectiveOptions).ConfigureAwait(false);
            }
            catch(Exception ex) when (
                ex is InvalidOperationException
                || ex is HttpRequestException)
            {
                Logger.HttpRequestError(
                    _logger, 
                    serviceName!,
                    effectiveOptions.BaseUrl!, 
                    effectiveOptions.RelativePath!, ex);
                throw;
            }
        }

#if NET8_0_OR_GREATER

        /// <inheritdoc/>
        public async Task<TOutput?> GetForUserAsync<TOutput>(
            string? serviceName,
            JsonTypeInfo<TOutput> outputJsonTypeInfo,
            Action<DownstreamApiOptionsReadOnlyHttpMethod>? downstreamApiOptionsOverride = null,
            ClaimsPrincipal? user = null,
            CancellationToken cancellationToken = default)
            where TOutput : class
        {
            DownstreamApiOptions effectiveOptions = MergeOptions(serviceName, downstreamApiOptionsOverride, HttpMethod.Get);
            try
            {
                HttpResponseMessage response = await CallApiInternalAsync(serviceName, effectiveOptions, false, null, user, cancellationToken).ConfigureAwait(false);
                return await DeserializeOutputAsync<TOutput>(response, effectiveOptions, outputJsonTypeInfo).ConfigureAwait(false);
            }
            catch(Exception ex) when (
                ex is InvalidOperationException
                || ex is HttpRequestException)
            {
                Logger.HttpRequestError(
                    _logger, 
                    serviceName!,
                    effectiveOptions.BaseUrl!, 
                    effectiveOptions.RelativePath!, ex);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<TOutput?> GetForUserAsync<TInput, TOutput>(
            string? serviceName,
            TInput input,
            JsonTypeInfo<TInput> inputJsonTypeInfo,
            JsonTypeInfo<TOutput> outputJsonTypeInfo,
            Action<DownstreamApiOptionsReadOnlyHttpMethod>? downstreamApiOptionsOverride = null,
            ClaimsPrincipal? user = null,
            CancellationToken cancellationToken = default)
            where TOutput : class
        {
            DownstreamApiOptions effectiveOptions = MergeOptions(serviceName, downstreamApiOptionsOverride, HttpMethod.Get);
            HttpContent? effectiveInput = SerializeInput(input, effectiveOptions, inputJsonTypeInfo);
            try
            {
                HttpResponseMessage response = await CallApiInternalAsync(serviceName, effectiveOptions, false, effectiveInput, user, cancellationToken).ConfigureAwait(false);

                // Only dispose the HttpContent if was created here, not provided by the caller.
                if (input is not HttpContent)
                {
                    effectiveInput?.Dispose();
                }
                response.EnsureSuccessStatusCode();
                return await DeserializeOutputAsync<TOutput>(response, effectiveOptions, outputJsonTypeInfo).ConfigureAwait(false);
            }
            catch(Exception ex) when (
                ex is InvalidOperationException
                || ex is HttpRequestException)
            {
                Logger.HttpRequestError(
                    _logger, 
                    serviceName!,
                    effectiveOptions.BaseUrl!, 
                    effectiveOptions.RelativePath!, ex);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<TOutput?> GetForAppAsync<TOutput>(
            string? serviceName,
            JsonTypeInfo<TOutput> outputJsonTypeInfo,
            Action<DownstreamApiOptionsReadOnlyHttpMethod>? downstreamApiOptionsOverride = null,
            CancellationToken cancellationToken = default)
            where TOutput : class
        {
            DownstreamApiOptions effectiveOptions = MergeOptions(serviceName, downstreamApiOptionsOverride, HttpMethod.Get);
            try
            {
                HttpResponseMessage response = await CallApiInternalAsync(serviceName, effectiveOptions, true, null, null, cancellationToken).ConfigureAwait(false);
                return await DeserializeOutputAsync<TOutput>(response, effectiveOptions, outputJsonTypeInfo).ConfigureAwait(false);
            }
            catch(Exception ex) when (
                ex is InvalidOperationException
                || ex is HttpRequestException)
            {
                Logger.HttpRequestError(
                    _logger, 
                    serviceName!,
                    effectiveOptions.BaseUrl!, 
                    effectiveOptions.RelativePath!, ex);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<TOutput?> GetForAppAsync<TInput, TOutput>(
            string? serviceName,
            TInput input,
            JsonTypeInfo<TInput> inputJsonTypeInfo,
            JsonTypeInfo<TOutput> outputJsonTypeInfo,
            Action<DownstreamApiOptionsReadOnlyHttpMethod>? downstreamApiOptionsOverride = null,
            CancellationToken cancellationToken = default)
            where TOutput : class
        {
            DownstreamApiOptions effectiveOptions = MergeOptions(serviceName, downstreamApiOptionsOverride, HttpMethod.Get);
            HttpContent? effectiveInput = SerializeInput(input, effectiveOptions, inputJsonTypeInfo);
            try
            {
                HttpResponseMessage response = await CallApiInternalAsync(serviceName, effectiveOptions, true, effectiveInput, null, cancellationToken).ConfigureAwait(false);

                // Only dispose the HttpContent if was created here, not provided by the caller.
                if (input is not HttpContent)
                {
                    effectiveInput?.Dispose();
                }
                response.EnsureSuccessStatusCode();
                return await DeserializeOutputAsync<TOutput>(response, effectiveOptions, outputJsonTypeInfo).ConfigureAwait(false);
            }
            catch(Exception ex) when (
                ex is InvalidOperationException
                || ex is HttpRequestException)
            {
                Logger.HttpRequestError(
                    _logger, 
                    serviceName!,
                    effectiveOptions.BaseUrl!, 
                    effectiveOptions.RelativePath!, ex);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task PostForUserAsync<TInput>(
            string? serviceName,
            TInput input,
            JsonTypeInfo<TInput> inputJsonTypeInfo,
            Action<DownstreamApiOptionsReadOnlyHttpMethod>? downstreamApiOptionsOverride = null,
            ClaimsPrincipal? user = null,
            CancellationToken cancellationToken = default)
        {
            DownstreamApiOptions effectiveOptions = MergeOptions(serviceName, downstreamApiOptionsOverride, HttpMethod.Post);
            HttpContent? effectiveInput = SerializeInput(input, effectiveOptions, inputJsonTypeInfo);
            try
            {
                HttpResponseMessage response = await CallApiInternalAsync(serviceName, effectiveOptions, false, effectiveInput, user, cancellationToken).ConfigureAwait(false);

                // Only dispose the HttpContent if was created here, not provided by the caller.
                if (input is not HttpContent)
                {
                    effectiveInput?.Dispose();
                }
                response.EnsureSuccessStatusCode();
            }
            catch(Exception ex) when (
                ex is InvalidOperationException
                || ex is HttpRequestException)
            {
                Logger.HttpRequestError(
                    _logger, 
                    serviceName!,
                    effectiveOptions.BaseUrl!, 
                    effectiveOptions.RelativePath!, ex);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<TOutput?> PostForUserAsync<TInput, TOutput>(
            string? serviceName,
            TInput input,
            JsonTypeInfo<TInput> inputJsonTypeInfo,
            JsonTypeInfo<TOutput> outputJsonTypeInfo,
            Action<DownstreamApiOptionsReadOnlyHttpMethod>? downstreamApiOptionsOverride = null,
            ClaimsPrincipal? user = null,
            CancellationToken cancellationToken = default)
            where TOutput : class
        {
            DownstreamApiOptions effectiveOptions = MergeOptions(serviceName, downstreamApiOptionsOverride, HttpMethod.Post);
            HttpContent? effectiveInput = SerializeInput(input, effectiveOptions, inputJsonTypeInfo);
            try
            {
                HttpResponseMessage response = await CallApiInternalAsync(serviceName, effectiveOptions, false, effectiveInput, user, cancellationToken).ConfigureAwait(false);

                // Only dispose the HttpContent if was created here, not provided by the caller.
                if (input is not HttpContent)
                {
                    effectiveInput?.Dispose();
                }
                response.EnsureSuccessStatusCode();
                return await DeserializeOutputAsync<TOutput>(response, effectiveOptions, outputJsonTypeInfo).ConfigureAwait(false);
            }
            catch(Exception ex) when (
                ex is InvalidOperationException
                || ex is HttpRequestException)
            {
                Logger.HttpRequestError(
                    _logger, 
                    serviceName!,
                    effectiveOptions.BaseUrl!, 
                    effectiveOptions.RelativePath!, ex);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task PostForAppAsync<TInput>(
            string? serviceName,
            TInput input,
            JsonTypeInfo<TInput> inputJsonTypeInfo,
            Action<DownstreamApiOptionsReadOnlyHttpMethod>? downstreamApiOptionsOverride = null,
            CancellationToken cancellationToken = default)
        {
            DownstreamApiOptions effectiveOptions = MergeOptions(serviceName, downstreamApiOptionsOverride, HttpMethod.Post);
            HttpContent? effectiveInput = SerializeInput(input, effectiveOptions, inputJsonTypeInfo);
            try
            {
                HttpResponseMessage response = await CallApiInternalAsync(serviceName, effectiveOptions, true, effectiveInput, null, cancellationToken).ConfigureAwait(false);

                // Only dispose the HttpContent if was created here, not provided by the caller.
                if (input is not HttpContent)
                {
                    effectiveInput?.Dispose();
                }
                response.EnsureSuccessStatusCode();
            }
            catch(Exception ex) when (
                ex is InvalidOperationException
                || ex is HttpRequestException)
            {
                Logger.HttpRequestError(
                    _logger, 
                    serviceName!,
                    effectiveOptions.BaseUrl!, 
                    effectiveOptions.RelativePath!, ex);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<TOutput?> PostForAppAsync<TInput, TOutput>(
            string? serviceName,
            TInput input,
            JsonTypeInfo<TInput> inputJsonTypeInfo,
            JsonTypeInfo<TOutput> outputJsonTypeInfo,
            Action<DownstreamApiOptionsReadOnlyHttpMethod>? downstreamApiOptionsOverride = null,
            CancellationToken cancellationToken = default)
            where TOutput : class
        {
            DownstreamApiOptions effectiveOptions = MergeOptions(serviceName, downstreamApiOptionsOverride, HttpMethod.Post);
            HttpContent? effectiveInput = SerializeInput(input, effectiveOptions, inputJsonTypeInfo);
            try
            {
                HttpResponseMessage response = await CallApiInternalAsync(serviceName, effectiveOptions, true, effectiveInput, null, cancellationToken).ConfigureAwait(false);

                // Only dispose the HttpContent if was created here, not provided by the caller.
                if (input is not HttpContent)
                {
                    effectiveInput?.Dispose();
                }
                response.EnsureSuccessStatusCode();
                return await DeserializeOutputAsync<TOutput>(response, effectiveOptions, outputJsonTypeInfo).ConfigureAwait(false);
            }
            catch(Exception ex) when (
                ex is InvalidOperationException
                || ex is HttpRequestException)
            {
                Logger.HttpRequestError(
                    _logger, 
                    serviceName!,
                    effectiveOptions.BaseUrl!, 
                    effectiveOptions.RelativePath!, ex);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task PutForUserAsync<TInput>(
            string? serviceName,
            TInput input,
            JsonTypeInfo<TInput> inputJsonTypeInfo,
            Action<DownstreamApiOptionsReadOnlyHttpMethod>? downstreamApiOptionsOverride = null,
            ClaimsPrincipal? user = null,
            CancellationToken cancellationToken = default)
        {
            DownstreamApiOptions effectiveOptions = MergeOptions(serviceName, downstreamApiOptionsOverride, HttpMethod.Put);
            HttpContent? effectiveInput = SerializeInput(input, effectiveOptions, inputJsonTypeInfo);
            try
            {
                HttpResponseMessage response = await CallApiInternalAsync(serviceName, effectiveOptions, false, effectiveInput, user, cancellationToken).ConfigureAwait(false);

                // Only dispose the HttpContent if was created here, not provided by the caller.
                if (input is not HttpContent)
                {
                    effectiveInput?.Dispose();
                }
                response.EnsureSuccessStatusCode();
            }
            catch(Exception ex) when (
                ex is InvalidOperationException
                || ex is HttpRequestException)
            {
                Logger.HttpRequestError(
                    _logger, 
                    serviceName!,
                    effectiveOptions.BaseUrl!, 
                    effectiveOptions.RelativePath!, ex);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<TOutput?> PutForUserAsync<TInput, TOutput>(
            string? serviceName,
            TInput input,
            JsonTypeInfo<TInput> inputJsonTypeInfo,
            JsonTypeInfo<TOutput> outputJsonTypeInfo,
            Action<DownstreamApiOptionsReadOnlyHttpMethod>? downstreamApiOptionsOverride = null,
            ClaimsPrincipal? user = null,
            CancellationToken cancellationToken = default)
            where TOutput : class
        {
            DownstreamApiOptions effectiveOptions = MergeOptions(serviceName, downstreamApiOptionsOverride, HttpMethod.Put);
            HttpContent? effectiveInput = SerializeInput(input, effectiveOptions, inputJsonTypeInfo);
            try
            {
                HttpResponseMessage response = await CallApiInternalAsync(serviceName, effectiveOptions, false, effectiveInput, user, cancellationToken).ConfigureAwait(false);

                // Only dispose the HttpContent if was created here, not provided by the caller.
                if (input is not HttpContent)
                {
                    effectiveInput?.Dispose();
                }
                response.EnsureSuccessStatusCode();
                return await DeserializeOutputAsync<TOutput>(response, effectiveOptions, outputJsonTypeInfo).ConfigureAwait(false);
            }
            catch(Exception ex) when (
                ex is InvalidOperationException
                || ex is HttpRequestException)
            {
                Logger.HttpRequestError(
                    _logger, 
                    serviceName!,
                    effectiveOptions.BaseUrl!, 
                    effectiveOptions.RelativePath!, ex);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task PutForAppAsync<TInput>(
            string? serviceName,
            TInput input,
            JsonTypeInfo<TInput> inputJsonTypeInfo,
            Action<DownstreamApiOptionsReadOnlyHttpMethod>? downstreamApiOptionsOverride = null,
            CancellationToken cancellationToken = default)
        {
            DownstreamApiOptions effectiveOptions = MergeOptions(serviceName, downstreamApiOptionsOverride, HttpMethod.Put);
            HttpContent? effectiveInput = SerializeInput(input, effectiveOptions, inputJsonTypeInfo);
            try
            {
                HttpResponseMessage response = await CallApiInternalAsync(serviceName, effectiveOptions, true, effectiveInput, null, cancellationToken).ConfigureAwait(false);

                // Only dispose the HttpContent if was created here, not provided by the caller.
                if (input is not HttpContent)
                {
                    effectiveInput?.Dispose();
                }
                response.EnsureSuccessStatusCode();
            }
            catch(Exception ex) when (
                ex is InvalidOperationException
                || ex is HttpRequestException)
            {
                Logger.HttpRequestError(
                    _logger, 
                    serviceName!,
                    effectiveOptions.BaseUrl!, 
                    effectiveOptions.RelativePath!, ex);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<TOutput?> PutForAppAsync<TInput, TOutput>(
            string? serviceName,
            TInput input,
            JsonTypeInfo<TInput> inputJsonTypeInfo,
            JsonTypeInfo<TOutput> outputJsonTypeInfo,
            Action<DownstreamApiOptionsReadOnlyHttpMethod>? downstreamApiOptionsOverride = null,
            CancellationToken cancellationToken = default)
            where TOutput : class
        {
            DownstreamApiOptions effectiveOptions = MergeOptions(serviceName, downstreamApiOptionsOverride, HttpMethod.Put);
            HttpContent? effectiveInput = SerializeInput(input, effectiveOptions, inputJsonTypeInfo);
            try
            {
                HttpResponseMessage response = await CallApiInternalAsync(serviceName, effectiveOptions, true, effectiveInput, null, cancellationToken).ConfigureAwait(false);

                // Only dispose the HttpContent if was created here, not provided by the caller.
                if (input is not HttpContent)
                {
                    effectiveInput?.Dispose();
                }
                response.EnsureSuccessStatusCode();
                return await DeserializeOutputAsync<TOutput>(response, effectiveOptions, outputJsonTypeInfo).ConfigureAwait(false);
            }
            catch(Exception ex) when (
                ex is InvalidOperationException
                || ex is HttpRequestException)
            {
                Logger.HttpRequestError(
                    _logger, 
                    serviceName!,
                    effectiveOptions.BaseUrl!, 
                    effectiveOptions.RelativePath!, ex);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task PatchForUserAsync<TInput>(
            string? serviceName,
            TInput input,
            JsonTypeInfo<TInput> inputJsonTypeInfo,
            Action<DownstreamApiOptionsReadOnlyHttpMethod>? downstreamApiOptionsOverride = null,
            ClaimsPrincipal? user = null,
            CancellationToken cancellationToken = default)
        {
            DownstreamApiOptions effectiveOptions = MergeOptions(serviceName, downstreamApiOptionsOverride, HttpMethod.Patch);
            HttpContent? effectiveInput = SerializeInput(input, effectiveOptions, inputJsonTypeInfo);
            try
            {
                HttpResponseMessage response = await CallApiInternalAsync(serviceName, effectiveOptions, false, effectiveInput, user, cancellationToken).ConfigureAwait(false);

                // Only dispose the HttpContent if was created here, not provided by the caller.
                if (input is not HttpContent)
                {
                    effectiveInput?.Dispose();
                }
                response.EnsureSuccessStatusCode();
            }
            catch(Exception ex) when (
                ex is InvalidOperationException
                || ex is HttpRequestException)
            {
                Logger.HttpRequestError(
                    _logger, 
                    serviceName!,
                    effectiveOptions.BaseUrl!, 
                    effectiveOptions.RelativePath!, ex);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<TOutput?> PatchForUserAsync<TInput, TOutput>(
            string? serviceName,
            TInput input,
            JsonTypeInfo<TInput> inputJsonTypeInfo,
            JsonTypeInfo<TOutput> outputJsonTypeInfo,
            Action<DownstreamApiOptionsReadOnlyHttpMethod>? downstreamApiOptionsOverride = null,
            ClaimsPrincipal? user = null,
            CancellationToken cancellationToken = default)
            where TOutput : class
        {
            DownstreamApiOptions effectiveOptions = MergeOptions(serviceName, downstreamApiOptionsOverride, HttpMethod.Patch);
            HttpContent? effectiveInput = SerializeInput(input, effectiveOptions, inputJsonTypeInfo);
            try
            {
                HttpResponseMessage response = await CallApiInternalAsync(serviceName, effectiveOptions, false, effectiveInput, user, cancellationToken).ConfigureAwait(false);

                // Only dispose the HttpContent if was created here, not provided by the caller.
                if (input is not HttpContent)
                {
                    effectiveInput?.Dispose();
                }
                response.EnsureSuccessStatusCode();
                return await DeserializeOutputAsync<TOutput>(response, effectiveOptions, outputJsonTypeInfo).ConfigureAwait(false);
            }
            catch(Exception ex) when (
                ex is InvalidOperationException
                || ex is HttpRequestException)
            {
                Logger.HttpRequestError(
                    _logger, 
                    serviceName!,
                    effectiveOptions.BaseUrl!, 
                    effectiveOptions.RelativePath!, ex);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task PatchForAppAsync<TInput>(
            string? serviceName,
            TInput input,
            JsonTypeInfo<TInput> inputJsonTypeInfo,
            Action<DownstreamApiOptionsReadOnlyHttpMethod>? downstreamApiOptionsOverride = null,
            CancellationToken cancellationToken = default)
        {
            DownstreamApiOptions effectiveOptions = MergeOptions(serviceName, downstreamApiOptionsOverride, HttpMethod.Patch);
            HttpContent? effectiveInput = SerializeInput(input, effectiveOptions, inputJsonTypeInfo);
            try
            {
                HttpResponseMessage response = await CallApiInternalAsync(serviceName, effectiveOptions, true, effectiveInput, null, cancellationToken).ConfigureAwait(false);

                // Only dispose the HttpContent if was created here, not provided by the caller.
                if (input is not HttpContent)
                {
                    effectiveInput?.Dispose();
                }
                response.EnsureSuccessStatusCode();
            }
            catch(Exception ex) when (
                ex is InvalidOperationException
                || ex is HttpRequestException)
            {
                Logger.HttpRequestError(
                    _logger, 
                    serviceName!,
                    effectiveOptions.BaseUrl!, 
                    effectiveOptions.RelativePath!, ex);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<TOutput?> PatchForAppAsync<TInput, TOutput>(
            string? serviceName,
            TInput input,
            JsonTypeInfo<TInput> inputJsonTypeInfo,
            JsonTypeInfo<TOutput> outputJsonTypeInfo,
            Action<DownstreamApiOptionsReadOnlyHttpMethod>? downstreamApiOptionsOverride = null,
            CancellationToken cancellationToken = default)
            where TOutput : class
        {
            DownstreamApiOptions effectiveOptions = MergeOptions(serviceName, downstreamApiOptionsOverride, HttpMethod.Patch);
            HttpContent? effectiveInput = SerializeInput(input, effectiveOptions, inputJsonTypeInfo);
            try
            {
                HttpResponseMessage response = await CallApiInternalAsync(serviceName, effectiveOptions, true, effectiveInput, null, cancellationToken).ConfigureAwait(false);

                // Only dispose the HttpContent if was created here, not provided by the caller.
                if (input is not HttpContent)
                {
                    effectiveInput?.Dispose();
                }
                response.EnsureSuccessStatusCode();
                return await DeserializeOutputAsync<TOutput>(response, effectiveOptions, outputJsonTypeInfo).ConfigureAwait(false);
            }
            catch(Exception ex) when (
                ex is InvalidOperationException
                || ex is HttpRequestException)
            {
                Logger.HttpRequestError(
                    _logger, 
                    serviceName!,
                    effectiveOptions.BaseUrl!, 
                    effectiveOptions.RelativePath!, ex);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task DeleteForUserAsync<TInput>(
            string? serviceName,
            TInput input,
            JsonTypeInfo<TInput> inputJsonTypeInfo,
            Action<DownstreamApiOptionsReadOnlyHttpMethod>? downstreamApiOptionsOverride = null,
            ClaimsPrincipal? user = null,
            CancellationToken cancellationToken = default)
        {
            DownstreamApiOptions effectiveOptions = MergeOptions(serviceName, downstreamApiOptionsOverride, HttpMethod.Delete);
            HttpContent? effectiveInput = SerializeInput(input, effectiveOptions, inputJsonTypeInfo);
            try
            {
                HttpResponseMessage response = await CallApiInternalAsync(serviceName, effectiveOptions, false, effectiveInput, user, cancellationToken).ConfigureAwait(false);

                // Only dispose the HttpContent if was created here, not provided by the caller.
                if (input is not HttpContent)
                {
                    effectiveInput?.Dispose();
                }
                response.EnsureSuccessStatusCode();
            }
            catch(Exception ex) when (
                ex is InvalidOperationException
                || ex is HttpRequestException)
            {
                Logger.HttpRequestError(
                    _logger, 
                    serviceName!,
                    effectiveOptions.BaseUrl!, 
                    effectiveOptions.RelativePath!, ex);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<TOutput?> DeleteForUserAsync<TInput, TOutput>(
            string? serviceName,
            TInput input,
            JsonTypeInfo<TInput> inputJsonTypeInfo,
            JsonTypeInfo<TOutput> outputJsonTypeInfo,
            Action<DownstreamApiOptionsReadOnlyHttpMethod>? downstreamApiOptionsOverride = null,
            ClaimsPrincipal? user = null,
            CancellationToken cancellationToken = default)
            where TOutput : class
        {
            DownstreamApiOptions effectiveOptions = MergeOptions(serviceName, downstreamApiOptionsOverride, HttpMethod.Delete);
            HttpContent? effectiveInput = SerializeInput(input, effectiveOptions, inputJsonTypeInfo);
            try
            {
                HttpResponseMessage response = await CallApiInternalAsync(serviceName, effectiveOptions, false, effectiveInput, user, cancellationToken).ConfigureAwait(false);

                // Only dispose the HttpContent if was created here, not provided by the caller.
                if (input is not HttpContent)
                {
                    effectiveInput?.Dispose();
                }
                response.EnsureSuccessStatusCode();
                return await DeserializeOutputAsync<TOutput>(response, effectiveOptions, outputJsonTypeInfo).ConfigureAwait(false);
            }
            catch(Exception ex) when (
                ex is InvalidOperationException
                || ex is HttpRequestException)
            {
                Logger.HttpRequestError(
                    _logger, 
                    serviceName!,
                    effectiveOptions.BaseUrl!, 
                    effectiveOptions.RelativePath!, ex);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task DeleteForAppAsync<TInput>(
            string? serviceName,
            TInput input,
            JsonTypeInfo<TInput> inputJsonTypeInfo,
            Action<DownstreamApiOptionsReadOnlyHttpMethod>? downstreamApiOptionsOverride = null,
            CancellationToken cancellationToken = default)
        {
            DownstreamApiOptions effectiveOptions = MergeOptions(serviceName, downstreamApiOptionsOverride, HttpMethod.Delete);
            HttpContent? effectiveInput = SerializeInput(input, effectiveOptions, inputJsonTypeInfo);
            try
            {
                HttpResponseMessage response = await CallApiInternalAsync(serviceName, effectiveOptions, true, effectiveInput, null, cancellationToken).ConfigureAwait(false);

                // Only dispose the HttpContent if was created here, not provided by the caller.
                if (input is not HttpContent)
                {
                    effectiveInput?.Dispose();
                }
                response.EnsureSuccessStatusCode();
            }
            catch(Exception ex) when (
                ex is InvalidOperationException
                || ex is HttpRequestException)
            {
                Logger.HttpRequestError(
                    _logger, 
                    serviceName!,
                    effectiveOptions.BaseUrl!, 
                    effectiveOptions.RelativePath!, ex);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<TOutput?> DeleteForAppAsync<TInput, TOutput>(
            string? serviceName,
            TInput input,
            JsonTypeInfo<TInput> inputJsonTypeInfo,
            JsonTypeInfo<TOutput> outputJsonTypeInfo,
            Action<DownstreamApiOptionsReadOnlyHttpMethod>? downstreamApiOptionsOverride = null,
            CancellationToken cancellationToken = default)
            where TOutput : class
        {
            DownstreamApiOptions effectiveOptions = MergeOptions(serviceName, downstreamApiOptionsOverride, HttpMethod.Delete);
            HttpContent? effectiveInput = SerializeInput(input, effectiveOptions, inputJsonTypeInfo);
            try
            {
                HttpResponseMessage response = await CallApiInternalAsync(serviceName, effectiveOptions, true, effectiveInput, null, cancellationToken).ConfigureAwait(false);

                // Only dispose the HttpContent if was created here, not provided by the caller.
                if (input is not HttpContent)
                {
                    effectiveInput?.Dispose();
                }
                response.EnsureSuccessStatusCode();
                return await DeserializeOutputAsync<TOutput>(response, effectiveOptions, outputJsonTypeInfo).ConfigureAwait(false);
            }
            catch(Exception ex) when (
                ex is InvalidOperationException
                || ex is HttpRequestException)
            {
                Logger.HttpRequestError(
                    _logger, 
                    serviceName!,
                    effectiveOptions.BaseUrl!, 
                    effectiveOptions.RelativePath!, ex);
                throw;
            }
        }
#endif // NET8_0_OR_GREATER
   }
}
