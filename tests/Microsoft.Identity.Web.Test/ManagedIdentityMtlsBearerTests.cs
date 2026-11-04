// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.KeyAttestation;
using Microsoft.Identity.Web.Test.Common.Mocks;
using Microsoft.Identity.Web.TestOnly;
using Xunit;
using MsalMtlsBindingStrength = Microsoft.Identity.Client.AppConfig.MtlsBindingStrength;

namespace Microsoft.Identity.Web.Test
{
    [CollectionDefinition(nameof(ManagedIdentityMtlsBearerTests), DisableParallelization = true)]
    [Collection(nameof(ManagedIdentityMtlsBearerTests))]
    public class ManagedIdentityMtlsBearerTests : IDisposable
    {
        private const string Scope = "https://graph.microsoft.com/.default";
        private const string AttestationInvoked = "Managed identity mTLS attestation invoked.";

        public ManagedIdentityMtlsBearerTests()
        {
            ResetManagedIdentitySource();
        }

        public void Dispose()
        {
            ResetManagedIdentitySource();
        }

        [Theory]
        [InlineData("", true, null, MtlsBindingStrength.None)]
        [InlineData("6325cd32-9911-41f3-819c-416cdf9104e7", true, null, MtlsBindingStrength.None)]
        [InlineData("", false, null, MtlsBindingStrength.None)]
        [InlineData("", false, "None", MtlsBindingStrength.None)]
        [InlineData("", false, "Software", MtlsBindingStrength.Software)]
        [InlineData("", false, "KeyGuard", MtlsBindingStrength.KeyGuard)]
        [InlineData("6325cd32-9911-41f3-819c-416cdf9104e7", false, "KeyGuard", MtlsBindingStrength.KeyGuard)]
        public async Task Configuration_Mtls_PreservesStrengthAndUsesRegisteredProvider(
            string clientId, bool requestOverMtls, string? configuredStrength, MtlsBindingStrength expectedStrength)
        {
            // Arrange
            string strengthJson = configuredStrength is null
                ? string.Empty
                : $",\"MinimumTokenBindingStrength\":\"{configuredStrength}\"";
            using var json = new MemoryStream(Encoding.UTF8.GetBytes($$"""
                {
                  "DownstreamApi": {
                    "ProtocolScheme": "{{(requestOverMtls ? "Bearer" : "MTLS_POP")}}",
                    "RequestAppToken": true,
                    "AcquireTokenOptions": {
                      "ManagedIdentity": { "UserAssignedClientId": "{{clientId}}" },
                      "RequestOverMtls": {{requestOverMtls.ToString().ToLowerInvariant()}}{{strengthJson}}
                    }
                  }
                }
                """));
            IConfiguration configuration = new ConfigurationBuilder().AddJsonStream(json).Build();
            var provider = new InspectingAttestationProvider(requestOverMtls, expectedStrength);
            TokenAcquirerFactory factory = CreateFactory(provider);
            factory.Services.AddDownstreamApi("Mtls", configuration.GetSection("DownstreamApi"));
            IServiceProvider services = factory.Build();
            DownstreamApiOptions options = services.GetRequiredService<IOptionsMonitor<DownstreamApiOptions>>().Get("Mtls");

            // Act
            Exception? exception = await Record.ExceptionAsync(
                () => services.GetRequiredService<IAuthorizationHeaderProvider>()
                    .CreateAuthorizationHeaderForAppAsync(Scope, options));

            // Assert
            Assert.Equal(expectedStrength, options.AcquireTokenOptions.MinimumTokenBindingStrength);
            AssertProviderFailure(exception, provider);
        }

        [Theory]
        [InlineData(true, MtlsBindingStrength.None)]
        [InlineData(false, MtlsBindingStrength.None)]
        [InlineData(false, MtlsBindingStrength.Software)]
        [InlineData(false, MtlsBindingStrength.KeyGuard)]
        public async Task TokenAcquirer_ClonedOptions_PreserveMtlsOptions(bool requestOverMtls, MtlsBindingStrength minimumStrength)
        {
            // Arrange
            var provider = new InspectingAttestationProvider(requestOverMtls, minimumStrength);
            TokenAcquirerFactory factory = CreateFactory(provider);
            _ = factory.Build();
            var options = new TokenAcquisitionOptions
            {
                ManagedIdentity = new ManagedIdentityOptions(),
                RequestOverMtls = requestOverMtls,
                MinimumTokenBindingStrength = minimumStrength,
                ExtraParameters = new Dictionary<string, object> { ["IsTokenBinding"] = !requestOverMtls },
            };
            TokenAcquisitionOptions clone = options.Clone();

            // Act
            Exception? exception = await Record.ExceptionAsync(
                () => factory.GetTokenAcquirer().GetTokenForAppAsync(Scope, clone));
            clone.MinimumTokenBindingStrength = minimumStrength == MtlsBindingStrength.KeyGuard
                ? MtlsBindingStrength.None
                : MtlsBindingStrength.KeyGuard;
            clone.RequestOverMtls = !requestOverMtls;

            // Assert
            Assert.Equal(requestOverMtls, options.RequestOverMtls);
            Assert.Equal(minimumStrength, options.MinimumTokenBindingStrength);
            AssertProviderFailure(exception, provider);
        }

        [Fact]
        public async Task UnsetMtlsOptions_PreserveBearerFlow()
        {
            // Arrange
            var provider = new InspectingAttestationProvider(true, MtlsBindingStrength.None);
            TokenAcquirerFactory factory = CreateFactory(provider);
            using var mockHttp = new MockHttpClientFactory();
            mockHttp.AddMockHandler(MockHttpCreator.CreateMsiTokenHandler("test-access-token"));
            factory.Services.AddSingleton<IManagedIdentityTestHttpClientFactory>(
                _ => new TestManagedIdentityHttpFactory(mockHttp));
            ITokenAcquisition acquisition = factory.Build().GetRequiredService<ITokenAcquisition>();
            var options = new TokenAcquisitionOptions { ManagedIdentity = new ManagedIdentityOptions() };

            // Act
            AuthenticationResult result = await acquisition.GetAuthenticationResultForAppAsync(
                Scope, tokenAcquisitionOptions: options);

            // Assert
            Assert.False(options.RequestOverMtls);
            Assert.Equal(MtlsBindingStrength.None, options.MinimumTokenBindingStrength);
            Assert.Equal("test-access-token", result.AccessToken);
            Assert.Equal("Bearer", result.TokenType);
            Assert.Null(result.BindingCertificate);
            Assert.Equal(0, provider.Calls);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(2)]
        [InlineData(42)]
        public async Task MinimumStrength_InvalidValue_IsRejected(int value)
        {
            ITokenAcquisition acquisition = CreateFactory().Build().GetRequiredService<ITokenAcquisition>();

            ArgumentOutOfRangeException exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
                () => acquisition.GetAuthenticationResultForAppAsync(Scope, tokenAcquisitionOptions: new TokenAcquisitionOptions
                {
                    ManagedIdentity = new ManagedIdentityOptions(),
                    MinimumTokenBindingStrength = (MtlsBindingStrength)value,
                    ExtraParameters = new Dictionary<string, object> { ["IsTokenBinding"] = true },
                }));

            Assert.Equal(nameof(AcquireTokenOptions.MinimumTokenBindingStrength), exception.ParamName);
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
                    ExtraParameters = new Dictionary<string, object> { ["IsTokenBinding"] = tokenBinding },
                }));

            Assert.Contains("requires managed identity", exception.Message, StringComparison.Ordinal);
        }

        [Theory]
        [InlineData(true, false, false, MtlsBindingStrength.Software)]
        [InlineData(true, false, true, MtlsBindingStrength.KeyGuard)]
        [InlineData(false, true, false, MtlsBindingStrength.KeyGuard)]
        [InlineData(false, false, false, MtlsBindingStrength.Software)]
        public async Task MinimumStrength_OutsideManagedIdentityPop_IsRejected(
            bool managedIdentity, bool tokenBinding, bool requestOverMtls, MtlsBindingStrength minimumStrength)
        {
            ITokenAcquisition acquisition = CreateFactory().Build().GetRequiredService<ITokenAcquisition>();

            ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(
                () => acquisition.GetAuthenticationResultForAppAsync(Scope, tokenAcquisitionOptions: new TokenAcquisitionOptions
                {
                    ManagedIdentity = managedIdentity ? new ManagedIdentityOptions() : null,
                    RequestOverMtls = requestOverMtls,
                    MinimumTokenBindingStrength = minimumStrength,
                    ExtraParameters = new Dictionary<string, object> { ["IsTokenBinding"] = tokenBinding },
                }));

            Assert.Equal(nameof(AcquireTokenOptions.MinimumTokenBindingStrength), exception.ParamName);
        }

        [Theory]
        [InlineData(true, MtlsBindingStrength.None)]
        [InlineData(false, MtlsBindingStrength.Software)]
        [InlineData(false, MtlsBindingStrength.KeyGuard)]
        public async Task UserFlow_RejectsManagedIdentityMtlsOptions(bool requestOverMtls, MtlsBindingStrength minimumStrength)
        {
            ITokenAcquisition acquisition = CreateFactory().Build().GetRequiredService<ITokenAcquisition>();

            ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(
                () => acquisition.GetAuthenticationResultForUserAsync(new[] { Scope }, tokenAcquisitionOptions: new TokenAcquisitionOptions
                {
                    RequestOverMtls = requestOverMtls,
                    MinimumTokenBindingStrength = minimumStrength,
                }));

            Assert.Contains("direct managed identity app token", exception.Message, StringComparison.Ordinal);
        }

        [Fact]
        public async Task KeyGuard_WithoutProvider_Fails()
        {
            ITokenAcquisition acquisition = CreateFactory().Build().GetRequiredService<ITokenAcquisition>();

            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => acquisition.GetAuthenticationResultForAppAsync(Scope, tokenAcquisitionOptions: new TokenAcquisitionOptions
                {
                    ManagedIdentity = new ManagedIdentityOptions(),
                    MinimumTokenBindingStrength = MtlsBindingStrength.KeyGuard,
                    ExtraParameters = new Dictionary<string, object> { ["IsTokenBinding"] = true },
                }));

            Assert.Contains("KeyGuard requires key attestation", exception.Message, StringComparison.Ordinal);
            Assert.Contains("AddMicrosoftIdentityWebKeyAttestation()", exception.Message, StringComparison.Ordinal);
        }

        [Theory]
        [InlineData(true, MtlsBindingStrength.None)]
        [InlineData(false, MtlsBindingStrength.None)]
        [InlineData(false, MtlsBindingStrength.Software)]
        public async Task Mtls_WithoutProvider_DoesNotRequireAttestationOrReusePlainBearer(
            bool requestOverMtls, MtlsBindingStrength minimumStrength)
        {
            // Arrange
            TokenAcquirerFactory factory = CreateFactory();
            using var mockHttp = new MockHttpClientFactory();
            mockHttp.AddMockHandler(MockHttpCreator.CreateMsiTokenHandler("cached-plain-bearer"));
            factory.Services.AddSingleton<IManagedIdentityTestHttpClientFactory>(
                _ => new TestManagedIdentityHttpFactory(mockHttp));
            _ = factory.Build();
            var options = new AcquireTokenOptions
            {
                ManagedIdentity = new ManagedIdentityOptions { UserAssignedClientId = Guid.NewGuid().ToString() },
            };
            AcquireTokenResult cached = await factory.GetTokenAcquirer().GetTokenForAppAsync(Scope, options);
            Assert.Equal("cached-plain-bearer", cached.AccessToken);
            options.RequestOverMtls = requestOverMtls;
            options.MinimumTokenBindingStrength = minimumStrength;
            options.ExtraParameters = new Dictionary<string, object> { ["IsTokenBinding"] = !requestOverMtls };

            // Act: no HTTP response remains; mTLS must not return the plain cached token.
            Exception? exception = await Record.ExceptionAsync(
                () => factory.GetTokenAcquirer().GetTokenForAppAsync(Scope, options));

            // Assert
            Assert.IsAssignableFrom<MsalException>(exception);
            Assert.DoesNotContain("requires key attestation", exception!.Message, StringComparison.Ordinal);
        }

        [Theory]
        [InlineData("attestation_failed")]
        [InlineData("min_strength_not_met")]
        public async Task KeyGuard_Failure_IsPropagatedWithoutRetry(string errorCode)
        {
            // Arrange
            var provider = new InspectingAttestationProvider(
                false, MtlsBindingStrength.KeyGuard, new MsalClientException(errorCode, "Required protection unavailable."));
            TokenAcquirerFactory factory = CreateFactory(provider);
            _ = factory.Build();

            // Act
            Exception? exception = await Record.ExceptionAsync(
                () => factory.GetTokenAcquirer().GetTokenForAppAsync(Scope, new AcquireTokenOptions
                {
                    ManagedIdentity = new ManagedIdentityOptions(),
                    MinimumTokenBindingStrength = MtlsBindingStrength.KeyGuard,
                    ExtraParameters = new Dictionary<string, object> { ["IsTokenBinding"] = true },
                }));

            // Assert
            AssertProviderFailure(exception, provider);
        }

        [Theory]
        [InlineData(true, MtlsBindingStrength.None)]
        [InlineData(false, MtlsBindingStrength.None)]
        [InlineData(false, MtlsBindingStrength.Software)]
        [InlineData(false, MtlsBindingStrength.KeyGuard)]
        public async Task Mtls_Execution_PartitionsByAttestationAndMinimumStrength(
            bool requestOverMtls, MtlsBindingStrength minimumStrength)
        {
            // Arrange
            var provider = new CapturingAttestationProvider();
            TokenAcquirerFactory factory = CreateFactory(provider);
            using var mockHttp = new MockHttpClientFactory();
            factory.Services.AddSingleton<IManagedIdentityTestHttpClientFactory>(
                _ => new TestManagedIdentityHttpFactory(mockHttp));
            _ = factory.Build();

            // Act: execution stamps MSAL's cache components before the mocked transport fails.
            Exception? exception = await Record.ExceptionAsync(
                () => factory.GetTokenAcquirer().GetTokenForAppAsync(Scope, new AcquireTokenOptions
                {
                    ManagedIdentity = new ManagedIdentityOptions(),
                    RequestOverMtls = requestOverMtls,
                    MinimumTokenBindingStrength = minimumStrength,
                    ExtraParameters = new Dictionary<string, object> { ["IsTokenBinding"] = !requestOverMtls },
                }));

            // Assert
            Assert.IsAssignableFrom<MsalException>(exception);
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Assert.Null(provider.Builder);
                Assert.Equal(MsalError.MtlsNotSupportedForManagedIdentity, ((MsalException)exception!).ErrorCode);
                return;
            }

            Assert.NotNull(provider.Builder);
            Assert.Equal(1, provider.Calls);
            object parameters = GetCommonParameters(provider.Builder);
            var components = Assert.IsAssignableFrom<IDictionary<string, Func<CancellationToken, Task<string>>>>(
                parameters.GetType().GetProperty("CacheKeyComponents")!.GetValue(parameters));
            Assert.Equal("1", await components[requestOverMtls ? "mtls_bearer" : "mi_att"](CancellationToken.None));
            if (minimumStrength == MtlsBindingStrength.None)
            {
                Assert.False(components.ContainsKey("mi_minstrength"));
            }
            else
            {
                Assert.Equal(minimumStrength.ToString(), await components["mi_minstrength"](CancellationToken.None));
                Assert.Equal(MsalError.MinStrengthNotMet, ((MsalException)exception!).ErrorCode);
            }
        }

        private static TokenAcquirerFactory CreateFactory(IKeyAttestationProvider? provider = null)
        {
            TokenAcquirerFactoryTesting.ResetTokenAcquirerFactoryInTest();
            TokenAcquirerFactory factory = TokenAcquirerFactory.GetDefaultInstance();
            if (provider is not null)
            {
                factory.Services.AddSingleton(provider);
            }

            return factory;
        }

        private static object GetCommonParameters(AcquireTokenForManagedIdentityParameterBuilder builder)
        {
            // Inspect the released MSAL builder without adding a product test hook or exposing its internal types.
            return typeof(AcquireTokenForManagedIdentityParameterBuilder)
                .GetProperty("CommonParameters", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(builder)!;
        }

        private static void ResetManagedIdentitySource()
        {
            // Capability probes cache process-wide results; isolate them from subsequent bearer acquisitions.
            typeof(ManagedIdentityApplication).Assembly
                .GetType("Microsoft.Identity.Client.ManagedIdentity.ManagedIdentityClient", throwOnError: true)!
                .GetMethod("ResetSourceForTest", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, null);
        }

        private static void AssertProviderFailure(Exception? exception, InspectingAttestationProvider provider)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Assert.Same(provider.Failure, exception);
                Assert.Equal(1, provider.Calls);
            }
            else
            {
                Assert.Equal(MsalError.MtlsNotSupportedForManagedIdentity, Assert.IsType<MsalClientException>(exception).ErrorCode);
                Assert.Equal(0, provider.Calls);
            }
        }

        private sealed class CapturingAttestationProvider : IKeyAttestationProvider
        {
            public AcquireTokenForManagedIdentityParameterBuilder? Builder { get; private set; }
            public int Calls { get; private set; }

            public AcquireTokenForManagedIdentityParameterBuilder EnableAttestation(AcquireTokenForManagedIdentityParameterBuilder builder)
            {
                Calls++;
                Builder = builder;
                return builder.WithAttestationSupport();
            }
        }

        private sealed class InspectingAttestationProvider : IKeyAttestationProvider
        {
            private readonly bool _requestOverMtls;
            private readonly MtlsBindingStrength _minimumStrength;

            public InspectingAttestationProvider(bool requestOverMtls, MtlsBindingStrength minimumStrength, Exception? failure = null)
            {
                _requestOverMtls = requestOverMtls;
                _minimumStrength = minimumStrength;
                Failure = failure ?? new InvalidOperationException(AttestationInvoked);
            }

            public Exception Failure { get; }
            public int Calls { get; private set; }

            public AcquireTokenForManagedIdentityParameterBuilder EnableAttestation(AcquireTokenForManagedIdentityParameterBuilder builder)
            {
                Calls++;
                object parameters = GetCommonParameters(builder);
                var actualStrength = Assert.IsType<MsalMtlsBindingStrength>(
                    parameters.GetType().GetProperty("MtlsPopMinStrength")!.GetValue(parameters));
                Assert.Equal((int)_minimumStrength, (int)actualStrength);
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
                throw Failure;
            }
        }
    }
}
