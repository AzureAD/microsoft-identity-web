// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Base class for Web app and Web API Microsoft authentication
    /// builders.
    /// </summary>
    public abstract class MicrosoftBaseAuthenticationBuilder
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="services">The services being configured.</param>
        /// <param name="configurationSection">Optional configuration section.</param>
        protected MicrosoftBaseAuthenticationBuilder(
            IServiceCollection services,
            IConfigurationSection? configurationSection = null)
        {
            Services = services;
            ConfigurationSection = configurationSection;
        }

        /// <summary>
        /// The services being configured.
        /// </summary>
        public IServiceCollection Services { get; private set; }

        /// <summary>
        /// Configuration section from which to bind options.
        /// </summary>
        /// <remarks>It can be null if the configuration happens with delegates
        /// rather than configuration.</remarks>
        protected IConfigurationSection? ConfigurationSection { get; set; }
    }
}
