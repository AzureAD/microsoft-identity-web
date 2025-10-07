// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web.Test.Common.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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

            var taf = new CustomTAF();
            taf.Services.AddManagedCertificateCredentials();
            taf.Services.AddTestServices();
            taf.Services.Configure<MicrosoftIdentityApplicationOptions>(options =>
            {
                options.Instance = instance;
                options.ClientId = clientId.ToString();
                options.TenantId = tenantId.ToString();
                options.ClientCredentials = [
                    new()
                {
                    SourceType = (CredentialSource)ManagedCertificateSourceLoader.Source,
                },
            ];
            });
            taf.Services.AddSingletonWithSelf<IHttpClientFactory, MockHttpClientFactory>();

            var provider = taf.Build();
            var testCertService = provider.GetRequiredService<ITestCertificateService>();
            var validCert = testCertService.AddCertificate(
                new CertificateDefinition()
                {
                    SubjectName = "CN=Test-Valid-Cert",
                    NotBefore = DateTime.UtcNow.AddDays(-1),
                    NotAfter = DateTime.UtcNow.AddYears(1),
                }.WithAppFmiId("eid", clientId, tenantId));

            var mockHttpFactory = provider.GetRequiredService<MockHttpClientFactory>();

            // Configure successful STS responses
            mockHttpFactory.ConfigureSuccessfulTokenResponse(authority, validCert);
            mockHttpFactory.ConfigureSuccessfulOpenIdConfiguration(authority);

            var ta = taf.GetTokenAcquirer(
                authority,
                clientId.ToString(),
                [
                    new()
                {
                    SourceType = (CredentialSource)ManagedCertificateSourceLoader.Source,
                }
                ]);

            var result = await ta.GetTokenForAppAsync(
                "https://graph.microsoft.com/.default",
                cancellationToken: this.TestContext.CancellationTokenSource.Token);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.AccessToken);

            // Verify observer was notified of successful usage
            var telemetry = provider.GetRequiredService<TestCertificateSelectionTelemetry>();
            Assert.AreEqual(1, telemetry.Successful.Count);
            Assert.AreEqual(0, telemetry.Failures.Count);

            var successEvent = telemetry.Successful[0];
            Assert.AreEqual(validCert.Thumbprint, successEvent.Certificate.Thumbprint);
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
}
