// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client;
using Abstractions = Microsoft.Identity.Abstractions;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Builds <see cref="Abstractions.AcquireTokenResult"/> instances from MSAL
    /// <see cref="AuthenticationResult"/>s, including token-acquisition metadata.
    /// </summary>
    internal static class AcquireTokenResultFactory
    {
        public static Abstractions.AcquireTokenResult FromMsal(AuthenticationResult result) =>
            new(
                result.AccessToken,
                result.ExpiresOn,
                result.TenantId,
                result.IdToken,
                result.Scopes,
                result.CorrelationId,
                result.TokenType)
            {
                AdditionalResponseParameters = result.AdditionalResponseParameters,
                BindingCertificate = result.BindingCertificate,
                Metadata = MapMetadata(result),
            };

        /// <summary>
        /// Maps the MSAL <see cref="AuthenticationResultMetadata"/> on an
        /// <see cref="AuthenticationResult"/> to its abstractions counterpart.
        /// Returns <see langword="null"/> when no metadata was captured.
        /// </summary>
        public static Abstractions.TokenAcquisitionMetadata? GetMetadata(AuthenticationResult result) =>
            MapMetadata(result);

        private static Abstractions.TokenAcquisitionMetadata? MapMetadata(AuthenticationResult result)
        {
            AuthenticationResultMetadata? source = result.AuthenticationResultMetadata;
            if (source is null)
            {
                return null;
            }

            // Enum casts are intentional: Abstractions enum values are kept in lock-step with MSAL.
            // Any future MSAL-only enum member falls through to its numeric value, which is
            // forward-compatible for diagnostic consumers.
            return new Abstractions.TokenAcquisitionMetadata
            {
                TokenSource = (Abstractions.AcquiredTokenSource)source.TokenSource,
                CacheRefreshReason = (Abstractions.AcquiredTokenCacheRefreshReason)source.CacheRefreshReason,
                CacheLevel = (Abstractions.AcquiredTokenCacheLevel)source.CacheLevel,
                TokenEndpoint = source.TokenEndpoint,
                DurationTotalInMs = source.DurationTotalInMs,
                DurationInHttpInMs = source.DurationInHttpInMs,
                DurationInCacheInMs = source.DurationInCacheInMs,
                RefreshOn = source.RefreshOn,
                ExpiresOn = result.ExpiresOn,
                RegionDetails = MapRegionDetails(source.RegionDetails),
            };
        }

        private static Abstractions.AcquiredTokenRegionDetails? MapRegionDetails(RegionDetails? source) =>
            source is null ? null : new Abstractions.AcquiredTokenRegionDetails
            {
                RegionUsed = source.RegionUsed,
                RegionOutcome = (Abstractions.AcquiredTokenRegionOutcome)source.RegionOutcome,
                AutoDetectionError = source.AutoDetectionError,
            };
    }
}
