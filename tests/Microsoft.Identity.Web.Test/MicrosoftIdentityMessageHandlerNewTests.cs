// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web.Test.Common.Mocks;
using NSubstitute;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    public class MicrosoftIdentityMessageHandlerNewTests
    {
        private readonly IAuthorizationHeaderProvider _mockHeaderProvider;
        private readonly ILogger<MicrosoftIdentityMessageHandler> _mockLogger;

        public MicrosoftIdentityMessageHandlerNewTests()
        {
            _mockHeaderProvider = Substitute.For<IAuthorizationHeaderProvider>();
            _mockLogger = Substitute.For<ILogger<MicrosoftIdentityMessageHandler>>();
        }

        [Fact]
        public void Constructor_WithNullHeaderProvider_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new MicrosoftIdentityMessageHandler(null!));
        }

        [Fact]
        public void Constructor_WithValidParameters_CreatesInstance()
        {
            // Arrange
            var options = new MicrosoftIdentityMessageHandlerOptions
            {
                Scopes = { "https://graph.microsoft.com/.default" }
            };

            // Act
            var handler = new MicrosoftIdentityMessageHandler(_mockHeaderProvider, options, _mockLogger);

            // Assert
            Assert.NotNull(handler);
        }

        [Fact]
        public void SendAsync_WithValidConfiguration_CanBeUsedWithHttpClient()
        {
            // Arrange
            var options = new MicrosoftIdentityMessageHandlerOptions
            {
                Scopes = { "https://graph.microsoft.com/.default" }
            };

            var handler = new MicrosoftIdentityMessageHandler(_mockHeaderProvider, options, _mockLogger);

            // Act & Assert - should not throw
            using var client = new HttpClient(handler);
            Assert.NotNull(client);
        }

        [Fact]
        public void SendAsync_WithPerRequestConfiguration_CanBeUsedWithHttpClient()
        {
            // Arrange
            var defaultOptions = new MicrosoftIdentityMessageHandlerOptions
            {
                Scopes = { "https://graph.microsoft.com/.default" }
            };

            var handler = new MicrosoftIdentityMessageHandler(_mockHeaderProvider, defaultOptions, _mockLogger);

            // Act & Assert - should not throw
            using var client = new HttpClient(handler);
            
            // Create a request with per-request options
            var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com")
                .WithAuthenticationOptions(options => 
                {
                    options.Scopes.Add("https://custom.api/.default");
                });

            Assert.NotNull(request.GetAuthenticationOptions());
            Assert.Contains("https://custom.api/.default", request.GetAuthenticationOptions()!.Scopes);
        }

        [Fact]
        public void HttpRequestMessageExtensions_WithAuthenticationOptions_SetsAndGetsOptions()
        {
            // Arrange
            var options = new MicrosoftIdentityMessageHandlerOptions
            {
                Scopes = { "https://test.api/.default" }
            };

            var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com");

            // Act
            request.WithAuthenticationOptions(options);
            var retrievedOptions = request.GetAuthenticationOptions();

            // Assert
            Assert.NotNull(retrievedOptions);
            Assert.Contains("https://test.api/.default", retrievedOptions.Scopes);
        }

        [Fact]
        public void HttpRequestMessageExtensions_WithAuthenticationOptionsDelegate_ConfiguresOptions()
        {
            // Arrange
            var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com");

            // Act
            request.WithAuthenticationOptions(options =>
            {
                options.Scopes.Add("https://configured.api/.default");
            });
            
            var retrievedOptions = request.GetAuthenticationOptions();

            // Assert
            Assert.NotNull(retrievedOptions);
            Assert.Contains("https://configured.api/.default", retrievedOptions.Scopes);
        }

        [Fact]
        public void MicrosoftIdentityAuthenticationException_WithMessage_CreatesException()
        {
            // Arrange
            const string message = "Test authentication error";

            // Act
            var exception = new MicrosoftIdentityAuthenticationException(message);

            // Assert
            Assert.Equal(message, exception.Message);
        }

        [Fact]
        public void MicrosoftIdentityAuthenticationException_WithMessageAndInnerException_CreatesException()
        {
            // Arrange
            const string message = "Test authentication error";
            var innerException = new InvalidOperationException("Inner error");

            // Act
            var exception = new MicrosoftIdentityAuthenticationException(message, innerException);

            // Assert
            Assert.Equal(message, exception.Message);
            Assert.Equal(innerException, exception.InnerException);
        }
    }
}