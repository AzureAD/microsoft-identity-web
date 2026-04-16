// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
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
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web.Experimental;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    public class CertificatesObserverTests
    {
        [Fact]
        public async Task ObserverSendsCorrectEvents_Tokens()
        {
            static void RemoveCertificate(X509Certificate2? certificate)
            {
                if (certificate is null)
                {
                    return;
                }

                using X509Store x509Store = new(StoreName.My, StoreLocation.CurrentUser);
                x509Store.Open(OpenFlags.ReadWrite);
                x509Store.Remove(certificate);
                x509Store.Close();
            }

            X509Certificate2? cert1 = null;
            X509Certificate2? cert2 = null;
            try
            {
                var clientId = Guid.NewGuid();
                var tenantId = Guid.NewGuid();
                var instance = "https://login.microsoftonline.com/";
                var authority = instance + tenantId + "/";

                string certName = $"CN=TestCert-{Guid.NewGuid():N}";
                cert1 = CreateAndInstallCertificate(certName);

                var description = new CredentialDescription
                {
                    SourceType = CredentialSource.StoreWithDistinguishedName,
                    CertificateDistinguishedName = cert1.SubjectName.Name,
                    CertificateStorePath = "CurrentUser/My",
                };

                var taf = new CustomTAF();
                taf.Services.Configure<MicrosoftIdentityApplicationOptions>(options =>
                {
                    options.Instance = instance;
                    options.ClientId = clientId.ToString();
                    options.TenantId = tenantId.ToString();
                    options.ClientCredentials = [description];
                });
                taf.Services.AddMockClientFactory(description);

                // Add two observers so that we can check if multiple observers works as intended.
                TestCertificatesObserver observer1 = new TestCertificatesObserver();
                taf.Services.AddSingleton<ICertificatesObserver>(observer1);
                TestCertificatesObserver observer2 = new TestCertificatesObserver();
                taf.Services.AddSingleton<ICertificatesObserver>(observer2);

                var provider = taf.Build();

                var mockHttpFactory = provider.GetRequiredService<MockHttpClientFactory>();

                // Configure successful STS responses
                mockHttpFactory.ConfigureSuccessfulTokenResponse(authority);
                mockHttpFactory.ValidCertificates.Add(cert1);

                var ta = taf.GetTokenAcquirer(
                    authority,
                    clientId.ToString(),
                    [description]);

                var result = await ta.GetTokenForAppAsync(
                    "https://graph.microsoft.com/.default");

                // Assert
                Assert.NotNull(result);
                Assert.NotNull(result.AccessToken);

                // Verify both observers got all events.
                Assert.Equal(observer1.Events.Count, observer2.Events.Count);

                // First event was selection.
                observer1.Events.TryDequeue(out var eventArg);
                Assert.NotNull(eventArg);
                Assert.Equal(CerticateObserverAction.Selected, eventArg.Action);
                Assert.Equal(cert1, eventArg.Certificate);
                Assert.Equal(description, eventArg.CredentialDescription);
                Assert.NotNull(eventArg.CredentialSourceLoaderParameters);
                Assert.Equal(ProtocolNames.Bearer, eventArg.CredentialSourceLoaderParameters.Protocol);
                Assert.Equal(new Uri(authority), new Uri(eventArg.CredentialSourceLoaderParameters.Authority));
                Assert.Equal(clientId.ToString(), eventArg.CredentialSourceLoaderParameters.ClientId);

                // Second event was successful usage.
                observer1.Events.TryDequeue(out eventArg);
                Assert.NotNull(eventArg);
                Assert.Equal(CerticateObserverAction.SuccessfullyUsed, eventArg.Action);
                Assert.Equal(cert1, eventArg.Certificate);
                Assert.Equal(description, eventArg.CredentialDescription);
                Assert.NotNull(eventArg.CredentialSourceLoaderParameters);
                Assert.Equal(ProtocolNames.Bearer, eventArg.CredentialSourceLoaderParameters.Protocol);
                Assert.Equal(new Uri(authority), new Uri(eventArg.CredentialSourceLoaderParameters.Authority));
                Assert.Equal(clientId.ToString(), eventArg.CredentialSourceLoaderParameters.ClientId);

                // No further events
                Assert.Empty(observer1.Events);

                // Rerun, should only get success event, no selection.
                result = await ta.GetTokenForAppAsync(
                    "https://graph.microsoft.com/.default",
                    new AcquireTokenOptions
                    {
                        ForceRefresh = true // Exceptionnaly as we want to test the cert rotation.
                    });
                observer1.Events.TryDequeue(out eventArg);
                Assert.NotNull(eventArg);
                Assert.Equal(CerticateObserverAction.SuccessfullyUsed, eventArg.Action);
                Assert.Empty(observer1.Events);

                // Change out the cert, so that if it reloads there will be a new one
                RemoveCertificate(cert1);
                cert2 = CreateAndInstallCertificate(certName);

                // Rerun but it fails this time
                mockHttpFactory.ValidCertificates.Clear();
                mockHttpFactory.ValidCertificates.Add(cert2);
                result = await ta.GetTokenForAppAsync(
                    "https://graph.microsoft.com/.default",
                    new AcquireTokenOptions
                    {
                        ForceRefresh = true // Exceptionnaly as we want to test the cert rotation.
                    });

                // First it deselects the old cert.
                observer1.Events.TryDequeue(out eventArg);
                Assert.NotNull(eventArg);
                Assert.Equal(CerticateObserverAction.Deselected, eventArg.Action);
                Assert.Equal(cert1, eventArg.Certificate);
                Assert.NotNull(eventArg.CredentialSourceLoaderParameters);
                Assert.Equal(ProtocolNames.Bearer, eventArg.CredentialSourceLoaderParameters.Protocol);
                Assert.Equal(new Uri(authority), new Uri(eventArg.CredentialSourceLoaderParameters.Authority));
                Assert.Equal(clientId.ToString(), eventArg.CredentialSourceLoaderParameters.ClientId);

                // Then, it uses a new cert successfully.
                observer1.Events.TryDequeue(out eventArg);
                Assert.NotNull(eventArg);
                Assert.Equal(CerticateObserverAction.Selected, eventArg.Action);
                Assert.Equal(cert2, eventArg.Certificate);
                observer1.Events.TryDequeue(out eventArg);
                Assert.NotNull(eventArg);
                Assert.Equal(CerticateObserverAction.SuccessfullyUsed, eventArg.Action);
                Assert.Equal(cert2, eventArg.Certificate);

                // No events left.
                Assert.Empty(observer1.Events);
            }
            finally
            {
                RemoveCertificate(cert1);
                RemoveCertificate(cert2);
            }
        }

        [Fact]
        public async Task ObserverSendsCorrectEvents_mTLS()
        {
            static void RemoveCertificate(X509Certificate2? certificate)
            {
                if (certificate is null)
                {
                    return;
                }

                using X509Store x509Store = new(StoreName.My, StoreLocation.CurrentUser);
                x509Store.Open(OpenFlags.ReadWrite);
                x509Store.Remove(certificate);
                x509Store.Close();
            }

            X509Certificate2? cert1 = null;
            X509Certificate2? cert2 = null;
            try
            {
                var clientId = Guid.NewGuid();
                var tenantId = Guid.NewGuid();
                var instance = "https://login.microsoftonline.com/";
                var authority = instance + tenantId;

                string certName = $"CN=TestCert-{Guid.NewGuid():N}";
                cert1 = CreateAndInstallCertificate(certName);

                // Verify certificate is properly installed in store with timeout
                await VerifyCertificateInStoreAsync(cert1, TimeSpan.FromSeconds(5));
                var description = new CredentialDescription
                {
                    SourceType = CredentialSource.StoreWithDistinguishedName,
                    CertificateDistinguishedName = cert1.SubjectName.Name,
                    CertificateStorePath = "CurrentUser/My",
                };

                var taf = new CustomTAF();
                taf.Services.AddDownstreamApi("mtls", opts => { opts.BaseUrl = "https://test.example"; });
                taf.Services.Configure<MicrosoftIdentityApplicationOptions>(options =>
                {
                    options.Instance = instance;
                    options.ClientId = clientId.ToString();
                    options.TenantId = tenantId.ToString();
                    options.ClientCredentials = [description];
                });
                taf.Services.AddMockClientFactory(description);

                // Add two observers so that we can check if multiple observers works as intended.
                TestCertificatesObserver observer1 = new TestCertificatesObserver();
                taf.Services.AddSingleton<ICertificatesObserver>(observer1);
                TestCertificatesObserver observer2 = new TestCertificatesObserver();
                taf.Services.AddSingleton<ICertificatesObserver>(observer2);

                var provider = taf.Build();

                var mockHttpFactory = provider.GetRequiredService<MockHttpClientFactory>();

                // Configure successful STS responses
                mockHttpFactory.ConfigureSuccessfulTokenResponse(authority);
                mockHttpFactory.ValidCertificates.Add(cert1);

                IDownstreamApi downstreamApi = provider.GetRequiredService<IDownstreamApi>();

                DownstreamApiOptions options = new DownstreamApiOptions()
                {
                    BaseUrl = authority,
                    ProtocolScheme = "mTLS",
                    RelativePath = "/oauth2/v2.0/token"
                };

                string apiUrl = authority + options.RelativePath;

                HttpResponseMessage result = await downstreamApi.CallApiAsync(options);

                // Assert
                Assert.NotNull(result);
                Assert.Equal(HttpStatusCode.OK, result.StatusCode);

                // Verify both observers got all events.
                Assert.Equal(observer1.Events.Count, observer2.Events.Count);

                // First event was selection.
                observer1.Events.TryDequeue(out var eventArg);
                Assert.NotNull(eventArg);
                Assert.Equal(CerticateObserverAction.Selected, eventArg.Action);
                Assert.Equal(cert1, eventArg.Certificate);
                Assert.Equal(description, eventArg.CredentialDescription);
                Assert.NotNull(eventArg.CredentialSourceLoaderParameters);
                Assert.Equal(ProtocolNames.Mtls, eventArg.CredentialSourceLoaderParameters.Protocol);
                Assert.Equal(apiUrl, eventArg.CredentialSourceLoaderParameters.ApiUrl);

                // Second event was successful usage.
                observer1.Events.TryDequeue(out eventArg);
                Assert.NotNull(eventArg);
                Assert.Equal(CerticateObserverAction.SuccessfullyUsed, eventArg.Action);
                Assert.Equal(cert1, eventArg.Certificate);
                Assert.Equal(description, eventArg.CredentialDescription);
                Assert.NotNull(eventArg.CredentialSourceLoaderParameters);
                Assert.Equal(ProtocolNames.Mtls, eventArg.CredentialSourceLoaderParameters.Protocol);
                Assert.Equal(apiUrl, eventArg.CredentialSourceLoaderParameters.ApiUrl);

                // No further events
                Assert.Empty(observer1.Events);

                // Rerun, should only get success event.
                result = await downstreamApi.CallApiAsync(options);

                // We get selected events each time we use mTLS for now.
                observer1.Events.TryDequeue(out eventArg);
                Assert.NotNull(eventArg);
                Assert.Equal(CerticateObserverAction.Selected, eventArg.Action);

                observer1.Events.TryDequeue(out eventArg);
                Assert.NotNull(eventArg);
                Assert.Equal(CerticateObserverAction.SuccessfullyUsed, eventArg.Action);
                Assert.Empty(observer1.Events);

                // Change out the cert, so that if it reloads there will be a new one
                RemoveCertificate(cert1);
                cert2 = CreateAndInstallCertificate(certName);

                // Verify certificate is properly installed in store with timeout
                await VerifyCertificateInStoreAsync(cert2, TimeSpan.FromSeconds(5));

                // Rerun but it fails this time
                mockHttpFactory.ValidCertificates.Clear();
                mockHttpFactory.ValidCertificates.Add(cert2);
                result = await downstreamApi.CallApiAsync(options);

                // We get selected events each time we use mTLS for now.
                observer1.Events.TryDequeue(out eventArg);
                Assert.NotNull(eventArg);
                Assert.Equal(CerticateObserverAction.Selected, eventArg.Action);

                // First it deselects the old cert.
                observer1.Events.TryDequeue(out eventArg);
                Assert.NotNull(eventArg);
                Assert.Equal(CerticateObserverAction.Deselected, eventArg.Action);
                Assert.Equal(cert1, eventArg.Certificate);
                Assert.NotNull(eventArg.CredentialSourceLoaderParameters);
                Assert.Equal(ProtocolNames.Mtls, eventArg.CredentialSourceLoaderParameters.Protocol);
                Assert.Equal(apiUrl, eventArg.CredentialSourceLoaderParameters.ApiUrl);

                // Then, it uses a new cert successfully.
                observer1.Events.TryDequeue(out eventArg);
                Assert.NotNull(eventArg);
                Assert.Equal(CerticateObserverAction.Selected, eventArg.Action);
                Assert.Equal(cert2, eventArg.Certificate);
                observer1.Events.TryDequeue(out eventArg);
                Assert.NotNull(eventArg);
                Assert.Equal(CerticateObserverAction.SuccessfullyUsed, eventArg.Action);
                Assert.Equal(cert2, eventArg.Certificate);

                // No events left.
                Assert.Empty(observer1.Events);
            }
            finally
            {
                RemoveCertificate(cert1);
                RemoveCertificate(cert2);
            }
        }

        internal X509Certificate2 CreateAndInstallCertificate(string certName)
        {
            // Create the self signed certificate
#if ECDsa
            var ecdsa = ECDsa.Create(); // generate asymmetric key pair
            var req = new CertificateRequest($"CN={certName}", ecdsa, HashAlgorithmName.SHA256);
#else
            using RSA rsa = RSA.Create(); // generate asymmetric key pair
            var req = new CertificateRequest($"CN={certName}", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
#endif

            // Backdate NotBefore to avoid time-boundary races with FindByTimeValid.
            var cert = req.CreateSelfSigned(DateTimeOffset.Now.AddMinutes(-5), DateTimeOffset.Now.AddDays(1));

            byte[] bytes = cert.Export(X509ContentType.Pfx, (string?)null);
#pragma warning disable SYSLIB0057 // Type or member is obsolete
            X509Certificate2 certWithPrivateKey = new(bytes);
#pragma warning restore SYSLIB0057 // Type or member is obsolete

            using X509Store x509Store = new(StoreName.My, StoreLocation.CurrentUser);
            x509Store.Open(OpenFlags.ReadWrite);
            x509Store.Add(certWithPrivateKey);
            x509Store.Close();

            // X509Store.Add is synchronous — verify the cert is immediately findable
            // using the same DN-based lookup that production code uses.
            VerifyCertificateInStore(certWithPrivateKey);

            return certWithPrivateKey;
        }

        /// <summary>
        /// Verifies that a certificate is installed in the store using the same
        /// distinguished-name lookup that the production credential loader uses.
        /// X509Store.Add is synchronous, so no polling is needed.
        /// </summary>
        private static void VerifyCertificateInStore(X509Certificate2 certificate)
        {
            using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadOnly);
            var found = store.Certificates
                .Find(X509FindType.FindBySubjectDistinguishedName, certificate.SubjectName.Name, false);
            if (found.Count == 0)
            {
                throw new InvalidOperationException(
                    $"Test setup failure: certificate '{certificate.SubjectName.Name}' (thumbprint {certificate.Thumbprint}) " +
                    $"was not found in CurrentUser/My immediately after Add. The store may be in an unexpected state.");
            }
        }

        private class TestCertificatesObserver : ICertificatesObserver
        {
            public Queue<CertificateChangeEventArg> Events { get; } = new();

            public void OnClientCertificateChanged(CertificateChangeEventArg e) => Events.Enqueue(e);
        }

        /// <summary>
        /// Mock HTTP client factory for simulating STS backend interactions.
        /// </summary>
        internal class MockHttpClientFactory : IHttpClientFactory, IMsalMtlsHttpClientFactory
        {
            private readonly MockHttpMessageHandler handler;

            public MockHttpClientFactory(CredentialDescription credential)
            {
                this.handler = new(credential);
            }

            public List<X509Certificate2> ValidCertificates => this.handler.ValidCertificates;

            /// <inheritdoc/>
            public HttpClient CreateClient(string name) => new(this.handler);
            public HttpClient GetHttpClient(X509Certificate2 x509Certificate2) => new(this.handler);
            public HttpClient GetHttpClient() => new(this.handler);

            public void ConfigureSuccessfulTokenResponse(string authority)
            {
                this.handler.ConfigureSuccessfulTokenResponse(authority);
                this.handler.ConfigureSuccessfulOpenIdConfiguration(authority);
            }

            public void ConfigureFailedTokenResponse(string authority, string errorCode, HttpStatusCode statusCode = HttpStatusCode.BadRequest)
            {
                this.handler.ConfigureFailedTokenResponse(authority, errorCode, statusCode);
                this.handler.ConfigureSuccessfulOpenIdConfiguration(authority);
            }
        }

        /// <summary>
        /// Mock HTTP message handler that simulates STS responses.
        /// </summary>
        private class MockHttpMessageHandler : HttpMessageHandler
        {
            private readonly Dictionary<string, Func<HttpResponseMessage>> responseMap = [];
            private readonly CredentialDescription description;

            public MockHttpMessageHandler(CredentialDescription description)
            {
                this.description = description;
            }

            public List<X509Certificate2> ValidCertificates { get; } = [];

            public void ConfigureSuccessfulTokenResponse(string authority)
            {
                var tokenEndpoint = $"{authority.TrimEnd('/')}/oauth2/v2.0/token";

                var token = GenerateMockAccessToken();
                var response = new
                {
                    access_token = token,
                    token_type = "Bearer",
                    expires_in = 3600,
                    scope = "https://graph.microsoft.com/.default",
                };

                this.ConfigureResponse(
                    tokenEndpoint,
                    () => new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(
                        System.Text.Json.JsonSerializer.Serialize(response),
                        Encoding.UTF8,
                        "application/json"),
                    });
            }

            public void ConfigureFailedTokenResponse(string authority, string errorCode, HttpStatusCode statusCode = HttpStatusCode.BadRequest)
            {
                var tokenEndpoint = $"{authority.TrimEnd('/')}/oauth2/v2.0/token";

                var errorResponse = new
                {
                    error = errorCode,
                    error_description = $"Mock error: {errorCode}",
                    error_codes = new[] { 50000 },
                    timestamp = DateTime.UtcNow,
                };

                this.ConfigureResponse(
                    tokenEndpoint,
                    () => new HttpResponseMessage(statusCode)
                    {
                        Content = new StringContent(
                            System.Text.Json.JsonSerializer.Serialize(errorResponse),
                            Encoding.UTF8,
                            "application/json"),
                    });
            }

            public void ConfigureSuccessfulOpenIdConfiguration(string authority)
            {
                var configEndpoint = $"{authority.TrimEnd('/')}/.well-known/openid_configuration";

                var config = new
                {
                    token_endpoint = $"{authority}/oauth2/v2.0/token",
                    issuer = authority,
                };

                this.ConfigureResponse(
                    configEndpoint,
                    () => new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(
                            System.Text.Json.JsonSerializer.Serialize(config),
                            Encoding.UTF8,
                            "application/json"),
                    });
            }

            protected void ConfigureResponse(string url, Func<HttpResponseMessage> response)
            {
                this.responseMap[url] = response;
            }

            /// <inheritdoc/>
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var uri = request.RequestUri?.ToString() ?? string.Empty;

                foreach (var kvp in this.responseMap)
                {
                    if (uri.StartsWith(kvp.Key, StringComparison.OrdinalIgnoreCase))
                    {
                        if (this.description.Certificate == null ||
                            !this.ValidCertificates.Any(cert => cert.Thumbprint.Equals(this.description.Certificate?.Thumbprint, StringComparison.OrdinalIgnoreCase)))
                        {
                            var errorResponse = new
                            {
                                error = "invalid_client",
                                error_description = $"AADSTS700027: Invalid certificate: {this.description.CachedValue}",
                                error_codes = new[] { 700027 },
                                timestamp = DateTime.UtcNow,
                            };

                            var message = new HttpResponseMessage(HttpStatusCode.Unauthorized)
                            {
                                Content = new StringContent(
                                    System.Text.Json.JsonSerializer.Serialize(errorResponse),
                                    Encoding.UTF8,
                                    "application/json"),
                            };

                            return Task.FromResult(message);
                        }

                        return Task.FromResult(kvp.Value());
                    }
                }

                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
            }

            private static string GenerateMockAccessToken()
            {
                var header = Convert.ToBase64String(Encoding.UTF8.GetBytes("{\"typ\":\"JWT\",\"alg\":\"RS256\"}"));
                var payload = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{{\"aud\":\"https://graph.microsoft.com/\",\"cert_thumbprint\":\"blahblah\",\"exp\":{DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds()},\"scp\":\"https://graph.microsoft.com/.default\"}}"));
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
    file static class TestServiceExtensions
#pragma warning restore SA1402 // File may only contain a single type
    {
        public static IServiceCollection AddMockClientFactory(this IServiceCollection services, CredentialDescription description)
        {
            //services.Remove(services.First(d => d.ServiceType == typeof(IMsalHttpClientFactory)));

            return services
                .AddSingleton(new CertificatesObserverTests.MockHttpClientFactory(description))
                .AddSingleton<IHttpClientFactory>(s => s.GetRequiredService<CertificatesObserverTests.MockHttpClientFactory>())
                .AddSingleton<IMsalHttpClientFactory>(s => s.GetRequiredService<CertificatesObserverTests.MockHttpClientFactory>());
        }
    }
}
