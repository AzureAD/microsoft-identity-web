// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Web.Http;
using System.Web.Mvc;
using Microsoft.Graph;
using Microsoft.Identity.Abstractions;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Extension methods to retrieve a Graph service client, or interfaces used to call
    /// downstream web APIs.
    /// </summary>
    public static class ApiControllerExtensions
    {
        /// <summary>
        /// Get the Graph service client.
        /// </summary>
        /// <param name="apiController"></param>
        /// <returns></returns>
        public static GraphServiceClient? GetGraphServiceClient(this ApiController apiController)
        {
            return TokenAcquirerFactory.GetDefaultInstance().ServiceProvider?.GetService(typeof(GraphServiceClient)) as GraphServiceClient;
        }

        /// <summary>
        /// Get the authorization header provider.
        /// </summary>
        /// <param name="apiController"></param>
        /// <returns></returns>
        public static IAuthorizationHeaderProvider? GettAuthorizationHeaderProvider(this ApiController apiController)
        {
            return TokenAcquirerFactory.GetDefaultInstance().ServiceProvider?.GetService(typeof(IAuthorizationHeaderProvider)) as IAuthorizationHeaderProvider;
        }

        /// <summary>
        /// Get the downstream REST API service.
        /// </summary>
        /// <param name="apiController"></param>
        /// <returns></returns>
        public static IDownstreamRestApi? GetDownstreamRestApi(this ApiController apiController)
        {
            return TokenAcquirerFactory.GetDefaultInstance().ServiceProvider?.GetService(typeof(IDownstreamRestApi)) as IDownstreamRestApi;
        }
    }
}
