// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Web.InstanceDiscovery;
using Microsoft.IdentityModel.Protocols;
using NSubstitute;
using Xunit;

namespace Microsoft.Identity.Web.IssuerValidator.Test
{
    public class IssuerConfigurationRetrieverTests
    {
        [Fact]
        public async Task GetConfigurationAsync_NullOrEmptyParameters_ThrowsException()
        {
            var configurationRetriever = new IssuerConfigurationRetriever();

            string expectedErrorMessage = IssuerValidatorErrorMessage.IssuerMetadataUrlIsRequired + " (Parameter 'address')";

            var exception = await Assert.ThrowsAsync<ArgumentNullException>("address", () => configurationRetriever.GetConfigurationAsync(null, null, CancellationToken.None)).ConfigureAwait(false);

#if DOTNET_462 || DOTNET_472
            string netFrameworkErrorMessage = "IDW10301: Azure AD Issuer metadata address URL is required. \r\nParameter name: address";
            Assert.Equal(netFrameworkErrorMessage, exception.Message);
#else
            Assert.Equal(expectedErrorMessage, exception.Message);
#endif
            exception = await Assert.ThrowsAsync<ArgumentNullException>("address", () => configurationRetriever.GetConfigurationAsync(string.Empty, null, CancellationToken.None)).ConfigureAwait(false);
#if DOTNET_462 || DOTNET_472
            Assert.Equal(netFrameworkErrorMessage, exception.Message);
#else
            Assert.Equal(expectedErrorMessage, exception.Message);
#endif

            exception = await Assert.ThrowsAsync<ArgumentNullException>("retriever", () => configurationRetriever.GetConfigurationAsync("address", null, CancellationToken.None)).ConfigureAwait(false);
#if DOTNET_462 || DOTNET_472
            netFrameworkErrorMessage = "IDW10302: No metadata document retriever is provided. \r\nParameter name: retriever";
            Assert.Equal(netFrameworkErrorMessage, exception.Message);
#else
            Assert.Equal(IssuerValidatorErrorMessage.NoMetadataDocumentRetrieverProvided + " (Parameter 'retriever')", exception.Message);
#endif
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
