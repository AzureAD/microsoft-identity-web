using Owin;
using Microsoft.Identity.Web;

namespace OwinWebApi
{
    public partial class Startup
    {
        // For more information on configuring authentication, please visit https://go.microsoft.com/fwlink/?LinkId=301864
        public void ConfigureAuth(IAppBuilder app)
        {
            app.AddMicrosoftIdentityWebApi(configureServices: services =>
            {
                services.AddMicrosoftGraph();
            });
        }
    }
}
