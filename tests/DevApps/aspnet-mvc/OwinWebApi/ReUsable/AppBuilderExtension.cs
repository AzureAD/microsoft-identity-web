using System;
using System.Collections.Generic;
using System.Configuration;
using System.Web;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.IdentityModel.Validators;
using Microsoft.Owin.Security.ActiveDirectory;
using Microsoft.Owin.Security.Jwt;
using Microsoft.Owin.Security.OAuth;
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
            Action<OAuthBearerAuthenticationOptions> updateOptions = null,
            string configurationSection = "AzureAd")
        {
            ReadConfiguration();

            // Add services
            ServiceCollection services = new ServiceCollection();

            // Configure the Microsoft identity options
            services.Configure<MicrosoftIdentityOptions>(string.Empty, configureMicrosoftIdentityOptions ?? (option =>
            {
                Configuration.GetSection(configurationSection).Bind(option);
                string x = option.ClientId;
            }));
            
            if (configureServices != null)
            {
                configureServices(services);
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
