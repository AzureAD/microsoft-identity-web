// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
#if !NET472 && !NET462
using Microsoft.AspNetCore.Authentication.JwtBearer;
#endif
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web.Hosts;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Extensions for IServiceCollection for startup initialization of web APIs.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Add the token acquisition service.
        /// </summary>
        /// <param name="services">Service collection.</param>
        /// <param name="isTokenAcquisitionSingleton">Specifies if an instance of <see cref="ITokenAcquisition"/> should be a singleton.</param>
        /// <returns>The service collection.</returns>
        /// <example>
        /// This method is typically called from the <c>ConfigureServices(IServiceCollection services)</c> in Startup.cs.
        /// Note that the implementation of the token cache can be chosen separately.
        ///
        /// <code>
        /// // Token acquisition service and its cache implementation as a session cache
        /// services.AddTokenAcquisition()
        /// .AddDistributedMemoryCache()
        /// .AddSession()
        /// .AddSessionBasedTokenCache();
        /// </code>
        /// </example>
        public static IServiceCollection AddTokenAcquisition(
            this IServiceCollection services,
            bool isTokenAcquisitionSingleton = false)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            ServiceDescriptor? tokenAcquisitionService = services.FirstOrDefault(s => s.ServiceType == typeof(ITokenAcquisition));
            if (tokenAcquisitionService == null)
            {
                services.AddSingleton<IPostConfigureOptions<MicrosoftAuthenticationOptions>, MicrosoftAuthenticationOptionsMerger>();
                services.AddSingleton<IPostConfigureOptions<MicrosoftIdentityOptions>, MicrosoftIdentityOptionsMerger>();
                services.AddSingleton<IPostConfigureOptions<ConfidentialClientApplicationOptions>, ConfidentialClientApplicationOptionsMerger>();
#if !NET472 && !NET462
                services.AddSingleton<IPostConfigureOptions<JwtBearerOptions>, JwtBearerOptionsMerger>();
#endif 
            }

            ServiceDescriptor? tokenAcquisitionInternalService = services.FirstOrDefault(s => s.ServiceType == typeof(ITokenAcquisitionInternal));
            ServiceDescriptor? tokenAcquisitionhost = services.FirstOrDefault(s => s.ServiceType == typeof(ITokenAcquisitionHost));
            if (tokenAcquisitionService != null && tokenAcquisitionInternalService != null && tokenAcquisitionhost != null)
            {
                if (isTokenAcquisitionSingleton ^ (tokenAcquisitionService.Lifetime == ServiceLifetime.Singleton))
                {
                    // The service was already added, but not with the right lifetime
                    services.Remove(tokenAcquisitionService);
                    services.Remove(tokenAcquisitionInternalService);
                    services.Remove(tokenAcquisitionhost);
                }
                else
                {
                    // The service is already added with the right lifetime
                    return services;
                }
            }

            // Token acquisition service
            if (isTokenAcquisitionSingleton)
            {
#if !NET472 && !NET462
                // ASP.NET Core
                services.AddHttpContextAccessor();
                services.AddSingleton<ITokenAcquisition, TokenAcquisitionAspNetCore>();
                services.AddSingleton(s => (ITokenAcquirer)s.GetRequiredService<ITokenAcquisition>());

                services.AddSingleton<ITokenAcquisitionHost, TokenAcquisitionAspnetCoreHost>();
                services.AddSingleton(s => (ITokenAcquisitionInternal)s.GetRequiredService<ITokenAcquisition>());
#else
                // .NET FW.
                services.AddSingleton<ITokenAcquisition, TokenAcquisition>();
                services.AddSingleton(s => (ITokenAcquirer)s.GetRequiredService<ITokenAcquisition>());

                services.AddSingleton<ITokenAcquisitionHost, DefaultTokenAcquisitionHost>();
#endif
            }
            else
            {
#if !NET472 && !NET462
                // ASP.NET Core
                services.AddHttpContextAccessor();

                services.AddScoped<ITokenAcquisition, TokenAcquisitionAspNetCore>();
                services.AddScoped(s => (ITokenAcquirer)s.GetRequiredService<ITokenAcquisition>());

                services.AddScoped<ITokenAcquisitionHost, TokenAcquisitionAspnetCoreHost>();
                services.AddScoped(s => (ITokenAcquisitionInternal)s.GetRequiredService<ITokenAcquisition>());
#else
                // .NET FW.
                services.AddScoped<ITokenAcquisition, TokenAcquisition>();
                services.AddScoped(s => (ITokenAcquirer)s.GetRequiredService<ITokenAcquisition>());

                services.AddScoped<ITokenAcquisitionHost, DefaultTokenAcquisitionHost>();
#endif
            }
            
            return services;
        }
    }
}
