using System.Web;
using Microsoft.Graph;

namespace Microsoft.Identity.Web
{
    public static class HttpContextExtensions
    {
        public static GraphServiceClient GetGraphServiceClient(this HttpContext httpContext)
        {
            return AppBuilderExtension.ServiceProvider.GetService(typeof(GraphServiceClient)) as GraphServiceClient; 
        }

        public static ITokenAcquirer GetTokenAcquirer(this HttpContext httpContext)
        {
            return AppBuilderExtension.ServiceProvider.GetService(typeof(ITokenAcquirer)) as ITokenAcquirer;
        }
    }
}
