using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Owin;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.TokenCacheProviders.InMemory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Client;

namespace OwinWebApp
{
    public partial class Startup
    {
        public void ConfigureAuth(IAppBuilder app)
        {
            app.SetDefaultSignInAsAuthenticationType(CookieAuthenticationDefaults.AuthenticationType);

            app.UseCookieAuthentication(new CookieAuthenticationOptions());

            app.AddMicrosoftIdentityWebApp(configureServices: services =>
            {
                services.Configure<ConfidentialClientApplicationOptions>(options => { options.RedirectUri = "https://localhost:44386/"; });
                services.AddMicrosoftGraph();
                services.AddInMemoryTokenCaches();
            });
                 
        }
    }
}
