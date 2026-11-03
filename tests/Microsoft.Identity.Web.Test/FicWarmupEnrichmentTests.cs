// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensibility;
using Microsoft.Identity.Web.Extensibility;
using Microsoft.Identity.Web.Test.Common;
using Microsoft.Identity.Web.Test.Common.Mocks;
using Microsoft.Identity.Web.TestOnly;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    [Collection(nameof(TokenAcquirerFactorySingletonProtection))]
    public class FicWarmupEnrichmentTests
    {
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        public async Task AppAcquisition_WithoutRequestOptions_EnrichesWarmupAndBothLegs(int providerKind)
        {
            // Arrange
            using var harness = new WarmupHarness(providerKind);
            harness.Factory.Services.Configure<TokenAcquisitionExtensionOptions>(
                options => options.DefaultAppTokenOtelTagsEnricher = harness.Enricher("default"));
            var acquisition = harness.Factory.Build().GetRequiredService<ITokenAcquisition>();

            // Act
            var result = await acquisition.GetAuthenticationResultForAppAsync(TestConstants.s_scopeForApp);

            // Assert
            Assert.Equal("outer", result.AccessToken);
            var expected = new[] { ("default", true), ("default", true), ("default", true) };
            Assert.Equal(expected, harness.Callbacks);
            Assert.Equal(expected, harness.Measurements);
        }

        [Theory]
        [InlineData(0, false)]
        [InlineData(0, true)]
        [InlineData(1, false)]
        [InlineData(1, true)]
        [InlineData(2, false)]
        [InlineData(2, true)]
        public async Task WarmupFailure_IsEnrichedAndPreservesFallback(int providerKind, bool fallback)
        {
            // Arrange
            using var harness = new WarmupHarness(providerKind, failWarmup: true, fallback);
            harness.Factory.Services.Configure<TokenAcquisitionExtensionOptions>(
                options => options.DefaultAppTokenOtelTagsEnricher = harness.Enricher("default"));
            var acquisition = harness.Factory.Build().GetRequiredService<ITokenAcquisition>();
            var options = new TokenAcquisitionOptions
            {
                ExtraParameters = new Dictionary<string, object>
                {
                    [Constants.OtelTagsEnricherKey] = harness.Enricher("request"),
                },
            };

            // Act
            if (fallback)
            {
                var result = await acquisition.GetAuthenticationResultForAppAsync(
                    TestConstants.s_scopeForApp, tokenAcquisitionOptions: options);
                Assert.Equal("outer", result.AccessToken);
                Assert.False(harness.OuterHandler!.ActualRequestPostData.ContainsKey("client_assertion"));
            }
            else
            {
                await Assert.ThrowsAsync<ArgumentException>(() => acquisition.GetAuthenticationResultForAppAsync(
                    TestConstants.s_scopeForApp, tokenAcquisitionOptions: options));
            }

            // Assert
            var expected = new List<(string Label, bool Success)> { ("request", false) };
            if (fallback)
            {
                expected.Add(("request", true));
            }
            Assert.Equal(expected, harness.Callbacks);
            Assert.Equal(expected, harness.Measurements);
        }

        [Theory]
        [InlineData(0, false)]
        [InlineData(0, true)]
        [InlineData(1, false)]
        [InlineData(1, true)]
        [InlineData(2, false)]
        [InlineData(2, true)]
        public async Task ContextlessConstruction_UsesDefaultWithoutRetainingRequestEnricher(
            int providerKind, bool configureDefault)
        {
            // Arrange
            using var harness = new WarmupHarness(providerKind);
            if (configureDefault)
            {
                harness.Factory.Services.Configure<TokenAcquisitionExtensionOptions>(
                    options => options.DefaultAppTokenOtelTagsEnricher = harness.Enricher("default"));
            }
            var services = harness.Factory.Build();
            var applicationProvider = services.GetRequiredService<IConfidentialClientApplicationProvider>();
            var acquisition = services.GetRequiredService<ITokenAcquisition>();

            // Act
            var first = await applicationProvider.GetConfidentialClientApplicationAsync();
            var second = await applicationProvider.GetConfidentialClientApplicationAsync();
            await acquisition.GetAuthenticationResultForAppAsync(
                TestConstants.s_scopeForApp,
                tokenAcquisitionOptions: new TokenAcquisitionOptions
                {
                    ExtraParameters = new Dictionary<string, object>
                    {
                        [Constants.OtelTagsEnricherKey] = harness.Enricher("request"),
                    },
                });
            await acquisition.GetAuthenticationResultForAppAsync(TestConstants.s_scopeForApp);

            // Assert
            Assert.Same(first, second);
            var expected = new List<(string Label, bool Success)>();
            if (configureDefault)
            {
                expected.Add(("default", true));
            }
            expected.Add(("request", true));
            expected.Add(("request", true));
            if (configureDefault)
            {
                expected.Add(("default", true));
            }
            Assert.Equal(expected, harness.Callbacks);
            Assert.Equal(expected, harness.Measurements);
        }

        [Theory]
        [InlineData(0, false)]
        [InlineData(0, true)]
        [InlineData(1, false)]
        [InlineData(1, true)]
        [InlineData(2, false)]
        [InlineData(2, true)]
        public async Task AppHook_PreservesPrecedenceAndRunsOnlyOncePerAcquisition(
            int providerKind, bool requestEnricher)
        {
            // Arrange
            using var harness = new WarmupHarness(providerKind);
            int hookCalls = 0;
            harness.Factory.Services.Configure<TokenAcquisitionExtensionOptions>(options =>
            {
                options.DefaultAppTokenOtelTagsEnricher = harness.Enricher("default");
                options.OnBeforeTokenAcquisitionForApp += (builder, _) =>
                {
                    hookCalls++;
                    builder.WithOtelTagsEnricher(harness.Enricher("hook"));
                };
            });
            var acquisition = harness.Factory.Build().GetRequiredService<ITokenAcquisition>();
            var tokenOptions = requestEnricher
                ? new TokenAcquisitionOptions
                {
                    ExtraParameters = new Dictionary<string, object>
                    {
                        [Constants.OtelTagsEnricherKey] = harness.Enricher("request"),
                    },
                }
                : null;

            // Act
            await acquisition.GetAuthenticationResultForAppAsync(
                TestConstants.s_scopeForApp, tokenAcquisitionOptions: tokenOptions);

            // Assert
            Assert.Equal(providerKind == 0 ? 3 : 1, hookCalls);
            var expected = new[]
            {
                (requestEnricher ? "request" : "default", true),
                (requestEnricher ? "request" : "hook", true),
                (requestEnricher ? "request" : "hook", true),
            };
            Assert.Equal(expected, harness.Callbacks);
            Assert.Equal(expected, harness.Measurements);
        }

        private sealed class WarmupHarness : IDisposable
        {
            private readonly MockHttpClientFactory _http = new();
            private readonly MockHttpClientFactory _miHttp = new();
            private readonly MeterListener _listener = new();
            private readonly string _metricKey = Guid.NewGuid().ToString();
            private readonly IMsalHttpClientFactory? _previousFactory;

            public WarmupHarness(int providerKind, bool failWarmup = false, bool fallback = false)
            {
                TokenAcquirerFactoryTesting.ResetTokenAcquirerFactoryInTest();
                Factory = TokenAcquirerFactory.GetDefaultInstance();
                _previousFactory = ManagedIdentityClientAssertionTestHook.HttpClientFactoryForTests;
                ManagedIdentityClientAssertionTestHook.HttpClientFactoryForTests = _miHttp;
                string innerClientId = Guid.NewGuid().ToString();
                string outerClientId = Guid.NewGuid().ToString();
                bool managedIdentity = providerKind != 0;
                var innerHandler = failWarmup
                    ? new MockHttpMessageHandler
                    {
                        ExceptionToThrow = new MsalServiceException("invalid_client", "Mock credential unavailable."),
                    }
                    : managedIdentity
                        ? MockHttpCreator.CreateMsiTokenHandler("assertion", "api://AzureADTokenExchange")
                        : MockHttpCreator.CreateClientCredentialTokenHandler("assertion");
                (managedIdentity ? _miHttp : _http).AddMockHandler(innerHandler);
                if (!failWarmup || fallback)
                {
                    OuterHandler = _http.AddMockHandler(MockHttpCreator.CreateClientCredentialTokenHandler("outer"));
                }
                Factory.Services.AddSingleton<IHttpClientFactory>(_http);
                Factory.Services.AddOidcFic();
                if (providerKind == 2)
                {
                    Factory.Services.AddMicrosoftIdentityWebKeyAttestation();
                }
                Factory.Services.Configure<MicrosoftIdentityApplicationOptions>("WarmupInner", options =>
                {
                    options.Instance = "https://login.microsoftonline.com/";
                    options.TenantId = "11111111-1111-1111-1111-111111111111";
                    options.ClientId = innerClientId;
                    options.ClientCredentials = new[]
                    {
                        new CredentialDescription { SourceType = CredentialSource.ClientSecret, ClientSecret = TestConstants.ClientSecret },
                    };
                });
                Factory.Services.Configure<MicrosoftIdentityApplicationOptions>(options =>
                {
                    options.Instance = "https://login.microsoftonline.com/";
                    options.TenantId = "11111111-1111-1111-1111-111111111111";
                    options.ClientId = outerClientId;
                    var credentials = new List<CredentialDescription>
                    {
                        managedIdentity
                            ? new CredentialDescription
                            {
                                SourceType = CredentialSource.SignedAssertionFromManagedIdentity,
                                ManagedIdentityClientId = innerClientId,
                            }
                            : new CredentialDescription
                            {
                                SourceType = CredentialSource.CustomSignedAssertion,
                                CustomSignedAssertionProviderName = "OidcIdpSignedAssertion",
                                CustomSignedAssertionProviderData = new Dictionary<string, object>
                                {
                                    ["ConfigurationSection"] = "WarmupInner",
                                },
                            },
                    };
                    if (fallback)
                    {
                        credentials.Add(new CredentialDescription
                        {
                            SourceType = CredentialSource.ClientSecret,
                            ClientSecret = TestConstants.ClientSecret,
                        });
                    }
                    options.ClientCredentials = credentials;
                });
                _listener.InstrumentPublished = (instrument, listener) =>
                {
                    if (instrument.Meter.Name.StartsWith("MicrosoftIdentityClient", StringComparison.Ordinal)
                        && (instrument.Name == "MsalSuccess" || instrument.Name == "MsalFailure"))
                    {
                        listener.EnableMeasurementEvents(instrument);
                    }
                };
                _listener.SetMeasurementEventCallback<long>((instrument, _, tags, _) =>
                {
                    foreach (var tag in tags)
                    {
                        if (tag.Key == _metricKey && tag.Value is string label)
                        {
                            Measurements.Add((label, instrument.Name == "MsalSuccess"));
                        }
                    }
                });
                _listener.Start();
            }

            public TokenAcquirerFactory Factory { get; }
            public MockHttpMessageHandler? OuterHandler { get; }
            public List<(string Label, bool Success)> Callbacks { get; } = new();
            public List<(string Label, bool Success)> Measurements { get; } = new();

            public Action<ExecutionResult, IList<KeyValuePair<string, object>>> Enricher(string label) =>
                (result, tags) =>
                {
                    Callbacks.Add((label, result.Successful));
                    tags.Add(new KeyValuePair<string, object>(_metricKey, label));
                };

            public void Dispose()
            {
                _listener.Dispose();
                ManagedIdentityClientAssertionTestHook.HttpClientFactoryForTests = _previousFactory;
                TokenAcquirerFactoryTesting.ResetTokenAcquirerFactoryInTest();
                _http.Dispose();
                _miHttp.Dispose();
            }
        }
    }
}
