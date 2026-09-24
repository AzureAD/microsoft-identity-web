// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensibility;
using Microsoft.Identity.Web.Test.Common;
using Microsoft.Identity.Web.Test.Common.Mocks;
using Microsoft.Identity.Web.TestOnly;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    [Collection(nameof(TokenAcquirerFactorySingletonProtection))]
    public class FicAssertionCacheTests
    {
        [Theory]
        [InlineData(false, false, false)]
        [InlineData(false, true, false)]
        [InlineData(true, false, false)]
        [InlineData(true, true, false)]
        [InlineData(false, false, true)]
        [InlineData(false, true, true)]
        [InlineData(true, false, true)]
        [InlineData(true, true, true)]
        public async Task AssertionRequests_UseMsalCacheAndCurrentEnricher(
            bool managedIdentity, bool metricsEnabled, bool shortLivedAssertion)
        {
            // Arrange
            TokenAcquirerFactoryTesting.ResetTokenAcquirerFactoryInTest();
            var factory = TokenAcquirerFactory.GetDefaultInstance();
            string innerClientId = Guid.NewGuid().ToString();
            string outerClientId = Guid.NewGuid().ToString();
            string metricTag = Guid.NewGuid().ToString();
            var callbacks = new List<(string Label, string Token, TokenSource Source)>();
            var measurements = new List<(string Label, TokenSource Source)>();
            using var listener = new MeterListener();
            if (metricsEnabled)
            {
                listener.InstrumentPublished = (instrument, meterListener) =>
                {
                    if (instrument.Meter.Name.StartsWith("MicrosoftIdentityClient", StringComparison.Ordinal)
                        && instrument.Name == "MsalSuccess")
                    {
                        meterListener.EnableMeasurementEvents(instrument);
                    }
                };
                listener.SetMeasurementEventCallback<long>((_, _, tags, _) =>
                {
                    string? label = null;
                    TokenSource source = default;
                    foreach (var tag in tags)
                    {
                        if (tag.Key == metricTag)
                        {
                            label = tag.Value as string;
                        }
                        else if (tag.Key == "TokenSource")
                        {
                            source = (TokenSource)Convert.ToInt32(tag.Value, CultureInfo.InvariantCulture);
                        }
                    }
                    if (label is not null)
                    {
                        measurements.Add((label, source));
                    }
                });
                listener.Start();
            }

            using var http = new MockHttpClientFactory();
            using var miHttp = new MockHttpClientFactory();
            int expiresIn = shortLivedAssertion ? 1 : 3599;
            MockHttpMessageHandler innerHandler = managedIdentity
                ? miHttp.AddMockHandler(MockHttpCreator.CreateMsiTokenHandler("inner-assertion", "api://AzureADTokenExchange", expiresIn))
                : http.AddMockHandler(MockHttpCreator.CreateClientCredentialTokenHandler("inner-assertion", expiresIn: expiresIn));
            string expectedAssertion = shortLivedAssertion ? "refreshed-assertion" : "inner-assertion";
            TokenSource firstInnerSource = shortLivedAssertion ? TokenSource.IdentityProvider : TokenSource.Cache;
            if (shortLivedAssertion)
            {
                if (managedIdentity)
                {
                    miHttp.AddMockHandler(MockHttpCreator.CreateMsiTokenHandler(expectedAssertion, "api://AzureADTokenExchange"));
                }
                else
                {
                    http.AddMockHandler(MockHttpCreator.CreateClientCredentialTokenHandler(expectedAssertion));
                }
            }
            var firstOuterHandler = http.AddMockHandler(MockHttpCreator.CreateClientCredentialTokenHandler("outer-first"));
            var secondOuterHandler = http.AddMockHandler(MockHttpCreator.CreateClientCredentialTokenHandler("outer-second"));
            var untaggedOuterHandler = http.AddMockHandler(MockHttpCreator.CreateClientCredentialTokenHandler("outer-untagged"));
            factory.Services.AddSingleton<IHttpClientFactory>(http);
            factory.Services.AddOidcFic();
            factory.Services.Configure<MicrosoftIdentityApplicationOptions>("FicCacheInner", options =>
            {
                options.Instance = "https://login.microsoftonline.com/";
                options.TenantId = "11111111-1111-1111-1111-111111111111";
                options.ClientId = innerClientId;
                options.ClientCredentials = new[]
                {
                    new CredentialDescription { SourceType = CredentialSource.ClientSecret, ClientSecret = TestConstants.ClientSecret },
                };
            });
            factory.Services.Configure<MicrosoftIdentityApplicationOptions>(options =>
            {
                options.Instance = "https://login.microsoftonline.com/";
                options.TenantId = "11111111-1111-1111-1111-111111111111";
                options.ClientId = outerClientId;
                options.ClientCredentials = new[]
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
                                ["ConfigurationSection"] = "FicCacheInner",
                            },
                        },
                };
            });

            TokenAcquisitionOptions CreateOptions(string label, bool forceRefresh = false)
            {
                Action<ExecutionResult, IList<KeyValuePair<string, object>>> enricher = (result, tags) =>
                {
                    callbacks.Add((label, result.Result.AccessToken, result.Result.AuthenticationResultMetadata.TokenSource));
                    tags.Add(new KeyValuePair<string, object>(metricTag, label));
                };
                return new TokenAcquisitionOptions
                {
                    ForceRefresh = forceRefresh,
                    ExtraParameters = new Dictionary<string, object> { [Constants.OtelTagsEnricherKey] = enricher },
                };
            }

            IMsalHttpClientFactory? previousFactory = ManagedIdentityClientAssertionTestHook.HttpClientFactoryForTests;
            ManagedIdentityClientAssertionTestHook.HttpClientFactoryForTests = miHttp;
            try
            {
                var tokenAcquisition = factory.Build().GetRequiredService<ITokenAcquisition>();

                // Act
                var first = await tokenAcquisition.GetAuthenticationResultForAppAsync(
                    TestConstants.s_scopeForApp, tokenAcquisitionOptions: CreateOptions("A"));
                var cached = await tokenAcquisition.GetAuthenticationResultForAppAsync(
                    TestConstants.s_scopeForApp, tokenAcquisitionOptions: CreateOptions("B"));
                var refreshed = await tokenAcquisition.GetAuthenticationResultForAppAsync(
                    TestConstants.s_scopeForApp, tokenAcquisitionOptions: CreateOptions("B", forceRefresh: true));
                var untagged = await tokenAcquisition.GetAuthenticationResultForAppAsync(
                    TestConstants.s_scopeForApp, tokenAcquisitionOptions: new TokenAcquisitionOptions { ForceRefresh = true });

                // Assert
                Assert.Equal("outer-first", first.AccessToken);
                Assert.Equal(first.AccessToken, cached.AccessToken);
                Assert.Equal(TokenSource.Cache, cached.AuthenticationResultMetadata.TokenSource);
                Assert.Equal("outer-second", refreshed.AccessToken);
                Assert.Equal("outer-untagged", untagged.AccessToken);
                Assert.NotNull(innerHandler.ActualRequestMessage);
                Assert.Equal(expectedAssertion, firstOuterHandler.ActualRequestPostData["client_assertion"]);
                Assert.Equal(expectedAssertion, secondOuterHandler.ActualRequestPostData["client_assertion"]);
                Assert.Equal(expectedAssertion, untaggedOuterHandler.ActualRequestPostData["client_assertion"]);
                Assert.Equal(new[]
                {
                    ("A", "inner-assertion", TokenSource.IdentityProvider),
                    ("A", expectedAssertion, firstInnerSource),
                    ("A", "outer-first", TokenSource.IdentityProvider),
                    ("B", "outer-first", TokenSource.Cache),
                    ("B", expectedAssertion, TokenSource.Cache),
                    ("B", "outer-second", TokenSource.IdentityProvider),
                }, callbacks);
                if (metricsEnabled)
                {
                    Assert.Equal(new[]
                    {
                        ("A", TokenSource.IdentityProvider),
                        ("A", firstInnerSource),
                        ("A", TokenSource.IdentityProvider),
                        ("B", TokenSource.Cache),
                        ("B", TokenSource.Cache),
                        ("B", TokenSource.IdentityProvider),
                    }, measurements);
                }
                else
                {
                    Assert.Empty(measurements);
                }
            }
            finally
            {
                ManagedIdentityClientAssertionTestHook.HttpClientFactoryForTests = previousFactory;
                TokenAcquirerFactoryTesting.ResetTokenAcquirerFactoryInTest();
            }
        }

        [Fact]
        public async Task ManagedIdentityAssertion_ConcurrentCacheHits_KeepEachRequestsEnricher()
        {
            // Arrange
            using var http = new MockHttpClientFactory();
            var handler = http.AddMockHandler(MockHttpCreator.CreateMsiTokenHandler("cached-assertion"));
            var provider = new ManagedIdentityClientAssertion(
                Guid.NewGuid().ToString(), tokenExchangeUrl: null, logger: null, testHttpClientFactory: http);
            await provider.GetSignedAssertionAsync(null);
            var callbacks = new ConcurrentQueue<(int Request, string Token, TokenSource Source)>();
            using var start = new SemaphoreSlim(0, 8);

            // Act
            Task<string>[] requests = Enumerable.Range(0, 8).Select(request => Task.Run(async () =>
            {
                await start.WaitAsync();
                return await provider.GetSignedAssertionAsync(new AssertionRequestOptions
                {
                    OtelTagsEnricher = (result, _) => callbacks.Enqueue(
                        (request, result.Result.AccessToken, result.Result.AuthenticationResultMetadata.TokenSource)),
                });
            })).ToArray();
            start.Release(8);
            string[] assertions = await Task.WhenAll(requests);

            // Assert
            Assert.NotNull(handler.ActualRequestMessage);
            Assert.All(assertions, assertion => Assert.Equal("cached-assertion", assertion));
            Assert.Equal(Enumerable.Range(0, 8), callbacks.Select(callback => callback.Request).OrderBy(request => request));
            Assert.All(callbacks, callback =>
            {
                Assert.Equal("cached-assertion", callback.Token);
                Assert.Equal(TokenSource.Cache, callback.Source);
            });
            Assert.NotNull(provider.Expiry);
        }

        [Fact]
        public async Task ManagedIdentityAssertion_ClaimsFailure_DoesNotReturnPreviousAssertion()
        {
            // Arrange
            using var http = new MockHttpClientFactory();
            http.AddMockHandler(MockHttpCreator.CreateMsiTokenHandler("cached-assertion"));
            var failure = new MsalServiceException("invalid_client", "Mock assertion acquisition failed.");
            http.AddMockHandler(new MockHttpMessageHandler { ExceptionToThrow = failure });
            var provider = new ManagedIdentityClientAssertion(
                Guid.NewGuid().ToString(), tokenExchangeUrl: null, logger: null, testHttpClientFactory: http);
            await provider.GetSignedAssertionAsync(null);
            DateTimeOffset? expiry = provider.Expiry;
            ExecutionResult? captured = null;

            // Act
            MsalServiceException exception = await Assert.ThrowsAsync<MsalServiceException>(() => provider.GetSignedAssertionAsync(new AssertionRequestOptions
            {
                Claims = "{\"access_token\":{}}",
                OtelTagsEnricher = (result, _) => captured = result,
            }));

            // Assert
            Assert.NotNull(captured);
            Assert.False(captured!.Successful);
            Assert.Same(failure, exception);
            Assert.Equal(expiry, provider.Expiry);
        }
    }
}
