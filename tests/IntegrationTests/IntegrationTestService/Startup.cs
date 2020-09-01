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
using Microsoft.Identity.Web.Test.LabInfrastructure;

namespace IntegrationTestService
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            KeyVaultSecretsProvider keyVault = new KeyVaultSecretsProvider();
            KeyVaultSecret = keyVault.GetSecret(TestConstants.OBOClientKeyVaultUri).Value;
        }

        public IConfiguration Configuration { get; }
        private string KeyVaultSecret { get;  }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {            
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                    .AddMicrosoftIdentityWebApi(
                       options =>
                        {
                        },
                       options =>
                        {
                            options.ClientId = "f4aa5217-e87c-42b2-82af-5624dd14ee72"; //TestConstants.ConfidentialClientId;
                            options.TenantId = "common"; //TestConstants.ConfidentialClientLabTenant;
                            options.Instance = TestConstants.AadInstance;
                            options.ClientSecret = KeyVaultSecret;
                        })
                        .EnableTokenAcquisitionToCallDownstreamApi(options =>
                        {
                            options.ClientId = "f4aa5217-e87c-42b2-82af-5624dd14ee72"; //TestConstants.ConfidentialClientId;
                            options.TenantId = "common"; //TestConstants.ConfidentialClientLabTenant;
                            options.Instance = TestConstants.AadInstance;
                            options.ClientSecret = KeyVaultSecret;
                        })                       
                        .AddInMemoryTokenCaches();
            services.AddRazorPages(options =>
            {
                options.Conventions.AuthorizePage("/SecurePage");
            });
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

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
