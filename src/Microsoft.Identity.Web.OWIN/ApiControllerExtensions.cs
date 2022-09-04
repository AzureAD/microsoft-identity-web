// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Web;
using System.Web.Http;
using Microsoft.Graph;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Extension methods to retrieve a Graph service or a token acquirer client from the HttpContext
    /// </summary>
    public static class ApiControllerExtensions
    {
        /// <summary>
        /// Get the graph service client.
        /// </summary>
        /// <param name="apiController"></param>
        /// <returns></returns>
        public static GraphServiceClient? GetGraphServiceClient(this ApiController apiController)
        {
            return AppBuilderExtension.ServiceProvider?.GetService(typeof(GraphServiceClient)) as GraphServiceClient; 
        }

        /// <summary>
        /// Get the token acquirer.
        /// </summary>
        /// <param name="apiController"></param>
        /// <returns></returns>
        public static ITokenAcquirer? GetTokenAcquirer(this ApiController apiController)
        {
            return AppBuilderExtension.ServiceProvider?.GetService(typeof(ITokenAcquirer)) as ITokenAcquirer;
        }
    }
}
