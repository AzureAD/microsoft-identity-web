// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.Test.Common;
using Microsoft.Identity.Web.TestOnly;
using Microsoft.Identity.Web.TokenCacheProviders.InMemory;
using Microsoft.IdentityModel.JsonWebTokens;
using Xunit;

namespace TokenAcquirerTests
{
#if !FROM_GITHUB_ACTION
    /// <summary>
    /// E2E tests for Managed Identity Federated Identity Credential (MSI FIC) token binding through
    /// Microsoft.Identity.Web, mirroring MSAL.NET's <c>ManagedIdentityImdsV2FicTests</c>
    /// (https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/pull/6132).
    ///
    /// Identity Web composes the two legs when the app token is requested with token binding
    /// (<c>ExtraParameters["IsTokenBinding"] = true</c>, i.e. <c>ProtocolScheme = "MTLS_POP"</c>):
    ///   Leg 1 — the Managed Identity mints an <c>mtls_pop</c> assertion + binding certificate for
    ///           <c>api://AzureADTokenExchange</c>
    ///           (<see cref="ManagedIdentityClientAssertion.GetSignedAssertionWithBindingAsync"/>).
    ///   Leg 2 — the confidential app exchanges that assertion for a final <c>mtls_pop</c> token bound
    ///           to the same certificate.
    ///
    /// These tests require a VM with an assigned managed identity and Credential Guard, so they run on
    /// the MISEManagedIdentity pool (Category = MI_E2E), not on Microsoft-hosted agents.
    ///
    /// Note: the "bearer final token with a bound client assertion" variant (the exact shape of the
    /// MSAL bearer test) requires <c>UseBoundCredential</c> wiring for signed assertions, which is a
    /// separate Identity Web change; this test covers the final <c>mtls_pop</c> flow that the current
    /// code already supports.
    /// </summary>
    [Collection(nameof(TokenAcquirerFactorySingletonProtection))]
    [Trait("Category", TestCategories.ManagedIdentity)]
    public class ManagedIdentityFicTests
    {
        private const string TokenExchangeUrl = "api://AzureADTokenExchange";
        private const string GraphScope = "https://graph.microsoft.com/.default";

        // Confidential app registered in the MSI-team tenant with a FIC trusting the MSALMSIV2 pool
        // managed identity (the same app MSAL.NET's ManagedIdentityImdsV2FicTests uses).
        private const string FicConfAppClientId = "f62c5ae3-bf3a-4af5-afa8-a68b800396e9";
        private const string FicConfAppTenantId = "72f988bf-86f1-41af-91ab-2d7cd011db47";

        // UAMI assigned to the MISEManagedIdentity pool; the pipeline overrides this via
        // IDWEB_MI_UAMI_CLIENTID (kept consistent with the other managed-identity E2E tests).
        private const string DefaultUamiClientId = "6325cd32-9911-41f3-819c-416cdf9104e7";

        [OnlyOnAzureDevopsFact]
        public Task MsiFic_TwoLeg_FinalMtlsPop_SystemAssignedManagedIdentityAsync()
            => RunFicTwoLegFinalMtlsPopAsync(managedIdentityClientId: null);

        [OnlyOnAzureDevopsFact]
        public Task MsiFic_TwoLeg_FinalMtlsPop_UserAssignedManagedIdentityAsync()
            => RunFicTwoLegFinalMtlsPopAsync(
                managedIdentityClientId: Environment.GetEnvironmentVariable("IDWEB_MI_UAMI_CLIENTID") ?? DefaultUamiClientId);

        private static async Task RunFicTwoLegFinalMtlsPopAsync(string? managedIdentityClientId)
        {
            // Arrange — a confidential app whose only credential is a Managed Identity signed
            // assertion (the MSI FIC credential). No separate binding certificate is configured; the
            // binding certificate flows from the inner MSI acquisition result.
            TokenAcquirerFactoryTesting.ResetTokenAcquirerFactoryInTest();
            TokenAcquirerFactory tokenAcquirerFactory = TokenAcquirerFactory.GetDefaultInstance();

            tokenAcquirerFactory.Services.Configure<MicrosoftIdentityApplicationOptions>(options =>
            {
                options.Instance = "https://login.microsoftonline.com/";
                options.TenantId = FicConfAppTenantId;
                options.ClientId = FicConfAppClientId;
                options.ClientCredentials = new[]
                {
                    new CredentialDescription
                    {
                        SourceType = CredentialSource.SignedAssertionFromManagedIdentity,
                        ManagedIdentityClientId = managedIdentityClientId,
                        TokenExchangeUrl = TokenExchangeUrl,
                    },
                };
            });
            tokenAcquirerFactory.Services.AddInMemoryTokenCaches();

            IServiceProvider serviceProvider = tokenAcquirerFactory.Build();
            ITokenAcquirer tokenAcquirer = tokenAcquirerFactory.GetTokenAcquirer(string.Empty);

            var acquireTokenOptions = new AcquireTokenOptions
            {
                ExtraParameters = new Dictionary<string, object> { { "IsTokenBinding", true } },
            };

            // Act — Identity Web composes MSI Leg 1 (mtls_pop assertion + binding certificate) and
            // ConfApp Leg 2 (final mtls_pop token) when token binding is requested.
            AcquireTokenResult result = await tokenAcquirer.GetTokenForAppAsync(GraphScope, acquireTokenOptions);

            // Assert — final mtls_pop token bound to the certificate produced by the inner MSI acquisition.
            Assert.False(string.IsNullOrEmpty(result.AccessToken), "MSI FIC should return an access token.");
            Assert.Equal("mtls_pop", result.TokenType);
            Assert.NotNull(result.BindingCertificate);

            var jsonWebToken = new JsonWebToken(result.AccessToken);
            Assert.True(
                jsonWebToken.TryGetPayloadValue("cnf", out object? cnfClaim),
                "The mtls_pop token should contain a 'cnf' claim.");

            var cnfJson = JsonSerializer.Deserialize<JsonElement>(cnfClaim!.ToString()!);
            Assert.True(
                cnfJson.TryGetProperty("x5t#S256", out JsonElement x5tS256),
                "The 'cnf' claim should contain an 'x5t#S256' property.");

            // The token's cnf thumbprint must match the propagated binding certificate.
            Assert.Equal(ComputeX5tS256(result.BindingCertificate!), x5tS256.GetString());

            // Client authentication went over the mTLS endpoint.
            Assert.Contains(
                "mtlsauth.microsoft.com",
                result.Metadata?.TokenEndpoint ?? string.Empty,
                StringComparison.OrdinalIgnoreCase);

            // The second call for the confidential-app (Leg 2) token is served from cache.
            AcquireTokenResult cached = await tokenAcquirer.GetTokenForAppAsync(GraphScope, acquireTokenOptions);
            Assert.Equal(result.AccessToken, cached.AccessToken);
        }

        private static string ComputeX5tS256(X509Certificate2 certificate)
        {
            byte[] hash = SHA256.HashData(certificate.RawData);
            return Convert.ToBase64String(hash)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }
    }
#endif
}
