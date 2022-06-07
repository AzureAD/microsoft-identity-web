// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Interface used to call a downstream REST API, for instance from controllers.
    /// </summary>
    public interface IDownstreamRestApi
    {
        /// <summary>
        /// Calls the downstream web API for the user, based on a description of the
        /// downstream web API in the configuration.
        /// </summary>
        /// <param name="serviceName">Name of the service describing the downstream web API. There can
        /// be several configuration named sections mapped to a <see cref="DownstreamRestApiOptions"/>,
        /// each for one downstream web API. You can pass-in null, but in that case <paramref name="calledDownstreamWebApiOptionsOverride"/>
        /// needs to be set.</param>
        /// <param name="calledDownstreamWebApiOptionsOverride">Overrides the options proposed in the configuration described
        /// by <paramref name="serviceName"/>.</param>
        /// <param name="user">[Optional] Claims representing a user. This is useful on platforms like Blazor
        /// or Azure Signal R, where the HttpContext is not available. In other platforms, the library
        /// will find the user from the HttpContext.</param>
        /// <param name="content">HTTP context in the case where <see cref="DownstreamRestApiOptions.HttpMethod"/> is
        /// <see cref="HttpMethod.Patch"/>, <see cref="HttpMethod.Post"/>, <see cref="HttpMethod.Put"/>.</param>
        /// <returns>An <see cref="HttpResponseMessage"/> that the application will process.</returns>
        Task<HttpResponseMessage> CallRestApiForUserAsync(
            string serviceName,
            Action<DownstreamRestApiOptions>? calledDownstreamWebApiOptionsOverride = null,
            ClaimsPrincipal? user = null,
            StringContent? content = null);

        /// <summary>
        /// Calls the downstream web API for the app, with the required scopes.
        /// </summary>
        /// <param name="serviceName">Name of the service describing the downstream web API. There can
        /// be several configuration named sections mapped to a <see cref="DownstreamRestApiOptions"/>,
        /// each for one downstream web API. You can pass-in null, but in that case <paramref name="downstreamWebApiOptionsOverride"/>
        /// needs to be set.</param>
        /// <param name="downstreamWebApiOptionsOverride">Overrides the options proposed in the configuration described
        /// by <paramref name="serviceName"/>.</param>
        /// <param name="content">HTTP content in the case where <see cref="DownstreamRestApiOptions.HttpMethod"/> is
        /// <see cref="HttpMethod.Patch"/>, <see cref="HttpMethod.Post"/>, <see cref="HttpMethod.Put"/>.</param>
        /// <returns>An <see cref="HttpResponseMessage"/> that the application will process.</returns>
        Task<HttpResponseMessage> CallWebApiForAppAsync(
            string serviceName,
            Action<DownstreamRestApiOptions>? downstreamWebApiOptionsOverride = null,
            StringContent? content = null);
    }
}
