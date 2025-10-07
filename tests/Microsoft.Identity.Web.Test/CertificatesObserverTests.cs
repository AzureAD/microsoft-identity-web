// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web.Experimental;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    public class CertificatesObserverTests
    {
        [Fact]
        public async Task SuccessfulTokenSelection()
        {
            var clientId = Guid.NewGuid();
            var tenantId = Guid.NewGuid();
            var instance = "https://login.microsoftonline.com/";
            var authority = instance + tenantId;

            var validCert = CreateCertificate("CN=TestCert");
            var validDescription = new CredentialDescription
            {
                SourceType = CredentialSource.Certificate,
                Certificate = validCert,
            };

            var taf = new CustomTAF();
            taf.Services.Configure<MicrosoftIdentityApplicationOptions>(options =>
            {
                options.Instance = instance;
                options.ClientId = clientId.ToString();
                options.TenantId = tenantId.ToString();
                options.ClientCredentials = [validDescription];
            });
            taf.Services.AddSingletonWithSelf<IHttpClientFactory, MockHttpClientFactory>();

            // Add two observers so that we can check if multiple observers works as intended.
            TestCertificatesObserver observer1 = new TestCertificatesObserver();
            taf.Services.AddSingleton<ICertificatesObserver>(observer1);
            TestCertificatesObserver observer2 = new TestCertificatesObserver();
            taf.Services.AddSingleton<ICertificatesObserver>(observer2);

            var provider = taf.Build();

            var mockHttpFactory = provider.GetRequiredService<MockHttpClientFactory>();

            // Configure successful STS responses
            mockHttpFactory.ConfigureSuccessfulTokenResponse(authority, validCert);
            mockHttpFactory.ConfigureSuccessfulOpenIdConfiguration(authority);

            var ta = taf.GetTokenAcquirer(
                authority,
                clientId.ToString(),
                [validDescription]);

            var result = await ta.GetTokenForAppAsync(
                "https://graph.microsoft.com/.default");

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.AccessToken);

            // Verify observer was notified of successful usage
            Assert.Equal(observer1.Events.Count, observer2.Events.Count);

            // First event was selection.
            observer1.Events.TryDequeue(out var eventArg);
            Assert.NotNull(eventArg);
            Assert.Equal(CerticateObserverAction.Selected, eventArg.Action);
            Assert.Equal(validCert, eventArg.Certificate);
            Assert.Equal(validDescription, eventArg.CredentialDescription);

            // Second event was successful usage.
            observer1.Events.TryDequeue(out eventArg);
            Assert.NotNull(eventArg);
            Assert.Equal(CerticateObserverAction.SuccessfullyUsed, eventArg.Action);
            Assert.Equal(validCert, eventArg.Certificate);
            Assert.Equal(validDescription, eventArg.CredentialDescription);

            // No further events
            Assert.Empty(observer1.Events);

            // Rerun, should only get success event, no selection.
            result = await ta.GetTokenForAppAsync(
                "https://graph.microsoft.com/.default");
            observer1.Events.TryDequeue(out eventArg);
            Assert.NotNull(eventArg);
            Assert.Equal(CerticateObserverAction.SuccessfullyUsed, eventArg.Action);
            Assert.Empty(observer1.Events);
        }

        private X509Certificate2 CreateCertificate(string certName)
        {
            // Create the self signed certificate
#if ECDsa
            var ecdsa = ECDsa.Create(); // generate asymmetric key pair
            var req = new CertificateRequest($"CN={certName}", ecdsa, HashAlgorithmName.SHA256);
#else
            using RSA rsa = RSA.Create(); // generate asymmetric key pair
            var req = new CertificateRequest($"CN={certName}", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
#endif

            var cert = req.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddDays(1));

            byte[] bytes = cert.Export(X509ContentType.Pfx, (string?)null);
#pragma warning disable SYSLIB0057 // Type or member is obsolete
            X509Certificate2 certWithPrivateKey = new(bytes);
#pragma warning restore SYSLIB0057 // Type or member is obsolete

            return certWithPrivateKey;
        }

        private class TestCertificatesObserver : ICertificatesObserver
        {
            public Queue<CertificateChangeEventArg> Events { get; } = new();

            public void OnClientCertificateChanged(CertificateChangeEventArg e) => Events.Enqueue(e);
        }


        /// <summary>
        /// Mock HTTP client factory for simulating STS backend interactions.
        /// </summary>
        private class MockHttpClientFactory : IHttpClientFactory
        {
            private readonly MockHttpMessageHandler handler = new();

            /// <inheritdoc/>
            public HttpClient CreateClient(string name) => new(this.handler);

            public void ConfigureSuccessfulTokenResponse(string authority, X509Certificate2 certificate)
                => this.handler.ConfigureSuccessfulTokenResponse(authority, certificate);

            public void ConfigureFailedTokenResponse(string authority, X509Certificate2 certificate, string errorCode, HttpStatusCode statusCode = HttpStatusCode.BadRequest)
                => this.handler.ConfigureFailedTokenResponse(authority, certificate, errorCode, statusCode);

            public void ConfigureSuccessfulOpenIdConfiguration(string authority)
                => this.handler.ConfigureSuccessfulOpenIdConfiguration(authority);
        }

        /// <summary>
        /// Mock HTTP message handler that simulates STS responses.
        /// </summary>
        private class MockHttpMessageHandler : HttpMessageHandler
        {
            private readonly Dictionary<string, Func<HttpRequestMessage, HttpResponseMessage>> responseMap = [];

            public void ConfigureSuccessfulTokenResponse(string authority, X509Certificate2 certificate)
            {
                var tokenEndpoint = $"{authority.TrimEnd('/')}/oauth2/v2.0/token";

                this.responseMap[tokenEndpoint] = request =>
                {
                    var token = GenerateMockAccessToken(certificate);
                    var response = new
                    {
                        access_token = token,
                        token_type = "Bearer",
                        expires_in = 3600,
                        scope = "https://graph.microsoft.com/.default",
                    };

                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(
                            System.Text.Json.JsonSerializer.Serialize(response),
                            Encoding.UTF8,
                            "application/json"),
                    };
                };
            }

            public void ConfigureFailedTokenResponse(string authority, X509Certificate2 certificate, string errorCode, HttpStatusCode statusCode = HttpStatusCode.BadRequest)
            {
                var tokenEndpoint = $"{authority.TrimEnd('/')}/oauth2/v2.0/token";

                this.responseMap[tokenEndpoint] = request =>
                {
                    var errorResponse = new
                    {
                        error = errorCode,
                        error_description = $"Mock error: {errorCode}",
                        error_codes = new[] { 50000 },
                        timestamp = DateTime.UtcNow,
                    };

                    return new HttpResponseMessage(statusCode)
                    {
                        Content = new StringContent(
                            System.Text.Json.JsonSerializer.Serialize(errorResponse),
                            Encoding.UTF8,
                            "application/json"),
                    };
                };
            }

            public void ConfigureSuccessfulOpenIdConfiguration(string authority)
            {
                var configEndpoint = $"{authority.TrimEnd('/')}/.well-known/openid_configuration";

                this.responseMap[configEndpoint] = request =>
                {
                    var config = new
                    {
                        token_endpoint = $"{authority}/oauth2/v2.0/token",
                        issuer = authority,
                    };

                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(
                            System.Text.Json.JsonSerializer.Serialize(config),
                            Encoding.UTF8,
                            "application/json"),
                    };
                };
            }

            /// <inheritdoc/>
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var uri = request.RequestUri?.ToString() ?? string.Empty;

                foreach (var kvp in this.responseMap)
                {
                    if (uri.StartsWith(kvp.Key, StringComparison.OrdinalIgnoreCase))
                    {
                        return Task.FromResult(kvp.Value(request));
                    }
                }

                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
            }

            private static string GenerateMockAccessToken(X509Certificate2 certificate)
            {
                var header = Convert.ToBase64String(Encoding.UTF8.GetBytes("{\"typ\":\"JWT\",\"alg\":\"RS256\"}"));
                var payload = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{{\"aud\":\"https://graph.microsoft.com/\",\"cert_thumbprint\":\"{certificate.Thumbprint}\",\"exp\":{DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds()},\"scp\":\"https://graph.microsoft.com/.default\"}}"));
                var signature = Convert.ToBase64String(Encoding.UTF8.GetBytes("mock-signature"));

                return $"{header}.{payload}.{signature}";
            }
        }

        /// <summary>
        /// Result of token acquisition operation.
        /// </summary>
        private class TokenAcquisitionResult
        {
            public string AccessToken { get; set; } = null!;

            public X509Certificate2? UsedCertificate { get; set; }

            public DateTimeOffset ExpiresOn { get; set; }
        }

        /// <summary>
        /// Represents a certificate usage event for telemetry.
        /// </summary>
        private class CertificateUsageEvent
        {
            public Guid ClientId { get; set; }

            public Guid? TenantId { get; set; }

            public string CertificateThumbprint { get; set; } = null!;

            public string? FailureReason { get; set; }

            public DateTime Timestamp { get; set; }
        }

        private class CustomTAF : TokenAcquirerFactory
        {
            public CustomTAF()
            {
                this.Services.AddTokenAcquisition();
                this.Services.AddHttpClient();
                this.Services.AddSingleton<ITokenAcquirerFactory, DefaultTokenAcquirerFactoryImplementation>();
            }

            protected override string DefineConfiguration(IConfigurationBuilder builder)
            {
                return AppContext.BaseDirectory;
            }
        }
    }

#pragma warning disable SA1402 // File may only contain a single type. Acceptable for extension methods.
    file static class TestSericeExtensions
#pragma warning restore SA1402 // File may only contain a single type
    {
        public static IServiceCollection AddSingletonWithSelf<TInterface, TImplementation>(this IServiceCollection services)
            where TImplementation : class, TInterface
            where TInterface : class
        {
            return services
                .AddSingleton<TImplementation>()
                .AddSingleton<TInterface>(s => s.GetRequiredService<TImplementation>());
        }
    }
}
