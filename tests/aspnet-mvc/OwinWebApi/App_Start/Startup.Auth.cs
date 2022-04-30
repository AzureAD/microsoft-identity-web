using Owin;
using Microsoft.Identity.Web;
using Microsoft.IdentityModel.Logging;
using Microsoft.Identity.Web.TokenCacheProviders.InMemory;
using System.Diagnostics.Tracing;
using System.IO;
using System;

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
            app.AddMicrosoftIdentityWebApi(configureServices: services =>
            {
                services.AddMicrosoftGraph();
                services.AddInMemoryTokenCaches();
            });
           
        }
    }
}
