// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using Microsoft.Identity.Abstractions;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Provides helper methods for creating and updating token acquisition options used in downstream API
    /// authentication scenarios.
    /// </summary>
    internal static class TokenAcquisitionOptionsHelper
    {
        /// <summary>
        /// Creates a new instance of the TokenAcquisitionOptions class based on the specified downstream API options.
        /// </summary>
        /// <param name="downstreamApiOptions">The options used to configure the authorization header provider for the downstream API. Can be null.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the token acquisition operation.</param>
        /// <returns>A TokenAcquisitionOptions object initialized with values from the provided downstream API options.</returns>
        public static TokenAcquisitionOptions CreateTokenAcquisitionOptionsFromApiOptions(
            AuthorizationHeaderProviderOptions? downstreamApiOptions,
            CancellationToken cancellationToken)
        {
            return new TokenAcquisitionOptions()
            {
                AuthenticationOptionsName = downstreamApiOptions?.AcquireTokenOptions.AuthenticationOptionsName,
                CancellationToken = cancellationToken,
                Claims = downstreamApiOptions?.AcquireTokenOptions.Claims,
                CorrelationId = downstreamApiOptions?.AcquireTokenOptions.CorrelationId ?? Guid.Empty,
                ExtraHeadersParameters = downstreamApiOptions?.AcquireTokenOptions.ExtraHeadersParameters,
                ExtraQueryParameters = downstreamApiOptions?.AcquireTokenOptions.ExtraQueryParameters,
                ExtraParameters = downstreamApiOptions?.AcquireTokenOptions.ExtraParameters,
                ForceRefresh = downstreamApiOptions?.AcquireTokenOptions.ForceRefresh ?? false,
                LongRunningWebApiSessionKey = downstreamApiOptions?.AcquireTokenOptions.LongRunningWebApiSessionKey,
                ManagedIdentity = downstreamApiOptions?.AcquireTokenOptions.ManagedIdentity,
                Tenant = downstreamApiOptions?.AcquireTokenOptions.Tenant,
                UserFlow = downstreamApiOptions?.AcquireTokenOptions.UserFlow,
                PopPublicKey = downstreamApiOptions?.AcquireTokenOptions.PopPublicKey,
                FmiPath = downstreamApiOptions?.AcquireTokenOptions.FmiPath,
            };
        }

        /// <summary>
        /// Updates the original token acquisition options with values from the specified new token acquisition options.
        /// </summary>
        /// <param name="acquireTokenOptions">The original token acquisition options to update. If null, no update is performed.</param>
        /// <param name="newTokenAcquisitionOptions">The new token acquisition options containing updated values. If null, no update is performed.</param>
        public static void UpdateOriginalTokenAcquisitionOptions(
            AcquireTokenOptions? acquireTokenOptions,
            TokenAcquisitionOptions newTokenAcquisitionOptions)
        {
            if (acquireTokenOptions is not null && newTokenAcquisitionOptions is not null)
            {
                acquireTokenOptions.LongRunningWebApiSessionKey = newTokenAcquisitionOptions.LongRunningWebApiSessionKey;
            }
        }
    }
}
