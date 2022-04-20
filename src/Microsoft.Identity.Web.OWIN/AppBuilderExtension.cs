using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Web.Hosts;
using Microsoft.IdentityModel.Tokens;
using Microsoft.IdentityModel.Validators;
using Microsoft.Owin.Security.Jwt;
using Microsoft.Owin.Security.OAuth;
using Owin;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Extension methods on an ASP.NET application to add a web API
    /// </summary>
    public static class AppBuilderExtension
    {
        /// <summary>
        /// Configuration
        /// </summary>
        static internal IConfiguration? Configuration { get; set; }

        /// <summary>
        /// Service Provider
        /// </summary>
        static internal IServiceProvider? ServiceProvider { get; set; }

        /// <summary>
        /// Adds a protected web API
        /// </summary>
        /// <param name="app">Application buidler</param>
        /// <param name="configureServices">Configure the services (if you want to call downstream web APIs).</param>
        /// <param name="configureMicrosoftIdentityOptions">Configure Microsoft identity options.</param>
        /// <param name="updateOptions">Update the OWIN options if you want to finess token validation.</param>
        /// <param name="configurationSection">Configuration section in which to read the options.</param>
        /// <returns>the app builder to chain</returns>
        public static IAppBuilder AddMicrosoftIdentityWebApi(
            this IAppBuilder app,
            Action<IServiceCollection>? configureServices = null,
            Action<MicrosoftIdentityOptions>? configureMicrosoftIdentityOptions = null,
            Action<OAuthBearerAuthenticationOptions>? updateOptions = null,
            string configurationSection = "AzureAd")
        {
            ReadConfiguration();

            // Add services
            ServiceCollection services = new ServiceCollection();

            // Configure the Microsoft identity options
            services.Configure(
                string.Empty, 
                configureMicrosoftIdentityOptions ?? (option =>
            {
                Configuration?.GetSection(configurationSection).Bind(option);
            }));
            
            if (configureServices != null)
            {
                configureServices(services);
            }

            // Replace the genenric host by an OWIN host
            ServiceDescriptor? tokenAcquisitionhost = services.FirstOrDefault(s => s.ServiceType == typeof(ITokenAcquisitionHost));
            if (tokenAcquisitionhost != null)
            {
                services.Remove(tokenAcquisitionhost);

                if (tokenAcquisitionhost.Lifetime == ServiceLifetime.Singleton)
                {
                    // The service was already added, but not with the right lifetime
                    services.AddSingleton<ITokenAcquisitionHost, OwinTokenAcquisitionHost>();     
                }
                else
                {
                    // The service is already added with the right lifetime
                    services.AddScoped<ITokenAcquisitionHost, OwinTokenAcquisitionHost>();
                }
            }

            ServiceProvider = services.BuildServiceProvider();
            string authority = Configuration.GetValue<string>("AzureAd:Instance") + Configuration.GetValue<string>("AzureAd:TenantId") + "/v2.0";
            TokenValidationParameters tokenValidationParameters = new TokenValidationParameters
            {
                ValidAudience = Configuration.GetValue<string>("AzureAd:Audience") ?? Configuration.GetValue<string>("AzureAd:ClientId"),
                IssuerValidator = AadIssuerValidator.GetAadIssuerValidator(authority).Validate,
                SaveSigninToken = true,
            };
            OAuthBearerAuthenticationOptions options = new OAuthBearerAuthenticationOptions
            {
                AccessTokenFormat = new JwtFormat(tokenValidationParameters, new OpenIdConnectCachingSecurityTokenProvider(authority+ "/.well-known/openid-configuration"))
            };
            if (updateOptions != null)
            {
                updateOptions(options);
            }

            return app.UseOAuthBearerAuthentication(options);
        }

        private static IConfiguration ReadConfiguration()
        {
            if (Configuration == null)
            {
                // Read the configuration from a file
                var builder = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
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
