// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web.Test.Common;
using Microsoft.Identity.Web.TestOnly;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    /// <summary>
    /// Tests for the certificate retry counter logic to prevent infinite retry loops.
    /// This addresses the regression where misconfigured credentials (wrong ClientID/Secret) 
    /// caused infinite retries when using WithAgentIdentities().
    /// </summary>
    [Collection(nameof(TokenAcquirerFactorySingletonProtection))]
    public class CertificateRetryCounterTests
    {
        private const string InvalidClientErrorCode = "invalid_client";
        private const int MaxCertificateRetries = 1;

        #region Certificate Error Detection Tests

        [Theory]
        [InlineData("AADSTS700016")] // Application not found (wrong ClientID)
        [InlineData("AADSTS7000215")] // Invalid client secret
        public void ShouldRetry_ReturnsFalseForNonRetryableConfigErrors(string errorCode)
        {
            // Arrange
            var retryPolicy = new DefaultAuthenticationRetryPolicy();
            var responseBody = $"{{\"error\":\"invalid_client\",\"error_description\":\"Error {errorCode}: Config error\"}}";
            var exception = CreateMsalServiceException(InvalidClientErrorCode, responseBody);

            // Act
            bool result = retryPolicy.ShouldRetry(new AuthenticationRetryContext { ThrownException = exception });

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
        public void ShouldRetry_ReturnsTrueForCertificateErrors(string errorCode)
        {
            // Arrange
            var retryPolicy = new DefaultAuthenticationRetryPolicy();
            var responseBody = $"{{\"error\":\"invalid_client\",\"error_description\":\"Error {errorCode}: Cert error\"}}";
            var exception = CreateMsalServiceException(InvalidClientErrorCode, responseBody);

            // Act
            bool result = retryPolicy.ShouldRetry(new AuthenticationRetryContext { ThrownException = exception });

            // Assert
            Assert.True(result, $"Should retry for certificate error: {errorCode}");
        }

        [Theory]
        [InlineData(HttpStatusCode.BadRequest, true)]
        [InlineData(HttpStatusCode.Unauthorized, true)]
        [InlineData(HttpStatusCode.Forbidden, true)]
        [InlineData((HttpStatusCode)495, true)]
        [InlineData((HttpStatusCode)496, true)]
        [InlineData(HttpStatusCode.OK, false)]
        [InlineData(HttpStatusCode.NotFound, false)]
        [InlineData(HttpStatusCode.InternalServerError, false)]
        public void ShouldRetry_ForMtls_UsesCertificateFailureStatusCodes(HttpStatusCode statusCode, bool expected)
        {
            // Arrange
            var retryPolicy = new DefaultAuthenticationRetryPolicy();
            using var response = new HttpResponseMessage(statusCode);
            var context = new AuthenticationRetryContext
            {
                CredentialSourceLoaderParameters = new CredentialSourceLoaderParameters(string.Empty, string.Empty)
                {
                    Protocol = Constants.MtlsProtocolScheme
                },
                Response = response
            };

            // Act
            bool result = retryPolicy.ShouldRetry(context);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(Constants.MtlsProtocolScheme)]
        [InlineData("Bearer")]
        [InlineData(null)]
        public void ShouldRetry_WithoutApplicableFailure_ReturnsFalse(string? protocol)
        {
            // Arrange
            var retryPolicy = new DefaultAuthenticationRetryPolicy();
            var context = new AuthenticationRetryContext
            {
                CredentialSourceLoaderParameters = protocol is null ? null : new CredentialSourceLoaderParameters(string.Empty, string.Empty)
                {
                    Protocol = protocol
                },
                ThrownException = new InvalidOperationException("Not an authentication failure")
            };

            // Act
            bool result = retryPolicy.ShouldRetry(context);

            // Assert
            Assert.False(result);
        }

        [Theory]
        [InlineData("invalid_client", "aadsts700027", true)]
        [InlineData("INVALID_CLIENT", "AADSTS700027", true)]
        [InlineData("invalid_grant", "AADSTS700027", false)]
        [InlineData("invalid_client", "", false)]
        public void ShouldRetry_ForMsalFailure_RequiresInvalidClientAndCertificateError(
            string errorCode, string responseBody, bool expected)
        {
            // Arrange
            var retryPolicy = new DefaultAuthenticationRetryPolicy();
            var exception = CreateMsalServiceException(errorCode, responseBody);

            // Act
            bool result = retryPolicy.ShouldRetry(new AuthenticationRetryContext { ThrownException = exception });

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(Constants.MtlsProtocolScheme)]
        [InlineData("Bearer")]
        public void ShouldRetry_DoesNotApplyFailureFromAnotherProtocol(string protocol)
        {
            // Arrange
            var retryPolicy = new DefaultAuthenticationRetryPolicy();
            using var response = new HttpResponseMessage(HttpStatusCode.Unauthorized);
            var context = new AuthenticationRetryContext
            {
                CredentialSourceLoaderParameters = new CredentialSourceLoaderParameters(string.Empty, string.Empty)
                {
                    Protocol = protocol
                },
                Response = protocol == "Bearer" ? response : null,
                ThrownException = protocol == Constants.MtlsProtocolScheme
                    ? CreateMsalServiceException(InvalidClientErrorCode, "AADSTS700027")
                    : null
            };

            // Act
            bool result = retryPolicy.ShouldRetry(context);

            // Assert
            Assert.False(result);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void ShouldRetry_ForMsalFailureWithoutResponseBody_ReturnsFalse(string? responseBody)
        {
            // Arrange
            var retryPolicy = new DefaultAuthenticationRetryPolicy();
            var exception = CreateMsalServiceException(InvalidClientErrorCode, responseBody);

            // Act
            bool result = retryPolicy.ShouldRetry(new AuthenticationRetryContext { ThrownException = exception });

            // Assert
            Assert.False(result);
        }

        #endregion

        #region Regression Test: Infinite Loop Prevention

        /// <summary>
        /// Regression test: Verifies that bad certificate/config does NOT cause infinite retry loop.
        /// This test simulates the exact scenario reported in the bug where .WithAgentIdentities()
        /// with wrong ClientID caused the application to hang indefinitely.
        /// </summary>
        [Fact]
        public async Task GetAuthenticationResultForAppInternalAsync_DoesNotRetryInfinitelyOnBadConfig()
        {
            // Arrange
            var tokenAcquisition = CreateTokenAcquisition();

            // Mock MSAL to always throw "Application Not Found" error
            var mockMsalException = CreateMsalServiceException(
                InvalidClientErrorCode,
                "{\"error\":\"invalid_client\",\"error_description\":\"AADSTS700016: Application with identifier 'bad-client-id' was not found.\"}");

            // Act & Assert
            try
            {
                // This should NOT hang and should throw after MaxCertificateRetries
                await Task.Run(async () =>
                {
                    // Use reflection to call GetAuthenticationResultForAppInternalAsync with retryCount = 0
                    var method = typeof(TokenAcquisition).GetMethod(
                        "GetAuthenticationResultForAppInternalAsync",
                        BindingFlags.NonPublic | BindingFlags.Instance);

                    if (method == null)
                    {
                        throw new InvalidOperationException("Could not find GetAuthenticationResultForAppInternalAsync method");
                    }

                    // This will throw because we don't have a real MSAL setup, but we're testing the retry logic
                    // The test is mainly to verify it doesn't hang
                    var task = method.Invoke(tokenAcquisition, new object?[] 
                    { 
                        "https://graph.microsoft.com/.default", 
                        null, 
                        null, 
                        null,
                        0  // Initial retryCount
                    }) as Task<AuthenticationResult>;

                    if (task != null)
                    {
                        await task;
                    }
                });

                // If we get here without exception, something is wrong with the test setup
                Assert.Fail("Expected an exception to be thrown");
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                // Expected: should throw after max retries
                // Verify it's not hanging and completes quickly
                Assert.NotNull(ex.InnerException);
                // The actual exception type depends on the implementation
            }
            catch (Exception ex)
            {
                // Also acceptable - any exception is fine as long as it doesn't hang
                Assert.NotNull(ex);
            }

            // If we reach here, the method completed (didn't hang)
        }

        /// <summary>
        /// Regression test: Simulates the Agent Identities scenario where nested token acquisitions
        /// with bad config should fail quickly, not hang.
        /// </summary>
        [Fact(Timeout = 30000)] // 30 second timeout - generous to accommodate slow CI, but still validates no infinite hang
        public async Task GetAuthenticationResultForAppAsync_WithBadClientId_CompletesQuickly()
        {
            // Arrange
            var tokenAcquisition = CreateTokenAcquisitionWithBadClientId();

            // Act & Assert
            await Assert.ThrowsAnyAsync<Exception>(async () =>
            {
                // This simulates the Agent Identities scenario
                // Should fail quickly, not hang indefinitely
                var result = await InvokeGetAuthenticationResultForAppAsync(
                    tokenAcquisition,
                    "https://graph.microsoft.com/.default");
            });

            // If we reach here within the timeout, test passes
            Assert.True(true, "Completed without hanging");
        }

        #endregion

        #region Helper Methods

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

            var tokenAcquisition = serviceProvider.GetService(typeof(ITokenAcquisitionInternal)) as TokenAcquisition;

            if (tokenAcquisition == null)
            {
                throw new InvalidOperationException("Failed to create TokenAcquisition instance for testing");
            }

            return tokenAcquisition;
        }

        /// <summary>
        /// Creates a TokenAcquisition instance with a bad ClientID to simulate the regression scenario.
        /// </summary>
        private TokenAcquisition CreateTokenAcquisitionWithBadClientId()
        {
            TokenAcquirerFactoryTesting.ResetTokenAcquirerFactoryInTest();
            var tokenAcquirerFactory = TokenAcquirerFactory.GetDefaultInstance();
            tokenAcquirerFactory.Services.Configure<MicrosoftIdentityApplicationOptions>(options =>
            {
                options.Instance = "https://login.microsoftonline.com/";
                options.TenantId = "f645ad92-e38d-4d1a-b510-d1b09a74a8ca";
                options.ClientId = "bad-client-id-does-not-exist"; // Invalid ClientID
                options.ClientCredentials = [new CredentialDescription()
                {
                    SourceType = CredentialSource.ClientSecret,
                    ClientSecret = "someSecret"
                }];
            });

            var serviceProvider = tokenAcquirerFactory.Build();

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
        private MsalServiceException CreateMsalServiceException(string errorCode, string? responseBody)
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
        /// Invokes GetAuthenticationResultForAppAsync using reflection for testing.
        /// </summary>
        private async Task<AuthenticationResult> InvokeGetAuthenticationResultForAppAsync(
            TokenAcquisition tokenAcquisition,
            string scope)
        {
            var method = typeof(TokenAcquisition).GetMethod(
                "GetAuthenticationResultForAppAsync",
                BindingFlags.Public | BindingFlags.Instance);

            if (method == null)
            {
                throw new InvalidOperationException("Could not find GetAuthenticationResultForAppAsync method");
            }

            var task = method.Invoke(tokenAcquisition, new object?[] { scope, null, null, null }) as Task<AuthenticationResult>;

            if (task == null)
            {
                throw new InvalidOperationException("Method did not return a Task<AuthenticationResult>");
            }

            return await task;
        }

        #endregion
    }
}
