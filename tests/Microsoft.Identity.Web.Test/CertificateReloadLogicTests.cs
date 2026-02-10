// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web.Test.Common;
using Microsoft.Identity.Web.TestOnly;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    /// <summary>
    /// Tests for the certificate reload logic to ensure it only triggers on certificate-related errors.
    /// This addresses the regression from PR #3430 where reload was triggered on all invalid_client errors.
    /// </summary>
    [Collection(nameof(TokenAcquirerFactorySingletonProtection))]
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
        public void IsInvalidClientCertificateOrSignedAssertionError_ReturnsTrueOnlyForCertificateErrors(
            string errorCode, 
            bool shouldTriggerReload)
        {
            // Arrange
            var tokenAcquisition = CreateTokenAcquisition();
            var responseBody = $"{{\"error\":\"invalid_client\",\"error_description\":\"Error {errorCode}: Test error\"}}";
            var exception = CreateMsalServiceException(InvalidClientErrorCode, responseBody);

            // Act
            bool result = InvokeIsInvalidClientCertificateOrSignedAssertionError(tokenAcquisition, exception);

            // Assert
            Assert.Equal(shouldTriggerReload, result);
        }

        [Fact]
        public void IsInvalidClientCertificateOrSignedAssertionError_ReturnsFalseWhenErrorCodeIsNotInvalidClient()
        {
            // Arrange
            var tokenAcquisition = CreateTokenAcquisition();
            var responseBody = "{\"error\":\"unauthorized_client\",\"error_description\":\"Test error\"}";
            var exception = CreateMsalServiceException("unauthorized_client", responseBody);

            // Act
            bool result = InvokeIsInvalidClientCertificateOrSignedAssertionError(tokenAcquisition, exception);

            // Assert
            Assert.False(result, "Should not trigger reload for non-invalid_client errors");
        }

        [Fact]
        public void IsInvalidClientCertificateOrSignedAssertionError_ReturnsFalseForEmptyResponseBody()
        {
            // Arrange
            var tokenAcquisition = CreateTokenAcquisition();
            var exception = CreateMsalServiceException(InvalidClientErrorCode, string.Empty);

            // Act
            bool result = InvokeIsInvalidClientCertificateOrSignedAssertionError(tokenAcquisition, exception);

            // Assert
            Assert.False(result, "Should not trigger reload when response body is empty");
        }

        [Theory]
        [InlineData("AADSTS700027")]  // Case sensitive check - should still work
        [InlineData("aadsts700027")]  // Lowercase
        [InlineData("AaDsTs700027")]  // Mixed case
        public void IsInvalidClientCertificateOrSignedAssertionError_IsCaseInsensitive(string errorCodeCase)
        {
            // Arrange
            var tokenAcquisition = CreateTokenAcquisition();
            var responseBody = $"{{\"error\":\"invalid_client\",\"error_description\":\"Error {errorCodeCase}: Test error\"}}";
            var exception = CreateMsalServiceException(InvalidClientErrorCode, responseBody);

            // Act
            bool result = InvokeIsInvalidClientCertificateOrSignedAssertionError(tokenAcquisition, exception);

            // Assert
            Assert.True(result, $"Should trigger reload regardless of case: {errorCodeCase}");
        }

        [Fact]
        public void IsInvalidClientCertificateOrSignedAssertionError_WorksWithMultipleErrorCodesInResponse()
        {
            // Arrange
            var tokenAcquisition = CreateTokenAcquisition();
            // Response might contain multiple error codes or descriptions
            var responseBody = $"{{\"error\":\"invalid_client\",\"error_description\":\"Error {Constants.CertificateHasBeenRevoked}: Certificate has been revoked. Also note AADSTS7000215 in logs.\"}}";
            var exception = CreateMsalServiceException(InvalidClientErrorCode, responseBody);

            // Act
            bool result = InvokeIsInvalidClientCertificateOrSignedAssertionError(tokenAcquisition, exception);

            // Assert
            Assert.True(result, "Should trigger reload when certificate error code is present, even if other codes are also mentioned");
        }

        /// <summary>
        /// Creates a TokenAcquisition instance for testing.
        /// </summary>
        private TokenAcquisition CreateTokenAcquisition()
        {
            TokenAcquirerFactoryTesting.ResetTokenAcquirerFactoryInTest();
            var tokenAcquirerFactory = TokenAcquirerFactory.GetDefaultInstance();
            tokenAcquirerFactory.Services.Configure<MicrosoftIdentityApplicationOptions>(options =>
            {
                options.Instance = "https://login.microsoftonline.com/";
                options.TenantId = "f645ad92-e38d-4d1a-b510-d1b09a74a8ca";
                options.ClientId = "idu773ld-e38d-jud3-45lk-d1b09a74a8ca";
                options.ClientCredentials = [new CredentialDescription()
                {
                    SourceType = CredentialSource.ClientSecret,
                    ClientSecret = "someSecret"
                }];
            });
            
            var serviceProvider = tokenAcquirerFactory.Build();
            
            // Get the TokenAcquisition instance from the service provider
            var tokenAcquisition = serviceProvider.GetService(typeof(ITokenAcquisitionInternal)) as TokenAcquisition;
            
            if (tokenAcquisition == null)
            {
                throw new InvalidOperationException("Failed to create TokenAcquisition instance for testing");
            }
            
            return tokenAcquisition;
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

        /// <summary>
        /// Invokes the private IsInvalidClientCertificateOrSignedAssertionError method using reflection.
        /// </summary>
        private bool InvokeIsInvalidClientCertificateOrSignedAssertionError(
            TokenAcquisition tokenAcquisition, 
            MsalServiceException exception)
        {
            var method = typeof(TokenAcquisition).GetMethod(
                "IsInvalidClientCertificateOrSignedAssertionError",
                BindingFlags.NonPublic | BindingFlags.Instance);

            if (method == null)
            {
                throw new InvalidOperationException("Could not find IsInvalidClientCertificateOrSignedAssertionError method");
            }

            var result = method.Invoke(tokenAcquisition, new object[] { exception });
            return (bool)result!;
        }
    }
}
