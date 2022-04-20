using Owin;
using Microsoft.Identity.Web;
using Microsoft.Ajax.Utilities;
using Microsoft.IdentityModel.Logging;
using Microsoft.Identity.Web.TokenCacheProviders.InMemory;

namespace OwinWebApi
{
    public partial class Startup
    {
        // For more information on configuring authentication, please visit https://go.microsoft.com/fwlink/?LinkId=301864
        public void ConfigureAuth(IAppBuilder app)
        {
            // IdentityModelEventSource.ShowPII = true;

            app.AddMicrosoftIdentityWebApi(configureServices: services =>
            {
                services.AddMicrosoftGraph();
                services.AddInMemoryTokenCaches();
            });
           
        }
    }
}
