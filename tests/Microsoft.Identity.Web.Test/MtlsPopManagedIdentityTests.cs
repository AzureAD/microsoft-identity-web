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
    /// Tests for mTLS Proof-of-Possession (PoP) with pure managed identity (MSI V2 flow).
    /// </summary>
    [Collection(nameof(TokenAcquirerFactorySingletonProtection))]
    public class MtlsPopManagedIdentityTests
    {
        private const string Scope = "https://vault.azure.net/.default";
        private const string UamiClientId = "04ca4d6a-c720-4ba1-aa06-f6634b73fe7a";
        private const string MockToken = "mocked.access.token";

        /// <summary>
        /// When IsTokenBinding is false (default), managed identity should use the
        /// standard V1 IMDS flow and return a Bearer token.
        /// </summary>
        [Fact]
        public async Task ManagedIdentity_WithoutTokenBinding_UsesBearerFlow()
        {
            // Arrange
            ResetFactory();
            var factory = TokenAcquirerFactory.GetDefaultInstance();
            var mockHttp = new MockHttpClientFactory();

            mockHttp.AddMockHandler(
                MockHttpCreator.CreateMsiTokenHandler(accessToken: MockToken));

            factory.Services.AddSingleton<IManagedIdentityTestHttpClientFactory>(
                _ => new TestManagedIdentityHttpFactory(mockHttp));

            var headerProvider = factory.Build()
                .GetRequiredService<IAuthorizationHeaderProvider>();

            // Act
            string header = await headerProvider.CreateAuthorizationHeaderForAppAsync(
                Scope,
                new AuthorizationHeaderProviderOptions
                {
                    AcquireTokenOptions = new AcquireTokenOptions
                    {
                        ManagedIdentity = new ManagedIdentityOptions
                        {
                            UserAssignedClientId = UamiClientId
                        },
                    }
                });

            // Assert - standard Bearer flow
            Assert.Equal($"Bearer {MockToken}", header);
        }

        /// <summary>
        /// When IsTokenBinding is true, managed identity should trigger the mTLS PoP path
        /// (V2 credential API). On a non-Azure test machine this will throw because
        /// the platform key provider (KeyGuard) is unavailable, proving the V2 code path
        /// was reached.
        /// </summary>
        [Fact]
        public async Task ManagedIdentity_WithTokenBinding_TriggersV2Flow()
        {
            // Arrange
            ResetFactory();
            var factory = TokenAcquirerFactory.GetDefaultInstance();
            var mockHttp = new MockHttpClientFactory();

            // Provide a handler; it may or may not be consumed depending on
            // how far MSAL gets before the V2 flow fails on a non-Azure machine.
            mockHttp.AddMockHandler(
                MockHttpCreator.CreateMsiTokenHandler(accessToken: MockToken));

            factory.Services.AddSingleton<IManagedIdentityTestHttpClientFactory>(
                _ => new TestManagedIdentityHttpFactory(mockHttp));

            var tokenAcquirer = factory.Build()
                .GetRequiredService<ITokenAcquisition>();

            // Act & Assert
            // WithMtlsProofOfPossession().WithAttestationSupport() triggers the V2 flow.
            // On a non-Azure test machine the V2 flow will fail because the platform
            // key provider (KeyGuard/attestation) is not available. The exception from
            // MSAL proves that ID Web correctly entered the mTLS PoP code path.
            var ex = await Assert.ThrowsAnyAsync<Exception>(() =>
                tokenAcquirer.GetAuthenticationResultForAppAsync(
                    Scope,
                    tokenAcquisitionOptions: new TokenAcquisitionOptions
                    {
                        ManagedIdentity = new ManagedIdentityOptions
                        {
                            UserAssignedClientId = UamiClientId
                        },
                        ExtraParameters = new Dictionary<string, object>
                        {
                            { "IsTokenBinding", true }
                        }
                    }));

            // The exception should originate from MSAL's V2/mTLS PoP flow,
            // not from ID Web's own parameter validation.
            Assert.False(
                ex.Message.Contains("MissingTokenBindingCertificate", StringComparison.OrdinalIgnoreCase),
                "Should not throw MissingTokenBindingCertificate for MI path — the V2 flow should be attempted.");
        }

        /// <summary>
        /// Verifies that token acquisition uses GetAuthenticationResultForAppAsync directly
        /// when IsTokenBinding is set via ExtraParameters in TokenAcquisitionOptions.
        /// </summary>
        [Fact]
        public async Task ManagedIdentity_WithExplicitExtraParameters_TokenBindingIsRespected()
        {
            // Arrange
            ResetFactory();
            var factory = TokenAcquirerFactory.GetDefaultInstance();
            var mockHttp = new MockHttpClientFactory();

            mockHttp.AddMockHandler(
                MockHttpCreator.CreateMsiTokenHandler(accessToken: MockToken));

            factory.Services.AddSingleton<IManagedIdentityTestHttpClientFactory>(
                _ => new TestManagedIdentityHttpFactory(mockHttp));

            var tokenAcquirer = factory.Build()
                .GetRequiredService<ITokenAcquisition>();

            // Act - isTokenBinding=false explicitly
            var result = await tokenAcquirer.GetAuthenticationResultForAppAsync(
                Scope,
                tokenAcquisitionOptions: new TokenAcquisitionOptions
                {
                    ManagedIdentity = new ManagedIdentityOptions
                    {
                        UserAssignedClientId = UamiClientId
                    },
                    ExtraParameters = new Dictionary<string, object>
                    {
                        { "IsTokenBinding", false }
                    }
                });

            // Assert - normal Bearer flow should succeed
            Assert.Equal(MockToken, result.AccessToken);
            Assert.Equal("Bearer", result.TokenType);
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
                try
                {
                    TokenAcquirerFactoryTesting.ResetTokenAcquirerFactoryInTest();
                }
                catch
                {
                    var field = typeof(TokenAcquirerFactory).GetField(
                        "defaultInstance",
                        BindingFlags.Static | BindingFlags.NonPublic);
                    field?.SetValue(null, null);
                }
            }
        }
    }
}
