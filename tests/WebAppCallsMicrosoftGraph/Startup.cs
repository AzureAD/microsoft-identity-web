// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.TokenCacheProviders.InMemory;
using Microsoft.Identity.Web.UI;

namespace WebAppCallsMicrosoftGraph
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
            services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
                    .AddMicrosoftIdentityWebApp(Configuration.GetSection("AzureAd"))
                        .CallsWebApi()
                           .AddInMemoryTokenCaches();  // Add a delegate overload. Should return the parent builder

            /*
             *   services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
                    .AddMicrosoftIdentityPlatformWebApp(Configuration.GetSection("AzureAd"))
                            .CallsWebApi()
                                .AddInMemoryTokenCaches() // Change the builder

                    .AddAuthentication()
                    .AddMicrosoftIdentityPlatformWebApi(Configuration.GetSection("AzureAd"))

*/


            /* OR
                        services.AddMicrosoftWebAppAuthentication(Configuration)
                                .CallsWebApi()
                                .AddInMemoryTokenCaches();

                        services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
                                           .AddMicrosoftWebApp(options =>
                                           {
                                               Configuration.Bind("AzureAd", options);
                                               // do something
                                           })
                                           .CallsWebApi(options =>
                                           {
                                               Configuration.Bind("AzureAd", options);
                                               // do something
                                           }
                                           )
                                           .AddInMemoryTokenCaches();
            */
            services.AddMicrosoftGraph(Configuration, new string[] { "user.read" });

            services.AddRazorPages().AddMvcOptions(options =>
            {
                var policy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
                options.Filters.Add(new AuthorizeFilter(policy));
            }).AddMicrosoftIdentityUI();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
                endpoints.MapControllers();
            });
        }
    }
}
