// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

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
            services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
                    .AddMicrosoftIdentityWebApp(Configuration.GetSection("AzureAd"))
                        .EnableTokenAcquisitionToCallDownstreamApi()
                           .AddMicrosoftGraph(Configuration.GetSection("GraphBeta"))
                           .AddDownstreamWebApi("GraphBeta", Configuration.GetSection("GraphBeta"))
                           .AddInMemoryTokenCaches();

            // How to use a signed assertion instead of a secret or certificate
            services.Configure<MicrosoftIdentityOptions>(
                    o => {
                        o.ClientSecret = null;
                        o.ClientCertificates = null;
                        o.ClientAssertionDescription = new ClientAssertionDescription(GetSignedAssertionFromMsi);
                    }
               );

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

        /// <summary>
        /// Prototype of certificate-less authentication using a signed assertion
        /// acquired with MSI (federated identity)
        /// </summary>
        /// <returns></returns>
        private async Task<ClientAssertion> GetSignedAssertionFromMsi(CancellationToken cancellationToken)
        {
            string userAssignedClientId = "cd6f23a4-ba6f-42af-b72f-986d068d7212";
            var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions { ManagedIdentityClientId = userAssignedClientId });
            var result = await credential.GetTokenAsync(
                new TokenRequestContext(new[] { "api://AzureADTokenExchange/.default" }, null), 
                cancellationToken);
            return new ClientAssertion(result.Token, result.ExpiresOn);
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
