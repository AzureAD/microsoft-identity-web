// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Web.Mvc;
using Microsoft.Graph;
using Microsoft.Identity.Abstractions;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Extension methods to retrieve a Graph service or a token acquirer client from the HttpContext.
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
            return AppBuilderExtension.ServiceProvider?.GetService(typeof(GraphServiceClient)) as GraphServiceClient;
        }

        /// <summary>
        /// Get the token acquirer.
        /// </summary>
        /// <param name="controllerBase"></param>
        /// <returns></returns>
        public static ITokenAcquirer? GetTokenAcquirer(this ControllerBase controllerBase)
        {
            return AppBuilderExtension.ServiceProvider?.GetService(typeof(ITokenAcquirer)) as ITokenAcquirer;
        }
    }
}
