// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.ApplicationInsights.Extensibility.EventCounterCollector;
using Microsoft.Identity.Web.Test.Common;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;
using PerformanceTestService.EventSource;

namespace PerformanceTestService
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
            var builder = services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                      .AddMicrosoftIdentityWebApi(Configuration)
                      .EnableTokenAcquisitionToCallDownstreamApi()
                            .AddDownstreamWebApi(
                                TestConstants.SectionNameCalledApi,
                                Configuration.GetSection(TestConstants.SectionNameCalledApi))
                            .AddMicrosoftGraph(Configuration.GetSection("GraphBeta"));

            // Replace existing cache provider with benchmark one
            // services.AddBenchmarkInMemoryTokenCaches();
            services.AddBenchmarkDistributedTokenCaches();

            // Add custom event counters to the App Insights collection
            services.ConfigureTelemetryModule<EventCounterCollectionModule>((module, o) =>
            {
                module.Counters.Add(new EventCounterCollectionRequest(MemoryCacheEventSource.EventSourceName, MemoryCacheEventSource.CacheItemCounterName));
                module.Counters.Add(new EventCounterCollectionRequest(MemoryCacheEventSource.EventSourceName, MemoryCacheEventSource.CacheWriteCounterName));
                module.Counters.Add(new EventCounterCollectionRequest(MemoryCacheEventSource.EventSourceName, MemoryCacheEventSource.CacheReadCounterName));
                module.Counters.Add(new EventCounterCollectionRequest(MemoryCacheEventSource.EventSourceName, MemoryCacheEventSource.CacheRemoveCounterName));
                module.Counters.Add(new EventCounterCollectionRequest("Microsoft.AspNetCore.Hosting", "requests-per-second"));
                module.Counters.Add(new EventCounterCollectionRequest("Microsoft.AspNetCore.Hosting", "total-requests"));
                module.Counters.Add(new EventCounterCollectionRequest("Microsoft.AspNetCore.Hosting", "current-requests"));
                module.Counters.Add(new EventCounterCollectionRequest("Microsoft.AspNetCore.Hosting", "failed-requests"));
            }
            );

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

            app.UseEndpoints(endpoints => {
                endpoints.MapControllers();
            });
        }
    }
}
