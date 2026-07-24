// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web.OidcFic;
using NSubstitute;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    public class OidcIdpSignedAssertionLoaderTests
    {
        private readonly ILogger<OidcIdpSignedAssertionLoader> _logger;
        private readonly IOptionsMonitor<MicrosoftIdentityApplicationOptions> _optionsMonitor;
        private readonly IServiceProvider _serviceProvider;
        private readonly ITokenAcquirerFactory _tokenAcquirerFactory;
        private readonly MicrosoftIdentityApplicationOptions _options;

        public OidcIdpSignedAssertionLoaderTests()
        {
            _logger = new LoggerFactory().CreateLogger<OidcIdpSignedAssertionLoader>();
            _optionsMonitor = Substitute.For<IOptionsMonitor<MicrosoftIdentityApplicationOptions>>();
            _serviceProvider = Substitute.For<IServiceProvider>();
            _tokenAcquirerFactory = Substitute.For<ITokenAcquirerFactory>();
            _options = new MicrosoftIdentityApplicationOptions();
        }

        [Fact]
        public async Task LoadIfNeededAsync_ConfigurationIsNull_ThrowsInvalidOperationException()
        {
            // Arrange
            var loader = new OidcIdpSignedAssertionLoader(_logger, _optionsMonitor, _serviceProvider, _tokenAcquirerFactory);

            var credentialDescription = new CredentialDescription
            {
                CustomSignedAssertionProviderData = new Dictionary<string, object>
                {
                    ["ConfigurationSection"] = "TestSection"
                }
            };

            // Configure options to trigger configuration binding
            _options.Instance = null;
            _optionsMonitor.Get("TestSection").Returns(_options);

            // Configure service provider to return null for IConfiguration
            _serviceProvider.GetService(typeof(IConfiguration)).Returns((IConfiguration?)null);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => loader.LoadIfNeededAsync(credentialDescription));

            Assert.Contains("IConfiguration is not registered in the service collection", exception.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("https://aka.ms/ms-id-web/fic-oidc/troubleshoot", exception.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task LoadIfNeededAsync_ConfigurationIsAvailable_SucceedsWithConfigurationBinding()
        {
            // Arrange
            var loader = new OidcIdpSignedAssertionLoader(_logger, _optionsMonitor, _serviceProvider, _tokenAcquirerFactory);

            var credentialDescription = new CredentialDescription
            {
                CustomSignedAssertionProviderData = new Dictionary<string, object>
                {
                    ["ConfigurationSection"] = "TestSection"
                },
                TokenExchangeUrl = "https://test.com/token"
            };

            // Configure options to trigger configuration binding
            _options.Instance = null;
            _optionsMonitor.Get("TestSection").Returns(_options);

            // Configure service provider to return a mock IConfiguration
            var configuration = Substitute.For<IConfiguration>();
            var configurationSection = Substitute.For<IConfigurationSection>();
            configuration.GetSection("TestSection").Returns(configurationSection);
            _serviceProvider.GetService(typeof(IConfiguration)).Returns(configuration);

            // Act
            // This should not throw an exception
            try
            {
                await loader.LoadIfNeededAsync(credentialDescription);
            }
            catch (Exception ex)
            {
                // We expect other exceptions (like issues with token acquisition), but NOT the configuration exception
                Assert.DoesNotContain("IConfiguration is not registered", ex.Message, StringComparison.OrdinalIgnoreCase);
            }

            // Assert
            // Verify that configuration binding was called
            configuration.Received(1).GetSection("TestSection");
        }

        [Fact]
        public async Task LoadIfNeededAsync_ConfigurationNotNeeded_SucceedsWithoutConfigurationBinding()
        {
            // Arrange
            var loader = new OidcIdpSignedAssertionLoader(_logger, _optionsMonitor, _serviceProvider, _tokenAcquirerFactory);

            var credentialDescription = new CredentialDescription
            {
                CustomSignedAssertionProviderData = new Dictionary<string, object>
                {
                    ["ConfigurationSection"] = "TestSection"
                },
                TokenExchangeUrl = "https://test.com/token"
            };

            // Configure options to NOT trigger configuration binding
            _options.Instance = "https://login.microsoftonline.com/";
            _options.Authority = "https://login.microsoftonline.com/tenantid/v2.0";
            _optionsMonitor.Get("TestSection").Returns(_options);

            // Configure service provider to return null for IConfiguration - this should not be called
            _serviceProvider.GetService(typeof(IConfiguration)).Returns((IConfiguration?)null);

            // Act
            // This should not throw an exception about configuration
            try
            {
                await loader.LoadIfNeededAsync(credentialDescription);
            }
            catch (Exception ex)
            {
                // We expect other exceptions (like issues with token acquisition), but NOT the configuration exception
                Assert.DoesNotContain("IConfiguration is not registered", ex.Message, StringComparison.OrdinalIgnoreCase);
            }

            // Assert
            // Verify that GetService<IConfiguration> was not called
            _serviceProvider.DidNotReceive().GetService(typeof(IConfiguration));
        }

        [Fact]
        public async Task LoadIfNeededAsync_CustomSignedAssertionProviderDataIsNull_ThrowsInvalidOperationException()
        {
            // Arrange
            var loader = new OidcIdpSignedAssertionLoader(_logger, _optionsMonitor, _serviceProvider, _tokenAcquirerFactory);

            var credentialDescription = new CredentialDescription
            {
                CustomSignedAssertionProviderData = null!
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => loader.LoadIfNeededAsync(credentialDescription));

            Assert.Equal("CustomSignedAssertionProviderData is null", exception.Message);
        }

        [Fact]
        public async Task LoadIfNeededAsync_ConfigurationSectionIsNull_ThrowsInvalidOperationException()
        {
            // Arrange
            var loader = new OidcIdpSignedAssertionLoader(_logger, _optionsMonitor, _serviceProvider, _tokenAcquirerFactory);

            var credentialDescription = new CredentialDescription
            {
                CustomSignedAssertionProviderData = new Dictionary<string, object>
                {
                    ["ConfigurationSection"] = null!
                }
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => loader.LoadIfNeededAsync(credentialDescription));

            Assert.Equal("ConfigurationSection is null", exception.Message);
        }

        #region Token-binding (bound signed assertion) loader tests

        private OidcIdpSignedAssertionLoader CreateLoader() =>
            new OidcIdpSignedAssertionLoader(_logger, _optionsMonitor, _serviceProvider, _tokenAcquirerFactory);

        private ITokenAcquirer SetupInnerAcquirer(string accessToken = "inner-assertion")
        {
            var acquirer = Substitute.For<ITokenAcquirer>();
            acquirer.GetTokenForAppAsync(
                    Arg.Any<string>(),
                    Arg.Any<AcquireTokenOptions>(),
                    Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(new AcquireTokenResult(
                    accessToken,
                    DateTimeOffset.UtcNow.AddHours(1),
                    "inner-tenant",
                    "inner-id-token",
                    new[] { "api://AzureADTokenExchange/.default" },
                    Guid.NewGuid(),
                    "Bearer")));
            _tokenAcquirerFactory.GetTokenAcquirer(Arg.Any<IdentityApplicationOptions>()).Returns(acquirer);
            return acquirer;
        }

        private static CredentialDescription CreateOidcCredential(bool useBoundCredential = false)
        {
            return new CredentialDescription
            {
                SourceType = CredentialSource.CustomSignedAssertion,
                CustomSignedAssertionProviderName = "OidcIdpSignedAssertion",
                UseBoundCredential = useBoundCredential,
                CustomSignedAssertionProviderData = new Dictionary<string, object>
                {
                    ["ConfigurationSection"] = "TestSection"
                }
            };
        }

        [Fact]
        public async Task LoadIfNeededAsync_NormalBearer_ConstructsAndCachesProvider_AndPerformsWarmup()
        {
            // Arrange
            _options.Instance = "https://login.microsoftonline.com/";
            _optionsMonitor.Get("TestSection").Returns(_options);
            var acquirer = SetupInnerAcquirer();
            var loader = CreateLoader();
            var credentialDescription = CreateOidcCredential();

            // Act
            await loader.LoadIfNeededAsync(credentialDescription);

            // Assert — provider constructed, cached, and reports token-binding capability.
            var provider = Assert.IsType<OidcIdpSignedAssertionProvider>(credentialDescription.CachedValue);
            Assert.True(provider.SupportsTokenBinding);
            Assert.False(credentialDescription.Skip);

            // Normal unbound Bearer loading preserves the existing warm-up validation behavior.
            await acquirer.Received().GetTokenForAppAsync(
                Arg.Any<string>(), Arg.Any<AcquireTokenOptions>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task LoadIfNeededAsync_MtlsPopProtocol_DoesNotPerformBearerAcquisition()
        {
            // Arrange
            _options.Instance = "https://login.microsoftonline.com/";
            _optionsMonitor.Get("TestSection").Returns(_options);
            var acquirer = SetupInnerAcquirer();
            var loader = CreateLoader();
            var credentialDescription = CreateOidcCredential();
            var parameters = new CredentialSourceLoaderParameters("c2", "https://login.microsoftonline.com/t2")
            {
                Protocol = "MTLS_POP"
            };

            // Act
            await loader.LoadIfNeededAsync(credentialDescription, parameters);

            // Assert — provider cached and binding-capable, but no Bearer warm-up acquisition happened.
            var provider = Assert.IsType<OidcIdpSignedAssertionProvider>(credentialDescription.CachedValue);
            Assert.True(provider.SupportsTokenBinding);
            Assert.False(credentialDescription.Skip);
            await acquirer.DidNotReceive().GetTokenForAppAsync(
                Arg.Any<string>(), Arg.Any<AcquireTokenOptions>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task LoadIfNeededAsync_UseBoundCredential_DoesNotPerformBearerAcquisition()
        {
            // Arrange
            _options.Instance = "https://login.microsoftonline.com/";
            _optionsMonitor.Get("TestSection").Returns(_options);
            var acquirer = SetupInnerAcquirer();
            var loader = CreateLoader();
            var credentialDescription = CreateOidcCredential(useBoundCredential: true);

            // Act
            await loader.LoadIfNeededAsync(credentialDescription);

            // Assert
            var provider = Assert.IsType<OidcIdpSignedAssertionProvider>(credentialDescription.CachedValue);
            Assert.True(provider.SupportsTokenBinding);
            Assert.False(credentialDescription.Skip);
            await acquirer.DidNotReceive().GetTokenForAppAsync(
                Arg.Any<string>(), Arg.Any<AcquireTokenOptions>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task LoadIfNeededAsync_UseBoundCredential_DoesNotRequireSecondCertificate_NorModifyInnerSkip()
        {
            // Arrange — inner named application configured with a single certificate credential.
            var innerCertificate = new CredentialDescription
            {
                SourceType = CredentialSource.StoreWithDistinguishedName,
                CertificateStorePath = "CurrentUser/My",
                CertificateDistinguishedName = "CN=OidcFicInner"
            };
            _options.Instance = "https://login.microsoftonline.com/";
            _options.ClientCredentials = new[] { innerCertificate };
            _optionsMonitor.Get("TestSection").Returns(_options);
            SetupInnerAcquirer();
            var loader = CreateLoader();
            var credentialDescription = CreateOidcCredential(useBoundCredential: true);

            // Act
            await loader.LoadIfNeededAsync(credentialDescription);

            // Assert — the loader neither added a second (binding) credential nor marked the inner one skipped.
            Assert.Single(_options.ClientCredentials);
            Assert.False(innerCertificate.Skip);
            Assert.False(credentialDescription.Skip);
        }

        #endregion
    }
}
