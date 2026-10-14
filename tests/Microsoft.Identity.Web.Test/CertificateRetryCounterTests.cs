// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web.Test.Common;
using Microsoft.Identity.Web.Test.Common.Mocks;
using Microsoft.Identity.Web.TestOnly;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    /// <summary>
    /// Tests certificate authentication error detection and bounded token acquisition retries.
    /// </summary>
    [Collection(nameof(TokenAcquirerFactorySingletonProtection))]
    public class CertificateRetryCounterTests
    {
        private const string InvalidClientErrorCode = "invalid_client";
        private const string TenantId = "f645ad92-e38d-4d1a-b510-d1b09a74a8ca";
        private const string TokenEndpoint = "https://login.microsoftonline.com/" + TenantId + "/oauth2/v2.0/token";
        private const string TestScope = "https://graph.microsoft.com/.default";

        #region Certificate Error Detection Tests

        [Theory]
        [InlineData("AADSTS700016")] // Application not found (wrong ClientID)
        [InlineData("AADSTS7000215")] // Invalid client secret
        public void IsInvalidClientCertificateOrSignedAssertionError_ReturnsFalseForNonRetryableConfigErrors(string errorCode)
        {
            // Arrange
            var tokenAcquisition = CreateTokenAcquisition();
            var responseBody = $"{{\"error\":\"invalid_client\",\"error_description\":\"Error {errorCode}: Config error\"}}";
            var exception = CreateMsalServiceException(InvalidClientErrorCode, responseBody);

            // Act
            bool result = InvokeIsInvalidClientCertificateOrSignedAssertionError(tokenAcquisition, exception);

            // Assert
            Assert.False(result, $"Should NOT retry for non-retryable config error: {errorCode}");
        }

        [Theory]
        [InlineData("AADSTS700027")]  // InvalidKeyError
        [InlineData("AADSTS700024")]  // SignedAssertionInvalidTimeRange
        [InlineData("AADSTS7000214")] // CertificateHasBeenRevoked
        [InlineData("AADSTS1000502")] // CertificateIsOutsideValidityWindow
        [InlineData("AADSTS7000274")] // ClientAssertionContainsInvalidSignature
        [InlineData("AADSTS7000277")] // CertificateWasRevoked
        public void IsInvalidClientCertificateOrSignedAssertionError_ReturnsTrueForCertificateErrors(string errorCode)
        {
            // Arrange
            var tokenAcquisition = CreateTokenAcquisition();
            var responseBody = $"{{\"error\":\"invalid_client\",\"error_description\":\"Error {errorCode}: Cert error\"}}";
            var exception = CreateMsalServiceException(InvalidClientErrorCode, responseBody);

            // Act
            bool result = InvokeIsInvalidClientCertificateOrSignedAssertionError(tokenAcquisition, exception);

            // Assert
            Assert.True(result, $"Should retry for certificate error: {errorCode}");
        }

        #endregion

        #region Regression Test: Infinite Loop Prevention

        /// <summary>
        /// Verifies that a bad client ID is surfaced without retrying the token request.
        /// </summary>
        [Fact]
        public async Task GetAuthenticationResultForAppAsync_WithBadClientId_DoesNotRetry()
        {
            // Arrange
            using var httpClientFactory = new MockHttpClientFactory();
            MockHttpMessageHandler handler = httpClientFactory.AddMockHandler(
                CreateInvalidClientHandler("AADSTS700016: Application with identifier 'bad-client-id' was not found."));
            var tokenAcquisition = CreateTokenAcquisition("bad-client-id-does-not-exist", httpClientFactory);

            // Act & Assert
            MsalServiceException exception = await Assert.ThrowsAsync<MsalServiceException>(
                () => tokenAcquisition.GetAuthenticationResultForAppAsync(TestScope));

            Assert.Equal(InvalidClientErrorCode, exception.ErrorCode);
            Assert.NotNull(handler.ActualRequestMessage);
            Assert.Equal(TokenEndpoint, handler.ActualRequestMessage.RequestUri!.AbsoluteUri);
        }

        /// <summary>
        /// Verifies that a retryable certificate error is attempted once and then surfaced.
        /// </summary>
        [Fact]
        public async Task GetAuthenticationResultForAppAsync_WithCertificateError_RetriesOnce()
        {
            // Arrange
            using var httpClientFactory = new MockHttpClientFactory();
            MockHttpMessageHandler firstHandler = httpClientFactory.AddMockHandler(
                CreateInvalidClientHandler("AADSTS700027: The certificate signature is invalid."));
            MockHttpMessageHandler retryHandler = httpClientFactory.AddMockHandler(
                CreateInvalidClientHandler("AADSTS700027: The certificate signature is invalid."));
            var tokenAcquisition = CreateTokenAcquisition("certificate-retry-client-id", httpClientFactory);

            // Act & Assert
            MsalServiceException exception = await Assert.ThrowsAsync<MsalServiceException>(
                () => tokenAcquisition.GetAuthenticationResultForAppAsync(TestScope));

            Assert.Equal(InvalidClientErrorCode, exception.ErrorCode);
            Assert.NotNull(firstHandler.ActualRequestMessage);
            Assert.NotNull(retryHandler.ActualRequestMessage);
            Assert.Equal(TokenEndpoint, firstHandler.ActualRequestMessage.RequestUri!.AbsoluteUri);
            Assert.Equal(TokenEndpoint, retryHandler.ActualRequestMessage.RequestUri!.AbsoluteUri);
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Creates a TokenAcquisition instance for testing.
        /// </summary>
        private TokenAcquisition CreateTokenAcquisition(
            string clientId = "idu773ld-e38d-jud3-45lk-d1b09a74a8ca",
            IMsalHttpClientFactory? httpClientFactory = null)
        {
            TokenAcquirerFactoryTesting.ResetTokenAcquirerFactoryInTest();
            var tokenAcquirerFactory = TokenAcquirerFactory.GetDefaultInstance();
            tokenAcquirerFactory.Services.Configure<MicrosoftIdentityApplicationOptions>(options =>
            {
                options.Instance = "https://login.microsoftonline.com/";
                options.TenantId = TenantId;
                options.ClientId = clientId;
                options.ClientCredentials = [new CredentialDescription()
                {
                    SourceType = CredentialSource.ClientSecret,
                    ClientSecret = "someSecret"
                }];
            });

            if (httpClientFactory is not null)
            {
                tokenAcquirerFactory.Services.AddSingleton(httpClientFactory);
            }

            var serviceProvider = tokenAcquirerFactory.Build();

            var tokenAcquisition = serviceProvider.GetService(typeof(ITokenAcquisitionInternal)) as TokenAcquisition;

            if (tokenAcquisition == null)
            {
                throw new InvalidOperationException("Failed to create TokenAcquisition instance for testing");
            }

            return tokenAcquisition;
        }

        private static MockHttpMessageHandler CreateInvalidClientHandler(string errorDescription)
        {
            return new MockHttpMessageHandler
            {
                ExpectedMethod = HttpMethod.Post,
                ExpectedUrl = TokenEndpoint,
                ResponseMessage = new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent(
                        $"{{\"error\":\"{InvalidClientErrorCode}\",\"error_description\":\"{errorDescription}\"}}"),
                },
            };
        }

        /// <summary>
        /// Creates a MsalServiceException for testing.
        /// </summary>
        private MsalServiceException CreateMsalServiceException(string errorCode, string responseBody)
        {
            var exception = new MsalServiceException(errorCode, $"Test exception: {errorCode}");

            // Set the ResponseBody property using reflection
            var responseBodyField = typeof(MsalServiceException).GetProperty("ResponseBody");
            if (responseBodyField != null && responseBodyField.CanWrite)
            {
                responseBodyField.SetValue(exception, responseBody);
            }
            else
            {
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

        #endregion
    }
}
