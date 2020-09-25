// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Linq;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.Test.Common;
using Microsoft.Identity.Web.Test.LabInfrastructure;
using Microsoft.Identity.Web.TokenCacheProviders;

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
            KeyVaultSecretsProvider _keyVault = new KeyVaultSecretsProvider();
            services.AddDistributedMemoryCache();

            var builder = services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                                  .AddMicrosoftIdentityWebApi(Configuration)
                                  .EnableTokenAcquisitionToCallDownstreamApi()
                                    .AddDistributedTokenCaches()  
                                    //.AddInMemoryTokenCaches(memoryCacheOptions:options => { options.SizeLimit = (long)1e9; })
                                        .AddDownstreamWebApi(
                                            TestConstants.SectionNameCalledApi,
                                            Configuration.GetSection(TestConstants.SectionNameCalledApi))
                                        .AddMicrosoftGraph(Configuration.GetSection("GraphBeta"));

            // Replaces MsalMemoryTokenCacheProvider for benchmarking purposes
            ServiceDescriptor msalMemoryCacheService = services.FirstOrDefault(s => s.ServiceType == typeof(IMsalTokenCacheProvider));
            services.Remove(msalMemoryCacheService);
            services.AddSingleton<IMsalTokenCacheProvider, BenchmarkMsalMemoryTokenCacheProvider>();

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

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
