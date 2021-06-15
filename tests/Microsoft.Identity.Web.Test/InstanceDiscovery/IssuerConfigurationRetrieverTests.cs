// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
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

            string expectedErrorMessage = IDWebErrorMessage.IssuerMetadataUrlIsRequired + " (Parameter 'address')";

            var exception = await Assert.ThrowsAsync<ArgumentNullException>("address", () => configurationRetriever.GetConfigurationAsync(null, null, CancellationToken.None)).ConfigureAwait(false);
            Assert.Equal(expectedErrorMessage, exception.Message);

            exception = await Assert.ThrowsAsync<ArgumentNullException>("address", () => configurationRetriever.GetConfigurationAsync(string.Empty, null, CancellationToken.None)).ConfigureAwait(false);
            Assert.Equal(expectedErrorMessage, exception.Message);

            exception = await Assert.ThrowsAsync<ArgumentNullException>("retriever", () => configurationRetriever.GetConfigurationAsync("address", null, CancellationToken.None)).ConfigureAwait(false);
            Assert.Equal(IDWebErrorMessage.NoMetadataDocumentRetrieverProvided + " (Parameter 'retriever')", exception.Message);
        }

        [Fact]
        public async Task GetConfigurationAsync_ValidParameters_ReturnsIssuerMetadata()
        {
            var metadata = @"{""tenant_discovery_endpoint"":""https://login.microsoftonline.com/common/v2.0/.well-known/openid-configuration"",""api-version"":""1.1"",""metadata"":[{""preferred_network"":""login.microsoftonline.com"",""preferred_cache"":""login.windows.net"",""aliases"":[""login.microsoftonline.com""]}]}";
            var metadataAddress = "address";

            var configurationRetriever = new IssuerConfigurationRetriever();
            var documentRetriever = Substitute.For<IDocumentRetriever>();
            documentRetriever.GetDocumentAsync(metadataAddress, CancellationToken.None).Returns(Task.FromResult(metadata));

            var actualIssuerMetadata = await configurationRetriever.GetConfigurationAsync(metadataAddress, documentRetriever, CancellationToken.None).ConfigureAwait(false);
        }
    }
}
