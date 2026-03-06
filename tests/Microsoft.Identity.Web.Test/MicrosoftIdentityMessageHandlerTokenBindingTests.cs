// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web.Test.Common;
using Microsoft.Identity.Web.Test.Common.Mocks;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    public class MicrosoftIdentityMessageHandlerTokenBindingTests
    {
        private readonly ILogger<MicrosoftIdentityMessageHandler> _mockLogger;

        public MicrosoftIdentityMessageHandlerTokenBindingTests()
        {
            _mockLogger = Substitute.For<ILogger<MicrosoftIdentityMessageHandler>>();
        }

        [Fact]
        public void Constructor_WithMtlsFactory_CreatesInstance()
        {
            // Arrange
            var mockHeaderProvider = Substitute.For<IAuthorizationHeaderProvider>();
            var mockMtlsFactory = Substitute.For<IMsalMtlsHttpClientFactory>();
            var options = new MicrosoftIdentityMessageHandlerOptions
            {
                Scopes = { "api://test/.default" },
                ProtocolScheme = "MTLS_POP",
                RequestAppToken = true
            };

            // Act
            var handler = new MicrosoftIdentityMessageHandler(
                mockHeaderProvider, options, mockMtlsFactory, _mockLogger);

            // Assert
            Assert.NotNull(handler);
        }

        [Fact]
        public void Constructor_WithNullMtlsFactory_CreatesInstance()
        {
            // Arrange
            var mockHeaderProvider = Substitute.For<IAuthorizationHeaderProvider>();

            // Act
            var handler = new MicrosoftIdentityMessageHandler(
                mockHeaderProvider, defaultOptions: null, mtlsHttpClientFactory: null, _mockLogger);

            // Assert
            Assert.NotNull(handler);
        }

        [Fact]
        public void Constructor_WithMtlsFactory_NullHeaderProvider_ThrowsArgumentNullException()
        {
            // Arrange
            var mockMtlsFactory = Substitute.For<IMsalMtlsHttpClientFactory>();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new MicrosoftIdentityMessageHandler(null!, null, mockMtlsFactory, _mockLogger));
        }

        [Fact]
        public void OriginalConstructor_ChainsToNewConstructor()
        {
            // Arrange
            var mockHeaderProvider = Substitute.For<IAuthorizationHeaderProvider>();
            var options = new MicrosoftIdentityMessageHandlerOptions
            {
                Scopes = { "api://test/.default" }
            };

            // Act - Use original constructor (without mtlsFactory)
            var handler = new MicrosoftIdentityMessageHandler(mockHeaderProvider, options, _mockLogger);

            // Assert
            Assert.NotNull(handler);
        }

        [Theory]
        [InlineData("MTLS_POP")]
        [InlineData("mtls_pop")]
        [InlineData("Mtls_Pop")]
        public async Task SendAsync_WithMtlsPopProtocolScheme_UsesBoundProvider(string protocolScheme)
        {
            // Arrange
            var testCertificate = CreateTestCertificate();
            var mockBoundProvider = Substitute.For<IAuthorizationHeaderProvider, IBoundAuthorizationHeaderProvider>();

            var authHeaderInfo = new AuthorizationHeaderInformation
            {
                AuthorizationHeaderValue = "MTLS_POP test-bound-token",
                BindingCertificate = testCertificate
            };

            var mockResult = new OperationResult<AuthorizationHeaderInformation, AuthorizationHeaderError>(authHeaderInfo);

            ((IBoundAuthorizationHeaderProvider)mockBoundProvider)
                .CreateBoundAuthorizationHeaderAsync(
                    Arg.Any<DownstreamApiOptions>(),
                    Arg.Any<ClaimsPrincipal>(),
                    Arg.Any<CancellationToken>())
                .Returns(mockResult);

            var mockMtlsHandler = new MockHttpMessageHandler
            {
                ExpectedMethod = HttpMethod.Get,
                ResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"result\": \"success\"}")
                }
            };
            var mockMtlsClient = new HttpClient(mockMtlsHandler);

            var mockMtlsFactory = Substitute.For<IMsalMtlsHttpClientFactory>();
            mockMtlsFactory.GetHttpClient(testCertificate).Returns(mockMtlsClient);

            var options = new MicrosoftIdentityMessageHandlerOptions
            {
                Scopes = { "api://test/.default" },
                ProtocolScheme = protocolScheme,
                RequestAppToken = true
            };

            var handler = new MicrosoftIdentityMessageHandler(
                mockBoundProvider, options, mockMtlsFactory, _mockLogger);

            // Need an inner handler for DelegatingHandler to work (even if we bypass it for mTLS)
            handler.InnerHandler = new MockHttpMessageHandler
            {
                ResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            };

            using var invoker = new HttpMessageInvoker(handler);
            var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/data");

            // Act
            var response = await invoker.SendAsync(request, CancellationToken.None);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // Verify bound provider was called
            await ((IBoundAuthorizationHeaderProvider)mockBoundProvider)
                .Received(1)
                .CreateBoundAuthorizationHeaderAsync(
                    Arg.Is<DownstreamApiOptions>(o =>
                        o.ProtocolScheme != null &&
                        o.ProtocolScheme.Equals("MTLS_POP", StringComparison.OrdinalIgnoreCase) &&
                        o.RequestAppToken == true &&
                        o.Scopes != null && o.Scopes.Contains("api://test/.default")),
                    Arg.Any<ClaimsPrincipal>(),
                    Arg.Any<CancellationToken>());

            // Verify regular CreateAuthorizationHeaderAsync was NOT called
            await ((IAuthorizationHeaderProvider)mockBoundProvider)
                .DidNotReceive()
                .CreateAuthorizationHeaderAsync(
                    Arg.Any<IEnumerable<string>>(),
                    Arg.Any<AuthorizationHeaderProviderOptions>(),
                    Arg.Any<ClaimsPrincipal>(),
                    Arg.Any<CancellationToken>());

            // Verify mTLS factory was used
            mockMtlsFactory.Received(1).GetHttpClient(testCertificate);

            // Verify authorization header was set on the request
            Assert.Equal("MTLS_POP test-bound-token", mockMtlsHandler.ActualRequestMessage.Headers.GetValues("Authorization").First());
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("Bearer")]
        public async Task SendAsync_WithNonMtlsProtocolScheme_UsesRegularProvider(string? protocolScheme)
        {
            // Arrange
            var mockBoundProvider = Substitute.For<IAuthorizationHeaderProvider, IBoundAuthorizationHeaderProvider>();

            ((IAuthorizationHeaderProvider)mockBoundProvider)
                .CreateAuthorizationHeaderAsync(
                    Arg.Any<IEnumerable<string>>(),
                    Arg.Any<AuthorizationHeaderProviderOptions>(),
                    Arg.Any<ClaimsPrincipal>(),
                    Arg.Any<CancellationToken>())
                .Returns("Bearer regular-token");

            var mockMtlsFactory = Substitute.For<IMsalMtlsHttpClientFactory>();

            var options = new MicrosoftIdentityMessageHandlerOptions
            {
                Scopes = { "api://test/.default" }
            };

            if (protocolScheme is not null)
            {
                options.ProtocolScheme = protocolScheme;
            }

            var innerHandler = new MockHttpMessageHandler
            {
                ExpectedMethod = HttpMethod.Get,
                ResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"result\": \"success\"}")
                }
            };

            var handler = new MicrosoftIdentityMessageHandler(
                mockBoundProvider, options, mockMtlsFactory, _mockLogger);
            handler.InnerHandler = innerHandler;

            using var invoker = new HttpMessageInvoker(handler);
            var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/data");

            // Act
            var response = await invoker.SendAsync(request, CancellationToken.None);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // Verify regular provider was called
            await ((IAuthorizationHeaderProvider)mockBoundProvider)
                .Received(1)
                .CreateAuthorizationHeaderAsync(
                    Arg.Any<IEnumerable<string>>(),
                    Arg.Any<AuthorizationHeaderProviderOptions>(),
                    Arg.Any<ClaimsPrincipal>(),
                    Arg.Any<CancellationToken>());

            // Verify bound provider was NOT called
            await ((IBoundAuthorizationHeaderProvider)mockBoundProvider)
                .DidNotReceive()
                .CreateBoundAuthorizationHeaderAsync(
                    Arg.Any<DownstreamApiOptions>(),
                    Arg.Any<ClaimsPrincipal>(),
                    Arg.Any<CancellationToken>());

            // Verify mTLS factory was NOT used
            mockMtlsFactory.DidNotReceive().GetHttpClient(Arg.Any<X509Certificate2>());
        }

        [Fact]
        public async Task SendAsync_WithMtlsPop_ProviderNotBound_UsesRegularPath()
        {
            // Arrange - Provider does NOT implement IBoundAuthorizationHeaderProvider
            var mockRegularProvider = Substitute.For<IAuthorizationHeaderProvider>();

            mockRegularProvider
                .CreateAuthorizationHeaderAsync(
                    Arg.Any<IEnumerable<string>>(),
                    Arg.Any<AuthorizationHeaderProviderOptions>(),
                    Arg.Any<ClaimsPrincipal>(),
                    Arg.Any<CancellationToken>())
                .Returns("Bearer fallback-token");

            var mockMtlsFactory = Substitute.For<IMsalMtlsHttpClientFactory>();

            var options = new MicrosoftIdentityMessageHandlerOptions
            {
                Scopes = { "api://test/.default" },
                ProtocolScheme = "MTLS_POP",
                RequestAppToken = true
            };

            var innerHandler = new MockHttpMessageHandler
            {
                ExpectedMethod = HttpMethod.Get,
                ResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            };

            var handler = new MicrosoftIdentityMessageHandler(
                mockRegularProvider, options, mockMtlsFactory, _mockLogger);
            handler.InnerHandler = innerHandler;

            using var invoker = new HttpMessageInvoker(handler);
            var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/data");

            // Act
            var response = await invoker.SendAsync(request, CancellationToken.None);

            // Assert - Falls back to regular path since provider doesn't implement IBoundAuthorizationHeaderProvider
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            await mockRegularProvider
                .Received(1)
                .CreateAuthorizationHeaderAsync(
                    Arg.Any<IEnumerable<string>>(),
                    Arg.Any<AuthorizationHeaderProviderOptions>(),
                    Arg.Any<ClaimsPrincipal>(),
                    Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task SendAsync_WithMtlsPop_NullBindingCertificate_UsesBaseSendAsync()
        {
            // Arrange
            var mockBoundProvider = Substitute.For<IAuthorizationHeaderProvider, IBoundAuthorizationHeaderProvider>();

            var authHeaderInfo = new AuthorizationHeaderInformation
            {
                AuthorizationHeaderValue = "MTLS_POP token-no-cert",
                BindingCertificate = null // No binding certificate returned
            };

            var mockResult = new OperationResult<AuthorizationHeaderInformation, AuthorizationHeaderError>(authHeaderInfo);

            ((IBoundAuthorizationHeaderProvider)mockBoundProvider)
                .CreateBoundAuthorizationHeaderAsync(
                    Arg.Any<DownstreamApiOptions>(),
                    Arg.Any<ClaimsPrincipal>(),
                    Arg.Any<CancellationToken>())
                .Returns(mockResult);

            var mockMtlsFactory = Substitute.For<IMsalMtlsHttpClientFactory>();

            var options = new MicrosoftIdentityMessageHandlerOptions
            {
                Scopes = { "api://test/.default" },
                ProtocolScheme = "MTLS_POP",
                RequestAppToken = true
            };

            var innerHandler = new MockHttpMessageHandler
            {
                ExpectedMethod = HttpMethod.Get,
                ResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            };

            var handler = new MicrosoftIdentityMessageHandler(
                mockBoundProvider, options, mockMtlsFactory, _mockLogger);
            handler.InnerHandler = innerHandler;

            using var invoker = new HttpMessageInvoker(handler);
            var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/data");

            // Act
            var response = await invoker.SendAsync(request, CancellationToken.None);

            // Assert - Sends through base handler pipeline, not mTLS factory
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            mockMtlsFactory.DidNotReceive().GetHttpClient(Arg.Any<X509Certificate2>());
        }

        [Fact]
        public async Task SendAsync_WithMtlsPop_NullMtlsFactory_UsesBaseSendAsync()
        {
            // Arrange
            var testCertificate = CreateTestCertificate();
            var mockBoundProvider = Substitute.For<IAuthorizationHeaderProvider, IBoundAuthorizationHeaderProvider>();

            var authHeaderInfo = new AuthorizationHeaderInformation
            {
                AuthorizationHeaderValue = "MTLS_POP token-with-cert",
                BindingCertificate = testCertificate
            };

            var mockResult = new OperationResult<AuthorizationHeaderInformation, AuthorizationHeaderError>(authHeaderInfo);

            ((IBoundAuthorizationHeaderProvider)mockBoundProvider)
                .CreateBoundAuthorizationHeaderAsync(
                    Arg.Any<DownstreamApiOptions>(),
                    Arg.Any<ClaimsPrincipal>(),
                    Arg.Any<CancellationToken>())
                .Returns(mockResult);

            var options = new MicrosoftIdentityMessageHandlerOptions
            {
                Scopes = { "api://test/.default" },
                ProtocolScheme = "MTLS_POP",
                RequestAppToken = true
            };

            var innerHandler = new MockHttpMessageHandler
            {
                ExpectedMethod = HttpMethod.Get,
                ResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            };

            // mtlsHttpClientFactory is null
            var handler = new MicrosoftIdentityMessageHandler(
                mockBoundProvider, options, mtlsHttpClientFactory: null, _mockLogger);
            handler.InnerHandler = innerHandler;

            using var invoker = new HttpMessageInvoker(handler);
            var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/data");

            // Act
            var response = await invoker.SendAsync(request, CancellationToken.None);

            // Assert - Sends through base handler pipeline since no mTLS factory is available
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task SendAsync_WithMtlsPop_BoundProviderFailure_ThrowsAuthenticationException()
        {
            // Arrange
            var mockBoundProvider = Substitute.For<IAuthorizationHeaderProvider, IBoundAuthorizationHeaderProvider>();

            var mockResult = new OperationResult<AuthorizationHeaderInformation, AuthorizationHeaderError>(
                new AuthorizationHeaderError("token_acquisition_failed", "Failed to acquire token"));

            ((IBoundAuthorizationHeaderProvider)mockBoundProvider)
                .CreateBoundAuthorizationHeaderAsync(
                    Arg.Any<DownstreamApiOptions>(),
                    Arg.Any<ClaimsPrincipal>(),
                    Arg.Any<CancellationToken>())
                .Returns(mockResult);

            var mockMtlsFactory = Substitute.For<IMsalMtlsHttpClientFactory>();

            var options = new MicrosoftIdentityMessageHandlerOptions
            {
                Scopes = { "api://test/.default" },
                ProtocolScheme = "MTLS_POP",
                RequestAppToken = true
            };

            var handler = new MicrosoftIdentityMessageHandler(
                mockBoundProvider, options, mockMtlsFactory, _mockLogger);
            handler.InnerHandler = new MockHttpMessageHandler
            {
                ResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            };

            using var invoker = new HttpMessageInvoker(handler);
            var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/data");

            // Act & Assert
            var exception = await Assert.ThrowsAsync<MicrosoftIdentityAuthenticationException>(
                () => invoker.SendAsync(request, CancellationToken.None));

            Assert.Contains("mTLS PoP", exception.Message, StringComparison.Ordinal);
        }

        [Fact]
        public async Task SendAsync_WithMtlsPop_BoundProviderException_WrapsInAuthenticationException()
        {
            // Arrange
            var mockBoundProvider = Substitute.For<IAuthorizationHeaderProvider, IBoundAuthorizationHeaderProvider>();

            ((IBoundAuthorizationHeaderProvider)mockBoundProvider)
                .CreateBoundAuthorizationHeaderAsync(
                    Arg.Any<DownstreamApiOptions>(),
                    Arg.Any<ClaimsPrincipal>(),
                    Arg.Any<CancellationToken>())
                .ThrowsAsync(new InvalidOperationException("Certificate not found"));

            var mockMtlsFactory = Substitute.For<IMsalMtlsHttpClientFactory>();

            var options = new MicrosoftIdentityMessageHandlerOptions
            {
                Scopes = { "api://test/.default" },
                ProtocolScheme = "MTLS_POP",
                RequestAppToken = true
            };

            var handler = new MicrosoftIdentityMessageHandler(
                mockBoundProvider, options, mockMtlsFactory, _mockLogger);
            handler.InnerHandler = new MockHttpMessageHandler
            {
                ResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            };

            using var invoker = new HttpMessageInvoker(handler);
            var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/data");

            // Act & Assert
            var exception = await Assert.ThrowsAsync<MicrosoftIdentityAuthenticationException>(
                () => invoker.SendAsync(request, CancellationToken.None));

            Assert.IsType<InvalidOperationException>(exception.InnerException);
            Assert.Equal("Certificate not found", exception.InnerException.Message);
        }

        [Fact]
        public async Task SendAsync_WithMtlsPop_PerRequestOptions_OverridesDefault()
        {
            // Arrange
            var testCertificate = CreateTestCertificate();
            var mockBoundProvider = Substitute.For<IAuthorizationHeaderProvider, IBoundAuthorizationHeaderProvider>();

            // Setup regular provider for default options
            ((IAuthorizationHeaderProvider)mockBoundProvider)
                .CreateAuthorizationHeaderAsync(
                    Arg.Any<IEnumerable<string>>(),
                    Arg.Any<AuthorizationHeaderProviderOptions>(),
                    Arg.Any<ClaimsPrincipal>(),
                    Arg.Any<CancellationToken>())
                .Returns("Bearer regular-token");

            // Setup bound provider for per-request mTLS PoP options
            var authHeaderInfo = new AuthorizationHeaderInformation
            {
                AuthorizationHeaderValue = "MTLS_POP per-request-token",
                BindingCertificate = testCertificate
            };
            var mockResult = new OperationResult<AuthorizationHeaderInformation, AuthorizationHeaderError>(authHeaderInfo);

            ((IBoundAuthorizationHeaderProvider)mockBoundProvider)
                .CreateBoundAuthorizationHeaderAsync(
                    Arg.Any<DownstreamApiOptions>(),
                    Arg.Any<ClaimsPrincipal>(),
                    Arg.Any<CancellationToken>())
                .Returns(mockResult);

            var mockMtlsHandler = new MockHttpMessageHandler
            {
                ExpectedMethod = HttpMethod.Get,
                ResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            };
            var mockMtlsClient = new HttpClient(mockMtlsHandler);

            var mockMtlsFactory = Substitute.For<IMsalMtlsHttpClientFactory>();
            mockMtlsFactory.GetHttpClient(testCertificate).Returns(mockMtlsClient);

            // Default options: regular Bearer
            var defaultOptions = new MicrosoftIdentityMessageHandlerOptions
            {
                Scopes = { "api://test/.default" }
            };

            var handler = new MicrosoftIdentityMessageHandler(
                mockBoundProvider, defaultOptions, mockMtlsFactory, _mockLogger);
            handler.InnerHandler = new MockHttpMessageHandler
            {
                ExpectedMethod = HttpMethod.Get,
                ResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            };

            using var invoker = new HttpMessageInvoker(handler);

            // Per-request options: mTLS PoP
            var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/data")
                .WithAuthenticationOptions(new MicrosoftIdentityMessageHandlerOptions
                {
                    Scopes = { "api://test/.default" },
                    ProtocolScheme = "MTLS_POP",
                    RequestAppToken = true
                });

            // Act
            var response = await invoker.SendAsync(request, CancellationToken.None);

            // Assert - mTLS PoP path was used via per-request options
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            await ((IBoundAuthorizationHeaderProvider)mockBoundProvider)
                .Received(1)
                .CreateBoundAuthorizationHeaderAsync(
                    Arg.Any<DownstreamApiOptions>(),
                    Arg.Any<ClaimsPrincipal>(),
                    Arg.Any<CancellationToken>());

            mockMtlsFactory.Received(1).GetHttpClient(testCertificate);
        }

        [Fact]
        public async Task SendAsync_WithMtlsPop_DownstreamApiOptionsHaveCorrectProperties()
        {
            // Arrange
            var testCertificate = CreateTestCertificate();
            var mockBoundProvider = Substitute.For<IAuthorizationHeaderProvider, IBoundAuthorizationHeaderProvider>();

            DownstreamApiOptions? capturedOptions = null;

            var authHeaderInfo = new AuthorizationHeaderInformation
            {
                AuthorizationHeaderValue = "MTLS_POP captured-token",
                BindingCertificate = testCertificate
            };
            var mockResult = new OperationResult<AuthorizationHeaderInformation, AuthorizationHeaderError>(authHeaderInfo);

            ((IBoundAuthorizationHeaderProvider)mockBoundProvider)
                .CreateBoundAuthorizationHeaderAsync(
                    Arg.Do<DownstreamApiOptions>(o => capturedOptions = o),
                    Arg.Any<ClaimsPrincipal>(),
                    Arg.Any<CancellationToken>())
                .Returns(mockResult);

            var mockMtlsClient = new HttpClient(new MockHttpMessageHandler
            {
                ExpectedMethod = HttpMethod.Get,
                ResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            });

            var mockMtlsFactory = Substitute.For<IMsalMtlsHttpClientFactory>();
            mockMtlsFactory.GetHttpClient(testCertificate).Returns(mockMtlsClient);

            var options = new MicrosoftIdentityMessageHandlerOptions
            {
                Scopes = { "api://test/read", "api://test/write" },
                ProtocolScheme = "MTLS_POP",
                RequestAppToken = true,
                BaseUrl = "https://api.example.com",
                RelativePath = "api/data"
            };

            var handler = new MicrosoftIdentityMessageHandler(
                mockBoundProvider, options, mockMtlsFactory, _mockLogger);
            handler.InnerHandler = new MockHttpMessageHandler
            {
                ExpectedMethod = HttpMethod.Get,
                ResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            };

            using var invoker = new HttpMessageInvoker(handler);
            var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/data");

            // Act
            await invoker.SendAsync(request, CancellationToken.None);

            // Assert - Verify the DownstreamApiOptions were properly constructed
            Assert.NotNull(capturedOptions);
            Assert.Equal("MTLS_POP", capturedOptions!.ProtocolScheme);
            Assert.True(capturedOptions.RequestAppToken);
            Assert.NotNull(capturedOptions.Scopes);
            Assert.Contains("api://test/read", (IEnumerable<string>)capturedOptions.Scopes!);
            Assert.Contains("api://test/write", (IEnumerable<string>)capturedOptions.Scopes!);
            Assert.Equal("https://api.example.com", capturedOptions.BaseUrl);
            Assert.Equal("api/data", capturedOptions.RelativePath);
        }

        [Fact]
        public void ExtensionMethods_AddMicrosoftIdentityMessageHandler_RegistersWithMtlsFactory()
        {
            // Arrange
            var services = new ServiceCollection();
            var mockHeaderProvider = Substitute.For<IAuthorizationHeaderProvider>();
            services.AddSingleton(mockHeaderProvider);

            var builder = services.AddHttpClient("MtlsPopClient");

            // Act
            builder.AddMicrosoftIdentityMessageHandler(options =>
            {
                options.Scopes.Add("api://test/.default");
                options.ProtocolScheme = "MTLS_POP";
                options.RequestAppToken = true;
            });

            // Assert - Build the service provider and create a client (ensures no DI errors)
            var serviceProvider = services.BuildServiceProvider();
            var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
            var client = httpClientFactory.CreateClient("MtlsPopClient");

            Assert.NotNull(client);
        }

        [Fact]
        public void ExtensionMethods_Parameterless_RegistersWithMtlsFactory()
        {
            // Arrange
            var services = new ServiceCollection();
            var mockHeaderProvider = Substitute.For<IAuthorizationHeaderProvider>();
            services.AddSingleton(mockHeaderProvider);

            var builder = services.AddHttpClient("FlexibleClient");

            // Act
            builder.AddMicrosoftIdentityMessageHandler();

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
            var client = httpClientFactory.CreateClient("FlexibleClient");

            Assert.NotNull(client);
        }

        [Fact]
        public void ExtensionMethods_WithOptionsInstance_RegistersWithMtlsFactory()
        {
            // Arrange
            var services = new ServiceCollection();
            var mockHeaderProvider = Substitute.For<IAuthorizationHeaderProvider>();
            services.AddSingleton(mockHeaderProvider);

            var builder = services.AddHttpClient("MtlsClient");
            var options = new MicrosoftIdentityMessageHandlerOptions
            {
                Scopes = { "api://test/.default" },
                ProtocolScheme = "MTLS_POP",
                RequestAppToken = true
            };

            // Act
            builder.AddMicrosoftIdentityMessageHandler(options);

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
            var client = httpClientFactory.CreateClient("MtlsClient");

            Assert.NotNull(client);
        }

        private static X509Certificate2 CreateTestCertificate()
        {
            var bytes = Convert.FromBase64String(TestConstants.CertificateX5c);

#if NET9_0_OR_GREATER
            return X509CertificateLoader.LoadCertificate(bytes);
#else
#pragma warning disable SYSLIB0057
            return new X509Certificate2(bytes);
#pragma warning restore SYSLIB0057
#endif
        }
    }
}
