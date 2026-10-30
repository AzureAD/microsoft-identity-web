// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web.Test.Common;
using Microsoft.Identity.Web.Test.Common.Mocks;
using Microsoft.Identity.Web.TestOnly;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    [Collection(nameof(TokenAcquirerFactorySingletonProtection))]
    public class ManagedIdentityMtlsBearerTests
    {
        private const string Scope = "https://graph.microsoft.com/.default";
        private const string AttestationInvoked = "Bearer-over-mTLS attestation invoked.";

        [Theory]
        [InlineData("")]
        [InlineData("6325cd32-9911-41f3-819c-416cdf9104e7")]
        public async Task Configuration_RequestOverMtls_ReachesMsalAndAttestation(string clientId)
        {
            using var json = new MemoryStream(Encoding.UTF8.GetBytes($$"""
                {
                  "DownstreamApi": {
                    "ProtocolScheme": "Bearer",
                    "RequestAppToken": true,
                    "AcquireTokenOptions": {
                      "ManagedIdentity": { "UserAssignedClientId": "{{clientId}}" },
                      "ExtraParameters": { "RequestOverMtls": true }
                    }
                  }
                }
                """));
            IConfiguration configuration = new ConfigurationBuilder().AddJsonStream(json).Build();
            TokenAcquirerFactory factory = CreateFactory();
            factory.Services.AddDownstreamApi("MtlsBearer", configuration.GetSection("DownstreamApi"));
            IServiceProvider services = factory.Build();
            DownstreamApiOptions options = services.GetRequiredService<IOptionsMonitor<DownstreamApiOptions>>().Get("MtlsBearer");

            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => services.GetRequiredService<IAuthorizationHeaderProvider>()
                    .CreateAuthorizationHeaderForAppAsync(Scope, options));

            Assert.Equal(AttestationInvoked, exception.Message);
        }

        [Fact]
        public async Task TokenAcquirer_ClonedOptions_PreserveRequestOverMtls()
        {
            TokenAcquirerFactory factory = CreateFactory();
            _ = factory.Build();
            var options = new TokenAcquisitionOptions
            {
                ManagedIdentity = new ManagedIdentityOptions(),
                ExtraParameters = new Dictionary<string, object> { ["RequestOverMtls"] = true },
            };
            TokenAcquisitionOptions clone = options.Clone();
            clone.ExtraParameters!["RequestOverMtls"] = "true";
            Assert.Equal(true, options.ExtraParameters["RequestOverMtls"]);

            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => factory.GetTokenAcquirer().GetTokenForAppAsync(Scope, clone));

            Assert.Equal(AttestationInvoked, exception.Message);
        }

        [Theory]
        [InlineData(false)]
        [InlineData("false")]
        [InlineData(null)]
        public async Task RequestOverMtls_Disabled_PreservesBearerFlow(object? enabled)
        {
            TokenAcquirerFactory factory = CreateFactory();
            var mockHttp = new MockHttpClientFactory();
            mockHttp.AddMockHandler(MockHttpCreator.CreateMsiTokenHandler("test-access-token"));
            factory.Services.AddSingleton<IManagedIdentityTestHttpClientFactory>(
                _ => new TestManagedIdentityHttpFactory(mockHttp));
            ITokenAcquisition acquisition = factory.Build().GetRequiredService<ITokenAcquisition>();

            AuthenticationResult result = await acquisition.GetAuthenticationResultForAppAsync(
                Scope,
                tokenAcquisitionOptions: new TokenAcquisitionOptions
                {
                    ManagedIdentity = new ManagedIdentityOptions(),
                    ExtraParameters = enabled is null ? null : new Dictionary<string, object> { ["RequestOverMtls"] = enabled },
                });

            Assert.Equal("test-access-token", result.AccessToken);
            Assert.Equal("Bearer", result.TokenType);
            Assert.Null(result.BindingCertificate);
        }

        [Theory]
        [InlineData("invalid")]
        [InlineData(1)]
        public async Task RequestOverMtls_InvalidValue_IsRejected(object value)
        {
            ITokenAcquisition acquisition = CreateFactory().Build().GetRequiredService<ITokenAcquisition>();

            ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(
                () => acquisition.GetAuthenticationResultForAppAsync(Scope, tokenAcquisitionOptions: new TokenAcquisitionOptions
                {
                    ManagedIdentity = new ManagedIdentityOptions(),
                    ExtraParameters = new Dictionary<string, object> { ["RequestOverMtls"] = value },
                }));

            Assert.Contains("must be a Boolean", exception.Message, StringComparison.Ordinal);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task RequestOverMtls_UnsupportedCombination_IsRejected(bool tokenBinding)
        {
            ITokenAcquisition acquisition = CreateFactory().Build().GetRequiredService<ITokenAcquisition>();

            ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(
                () => acquisition.GetAuthenticationResultForAppAsync(Scope, tokenAcquisitionOptions: new TokenAcquisitionOptions
                {
                    ManagedIdentity = tokenBinding ? new ManagedIdentityOptions() : null,
                    ExtraParameters = new Dictionary<string, object>
                    {
                        ["RequestOverMtls"] = true,
                        ["IsTokenBinding"] = tokenBinding,
                    },
                }));

            Assert.Contains("requires managed identity", exception.Message, StringComparison.Ordinal);
        }

        private static TokenAcquirerFactory CreateFactory()
        {
            TokenAcquirerFactoryTesting.ResetTokenAcquirerFactoryInTest();
            TokenAcquirerFactory factory = TokenAcquirerFactory.GetDefaultInstance();
            factory.Services.AddSingleton<IKeyAttestationProvider, InspectingAttestationProvider>();
            return factory;
        }

        private sealed class InspectingAttestationProvider : IKeyAttestationProvider
        {
            public AcquireTokenForManagedIdentityParameterBuilder EnableAttestation(
                AcquireTokenForManagedIdentityParameterBuilder builder)
            {
                // MSAL rejects PoP only if WithRequestOverMtls has already selected Bearer-over-mTLS.
                MsalClientException exception = Assert.Throws<MsalClientException>(() => builder.WithMtlsProofOfPossession());
                Assert.Equal("mtls_pop_and_bearer_exclusive", exception.ErrorCode);
                throw new InvalidOperationException(AttestationInvoked);
            }
        }
    }
}
