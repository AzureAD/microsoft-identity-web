// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Client;

namespace Microsoft.Identity.Web.Internal
{
    /// <summary>
    /// AOT-safe binder for <see cref="ConfidentialClientApplicationOptions"/>.
    /// Binds properties from the JSON schema that map to MSAL's <see cref="ConfidentialClientApplicationOptions"/>
    /// and its base class <see cref="ApplicationOptions"/>.
    /// </summary>
    internal static class ConfidentialClientApplicationOptionsBinder
    {
        /// <summary>
        /// Binds the <see cref="ConfidentialClientApplicationOptions"/> from the specified configuration section.
        /// </summary>
        /// <param name="options">The options instance to bind to.</param>
        /// <param name="configurationSection">The configuration section containing the values.</param>
        public static void Bind(ConfidentialClientApplicationOptions options, IConfigurationSection? configurationSection)
        {
            if (configurationSection == null)
            {
                return;
            }

            // ApplicationOptions properties (base class)
            if (configurationSection[nameof(options.ClientId)] is string clientId)
            {
                options.ClientId = clientId;
            }

            if (configurationSection[nameof(options.TenantId)] is string tenantId)
            {
                options.TenantId = tenantId;
            }

            if (configurationSection[nameof(options.Instance)] is string instance)
            {
                options.Instance = instance;
            }

            if (configurationSection[nameof(options.AadAuthorityAudience)] is string aadAuthorityAudience &&
                Enum.TryParse<AadAuthorityAudience>(aadAuthorityAudience, ignoreCase: true, out var aadAuthorityAudienceValue))
            {
                options.AadAuthorityAudience = aadAuthorityAudienceValue;
            }

            if (configurationSection[nameof(options.AzureCloudInstance)] is string azureCloudInstance &&
                Enum.TryParse<AzureCloudInstance>(azureCloudInstance, ignoreCase: true, out var azureCloudInstanceValue))
            {
                options.AzureCloudInstance = azureCloudInstanceValue;
            }

            if (configurationSection[nameof(options.RedirectUri)] is string redirectUri)
            {
                options.RedirectUri = redirectUri;
            }

            if (configurationSection[nameof(options.ClientName)] is string clientName)
            {
                options.ClientName = clientName;
            }

            if (configurationSection[nameof(options.ClientVersion)] is string clientVersion)
            {
                options.ClientVersion = clientVersion;
            }

            if (configurationSection[nameof(options.LegacyCacheCompatibilityEnabled)] is string legacyCacheCompatibilityEnabled &&
                bool.TryParse(legacyCacheCompatibilityEnabled, out bool legacyCacheCompatibilityEnabledValue))
            {
                options.LegacyCacheCompatibilityEnabled = legacyCacheCompatibilityEnabledValue;
            }

            // ConfidentialClientApplicationOptions properties (derived class)
            if (configurationSection[nameof(options.ClientSecret)] is string clientSecret)
            {
                options.ClientSecret = clientSecret;
            }

            if (configurationSection[nameof(options.AzureRegion)] is string azureRegion)
            {
                options.AzureRegion = azureRegion;
            }

            if (configurationSection[nameof(options.EnableCacheSynchronization)] is string enableCacheSynchronization &&
                bool.TryParse(enableCacheSynchronization, out bool enableCacheSynchronizationValue))
            {
                options.EnableCacheSynchronization = enableCacheSynchronizationValue;
            }
        }
    }
}
