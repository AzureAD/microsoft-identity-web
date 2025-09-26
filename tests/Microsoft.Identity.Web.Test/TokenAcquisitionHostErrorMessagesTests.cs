// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web.Hosts;
using Microsoft.Identity.Web.Test.Common;
using NSubstitute;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    [Collection(nameof(TokenAcquirerFactorySingletonProtection))]
    public class TokenAcquisitionHostErrorMessagesTests
    {
        [Fact]
        public void GetOptions_WhenMicrosoftIdentityApplicationOptionsNotConfigured_ThrowsSpecificError()
        {
            // Arrange
            var mergedOptionsMonitor = Substitute.For<IMergedOptionsStore>();
            var ccaOptionsMonitor = Substitute.For<IOptionsMonitor<ConfidentialClientApplicationOptions>>();
            var microsoftIdentityOptionsMonitor = Substitute.For<IOptionsMonitor<MicrosoftIdentityOptions>>();
            var microsoftIdentityApplicationOptionsMonitor = Substitute.For<IOptionsMonitor<MicrosoftIdentityApplicationOptions>>();

            // Configure merged options to have empty Instance (this is the condition that triggers the error)
            var emptyMergedOptions = new MergedOptions();
            mergedOptionsMonitor.Get(Arg.Any<string>()).Returns(emptyMergedOptions);

            // Configure MicrosoftIdentityApplicationOptions to be empty (not configured)
            // Note: The default Authority value is "//v2.0" which should be ignored
            var emptyAppOptions = new MicrosoftIdentityApplicationOptions();
            microsoftIdentityApplicationOptionsMonitor.Get(Arg.Any<string>()).Returns(emptyAppOptions);

            var host = new DefaultTokenAcquisitionHost(
                microsoftIdentityOptionsMonitor,
                mergedOptionsMonitor,
                ccaOptionsMonitor,
                microsoftIdentityApplicationOptionsMonitor);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => 
                host.GetOptions("testScheme", out string effectiveScheme));

            Assert.Contains("MicrosoftIdentityApplicationOptions are not configured", exception.Message, StringComparison.Ordinal);
            Assert.Contains("testScheme", exception.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void GetOptions_WhenMicrosoftIdentityApplicationOptionsConfiguredButInstanceEmpty_ThrowsSchemeError()
        {
            // Arrange
            var mergedOptionsMonitor = Substitute.For<IMergedOptionsStore>();
            var ccaOptionsMonitor = Substitute.For<IOptionsMonitor<ConfidentialClientApplicationOptions>>();
            var microsoftIdentityOptionsMonitor = Substitute.For<IOptionsMonitor<MicrosoftIdentityOptions>>();
            var microsoftIdentityApplicationOptionsMonitor = Substitute.For<IOptionsMonitor<MicrosoftIdentityApplicationOptions>>();

            // Configure merged options to have empty Instance
            var emptyMergedOptions = new MergedOptions();
            mergedOptionsMonitor.Get(Arg.Any<string>()).Returns(emptyMergedOptions);

            // Configure MicrosoftIdentityApplicationOptions to have ClientId configured (indicating it is configured)
            var configuredAppOptions = new MicrosoftIdentityApplicationOptions
            {
                ClientId = "test-client-id"
            };
            microsoftIdentityApplicationOptionsMonitor.Get(Arg.Any<string>()).Returns(configuredAppOptions);

            var host = new DefaultTokenAcquisitionHost(
                microsoftIdentityOptionsMonitor,
                mergedOptionsMonitor,
                ccaOptionsMonitor,
                microsoftIdentityApplicationOptionsMonitor);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => 
                host.GetOptions("testScheme", out string effectiveScheme));

            Assert.Contains("Cannot determine the cloud Instance", exception.Message, StringComparison.Ordinal);
            Assert.Contains("authentication scheme", exception.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void GetOptions_WhenMicrosoftIdentityApplicationOptionsHasInstance_DoesNotThrow()
        {
            // Arrange
            var mergedOptionsMonitor = Substitute.For<IMergedOptionsStore>();
            var ccaOptionsMonitor = Substitute.For<IOptionsMonitor<ConfidentialClientApplicationOptions>>();
            var microsoftIdentityOptionsMonitor = Substitute.For<IOptionsMonitor<MicrosoftIdentityOptions>>();
            var microsoftIdentityApplicationOptionsMonitor = Substitute.For<IOptionsMonitor<MicrosoftIdentityApplicationOptions>>();

            // Configure merged options to have Instance populated (normal working case)
            var configuredMergedOptions = new MergedOptions
            {
                Instance = "https://login.microsoftonline.com/"
            };
            mergedOptionsMonitor.Get(Arg.Any<string>()).Returns(configuredMergedOptions);

            var host = new DefaultTokenAcquisitionHost(
                microsoftIdentityOptionsMonitor,
                mergedOptionsMonitor,
                ccaOptionsMonitor,
                microsoftIdentityApplicationOptionsMonitor);

            // Act
            var result = host.GetOptions("testScheme", out string effectiveScheme);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("https://login.microsoftonline.com/", result.Instance);
        }
    }
}