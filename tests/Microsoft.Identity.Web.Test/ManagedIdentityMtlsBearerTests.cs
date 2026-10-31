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
        [InlineData("", true)]
        [InlineData("6325cd32-9911-41f3-819c-416cdf9104e7", true)]
        [InlineData("", false)]
        public async Task Configuration_Mtls_RequiresAttestationByDefault(string clientId, bool requestOverMtls)
        {
            using var json = new MemoryStream(Encoding.UTF8.GetBytes($$"""
                {
                  "DownstreamApi": {
                    "ProtocolScheme": "{{(requestOverMtls ? "Bearer" : "MTLS_POP")}}",
                    "RequestAppToken": true,
                    "AcquireTokenOptions": {
                      "ManagedIdentity": { "UserAssignedClientId": "{{clientId}}" },
                      "RequestOverMtls": {{requestOverMtls.ToString().ToLowerInvariant()}}
                    }
                  }
                }
                """));
            IConfiguration configuration = new ConfigurationBuilder().AddJsonStream(json).Build();
            TokenAcquirerFactory factory = CreateFactory(requestOverMtls);
            factory.Services.AddDownstreamApi("MtlsBearer", configuration.GetSection("DownstreamApi"));
            IServiceProvider services = factory.Build();
            DownstreamApiOptions options = services.GetRequiredService<IOptionsMonitor<DownstreamApiOptions>>().Get("MtlsBearer");
            Assert.Equal(KeyAttestationMode.Required, options.AcquireTokenOptions.KeyAttestation);

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
                RequestOverMtls = true,
                KeyAttestation = KeyAttestationMode.Disabled,
            };
            TokenAcquisitionOptions clone = options.Clone();
            Assert.True(clone.RequestOverMtls);
            Assert.Equal(KeyAttestationMode.Disabled, clone.KeyAttestation);
            clone.KeyAttestation = KeyAttestationMode.Required;
            Assert.Equal(KeyAttestationMode.Disabled, options.KeyAttestation);

            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => factory.GetTokenAcquirer().GetTokenForAppAsync(Scope, clone));

            Assert.Equal(AttestationInvoked, exception.Message);
        }

        [Fact]
        public async Task RequestOverMtls_Disabled_PreservesBearerFlow()
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
                });

            Assert.Equal("test-access-token", result.AccessToken);
            Assert.Equal("Bearer", result.TokenType);
            Assert.Null(result.BindingCertificate);
        }

        [Fact]
        public async Task KeyAttestation_InvalidValue_IsRejected()
        {
            ITokenAcquisition acquisition = CreateFactory().Build().GetRequiredService<ITokenAcquisition>();

            ArgumentOutOfRangeException exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
                () => acquisition.GetAuthenticationResultForAppAsync(Scope, tokenAcquisitionOptions: new TokenAcquisitionOptions
                {
                    ManagedIdentity = new ManagedIdentityOptions(),
                    RequestOverMtls = true,
                    KeyAttestation = (KeyAttestationMode)42,
                }));

            Assert.Equal(nameof(AcquireTokenOptions.KeyAttestation), exception.ParamName);
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
                    RequestOverMtls = true,
                    ExtraParameters = new Dictionary<string, object>
                    {
                        ["IsTokenBinding"] = tokenBinding,
                    },
                }));

            Assert.Contains("requires managed identity", exception.Message, StringComparison.Ordinal);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task KeyAttestation_RequiredWithoutProvider_Fails(bool requestOverMtls)
        {
            ITokenAcquisition acquisition = CreateFactory(null).Build().GetRequiredService<ITokenAcquisition>();

            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => acquisition.GetAuthenticationResultForAppAsync(Scope, tokenAcquisitionOptions: new TokenAcquisitionOptions
                {
                    ManagedIdentity = new ManagedIdentityOptions(),
                    RequestOverMtls = requestOverMtls,
                    ExtraParameters = new Dictionary<string, object> { ["IsTokenBinding"] = !requestOverMtls },
                }));

            Assert.Contains("requires key attestation", exception.Message, StringComparison.Ordinal);
            Assert.Contains("AddMicrosoftIdentityWebKeyAttestation()", exception.Message, StringComparison.Ordinal);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task KeyAttestation_Disabled_SkipsProviderAndCachedPlainBearer(bool requestOverMtls)
        {
            // Arrange
            TokenAcquirerFactory factory = CreateFactory(requestOverMtls);
            var mockHttp = new MockHttpClientFactory();
            mockHttp.AddMockHandler(MockHttpCreator.CreateMsiTokenHandler("cached-plain-bearer"));
            factory.Services.AddSingleton<IManagedIdentityTestHttpClientFactory>(
                _ => new TestManagedIdentityHttpFactory(mockHttp));
            IServiceProvider services = factory.Build();
            var options = new AcquireTokenOptions
            {
                ManagedIdentity = new ManagedIdentityOptions { UserAssignedClientId = Guid.NewGuid().ToString() },
            };
            AcquireTokenResult cached = await factory.GetTokenAcquirer().GetTokenForAppAsync(Scope, options);
            Assert.Equal("cached-plain-bearer", cached.AccessToken);

            options.RequestOverMtls = requestOverMtls;
            options.KeyAttestation = KeyAttestationMode.Disabled;

            // Act: no HTTP response remains for mTLS acquisition; it must not return the plain cached token.
            Exception? exception = await Record.ExceptionAsync(async () =>
            {
                if (requestOverMtls)
                {
                    await factory.GetTokenAcquirer().GetTokenForAppAsync(Scope, options);
                }
                else
                {
                    await services.GetRequiredService<IAuthorizationHeaderProvider>()
                        .CreateAuthorizationHeaderForAppAsync(Scope, new AuthorizationHeaderProviderOptions
                        {
                            ProtocolScheme = "MTLS_POP",
                            RequestAppToken = true,
                            AcquireTokenOptions = options,
                        });
                }
            });

            // Assert
            Assert.NotNull(exception);
            Assert.NotEqual(AttestationInvoked, exception.Message);
            Assert.IsAssignableFrom<MsalException>(exception);
        }

        private static TokenAcquirerFactory CreateFactory(bool? requestOverMtls = true)
        {
            TokenAcquirerFactoryTesting.ResetTokenAcquirerFactoryInTest();
            TokenAcquirerFactory factory = TokenAcquirerFactory.GetDefaultInstance();
            if (requestOverMtls.HasValue)
            {
                factory.Services.AddSingleton<IKeyAttestationProvider>(
                    new InspectingAttestationProvider(requestOverMtls.Value));
            }

            return factory;
        }

        private sealed class InspectingAttestationProvider : IKeyAttestationProvider
        {
            private readonly bool _requestOverMtls;

            public InspectingAttestationProvider(bool requestOverMtls)
            {
                _requestOverMtls = requestOverMtls;
            }

            public AcquireTokenForManagedIdentityParameterBuilder EnableAttestation(
                AcquireTokenForManagedIdentityParameterBuilder builder)
            {
                MsalClientException exception = Assert.Throws<MsalClientException>(() =>
                {
                    if (_requestOverMtls)
                    {
                        builder.WithMtlsProofOfPossession();
                    }
                    else
                    {
                        builder.WithRequestOverMtls();
                    }
                });
                Assert.Equal("mtls_pop_and_bearer_exclusive", exception.ErrorCode);
                throw new InvalidOperationException(AttestationInvoked);
            }
        }
    }
}
