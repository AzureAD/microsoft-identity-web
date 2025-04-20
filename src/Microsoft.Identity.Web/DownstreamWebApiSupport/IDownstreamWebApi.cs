// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Interface used to call a downstream web API, for instance from controllers.
    /// </summary>
    [Obsolete("Use IDownstreamApi in Microsoft.Identity.Abstractions, implemented in Microsoft.Identity.Web.DownstreamApi." +
        "See aka.ms/id-web-downstream-api-v2 for migration details.", false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
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
            StringContent? content = null)
        {
            return CallWebApiForUserAsync(
                serviceName,
                null,
                calledDownstreamWebApiOptionsOverride,
                user,
                content);
        }

        /// <summary>
        /// Calls the downstream web API for the user, based on a description of the
        /// downstream web API in the configuration.
        /// </summary>
        /// <param name="serviceName">Name of the service describing the downstream web API. There can
        /// be several configuration named sections mapped to a <see cref="DownstreamWebApiOptions"/>,
        /// each for one downstream web API. You can pass-in null, but in that case <paramref name="calledDownstreamWebApiOptionsOverride"/>
        /// needs to be set.</param>
        /// <param name="authenticationScheme">Authentication scheme. If null, will use OpenIdConnectDefault.AuthenticationScheme
        /// if called from a web app, and JwtBearerDefault.AuthenticationScheme if called from a web API.</param>
        /// <param name="calledDownstreamWebApiOptionsOverride">Overrides the options proposed in the configuration described
        /// by <paramref name="serviceName"/>.</param>
        /// <param name="user">[Optional] Claims representing a user. This is useful on platforms like Blazor
        /// or Azure Signal R, where the HttpContext is not available. In other platforms, the library
        /// will find the user from the HttpContext.</param>
        /// <param name="content">HTTP context in the case where <see cref="DownstreamWebApiOptions.HttpMethod"/> is
        /// <see cref="HttpMethod.Patch"/>, <see cref="HttpMethod.Post"/>, <see cref="HttpMethod.Put"/>.</param>
        /// <returns>An <see cref="HttpResponseMessage"/> that the application will process.</returns>
        Task<HttpResponseMessage> CallWebApiForUserAsync(
            string serviceName,
            string? authenticationScheme,
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
        /// A list method that returns an IEnumerable&lt;MyItem&gt;&gt;.
        /// <code>
        /// public Task&lt;IEnumerable&lt;MyItem&gt;&gt; GetAsync()
        /// {
        ///  return _downstreamWebApi.CallWebApiForUserAsync&lt;object, IEnumerable&lt;MyItem&gt;&gt;(
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
        /// public Task&lt;MyItem&gt; EditAsync(MyItem myItem)
        /// {
        ///   return _downstreamWebApi.CallWebApiForUserAsync&lt;MyItem, MyItem&gt;(
        ///         ServiceName,
        ///         nyItem,
        ///         options =>
        ///         {
        ///            options.HttpMethod = HttpMethod.Patch;
        ///            options.RelativePath = $"api/todolist/{myItem.Id}";
        ///         });
        /// }
        /// </code>
        /// </example>
#if NET6_0_OR_GREATER
        [RequiresUnreferencedCode("Calls System.Text.Json.JsonSerializer.Serialize<TValue>(TValue, JsonSerializerOptions).")]
#endif
#if NET8_0_OR_GREATER
        [RequiresDynamicCode("Calls System.Text.Json.JsonSerializer.Serialize<TValue>(TValue, JsonSerializerOptions).")]
#endif   
        public Task<TOutput?> CallWebApiForUserAsync<TInput, TOutput>(
            string serviceName,
            TInput input,
            Action<DownstreamWebApiOptions>? downstreamWebApiOptionsOverride = null,
            ClaimsPrincipal? user = null)
            where TOutput : class
        {
            return CallWebApiForUserAsync<TInput, TOutput>(
                serviceName,
                input,
                null,
                downstreamWebApiOptionsOverride,
                user);
        }

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
        /// <param name="authenticationScheme">Authentication scheme. If null, will use OpenIdConnectDefault.AuthenticationScheme
        /// if called from a web app, and JwtBearerDefault.AuthenticationScheme if called from a web API.</param>
        /// <param name="downstreamWebApiOptionsOverride">Overrides the options proposed in the configuration described
        /// by <paramref name="serviceName"/>.</param>
        /// <param name="user">[Optional] Claims representing a user. This is useful in platforms like Blazor
        /// or Azure Signal R, where the HttpContext is not available. In other platforms, the library
        /// will find the user from the HttpContext.</param>
        /// <returns>The value returned by the downstream web API.</returns>
        /// <example>
        /// A list method that returns an IEnumerable&lt;MyItem&gt;&gt;.
        /// <code>
        /// public Task&lt;IEnumerable&lt;MyItem&gt;&gt; GetAsync()
        /// {
        ///  return _downstreamWebApi.CallWebApiForUserAsync&lt;object, IEnumerable&lt;MyItem&gt;&gt;(
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
        /// public Task&lt;MyItem&gt; EditAsync(MyItem myItem)
        /// {
        ///   return _downstreamWebApi.CallWebApiForUserAsync&lt;MyItem, MyItem&gt;(
        ///         ServiceName,
        ///         nyItem,
        ///         options =>
        ///         {
        ///            options.HttpMethod = HttpMethod.Patch;
        ///            options.RelativePath = $"api/todolist/{myItem.Id}";
        ///         });
        /// }
        /// </code>
        /// </example>
#if NET6_0_OR_GREATER
        [RequiresUnreferencedCode("Calls System.Text.Json.JsonSerializer.Serialize<TValue>(TValue, JsonSerializerOptions).")]
#endif
#if NET8_0_OR_GREATER
        [RequiresDynamicCode("Calls System.Text.Json.JsonSerializer.Deserialize<TValue>(String, JsonSerializerOptions).")]
#endif    
        Task<TOutput?> CallWebApiForUserAsync<TInput, TOutput>(
            string serviceName,
            TInput input,
            string? authenticationScheme,
            Action<DownstreamWebApiOptions>? downstreamWebApiOptionsOverride = null,
            ClaimsPrincipal? user = null)
            where TOutput : class;

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
            StringContent? content = null)
        {
            return CallWebApiForAppAsync(
                serviceName,
                null,
                downstreamWebApiOptionsOverride,
                content);
        }

        /// <summary>
        /// Calls the downstream web API for the app, with the required scopes.
        /// </summary>
        /// <param name="serviceName">Name of the service describing the downstream web API. There can
        /// be several configuration named sections mapped to a <see cref="DownstreamWebApiOptions"/>,
        /// each for one downstream web API. You can pass-in null, but in that case <paramref name="downstreamWebApiOptionsOverride"/>
        /// needs to be set.</param>
        /// <param name="authenticationScheme">Authentication scheme. If null, will use OpenIdConnectDefault.AuthenticationScheme
        /// if called from a web app, and JwtBearerDefault.AuthenticationScheme if called from a web API.</param>
        /// <param name="downstreamWebApiOptionsOverride">Overrides the options proposed in the configuration described
        /// by <paramref name="serviceName"/>.</param>
        /// <param name="content">HTTP content in the case where <see cref="DownstreamWebApiOptions.HttpMethod"/> is
        /// <see cref="HttpMethod.Patch"/>, <see cref="HttpMethod.Post"/>, <see cref="HttpMethod.Put"/>.</param>
        /// <returns>An <see cref="HttpResponseMessage"/> that the application will process.</returns>
        Task<HttpResponseMessage> CallWebApiForAppAsync(
            string serviceName,
            string? authenticationScheme,
            Action<DownstreamWebApiOptions>? downstreamWebApiOptionsOverride = null,
            StringContent? content = null);
    }
}
