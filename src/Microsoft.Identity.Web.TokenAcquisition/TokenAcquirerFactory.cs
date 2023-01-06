// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web.TokenCacheProviders;
using Microsoft.Identity.Web.TokenCacheProviders.InMemory;

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
        public IServiceProvider? ServiceProvider { get; protected internal set; }

        /// <summary>
        /// Services. Used in the initialization phase.
        /// </summary>
        /// <exception cref="InvalidOperationException"/> will be thrown if you try to access
        /// Services after you called <see cref="Build"/>.
        public ServiceCollection Services
        {
            get
            {
                if (ServiceProvider != null)
                {
                    throw new InvalidOperationException("Cannot change services once you called Build()");
                }
                return _services;
            }

            private set 
            {
                _services = value;
            }
        }
        private ServiceCollection _services = new ServiceCollection();

        /// <summary>
        /// Constructor
        /// </summary>
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        protected TokenAcquirerFactory()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {
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
                instance.Services.AddSingleton<ITokenAcquirerFactory>(defaultInstance);
                instance.Services.AddSingleton(defaultInstance.Configuration);
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
                instance.Services.AddOptions<MicrosoftIdentityApplicationOptions>(string.Empty);
                instance.Services.Configure<MicrosoftIdentityApplicationOptions>(option => instance.Configuration.Bind(option));
                instance.Services.AddSingleton<ITokenAcquirerFactory>(defaultInstance);
                instance.Services.AddSingleton(defaultInstance.Configuration);
            }
            return defaultInstance!;
        }

        /// <summary>
        /// Build the Token acquirer
        /// </summary>
        /// <returns></returns>
        public IServiceProvider Build()
        {
            // Prevent from building twice.
            if (ServiceProvider != null)
            {
                throw new InvalidOperationException("You shouldn't call Build() twice");
            }

            // Additional processing before creating the service provider
            PreBuild();
            ServiceProvider = Services.BuildServiceProvider();
            return ServiceProvider;
        }

        /// <summary>
        /// Additional processing before the Build (adds an in-memory token cache if no cache was added)
        /// </summary>
        protected virtual void PreBuild()
        {
            ServiceDescriptor? tokenAcquisitionServiceDescription = Services.FirstOrDefault(s => s.ServiceType == typeof(ITokenAcquisition));
            ServiceDescriptor? msalTokenCacheProviderServiceDescription = Services.FirstOrDefault(s => s.ServiceType == typeof(IMsalTokenCacheProvider));
            if (tokenAcquisitionServiceDescription!=null && msalTokenCacheProviderServiceDescription==null)
            {
                Services.AddInMemoryTokenCaches();
            }
        }

        /// <summary>
        /// Default instance
        /// </summary>
        private static TokenAcquirerFactory? defaultInstance { get; set; }

        /// <summary>
        /// Resets the default instance. Useful for tests as token acquirer factory is a singleton
        /// in most configurations (except ASP.NET Core)
        /// </summary>
        internal /* for unit tests */ static void ResetDefaultInstance() { defaultInstance = null; }

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
            Assembly assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
            return Path.GetDirectoryName(assembly!.Location)!;
        }

        TokenAcquirerFactory_GetTokenAcquirers implementation;

        /// <inheritdoc/>
        public ITokenAcquirer GetTokenAcquirer(string authority, string clientId, IEnumerable<CredentialDescription> clientCredentials, string? region = null)
        {
            implementation ??= new TokenAcquirerFactory_GetTokenAcquirers(ServiceProvider!);
            return implementation.GetTokenAcquirer(authority, clientId, clientCredentials, region);
        }

        /// <inheritdoc/>
        public ITokenAcquirer GetTokenAcquirer(IdentityApplicationOptions applicationIdentityOptions)
        {
            implementation ??= new TokenAcquirerFactory_GetTokenAcquirers(ServiceProvider!);
            return implementation.GetTokenAcquirer(applicationIdentityOptions);
        }

        /// <inheritdoc/>
        public ITokenAcquirer GetTokenAcquirer(string optionName = "")
        {
            implementation ??= new TokenAcquirerFactory_GetTokenAcquirers(ServiceProvider!);
            return implementation.GetTokenAcquirer(optionName);
        }
    }
}
