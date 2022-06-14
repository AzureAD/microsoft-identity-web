using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
        /// Services
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
            }
            return defaultInstance!;
        }

        /// <summary>
        /// Get the default instance.
        /// </summary>
        static internal TokenAcquirerFactory FromConfigurationAndServices(IConfiguration configuration, ServiceCollection services)
        {
            TokenAcquirerFactory factory = new TokenAcquirerFactory();
            factory.Services = services;
            factory.Configuration = configuration;

            if (defaultInstance == null)
            {
                defaultInstance = factory;
            }
            return defaultInstance;
        }

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

        IDictionary<string, ITokenAcquirer> tokenAcquirers = new Dictionary<string, ITokenAcquirer>();

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
            throw new NotImplementedException();
        }

        /// <summary>
        /// Get a token described by a specific authentication scheme/configuration
        /// </summary>
        /// <param name="authenticationScheme"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public ITokenAcquirer GetTokenAcquirer(string authenticationScheme)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Get a token acquirer from the application identity options
        /// </summary>
        /// <param name="applicationIdentityOptions"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public ITokenAcquirer GetTokenAcquirer(ApplicationIdentityOptions applicationIdentityOptions)
        {
            throw new NotImplementedException();
        }
    }
}
