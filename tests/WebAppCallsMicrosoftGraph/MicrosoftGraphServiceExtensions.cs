using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Graph;
using Microsoft.Identity.Web;

namespace WebAppCallsMicrosoftGraph
{
    public static class MicrosoftGraphServiceExtensions
    {
        public static void AddMicrosoftGraph(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<GraphServiceClient, GraphServiceClient>(serviceProvider =>
            {
                var tokenAquisitionService = serviceProvider.GetService<ITokenAcquisition>();
                return new GraphServiceClient(new WebSignInCredential(tokenAquisitionService));
            });
        }
    }
}
