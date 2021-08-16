// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web.TokenCacheProviders;
using Microsoft.Identity.Web.TokenCacheProviders.Distributed;
using Microsoft.Identity.Web.TokenCacheProviders.InMemory;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Extension methods to expose a simplified developer experience for
    /// adding token caches to MSAL.NET confidential client applications
    /// in ASP.NET, or .NET Core, or .NET FW.
    /// </summary>
    public static class TokenCacheExtensions
    {
        private static IServiceProvider serviceProvider;

        /// <summary>
        /// Use a token cache and choose the serialization part by adding it to
        /// the services collection and configuring its options.
        /// </summary>
        /// <returns>The confidential client application.</returns>
        /// <param name="confidentialClientApp">Confidential client application.</param>
        /// <param name="initializeCaches">Action that you'll use to add a cache serialization
        /// to the service collection passed as an argument.</param>
        /// <returns>The application for chaining.</returns>
        /// <example>
        ///
        /// The following code adds a distributed in-memory token cache.
        ///
        /// <code>
        ///  app.AddTokenCaches(services =>
        ///  {
        ///      // In memory distributed token cache
        ///      // In net472, requires to reference Microsoft.Extensions.Caching.Memory
        ///      services.AddDistributedTokenCache();
        ///      services.AddDistributedMemoryCache();
        ///  });
        /// </code>
        ///
        /// The following code adds a token cache based on REDIS and initializes
        /// its configuration.
        ///
        /// <code>
        ///  app.AddTokenCaches(services =>
        ///  {
        ///       services.AddDistributedTokenCache();
        ///       // Redis token cache
        ///       // Requires to reference Microsoft.Extensions.Caching.StackExchangeRedis
        ///       services.AddStackExchangeRedisCache(options =>
        ///       {
        ///           options.Configuration = "localhost";
        ///           options.InstanceName = "Redis";
        ///       });
        ///  });
        /// </code>
        /// If using distributed token caches, use AddDistributedTokenCache.
        /// </example>
        /// <remarks>Don't use this method in ASP.NET Core. Just add use the ConfigureServices method
        /// instead.</remarks>
        internal static IConfidentialClientApplication AddTokenCaches(
            this IConfidentialClientApplication confidentialClientApp,
            Action<IServiceCollection> initializeCaches)
        {
            if (confidentialClientApp is null)
            {
                throw new ArgumentNullException(nameof(confidentialClientApp));
            }

            if (initializeCaches is null)
            {
                throw new ArgumentNullException(nameof(initializeCaches));
            }

            if (serviceProvider == null)
            {
                IHostBuilder hostBuilder = Host.CreateDefaultBuilder()
                    .ConfigureLogging(logger => { })
                    .ConfigureServices(services =>
                    {
                        initializeCaches(services);
                        services.AddDataProtection();
                    });

                serviceProvider = hostBuilder.Build().Services;
            }

            IMsalTokenCacheProvider msalTokenCacheProvider = serviceProvider.GetRequiredService<IMsalTokenCacheProvider>();
            msalTokenCacheProvider.Initialize(confidentialClientApp.UserTokenCache);
            msalTokenCacheProvider.Initialize(confidentialClientApp.AppTokenCache);
            return confidentialClientApp;
        }

        /// <summary>
        /// Add an in-memory well partitioned token cache to MSAL.NET confidential client
        /// application. Don't use this method in ASP.NET Core: rather use:
        /// <code>services.AddInMemoryTokenCache()</code> in ConfigureServices.
        /// </summary>
        /// <param name="confidentialClientApp">Confidential client application.</param>
        /// <returns>The application for chaining.</returns>
        /// <example>
        ///
        /// The following code adds an in-memory token cache.
        ///
        /// <code>
        ///  app.AddInMemoryTokenCache();
        /// </code>
        ///
        /// </example>
        /// <remarks>Don't use this method in ASP.NET Core. Just add use the ConfigureServices method
        /// instead.</remarks>
        public static IConfidentialClientApplication AddInMemoryTokenCache(
            this IConfidentialClientApplication confidentialClientApp)
        {
            if (confidentialClientApp is null)
            {
                throw new ArgumentNullException(nameof(confidentialClientApp));
            }

            confidentialClientApp.AddTokenCaches(services =>
            {
                services.AddInMemoryTokenCaches();
            });
            return confidentialClientApp;
        }

        /// <summary>
        /// Add a distributed token cache.
        /// </summary>
        /// <param name="confidentialClientApp">Confidential client application.</param>
        /// <param name="initializeDistributedCache">Action taking a <see cref="IServiceCollection"/>
        /// and by which you initialize your distributed cache.</param>
        /// <returns>The application for chaining.</returns>
        /// <example>
        /// The following code adds a token cache based on REDIS and initializes
        /// its configuration.
        ///
        /// <code>
        ///  app.AddDistributedTokenCache(services =>
        ///  {
        ///       // Redis token cache
        ///       // Requires to reference Microsoft.Extensions.Caching.StackExchangeRedis
        ///       services.AddStackExchangeRedisCache(options =>
        ///       {
        ///           options.Configuration = "localhost";
        ///           options.InstanceName = "Redis";
        ///       });
        ///  });
        /// </code>
        ///
        /// </example>
        /// <remarks>Don't use this method in ASP.NET Core. Just add use the ConfigureServices method
        /// instead.</remarks>
        public static IConfidentialClientApplication AddDistributedTokenCache(
            this IConfidentialClientApplication confidentialClientApp,
            Action<IServiceCollection> initializeDistributedCache)
        {
            if (confidentialClientApp is null)
            {
                throw new ArgumentNullException(nameof(confidentialClientApp));
            }

            if (initializeDistributedCache is null)
            {
                throw new ArgumentNullException(nameof(initializeDistributedCache));
            }

            confidentialClientApp.AddTokenCaches(services =>
            {
                services.AddDistributedTokenCaches();
                initializeDistributedCache(services);
            });
            return confidentialClientApp;
        }
    }
}
