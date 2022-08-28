// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Factory of a token acquirer.
    /// </summary>
    public class TokenAcquirerFactory : ITokenAcquirerFactory
    {
        /// <summary>
        /// Configuration
        /// </summary>
        public IConfiguration Configuration { get; protected set; }

        /// <summary>
        /// Service Provider
        /// </summary>
        public IServiceProvider? ServiceProvider { get; protected set; }

        /// <summary>
        /// Services. Used in the initialization phase.
        /// </summary>
        public ServiceCollection Services { get; protected set; } = new ServiceCollection();

        /// <summary>
        /// Constructor
        /// </summary>
        protected TokenAcquirerFactory()
        {
            Configuration = null!;
        }

        /// <summary>
        /// Get the default instance.
        /// </summary>
        static public T GetDefaultInstance<T>() where T : TokenAcquirerFactory, new()
        {
            T instance;
            if (defaultInstance == null)
            {
                instance = new T();
                instance.ReadConfiguration();
                defaultInstance = instance;
                instance.Services.AddTokenAcquisition();
                instance.Services.AddHttpClient();
            }
            return (defaultInstance as T)!;
        }


        /// <summary>
        /// Get the default instance
        /// </summary>
        /// <returns></returns>
        static public TokenAcquirerFactory GetDefaultInstance()
        {
            TokenAcquirerFactory instance;
            if (defaultInstance == null)
            {
                instance = new TokenAcquirerFactory();
                instance.ReadConfiguration();
                defaultInstance = instance;
                instance.Services.AddTokenAcquisition();
                instance.Services.AddHttpClient();
                instance.Services.AddOptions<MicrosoftAuthenticationOptions>(string.Empty);
            }
            return defaultInstance!;
        }

        /// <summary>
        /// Get the default instance.
        /// </summary>
        //static internal TokenAcquirerFactory FromConfigurationAndServices(IConfiguration configuration, ServiceCollection services)
        //{
        //    TokenAcquirerFactory factory = new TokenAcquirerFactory();
        //    factory.Services = services;
        //    factory.Configuration = configuration;
        //    factory.Services.AddTokenAcquisition();

        //    if (defaultInstance == null)
        //    {
        //        defaultInstance = factory;
        //    }
        //    return defaultInstance;
        //}

        /// <summary>
        /// Build the Token acquirer
        /// </summary>
        /// <returns></returns>
        public IServiceProvider Build()
        {
            ServiceProvider = Services.BuildServiceProvider();
            return ServiceProvider;
        }

        /// <summary>
        /// Default instance
        /// </summary>
        private static TokenAcquirerFactory? defaultInstance { get; set; }

        // Move to a derived class?

        private IConfiguration ReadConfiguration()
        {
            if (Configuration == null)
            {
                // Read the configuration from a file
                var builder = new ConfigurationBuilder();
                string basePath = DefineConfiguration(builder);
                builder.SetBasePath(basePath)
                       .AddJsonFile("appsettings.json", optional: true);
                Configuration = builder.Build();
            }
            return Configuration;
        }

        /// <summary>
        /// Adding additional configuration and returns the base path for configuration
        /// files
        /// </summary>
        /// <param name="builder"></param>
        /// <returns>Returns the base path for configuration files</returns>
        protected virtual string DefineConfiguration(IConfigurationBuilder builder)
        {
            return Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location)!;
        }

        IDictionary<string, ITokenAcquirer> authSchemes = new Dictionary<string, ITokenAcquirer>();

        /// <summary>
        /// Get a token acquirer for  a given authority, region, clientId, certificate?
        /// </summary>
        /// <param name="authority"></param>
        /// <param name="region"></param>
        /// <param name="clientId"></param>
        /// <param name="certificate"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public ITokenAcquirer GetTokenAcquirer(string authority, string region, string clientId, CredentialDescription certificate)
        {
            // Compute the key
            string key = GetKey(authority, clientId);
            if (!authSchemes.TryGetValue(key, out ITokenAcquirer tokenAcquirer))
            {
                MicrosoftAuthenticationOptions microsoftAuthenticationOptions = new MicrosoftAuthenticationOptions()
                {
                    ClientId = clientId,
                    Authority = authority,
                    ClientCredentials = new CredentialDescription[] { certificate },
                    SendX5C = true
                };
                if (region != null)
                {
                    microsoftAuthenticationOptions.AzureRegion = region;
                }

                var optionsMonitor = ServiceProvider.GetRequiredService<IOptionsMonitor<MergedOptions>>();
                var mergedOptions = optionsMonitor.Get(key);
                MergedOptions.UpdateMergedOptionsFromMicrosoftAuthenticationOptions(microsoftAuthenticationOptions, mergedOptions);
                tokenAcquirer = GetTokenAcquirer(key);
            }
            return tokenAcquirer;
        }

        /// <summary>
        /// Get a token acquirer from the application authentication options.
        /// </summary>
        /// <param name="applicationAuthenticationOptions">The authentication options describing the service.</param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public ITokenAcquirer GetTokenAcquirer(AuthenticationOptions applicationAuthenticationOptions)
        {
            if (applicationAuthenticationOptions is null)
            {
                throw new ArgumentNullException(nameof(applicationAuthenticationOptions));
            }

            // Compute the Azure region if the option is a MicrosoftAuthenticationOptions.
            MicrosoftAuthenticationOptions? microsoftAuthenticationOptions = applicationAuthenticationOptions as MicrosoftAuthenticationOptions;
            if (microsoftAuthenticationOptions == null)
            {
                microsoftAuthenticationOptions = new MicrosoftAuthenticationOptions
                {
                    AllowWebApiToBeAuthorizedByACL = applicationAuthenticationOptions.AllowWebApiToBeAuthorizedByACL,
                    Audience = applicationAuthenticationOptions.Audience,
                    Audiences = applicationAuthenticationOptions.Audiences,
                    Authority = applicationAuthenticationOptions.Authority,
                    ClientCredentials = applicationAuthenticationOptions.ClientCredentials,
                    ClientId = applicationAuthenticationOptions.ClientId,
                    SendX5C = applicationAuthenticationOptions.SendX5C,
                    TokenDecryptionCredentials = applicationAuthenticationOptions.TokenDecryptionCredentials,
                    EnablePiiLogging = applicationAuthenticationOptions.EnablePiiLogging,
                };
            }

            // Compute the key
            string key = GetKey(applicationAuthenticationOptions.Authority, applicationAuthenticationOptions.ClientId);
            if (!authSchemes.TryGetValue(key, out ITokenAcquirer tokenAcquirer))
            {
                var optionsMonitor = ServiceProvider.GetRequiredService<IOptionsMonitor<MergedOptions>>();
                var mergedOptions = optionsMonitor.Get(key);
                MergedOptions.UpdateMergedOptionsFromMicrosoftAuthenticationOptions(microsoftAuthenticationOptions, mergedOptions);
                tokenAcquirer = GetTokenAcquirer(key);
            }
            return tokenAcquirer;
        }

        /// <inheritdoc>
        public ITokenAcquirer GetTokenAcquirer(string authenticationScheme)
        {
            if (!authSchemes.TryGetValue(authenticationScheme, out ITokenAcquirer acquirer))
            {
                var tokenAcquisition = ServiceProvider.GetRequiredService<ITokenAcquisition>();
                acquirer = new TokenAcquirer(tokenAcquisition, authenticationScheme);
                authSchemes.Add(authenticationScheme, acquirer);
            }
            return acquirer;
        }

        private static string GetKey(string? authority, string? clientId)
        {
            return $"{authority}{clientId}";
        }
    }
}
