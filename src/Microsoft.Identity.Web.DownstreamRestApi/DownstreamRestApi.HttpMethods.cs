// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net.Http;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Abstractions;

namespace Microsoft.Identity.Web
{
    /// <inheritdoc/>
    internal partial class DownstreamRestApi : IDownstreamRestApi
    {

        /// <inheritdoc/>
        public async Task<TOutput?> GetForUserAsync<TOutput>(
            string serviceName,
            Action<DownstreamRestApiOptionsNoHttpMethod>? downstreamRestApiOptionsOverride = null,
            ClaimsPrincipal? user = null,
            CancellationToken cancellationToken = default)
            where TOutput : class
        {
            DownstreamRestApiOptions effectiveOptions = MergeOptions(serviceName, downstreamRestApiOptionsOverride, HttpMethod.Get);
            HttpResponseMessage response = await CallRestApiInternalAsync(serviceName, effectiveOptions, false,
                                                                          null, user, cancellationToken).ConfigureAwait(false);


            return await DeserializeOutput<TOutput>(response, effectiveOptions).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<TOutput?> GetForUserAsync<TInput, TOutput>(
            string serviceName,
            TInput input,
            Action<DownstreamRestApiOptionsNoHttpMethod>? downstreamRestApiOptionsOverride = null,
            ClaimsPrincipal? user = null,
            CancellationToken cancellationToken = default)
            where TOutput : class
        {
            DownstreamRestApiOptions effectiveOptions = MergeOptions(serviceName, downstreamRestApiOptionsOverride, HttpMethod.Get);
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
        public async Task<TOutput?> GetForAppAsync<TOutput>(
            string serviceName,
            Action<DownstreamRestApiOptionsNoHttpMethod>? downstreamRestApiOptionsOverride = null,
            CancellationToken cancellationToken = default)
            where TOutput : class
        {
            DownstreamRestApiOptions effectiveOptions = MergeOptions(serviceName, downstreamRestApiOptionsOverride, HttpMethod.Get);
            HttpResponseMessage response = await CallRestApiInternalAsync(serviceName, effectiveOptions, true,
                                                                          null, null, cancellationToken).ConfigureAwait(false);


            return await DeserializeOutput<TOutput>(response, effectiveOptions).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<TOutput?> GetForAppAsync<TInput, TOutput>(
            string serviceName,
            TInput input,
            Action<DownstreamRestApiOptionsNoHttpMethod>? downstreamRestApiOptionsOverride = null,
            CancellationToken cancellationToken = default)
            where TOutput : class
        {
            DownstreamRestApiOptions effectiveOptions = MergeOptions(serviceName, downstreamRestApiOptionsOverride, HttpMethod.Get);
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
        public async Task PostForUserAsync<TInput>(
            string serviceName,
            TInput input,
            Action<DownstreamRestApiOptionsNoHttpMethod>? downstreamRestApiOptionsOverride = null,
            ClaimsPrincipal? user = null,
            CancellationToken cancellationToken = default)
        {
            DownstreamRestApiOptions effectiveOptions = MergeOptions(serviceName, downstreamRestApiOptionsOverride, HttpMethod.Post);
            HttpContent? effectiveInput = SerializeInput(input, effectiveOptions);
            HttpResponseMessage response = await CallRestApiInternalAsync(serviceName, effectiveOptions, false,
                                                                          effectiveInput, user, cancellationToken).ConfigureAwait(false);

            // Only dispose the HttpContent if was created here, not provided by the caller.
            if (input is not HttpContent)
            {
                effectiveInput?.Dispose();
            }

        }

        /// <inheritdoc/>
        public async Task<TOutput?> PostForUserAsync<TInput, TOutput>(
            string serviceName,
            TInput input,
            Action<DownstreamRestApiOptionsNoHttpMethod>? downstreamRestApiOptionsOverride = null,
            ClaimsPrincipal? user = null,
            CancellationToken cancellationToken = default)
            where TOutput : class
        {
            DownstreamRestApiOptions effectiveOptions = MergeOptions(serviceName, downstreamRestApiOptionsOverride, HttpMethod.Post);
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
        public async Task PostForAppAsync<TInput>(
            string serviceName,
            TInput input,
            Action<DownstreamRestApiOptionsNoHttpMethod>? downstreamRestApiOptionsOverride = null,
            CancellationToken cancellationToken = default)
        {
            DownstreamRestApiOptions effectiveOptions = MergeOptions(serviceName, downstreamRestApiOptionsOverride, HttpMethod.Post);
            HttpContent? effectiveInput = SerializeInput(input, effectiveOptions);
            HttpResponseMessage response = await CallRestApiInternalAsync(serviceName, effectiveOptions, true,
                                                                          effectiveInput, null, cancellationToken).ConfigureAwait(false);

            // Only dispose the HttpContent if was created here, not provided by the caller.
            if (input is not HttpContent)
            {
                effectiveInput?.Dispose();
            }

        }

        /// <inheritdoc/>
        public async Task<TOutput?> PostForAppAsync<TInput, TOutput>(
            string serviceName,
            TInput input,
            Action<DownstreamRestApiOptionsNoHttpMethod>? downstreamRestApiOptionsOverride = null,
            CancellationToken cancellationToken = default)
            where TOutput : class
        {
            DownstreamRestApiOptions effectiveOptions = MergeOptions(serviceName, downstreamRestApiOptionsOverride, HttpMethod.Post);
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
        public async Task PutForUserAsync<TInput>(
            string serviceName,
            TInput input,
            Action<DownstreamRestApiOptionsNoHttpMethod>? downstreamRestApiOptionsOverride = null,
            ClaimsPrincipal? user = null,
            CancellationToken cancellationToken = default)
        {
            DownstreamRestApiOptions effectiveOptions = MergeOptions(serviceName, downstreamRestApiOptionsOverride, HttpMethod.Put);
            HttpContent? effectiveInput = SerializeInput(input, effectiveOptions);
            HttpResponseMessage response = await CallRestApiInternalAsync(serviceName, effectiveOptions, false,
                                                                          effectiveInput, user, cancellationToken).ConfigureAwait(false);

            // Only dispose the HttpContent if was created here, not provided by the caller.
            if (input is not HttpContent)
            {
                effectiveInput?.Dispose();
            }

        }

        /// <inheritdoc/>
        public async Task<TOutput?> PutForUserAsync<TInput, TOutput>(
            string serviceName,
            TInput input,
            Action<DownstreamRestApiOptionsNoHttpMethod>? downstreamRestApiOptionsOverride = null,
            ClaimsPrincipal? user = null,
            CancellationToken cancellationToken = default)
            where TOutput : class
        {
            DownstreamRestApiOptions effectiveOptions = MergeOptions(serviceName, downstreamRestApiOptionsOverride, HttpMethod.Put);
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
        public async Task PutForAppAsync<TInput>(
            string serviceName,
            TInput input,
            Action<DownstreamRestApiOptionsNoHttpMethod>? downstreamRestApiOptionsOverride = null,
            CancellationToken cancellationToken = default)
        {
            DownstreamRestApiOptions effectiveOptions = MergeOptions(serviceName, downstreamRestApiOptionsOverride, HttpMethod.Put);
            HttpContent? effectiveInput = SerializeInput(input, effectiveOptions);
            HttpResponseMessage response = await CallRestApiInternalAsync(serviceName, effectiveOptions, true,
                                                                          effectiveInput, null, cancellationToken).ConfigureAwait(false);

            // Only dispose the HttpContent if was created here, not provided by the caller.
            if (input is not HttpContent)
            {
                effectiveInput?.Dispose();
            }

        }

        /// <inheritdoc/>
        public async Task<TOutput?> PutForAppAsync<TInput, TOutput>(
            string serviceName,
            TInput input,
            Action<DownstreamRestApiOptionsNoHttpMethod>? downstreamRestApiOptionsOverride = null,
            CancellationToken cancellationToken = default)
            where TOutput : class
        {
            DownstreamRestApiOptions effectiveOptions = MergeOptions(serviceName, downstreamRestApiOptionsOverride, HttpMethod.Put);
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
        public async Task DeleteForUserAsync<TInput>(
            string serviceName,
            TInput input,
            Action<DownstreamRestApiOptionsNoHttpMethod>? downstreamRestApiOptionsOverride = null,
            ClaimsPrincipal? user = null,
            CancellationToken cancellationToken = default)
        {
            DownstreamRestApiOptions effectiveOptions = MergeOptions(serviceName, downstreamRestApiOptionsOverride, HttpMethod.Delete);
            HttpContent? effectiveInput = SerializeInput(input, effectiveOptions);
            HttpResponseMessage response = await CallRestApiInternalAsync(serviceName, effectiveOptions, false,
                                                                          effectiveInput, user, cancellationToken).ConfigureAwait(false);

            // Only dispose the HttpContent if was created here, not provided by the caller.
            if (input is not HttpContent)
            {
                effectiveInput?.Dispose();
            }

        }

        /// <inheritdoc/>
        public async Task<TOutput?> DeleteForUserAsync<TInput, TOutput>(
            string serviceName,
            TInput input,
            Action<DownstreamRestApiOptionsNoHttpMethod>? downstreamRestApiOptionsOverride = null,
            ClaimsPrincipal? user = null,
            CancellationToken cancellationToken = default)
            where TOutput : class
        {
            DownstreamRestApiOptions effectiveOptions = MergeOptions(serviceName, downstreamRestApiOptionsOverride, HttpMethod.Delete);
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
        public async Task DeleteForAppAsync<TInput>(
            string serviceName,
            TInput input,
            Action<DownstreamRestApiOptionsNoHttpMethod>? downstreamRestApiOptionsOverride = null,
            CancellationToken cancellationToken = default)
        {
            DownstreamRestApiOptions effectiveOptions = MergeOptions(serviceName, downstreamRestApiOptionsOverride, HttpMethod.Delete);
            HttpContent? effectiveInput = SerializeInput(input, effectiveOptions);
            HttpResponseMessage response = await CallRestApiInternalAsync(serviceName, effectiveOptions, true,
                                                                          effectiveInput, null, cancellationToken).ConfigureAwait(false);

            // Only dispose the HttpContent if was created here, not provided by the caller.
            if (input is not HttpContent)
            {
                effectiveInput?.Dispose();
            }

        }

        /// <inheritdoc/>
        public async Task<TOutput?> DeleteForAppAsync<TInput, TOutput>(
            string serviceName,
            TInput input,
            Action<DownstreamRestApiOptionsNoHttpMethod>? downstreamRestApiOptionsOverride = null,
            CancellationToken cancellationToken = default)
            where TOutput : class
        {
            DownstreamRestApiOptions effectiveOptions = MergeOptions(serviceName, downstreamRestApiOptionsOverride, HttpMethod.Delete);
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
    }
}
