// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Extensin methods for adding Azure credentials to the service collection.
    /// </summary>
    public static class ServiceCollectionExtensionForAzureCreds
	{
        /// <summary>
        /// Enables apps to use the <see cref="MicrosoftIdentityTokenCredential"/> for Azure AD authentication. 
        /// </summary>
        /// <param name="services">Service collection where to add the <see cref="MicrosoftIdentityTokenCredential"/>.</param>
        /// <returns>the service collection.</returns>
		public static IServiceCollection AddMicrosoftIdentityAzureTokenCredential(this IServiceCollection services)
		{
			services.AddScoped<MicrosoftIdentityTokenCredential>();
			return services;
		}

	}
}
