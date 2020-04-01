using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
#if (RequiresHttps)
using Microsoft.AspNetCore.HttpsPolicy;
#endif
using Microsoft.AspNetCore.Mvc;
#if (OrganizationalAuth || IndividualB2CAuth)
using Microsoft.AspNetCore.Authentication;
using Microsoft.Identity.Web;
#endif
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace Company.WebApplication1
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
#if (OrganizationalAuth)
            // Adds Microsoft Identity platform (AAD v2.0) support to protect this Api
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                    .AddProtectedWebApi(Configuration, "AzureAd");
            // Uncomment the following lines if you want your Web API to call a downstream API
            // services.AddProtectedWebApiCallsProtectedWebApi(Configuration, "AzureAd")
            //         .AddInMemoryTokenCaches();
#elif (IndividualB2CAuth)
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                    .AddProtectedWebApi("AzureAdB2C", Configuration, options => Configuration.Bind("AzureAdB2C", options));
            // Uncomment the following lines if you want your Web API to call a downstream API
            // services.AddProtectedWebApiCallsProtectedWebApi(Configuration, "AzureAdB2C")
            //         .AddInMemoryTokenCaches();
#endif

            services.AddControllers();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
#if (RequiresHttps)

            app.UseHttpsRedirection();
#endif

            app.UseRouting();

#if (OrganizationalAuth || IndividualAuth)
            app.UseAuthentication();
#endif
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
