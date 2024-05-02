using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Owin;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.TokenCacheProviders.InMemory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web.OWIN;

namespace OwinWebApp
{
    public partial class Startup
    {
        public void ConfigureAuth(IAppBuilder app)
        {
            app.SetDefaultSignInAsAuthenticationType(CookieAuthenticationDefaults.AuthenticationType);

            app.UseCookieAuthentication(new CookieAuthenticationOptions());

            OwinTokenAcquirerFactory factory = TokenAcquirerFactory.GetDefaultInstance<OwinTokenAcquirerFactory>();

            app.AddMicrosoftIdentityWebApp(factory);
            factory.Services
                .Configure<ConfidentialClientApplicationOptions>(options => { options.RedirectUri = "https://localhost:44386/"; })
                .AddMicrosoftGraph()
                .AddDownstreamApi("DownstreamAPI1", factory.Configuration.GetSection("DownstreamAPI"))
                .AddInMemoryTokenCaches();
            factory.Build();

            /*
            app.AddMicrosoftIdentityWebApp(configureServices: services =>
            {
                services
                .Configure<ConfidentialClientApplicationOptions>(options => { options.RedirectUri = "https://localhost:44386/"; })
                .AddMicrosoftGraph()
               // WE cannot do that today: Configuration is not available.
               // .AddDownstreamApi("CalledApi", null)
                .AddInMemoryTokenCaches();
            });
            */

        }
    }
}
