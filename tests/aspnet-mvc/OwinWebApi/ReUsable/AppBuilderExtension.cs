using System;
using System.Collections.Generic;
using System.Configuration;
using System.Web;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Owin.Security.ActiveDirectory;
using Owin;

namespace Microsoft.Identity.Web
{
    public static class AppBuilderExtension
    {
        static internal IConfiguration Configuration { get; set; }

        static internal IServiceProvider ServiceProvider { get; set; }

        public static IAppBuilder AddMicrosoftIdentityWebApi(
            this IAppBuilder app, 
            Action<MicrosoftIdentityOptions> configureMicrosoftIdentityOptions = null,
            Action<IServiceCollection> configureServices = null,
            Action<WindowsAzureActiveDirectoryBearerAuthenticationOptions> updateOptions = null)
        {
            ReadConfiguration();

            // Add services
            ServiceCollection services = new ServiceCollection();

            // Configure the Microsoft identity options
            services.Configure(configureMicrosoftIdentityOptions ?? (option => Configuration.Bind(option)));
            if (configureServices != null)
            {
                configureServices(services);
            }

            ServiceProvider serviceProvider = services.BuildServiceProvider();

            WindowsAzureActiveDirectoryBearerAuthenticationOptions options = new WindowsAzureActiveDirectoryBearerAuthenticationOptions
            {
                Tenant = Configuration.GetValue<string>("AzureAd:TenantId"),
                TokenValidationParameters = new TokenValidationParameters
                {
                    ValidAudience = Configuration.GetValue<string>("AzureAd:Audience"),
                    SaveSigninToken = true,
                },
            };
            if (updateOptions != null)
            {
                updateOptions(options);
            }

            return app.UseWindowsAzureActiveDirectoryBearerAuthentication(options);
        }

        private static IConfiguration ReadConfiguration()
        {
            if (Configuration == null)
            {
                // Read the configuration from a file
                var builder = new ConfigurationBuilder()
                 .AddInMemoryCollection(new Dictionary<string, string>()
                 {
                     ["AzureAd:Instance"] = ConfigurationManager.AppSettings["ida:Instance"] ?? "https://login.microsoftonline.com/",
                     ["AzureAd:ClientId"] = ConfigurationManager.AppSettings["ida:ClientId"],
                     ["AzureAd:TenantId"] = ConfigurationManager.AppSettings["ida:Tenant"],
                     ["AzureAd:Audience"] = ConfigurationManager.AppSettings["ida:Audience"],
                 })
                 .SetBasePath(HttpContext.Current.Request.PhysicalApplicationPath)
                 .AddJsonFile("appsettings.json", optional: true);
                Configuration = builder.Build();
            }
            return Configuration;
        }
    }
}
