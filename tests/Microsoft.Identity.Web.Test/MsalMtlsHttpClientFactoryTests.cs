// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Identity.Web.Test.Common;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    public class MsalMtlsHttpClientFactoryTests : IDisposable
    {
        private readonly TestHttpClientFactory _httpClientFactory;
        private readonly MsalMtlsHttpClientFactory _factory;
        private bool _disposed = false;

        public MsalMtlsHttpClientFactoryTests()
        {
            _httpClientFactory = new TestHttpClientFactory();
            _factory = new MsalMtlsHttpClientFactory(_httpClientFactory);
        }

#if NET462
        [Fact]
        public void GetHttpClient_WithCertificateOnUnsupportedPlatform_ShouldThrowNotSupportedException()
        {
            // Arrange
            using var certificate = CreateTestCertificate();

            // Act & Assert
            Assert.Throws<NotSupportedException>(() => _factory.GetHttpClient(certificate));
        }
#else // NET462
        [Fact]
        public void Constructor_WithValidHttpClientFactory_ShouldNotThrow()
        {
            // Arrange & Act
            var factory = new MsalMtlsHttpClientFactory(_httpClientFactory);

            // Assert
            Assert.NotNull(factory);
        }

        [Fact]
        public void Constructor_WithNullHttpClientFactory_ShouldAcceptNull()
        {
            // Arrange & Act
            var factory = new MsalMtlsHttpClientFactory(null!);

            // Assert
            Assert.NotNull(factory);
        }

        [Fact]
        public void GetHttpClient_WithoutCertificate_ShouldReturnConfiguredHttpClient()
        {
            // Arrange & Act
            HttpClient actualHttpClient = _factory.GetHttpClient();

            // Assert
            Assert.NotNull(actualHttpClient);

            // Verify telemetry header is present
            Assert.True(actualHttpClient.DefaultRequestHeaders.Contains(Constants.TelemetryHeaderKey));

            var telemetryHeaderValues = actualHttpClient.DefaultRequestHeaders.GetValues(Constants.TelemetryHeaderKey);
            Assert.Single(telemetryHeaderValues);
        }

        [Fact]
        public void GetHttpClient_WithNullCertificate_ShouldReturnConfiguredHttpClient()
        {
            // Arrange & Act
            HttpClient actualHttpClient = _factory.GetHttpClient(null!);

            // Assert
            Assert.NotNull(actualHttpClient);
            Assert.True(actualHttpClient.DefaultRequestHeaders.Contains(Constants.TelemetryHeaderKey));
        }

        [Fact]
        public void GetHttpClient_WithSameCertificate_ShouldReturnCachedClient()
        {
            // Arrange
            using var certificate = CreateTestCertificate();

            // Act
            HttpClient firstClient = _factory.GetHttpClient(certificate);
            HttpClient secondClient = _factory.GetHttpClient(certificate);

            // Assert
            Assert.Same(firstClient, secondClient);
        }

        [Fact]
        public void GetHttpClient_WithCertificate_ShouldConfigureProperHeaders()
        {
            // Arrange
            using var certificate = CreateTestCertificate();

            // Act
            HttpClient httpClient = _factory.GetHttpClient(certificate);

            // Assert
            // Verify telemetry header
            Assert.True(httpClient.DefaultRequestHeaders.Contains(Constants.TelemetryHeaderKey));

            // Verify max response buffer size
            Assert.Equal(1024 * 1024, httpClient.MaxResponseContentBufferSize);
        }

        [Fact]
        public void GetHttpClient_CreatesClientFromFactory()
        {
            // Arrange & Act
            _factory.GetHttpClient();

            // Assert
            Assert.True(_httpClientFactory.CreateClientCalled);
        }

        [Fact]
        public void GetHttpClient_MultipleCalls_CallsFactoryEachTime()
        {
            // Arrange & Act
            _factory.GetHttpClient();
            _factory.GetHttpClient();

            // Assert
            Assert.Equal(2, _httpClientFactory.CreateClientCallCount);
        }

        private static X509Certificate2 CreateTestCertificate()
        {
            // Create a simple test certificate for mocking purposes
            // We don't need a real certificate with private key for HTTP client factory testing
            var bytes = Convert.FromBase64String(TestConstants.CertificateX5c);

#if NET9_0_OR_GREATER
            // Use the new X509CertificateLoader for .NET 9.0+
            return X509CertificateLoader.LoadCertificate(bytes);
#else // NET9_0_OR_GREATER
            // Use the legacy constructor for older frameworks
#pragma warning disable SYSLIB0057 // Type or member is obsolete
            return new X509Certificate2(bytes);
#pragma warning restore SYSLIB0057 // Type or member is obsolete
#endif // NET9_0_OR_GREATER
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _httpClientFactory?.Dispose();
                }
                _disposed = true;
            }
        }

        /// <summary>
        /// Simple test HttpClientFactory implementation for testing purposes.
        /// </summary>
        private sealed class TestHttpClientFactory : IHttpClientFactory, IDisposable
        {
            public bool CreateClientCalled { get; private set; }
            public int CreateClientCallCount { get; private set; }
            private bool _disposed = false;

            public HttpClient CreateClient(string name)
            {
                CreateClientCalled = true;
                CreateClientCallCount++;
                return new HttpClient();
            }

            public void Dispose()
            {
                if (!_disposed)
                {
                    _disposed = true;
                }
            }
        }
#endif // NET462
    }
}
