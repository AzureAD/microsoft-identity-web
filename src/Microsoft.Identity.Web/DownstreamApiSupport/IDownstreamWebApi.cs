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
        /// Calls the Web API for the user, based on a description of the
        /// web API in the configuration.
        /// </summary>
        /// <param name="optionsInstanceName">Name of the options instance describing the API. There can
        /// be several configuration namedsections mapped to a <see cref="DownstreamApiOptions"/>,
        /// each for one API. You can pass-in null, but in that case <paramref name="calledApiOptionsOverride"/>
        /// need to be set.</param>
        /// <param name="calledApiOptionsOverride">Overides the options proposed in the configuration described
        /// by <paramref name="optionsInstanceName"/>.</param>
        /// <param name="user">[Optional] claims representing a user. This is useful in scenarios like Blazor
        /// or Azure Signal R where the HttpContext is not available. In the other scenarios, the library
        /// will find the user itself.</param>
        /// <param name="content">Http content in the case where <see cref="DownstreamApiOptions.HttpMethod"/> is
        /// <see cref="HttpMethod.Patch"/>, <see cref="HttpMethod.Post"/>, <see cref="HttpMethod.Put"/>.</param>
        /// <returns>An <see cref="HttpResponseMessage"/> that the application will process.</returns>
        public Task<HttpResponseMessage> CallWebApiForUserAsync(
            string optionsInstanceName,
            Action<DownstreamApiOptions>? calledApiOptionsOverride = null,
            ClaimsPrincipal? user = null,
            StringContent? content = null);

        /// <summary>
        /// Calls a Web API consuming JSon with some data and returns data.
        /// </summary>
        /// <typeparam name="TInput">Input type.</typeparam>
        /// <typeparam name="TOutput">Output type.</typeparam>
        /// <param name="optionsInstanceName">Name of the options instance describing the API. There can
        /// be several configuration namedsections mapped to a <see cref="DownstreamApiOptions"/>,
        /// each for one API. You can pass-in null, but in that case <paramref name="downstreamApiOptionsOverride"/>
        /// need to be set.</param>
        /// <param name="input">Input parameter to the API.</param>
        /// <param name="downstreamApiOptionsOverride">Overides the options proposed in the configuration described
        /// by <paramref name="optionsInstanceName"/>.</param>
        /// <param name="user">[Optional] claims representing a user. This is useful in scenarios like Blazor
        /// or Azure Signal R where the HttpContext is not available. In the other scenarios, the library
        /// will find the user itself.</param>
        /// <returns>The value returned by the API.</returns>
        /// <example>
        /// A list method that returns an IEnumerable&lt;Todo&gt;&gt;
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
            string optionsInstanceName,
            TInput input,
            Action<DownstreamApiOptions>? downstreamApiOptionsOverride = null,
            ClaimsPrincipal? user = null)
            where TOutput : class;

        /// <summary>
        /// Calls the Web API for the app, with the required scopes.
        /// </summary>
        /// <param name="optionsInstanceName">Name of the options instance describing the API. There can
        /// be several configuration namedsections mapped to a <see cref="DownstreamApiOptions"/>,
        /// each for one API. You can pass-in null, but in that case <paramref name="downstreamApiOptionsOverride"/>
        /// need to be set.</param>
        /// <param name="downstreamApiOptionsOverride">Overides the options proposed in the configuration described
        /// by <paramref name="optionsInstanceName"/>.</param>
        /// <param name="content">Http content in the case where <see cref="DownstreamApiOptions.HttpMethod"/> is
        /// <see cref="HttpMethod.Patch"/>, <see cref="HttpMethod.Post"/>, <see cref="HttpMethod.Put"/>.</param>
        /// <returns>An <see cref="HttpResponseMessage"/> that the application will process.</returns>
        public Task<HttpResponseMessage> CallWebApiForAppAsync(
            string optionsInstanceName,
            Action<DownstreamApiOptions>? downstreamApiOptionsOverride = null,
            StringContent? content = null);
    }
}
