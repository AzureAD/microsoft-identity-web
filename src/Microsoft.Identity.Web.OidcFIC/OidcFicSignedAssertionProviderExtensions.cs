// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Abstractions;
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

            
            services.TryAddEnumerable[ ServiceDescriptor.Singleton<ICustomSignedAssertionProvider, OidcIdpSignedAssertionLoader>] ();
            return services;
        }
    }
}
