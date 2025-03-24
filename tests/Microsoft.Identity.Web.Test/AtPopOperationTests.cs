using Microsoft.Identity.Client;
using Microsoft.Identity.Web;
using Microsoft.IdentityModel.Tokens;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.Identity.Web.TokenAcquisition.Tests
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
