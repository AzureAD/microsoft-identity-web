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
        /// <param name="memoryCacheOptions"><see cref="MemoryCacheOptions"/> to configure.</param>
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
            Services.AddDistributedTokenCaches();
            return this;
        }
    }
}
