// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Web.Mvc;
using Microsoft.Graph;
using Microsoft.Identity.Abstractions;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Extension methods to retrieve a Graph service client and interfaces used
    /// to call a downstream web API.
    /// </summary>
    public static class ControllerBaseExtensions
    {
        /// <summary>
        /// Get the Graph service client.
        /// </summary>
        /// <param name="controllerBase"></param>
        /// <returns></returns>
        public static GraphServiceClient? GetGraphServiceClient(this ControllerBase controllerBase)
        {
            return TokenAcquirerFactory.GetDefaultInstance().ServiceProvider?.GetService(typeof(GraphServiceClient)) as GraphServiceClient;
        }

        /// <summary>
        /// Get the authorization header provider.
        /// </summary>
        /// <param name="controllerBase"></param>
        /// <returns></returns>
        public static IAuthorizationHeaderProvider? GettAuthorizationHeaderProvider(this ControllerBase controllerBase)
        {
            return TokenAcquirerFactory.GetDefaultInstance().ServiceProvider?.GetService(typeof(IAuthorizationHeaderProvider)) as IAuthorizationHeaderProvider;
        }

        /// <summary>
        /// Get the authorization header provider.
        /// </summary>
        /// <param name="controllerBase"></param>
        /// <returns></returns>
        public static IDownstreamRestApi? GetDownstreamRestApi(this ControllerBase controllerBase)
        {
            return TokenAcquirerFactory.GetDefaultInstance().ServiceProvider?.GetService(typeof(IDownstreamRestApi)) as IDownstreamRestApi;
        }
    }
}
