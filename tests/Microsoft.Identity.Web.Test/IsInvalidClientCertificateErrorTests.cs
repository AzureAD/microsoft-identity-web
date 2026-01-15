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
            bool result = InvokeIsInvalidClientCertificateError(exception, retryAlreadyAttempted: false);

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
            bool result = InvokeIsInvalidClientCertificateError(exception, retryAlreadyAttempted: false);

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
            bool result = InvokeIsInvalidClientCertificateError(exception, retryAlreadyAttempted: false);

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
            bool result = InvokeIsInvalidClientCertificateError(exception, retryAlreadyAttempted: false);

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
            bool result = InvokeIsInvalidClientCertificateError(exception, retryAlreadyAttempted: false);

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
            bool result = InvokeIsInvalidClientCertificateError(exception, retryAlreadyAttempted: false);

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
            bool result = InvokeIsInvalidClientCertificateError(exception, retryAlreadyAttempted: false);

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
            bool result = InvokeIsInvalidClientCertificateError(exception, retryAlreadyAttempted: false);

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
            bool result = InvokeIsInvalidClientCertificateError(exception, retryAlreadyAttempted: false);

            // Assert
            Assert.False(result, "Should return false for AADSTS50012 (invalid client credentials) - too generic");
        }

        /// <summary>
        /// Test that the method returns false when a retry has already been attempted.
        /// This prevents infinite retry loops.
        /// </summary>
        [Fact]
        public void IsInvalidClientCertificateError_RetryAlreadyAttempted_ReturnsFalse()
        {
            // Arrange
            var exception = CreateMsalServiceException(
                Constants.InvalidClient,
                $"{Constants.InvalidKeyError}: Client assertion contains an invalid signature.");

            // Act
            bool result = InvokeIsInvalidClientCertificateError(exception, retryAlreadyAttempted: true);

            // Assert
            Assert.False(result, "Should return false when retry has already been attempted to prevent infinite loops");
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
            bool result = InvokeIsInvalidClientCertificateError(exception, retryAlreadyAttempted: false);

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
            bool result = InvokeIsInvalidClientCertificateError(exception, retryAlreadyAttempted: false);

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
            bool result = InvokeIsInvalidClientCertificateError(exception, retryAlreadyAttempted: false);

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
            bool result = InvokeIsInvalidClientCertificateError(exception, retryAlreadyAttempted: false);

            // Assert
            Assert.False(result, "Should return false for empty error message");
        }

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
        private static bool InvokeIsInvalidClientCertificateError(MsalServiceException exception, bool retryAlreadyAttempted)
        {
            // Create a minimal TokenAcquisition instance for testing
            var tokenAcquisition = TokenAcquisitionTestHelper.CreateTokenAcquisition(retryAlreadyAttempted);

            // Use reflection to invoke the private method
            var method = typeof(TokenAcquisition).GetMethod(
                "IsInvalidClientCertificateOrSignedAssertionError",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (method == null)
            {
                throw new InvalidOperationException("Could not find IsInvalidClientCertificateOrSignedAssertionError method via reflection");
            }

            var result = method.Invoke(tokenAcquisition, new object[] { exception });
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
        /// Creates a TokenAcquisition instance with the specified retry state for testing purposes.
        /// </summary>
        public static TokenAcquisition CreateTokenAcquisition(bool retryAlreadyAttempted)
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

            // Set the _retryClientCertificate field using reflection
            if (retryAlreadyAttempted)
            {
                var field = typeof(TokenAcquisition).GetField(
                    "_retryClientCertificate",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                field?.SetValue(tokenAcquisition, true);
            }

            return tokenAcquisition;
        }
    }
}
