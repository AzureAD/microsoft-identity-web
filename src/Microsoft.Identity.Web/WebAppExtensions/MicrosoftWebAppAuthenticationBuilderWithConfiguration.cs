// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Builder for a Microsoft web app authentication where configuration is
    /// available for CallsWebApi.
    /// </summary>
    public class MicrosoftWebAppAuthenticationBuilderWithConfiguration : MicrosoftWebAppAuthenticationBuilder
    {
        /// <summary>
        ///  Constructor.
        /// </summary>
        /// <param name="services"> The services being configured.</param>
        /// <param name="openIdConnectScheme">Defaut scheme used for OpenIdConnect.</param>
        /// <param name="configureMicrosoftIdentityOptions">Action called to configure
        /// the <see cref="MicrosoftIdentityOptions"/>Microsoft identity options.</param>
        /// <param name="configurationSection">Optional configuration section.</param>
        internal MicrosoftWebAppAuthenticationBuilderWithConfiguration(
            IServiceCollection services,
            string openIdConnectScheme,
            Action<MicrosoftIdentityOptions> configureMicrosoftIdentityOptions,
            IConfigurationSection configurationSection)
            : base(services, openIdConnectScheme, configureMicrosoftIdentityOptions, configurationSection)
        {
            if (configurationSection == null)
            {
                throw new ArgumentNullException(nameof(configurationSection));
            }
        }

        /// <summary>
        /// Add support for the Web app to acquire tokens to call an API.
        /// </summary>
        /// <param name="initialScopes">Optional initial scopes to request.</param>
        /// <returns>The authentication builder for chaining.</returns>
        public MicrososoftAppCallingWebApiAuthenticationBuilder CallsWebApi(
            IEnumerable<string>? initialScopes = null)
        {
            return CallsWebApi(
                options => ConfigurationSection.Bind(options),
                initialScopes);
        }
    }
}
