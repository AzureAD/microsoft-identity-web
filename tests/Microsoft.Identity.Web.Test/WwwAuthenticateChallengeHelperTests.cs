// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    public class WwwAuthenticateChallengeHelperTests
    {
        private const string CaeClaims = "{\\\"access_token\\\":{\\\"capolids\\\":{\\\"essential\\\":true,\\\"values\\\":[\\\"c1\\\"]}}}";

        [Fact]
        public void ExtractClaimsChallenge_WithValidClaimsChallenge_ReturnsClaimsString()
        {
            // Arrange
            string challengeB64 = Base64UrlEncoder.Encode(Encoding.UTF8.GetBytes(CaeClaims));
            
            var response = new HttpResponseMessage(HttpStatusCode.Unauthorized);
            response.Headers.WwwAuthenticate.ParseAdd(
                $"Bearer realm=\"\", error=\"insufficient_claims\", " +
                $"error_description=\"token requires claims\", " +
                $"claims=\"{challengeB64}\"");

            // Act
            string? claims = WwwAuthenticateChallengeHelper.ExtractClaimsChallenge(response.Headers);

            // Assert
            Assert.NotNull(claims);
            Assert.Equal(challengeB64, claims);
        }

        [Fact]
        public void ExtractClaimsChallenge_WithoutClaimsChallenge_ReturnsNull()
        {
            // Arrange
            var response = new HttpResponseMessage(HttpStatusCode.Unauthorized);
            response.Headers.WwwAuthenticate.ParseAdd("Bearer realm=\"\"");

            // Act
            string? claims = WwwAuthenticateChallengeHelper.ExtractClaimsChallenge(response.Headers);

            // Assert
            Assert.Null(claims);
        }

        [Fact]
        public void ExtractClaimsChallenge_WithEmptyHeaders_ReturnsNull()
        {
            // Arrange
            var response = new HttpResponseMessage(HttpStatusCode.Unauthorized);

            // Act
            string? claims = WwwAuthenticateChallengeHelper.ExtractClaimsChallenge(response.Headers);

            // Assert
            Assert.Null(claims);
        }

        [Fact]
        public async Task CloneHttpContentAsync_WithNullContent_ReturnsNull()
        {
            // Act
            var clonedContent = await WwwAuthenticateChallengeHelper.CloneHttpContentAsync(null);

            // Assert
            Assert.Null(clonedContent);
        }

        [Fact]
        public async Task CloneHttpContentAsync_WithStringContent_ClonesContentAndHeaders()
        {
            // Arrange
            string originalText = "Hello, World!";
            var originalContent = new StringContent(originalText, Encoding.UTF8, "application/json");

            // Act
            var clonedContent = await WwwAuthenticateChallengeHelper.CloneHttpContentAsync(originalContent);

            // Assert
            Assert.NotNull(clonedContent);
            
            // Verify content is the same
            string clonedText = await clonedContent!.ReadAsStringAsync();
            Assert.Equal(originalText, clonedText);

            // Verify headers are copied
            Assert.Equal(originalContent.Headers.ContentType?.MediaType, clonedContent.Headers.ContentType?.MediaType);
            Assert.Equal(originalContent.Headers.ContentType?.CharSet, clonedContent.Headers.ContentType?.CharSet);
        }

        [Fact]
        public async Task CloneHttpContentAsync_WithByteArrayContent_ClonesContent()
        {
            // Arrange
            byte[] originalBytes = Encoding.UTF8.GetBytes("Test data");
            var originalContent = new ByteArrayContent(originalBytes);
            originalContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");

            // Act
            var clonedContent = await WwwAuthenticateChallengeHelper.CloneHttpContentAsync(originalContent);

            // Assert
            Assert.NotNull(clonedContent);
            
            // Verify content is the same
            byte[] clonedBytes = await clonedContent!.ReadAsByteArrayAsync();
            Assert.Equal(originalBytes, clonedBytes);

            // Verify headers are copied
            Assert.Equal("application/octet-stream", clonedContent.Headers.ContentType?.MediaType);
        }

        [Fact]
        public async Task CloneHttpContentAsync_CanBeUsedMultipleTimes()
        {
            // Arrange
            string originalText = "Reusable content";
            var originalContent = new StringContent(originalText, Encoding.UTF8, "text/plain");

            // Act - Clone the content
            var clonedContent = await WwwAuthenticateChallengeHelper.CloneHttpContentAsync(originalContent);

            // Read the cloned content multiple times to verify it's reusable
            string firstRead = await clonedContent!.ReadAsStringAsync();
            
            // Create another clone from the first clone
            var secondClone = await WwwAuthenticateChallengeHelper.CloneHttpContentAsync(clonedContent);
            string secondRead = await secondClone!.ReadAsStringAsync();

            // Assert
            Assert.Equal(originalText, firstRead);
            Assert.Equal(originalText, secondRead);
        }

        [Fact]
        public void ShouldAttemptClaimsChallengeRetry_With401Response_ReturnsTrue()
        {
            // Arrange
            var response = new HttpResponseMessage(HttpStatusCode.Unauthorized);

            // Act
            bool shouldRetry = WwwAuthenticateChallengeHelper.ShouldAttemptClaimsChallengeRetry(response);

            // Assert
            Assert.True(shouldRetry);
        }

        [Theory]
        [InlineData(HttpStatusCode.OK)]
        [InlineData(HttpStatusCode.BadRequest)]
        [InlineData(HttpStatusCode.Forbidden)]
        [InlineData(HttpStatusCode.NotFound)]
        [InlineData(HttpStatusCode.InternalServerError)]
        public void ShouldAttemptClaimsChallengeRetry_WithNon401Response_ReturnsFalse(HttpStatusCode statusCode)
        {
            // Arrange
            var response = new HttpResponseMessage(statusCode);

            // Act
            bool shouldRetry = WwwAuthenticateChallengeHelper.ShouldAttemptClaimsChallengeRetry(response);

            // Assert
            Assert.False(shouldRetry);
        }

        [Fact]
        public async Task CloneHttpContentAsync_WithCustomHeaders_CopiesAllHeaders()
        {
            // Arrange
            var originalContent = new ByteArrayContent(Encoding.UTF8.GetBytes("data"));
            originalContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
            originalContent.Headers.Add("X-Custom-Header", "CustomValue");
            originalContent.Headers.ContentEncoding.Add("gzip");

            // Act
            var clonedContent = await WwwAuthenticateChallengeHelper.CloneHttpContentAsync(originalContent);

            // Assert
            Assert.NotNull(clonedContent);
            Assert.Equal("application/json", clonedContent!.Headers.ContentType?.MediaType);
            Assert.Contains("CustomValue", clonedContent.Headers.GetValues("X-Custom-Header"));
            Assert.Contains("gzip", clonedContent.Headers.ContentEncoding);
        }
    }
}
