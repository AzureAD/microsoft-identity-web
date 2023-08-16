// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
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
#if DEBUG
        internal static ConcurrentDictionary<MethodInfo, IServiceProvider> s_serviceProviderFromAction
            = new ConcurrentDictionary<MethodInfo, IServiceProvider>();
#else
        private static readonly ConcurrentDictionary<MethodInfo, IServiceProvider> s_serviceProviderFromAction
            = new ConcurrentDictionary<MethodInfo, IServiceProvider>();
#endif

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
            _ = Throws.IfNull(confidentialClientApp);
            _ = Throws.IfNull(initializeCaches);

            // try to reuse existing XYZ cache if AddXYZCache was called before, to simulate ASP.NET Core
            var serviceProvider = s_serviceProviderFromAction.GetOrAdd(initializeCaches.Method, _ =>
            {
                lock (s_serviceProviderFromAction)
                {
                    ServiceCollection services = new ServiceCollection();
                    initializeCaches(services);
                    services.AddLogging();

                    return services.BuildServiceProvider();
                }
            });

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
            _ = Throws.IfNull(confidentialClientApp);

            confidentialClientApp.AddTokenCaches(services =>
            {
                services.AddInMemoryTokenCaches();
            });
            return confidentialClientApp;
        }

        /// <summary>
        /// Add an in-memory well partitioned token cache to MSAL.NET confidential client
        /// application. Don't use this method in ASP.NET Core: rather use:
        /// <code>services.AddInMemoryTokenCache()</code> in ConfigureServices.
        /// </summary>
        /// <param name="confidentialClientApp">Confidential client application.</param>
        /// <param name="initializeMemoryCache">Action taking a <see cref="IServiceCollection"/>
        /// and by which you initialize your memory cache.</param>
        /// <returns>The application for chaining.</returns>
        /// <example>
        ///
        /// The following code adds an in-memory token cache.
        ///
        /// <code>
        ///  app.AddInMemoryTokenCache(services =>
        ///  {
        ///       services.Configure&lt;MemoryCacheOptions&gt;(options =>
        ///       {
        ///           options.SizeLimit = 5000000; // in bytes (5 Mb), for example
        ///       });
        ///  });
        /// </code>
        ///
        /// </example>
        /// <remarks>Don't use this method in ASP.NET Core. Just add use the ConfigureServices method
        /// instead.</remarks>
        public static IConfidentialClientApplication AddInMemoryTokenCache(
            this IConfidentialClientApplication confidentialClientApp,
            Action<IServiceCollection> initializeMemoryCache)
        {
            _ = Throws.IfNull(confidentialClientApp);
            _ = Throws.IfNull(initializeMemoryCache);

            confidentialClientApp.AddTokenCaches(services =>
            {
                services.AddInMemoryTokenCaches();
                initializeMemoryCache(services);
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
            _ = Throws.IfNull(confidentialClientApp);
            _ = Throws.IfNull(initializeDistributedCache);

            confidentialClientApp.AddTokenCaches(services =>
            {
                services.AddDistributedTokenCaches();
                services.AddDataProtection();
                initializeDistributedCache(services);
            });
            return confidentialClientApp;
        }

        //internal static /*For testing only */ void ResetStaticCache()
        //{
        //    s_serviceProviderFromAction = new ConcurrentDictionary<MethodInfo, IServiceProvider>();
        //}
    }
}
