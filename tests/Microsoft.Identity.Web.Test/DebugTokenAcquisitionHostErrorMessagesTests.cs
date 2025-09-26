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
    public class DebugTokenAcquisitionHostErrorMessagesTests
    {
        [Fact]
        public void Debug_GetOptions_WhenMicrosoftIdentityApplicationOptionsNotConfigured_PrintsActualError()
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

            // Print the actual error message to debug
            Console.WriteLine($"Actual error message: {exception.Message}");
            Assert.False(string.IsNullOrEmpty(exception.Message));
        }
    }
}