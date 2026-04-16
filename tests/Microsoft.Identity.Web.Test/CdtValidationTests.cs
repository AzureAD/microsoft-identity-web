// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Identity.Abstractions;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    public class CdtValidationTests
    {
        /// <summary>
        /// Creates a minimal unsigned JWT token with the given payload claims.
        /// Format: base64url(header).base64url(payload).signature
        /// </summary>
        private static string CreateTestJwt(Dictionary<string, object> claims)
        {
            var header = "{\"alg\":\"none\",\"typ\":\"JWT\"}";
            var payloadBuilder = new StringBuilder("{");
            bool first = true;
            foreach (var claim in claims)
            {
                if (!first)
                {
                    payloadBuilder.Append(',');
                }

                payloadBuilder.Append('"').Append(claim.Key).Append("\":");
                if (claim.Value is string s)
                {
                    payloadBuilder.Append('"').Append(s).Append('"');
                }
                else
                {
                    payloadBuilder.Append(claim.Value);
                }

                first = false;
            }

            payloadBuilder.Append('}');

            var headerB64 = Base64UrlEncode(header);
            var payloadB64 = Base64UrlEncode(payloadBuilder.ToString());
            return $"{headerB64}.{payloadB64}.";
        }

        private static string Base64UrlEncode(string input)
        {
            var bytes = Encoding.UTF8.GetBytes(input);
            return Convert.ToBase64String(bytes)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }

        #region IsCdt Tests

        [Fact]
        public void IsCdt_TokenWithBothTAndCClaims_ReturnsTrue()
        {
            // Arrange
            var cdtToken = CreateTestJwt(new Dictionary<string, object>
            {
                { "t", "some_inner_token" },
                { "c", "some_context" }
            });

            // Act
            bool result = TokenAcquisition.IsCdt(cdtToken);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsCdt_TokenWithOnlyTClaim_ReturnsFalse()
        {
            // Arrange
            var token = CreateTestJwt(new Dictionary<string, object>
            {
                { "t", "some_inner_token" },
                { "sub", "user123" }
            });

            // Act
            bool result = TokenAcquisition.IsCdt(token);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsCdt_TokenWithOnlyCClaim_ReturnsFalse()
        {
            // Arrange
            var token = CreateTestJwt(new Dictionary<string, object>
            {
                { "c", "some_context" },
                { "sub", "user123" }
            });

            // Act
            bool result = TokenAcquisition.IsCdt(token);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsCdt_RegularJwtToken_ReturnsFalse()
        {
            // Arrange
            var token = CreateTestJwt(new Dictionary<string, object>
            {
                { "sub", "user123" },
                { "name", "Test User" }
            });

            // Act
            bool result = TokenAcquisition.IsCdt(token);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsCdt_InvalidTokenString_ReturnsFalse()
        {
            // Arrange
            var invalidToken = "not-a-jwt-token";

            // Act
            bool result = TokenAcquisition.IsCdt(invalidToken);

            // Assert
            Assert.False(result);
        }

        #endregion

        #region ThrowIfCdtMissingFromExtraParameters Tests

        [Fact]
        public void ThrowIfCdtMissingFromExtraParameters_CdtTokenWithoutCdtInExtraParameters_Throws()
        {
            // Arrange
            var cdtToken = CreateTestJwt(new Dictionary<string, object>
            {
                { "t", "some_inner_token" },
                { "c", "some_context" }
            });
            var options = new TokenAcquisitionOptions
            {
                ExtraParameters = new Dictionary<string, object>()
            };

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(
                () => TokenAcquisition.ThrowIfCdtMissingFromExtraParameters(cdtToken, options));
            Assert.Contains("IDW10506", exception.Message, StringComparison.Ordinal);
            Assert.Contains("Cdt", exception.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void ThrowIfCdtMissingFromExtraParameters_CdtTokenWithNullOptions_Throws()
        {
            // Arrange
            var cdtToken = CreateTestJwt(new Dictionary<string, object>
            {
                { "t", "some_inner_token" },
                { "c", "some_context" }
            });

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(
                () => TokenAcquisition.ThrowIfCdtMissingFromExtraParameters(cdtToken, null));
            Assert.Contains("IDW10506", exception.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void ThrowIfCdtMissingFromExtraParameters_CdtTokenWithNullExtraParameters_Throws()
        {
            // Arrange
            var cdtToken = CreateTestJwt(new Dictionary<string, object>
            {
                { "t", "some_inner_token" },
                { "c", "some_context" }
            });
            var options = new TokenAcquisitionOptions();

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(
                () => TokenAcquisition.ThrowIfCdtMissingFromExtraParameters(cdtToken, options));
            Assert.Contains("IDW10506", exception.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void ThrowIfCdtMissingFromExtraParameters_CdtTokenWithCdtInExtraParameters_DoesNotThrow()
        {
            // Arrange
            var cdtToken = CreateTestJwt(new Dictionary<string, object>
            {
                { "t", "some_inner_token" },
                { "c", "some_context" }
            });
            var options = new TokenAcquisitionOptions
            {
                ExtraParameters = new Dictionary<string, object>
                {
                    { "Cdt", "cached_cdt_value" }
                }
            };

            // Act & Assert (no exception expected)
            TokenAcquisition.ThrowIfCdtMissingFromExtraParameters(cdtToken, options);
        }

        [Fact]
        public void ThrowIfCdtMissingFromExtraParameters_NonCdtToken_DoesNotThrow()
        {
            // Arrange
            var regularToken = CreateTestJwt(new Dictionary<string, object>
            {
                { "sub", "user123" },
                { "name", "Test User" }
            });
            var options = new TokenAcquisitionOptions
            {
                ExtraParameters = new Dictionary<string, object>()
            };

            // Act & Assert (no exception expected)
            TokenAcquisition.ThrowIfCdtMissingFromExtraParameters(regularToken, options);
        }

        [Fact]
        public void ThrowIfCdtMissingFromExtraParameters_NonCdtTokenWithNullOptions_DoesNotThrow()
        {
            // Arrange
            var regularToken = CreateTestJwt(new Dictionary<string, object>
            {
                { "sub", "user123" },
                { "name", "Test User" }
            });

            // Act & Assert (no exception expected)
            TokenAcquisition.ThrowIfCdtMissingFromExtraParameters(regularToken, null);
        }

        [Fact]
        public void ThrowIfCdtMissingFromExtraParameters_InvalidToken_DoesNotThrow()
        {
            // Arrange
            var invalidToken = "not-a-jwt-token";

            // Act & Assert (no exception expected - invalid tokens are not CDTs)
            TokenAcquisition.ThrowIfCdtMissingFromExtraParameters(invalidToken, null);
        }

        #endregion
    }
}
