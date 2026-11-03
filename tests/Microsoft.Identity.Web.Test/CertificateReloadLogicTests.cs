// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Reflection;
using Microsoft.Identity.Client;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    /// <summary>
    /// Tests for the certificate reload logic to ensure it only triggers on certificate-related errors.
    /// This addresses the regression from PR #3430 where reload was triggered on all invalid_client errors.
    /// </summary>
    public class CertificateReloadLogicTests
    {
        private const string InvalidClientErrorCode = "invalid_client";

        [Theory]
        [InlineData("AADSTS700027", true)]  // InvalidKeyError
        [InlineData("AADSTS700024", true)]  // SignedAssertionInvalidTimeRange
        [InlineData("AADSTS7000214", true)] // CertificateHasBeenRevoked
        [InlineData("AADSTS1000502", true)] // CertificateIsOutsideValidityWindow
        [InlineData("AADSTS7000274", true)] // ClientAssertionContainsInvalidSignature
        [InlineData("AADSTS7000277", true)] // CertificateWasRevoked
        [InlineData("AADSTS7000215", false)] // Invalid client secret - should NOT trigger reload
        [InlineData("AADSTS700016", false)]  // Application not found - should NOT trigger reload
        [InlineData("AADSTS7000222", false)] // Invalid client secret (expired) - should NOT trigger reload
        [InlineData("AADSTS50011", false)]   // Invalid reply address - should NOT trigger reload
        [InlineData("AADSTS50012", false)]   // Invalid client credentials - should NOT trigger reload
        public void ShouldRetry_ReturnsTrueOnlyForCertificateErrors(
            string errorCode,
            bool shouldTriggerReload)
        {
            // Arrange
            var retryPolicy = new DefaultAuthenticationRetryPolicy();
            var responseBody = $"{{\"error\":\"invalid_client\",\"error_description\":\"Error {errorCode}: Test error\"}}";
            var exception = CreateMsalServiceException(InvalidClientErrorCode, responseBody);

            // Act
            bool result = retryPolicy.ShouldRetry(new AuthenticationRetryContext { ThrownException = exception });

            // Assert
            Assert.Equal(shouldTriggerReload, result);
        }

        [Fact]
        public void ShouldRetry_ReturnsFalseWhenErrorCodeIsNotInvalidClient()
        {
            // Arrange
            var retryPolicy = new DefaultAuthenticationRetryPolicy();
            var responseBody = "{\"error\":\"unauthorized_client\",\"error_description\":\"Test error\"}";
            var exception = CreateMsalServiceException("unauthorized_client", responseBody);

            // Act
            bool result = retryPolicy.ShouldRetry(new AuthenticationRetryContext { ThrownException = exception });

            // Assert
            Assert.False(result, "Should not trigger reload for non-invalid_client errors");
        }

        [Fact]
        public void ShouldRetry_ReturnsFalseForEmptyResponseBody()
        {
            // Arrange
            var retryPolicy = new DefaultAuthenticationRetryPolicy();
            var exception = CreateMsalServiceException(InvalidClientErrorCode, string.Empty);

            // Act
            bool result = retryPolicy.ShouldRetry(new AuthenticationRetryContext { ThrownException = exception });

            // Assert
            Assert.False(result, "Should not trigger reload when response body is empty");
        }

        [Theory]
        [InlineData("AADSTS700027")]  // Case sensitive check - should still work
        [InlineData("aadsts700027")]  // Lowercase
        [InlineData("AaDsTs700027")]  // Mixed case
        public void ShouldRetry_IsCaseInsensitive(string errorCodeCase)
        {
            // Arrange
            var retryPolicy = new DefaultAuthenticationRetryPolicy();
            var responseBody = $"{{\"error\":\"invalid_client\",\"error_description\":\"Error {errorCodeCase}: Test error\"}}";
            var exception = CreateMsalServiceException(InvalidClientErrorCode, responseBody);

            // Act
            bool result = retryPolicy.ShouldRetry(new AuthenticationRetryContext { ThrownException = exception });

            // Assert
            Assert.True(result, $"Should trigger reload regardless of case: {errorCodeCase}");
        }

        [Fact]
        public void ShouldRetry_WorksWithMultipleErrorCodesInResponse()
        {
            // Arrange
            var retryPolicy = new DefaultAuthenticationRetryPolicy();
            // Response might contain multiple error codes or descriptions
            var responseBody = $"{{\"error\":\"invalid_client\",\"error_description\":\"Error {Constants.CertificateHasBeenRevoked}: Certificate has been revoked. Also note AADSTS7000215 in logs.\"}}";
            var exception = CreateMsalServiceException(InvalidClientErrorCode, responseBody);

            // Act
            bool result = retryPolicy.ShouldRetry(new AuthenticationRetryContext { ThrownException = exception });

            // Assert
            Assert.True(result, "Should trigger reload when certificate error code is present, even if other codes are also mentioned");
        }

        /// <summary>
        /// Creates a MsalServiceException for testing.
        /// </summary>
        private MsalServiceException CreateMsalServiceException(string errorCode, string responseBody)
        {
            // Use the MsalServiceException constructor with errorCode and errorMessage
            // The ResponseBody property is internal but can be accessed via reflection
            var exception = new MsalServiceException(errorCode, $"Test exception: {errorCode}");

            // Set the ResponseBody property using reflection
            var responseBodyField = typeof(MsalServiceException).GetProperty("ResponseBody");
            if (responseBodyField != null && responseBodyField.CanWrite)
            {
                responseBodyField.SetValue(exception, responseBody);
            }
            else
            {
                // Try the backing field instead
                var backingField = typeof(MsalServiceException).GetField("<ResponseBody>k__BackingField",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                if (backingField != null)
                {
                    backingField.SetValue(exception, responseBody);
                }
            }

            return exception;
        }
    }
}
