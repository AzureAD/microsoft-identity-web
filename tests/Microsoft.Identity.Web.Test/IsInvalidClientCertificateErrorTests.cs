// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web.Test.Common.TestHelpers;
using Microsoft.Identity.Web.TokenCacheProviders.InMemory;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    /// <summary>
    /// Unit tests for the IsInvalidClientCertificateOrSignedAssertionError method.
    /// This method should only return true for certificate-related errors, not for generic invalid_client errors.
    /// </summary>
    public class IsInvalidClientCertificateErrorTests
    {
        /// <summary>
        /// Test that the method returns true when the error message contains "AADSTS700027" (Invalid key error).
        /// This is a certificate-related error that should trigger a reload.
        /// </summary>
        [Fact]
        public void IsInvalidClientCertificateError_InvalidKeyError_ReturnsTrue()
        {
            // Arrange
            var exception = CreateMsalServiceException(
                Constants.InvalidClient,
                $"AADSTS700027: Client assertion contains an invalid signature. " +
                $"[Reason - The key was not found. Thumbprint of key used by client: 'ABC123']");

            // Act
            bool result = InvokeIsInvalidClientCertificateError(exception, retryCount: 0);

            // Assert
            Assert.True(result, "Should return true for AADSTS700027 (invalid key) error");
        }

        /// <summary>
        /// Test that the method returns true when the error message contains "AADSTS700024" (Signed assertion invalid time range).
        /// This is a certificate-related error that should trigger a reload.
        /// </summary>
        [Fact]
        public void IsInvalidClientCertificateError_SignedAssertionInvalidTimeRange_ReturnsTrue()
        {
            // Arrange
            var exception = CreateMsalServiceException(
                Constants.InvalidClient,
                "AADSTS700024: Client assertion is not within its valid time range. Current time: 2024-01-15");

            // Act
            bool result = InvokeIsInvalidClientCertificateError(exception, retryCount: 0);

            // Assert
            Assert.True(result, "Should return true for AADSTS700024 (signed assertion time range) error");
        }

        /// <summary>
        /// Test that the method returns true when the error message contains "AADSTS7000214" (Certificate has been revoked).
        /// This is a certificate-related error that should trigger a reload.
        /// </summary>
        [Fact]
        public void IsInvalidClientCertificateError_CertificateRevoked_ReturnsTrue()
        {
            // Arrange
            var exception = CreateMsalServiceException(
                Constants.InvalidClient,
                "AADSTS7000214: The client certificate has been revoked. Please use a valid certificate.");

            // Act
            bool result = InvokeIsInvalidClientCertificateError(exception, retryCount: 0);

            // Assert
            Assert.True(result, "Should return true for AADSTS7000214 (certificate revoked) error");
        }

        /// <summary>
        /// Test that the method returns true when the error message contains "AADSTS1000502" (Certificate outside validity window).
        /// This is a certificate-related error that should trigger a reload.
        /// </summary>
        [Fact]
        public void IsInvalidClientCertificateError_CertificateOutsideValidityWindow_ReturnsTrue()
        {
            // Arrange
            var exception = CreateMsalServiceException(
                Constants.InvalidClient,
                "AADSTS1000502: The client certificate is not within its valid time range. Ensure that the certificate is valid.");

            // Act
            bool result = InvokeIsInvalidClientCertificateError(exception, retryCount: 0);

            // Assert
            Assert.True(result, "Should return true for AADSTS1000502 (certificate outside validity) error");
        }

        /// <summary>
        /// Test that the method returns false when the error is AADSTS7000215 (Invalid client secret).
        /// This is NOT a certificate-related error and should NOT trigger a reload.
        /// </summary>
        [Fact]
        public void IsInvalidClientCertificateError_InvalidClientSecret_ReturnsFalse()
        {
            // Arrange
            var exception = CreateMsalServiceException(
                Constants.InvalidClient,
                "AADSTS7000215: Invalid client secret provided. Ensure the secret being sent in the request is the client secret value, not the client secret ID.");

            // Act
            bool result = InvokeIsInvalidClientCertificateError(exception, retryCount: 0);

            // Assert
            Assert.False(result, "Should return false for AADSTS7000215 (invalid client secret) - not certificate related");
        }

        /// <summary>
        /// Test that the method returns false when the error is AADSTS700016 (Application not found).
        /// This is NOT a certificate-related error and should NOT trigger a reload.
        /// </summary>
        [Fact]
        public void IsInvalidClientCertificateError_ApplicationNotFound_ReturnsFalse()
        {
            // Arrange
            var exception = CreateMsalServiceException(
                Constants.InvalidClient,
                "AADSTS700016: Application with identifier 'abc-123' was not found in the directory.");

            // Act
            bool result = InvokeIsInvalidClientCertificateError(exception, retryCount: 0);

            // Assert
            Assert.False(result, "Should return false for AADSTS700016 (application not found) - not certificate related");
        }

        /// <summary>
        /// Test that the method returns false when the error is AADSTS7000222 (Invalid client secret expired).
        /// This is NOT a certificate-related error and should NOT trigger a reload.
        /// </summary>
        [Fact]
        public void IsInvalidClientCertificateError_ExpiredClientSecret_ReturnsFalse()
        {
            // Arrange
            var exception = CreateMsalServiceException(
                Constants.InvalidClient,
                "AADSTS7000222: The provided client secret keys are expired. Create new keys for your app in Azure Portal.");

            // Act
            bool result = InvokeIsInvalidClientCertificateError(exception, retryCount: 0);

            // Assert
            Assert.False(result, "Should return false for AADSTS7000222 (expired client secret) - not certificate related");
        }

        /// <summary>
        /// Test that the method returns false when the error is AADSTS50011 (Invalid reply address).
        /// This is NOT a certificate-related error and should NOT trigger a reload.
        /// </summary>
        [Fact]
        public void IsInvalidClientCertificateError_InvalidReplyAddress_ReturnsFalse()
        {
            // Arrange
            var exception = CreateMsalServiceException(
                Constants.InvalidClient,
                "AADSTS50011: The redirect URI specified in the request does not match the redirect URIs configured for the application.");

            // Act
            bool result = InvokeIsInvalidClientCertificateError(exception, retryCount: 0);

            // Assert
            Assert.False(result, "Should return false for AADSTS50011 (invalid reply address) - not certificate related");
        }

        /// <summary>
        /// Test that the method returns false when the error is AADSTS50012 (Invalid client credentials).
        /// This is NOT necessarily a certificate-related error and should NOT trigger a reload.
        /// </summary>
        [Fact]
        public void IsInvalidClientCertificateError_InvalidClientCredentials_ReturnsFalse()
        {
            // Arrange
            var exception = CreateMsalServiceException(
                Constants.InvalidClient,
                "AADSTS50012: Invalid client credentials.");

            // Act
            bool result = InvokeIsInvalidClientCertificateError(exception, retryCount: 0);

            // Assert
            Assert.False(result, "Should return false for AADSTS50012 (invalid client credentials) - too generic");
        }

        /// <summary>
        /// Test that the method returns false when a retry has already been attempted.
        /// This prevents infinite retry loops.
        /// </summary>
        [Fact]
        public void IsInvalidClientCertificateError_RetryCountAtMax_ReturnsFalse()
        {
            // Arrange
            var exception = CreateMsalServiceException(
                Constants.InvalidClient,
                $"{Constants.InvalidKeyError}: Client assertion contains an invalid signature.");

            // Act
            bool result = InvokeIsInvalidClientCertificateError(exception, retryCount: 1);

            // Assert
            Assert.False(result, "Should return false when retry count has reached max (1) to prevent infinite loops");
        }

        /// <summary>
        /// Test that the method returns true when retry count is below max.
        /// </summary>
        [Fact]
        public void IsInvalidClientCertificateError_RetryCountBelowMax_ReturnsTrue()
        {
            // Arrange
            var exception = CreateMsalServiceException(
                Constants.InvalidClient,
                $"{Constants.InvalidKeyError}: Client assertion contains an invalid signature.");

            // Act
            bool result = InvokeIsInvalidClientCertificateError(exception, retryCount: 0);

            // Assert
            Assert.True(result, "Should return true when retry count is below max");
        }

        /// <summary>
        /// Test that the method returns false for a generic invalid_client error without certificate-specific details.
        /// </summary>
        [Fact]
        public void IsInvalidClientCertificateError_GenericInvalidClient_ReturnsFalse()
        {
            // Arrange
            var exception = CreateMsalServiceException(
                Constants.InvalidClient,
                "Client authentication failed.");

            // Act
            bool result = InvokeIsInvalidClientCertificateError(exception, retryCount: 0);

            // Assert
            Assert.False(result, "Should return false for generic invalid_client error without certificate details");
        }

        /// <summary>
        /// Test case sensitivity of error code matching.
        /// Error codes in messages could be in different cases.
        /// </summary>
        [Fact]
        public void IsInvalidClientCertificateError_CaseInsensitiveMatching_ReturnsTrue()
        {
            // Arrange
            var exception = CreateMsalServiceException(
                Constants.InvalidClient,
                "aadsts700027: Client assertion contains an invalid signature.");

            // Act
            bool result = InvokeIsInvalidClientCertificateError(exception, retryCount: 0);

            // Assert
            Assert.True(result, "Should match error codes case-insensitively");
        }

        /// <summary>
        /// Test that multiple certificate error codes in the same message are handled correctly.
        /// </summary>
        [Fact]
        public void IsInvalidClientCertificateError_MultipleCertificateErrors_ReturnsTrue()
        {
            // Arrange
            var exception = CreateMsalServiceException(
                Constants.InvalidClient,
                $"{Constants.InvalidKeyError}: Invalid key. {Constants.CertificateHasBeenRevoked}: Certificate revoked.");

            // Act
            bool result = InvokeIsInvalidClientCertificateError(exception, retryCount: 0);

            // Assert
            Assert.True(result, "Should return true when message contains certificate-related error codes");
        }

        /// <summary>
        /// Test that the method handles null or empty error messages gracefully.
        /// </summary>
        [Fact]
        public void IsInvalidClientCertificateError_EmptyMessage_ReturnsFalse()
        {
            // Arrange
            var exception = CreateMsalServiceException(Constants.InvalidClient, string.Empty);

            // Act
            bool result = InvokeIsInvalidClientCertificateError(exception, retryCount: 0);

            // Assert
            Assert.False(result, "Should return false for empty error message");
        }

        #region Retry Count Tests

        /// <summary>
        /// Test that retry count of 0 allows certificate reload.
        /// </summary>
        [Fact]
        public void IsInvalidClientCertificateError_RetryCount0_AllowsRetry()
        {
            // Arrange
            var exception = CreateMsalServiceException(
                Constants.InvalidClient,
                $"{Constants.InvalidKeyError}: Invalid certificate signature");

            // Act
            bool result = InvokeIsInvalidClientCertificateError(exception, retryCount: 0);

            // Assert
            Assert.True(result, "Should return true when retryCount is 0 (first attempt)");
        }

        /// <summary>
        /// Test that retry count of 1 (at max) prevents further retries.
        /// </summary>
        [Fact]
        public void IsInvalidClientCertificateError_RetryCount1_PreventsRetry()
        {
            // Arrange
            var exception = CreateMsalServiceException(
                Constants.InvalidClient,
                $"{Constants.InvalidKeyError}: Invalid certificate signature");

            // Act
            bool result = InvokeIsInvalidClientCertificateError(exception, retryCount: 1);

            // Assert
            Assert.False(result, "Should return false when retryCount is 1 (max attempts reached)");
        }

        /// <summary>
        /// Test that retry count greater than max prevents retries.
        /// </summary>
        [Fact]
        public void IsInvalidClientCertificateError_RetryCountAboveMax_PreventsRetry()
        {
            // Arrange
            var exception = CreateMsalServiceException(
                Constants.InvalidClient,
                $"{Constants.InvalidKeyError}: Invalid certificate signature");

            // Act
            bool result = InvokeIsInvalidClientCertificateError(exception, retryCount: 2);

            // Assert
            Assert.False(result, "Should return false when retryCount exceeds max (safety check)");
        }

        /// <summary>
        /// Test that retry count respects the limit even for multiple certificate error types.
        /// </summary>
        [Theory]
        [InlineData(0, true)]   // First attempt - should allow retry
        [InlineData(1, false)]  // Second attempt (max reached) - should not allow retry
        [InlineData(2, false)]  // Beyond max - should not allow retry
        public void IsInvalidClientCertificateError_RetryCountRespectedForAllCertErrors(int retryCount, bool expectedResult)
        {
            // Arrange - test with certificate revoked error
            var exception = CreateMsalServiceException(
                Constants.InvalidClient,
                $"{Constants.CertificateHasBeenRevoked}: Certificate has been revoked");

            // Act
            bool result = InvokeIsInvalidClientCertificateError(exception, retryCount);

            // Assert
            Assert.Equal(expectedResult, result);
        }

        /// <summary>
        /// Test that non-certificate errors return false regardless of retry count.
        /// </summary>
        [Theory]
        [InlineData(0)]  // First attempt
        [InlineData(1)]  // At max
        [InlineData(5)]  // Way beyond max
        public void IsInvalidClientCertificateError_NonCertError_AlwaysReturnsFalse(int retryCount)
        {
            // Arrange - non-certificate related error
            var exception = CreateMsalServiceException(
                Constants.InvalidClient,
                "AADSTS7000215: Invalid client secret provided");

            // Act
            bool result = InvokeIsInvalidClientCertificateError(exception, retryCount);

            // Assert
            Assert.False(result, $"Should return false for non-certificate error regardless of retryCount ({retryCount})");
        }

        /// <summary>
        /// Test retry count behavior with signed assertion time range error.
        /// </summary>
        [Theory]
        [InlineData(0, true)]
        [InlineData(1, false)]
        public void IsInvalidClientCertificateError_SignedAssertionError_RespectsRetryCount(int retryCount, bool expectedResult)
        {
            // Arrange
            var exception = CreateMsalServiceException(
                Constants.InvalidClient,
                $"{Constants.SignedAssertionInvalidTimeRange}: Signed assertion is not within valid time range");

            // Act
            bool result = InvokeIsInvalidClientCertificateError(exception, retryCount);

            // Assert
            Assert.Equal(expectedResult, result);
        }

        /// <summary>
        /// Test retry count behavior with certificate outside validity window error.
        /// </summary>
        [Theory]
        [InlineData(0, true)]
        [InlineData(1, false)]
        public void IsInvalidClientCertificateError_CertOutsideValidityWindow_RespectsRetryCount(int retryCount, bool expectedResult)
        {
            // Arrange
            var exception = CreateMsalServiceException(
                Constants.InvalidClient,
                $"{Constants.CertificateIsOutsideValidityWindow}: Certificate is not within its valid time range");

            // Act
            bool result = InvokeIsInvalidClientCertificateError(exception, retryCount);

            // Assert
            Assert.Equal(expectedResult, result);
        }

        /// <summary>
        /// Test that negative retry count is handled gracefully (should allow retry as it's less than max).
        /// </summary>
        [Fact]
        public void IsInvalidClientCertificateError_NegativeRetryCount_AllowsRetry()
        {
            // Arrange
            var exception = CreateMsalServiceException(
                Constants.InvalidClient,
                $"{Constants.InvalidKeyError}: Invalid certificate signature");

            // Act
            bool result = InvokeIsInvalidClientCertificateError(exception, retryCount: -1);

            // Assert
            Assert.True(result, "Should return true for negative retryCount (defensive programming - treats as first attempt)");
        }

        /// <summary>
        /// Test boundary condition: retry count exactly at the maximum allowed attempts.
        /// </summary>
        [Fact]
        public void IsInvalidClientCertificateError_RetryCountAtBoundary_BehavesCorrectly()
        {
            // Arrange
            var exception = CreateMsalServiceException(
                Constants.InvalidClient,
                $"{Constants.InvalidKeyError}: Invalid certificate signature");

            // Act - Test just below max (should allow)
            bool resultBelowMax = InvokeIsInvalidClientCertificateError(exception, retryCount: 0);
            
            // Act - Test at max (should not allow)
            bool resultAtMax = InvokeIsInvalidClientCertificateError(exception, retryCount: 1);

            // Assert
            Assert.True(resultBelowMax, "Should return true when retryCount is below max (0 < 1)");
            Assert.False(resultAtMax, "Should return false when retryCount equals max (1 >= 1)");
        }

        /// <summary>
        /// Test that each certificate error type respects the retry limit independently.
        /// </summary>
        [Theory]
        [InlineData(Constants.InvalidKeyError, 0, true)]
        [InlineData(Constants.InvalidKeyError, 1, false)]
        [InlineData(Constants.SignedAssertionInvalidTimeRange, 0, true)]
        [InlineData(Constants.SignedAssertionInvalidTimeRange, 1, false)]
        [InlineData(Constants.CertificateHasBeenRevoked, 0, true)]
        [InlineData(Constants.CertificateHasBeenRevoked, 1, false)]
        [InlineData(Constants.CertificateIsOutsideValidityWindow, 0, true)]
        [InlineData(Constants.CertificateIsOutsideValidityWindow, 1, false)]
        public void IsInvalidClientCertificateError_AllCertErrorTypes_RespectRetryLimit(
            string errorCode, int retryCount, bool expectedResult)
        {
            // Arrange
            var exception = CreateMsalServiceException(
                Constants.InvalidClient,
                $"{errorCode}: Certificate error occurred");

            // Act
            bool result = InvokeIsInvalidClientCertificateError(exception, retryCount);

            // Assert
            Assert.Equal(expectedResult, result);
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Creates a MsalServiceException with the specified error code and message.
        /// </summary>
        private static MsalServiceException CreateMsalServiceException(string errorCode, string message)
        {
            return new MsalServiceException(errorCode, message);
        }

        /// <summary>
        /// Uses reflection to invoke the private IsInvalidClientCertificateOrSignedAssertionError method.
        /// </summary>
        private static bool InvokeIsInvalidClientCertificateError(MsalServiceException exception, int retryCount)
        {
            // Create a minimal TokenAcquisition instance for testing
            var tokenAcquisition = TokenAcquisitionTestHelper.CreateTokenAcquisition();

            // Use reflection to invoke the private method
            var method = typeof(TokenAcquisition).GetMethod(
                "IsInvalidClientCertificateOrSignedAssertionError",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (method == null)
            {
                throw new InvalidOperationException("Could not find IsInvalidClientCertificateOrSignedAssertionError method via reflection");
            }

            var result = method.Invoke(tokenAcquisition, new object[] { exception, retryCount });
            return (bool)result!;
        }

        #endregion
    }

    /// <summary>
    /// Helper class to create TokenAcquisition instances for testing.
    /// </summary>
    internal static class TokenAcquisitionTestHelper
    {
        /// <summary>
        /// Creates a TokenAcquisition instance for testing purposes.
        /// </summary>
        public static TokenAcquisition CreateTokenAcquisition()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IMemoryCache>(new MemoryCache(new MemoryCacheOptions()));
            services.AddSingleton<IMergedOptionsStore, MergedOptionsStore>();
            services.AddHttpClient();
            services.AddLogging();
            var serviceProvider = services.BuildServiceProvider();

            var tokenCacheProvider = new MsalTestTokenCacheProvider(
                serviceProvider.GetRequiredService<IMemoryCache>(),
                Options.Create(new MsalMemoryTokenCacheOptions()));

            var tokenAcquisitionHost = new Hosts.DefaultTokenAcquisitionHost(
                new TestOptionsMonitor<MicrosoftIdentityOptions>(new MicrosoftIdentityOptions()),
                serviceProvider.GetRequiredService<IMergedOptionsStore>(),
                new TestOptionsMonitor<ConfidentialClientApplicationOptions>(new ConfidentialClientApplicationOptions()),
                new TestOptionsMonitor<Microsoft.Identity.Abstractions.MicrosoftIdentityApplicationOptions>(
                    new Microsoft.Identity.Abstractions.MicrosoftIdentityApplicationOptions()));

            var tokenAcquisition = new TokenAcquisition(
                tokenCacheProvider,
                tokenAcquisitionHost,
                serviceProvider.GetRequiredService<IHttpClientFactory>(),
                serviceProvider.GetRequiredService<ILogger<TokenAcquisition>>(),
                serviceProvider,
                new DefaultCredentialsLoader());

            return tokenAcquisition;
        }
    }
}
