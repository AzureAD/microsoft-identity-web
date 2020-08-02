// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Identity.Web.TokenCacheProviders.Session
{
    /// <summary>
    /// Extension class to add a session token cache serializer to MSAL.
    /// </summary>
    public static class SessionTokenCacheProviderExtension
    {
        /// <summary>
        /// Adds both application and per-user session token caches.
        /// </summary>
        /// <remarks>
        /// For this session cache to work effectively the ASP.NET Core session has to be configured properly.
        /// The latest guidance is provided at https://docs.microsoft.com/aspnet/core/fundamentals/app-state.
        ///
        /// In the method <c>public void ConfigureServices(IServiceCollection services)</c> in Startup.cs, add the following:
        /// <code>
        /// services.AddSession(option =>
        /// {
        ///     option.Cookie.IsEssential = true;
        /// });
        /// </code>
        /// In the method <c>public void Configure(IApplicationBuilder app, IHostingEnvironment env)</c> in Startup.cs, add the following:
        /// <code>
        /// app.UseSession(); // Before UseMvc()
        /// </code>
        /// Because session token caches are added with scoped lifetime, they should not be used when <c>TokenAcquisition</c> is also used as a singleton (for example, when using Microsoft Graph SDK).
        /// </remarks>
        /// <param name="services">The services collection to add to.</param>
        /// <returns>The service collection.</returns>
        public static IServiceCollection AddSessionTokenCaches(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            // Add session if you are planning to use session based token cache
            var sessionStoreService = services.FirstOrDefault(x => x.ServiceType.Name == "ISessionStore");

            // If not added already
            if (sessionStoreService == null)
            {
                services.AddSession(option =>
                {
                    option.Cookie.IsEssential = true;
                });
            }
            else
            {
                // If already added, ensure the options are set to use Cookies
                services.Configure<SessionOptions>(option =>
                {
                    option.Cookie.IsEssential = true;
                });
            }

            services.AddHttpContextAccessor();
            services.AddScoped<IMsalTokenCacheProvider, MsalSessionTokenCacheProvider>();
            services.TryAddScoped(provider =>
            {
                var httpContext = provider.GetRequiredService<IHttpContextAccessor>().HttpContext;
                if (httpContext == null)
                {
                    throw new InvalidOperationException(IDWebErrorMessage.HttpContextIsNull);
                }

                return httpContext.Session;
            });

            return services;
        }

        /// <summary>
        /// Adds both application and per-user session token caches.
        /// </summary>
        /// <remarks>
        /// For this session cache to work effectively the ASP.NET Core session has to be configured properly.
        /// The latest guidance is provided at https://docs.microsoft.com/aspnet/core/fundamentals/app-state.
        ///
        /// In the method <c>public void ConfigureServices(IServiceCollection services)</c> in Startup.cs, add the following:
        /// <code>
        /// services.AddSession(option =>
        /// {
        ///     option.Cookie.IsEssential = true;
        /// });
        /// </code>
        /// In the method <c>public void Configure(IApplicationBuilder app, IHostingEnvironment env)</c> in Startup.cs, add the following:
        /// <code>
        /// app.UseSession(); // Before UseMvc()
        /// </code>
        /// Because session token caches are added with scoped lifetime, they should not be used when <c>TokenAcquisition</c> is also used as a singleton (for example, when using Microsoft Graph SDK).
        /// </remarks>
        /// <param name="builder">The authentication builder to add the session token caches to.</param>
        /// <returns>The builder to chain more commands.</returns>
        public static AuthenticationBuilder AddSessionTokenCaches(this AuthenticationBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.Services.AddSessionTokenCaches();
            return builder;
        }

        /// <summary>
        /// Adds an HTTP session-based application token cache to the service collection.
        /// </summary>
        /// <remarks>
        /// For this session cache to work effectively the ASP.NET Core session has to be configured properly.
        /// The latest guidance is provided at https://docs.microsoft.com/aspnet/core/fundamentals/app-state.
        ///
        /// In the method <c>public void ConfigureServices(IServiceCollection services)</c> in Startup.cs, add the following:
        /// <code>
        /// services.AddSession(option =>
        /// {
        ///     option.Cookie.IsEssential = true;
        /// });
        /// </code>
        /// In the method <c>public void Configure(IApplicationBuilder app, IHostingEnvironment env)</c> in Startup.cs, add the following:
        /// <code>
        /// app.UseSession(); // Before UseMvc()
        /// </code>
        /// Because session token caches are added with scoped lifetime, they should not be used when <c>TokenAcquisition</c> is also used as a singleton (for example, when using Microsoft Graph SDK).
        /// </remarks>
        /// <param name="services">The services collection to add to.</param>
        /// <returns>The service collection.</returns>
        public static IServiceCollection AddSessionAppTokenCache(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddHttpContextAccessor();
            services.AddSession(option =>
            {
                option.Cookie.IsEssential = true;
            });
            services.AddScoped<IMsalTokenCacheProvider, MsalSessionTokenCacheProvider>();
            services.TryAddScoped(provider =>
            {
                var httpContext = provider.GetRequiredService<IHttpContextAccessor>().HttpContext;
                if (httpContext == null)
                {
                    throw new InvalidOperationException(IDWebErrorMessage.HttpContextIsNull);
                }

                return httpContext.Session;
            });

            return services;
        }

        /// <summary>
        /// Adds an HTTP session-based per-user token cache to the service collection.
        /// </summary>
        /// <remarks>
        /// For this session cache to work effectively the ASP.NET Core session has to be configured properly.
        /// The latest guidance is provided at https://docs.microsoft.com/aspnet/core/fundamentals/app-state.
        ///
        /// In the method <c>public void ConfigureServices(IServiceCollection services)</c> in Startup.cs, add the following:
        /// <code>
        /// services.AddSession(option =>
        /// {
        ///     option.Cookie.IsEssential = true;
        /// });
        /// </code>
        /// In the method <c>public void Configure(IApplicationBuilder app, IHostingEnvironment env)</c> in Startup.cs, add the following:
        /// <code>
        /// app.UseSession(); // Before UseMvc()
        /// </code>
        /// Because session token caches are added with scoped lifetime, they should not be used when <c>TokenAcquisition</c> is also used as a singleton (for example, when using Microsoft Graph SDK).
        /// </remarks>
        /// <param name="services">The services collection to add to.</param>
        /// <returns>The service collection.</returns>
        public static IServiceCollection AddSessionPerUserTokenCache(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddHttpContextAccessor();
            services.AddSession(option =>
            {
                option.Cookie.IsEssential = true;
            });
            services.AddScoped<IMsalTokenCacheProvider, MsalSessionTokenCacheProvider>();
            services.TryAddScoped(provider =>
            {
                var httpContext = provider.GetRequiredService<IHttpContextAccessor>().HttpContext;
                if (httpContext == null)
                {
                    throw new InvalidOperationException(IDWebErrorMessage.HttpContextIsNull);
                }

                return httpContext.Session;
            });

            return services;
        }
    }
}
