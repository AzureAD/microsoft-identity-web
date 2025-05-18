// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
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
        /// Configuration. By default the configuration contains the content of the 
        /// appsettings.json file, provided this file is copied in the folder of the app.
        /// </summary>
        public IConfiguration Configuration { get; protected set; }

        /// <summary>
        /// Service Provider. The service provider is only available once the factory was built
        /// using <see cref="Build"/>.
        /// </summary>
        public IServiceProvider? ServiceProvider { get; protected internal set; }

        /// <summary>
        /// Services. Used in the initialization phase
        /// </summary>
        /// <exception cref="InvalidOperationException"/> will be thrown if you try to access
        /// Services after you called <see cref="Build"/>.
        public ServiceCollection Services
        {
            get
            {
                return _services;
            }

            private set 
            {
                _services = value;
            }
        }
        private ServiceCollection _services = new ServiceCollection();
#if NET9_0_OR_GREATER
        private static readonly Lock s_defaultInstanceLock = new();
        private readonly Lock _buildLock = new();
#else
        private static readonly object s_defaultInstanceLock = new();
        private readonly object _buildLock = new();
#endif

        /// <summary>
        /// Constructor
        /// </summary>
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        protected TokenAcquirerFactory()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {
        }

        /// <summary>
        /// Get the default instance of a token acquirer factory. Use this method, for instance, if you have an ASP.NET OWIN application
        /// and you want to get the default instance of the OwinTokenAcquirerFactory.
        /// </summary>
        /// /// <example>
        /// <format type="text/markdown">
        /// <![CDATA[
        ///  [!code-csharp[ConvertType](~/../tests/DevApps/aspnet-mvc/OwinWebApp/App_Start/Startup.Auth.cs?highlight=22)]
        /// ]]></format>
        /// </example>
#if NET6_0_OR_GREATER && !NET8_0_OR_GREATER
        [RequiresUnreferencedCode("Calls Microsoft.Extensions.Configuration.ConfigurationBinder.Bind(IConfiguration, Object).")]
#endif
        static public T GetDefaultInstance<T>(string configSection="AzureAd") where T : TokenAcquirerFactory, new()
        {
            T instance;
            if (defaultInstance == null)
            {
                lock (s_defaultInstanceLock)
                {
                    if (defaultInstance == null)
                    {
                        instance = new T();
                        instance.ReadConfiguration();
                        defaultInstance = instance;
                        instance.Services.AddTokenAcquisition();
                        instance.Services.AddHttpClient();
                        instance.Services.Configure<MicrosoftIdentityApplicationOptions>(option =>
                        {
                            instance.Configuration.GetSection(configSection).Bind(option);

                            // This is temporary and will be removed eventually.
                            CiamAuthorityHelper.BuildCiamAuthorityIfNeeded(option);
                        });
                        instance.Services.AddSingleton<ITokenAcquirerFactory, DefaultTokenAcquirerFactoryImplementation>();
                        instance.Services.AddSingleton(defaultInstance.Configuration);
                    }
                }
            }
            return (defaultInstance as T)!;
        }

        /// <summary>
        /// Get the default instance. Use this method to retrieve the instance, optionally add some services to 
        /// the service collection, and build the instance.
        /// </summary>
        /// <returns></returns>
        /// <example>
        /// <format type="text/markdown">
        /// <![CDATA[
        ///  [!code-csharp[ConvertType](~/../tests/DevApps/daemon-app/daemon-console-calling-msgraph/Program.cs?highlight=5)]
        /// ]]></format>
        /// </example>
#if NET6_0_OR_GREATER && !NET8_0_OR_GREATER
        [RequiresUnreferencedCode("Calls Microsoft.Extensions.Configuration.ConfigurationBinder.Bind(IConfiguration, Object).")]
#endif
        static public TokenAcquirerFactory GetDefaultInstance(string configSection = "AzureAd")
        {
            TokenAcquirerFactory instance;
            if (defaultInstance == null)
            {
                lock (s_defaultInstanceLock)
                {
                    if (defaultInstance == null)
                    {
                        instance = new TokenAcquirerFactory();
                        instance.ReadConfiguration();
                        defaultInstance = instance;
                        instance.Services.AddTokenAcquisition();
                        instance.Services.AddHttpClient();
                        instance.Services.Configure<MicrosoftIdentityApplicationOptions>(option =>
                        {
                            instance.Configuration.GetSection(configSection).Bind(option);

                            // This is temporary and will be removed eventually.
                            CiamAuthorityHelper.BuildCiamAuthorityIfNeeded(option);
                        });
                        instance.Services.AddSingleton<ITokenAcquirerFactory, DefaultTokenAcquirerFactoryImplementation>();
                        instance.Services.AddSingleton(defaultInstance.Configuration);
                    }
                }
            }
            return defaultInstance!;
        }

        /// <summary>
        /// Build the Token acquirer. This does the composition of all the services you have added to
        /// <see cref="Services"/>, and returns a service provider, which you can then use to get access
        /// to the services you have added.
        /// </summary>
        /// <returns></returns>
        /// <example>
        /// The following example shows how you add Microsoft GraphServiceClient to the services
        /// and use it after you've built the token acquirer factory. The authentication is handled
        /// transparently based on the information in the appsettings.json.
        /// <format type="text/markdown">
        /// <![CDATA[
        ///  [!code-csharp[ConvertType](~/../tests/DevApps/daemon-app/daemon-console-calling-msgraph/Program.cs?highlight=7)]
        /// ]]></format>
        /// </example>
        public IServiceProvider Build()
        {
            // Prevent from building twice.
            if (ServiceProvider != null)
            {
                throw new InvalidOperationException("You shouldn't call Build() twice");
            }

            lock(_buildLock)
            {
                if (ServiceProvider != null)
                {
                    throw new InvalidOperationException("You shouldn't call Build() twice");
                }

                // Additional processing before creating the service provider
                PreBuild();
                ServiceProvider = Services.BuildServiceProvider();
            }

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
        internal /* for unit tests */ static void ResetDefaultInstance()
        {
            if (defaultInstance?.ServiceProvider != null)
            {
                (defaultInstance.ServiceProvider as IDisposable)?.Dispose();
            }
            defaultInstance = null;
        }

        // Move to a derived class?

        private IConfiguration ReadConfiguration()
        {
            if (Configuration == null)
            {
                // Read the configuration from a file and augment/replace from environment variable
                var builder = new ConfigurationBuilder();
                string basePath = DefineConfiguration(builder);
                builder.SetBasePath(basePath)
                       .AddJsonFile("appsettings.json", optional: true)
                       .AddEnvironmentVariables();
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
            return Path.GetDirectoryName(AppContext.BaseDirectory)!;
        }

        ITokenAcquirerFactory implementation;

        /// <inheritdoc/>
        public ITokenAcquirer GetTokenAcquirer(string authority, string clientId, IEnumerable<CredentialDescription> clientCredentials, string? region = null)
        {
            implementation ??= ServiceProvider!.GetRequiredService<ITokenAcquirerFactory>();
            return implementation.GetTokenAcquirer(new MicrosoftIdentityApplicationOptions
            {
                Authority = authority,
                ClientId = clientId,
                ClientCredentials = clientCredentials,
                AzureRegion = region
            }) ;
        }

        /// <inheritdoc/>
        public ITokenAcquirer GetTokenAcquirer(IdentityApplicationOptions applicationIdentityOptions)
        {
            implementation ??= ServiceProvider!.GetRequiredService<ITokenAcquirerFactory>();
            return implementation.GetTokenAcquirer(applicationIdentityOptions);
        }

        /// <inheritdoc/>
        public ITokenAcquirer GetTokenAcquirer(string optionName = "")
        {
            implementation ??= ServiceProvider!.GetRequiredService<ITokenAcquirerFactory>();
            return implementation.GetTokenAcquirer(optionName);
        }
    }
}
