// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.OidcFic;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension class to add OIDC FIC signed assertion provider to the service collection
    ///
    /// </summary>
    public static class OidcFicSignedAssertionProviderExtensions
    {
        /// <summary>
        /// Adds OIDC FIC signed assertion provider to the service collection
        /// </summary>
        /// <param name="services">service collection</param>
        /// <returns>the service collection for chaining.</returns>
        public static IServiceCollection AddOidcFic(this IServiceCollection services)
        {
            services.AddTokenAcquisition(true);
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureOptions<MicrosoftIdentityApplicationOptions>, OidcConfigurationOptions>());
            services.TryAddEnumerable(ServiceDescriptor.Singleton<ICustomSignedAssertionProvider, OidcIdpSignedAssertionLoader>());
            return services;
        }

        private sealed class OidcConfigurationOptions : IConfigureNamedOptions<MicrosoftIdentityApplicationOptions>
        {
            private readonly IServiceProvider _serviceProvider;

            public OidcConfigurationOptions(IServiceProvider serviceProvider)
            {
                _serviceProvider = serviceProvider;
            }

            public void Configure(MicrosoftIdentityApplicationOptions options) => Configure(string.Empty, options);

            public void Configure(string? name, MicrosoftIdentityApplicationOptions options)
            {
                if (!string.IsNullOrEmpty(name)
                    && string.IsNullOrEmpty(options.Instance)
                    && options.Authority == "//v2.0")
                {
                    _serviceProvider.GetService<IConfiguration>()?.GetSection(name!).Bind(options);
                }
            }
        }
    }
}
