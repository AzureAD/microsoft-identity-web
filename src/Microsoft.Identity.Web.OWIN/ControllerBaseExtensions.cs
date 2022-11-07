// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Configuration;
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
        /// <returns></returns>
        public static GraphServiceClient GetGraphServiceClient(this ControllerBase _)
        {
            GraphServiceClient? graphServiceClient = TokenAcquirerFactory.GetDefaultInstance().ServiceProvider?.GetService(typeof(GraphServiceClient)) as GraphServiceClient;
            if (graphServiceClient == null)
            {
                throw new ConfigurationErrorsException("Cannot find GraphServiceClient. Did you add services.AddMicrosoftGraph() in Startup_Auth.cs?. See https://aka.ms/ms-id-web/owin. ");
            }
            return graphServiceClient;
        }

        /// <summary>
        /// Get the authorization header provider.
        /// </summary>
        /// <returns></returns>
        public static IAuthorizationHeaderProvider GetAuthorizationHeaderProvider(this ControllerBase _)
        {
            IAuthorizationHeaderProvider? headerProvider = TokenAcquirerFactory.GetDefaultInstance().ServiceProvider?.GetService(typeof(IAuthorizationHeaderProvider)) as IAuthorizationHeaderProvider;
            if (headerProvider == null)
            {
                throw new ConfigurationErrorsException("Cannot find IAuthorizationHeaderProvider. Did you create an OwinTokenAcquirerFactory in Startup_Auth.cs?. See https://aka.ms/ms-id-web/owin. ");
            }
            return headerProvider;
        }

        /// <summary>
        /// Get the downstream REST API service from an ApiController.
        /// </summary>
        /// <returns></returns>
        public static IDownstreamRestApi GetDownstreamRestApi(this ControllerBase _)
        {
            IDownstreamRestApi? downstreamRestApi = TokenAcquirerFactory.GetDefaultInstance().ServiceProvider?.GetService(typeof(IDownstreamRestApi)) as IDownstreamRestApi;
            if (downstreamRestApi == null)
            {
                throw new ConfigurationErrorsException("Cannot find IDownstreamRestApi. Did you add services.AddDownstreamRestApi() in Startup_Auth.cs? See https://aka.ms/ms-id-web/owin. ");
            }
            return downstreamRestApi;
        }

        // An extension method to get the TokenAcquirerFactory is, on purpose, not provided because to avoid encouraging
        // developers to get just a token. Get the authorization header is better because all the protocols are supported, whereas
        // getting a token implies Bearer.
    }
}
