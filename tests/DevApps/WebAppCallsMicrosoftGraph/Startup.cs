// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// #define USE_SIGNED_ASSERTION
using System;
#if NET8_0
using Microsoft.AspNetCore.Authentication.Certificate;
#endif
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Connections;
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
#if USE_SIGNED_ASSERTION
            string configSection = "AzureAdCertless";
#else
            string configSection = "AzureAd";
#endif

            int count = 0;
#if NET8_0
            services.AddAuthentication(CertificateAuthenticationDefaults.AuthenticationScheme)
                    .AddCertificate();
#endif
            services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
                    .AddMicrosoftIdentityWebApp(options =>
                    {
                        // Verification of the fix for #2456
                        if (count>0)
                        {
                            throw new ArgumentException("AddMicrosoftIdentityWebApp(delegate). the delegate" +
                                "is called more than once");
                        }
                        else
                        {
                            count++;
                        }

                        Configuration.Bind(configSection, options);
                    }
                )
                        .EnableTokenAcquisitionToCallDownstreamApi()
                           .AddMicrosoftGraph(Configuration.GetSection("GraphBeta"))
                           .AddDownstreamApi("GraphBeta", Configuration.GetSection("GraphBeta"))
                           .AddInMemoryTokenCaches();

            //          services.Configure<OpenIdConnectOptions>(OpenIdConnectDefaults.AuthenticationScheme,
            //options =>
            //{
            //    var previous = options.Events.OnAuthorizationCodeReceived;
            //    options.Events.OnAuthorizationCodeReceived = async context =>
            //    {
            //        // In the case you want to change the tenant ID based on the MyApp query parameter:
            //        context.ProtocolMessage.DomainHint = "{yourTenantID}";
            //        await previous(context);
            //    };
            //});
            //services.Configure<ConfidentialClientApplicationOptions>(OpenIdConnectDefaults.AuthenticationScheme,
            //    options => { options.AzureRegion = ConfidentialClientApplication.AttemptRegionDiscovery; });

            /*
             *   services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
                    .AddMicrosoftIdentityWebApp(Configuration.GetSection("AzureAd"))
                            .EnableTokenAcquisitionToCallDownstreamApi()
                                .AddInMemoryTokenCaches() // Change the builder

                    .AddAuthentication()
                    .AddMicrosoftIdentityWebApi(Configuration.GetSection("AzureAd"))

*/


            /* OR
                        services.AddMicrosoftIdentityWebAppAuthentication(Configuration)
                                .EnableTokenAcquisitionToCallDownstreamApi()
                                .AddInMemoryTokenCaches();

                        services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
                                           .AddMicrosoftIdentityWebApp(options =>
                                           {
                                               Configuration.Bind("AzureAd", options);
                                               // do something
                                           })
                                           .EnableTokenAcquisitionToCallDownstreamApi(options =>
                                           {
                                               Configuration.Bind("AzureAd", options);
                                               // do something
                                           }
                                           )
                                           .AddInMemoryTokenCaches();
            */

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
            app.UseTokenAcquirerFactory();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
                endpoints.MapControllers();
            });
        }
    }
}
