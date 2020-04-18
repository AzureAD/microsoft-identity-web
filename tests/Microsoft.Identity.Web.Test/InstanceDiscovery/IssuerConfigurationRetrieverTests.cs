// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Web.InstanceDiscovery;
using Microsoft.IdentityModel.Protocols;
using NSubstitute;
using Xunit;

namespace Microsoft.Identity.Web.Test.InstanceDiscovery
{
    public class IssuerConfigurationRetrieverTests
    {
        [Fact]
        public async Task GetConfigurationAsync_NullOrEmptyParameters_ThrowsException()
        {
            var configurationRetriever = new IssuerConfigurationRetriever();
            var expectedAddressExceptionMessage = "Azure AD Issuer metadata address url is required (Parameter 'address')";
            var expectedRetrieverExceptionMessage = "No metadata document retriever is provided (Parameter 'retriever')";

            var exception = await Assert.ThrowsAsync<ArgumentNullException>("address", () => configurationRetriever.GetConfigurationAsync(null, null, CancellationToken.None)).ConfigureAwait(false);
            Assert.Equal(expectedAddressExceptionMessage, exception.Message);

            exception = await Assert.ThrowsAsync<ArgumentNullException>("address", () => configurationRetriever.GetConfigurationAsync(string.Empty, null, CancellationToken.None)).ConfigureAwait(false);
            Assert.Equal(expectedAddressExceptionMessage, exception.Message);

            exception = await Assert.ThrowsAsync<ArgumentNullException>("retriever", () => configurationRetriever.GetConfigurationAsync("address", null, CancellationToken.None)).ConfigureAwait(false);
            Assert.Equal(expectedRetrieverExceptionMessage, exception.Message);
        }

        [Fact]
        public async Task GetConfigurationAsync_ValidParameters_ReturnsIssuerMetadata()
        {
            var metadata = @"{""tenant_discovery_endpoint"":""https://login.microsoftonline.com/common/v2.0/.well-known/openid-configuration"",""api-version"":""1.1"",""metadata"":[{""preferred_network"":""login.microsoftonline.com"",""preferred_cache"":""login.windows.net"",""aliases"":[""login.microsoftonline.com""]}]}";
            var metadataAddress = "address";
            var expectedIssuerMetadata = MakeIssuerMetadata();

            var configurationRetriever = new IssuerConfigurationRetriever();
            var documentRetriever = Substitute.For<IDocumentRetriever>();
            documentRetriever.GetDocumentAsync(metadataAddress, CancellationToken.None).Returns(Task.FromResult(metadata));

            var actualIssuerMetadata = await configurationRetriever.GetConfigurationAsync(metadataAddress, documentRetriever, CancellationToken.None).ConfigureAwait(false);

            Assert.Equal(expectedIssuerMetadata, actualIssuerMetadata, new IssuerMetadataComparer());
        }

        private IssuerMetadata MakeIssuerMetadata()
        {
            return new IssuerMetadata
            {
                TenantDiscoveryEndpoint = "https://login.microsoftonline.com/common/v2.0/.well-known/openid-configuration",
                ApiVersion = "1.1",
                Metadata = new List<Metadata>
                {
                    new Metadata()
                    {
                        PreferredNetwork = "login.microsoftonline.com",
                        PreferredCache = "login.windows.net",
                        Aliases = new List<string>
                        {
                            "login.microsoftonline.com"
                        }
                    }
                }
            };
        }

        private class IssuerMetadataComparer : IEqualityComparer<IssuerMetadata>
        {
            public bool Equals([AllowNull] IssuerMetadata x, [AllowNull] IssuerMetadata y)
            {
                if (x == null && y == null)
                {
                    return true;
                }
                else if (x == null || y == null)
                {
                    return false;
                }

                return x.TenantDiscoveryEndpoint == y.TenantDiscoveryEndpoint &&
                        x.ApiVersion == y.ApiVersion &&
                        x.Metadata.SequenceEqual(y.Metadata, new MetadataComparer());
            }

            public int GetHashCode([DisallowNull] IssuerMetadata obj) => throw new NotImplementedException();
        }

        private class MetadataComparer : IEqualityComparer<Metadata>
        {
            public bool Equals([AllowNull] Metadata x, [AllowNull] Metadata y)
            {
                if (x == null && y == null)
                {
                    return true;
                }
                else if (x == null || y == null)
                {
                    return false;
                }

                return x.PreferredNetwork == y.PreferredNetwork &&
                    x.PreferredCache == y.PreferredCache &&
                    x.Aliases.SequenceEqual(y.Aliases);
            }

            public int GetHashCode([DisallowNull] Metadata obj) => throw new NotImplementedException();
        }
    }
}
