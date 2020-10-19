// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Interface used to call a downstream web API, for instance from controllers.
    /// </summary>
    public interface IDownstreamWebApi
    {
        /// <summary>
        /// Calls the downstream web API for the user, based on a description of the
        /// downstream web API in the configuration.
        /// </summary>
        /// <param name="serviceName">Name of the service describing the downstream web API. There can
        /// be several configuration named sections mapped to a <see cref="DownstreamWebApiOptions"/>,
        /// each for one downstream web API. You can pass-in null, but in that case <paramref name="calledDownstreamWebApiOptionsOverride"/>
        /// needs to be set.</param>
        /// <param name="calledDownstreamWebApiOptionsOverride">Overrides the options proposed in the configuration described
        /// by <paramref name="serviceName"/>.</param>
        /// <param name="user">[Optional] Claims representing a user. This is useful on platforms like Blazor
        /// or Azure Signal R, where the HttpContext is not available. In other platforms, the library
        /// will find the user from the HttpContext.</param>
        /// <param name="content">HTTP context in the case where <see cref="DownstreamWebApiOptions.HttpMethod"/> is
        /// <see cref="HttpMethod.Patch"/>, <see cref="HttpMethod.Post"/>, <see cref="HttpMethod.Put"/>.</param>
        /// <returns>An <see cref="HttpResponseMessage"/> that the application will process.</returns>
        public Task<HttpResponseMessage> CallWebApiForUserAsync(
            string serviceName,
            Action<DownstreamWebApiOptions>? calledDownstreamWebApiOptionsOverride = null,
            ClaimsPrincipal? user = null,
            StringContent? content = null);

        /// <summary>
        /// Calls a downstream web API consuming JSON with some data and returns data.
        /// </summary>
        /// <typeparam name="TInput">Input type.</typeparam>
        /// <typeparam name="TOutput">Output type.</typeparam>
        /// <param name="serviceName">Name of the service describing the downstream web API. There can
        /// be several configuration named sections mapped to a <see cref="DownstreamWebApiOptions"/>,
        /// each for one downstream web API. You can pass-in null, but in that case <paramref name="downstreamWebApiOptionsOverride"/>
        /// needs to be set.</param>
        /// <param name="input">Input parameter to the downstream web API.</param>
        /// <param name="downstreamWebApiOptionsOverride">Overrides the options proposed in the configuration described
        /// by <paramref name="serviceName"/>.</param>
        /// <param name="user">[Optional] Claims representing a user. This is useful in platforms like Blazor
        /// or Azure Signal R, where the HttpContext is not available. In other platforms, the library
        /// will find the user from the HttpContext.</param>
        /// <returns>The value returned by the downstream web API.</returns>
        /// <example>
        /// A list method that returns an IEnumerable&lt;Todo&gt;&gt;.
        /// <code>
        /// public async Task&lt;IEnumerable&lt;Todo&gt;&gt; GetAsync()
        /// {
        ///  return await _downstreamWebApi.CallWebApiForUserAsync&lt;object, IEnumerable&lt;Todo&gt;&gt;(
        ///         ServiceName,
        ///         null,
        ///         options =>
        ///         {
        ///           options.RelativePath = $"api/todolist";
        ///         });
        /// }
        /// </code>
        ///
        /// Example of editing.
        /// <code>
        /// public async Task&lt;Todo&gt; EditAsync(Todo todo)
        /// {
        ///   return await _downstreamWebApi.CallWebApiForUserAsync&lt;Todo, Todo&gt;(
        ///         ServiceName,
        ///         todo,
        ///         options =>
        ///         {
        ///            options.HttpMethod = HttpMethod.Patch;
        ///            options.RelativePath = $"api/todolist/{todo.Id}";
        ///         });
        /// }
        /// </code>
        /// </example>
        public Task<TOutput?> CallWebApiForUserAsync<TInput, TOutput>(
            string serviceName,
            TInput input,
            Action<DownstreamWebApiOptions>? downstreamWebApiOptionsOverride = null,
            ClaimsPrincipal? user = null)
            where TOutput : class;

        /// <summary>
        /// Get a strongly typed response from the web API.
        /// </summary>
        /// <typeparam name="TOutput">Output type.</typeparam>
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
        /// <returns>A strongly typed response from the web API.</returns>
        public Task<TOutput?> GetForUserAsync<TOutput>(
            string serviceName,
            string relativePath,
            Action<DownstreamWebApiOptions>? downstreamWebApiOptionsOverride = null,
            ClaimsPrincipal? user = null)
            where TOutput : class;

        /// <summary>
        /// Sends an HttpPost to the web API.
        /// </summary>
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
        /// <returns>A Task.</returns>
        public Task PostWebApiForUserAsync(
            string serviceName,
            string relativePath,
            Action<DownstreamWebApiOptions>? downstreamWebApiOptionsOverride = null,
            ClaimsPrincipal? user = null);

        /// <summary>
        /// Calls the web API with an HttpPost, providing strongly typed input and getting
        /// strongly typed output.
        /// </summary>
        /// <typeparam name="TOutput">Output type.</typeparam>
        /// <typeparam name="TInput">Input type.</typeparam>
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
        /// <returns>A strongly typed response from the web API.</returns>
        public Task<TOutput?> PostForUserAsync<TOutput, TInput>(
            string serviceName,
            string relativePath,
            TInput inputData,
            Action<DownstreamWebApiOptions>? downstreamWebApiOptionsOverride = null,
            ClaimsPrincipal? user = null)
            where TOutput : class;

        /// <summary>
        /// Calls the web API endpoint with an HttpPut, providing strongly typed input data.
        /// </summary>
        /// <typeparam name="TInput">Input type.</typeparam>
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
        /// <returns>The value returned by the downstream web API.</returns>
        public Task PutForUserAsync<TInput>(
            string serviceName,
            string relativePath,
            TInput inputData,
            Action<DownstreamWebApiOptions>? downstreamWebApiOptionsOverride = null,
            ClaimsPrincipal? user = null);

        /// <summary>
        /// Calls the web API endpoint with an HttpPut, provinding strongly typed input data
        /// and getting back strongly typed data.
        /// </summary>
        /// <typeparam name="TOutput">Output type.</typeparam>
        /// <typeparam name="TInput">Input type.</typeparam>
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
        /// <returns>A strongly typed response from the web API.</returns>
        public Task<TOutput?> PutForUserAsync<TOutput, TInput>(
            string serviceName,
            string relativePath,
            TInput inputData,
            Action<DownstreamWebApiOptions>? downstreamWebApiOptionsOverride = null,
            ClaimsPrincipal? user = null)
            where TOutput : class;

        /// <summary>
        /// Call a web API endpoint with an HttpGet,
        /// and return strongly typed data.
        /// </summary>
        /// <typeparam name="TOutput">Output type.</typeparam>
        /// <param name="serviceName">Name of the service describing the downstream web API. There can
        /// be several configuration named sections mapped to a <see cref="DownstreamWebApiOptions"/>,
        /// each for one downstream web API. You can pass-in null, but in that case <paramref name="downstreamWebApiOptionsOverride"/>
        /// needs to be set.</param>
        /// <param name="downstreamWebApiOptionsOverride">Overrides the options proposed in the configuration described
        /// by <paramref name="serviceName"/>.</param>
        /// <param name="user">[Optional] Claims representing a user. This is useful in platforms like Blazor
        /// or Azure Signal R, where the HttpContext is not available. In other platforms, the library
        /// will find the user from the HttpContext.</param>
        /// <returns>The value returned by the downstream web API.</returns>
        public Task<TOutput?> CallWebApiForUserAsync<TOutput>(
            string serviceName,
            Action<DownstreamWebApiOptions>? downstreamWebApiOptionsOverride = null,
            ClaimsPrincipal? user = null)
            where TOutput : class;

        /// <summary>
        /// Call a web API with a strongly typed input, with an HttpGet.
        /// </summary>
        /// <typeparam name="TInput">Input type.</typeparam>
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
        /// <returns>The value returned by the downstream web API.</returns>
        public Task GetForUserAsync<TInput>(
            string serviceName,
            TInput inputData,
            Action<DownstreamWebApiOptions>? downstreamWebApiOptionsOverride = null,
            ClaimsPrincipal? user = null);

        /// <summary>
        /// Calls the downstream web API for the app, with the required scopes.
        /// </summary>
        /// <param name="serviceName">Name of the service describing the downstream web API. There can
        /// be several configuration named sections mapped to a <see cref="DownstreamWebApiOptions"/>,
        /// each for one downstream web API. You can pass-in null, but in that case <paramref name="downstreamWebApiOptionsOverride"/>
        /// needs to be set.</param>
        /// <param name="downstreamWebApiOptionsOverride">Overrides the options proposed in the configuration described
        /// by <paramref name="serviceName"/>.</param>
        /// <param name="content">HTTP content in the case where <see cref="DownstreamWebApiOptions.HttpMethod"/> is
        /// <see cref="HttpMethod.Patch"/>, <see cref="HttpMethod.Post"/>, <see cref="HttpMethod.Put"/>.</param>
        /// <returns>An <see cref="HttpResponseMessage"/> that the application will process.</returns>
        public Task<HttpResponseMessage> CallWebApiForAppAsync(
            string serviceName,
            Action<DownstreamWebApiOptions>? downstreamWebApiOptionsOverride = null,
            StringContent? content = null);
    }
}
