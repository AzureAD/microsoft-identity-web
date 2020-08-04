// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Identity.Web.TokenCacheProviders;
using Microsoft.Identity.Web.TokenCacheProviders.Distributed;
using Microsoft.Identity.Web.TokenCacheProviders.InMemory;
using Microsoft.Identity.Web.TokenCacheProviders.Session;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Authentication builder returned by the CallsWebApi methods
    /// enabling to decide token cache implementations.
    /// </summary>
    public class MicrososoftAppCallingWebApiAuthenticationBuilder : MicrosoftBaseAuthenticationBuilder
    {
        internal MicrososoftAppCallingWebApiAuthenticationBuilder(
            IServiceCollection services,
            IConfigurationSection? configurationSection = null)
            : base(services, configurationSection)
        {
        }

        /// <summary>
        /// Add in memory token caches.
        /// </summary>
        /// <returns>the service collection.</returns>
        public IServiceCollection AddInMemoryTokenCaches()
        {
            Services.AddMemoryCache();
            Services.AddHttpContextAccessor();
            Services.AddSingleton<IMsalTokenCacheProvider, MsalMemoryTokenCacheProvider>();
            return Services;
        }

        /// <summary>
        /// Add distributed token caches.
        /// </summary>
        /// <returns>the service collection.</returns>
        public IServiceCollection AddDistributedTokenCaches()
        {
            Services.AddDistributedAppTokenCache();
            Services.AddDistributedUserTokenCache();
            return Services;
        }

        /// <summary>
        /// Add session token caches.
        /// </summary>
        /// <returns>the service collection.</returns>
        public IServiceCollection AddSessionTokenCaches()
        {
            // Add session if you are planning to use session based token cache
            var sessionStoreService = Services.FirstOrDefault(x => x.ServiceType.Name == "ISessionStore");

            // If not added already
            if (sessionStoreService == null)
            {
                Services.AddSession(option =>
                {
                    option.Cookie.IsEssential = true;
                });
            }
            else
            {
                // If already added, ensure the options are set to use Cookies
                Services.Configure<SessionOptions>(option =>
                {
                    option.Cookie.IsEssential = true;
                });
            }

            Services.AddHttpContextAccessor();
            Services.AddScoped<IMsalTokenCacheProvider, MsalSessionTokenCacheProvider>();
            Services.TryAddScoped(provider =>
            {
                var httpContext = provider.GetRequiredService<IHttpContextAccessor>().HttpContext;
                if (httpContext == null)
                {
                    throw new InvalidOperationException(IDWebErrorMessage.HttpContextIsNull);
                }

                return httpContext.Session;
            });

            return Services;
        }
    }
}
