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
        /// <summary>
        /// Use a token cache and choose the serialization part by adding it to
        /// the services collection and configuring its options.
        /// </summary>
        /// <returns>The confidential client application.</returns>
        /// <param name="app">MSAL.NET cca object.</param>
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
        ///      services.AddDistributedTokenCaches();
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
        ///       services.AddDistributedTokenCaches();
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
        public static IConfidentialClientApplication AddTokenCaches(
            this IConfidentialClientApplication app,
            Action<IServiceCollection> initializeCaches)
        {
            if (app is null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            if (initializeCaches is null)
            {
                throw new ArgumentNullException(nameof(initializeCaches));
            }

            IHostBuilder hostBuilder = Host.CreateDefaultBuilder()
                .ConfigureLogging(logger => { })
                .ConfigureServices(services =>
                 {
                     initializeCaches(services);
                 });

            IServiceProvider serviceProvider = hostBuilder.Build().Services;
            IMsalTokenCacheProvider msalTokenCacheProvider = serviceProvider.GetRequiredService<IMsalTokenCacheProvider>();
            msalTokenCacheProvider.Initialize(app.UserTokenCache);
            msalTokenCacheProvider.Initialize(app.AppTokenCache);
            return app;
        }

        /// <summary>
        /// Add an in-memory well partitioned token cache to MSAL.NET confidential client
        /// application. Don't use this method in ASP.NET Core: rather use:
        /// <code>services.AddInMemoryTokenCaches()</code> in ConfigureServices.
        /// In net462 and net472, you'll need to reference Microsoft.Extensions.Caching.Memory.
        /// </summary>
        /// <param name="app">Application.</param>
        /// <returns>The application for chaining.</returns>
        /// <example>
        ///
        /// The following code adds an in-memory token cache.
        ///
        /// <code>
        ///  app.AddInMemoryTokenCaches();
        /// </code>
        ///
        /// </example>
        /// <remarks>Don't use this method in ASP.NET Core. Just add use the ConfigureServices method
        /// instead.</remarks>
        public static IConfidentialClientApplication AddInMemoryTokenCaches(
            this IConfidentialClientApplication app)
        {
            if (app is null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            // In memory token cache
            // In net472, requires to reference Microsoft.Extensions.Caching.Memory
            app.AddTokenCaches(services =>
            {
                services.AddInMemoryTokenCaches();
            });
            return app;
        }

        /// <summary>
        /// Add a distributed token cache.
        /// </summary>
        /// <param name="app">Application.</param>
        /// <param name="initializeDistributedCache">Action taking a <see cref="IServiceCollection"/>
        /// and by which you initialize your distributed cache.</param>
        /// <returns>The application for chaining.</returns>
        /// <example>
        /// The following code adds a token cache based on REDIS and initializes
        /// its configuration.
        ///
        /// <code>
        ///  app.AddDistributedTokenCaches(services =>
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
        public static IConfidentialClientApplication AddDistributedTokenCaches(
            this IConfidentialClientApplication app,
            Action<IServiceCollection> initializeDistributedCache)
        {
            if (app is null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            if (initializeDistributedCache is null)
            {
                throw new ArgumentNullException(nameof(initializeDistributedCache));
            }

            app.AddTokenCaches(services =>
            {
                services.AddDistributedTokenCaches();
                initializeDistributedCache(services);
            });
            return app;
        }
    }
}
