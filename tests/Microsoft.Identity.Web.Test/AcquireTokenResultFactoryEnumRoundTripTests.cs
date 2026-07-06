// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Region;
using Xunit;
using Abstractions = Microsoft.Identity.Abstractions;

namespace Microsoft.Identity.Web.Test
{
    /// <summary>
    /// Pins the numeric lockstep between MSAL enums and their Abstractions counterparts.
    /// <see cref="AcquireTokenResultFactory.MapMetadata"/> casts MSAL enum values directly
    /// to Abstractions enum values. If MSAL ever renumbers a member or adds a new one
    /// out of order, these tests will fail loudly instead of producing silently-wrong
    /// telemetry. One assertion per member of every casted enum.
    /// </summary>
    public class AcquireTokenResultFactoryEnumRoundTripTests
    {
        [Theory]
        [InlineData(TokenSource.IdentityProvider, Abstractions.AcquiredTokenSource.IdentityProvider)]
        [InlineData(TokenSource.Cache, Abstractions.AcquiredTokenSource.Cache)]
        [InlineData(TokenSource.Broker, Abstractions.AcquiredTokenSource.Broker)]
        public void TokenSource_NumericCast_RoundTrips(TokenSource msal, Abstractions.AcquiredTokenSource expected)
        {
            Assert.Equal((int)expected, (int)msal);
            Assert.Equal(expected, (Abstractions.AcquiredTokenSource)msal);
        }

        [Theory]
        [InlineData(CacheRefreshReason.NotApplicable, Abstractions.AcquiredTokenCacheRefreshReason.NotApplicable)]
        [InlineData(CacheRefreshReason.ForceRefreshOrClaims, Abstractions.AcquiredTokenCacheRefreshReason.ForceRefreshOrClaims)]
        [InlineData(CacheRefreshReason.NoCachedAccessToken, Abstractions.AcquiredTokenCacheRefreshReason.NoCachedAccessToken)]
        [InlineData(CacheRefreshReason.Expired, Abstractions.AcquiredTokenCacheRefreshReason.Expired)]
        [InlineData(CacheRefreshReason.ProactivelyRefreshed, Abstractions.AcquiredTokenCacheRefreshReason.ProactivelyRefreshed)]
        [InlineData(CacheRefreshReason.CacheDisabled, Abstractions.AcquiredTokenCacheRefreshReason.CacheDisabled)]
        public void CacheRefreshReason_NumericCast_RoundTrips(CacheRefreshReason msal, Abstractions.AcquiredTokenCacheRefreshReason expected)
        {
            Assert.Equal((int)expected, (int)msal);
            Assert.Equal(expected, (Abstractions.AcquiredTokenCacheRefreshReason)msal);
        }

        [Theory]
        [InlineData(CacheLevel.None, Abstractions.AcquiredTokenCacheLevel.None)]
        [InlineData(CacheLevel.Unknown, Abstractions.AcquiredTokenCacheLevel.Unknown)]
        [InlineData(CacheLevel.L1Cache, Abstractions.AcquiredTokenCacheLevel.L1Cache)]
        [InlineData(CacheLevel.L2Cache, Abstractions.AcquiredTokenCacheLevel.L2Cache)]
        public void CacheLevel_NumericCast_RoundTrips(CacheLevel msal, Abstractions.AcquiredTokenCacheLevel expected)
        {
            Assert.Equal((int)expected, (int)msal);
            Assert.Equal(expected, (Abstractions.AcquiredTokenCacheLevel)msal);
        }

        [Theory]
        [InlineData(RegionOutcome.None, Abstractions.AcquiredTokenRegionOutcome.None)]
        [InlineData(RegionOutcome.UserProvidedValid, Abstractions.AcquiredTokenRegionOutcome.UserProvidedValid)]
        [InlineData(RegionOutcome.UserProvidedAutodetectionFailed, Abstractions.AcquiredTokenRegionOutcome.UserProvidedAutodetectionFailed)]
        [InlineData(RegionOutcome.UserProvidedInvalid, Abstractions.AcquiredTokenRegionOutcome.UserProvidedInvalid)]
        [InlineData(RegionOutcome.AutodetectSuccess, Abstractions.AcquiredTokenRegionOutcome.AutodetectSuccess)]
        [InlineData(RegionOutcome.FallbackToGlobal, Abstractions.AcquiredTokenRegionOutcome.FallbackToGlobal)]
        public void RegionOutcome_NumericCast_RoundTrips(RegionOutcome msal, Abstractions.AcquiredTokenRegionOutcome expected)
        {
            Assert.Equal((int)expected, (int)msal);
            Assert.Equal(expected, (Abstractions.AcquiredTokenRegionOutcome)msal);
        }

        /// <summary>
        /// Defends against MSAL adding a new member at any position other than the next sequential
        /// integer. If MSAL grows or the Abstractions counterpart grows, both sides must be updated
        /// together. A drift here means the casts in AcquireTokenResultFactory will silently produce
        /// wrong values for new members.
        /// </summary>
        [Fact]
        public void EnumMemberCounts_MatchAbstractions()
        {
            Assert.Equal(
                System.Enum.GetValues(typeof(TokenSource)).Length,
                System.Enum.GetValues(typeof(Abstractions.AcquiredTokenSource)).Length);

            Assert.Equal(
                System.Enum.GetValues(typeof(CacheRefreshReason)).Length,
                System.Enum.GetValues(typeof(Abstractions.AcquiredTokenCacheRefreshReason)).Length);

            Assert.Equal(
                System.Enum.GetValues(typeof(CacheLevel)).Length,
                System.Enum.GetValues(typeof(Abstractions.AcquiredTokenCacheLevel)).Length);

            Assert.Equal(
                System.Enum.GetValues(typeof(RegionOutcome)).Length,
                System.Enum.GetValues(typeof(Abstractions.AcquiredTokenRegionOutcome)).Length);
        }

        [Fact]
        public void GetMetadata_SetsExpiresOn_FromResult_WhenMetadataAvailable()
        {
            // Arrange
            DateTimeOffset expiresOn = DateTimeOffset.UtcNow.AddHours(1);
            DateTimeOffset extendedExpiresOn = DateTimeOffset.UtcNow.AddHours(2);
            DateTimeOffset refreshOn = DateTimeOffset.UtcNow.AddMinutes(30);
            var source = new AuthenticationResultMetadata(TokenSource.IdentityProvider)
            {
                RefreshOn = refreshOn,
            };
            var result = new AuthenticationResult(
                "access-token",
                false,
                null,
                expiresOn,
                extendedExpiresOn,
                "tenant",
                null,
                null,
                new[] { "scope" },
                Guid.NewGuid(),
                source);

            // Act
            Abstractions.TokenAcquisitionMetadata? metadata = AcquireTokenResultFactory.GetMetadata(result);

            // Assert
            Assert.NotNull(metadata);
            Assert.Equal(expiresOn, metadata!.ExpiresOn);
            // ExpiresOn flows from result.ExpiresOn, distinct from the metadata's RefreshOn hint.
            Assert.Equal(refreshOn, metadata.RefreshOn);
        }

        [Fact]
        public void GetMetadata_ReturnsNull_WhenMetadataAbsent()
        {
            // Arrange
            DateTimeOffset expiresOn = DateTimeOffset.UtcNow.AddHours(1);
            DateTimeOffset extendedExpiresOn = DateTimeOffset.UtcNow.AddHours(2);
            var result = new AuthenticationResult(
                "access-token",
                false,
                null,
                expiresOn,
                extendedExpiresOn,
                "tenant",
                null,
                null,
                new[] { "scope" },
                Guid.NewGuid());

            // Act
            Abstractions.TokenAcquisitionMetadata? metadata = AcquireTokenResultFactory.GetMetadata(result);

            // Assert
            Assert.Null(metadata);
        }

        [Fact]
        public void FromMsal_MapsExpiresOn_ToTopLevelAndMetadata()
        {
            // Arrange
            DateTimeOffset expiresOn = DateTimeOffset.UtcNow.AddHours(1);
            DateTimeOffset extendedExpiresOn = DateTimeOffset.UtcNow.AddHours(2);
            var source = new AuthenticationResultMetadata(TokenSource.IdentityProvider);
            var result = new AuthenticationResult(
                "access-token",
                false,
                null,
                expiresOn,
                extendedExpiresOn,
                "tenant",
                null,
                null,
                new[] { "scope" },
                Guid.NewGuid(),
                source);

            // Act
            Abstractions.AcquireTokenResult acquired = AcquireTokenResultFactory.FromMsal(result);

            // Assert — ExpiresOn is surfaced both at the top level and on the metadata surface.
            Assert.Equal(expiresOn, acquired.ExpiresOn);
            Assert.NotNull(acquired.Metadata);
            Assert.Equal(expiresOn, acquired.Metadata!.ExpiresOn);
        }
    }
}
