// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Text;
using System.Text.Json;
using Microsoft.Identity.Web.Test.Common;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    public class ClientInfoTests
    {
        private const string Uid = TestConstants.Uid;
        private const string Utid = TestConstants.Utid;
        private string _decodedJson = $"{{\"uid\":\"{Uid}\",\"utid\":\"{Utid}\"}}";
        private string _decodedEmptyJson = "{}";
        private string _invalidJson = $"{{\"uid\":\"{Uid}\",\"utid\":\"{Utid}\"";

        [Fact]
        public void CreateFromJson_ValidJson_ReturnsClientInfo()
        {
            var clientInfoResult = ClientInfo.CreateFromJson(Base64UrlHelpers.Encode(_decodedJson));

            Assert.NotNull(clientInfoResult);
            Assert.Equal(Uid, clientInfoResult.UniqueObjectIdentifier);
            Assert.Equal(Utid, clientInfoResult.UniqueTenantIdentifier);

            clientInfoResult = ClientInfo.CreateFromJson(Base64UrlHelpers.Encode(_decodedEmptyJson));
            Assert.NotNull(clientInfoResult);
            Assert.Null(clientInfoResult.UniqueObjectIdentifier);
            Assert.Null(clientInfoResult.UniqueTenantIdentifier);
        }

        [Fact]
        public void CreateFromJson_NullOrEmptyString_ThrowsException()
        {
            var expectedErrorMessage = "client info returned from the server is null (Parameter 'clientInfo')";

            var exception = Assert.Throws<ArgumentNullException>(() => ClientInfo.CreateFromJson(string.Empty));
            Assert.Equal(expectedErrorMessage, exception.Message);

            exception = Assert.Throws<ArgumentNullException>(() => ClientInfo.CreateFromJson(null));
            Assert.Equal(expectedErrorMessage, exception.Message);
        }

        [Fact]
        public void CreateFromJson_InvalidString_ThrowsException()
        {
            Assert.Throws<JsonException>(() => ClientInfo.CreateFromJson(Base64UrlHelpers.Encode(_invalidJson)));

            Assert.Throws<ArgumentException>(() => ClientInfo.CreateFromJson(_invalidJson));
        }

        [Fact]
        public void DeserializeFromJson_ValidByteArray_ReturnsClientInfo()
        {
            var clientInfoResult = ClientInfo.DeserializeFromJson(Encoding.UTF8.GetBytes(_decodedJson));

            Assert.NotNull(clientInfoResult);
            Assert.Equal(Uid, clientInfoResult.UniqueObjectIdentifier);
            Assert.Equal(Utid, clientInfoResult.UniqueTenantIdentifier);

            clientInfoResult = ClientInfo.DeserializeFromJson(Encoding.UTF8.GetBytes(_decodedEmptyJson));
            Assert.NotNull(clientInfoResult);
            Assert.Null(clientInfoResult.UniqueObjectIdentifier);
            Assert.Null(clientInfoResult.UniqueTenantIdentifier);
        }

        [Fact]
        public void DeserializeFromJson_NullOrEmptyJsonByteArray_ReturnsNull()
        {
            var actualClientInfo = ClientInfo.DeserializeFromJson(Array.Empty<byte>());

            Assert.Null(actualClientInfo);

            actualClientInfo = ClientInfo.DeserializeFromJson(null);

            Assert.Null(actualClientInfo);
        }

        [Fact]
        public void DeserializeFromJson_InvalidJsonByteArray_ReturnsNull()
        {
            Assert.Throws<JsonException>(() => ClientInfo.DeserializeFromJson(Encoding.UTF8.GetBytes(_invalidJson)));
        }
    }
}
