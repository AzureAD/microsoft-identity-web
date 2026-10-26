// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Identity.Client;
using Xunit;

namespace Microsoft.Identity.Web.TokenCache.SnapshotPoc
{
    public class SnapshotCachePipelineTests
    {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task OptedInProviderWritesSnapshotAndFreshApplicationHitsL1WithoutCacheJson(bool distributed)
        {
            // Arrange
            var l2 = distributed ? new CountingDistributedCache() : null;
            using var provider = new SnapshotTokenCacheProvider(l2, enableSnapshots: true);
            using var endpoint = new TokenEndpoint();

            // Act
            var first = await endpoint.CreateApplication(provider).AcquireTokenForClient(new[] { TokenEndpoint.Scope }).ExecuteAsync();

            // Assert
            Assert.Equal(TokenSource.IdentityProvider, first.AuthenticationResultMetadata.TokenSource);
            Assert.Equal(1, endpoint.TokenRequests);
            Assert.Equal(1, provider.Captures);
            Assert.Equal(distributed ? 1 : 0, provider.JsonSerializations);
            Assert.Equal(0, provider.JsonDeserializations);
            Assert.IsType<TokenCacheSnapshot>(provider.L1.Get(provider.LastKey!));
            int l2Reads = l2?.Reads ?? 0;
            int l2Writes = l2?.Writes ?? 0;

            // Act
            var second = await endpoint.CreateApplication(provider).AcquireTokenForClient(new[] { TokenEndpoint.Scope }).ExecuteAsync();

            // Assert
            Assert.Equal(TokenSource.Cache, second.AuthenticationResultMetadata.TokenSource);
            Assert.Equal(first.AccessToken, second.AccessToken);
            Assert.Equal(1, endpoint.TokenRequests);
            Assert.Equal(1, provider.Restores);
            Assert.Equal(1, provider.Captures);
            Assert.Equal(distributed ? 1 : 0, provider.JsonSerializations);
            Assert.Equal(0, provider.JsonDeserializations);
            Assert.Equal(l2Reads, l2?.Reads ?? 0);
            Assert.Equal(l2Writes, l2?.Writes ?? 0);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task DefaultStillUsesExistingJsonProvider(bool distributed)
        {
            // Arrange
            var l2 = distributed ? new CountingDistributedCache() : null;
            using var provider = new SnapshotTokenCacheProvider(l2);
            using var endpoint = new TokenEndpoint();

            // Act
            var first = await endpoint.CreateApplication(provider).AcquireTokenForClient(new[] { TokenEndpoint.Scope }).ExecuteAsync();
            var second = await endpoint.CreateApplication(provider).AcquireTokenForClient(new[] { TokenEndpoint.Scope }).ExecuteAsync();

            // Assert
            Assert.Equal(TokenSource.Cache, second.AuthenticationResultMetadata.TokenSource);
            Assert.Equal(first.AccessToken, second.AccessToken);
            Assert.Equal(1, endpoint.TokenRequests);
            Assert.Equal(0, provider.Captures);
            Assert.Equal(0, provider.Restores);
            Assert.Equal(0, provider.L1.Count);
            string key = $"{TokenEndpoint.ClientId}_{TokenEndpoint.TenantA}_AppTokenCache";
            byte[] bytes = distributed ? l2!.Get(key)! : Assert.IsType<byte[]>(provider.GetMemoryOnlyControlEntry(key));
            Assert.NotNull(JsonNode.Parse(bytes)!["AccessToken"]);
        }

        [Fact]
        public async Task DistributedL1EvictionFallsBackToJsonL2AndRepopulatesSnapshot()
        {
            // Arrange
            var l2 = new CountingDistributedCache();
            using var provider = new SnapshotTokenCacheProvider(l2, enableSnapshots: true);
            using var endpoint = new TokenEndpoint();
            var first = await endpoint.CreateApplication(provider).AcquireTokenForClient(new[] { TokenEndpoint.Scope }).ExecuteAsync();
            provider.L1.Remove(provider.LastKey!);
            int reads = l2.Reads;

            // Act
            var restored = await endpoint.CreateApplication(provider).AcquireTokenForClient(new[] { TokenEndpoint.Scope }).ExecuteAsync();

            // Assert
            Assert.Equal(TokenSource.Cache, restored.AuthenticationResultMetadata.TokenSource);
            Assert.Equal(first.AccessToken, restored.AccessToken);
            Assert.Equal(reads + 1, l2.Reads);
            Assert.Equal(1, provider.JsonDeserializations);
            Assert.Equal(1, provider.JsonSerializations);
            Assert.Equal(1, l2.Writes);
            Assert.Equal(2, provider.Captures);
            Assert.Equal(1, endpoint.TokenRequests);
            Assert.IsType<TokenCacheSnapshot>(provider.L1.Get(provider.LastKey!));

            // Act
            await endpoint.CreateApplication(provider).AcquireTokenForClient(new[] { TokenEndpoint.Scope }).ExecuteAsync();

            // Assert
            Assert.Equal(reads + 1, l2.Reads);
            Assert.Equal(1, provider.JsonDeserializations);
            Assert.Equal(1, provider.JsonSerializations);
            Assert.Equal(1, endpoint.TokenRequests);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task DistributedSnapshotAndExistingJsonProvidersCanReadEachOthersL2(bool snapshotWriter)
        {
            // Arrange
            var l2 = new CountingDistributedCache();
            using var writer = new SnapshotTokenCacheProvider(l2, enableSnapshots: snapshotWriter);
            using var reader = new SnapshotTokenCacheProvider(l2, enableSnapshots: !snapshotWriter);
            using var endpoint = new TokenEndpoint();
            var first = await endpoint.CreateApplication(writer).AcquireTokenForClient(new[] { TokenEndpoint.Scope }).ExecuteAsync();

            // Act
            var second = await endpoint.CreateApplication(reader).AcquireTokenForClient(new[] { TokenEndpoint.Scope }).ExecuteAsync();

            // Assert
            Assert.Equal(TokenSource.Cache, second.AuthenticationResultMetadata.TokenSource);
            Assert.Equal(first.AccessToken, second.AccessToken);
            Assert.Equal(1, endpoint.TokenRequests);
            Assert.Equal(1, l2.Writes);
            Assert.Equal(snapshotWriter ? 0 : 1, reader.JsonDeserializations);
        }

        [Fact]
        public async Task UnknownTopLevelAndItemFieldsSurviveL2ToSnapshotAndBackToV3()
        {
            // Arrange
            var l2 = new CountingDistributedCache();
            using var writer = new SnapshotTokenCacheProvider(l2);
            using var reader = new SnapshotTokenCacheProvider(l2, enableSnapshots: true);
            using var endpoint = new TokenEndpoint();
            await endpoint.CreateApplication(writer).AcquireTokenForClient(new[] { TokenEndpoint.Scope }).ExecuteAsync();
            string key = $"{TokenEndpoint.ClientId}_{TokenEndpoint.TenantA}_AppTokenCache";
            JsonNode json = JsonNode.Parse(l2.Get(key)!)!;
            json["future_root"] = JsonNode.Parse("{\"nested\":[null,true,123456789012345678901234567890]}");
            foreach (var token in json["AccessToken"]!.AsObject())
            {
                token.Value!["future_item"] = JsonNode.Parse("{\"value\":\"preserved\"}");
            }

            l2.Set(key, Encoding.UTF8.GetBytes(json.ToJsonString()), new DistributedCacheEntryOptions());

            // Act
            await endpoint.CreateApplication(reader).AcquireTokenForClient(new[] { TokenEndpoint.Scope }).ExecuteAsync();
            var snapshot = Assert.IsType<TokenCacheSnapshot>(reader.L1.Get(key));
            JsonNode exported = JsonNode.Parse(snapshot.SerializeMsalV3())!;

            // Assert
            Assert.True(JsonNode.DeepEquals(json, exported));
            Assert.Equal(1, reader.JsonDeserializations);
            Assert.Equal(0, reader.JsonSerializations);
            Assert.Equal(1, endpoint.TokenRequests);
        }

        [Fact]
        public async Task MalformedL2JsonFailsInsteadOfSilentlyAcquiringAnotherToken()
        {
            // Arrange
            var l2 = new CountingDistributedCache();
            using var provider = new SnapshotTokenCacheProvider(l2, enableSnapshots: true);
            using var endpoint = new TokenEndpoint();
            string key = $"{TokenEndpoint.ClientId}_{TokenEndpoint.TenantA}_AppTokenCache";
            l2.Set(key, Encoding.UTF8.GetBytes("{invalid"), new DistributedCacheEntryOptions());

            // Act
            var error = await Assert.ThrowsAsync<MsalClientException>(() =>
                endpoint.CreateApplication(provider).AcquireTokenForClient(new[] { TokenEndpoint.Scope }).ExecuteAsync());

            // Assert
            Assert.Equal(MsalError.JsonParseError, error.ErrorCode);
            Assert.Equal(0, endpoint.TokenRequests);
            Assert.Equal(0, provider.Captures);
            Assert.Equal(0, provider.JsonSerializations);
            Assert.Equal(0, provider.L1.Count);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task OneWorkingCacheCanSwitchTenantPartitionsWithoutAliasingSnapshots(bool distributed)
        {
            // Arrange
            using var provider = new SnapshotTokenCacheProvider(distributed ? new CountingDistributedCache() : null, enableSnapshots: true);
            using var endpoint = new TokenEndpoint();
            var app = endpoint.CreateApplication(provider);

            // Act
            var tenantA = await app.AcquireTokenForClient(new[] { TokenEndpoint.Scope }).WithTenantId(TokenEndpoint.TenantA).ExecuteAsync();
            var tenantB = await app.AcquireTokenForClient(new[] { TokenEndpoint.Scope }).WithTenantId(TokenEndpoint.TenantB).ExecuteAsync();
            var tenantAAgain = await app.AcquireTokenForClient(new[] { TokenEndpoint.Scope }).WithTenantId(TokenEndpoint.TenantA).ExecuteAsync();

            // Assert
            Assert.Equal(TokenSource.Cache, tenantAAgain.AuthenticationResultMetadata.TokenSource);
            Assert.Equal(tenantA.AccessToken, tenantAAgain.AccessToken);
            Assert.NotEqual(tenantB.AccessToken, tenantAAgain.AccessToken);
            Assert.Equal(TokenEndpoint.TenantA, tenantAAgain.TenantId);
            Assert.Equal(2, endpoint.TokenRequests);
            Assert.Equal(2, provider.L1.Count);
            Assert.Equal(2, provider.Captures);
            Assert.Equal(distributed ? 2 : 0, provider.JsonSerializations);
            Assert.Equal(0, provider.JsonDeserializations);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task UserCacheOboPartitionsAlsoRestoreWithoutCacheJson(bool distributed)
        {
            // Arrange
            using var provider = new SnapshotTokenCacheProvider(distributed ? new CountingDistributedCache() : null, enableSnapshots: true);
            using var endpoint = new TokenEndpoint();
            var app = endpoint.CreateApplication(provider);
            var assertionA = new UserAssertion("synthetic-assertion-a");
            var assertionB = new UserAssertion("synthetic-assertion-b");

            // Act
            var first = await app.AcquireTokenOnBehalfOf(new[] { TokenEndpoint.Scope }, assertionA).ExecuteAsync();
            await app.AcquireTokenOnBehalfOf(new[] { TokenEndpoint.Scope }, assertionB).ExecuteAsync();
            var restored = await endpoint.CreateApplication(provider).AcquireTokenOnBehalfOf(new[] { TokenEndpoint.Scope }, assertionA).ExecuteAsync();

            // Assert
            Assert.Equal(TokenSource.Cache, restored.AuthenticationResultMetadata.TokenSource);
            Assert.Equal(first.AccessToken, restored.AccessToken);
            Assert.Equal(2, endpoint.TokenRequests);
            Assert.Equal(2, provider.L1.Count);
            Assert.Equal(distributed ? 2 : 0, provider.JsonSerializations);
            Assert.Equal(0, provider.JsonDeserializations);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ClearingPartitionRemovesSnapshotAndL2AndNextAcquisitionMisses(bool distributed)
        {
            // Arrange
            var l2 = distributed ? new CountingDistributedCache() : null;
            using var provider = new SnapshotTokenCacheProvider(l2, enableSnapshots: true);
            using var endpoint = new TokenEndpoint();
            await endpoint.CreateApplication(provider).AcquireTokenForClient(new[] { TokenEndpoint.Scope }).ExecuteAsync();
            string key = provider.LastKey!;

            // Act
            await provider.ClearAsync(key);

            // Assert
            Assert.Null(provider.L1.Get(key));
            Assert.Null(l2?.Get(key));

            // Act
            var next = await endpoint.CreateApplication(provider).AcquireTokenForClient(new[] { TokenEndpoint.Scope }).ExecuteAsync();

            // Assert
            Assert.Equal(TokenSource.IdentityProvider, next.AuthenticationResultMetadata.TokenSource);
            Assert.Equal(2, endpoint.TokenRequests);
        }
    }
}
