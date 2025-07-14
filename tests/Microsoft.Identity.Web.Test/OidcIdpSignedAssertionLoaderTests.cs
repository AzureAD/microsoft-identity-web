// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
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
            _logger = Substitute.For<ILogger<OidcIdpSignedAssertionLoader>>();
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
            _options.Authority = "//v2.0";
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
            _options.Authority = "//v2.0";
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
    }
}