// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
#define USE_SIGNED_ASSERTION
using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core;
using Azure.Identity;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web;
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

            services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
                    .AddMicrosoftIdentityWebApp(Configuration.GetSection(configSection))
                        .EnableTokenAcquisitionToCallDownstreamApi()
                           .AddMicrosoftGraph(Configuration.GetSection("GraphBeta"))
                           .AddDownstreamWebApi("GraphBeta", Configuration.GetSection("GraphBeta"))
                           .AddInMemoryTokenCaches();

#if USE_SIGNED_ASSERTION
            string userAssignedManagedIdentityClientId = Configuration.GetValue<string>("AzureAdCertless:UserAssignedManagedIdentityClientId");
            // How to use a signed assertion instead of a secret or certificate
            services.Configure<MicrosoftIdentityOptions>(
                    o => {
                        o.ClientSecret = null;
                        o.ClientCertificates = null;
                        o.ClientAssertionDescription = new MsiSignedAssertionProvider(userAssignedManagedIdentityClientId);
                    }
               );
#endif

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

#if USE_SIGNED_ASSERTION
        // Do we want to provide this class as product code?
        // Or change default certificate loader so that it provides it?
        internal class MsiSignedAssertionProvider : ClientAssertionDescription
        {
            public MsiSignedAssertionProvider(string userAssignedManagedIdentityClientId) : base(null)
            {
                this.userAssignedManagedIdentityClientId = userAssignedManagedIdentityClientId;
                ClientAssertionProvider = GetSignedAssertionFromMsi;
            }

            private string userAssignedManagedIdentityClientId;

            /// <summary>
            /// Prototype of certificate-less authentication using a signed assertion
            /// acquired with MSI (federated identity)
            /// </summary>
            /// <returns></returns>
            private async Task<ClientAssertion> GetSignedAssertionFromMsi(CancellationToken cancellationToken)
            {
                var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions { ManagedIdentityClientId = userAssignedManagedIdentityClientId });
                var result = await credential.GetTokenAsync(
                    new TokenRequestContext(new[] { "api://AzureADTokenExchange/.default" }, null),
                    cancellationToken);
                return new ClientAssertion(result.Token, result.ExpiresOn);
            }
        }
#endif

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
