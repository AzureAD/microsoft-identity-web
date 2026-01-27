// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#if !NETSTANDARD2_0 && !NET462 && !NET472
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Identity.Web.Internal
{
    /// <summary>
    /// AOT-safe configuration binder for <see cref="JwtBearerOptions"/>.
    /// This binder reads configuration values without using reflection-based ConfigurationBinder.Bind().
    /// </summary>
    internal static class JwtBearerOptionsBinder
    {
        /// <summary>
        /// Binds configuration values to the specified <see cref="JwtBearerOptions"/> instance.
        /// </summary>
        /// <param name="options">The options instance to bind to.</param>
        /// <param name="configurationSection">The configuration section containing the values.</param>
        public static void Bind(JwtBearerOptions options, IConfigurationSection? configurationSection)
        {
            if (configurationSection is null)
            {
                return;
            }

            // Audience is the main property from AzureAd config that applies to JwtBearerOptions
            var audience = configurationSection[nameof(JwtBearerOptions.Audience)];
            if (!string.IsNullOrEmpty(audience))
            {
                options.Audience = audience;
            }

            // Authority can also be bound from configuration
            var authority = configurationSection[nameof(JwtBearerOptions.Authority)];
            if (!string.IsNullOrEmpty(authority))
            {
                options.Authority = authority;
            }

            // MetadataAddress can be bound from configuration
            var metadataAddress = configurationSection[nameof(JwtBearerOptions.MetadataAddress)];
            if (!string.IsNullOrEmpty(metadataAddress))
            {
                options.MetadataAddress = metadataAddress;
            }

            // RequireHttpsMetadata can be bound from configuration
            var requireHttpsMetadata = configurationSection[nameof(JwtBearerOptions.RequireHttpsMetadata)];
            if (!string.IsNullOrEmpty(requireHttpsMetadata) && bool.TryParse(requireHttpsMetadata, out var requireHttps))
            {
                options.RequireHttpsMetadata = requireHttps;
            }

            // SaveToken can be bound from configuration
            var saveToken = configurationSection[nameof(JwtBearerOptions.SaveToken)];
            if (!string.IsNullOrEmpty(saveToken) && bool.TryParse(saveToken, out var save))
            {
                options.SaveToken = save;
            }

            // IncludeErrorDetails can be bound from configuration
            var includeErrorDetails = configurationSection[nameof(JwtBearerOptions.IncludeErrorDetails)];
            if (!string.IsNullOrEmpty(includeErrorDetails) && bool.TryParse(includeErrorDetails, out var includeDetails))
            {
                options.IncludeErrorDetails = includeDetails;
            }

            // RefreshOnIssuerKeyNotFound can be bound from configuration
            var refreshOnIssuerKeyNotFound = configurationSection[nameof(JwtBearerOptions.RefreshOnIssuerKeyNotFound)];
            if (!string.IsNullOrEmpty(refreshOnIssuerKeyNotFound) && bool.TryParse(refreshOnIssuerKeyNotFound, out var refresh))
            {
                options.RefreshOnIssuerKeyNotFound = refresh;
            }
        }
    }
}
#endif
