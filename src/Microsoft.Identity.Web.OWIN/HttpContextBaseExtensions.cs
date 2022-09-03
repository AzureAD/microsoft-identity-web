// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Web;
using Microsoft.Graph;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Extension methods to retrieve a Graph service or a token acquirer client from the HttpContext
    /// </summary>
    public static class HttpContextBaseExtensions
    {
        /// <summary>
        /// Get the graph service client.
        /// </summary>
        /// <param name="httpContextBase"></param>
        /// <returns></returns>
        public static GraphServiceClient? GetGraphServiceClient(this HttpContextBase httpContextBase)
        {
            return AppBuilderExtension.ServiceProvider?.GetService(typeof(GraphServiceClient)) as GraphServiceClient;
        }

        /// <summary>
        /// Get the token acquirer.
        /// </summary>
        /// <param name="httpContextBase"></param>
        /// <returns></returns>
        public static ITokenAcquirer? GetTokenAcquirer(this HttpContextBase httpContextBase)
        {
            return AppBuilderExtension.ServiceProvider?.GetService(typeof(ITokenAcquirer)) as ITokenAcquirer;
        }
    }
}
