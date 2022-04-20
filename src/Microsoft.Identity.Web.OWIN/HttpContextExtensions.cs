using System.Web;
using Microsoft.Graph;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Extension methods to retrieve a Graph service or a token acquirer client from the HttpContext
    /// </summary>
    public static class HttpContextExtensions
    {
        /// <summary>
        /// Get the graph service client.
        /// </summary>
        /// <param name="httpContext"></param>
        /// <returns></returns>
        public static GraphServiceClient? GetGraphServiceClient(this HttpContext httpContext)
        {
            return AppBuilderExtension.ServiceProvider?.GetService(typeof(GraphServiceClient)) as GraphServiceClient; 
        }

        /// <summary>
        /// Get the token acquirer.
        /// </summary>
        /// <param name="httpContext"></param>
        /// <returns></returns>
        public static ITokenAcquirer? GetTokenAcquirer(this HttpContext httpContext)
        {
            return AppBuilderExtension.ServiceProvider?.GetService(typeof(ITokenAcquirer)) as ITokenAcquirer;
        }
    }
}
