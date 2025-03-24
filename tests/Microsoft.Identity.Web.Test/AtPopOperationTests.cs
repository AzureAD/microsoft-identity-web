// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    public class AtPopOperationTests
    {
        [Fact]
        public void Constructor_InitializesProperties()
        {
            // Arrange
            string keyId = "testKeyId";
            string reqCnf = "testReqCnf";

            // Act
            var atPopOperation = new AtPopOperation(keyId, reqCnf);

            // Assert
            Assert.Equal(keyId, atPopOperation.KeyId);
            Assert.Equal(4, atPopOperation.TelemetryTokenType);
            Assert.Equal("Bearer", atPopOperation.AuthorizationHeaderPrefix);
            Assert.Equal("pop", atPopOperation.AccessTokenType);
        }

        [Fact]
        public void GetTokenRequestParams_ReturnsCorrectDictionary()
        {
            // Arrange
            string reqCnf = "testReqCnf";
            var atPopOperation = new AtPopOperation("testKeyId", reqCnf);

            // Act
            var tokenRequestParams = atPopOperation.GetTokenRequestParams();

            // Assert
            Assert.Equal(2, tokenRequestParams.Count);
            Assert.Equal(Base64UrlEncoder.Encode(reqCnf), tokenRequestParams["req_cnf"]);
            Assert.Equal("pop", tokenRequestParams["token_type"]);
        }
    }
}
