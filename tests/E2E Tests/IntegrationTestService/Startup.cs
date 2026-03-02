// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.Test.Common;
using Microsoft.Identity.Test.LabInfrastructure;
using Microsoft.Identity.Web.TokenCacheProviders.InMemory;

namespace IntegrationTestService
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
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                                  .AddMicrosoftIdentityWebApi(Configuration, jwtBearerScheme: JwtBearerDefaults.AuthenticationScheme, subscribeToJwtBearerMiddlewareDiagnosticsEvents: true)
                                  .EnableTokenAcquisitionToCallDownstreamApi()
                                        .AddDownstreamApi(
                                            TestConstants.SectionNameCalledApi,
                                            Configuration.GetSection(TestConstants.SectionNameCalledApi))
                                        .AddMicrosoftGraph(Configuration.GetSection("GraphBeta"));

            services.AddAuthentication()
                                  .AddMicrosoftIdentityWebApi(Configuration, jwtBearerScheme: TestConstants.CustomJwtScheme2, configSectionName: "AzureAd2", subscribeToJwtBearerMiddlewareDiagnosticsEvents: true)
                                  .EnableTokenAcquisitionToCallDownstreamApi()
                                         .AddDownstreamApi(
                                            TestConstants.SectionNameCalledApi,
                                            Configuration.GetSection(TestConstants.SectionNameCalledApi))
                                        .AddMicrosoftGraph(Configuration.GetSection("GraphBeta"));

            // Will be overriden by tests if needed
            services.AddInMemoryTokenCaches();

            services.AddAuthorization();

            services.AddRazorPages();
            services.AddControllers();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
