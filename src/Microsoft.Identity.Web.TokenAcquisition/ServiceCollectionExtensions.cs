// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Net.Http;
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
        /// <param name="enableTokenBinding">Specifies token acquisition should be configured with token binding (mTLS PoP) enabled.</param>
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
            bool isTokenAcquisitionSingleton = false,
            bool enableTokenBinding = false)
        {
            _ = Throws.IfNull(services);

#if !NETSTANDARD2_0 && !NET462 && !NET472
            bool forceSdk = !services.Any(s => s.ServiceType.FullName == "Microsoft.AspNetCore.Authentication.IAuthenticationService");
#endif

            if (!HasImplementationType(services, typeof(DefaultCertificateLoader)))
            {
                services.TryAddSingleton<ICredentialsLoader, DefaultCertificateLoader>();
            }

            if (!HasImplementationType(services, typeof(MicrosoftIdentityOptionsMerger)))
            {
                services.TryAddSingleton<IPostConfigureOptions<MicrosoftIdentityOptions>, MicrosoftIdentityOptionsMerger>();
            }
            if (!HasImplementationType(services, typeof(MicrosoftIdentityApplicationOptionsMerger)))
            {
                services.TryAddSingleton<IPostConfigureOptions<MicrosoftIdentityApplicationOptions>, MicrosoftIdentityApplicationOptionsMerger>();
            }
            if (!HasImplementationType(services, typeof(ConfidentialClientApplicationOptionsMerger)))
            {
                services.TryAddSingleton<IPostConfigureOptions<ConfidentialClientApplicationOptions>, ConfidentialClientApplicationOptionsMerger>();
            }

            ServiceDescriptor? tokenAcquisitionService = services.FirstOrDefault(s => s.ServiceType == typeof(ITokenAcquisition));
            ServiceDescriptor? tokenAcquisitionInternalService = services.FirstOrDefault(s => s.ServiceType == typeof(ITokenAcquisitionInternal));
            ServiceDescriptor? tokenAcquisitionhost = services.FirstOrDefault(s => s.ServiceType == typeof(ITokenAcquisitionHost));
            ServiceDescriptor? authenticationHeaderCreator = services.FirstOrDefault(s => s.ServiceType == typeof(IAuthorizationHeaderProvider));
            ServiceDescriptor? tokenAcquirerFactory = services.FirstOrDefault(s => s.ServiceType == typeof(ITokenAcquirerFactory));
            ServiceDescriptor? authSchemeInfoProvider = services.FirstOrDefault(s => s.ServiceType == typeof(Abstractions.IAuthenticationSchemeInformationProvider));

            if (tokenAcquisitionService != null && tokenAcquisitionInternalService != null &&
                tokenAcquisitionhost != null && authenticationHeaderCreator != null && authSchemeInfoProvider != null)
            {
                if (isTokenAcquisitionSingleton ^ (tokenAcquisitionService.Lifetime == ServiceLifetime.Singleton))
                {
                    // The service was already added, but not with the right lifetime
                    services.Remove(tokenAcquisitionService);
                    services.Remove(tokenAcquisitionInternalService);
                    services.Remove(tokenAcquisitionhost);
                    services.Remove(authenticationHeaderCreator);
                    services.Remove(authSchemeInfoProvider);

                    // To support ASP.NET Core 2.x on .NET FW. It won't use the TokenAcquirerFactory.GetDefaultInstance()
                    if (tokenAcquirerFactory != null)
                    {
                        services.Remove(tokenAcquirerFactory);
                        tokenAcquirerFactory = null;
                    }
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
#if NET6_0_OR_GREATER
                // ASP.NET Core
                if (forceSdk)
                {
                    services.AddSingleton<ITokenAcquisitionHost, DefaultTokenAcquisitionHost>();
                }
                else
                {
                    services.AddSingleton<ITokenAcquisitionHost, TokenAcquisitionAspnetCoreHost>();
                    services.AddHttpContextAccessor();
                }
                services.AddSingleton<ITokenAcquisition, TokenAcquisitionAspNetCore>();
                services.AddSingleton<ITokenAcquirerFactory, DefaultTokenAcquirerFactoryImplementation>();

#else
                // .NET FW.
                services.AddSingleton<ITokenAcquisition, TokenAcquisition>();
                services.AddSingleton<ITokenAcquisitionHost, DefaultTokenAcquisitionHost>();

                // To support ASP.NET Core 2.x on .NET FW. It won't use the TokenAcquirerFactory.GetDefaultInstance()
                if (tokenAcquirerFactory == null)
                {
                    services.AddSingleton<ITokenAcquirerFactory, DefaultTokenAcquirerFactoryImplementation>();
                }
#endif
                services.AddSingleton(s => (ITokenAcquisitionInternal)s.GetRequiredService<ITokenAcquisition>());
                services.AddSingleton<Abstractions.IAuthenticationSchemeInformationProvider>(sp =>
                    sp.GetRequiredService<ITokenAcquisitionHost>());
                services.AddSingleton<IAuthorizationHeaderProvider, DefaultAuthorizationHeaderProvider>();
            }
            else
            {
#if NET6_0_OR_GREATER

                if (forceSdk)
                {
                    services.AddScoped<ITokenAcquisitionHost, DefaultTokenAcquisitionHost>();
                }
                else
                {
                    services.AddScoped<ITokenAcquisitionHost, TokenAcquisitionAspnetCoreHost>();
                    services.AddHttpContextAccessor();
                }
                services.AddScoped<ITokenAcquisition, TokenAcquisitionAspNetCore>();
                services.AddScoped<ITokenAcquirerFactory, DefaultTokenAcquirerFactoryImplementation>();

#else
                // .NET FW.
                services.AddScoped<ITokenAcquisition, TokenAcquisition>();
                services.AddScoped<ITokenAcquisitionHost, DefaultTokenAcquisitionHost>();
                if (tokenAcquirerFactory == null)
                {
                    services.AddScoped<ITokenAcquirerFactory, DefaultTokenAcquirerFactoryImplementation>();
                }
#endif
                services.AddScoped(s => (ITokenAcquisitionInternal)s.GetRequiredService<ITokenAcquisition>());
                services.AddScoped<Abstractions.IAuthenticationSchemeInformationProvider>(sp =>
                    sp.GetRequiredService<ITokenAcquisitionHost>());
                services.AddScoped<IAuthorizationHeaderProvider, DefaultAuthorizationHeaderProvider>();
            }

            if (enableTokenBinding)
            {
                EnableTokenBinding(services);
            }

            services.TryAddSingleton<IMergedOptionsStore, MergedOptionsStore>();
            return services;
        }

        /// <summary>
        /// Enables token binding by registering required for it services in the service collection.
        /// </summary>
        /// <remarks>
        /// This extension should be called when token acquisition is already configured.
        /// </remarks>
        /// <param name="services">The service collection to which the token binding support will be added. Must not be null.</param>
        /// <returns>The same IServiceCollection instance, allowing for method chaining.</returns>
        public static IServiceCollection EnableTokenBinding(this IServiceCollection services)
        {
            ServiceDescriptor? msalHttpClientFactory = services.FirstOrDefault(s => s.ServiceType == typeof(IMsalHttpClientFactory));
            if (msalHttpClientFactory != null)
            {
                services.Remove(msalHttpClientFactory);
            }

            // Registering HTTP client factory which allows to create HTTP client with client certificate
            services.AddSingleton<IMsalHttpClientFactory>(sp =>
                new MsalMtlsHttpClientFactory(sp.GetRequiredService<IHttpClientFactory>()));

            ServiceDescriptor? existingAuthorizationHeaderProvider = services.FirstOrDefault(s => s.ServiceType == typeof(IAuthorizationHeaderProvider));

            // Only register authorization header bound provider if there is no one already
            if (existingAuthorizationHeaderProvider?.ServiceType is not IAuthorizationHeaderBoundProvider)
            {
                if (existingAuthorizationHeaderProvider != null)
                {
                    services.Remove(existingAuthorizationHeaderProvider);
                }

                if (existingAuthorizationHeaderProvider?.Lifetime == ServiceLifetime.Singleton)
                {
                    // Registering singleton authorization header provider which returns authorization header with bound certificate
                    services.AddSingleton<IAuthorizationHeaderProvider>(sp =>
                    {
                        IAuthorizationHeaderProvider parentAuthorizationHeaderProvider = existingAuthorizationHeaderProvider != null
                            ? (IAuthorizationHeaderProvider)ActivatorUtilities.CreateInstance(sp, existingAuthorizationHeaderProvider.ImplementationType ?? typeof(DefaultAuthorizationHeaderProvider))
                            : new DefaultAuthorizationHeaderProvider(sp.GetRequiredService<ITokenAcquisition>());

                        return new DefaultAuthorizationHeaderBoundProvider(
                            parentAuthorizationHeaderProvider,
                            sp.GetRequiredService<ITokenAcquisition>());
                    });
                }
                else
                {
                    // Registering scoped authorization header provider which returns authorization header with bound certificate
                    services.AddScoped<IAuthorizationHeaderProvider>(sp =>
                    {
                        IAuthorizationHeaderProvider parentAuthorizationHeaderProvider = existingAuthorizationHeaderProvider != null
                            ? (IAuthorizationHeaderProvider)ActivatorUtilities.CreateInstance(sp, existingAuthorizationHeaderProvider.ImplementationType ?? typeof(DefaultAuthorizationHeaderProvider))
                            : new DefaultAuthorizationHeaderProvider(sp.GetRequiredService<ITokenAcquisition>());

                        return new DefaultAuthorizationHeaderBoundProvider(
                            parentAuthorizationHeaderProvider,
                            sp.GetRequiredService<ITokenAcquisition>());
                    });
                }
            }

            return services;
        }

        private static bool HasImplementationType(IServiceCollection services, Type implementationType)
        {
            return services.Any(s =>
#if NET8_0_OR_GREATER
                s.ServiceKey is null &&
#endif
                s.ImplementationType == implementationType);
        }
    }
}
