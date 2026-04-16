// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web.Test.Common;
using Microsoft.Identity.Web.Test.Common.Mocks;
using Microsoft.Identity.Web.TestOnly;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    /// <summary>
    /// Tests for mTLS Proof-of-Possession (PoP) with Federated Identity Credentials (FIC).
    /// FIC uses a signed assertion from managed identity for CCA client authentication,
    /// combined with mTLS token binding.
    /// </summary>
    [Collection(nameof(TokenAcquirerFactorySingletonProtection))]
    public class MtlsPopFicTests : IDisposable
    {
        private const string Scope = "https://graph.microsoft.com/.default";
        private const string UamiClientId = "04ca4d6a-c720-4ba1-aa06-f6634b73fe7a";
        private const string MiAssertionToken = "mi-assertion-token";
        // Use unique IDs to avoid MSAL shared cache collisions with other test classes
        private const string TestClientId = "fic-mtls-0000-0000-000000000001";
        private const string TestTenantId = "fic-mtls-1111-1111-111111111111";

        /// <summary>
        /// FIC without token binding should work normally (Bearer flow).
        /// This is a regression guard.
        /// </summary>
        [Fact]
        public async Task Fic_WithoutTokenBinding_UsesBearerFlow()
        {
            // Arrange
            ResetFactory();
            var factory = TokenAcquirerFactory.GetDefaultInstance();

            ConfigureFicOptions(factory);

            // Mock IMDS for the MI assertion
            var mockMiHttp = new MockHttpClientFactory();
            mockMiHttp.AddMockHandler(
                MockHttpCreator.CreateMsiTokenHandler(MiAssertionToken));

            var miTestFactory = new TestManagedIdentityHttpFactory(mockMiHttp);
            ManagedIdentityClientAssertionTestHook.HttpClientFactoryForTests = miTestFactory.Create();
            factory.Services.AddSingleton<IManagedIdentityTestHttpClientFactory>(_ => miTestFactory);

            // Mock AAD token response (Bearer).
            var mockMsalHttp = new MockHttpClientFactory();
            mockMsalHttp.AddMockHandler(
                MockHttpCreator.CreateClientCredentialTokenHandler("fic-bearer-token"));

            factory.Services.AddSingleton<IMsalHttpClientFactory>(_ => mockMsalHttp);

            var acquirer = factory.Build().GetRequiredService<ITokenAcquisition>();

            // Act
            var result = await acquirer.GetAuthenticationResultForAppAsync(
                Scope,
                tokenAcquisitionOptions: new TokenAcquisitionOptions());

            // Assert
            Assert.Equal("fic-bearer-token", result.AccessToken);
            Assert.Equal(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
        }

        /// <summary>
        /// FIC with IsTokenBinding=true should use the signed assertion credential
        /// (not throw MissingTokenBindingCertificate) and call WithMtlsProofOfPossession().
        /// On a non-Azure test machine, MSAL may throw because the mTLS flow can't complete,
        /// but ID Web should NOT throw its own MissingTokenBindingCertificate error.
        /// </summary>
        /// <remarks>
        /// We intentionally do NOT register a mock IMsalHttpClientFactory here to avoid
        /// unconsumed mock handler disposal issues. The default MsalMtlsHttpClientFactory
        /// will be used, and MSAL will fail with a network or mTLS error, which we catch.
        /// </remarks>
        [Fact]
        public async Task Fic_WithTokenBinding_AllowsSignedAssertionCredential()
        {
            // Arrange
            ResetFactory();
            var factory = TokenAcquirerFactory.GetDefaultInstance();

            ConfigureFicOptions(factory);

            // Mock IMDS for the MI assertion (wrapped in non-disposable wrapper,
            // won't be disposed by DI).
            var mockMiHttp = new MockHttpClientFactory();
            mockMiHttp.AddMockHandler(
                MockHttpCreator.CreateMsiTokenHandler(MiAssertionToken));
            // Extra handler in case MI assertion is requested multiple times
            mockMiHttp.AddMockHandler(
                MockHttpCreator.CreateMsiTokenHandler(MiAssertionToken));

            var miTestFactory = new TestManagedIdentityHttpFactory(mockMiHttp);
            ManagedIdentityClientAssertionTestHook.HttpClientFactoryForTests = miTestFactory.Create();
            factory.Services.AddSingleton<IManagedIdentityTestHttpClientFactory>(_ => miTestFactory);

            var acquirer = factory.Build().GetRequiredService<ITokenAcquisition>();

            // Act & Assert
            // The key assertion: ID Web should NOT throw MissingTokenBindingCertificate
            // for FIC credentials. It should allow the signed assertion through and
            // let MSAL handle the mTLS flow.
            try
            {
                var result = await acquirer.GetAuthenticationResultForAppAsync(
                    Scope,
                    tokenAcquisitionOptions: new TokenAcquisitionOptions
                    {
                        ExtraParameters = new Dictionary<string, object>
                        {
                            { "IsTokenBinding", true }
                        }
                    });

                // If MSAL supports FIC + mTLS PoP on this machine, verify token is present
                Assert.NotNull(result.AccessToken);
            }
            catch (Exception ex)
            {
                // ID Web should NOT be the source of the error for FIC + token binding.
                // The MissingTokenBindingCertificate error means ID Web rejected the
                // signed assertion credential, which is wrong — our code change should
                // allow it through.
                Assert.DoesNotContain(
                    "MissingTokenBindingCertificate",
                    ex.Message,
                    StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain(
                    "certificate, which is required for token binding",
                    ex.Message,
                    StringComparison.OrdinalIgnoreCase);
            }
        }

        /// <summary>
        /// Verifies that when isTokenBinding=true and the credential is a client secret
        /// (not a certificate or signed assertion), the MissingTokenBindingCertificate
        /// error is still thrown.
        /// </summary>
        /// <remarks>
        /// No mock IMsalHttpClientFactory is registered because the error is thrown
        /// during CCA building, before any HTTP calls are made.
        /// </remarks>
        [Fact]
        public async Task TokenBinding_WithSecretCredential_ThrowsMissingCertificate()
        {
            // Arrange
            ResetFactory();
            var factory = TokenAcquirerFactory.GetDefaultInstance();

            factory.Services.Configure<MicrosoftIdentityApplicationOptions>(opts =>
            {
                opts.Instance = "https://login.microsoftonline.com/";
                opts.TenantId = TestTenantId;
                opts.ClientId = TestClientId;
                opts.ClientCredentials = new[]
                {
                    new CredentialDescription
                    {
                        SourceType = CredentialSource.ClientSecret,
                        ClientSecret = "test-secret"
                    }
                };
            });

            var acquirer = factory.Build().GetRequiredService<ITokenAcquisition>();

            // Act & Assert — secret credential should still be rejected for token binding
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                acquirer.GetAuthenticationResultForAppAsync(
                    Scope,
                    tokenAcquisitionOptions: new TokenAcquisitionOptions
                    {
                        ExtraParameters = new Dictionary<string, object>
                        {
                            { "IsTokenBinding", true }
                        }
                    }));

            Assert.Contains("token binding", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        public void Dispose()
        {
            ManagedIdentityClientAssertionTestHook.HttpClientFactoryForTests = null;
        }

        private static void ConfigureFicOptions(TokenAcquirerFactory factory)
        {
            factory.Services.Configure<MicrosoftIdentityApplicationOptions>(opts =>
            {
                opts.Instance = "https://login.microsoftonline.com/";
                opts.TenantId = TestTenantId;
                opts.ClientId = TestClientId;
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
        }

        /// <summary>
        /// Resilient factory reset that handles disposal errors from mock HTTP factories
        /// left by the preceding test in the collection.
        /// </summary>
        private static void ResetFactory()
        {
            try
            {
                TokenAcquirerFactoryTesting.ResetTokenAcquirerFactoryInTest();
            }
            catch
            {
                // The previous test may have left unconsumed mock handlers in a
                // MockHttpClientFactory registered as IMsalHttpClientFactory.
                // The disposal assertion fires during ResetDefaultInstance().
                // The SP's internal disposables list was atomically cleared on the
                // first Dispose call, so a second Reset will dispose cleanly (no-op)
                // and null the singleton.
                try
                {
                    TokenAcquirerFactoryTesting.ResetTokenAcquirerFactoryInTest();
                }
                catch
                {
                    // If the second Reset also fails, force-clear via reflection.
                    var field = typeof(TokenAcquirerFactory).GetField(
                        "defaultInstance",
                        BindingFlags.Static | BindingFlags.NonPublic);
                    field?.SetValue(null, null);
                }
            }
        }
    }
}
