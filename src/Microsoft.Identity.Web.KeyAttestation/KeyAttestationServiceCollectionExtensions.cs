// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Identity.Abstractions;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Extensions for adding optional key attestation support.
    /// </summary>
    public static class KeyAttestationServiceCollectionExtensions
    {
        /// <summary>
        /// Adds Credential Guard key attestation support for managed identity mTLS
        /// proof-of-possession requests.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMicrosoftIdentityWebKeyAttestation(
            this IServiceCollection services)
        {
            ArgumentNullException.ThrowIfNull(services);

            services.TryAddSingleton<IManagedIdentityAttestationProvider, ManagedIdentityAttestationProvider>();
            services.TryAddEnumerable(
                ServiceDescriptor.Singleton<ICredentialSourceLoader, KeyAttestedManagedIdentityCredentialLoader>());

            return services;
        }
    }
}
