// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Portion of the TokenAcquisition class that handles logic unique to managed identity.
    /// </summary>
    internal partial class TokenAcquisition
    {
        private readonly ConcurrentDictionary<string, IManagedIdentityApplication> _managedIdentityApplicationsByClientId = new();
        private readonly SemaphoreSlim _managedIdSemaphore = new(1, 1);
        private const string SystemAssignedManagedIdentityKey = "SYSTEM";

        /// <summary>
        /// Gets a cached ManagedIdentityApplication object or builds a new one if not found.
        /// </summary>
        /// <param name="mergedOptions">The configuration options for the app.</param>
        /// <param name="managedIdentityOptions">The configuration specific to managed identity.</param>
        /// <returns>The application object used to request a token with managed identity.</returns>
        internal async Task<IManagedIdentityApplication> GetOrBuildManagedIdentityApplicationAsync(
            MergedOptions mergedOptions,
            ManagedIdentityOptions managedIdentityOptions)
        {
            string key = GetCacheKeyForManagedId(managedIdentityOptions);

            // Check if the application is already built, if so return it without grabbing the lock
            if (_managedIdentityApplicationsByClientId.TryGetValue(key, out IManagedIdentityApplication? application))
            {
                return application;
            }

            // Lock the potential write of the dictionary to prevent multiple threads from creating the same application.
            await _managedIdSemaphore.WaitAsync();
            try
            {
                // Check if the application is already built (could happen between previous check and obtaining the key)
                if (_managedIdentityApplicationsByClientId.TryGetValue(key, out application))
                {
                    return application;
                }

                // Set managedIdentityId to the correct value for either system or user assigned
                ManagedIdentityId managedIdentityId;
                if (key == SystemAssignedManagedIdentityKey)
                {
                    managedIdentityId = ManagedIdentityId.SystemAssigned;
                }
                else
                {
                    managedIdentityId = ManagedIdentityId.WithUserAssignedClientId(key);
                }

                // Build the application
                application = BuildManagedIdentityApplication(
                    managedIdentityId,
                    mergedOptions.ConfidentialClientApplicationOptions.EnablePiiLogging
                );

                // Add the application to the cache
                _managedIdentityApplicationsByClientId.TryAdd(key, application);
            }
            finally
            {
                // Now that the dictionary is updated, release the semaphore
                _managedIdSemaphore.Release();
            }
            return application;
        }

        /// <summary>
        /// Creates a managed identity client application.
        /// </summary>
        /// <param name="managedIdentityId">Indicates if system-assigned or user-assigned managed identity is used.</param>
        /// <param name="enablePiiLogging">Indicates if logging that may contain personally identifiable information is enabled.</param>
        /// <returns>A managed identity application.</returns>
        private IManagedIdentityApplication BuildManagedIdentityApplication(ManagedIdentityId managedIdentityId, bool enablePiiLogging)
        {
            return ManagedIdentityApplicationBuilder
                .Create(managedIdentityId)
                .WithLogging(
                    Log,
                    ConvertMicrosoftExtensionsLogLevelToMsal(_logger),
                    enablePiiLogging: enablePiiLogging)
                .Build();
        }

        /// <summary>
        /// Gets the key value for the Managed Identity cache, the default key for system-assigned identity is used if there is
        /// no clientId for a user-assigned identity specified. The method is internal rather than private for testing purposes.
        /// </summary>
        /// <param name="managedIdOptions">Holds the clientId for managed identity if none is present.</param>
        /// <returns>A key value for the Managed Identity cache.</returns>
        internal static string GetCacheKeyForManagedId(ManagedIdentityOptions managedIdOptions)
        {
            if (string.IsNullOrEmpty(managedIdOptions.UserAssignedClientId))
            {
                return SystemAssignedManagedIdentityKey;
            }
            else
            {
                return managedIdOptions.UserAssignedClientId!;
            }
        }
    }
}
