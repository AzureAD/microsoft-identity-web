// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Microsoft.Identity.Web.TokenCacheProviders.Session
{
    /// <summary>
    /// Extension class to add a session token cache serializer to MSAL.
    /// </summary>
#if NET8_0_OR_GREATER
    [RequiresUnreferencedCode("Calls Microsoft.Extensions.Configuration.ConfigurationBinder.Bind(IConfiguration, Object)")]
    [RequiresDynamicCode("Calls Microsoft.Extensions.Configuration.ConfigurationBinder.Bind(IConfiguration, Object)")]
#endif
    public static class SessionTokenCacheProviderExtension
    {
        /// <summary>
        /// Adds an HTTP session-based application token cache to the service collection.
        /// </summary>
        /// <remarks>
        /// For this session cache to work effectively the ASP.NET Core session has to be configured properly.
        /// The latest guidance is provided at https://learn.microsoft.com/aspnet/core/fundamentals/app-state.
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
            return CreateSessionTokenCache(services);
        }

        /// <summary>
        /// Adds an HTTP session-based per-user token cache to the service collection.
        /// </summary>
        /// <remarks>
        /// For this session cache to work effectively the ASP.NET Core session has to be configured properly.
        /// The latest guidance is provided at https://learn.microsoft.com/aspnet/core/fundamentals/app-state.
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
            return CreateSessionTokenCache(services);
        }

        private static IServiceCollection CreateSessionTokenCache(IServiceCollection services)
        {
            _ = Throws.IfNull(services);

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
