// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.Test;
using Microsoft.Identity.Web.Test.Common;
using Microsoft.Identity.Web.Test.Common.Mocks;
using Microsoft.Identity.Web.TestOnly;
using Xunit;

namespace Microsoft.Identity.Web.Tests.Certificateless
{
    [Collection(nameof(TokenAcquirerFactorySingletonProtection))]
    public class FederatedIdentityCaeTests
    {
        private const string Scope = "https://graph.microsoft.com/.default";
        private const string CaeClaims = @"{""access_token"":{""xms_cc"":{""values"":[""claims1""]}}}";
        private const string UamiClientId = "04ca4d6a-c720-4ba1-aa06-f6634b73fe7a";

        [Fact]
        public async Task ManagedIdentityWithFic_WithClaims_BypassesCache()
        {
            TokenAcquirerFactoryTesting.ResetTokenAcquirerFactoryInTest();
            var factory = TokenAcquirerFactory.GetDefaultInstance();

            // Configure FIC via Managed Identity–signed assertion
            factory.Services.Configure<MicrosoftIdentityApplicationOptions>(opts =>
            {
                opts.Instance = "https://login.microsoftonline.com/";
                opts.TenantId = "11111111-1111-1111-1111-111111111111";
                opts.ClientId = "00000000-0000-0000-0000-000000000000";

                // Make this a CP1-capable app
                opts.ClientCapabilities = new[] { "cp1" };
                opts.ClientCredentials = new[]
                {
                    new CredentialDescription
                    {
                        SourceType = CredentialSource.SignedAssertionFromManagedIdentity,
                        ManagedIdentityClientId = UamiClientId
                    }
                };
            });

            // Mock IMDS (for the MI assertion)
            var mockMiHttp = new MockHttpClientFactory();
            mockMiHttp.AddMockHandler(MockHttpCreator.CreateMsiTokenHandler("mi-assertion-token"));

            var miTestFactory = new TestManagedIdentityHttpFactory(mockMiHttp);
            ManagedIdentityClientAssertionTestHook.HttpClientFactoryForTests = miTestFactory.Create();
            factory.Services.AddSingleton<IManagedIdentityTestHttpClientFactory>(_ => miTestFactory);

            // Mock AAD token responses for client credentials
            var mockMsalHttp = new MockHttpClientFactory();

            // 1st HTTP call to AAD -> token1
            var firstTokenHandler = mockMsalHttp.AddMockHandler(
                MockHttpCreator.CreateClientCredentialTokenHandler("token1"));

            // 2nd HTTP call to AAD -> token2
            var secondTokenHandler = mockMsalHttp.AddMockHandler(
                MockHttpCreator.CreateClientCredentialTokenHandler("token2"));

            factory.Services.AddSingleton<IMsalHttpClientFactory>(_ => mockMsalHttp);

            var acquirer = factory.Build().GetRequiredService<ITokenAcquisition>();

            // ---------- 1) First call – no custom claims, must hit IdP ----------
            var r1 = await acquirer.GetAuthenticationResultForAppAsync(
                Scope,
                tokenAcquisitionOptions: new TokenAcquisitionOptions());

            Assert.Equal("token1", r1.AccessToken);
            Assert.Equal(TokenSource.IdentityProvider, r1.AuthenticationResultMetadata.TokenSource);

            // First HTTP request already contains CP1 in xms_cc because of ClientCapabilities
            Assert.True(firstTokenHandler.ActualRequestPostData.TryGetValue("claims", out var firstClaimsJson));
            using (var doc = JsonDocument.Parse(firstClaimsJson))
            {
                var values = doc.RootElement
                                .GetProperty("access_token")
                                .GetProperty("xms_cc")
                                .GetProperty("values");

                bool hasCp1 = false;
                bool hasClaims1 = false;

                foreach (var v in values.EnumerateArray())
                {
                    var s = v.GetString();
                    if (s == "cp1") hasCp1 = true;
                    if (s == "claims1") hasClaims1 = true;
                }

                Assert.True(hasCp1);       // capability propagated
                Assert.False(hasClaims1);    // custom CAE claim NOT present yet
            }

            // ---------- 2) Second call – still no claims, should come from CACHE ----------
            var r2 = await acquirer.GetAuthenticationResultForAppAsync(
                Scope,
                tokenAcquisitionOptions: new TokenAcquisitionOptions());

            // Same token as r1 and came from cache (no extra HTTP call to AAD)
            Assert.Equal("token1", r2.AccessToken);
            Assert.Equal(TokenSource.Cache, r2.AuthenticationResultMetadata.TokenSource);

            // ---------- 3) Third call – WITH claims, must bypass cache and hit IdP ----------
            var r3 = await acquirer.GetAuthenticationResultForAppAsync(
                Scope,
                tokenAcquisitionOptions: new TokenAcquisitionOptions { Claims = CaeClaims });

            // New token & explicitly from IdentityProvider
            Assert.Equal("token2", r3.AccessToken);
            Assert.Equal(TokenSource.IdentityProvider, r3.AuthenticationResultMetadata.TokenSource);

            // And the actual HTTP POST for that second AAD call contained the merged claims (cp1 + mark1)
            Assert.True(secondTokenHandler.ActualRequestPostData.TryGetValue("claims", out var secondClaimsJson));

            using (var doc = JsonDocument.Parse(secondClaimsJson))
            {
                var values = doc.RootElement
                                .GetProperty("access_token")
                                .GetProperty("xms_cc")
                                .GetProperty("values");

                bool hasCp1 = false;
                bool hasClaims1 = false;

                foreach (var v in values.EnumerateArray())
                {
                    var s = v.GetString();
                    if (s == "cp1") hasCp1 = true;
                    if (s == "claims1") hasClaims1 = true;
                }

                Assert.True(hasCp1);    // capability kept
                Assert.True(hasClaims1);  // custom CAE claim merged in
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task Fic_CustomSignedAssertion_ClaimsAndCapabilities_AreSent_OnSecondRequest(bool withFmiPath)
        {
            using var httpFactoryForTest = new MockHttpClientFactory();
            // First request (credential exchange)
            var credentialRequestHttpHandler = httpFactoryForTest.AddMockHandler(
                MockHttpCreator.CreateClientCredentialTokenHandler("token-exchange-1"));
            // Second request (actual token acquisition)
            var tokenRequestHttpHandler = httpFactoryForTest.AddMockHandler(
                MockHttpCreator.CreateClientCredentialTokenHandler("final-access-token"));

            TokenAcquirerFactoryTesting.ResetTokenAcquirerFactoryInTest();
            var tokenAcquirerFactory = TokenAcquirerFactory.GetDefaultInstance();
            tokenAcquirerFactory.Services.AddOidcFic();
            tokenAcquirerFactory.Services.AddSingleton<IHttpClientFactory>(httpFactoryForTest);

            // Source app (provides assertion)
            tokenAcquirerFactory.Services.Configure<MicrosoftIdentityApplicationOptions>("AzureAd2", options =>
            {
                options.Instance = "https://login.microsoftonline.us/";
                options.TenantId = "t1";
                options.ClientId = "c1";
                options.ClientCredentials = new[]
                {
                    new CredentialDescription
                    {
                        SourceType = CredentialSource.ClientSecret,
                        ClientSecret = TestConstants.ClientSecret
                    }
                };
            });

            // Target app (uses custom signed assertion and carries cp1)
            var customAssertionProvidedData = new Dictionary<string, object>
            {
                ["ConfigurationSection"] = "AzureAd2"
            };
            if (withFmiPath)
            {
                customAssertionProvidedData["RequiresSignedAssertionFmiPath"] = true;
            }

            tokenAcquirerFactory.Services.Configure<MicrosoftIdentityApplicationOptions>(options =>
            {
                options.Instance = "https://login.microsoftonline.com/";
                options.TenantId = "t2";
                options.ClientId = "c2";
                options.ClientCapabilities = new[] { "cp1" };
                options.ClientCredentials = new[]
                {
                    new CredentialDescription
                    {
                        SourceType = CredentialSource.CustomSignedAssertion,
                        CustomSignedAssertionProviderName = "OidcIdpSignedAssertion",
                        CustomSignedAssertionProviderData = customAssertionProvidedData
                    }
                };
            });

            var serviceProvider = tokenAcquirerFactory.Build();
            var authorizationHeaderProvider = serviceProvider.GetRequiredService<IAuthorizationHeaderProvider>();

            // Second call will carry claims (with cp1 in xms_cc)
            var claimsPayload = "{\"access_token\":{\"xms_cc\":{\"values\":[\"cp1\"]}}}";

            var header = await authorizationHeaderProvider.CreateAuthorizationHeaderForAppAsync(
                TestConstants.s_scopeForApp,
                new AuthorizationHeaderProviderOptions
                {
                    AcquireTokenOptions = new AcquireTokenOptions
                    {
                        Claims = claimsPayload,
                        ExtraParameters = withFmiPath
                            ? new Dictionary<string, object>
                            {
                                [Constants.FmiPathForClientAssertion] = "myFmiPathForSignedAssertion"
                            }
                            : null
                    }
                });

            // Assert endpoints, scopes, client IDs
            // The source app (c1) is configured on US Gov (login.microsoftonline.us), so the FIC
            // token-exchange audience auto-resolves from MSAL's cloud metadata to the US Gov value
            // rather than the public-cloud default — the cross-cloud-metadata resolution under test.
            Assert.Equal("api://AzureADTokenExchangeUSGov/.default", credentialRequestHttpHandler.ActualRequestPostData["scope"]);
            Assert.Equal(TestConstants.s_scopeForApp, tokenRequestHttpHandler.ActualRequestPostData["scope"]);
            Assert.Equal("c1", credentialRequestHttpHandler.ActualRequestPostData["client_id"]);
            Assert.Equal("https://login.microsoftonline.us/t1/oauth2/v2.0/token",
                         credentialRequestHttpHandler.ActualRequestMessage?.RequestUri?.AbsoluteUri);
            Assert.Equal("c2", tokenRequestHttpHandler.ActualRequestPostData["client_id"]);
            Assert.Equal("https://login.microsoftonline.com/t2/oauth2/v2.0/token",
                         tokenRequestHttpHandler.ActualRequestMessage?.RequestUri?.AbsoluteUri);

            if (withFmiPath)
            {
                Assert.Equal("myFmiPathForSignedAssertion", credentialRequestHttpHandler.ActualRequestPostData["fmi_path"]);
            }

            // Claims: absent on first request, present on second with cp1
            Assert.False(credentialRequestHttpHandler.ActualRequestPostData.ContainsKey("claims"));
            Assert.True(tokenRequestHttpHandler.ActualRequestPostData.ContainsKey("claims"));

            var claimsJson = tokenRequestHttpHandler.ActualRequestPostData["claims"];
            using var doc = JsonDocument.Parse(claimsJson);
            var cp = doc.RootElement
                        .GetProperty("access_token")
                        .GetProperty("xms_cc")
                        .GetProperty("values")[0]
                        .GetString();
            Assert.Equal("cp1", cp);

            // First token is reused as client_assertion on second request
            string accessTokenFromRequest1;
            using (var document = JsonDocument.Parse(credentialRequestHttpHandler.ResponseString))
            {
                accessTokenFromRequest1 = document.RootElement.GetProperty("access_token").GetString()!;
            }
            Assert.Equal(accessTokenFromRequest1, tokenRequestHttpHandler.ActualRequestPostData["client_assertion"]);

            // Bearer header returned
            Assert.StartsWith("Bearer", header, StringComparison.Ordinal);
        }

        [Fact]
        public async Task Fic_CustomSignedAssertion_HonorsInjectedCloudMetadataProvider()
        {
            using var httpFactoryForTest = new MockHttpClientFactory();
            // First request (credential exchange) — its "scope" is what we assert on.
            var credentialRequestHttpHandler = httpFactoryForTest.AddMockHandler(
                MockHttpCreator.CreateClientCredentialTokenHandler("token-exchange-1"));
            // Second request (actual token acquisition). The handler must be queued so the second leg has a
            // response, but the test asserts only on the first (credential-exchange) request.
            httpFactoryForTest.AddMockHandler(
                MockHttpCreator.CreateClientCredentialTokenHandler("final-access-token"));

            TokenAcquirerFactoryTesting.ResetTokenAcquirerFactoryInTest();
            var tokenAcquirerFactory = TokenAcquirerFactory.GetDefaultInstance();
            tokenAcquirerFactory.Services.AddOidcFic();
            tokenAcquirerFactory.Services.AddSingleton<IHttpClientFactory>(httpFactoryForTest);

            // A caller (or MISE) registers an upstream cloud-metadata provider that OVERRIDES the FIC
            // audience for the source app's cloud (US Gov). This must win over MSAL's built-in US Gov value,
            // proving the OIDC client-credentials FIC leg honors the injected ICloudMetadataProvider end-to-end.
            tokenAcquirerFactory.Services.AddSingleton<ICloudMetadataProvider>(
                new InMemoryCloudMetadataProvider().AddOrUpdate(
                    "login.microsoftonline.us",
                    new Dictionary<string, string>
                    {
                        [AbstractionsCloudKeys.TokenExchangeAudience] = "api://AzureADTokenExchangeCustomGov"
                    }));

            // Source app (provides assertion), configured on US Gov.
            tokenAcquirerFactory.Services.Configure<MicrosoftIdentityApplicationOptions>("AzureAd2", options =>
            {
                options.Instance = "https://login.microsoftonline.us/";
                options.TenantId = "t1";
                options.ClientId = "c1";
                options.ClientCredentials = new[]
                {
                    new CredentialDescription
                    {
                        SourceType = CredentialSource.ClientSecret,
                        ClientSecret = TestConstants.ClientSecret
                    }
                };
            });

            // Target app (uses custom signed assertion).
            tokenAcquirerFactory.Services.Configure<MicrosoftIdentityApplicationOptions>(options =>
            {
                options.Instance = "https://login.microsoftonline.com/";
                options.TenantId = "t2";
                options.ClientId = "c2";
                options.ClientCredentials = new[]
                {
                    new CredentialDescription
                    {
                        SourceType = CredentialSource.CustomSignedAssertion,
                        CustomSignedAssertionProviderName = "OidcIdpSignedAssertion",
                        CustomSignedAssertionProviderData = new Dictionary<string, object>
                        {
                            ["ConfigurationSection"] = "AzureAd2"
                        }
                    }
                };
            });

            var serviceProvider = tokenAcquirerFactory.Build();
            var authorizationHeaderProvider = serviceProvider.GetRequiredService<IAuthorizationHeaderProvider>();

            var header = await authorizationHeaderProvider.CreateAuthorizationHeaderForAppAsync(
                TestConstants.s_scopeForApp);

            // The injected provider's audience — NOT MSAL's built-in "api://AzureADTokenExchangeUSGov" —
            // is used for the credential exchange, with /.default computed by the resolver.
            Assert.Equal("api://AzureADTokenExchangeCustomGov/.default", credentialRequestHttpHandler.ActualRequestPostData["scope"]);
            Assert.StartsWith("Bearer", header, StringComparison.Ordinal);
        }

        // ---------------------------------------------------------------------------------------------
        // Cross-cloud token-exchange (FIC) pseudo-E2E tests: public / sovereign x default / custom.
        //
        // Each test builds the REAL OIDC-CC FIC pipeline (ID Web -> MSAL) and mocks ONLY MSAL's outbound
        // HTTP, then asserts the credential-exchange request's POST 'scope' equals the expected cloud-
        // specific token-exchange audience + "/.default". Auto-resolution here flows from MSAL's built-in
        // baseline (KnownCloudData) keyed by the SOURCE app's cloud — no injected provider needed for the
        // clouds MSAL ships (public, US Gov). A caller override (credential TokenExchangeUrl) must win.
        // The US-Gov-default case is also the regression guard for the Authority-vs-Instance resolution fix.
        // ---------------------------------------------------------------------------------------------

        [Fact]
        public Task Fic_Exchange_PublicCloud_DefaultEndpoint_SendsPublicAudienceAsync()
            => RunFicExchangeScenarioAsync(
                scenario: "Public cloud, default endpoint (auto-resolve via MSAL baseline)",
                sourceInstance: "https://login.microsoftonline.com/",
                sourceAuthority: null,
                customTokenExchangeUrl: null,
                expectedExchangeScope: "api://AzureADTokenExchange/.default");

        [Fact]
        public Task Fic_Exchange_UsGovCloud_DefaultEndpoint_AutoResolvesUsGovAudienceAsync()
            // Source app configured via AUTHORITY ONLY (Instance empty) — this is the regression guard for
            // the OidcIdpSignedAssertionProvider fix: '_options.Instance ?? _options.Authority' silently
            // fell back to the public audience because Instance defaults to "" (not null). US Gov is a
            // cloud MSAL ships, so its audience auto-resolves from the baseline with no injected provider.
            => RunFicExchangeScenarioAsync(
                scenario: "US Gov cloud (Authority-only source), default endpoint (auto-resolve via MSAL baseline)",
                sourceInstance: null,
                sourceAuthority: "https://login.microsoftonline.us/t1",
                customTokenExchangeUrl: null,
                expectedExchangeScope: "api://AzureADTokenExchangeUSGov/.default");

        [Fact]
        public Task Fic_Exchange_PublicCloud_CustomEndpoint_SendsCustomAudienceAsync()
            => RunFicExchangeScenarioAsync(
                scenario: "Public cloud, caller-provided custom endpoint (override wins)",
                sourceInstance: "https://login.microsoftonline.com/",
                sourceAuthority: null,
                customTokenExchangeUrl: "api://MyCustomTokenExchange",
                expectedExchangeScope: "api://MyCustomTokenExchange/.default");

        /// <summary>
        /// Drives the real OIDC-CC FIC pipeline (ID Web -> MSAL, mocking only MSAL HTTP) and asserts the
        /// credential-exchange request carried <paramref name="expectedExchangeScope"/> as its POST 'scope'.
        /// The source app ("AzureAd2") is configured via EITHER <paramref name="sourceInstance"/> OR
        /// <paramref name="sourceAuthority"/> (exactly one non-null); cloud resolution keys off it.
        /// </summary>
        private async Task RunFicExchangeScenarioAsync(
            string scenario,
            string? sourceInstance,
            string? sourceAuthority,
            string? customTokenExchangeUrl,
            string expectedExchangeScope)
        {
            using var httpFactoryForTest = new MockHttpClientFactory();
            // Leg 1: the credential EXCHANGE (client-credentials on the source cloud). Its 'scope' is the
            // cloud-specific token-exchange audience under test.
            var credentialRequestHttpHandler = httpFactoryForTest.AddMockHandler(
                MockHttpCreator.CreateClientCredentialTokenHandler("token-exchange-1"));
            // Leg 2: the target app's actual token acquisition using the signed assertion.
            httpFactoryForTest.AddMockHandler(
                MockHttpCreator.CreateClientCredentialTokenHandler("final-access-token"));

            TokenAcquirerFactoryTesting.ResetTokenAcquirerFactoryInTest();
            var tokenAcquirerFactory = TokenAcquirerFactory.GetDefaultInstance();
            tokenAcquirerFactory.Services.AddOidcFic();
            tokenAcquirerFactory.Services.AddSingleton<IHttpClientFactory>(httpFactoryForTest);

            // Source app (provides assertion), configured on the cloud under test — via Instance OR Authority.
            tokenAcquirerFactory.Services.Configure<MicrosoftIdentityApplicationOptions>("AzureAd2", options =>
            {
                if (!string.IsNullOrEmpty(sourceInstance))
                {
                    options.Instance = sourceInstance;
                    options.TenantId = "t1";
                }
                else
                {
                    options.Authority = sourceAuthority;
                }

                options.ClientId = "c1";
                options.ClientCredentials = new[]
                {
                    new CredentialDescription
                    {
                        SourceType = CredentialSource.ClientSecret,
                        ClientSecret = TestConstants.ClientSecret
                    }
                };
            });

            // Target app (uses the custom signed assertion; a per-call TokenExchangeUrl overrides resolution).
            var customSignedAssertion = new CredentialDescription
            {
                SourceType = CredentialSource.CustomSignedAssertion,
                CustomSignedAssertionProviderName = "OidcIdpSignedAssertion",
                CustomSignedAssertionProviderData = new Dictionary<string, object>
                {
                    ["ConfigurationSection"] = "AzureAd2"
                }
            };
            if (!string.IsNullOrEmpty(customTokenExchangeUrl))
            {
                customSignedAssertion.TokenExchangeUrl = customTokenExchangeUrl;
            }

            tokenAcquirerFactory.Services.Configure<MicrosoftIdentityApplicationOptions>(options =>
            {
                options.Instance = "https://login.microsoftonline.com/";
                options.TenantId = "t2";
                options.ClientId = "c2";
                options.ClientCredentials = new[] { customSignedAssertion };
            });

            var serviceProvider = tokenAcquirerFactory.Build();
            var authorizationHeaderProvider = serviceProvider.GetRequiredService<IAuthorizationHeaderProvider>();

            var header = await authorizationHeaderProvider.CreateAuthorizationHeaderForAppAsync(
                TestConstants.s_scopeForApp);

            // PRIMARY: the exchange request MSAL sent carried the expected cloud-specific scope.
            Assert.Equal(expectedExchangeScope, credentialRequestHttpHandler.ActualRequestPostData["scope"]);
            Assert.StartsWith("Bearer", header, StringComparison.Ordinal);
        }
    }
}
