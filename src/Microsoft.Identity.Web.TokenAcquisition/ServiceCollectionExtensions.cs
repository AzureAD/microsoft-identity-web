// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Abstractions;
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
            _ = Throws.IfNull(services);

            if (services.FirstOrDefault(s => s.ImplementationType == typeof(MicrosoftIdentityOptionsMerger)) == null)
            {
                services.TryAddSingleton<IPostConfigureOptions<MicrosoftIdentityOptions>, MicrosoftIdentityOptionsMerger>();
            }
            if (services.FirstOrDefault(s => s.ImplementationType == typeof(MicrosoftAuthenticationOptionsMerger)) == null)
            {
                services.TryAddSingleton<IPostConfigureOptions<MicrosoftAuthenticationOptions>, MicrosoftAuthenticationOptionsMerger>();
            }
            if (services.FirstOrDefault(s => s.ImplementationType == typeof(ConfidentialClientApplicationOptionsMerger)) == null)
            {
                services.TryAddSingleton<IPostConfigureOptions<ConfidentialClientApplicationOptions>, ConfidentialClientApplicationOptionsMerger>();
            }

            ServiceDescriptor? tokenAcquisitionService = services.FirstOrDefault(s => s.ServiceType == typeof(ITokenAcquisition));             
            ServiceDescriptor? tokenAcquisitionInternalService = services.FirstOrDefault(s => s.ServiceType == typeof(ITokenAcquisitionInternal));
            ServiceDescriptor? tokenAcquisitionhost = services.FirstOrDefault(s => s.ServiceType == typeof(ITokenAcquisitionHost));
            ServiceDescriptor? authenticationHeaderCreator = services.FirstOrDefault(s => s.ServiceType == typeof(IAuthorizationHeaderProvider));
            ServiceDescriptor? tokenAcquirerFactory = services.FirstOrDefault(s => s.ServiceType == typeof(ITokenAcquirerFactory));
            if (tokenAcquisitionService != null && tokenAcquisitionInternalService != null && tokenAcquisitionhost != null && authenticationHeaderCreator != null && tokenAcquirerFactory != null)
            {
                if (isTokenAcquisitionSingleton ^ (tokenAcquisitionService.Lifetime == ServiceLifetime.Singleton))
                {
                    // The service was already added, but not with the right lifetime
                    services.Remove(tokenAcquisitionService);
                    services.Remove(tokenAcquisitionInternalService);
                    services.Remove(tokenAcquisitionhost);
                    services.Remove(authenticationHeaderCreator);
                    services.Remove(tokenAcquirerFactory);
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
#if !NETSTANDARD2_0 && !NET462 && !NET472
                // ASP.NET Core
                services.AddHttpContextAccessor();
                services.AddSingleton<ITokenAcquisition, TokenAcquisitionAspNetCore>();
                services.AddSingleton(s => (ITokenAcquirerFactory)s.GetRequiredService<ITokenAcquisition>());

                services.AddSingleton<ITokenAcquisitionHost, TokenAcquisitionAspnetCoreHost>();

#else
                // .NET FW.
                services.AddSingleton<ITokenAcquisition, TokenAcquisition>();
                services.AddSingleton<ITokenAcquisitionHost, DefaultTokenAcquisitionHost>();
#endif
                services.AddSingleton(s => (ITokenAcquisitionInternal)s.GetRequiredService<ITokenAcquisition>());
                services.AddSingleton<IAuthorizationHeaderProvider, DefaultAuthorizationHeaderProvider>();
            }
            else
            {
#if !NETSTANDARD2_0 && !NET462 && !NET472
                // ASP.NET Core
                services.AddHttpContextAccessor();

                services.AddScoped<ITokenAcquisition, TokenAcquisitionAspNetCore>();
                services.AddScoped(s => (ITokenAcquirerFactory)s.GetRequiredService<ITokenAcquisition>());

                services.AddScoped<ITokenAcquisitionHost, TokenAcquisitionAspnetCoreHost>();
#else
                // .NET FW.
                services.AddScoped<ITokenAcquisition, TokenAcquisition>();
                services.AddScoped<ITokenAcquisitionHost, DefaultTokenAcquisitionHost>();
#endif
                services.AddScoped(s => (ITokenAcquisitionInternal)s.GetRequiredService<ITokenAcquisition>());
                services.AddScoped<IAuthorizationHeaderProvider, DefaultAuthorizationHeaderProvider>();
            }

            return services;
        }
    }
}
