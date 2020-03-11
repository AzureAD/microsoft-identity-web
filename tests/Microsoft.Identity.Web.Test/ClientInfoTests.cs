// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Text;
using Microsoft.Identity.Web.Test.Common;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    public class ClientInfoTests
    {
        [Fact]
        public void CreateFromJson_ValidJson_ReturnsClientInfo()
        {
            var decodedJson = $"{{\"uid\":\"{TestConstants.Uid}\",\"utid\":\"{TestConstants.Utid}\"}}";
            var clientInfoResult = ClientInfo.CreateFromJson(Base64UrlHelpers.Encode(decodedJson));

            Assert.NotNull(clientInfoResult);
            Assert.Equal(TestConstants.Uid, clientInfoResult.UniqueObjectIdentifier);
            Assert.Equal(TestConstants.Utid, clientInfoResult.UniqueTenantIdentifier);

            var decodedEmptyJson = "{}";

            clientInfoResult = ClientInfo.CreateFromJson(Base64UrlHelpers.Encode(decodedEmptyJson));
            Assert.NotNull(clientInfoResult);
            Assert.Null(clientInfoResult.UniqueObjectIdentifier);
            Assert.Null(clientInfoResult.UniqueTenantIdentifier);
        }

        [Fact]
        public void CreateFromJson_NullOrEmptyString_ThrowsException()
        {
            var expectedErrorMessage = "client info returned from the server is null (Parameter 'clientInfo')";

            var exception = Assert.Throws<ArgumentNullException>(() => ClientInfo.CreateFromJson(""));
            Assert.Equal(expectedErrorMessage, exception.Message);

            exception = Assert.Throws<ArgumentNullException>(() => ClientInfo.CreateFromJson(null));
            Assert.Equal(expectedErrorMessage, exception.Message);
        }

        [Fact]
        public void CreateFromJson_InvalidString_ThrowsException()
        {
            var invalidJson = $"{{\"uid\":\"{TestConstants.Uid}\",\"utid\":\"{TestConstants.Utid}\"";

            Assert.Throws<Newtonsoft.Json.JsonSerializationException>(() => ClientInfo.CreateFromJson(Base64UrlHelpers.Encode(invalidJson)));

            Assert.Throws<FormatException>(() => ClientInfo.CreateFromJson(invalidJson));
        }

        [Fact]
        public void DeserializeFromJson_ValidByteArray_ReturnsClientInfo()
        {
            var decodedJson = $"{{\"uid\":\"{TestConstants.Uid}\",\"utid\":\"{TestConstants.Utid}\"}}";
            var clientInfoResult = ClientInfo.DeserializeFromJson<ClientInfo>(Encoding.UTF8.GetBytes(decodedJson));

            Assert.NotNull(clientInfoResult);
            Assert.Equal(TestConstants.Uid, clientInfoResult.UniqueObjectIdentifier);
            Assert.Equal(TestConstants.Utid, clientInfoResult.UniqueTenantIdentifier);

            var decodedEmptyJson = "{}";

            clientInfoResult = ClientInfo.DeserializeFromJson<ClientInfo>(Encoding.UTF8.GetBytes(decodedEmptyJson));
            Assert.NotNull(clientInfoResult);
            Assert.Null(clientInfoResult.UniqueObjectIdentifier);
            Assert.Null(clientInfoResult.UniqueTenantIdentifier);
        }

        [Fact]
        public void DeserializeFromJson_NullOrEmptyJsonByteArray_ReturnsNull()
        {
            var actualClientInfo = ClientInfo.DeserializeFromJson<ClientInfo>(new byte[0]);

            Assert.Null(actualClientInfo);

            actualClientInfo = ClientInfo.DeserializeFromJson<ClientInfo>(null);
            
            Assert.Null(actualClientInfo);
        }

        [Fact]
        public void DeserializeFromJson_InvalidJsonByteArray_ReturnsNull()
        {
            var invalidJson = $"{{\"uid\":\"{TestConstants.Uid}\",\"utid\":\"{TestConstants.Utid}\"";

            Assert.Throws<Newtonsoft.Json.JsonSerializationException>(() => ClientInfo.DeserializeFromJson<ClientInfo>(Encoding.UTF8.GetBytes(invalidJson)));
        }
    }
}
