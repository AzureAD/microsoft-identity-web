using Microsoft.Identity.Web;
using Microsoft.Identity.Web.OWIN;
using Owin;

namespace OwinWebApi
{
    public partial class Startup
    {
        // For more information on configuring authentication, please visit https://go.microsoft.com/fwlink/?LinkId=301864
        public void ConfigureAuth(IAppBuilder app)
        {
            /*
                        IdentityModelEventSource.Logger.LogLevel = EventLevel.Verbose;
                        IdentityModelEventSource.ShowPII = true;
                        var listener = new TextWriterEventListener(@"c:\temp\diag.txt");
                        listener.EnableEvents(IdentityModelEventSource.Logger, EventLevel.LogAlways);
            */

            OwinTokenAcquirerFactory factory = TokenAcquirerFactory.GetDefaultInstance<OwinTokenAcquirerFactory>();
            app.AddMicrosoftIdentityWebApi(factory);
            factory.Services
                .AddMicrosoftGraph()
                .AddDownstreamApi("DownstreamAPI", factory.Configuration.GetSection("DownstreamAPI"));
            factory.Build();
        }
    }
}
