// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

//#define UseRedisCache
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Web;

using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web.UI;
using Microsoft.Identity.Web.TokenCacheProviders.Distributed;

namespace WebApp_OpenIDConnect_DotNet
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
            services.AddDistributedMemoryCache();

            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.Unspecified;
                // Handling SameSite cookie according to https://learn.microsoft.com/aspnet/core/security/samesite
                options.HandleSameSiteCookieCompatibility();
            });

            services.AddOptions();

#if UseRedisCache
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = Configuration.GetConnectionString("Redis");
                options.InstanceName = "RedisDemos_"; //should be unique to the app
            });
            services.Configure<MsalDistributedTokenCacheAdapterOptions>(options => 
            {
                //options.DisableL1Cache = true;
                options.OnL2CacheFailure = (ex) =>
                {
                    if (ex is StackExchange.Redis.RedisConnectionException)
                    {
                        // action: try to reconnect or something
                        return true; //try to do the cache operation again
                    }
                    return false;
                };
            });
#endif

            services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
              .AddMicrosoftIdentityWebApp(Configuration)
                 .EnableTokenAcquisitionToCallDownstreamApi()

#if UseRedisCache
                     .AddDistributedTokenCaches();
#else
                     .AddInMemoryTokenCaches();
#endif
            //services.Configure<MicrosoftIdentityOptions>(
            //                                 o => o.ClientSecret = "");

            services.AddDownstreamApis(Configuration.GetSection("DownstreamApis"));

            services.AddControllersWithViews(options =>
            {
                var policy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
                options.Filters.Add(new AuthorizeFilter(policy));
            }).AddMicrosoftIdentityUI();

            services.AddRazorPages();
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
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseCookiePolicy();

            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
                endpoints.MapRazorPages();
            });
        }
    }
}
