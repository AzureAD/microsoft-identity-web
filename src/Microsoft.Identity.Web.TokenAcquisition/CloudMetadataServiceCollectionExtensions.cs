// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Identity.Abstractions;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Extension methods that register cloud-specific metadata (for example the Federated Identity Credential
    /// token-exchange audience, keyed by authority host) from configuration, so Microsoft.Identity.Web can
    /// resolve the correct value for sovereign or private clouds it and MSAL do not ship — with no code beyond
    /// binding an <c>appsettings.json</c> section.
    /// </summary>
    public static class CloudMetadataServiceCollectionExtensions
    {
        /// <summary>
        /// Registers an <see cref="ICloudMetadataProvider"/> populated from <paramref name="cloudMetadataSection"/>
        /// so Microsoft.Identity.Web resolves cloud-specific FIC token-exchange values (for clouds it and MSAL do
        /// not know about) from configuration. This is the ID Web counterpart to MISE's
        /// <c>AddMiseCloudMetadata(IConfiguration)</c> and uses the same section shape.
        /// </summary>
        /// <param name="services">The service collection to modify.</param>
        /// <param name="cloudMetadataSection">A configuration section whose immediate children are authority
        /// hosts, each of whose children are metadata key/value pairs. For example:
        /// <code>
        /// "CloudMetadata": {
        ///   "login.microsoftonline.us": { "token_exchange_audience": "api://AzureADTokenExchangeUSGov" },
        ///   "login.mynewcloud.example": { "token_exchange_audience": "api://AzureADTokenExchangeMyCloud" }
        /// }
        /// </code>
        /// Keys should come from <see cref="AbstractionsCloudKeys"/>.
        /// </param>
        /// <returns>The original service collection instance.</returns>
        /// <remarks>
        /// The provider is registered with <c>TryAddSingleton</c>, so an <see cref="ICloudMetadataProvider"/> a
        /// caller (or an upstream SDK such as MISE) registered explicitly beforehand takes precedence and is
        /// left untouched.
        /// </remarks>
        public static IServiceCollection AddCloudMetadata(this IServiceCollection services, IConfiguration cloudMetadataSection)
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (cloudMetadataSection is null)
            {
                throw new ArgumentNullException(nameof(cloudMetadataSection));
            }

            var provider = new InMemoryCloudMetadataProvider();

            foreach (IConfigurationSection hostSection in cloudMetadataSection.GetChildren())
            {
                var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (IConfigurationSection entry in hostSection.GetChildren())
                {
                    if (entry.Value is not null)
                    {
                        values[entry.Key] = entry.Value;
                    }
                }

                if (values.Count > 0)
                {
                    provider.AddOrUpdate(hostSection.Key, values);
                }
            }

            services.TryAddSingleton<ICloudMetadataProvider>(provider);
            return services;
        }
    }
}
