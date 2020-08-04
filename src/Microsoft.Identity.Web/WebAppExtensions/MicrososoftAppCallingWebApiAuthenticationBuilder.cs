// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
            return Services.AddInMemoryTokenCaches();
        }

        /// <summary>
        /// Add distributed token caches.
        /// </summary>
        /// <returns>the service collection.</returns>
        public IServiceCollection AddDistributedTokenCaches()
        {
            return Services.AddDistributedTokenCaches();
        }

        /// <summary>
        /// Add session token caches.
        /// </summary>
        /// <returns>the service collection.</returns>
        public IServiceCollection AddSessionTokenCaches()
        {
            return Services.AddSessionTokenCaches();
        }
    }
}
