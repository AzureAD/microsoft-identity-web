// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
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
    /// Authentication builder returned by the EnableTokenAcquisitionToCallDownstreamApi methods
    /// enabling you to decide token cache implementations.
    /// </summary>
    public class MicrosoftIdentityAppCallsWebApiAuthenticationBuilder : MicrosoftIdentityBaseAuthenticationBuilder
    {
        internal MicrosoftIdentityAppCallsWebApiAuthenticationBuilder(
            IServiceCollection services,
            IConfigurationSection? configurationSection = null)
            : base(services, configurationSection)
        {
        }

        /// <summary>
        /// Add in memory token caches.
        /// </summary>
        /// <param name="configureOptions"><see cref="MsalMemoryTokenCacheOptions"/> to configure.</param>
        /// <param name="memoryCacheOptions">TODO.</param>
        /// <returns>the service collection.</returns>
        public MicrosoftIdentityAppCallsWebApiAuthenticationBuilder AddInMemoryTokenCaches(
        Action<MsalMemoryTokenCacheOptions>? configureOptions = null,
        Action<MemoryCacheOptions>? memoryCacheOptions = null)
        {
            if (configureOptions != null)
            {
                Services.Configure(configureOptions);
            }

            if (memoryCacheOptions != null)
            {
                Services.AddMemoryCache(memoryCacheOptions);
            }
            else
            {
                Services.AddMemoryCache();
            }
            Services.AddHttpContextAccessor();
            Services.AddSingleton<IMsalTokenCacheProvider, MsalMemoryTokenCacheProvider>();
            return this;
        }

        /// <summary>
        /// Add distributed token caches.
        /// </summary>
        /// <returns>the service collection.</returns>
        public MicrosoftIdentityAppCallsWebApiAuthenticationBuilder AddDistributedTokenCaches()
        {
            Services.AddDistributedAppTokenCache();
            Services.AddDistributedUserTokenCache();
            return this;
        }

        /// <summary>
        /// Add session token caches.
        /// </summary>
        /// <returns>the service collection.</returns>
        public MicrosoftIdentityAppCallsWebApiAuthenticationBuilder AddSessionTokenCaches()
        {
            // Add session if you are planning to use session based token cache
            var sessionStoreService = Services.FirstOrDefault(x => x.ServiceType.Name == Constants.ISessionStore);

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

            return this;
        }
    }
}
