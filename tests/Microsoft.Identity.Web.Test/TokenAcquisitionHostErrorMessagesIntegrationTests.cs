// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web.Hosts;
using Microsoft.Identity.Web.Test.Common;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    [Collection(nameof(TokenAcquirerFactorySingletonProtection))]
    public class TokenAcquisitionHostErrorMessagesIntegrationTests
    {
        [Fact]
        public void GetOptions_WhenMicrosoftIdentityApplicationOptionsNotConfigured_ThrowsSpecificError()
        {
            // Arrange - Create a service collection with minimal setup (no MicrosoftIdentityApplicationOptions configured)
            var services = new ServiceCollection();
            services.AddSingleton<IMergedOptionsStore>(new MergedOptionsStore());
            
            // Register the options mergers to enable the merging behavior
            services.AddSingleton<IPostConfigureOptions<MicrosoftIdentityOptions>, MicrosoftIdentityOptionsMerger>();
            services.AddSingleton<IPostConfigureOptions<MicrosoftIdentityApplicationOptions>, MicrosoftIdentityApplicationOptionsMerger>();
            services.AddSingleton<IPostConfigureOptions<ConfidentialClientApplicationOptions>, ConfidentialClientApplicationOptionsMerger>();
            
            services.Configure<MicrosoftIdentityOptions>("test", opt => { });
            services.Configure<ConfidentialClientApplicationOptions>("test", opt => { });
            // Note: We don't configure MicrosoftIdentityApplicationOptions to simulate the error condition

            var serviceProvider = services.BuildServiceProvider();
            
            var mergedOptionsMonitor = serviceProvider.GetRequiredService<IMergedOptionsStore>();
            var ccaOptionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<ConfidentialClientApplicationOptions>>();
            var microsoftIdentityOptionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<MicrosoftIdentityOptions>>();
            var microsoftIdentityApplicationOptionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<MicrosoftIdentityApplicationOptions>>();

            var host = new DefaultTokenAcquisitionHost(
                microsoftIdentityOptionsMonitor,
                mergedOptionsMonitor,
                ccaOptionsMonitor,
                microsoftIdentityApplicationOptionsMonitor);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => 
                host.GetOptions("test", out string effectiveScheme));

            // Debug output
            Console.WriteLine($"Actual error message: {exception.Message}");
            
            // This should now use the new error message
            Assert.Contains("MicrosoftIdentityApplicationOptions are not configured", exception.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void GetOptions_WhenMicrosoftIdentityApplicationOptionsConfiguredWithInstance_Succeeds()
        {
            // Arrange - Create a service collection with properly configured MicrosoftIdentityApplicationOptions
            var services = new ServiceCollection();
            services.AddSingleton<IMergedOptionsStore>(new MergedOptionsStore());
            
            // Register the options mergers to enable the merging behavior
            services.AddSingleton<IPostConfigureOptions<MicrosoftIdentityOptions>, MicrosoftIdentityOptionsMerger>();
            services.AddSingleton<IPostConfigureOptions<MicrosoftIdentityApplicationOptions>, MicrosoftIdentityApplicationOptionsMerger>();
            services.AddSingleton<IPostConfigureOptions<ConfidentialClientApplicationOptions>, ConfidentialClientApplicationOptionsMerger>();
            
            services.Configure<MicrosoftIdentityOptions>("test", opt => { });
            services.Configure<ConfidentialClientApplicationOptions>("test", opt => { });
            services.Configure<MicrosoftIdentityApplicationOptions>("test", opt => 
            {
                opt.Instance = "https://login.microsoftonline.com/";
                opt.ClientId = "test-client-id";
            });

            var serviceProvider = services.BuildServiceProvider();
            
            var mergedOptionsMonitor = serviceProvider.GetRequiredService<IMergedOptionsStore>();
            var ccaOptionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<ConfidentialClientApplicationOptions>>();
            var microsoftIdentityOptionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<MicrosoftIdentityOptions>>();
            var microsoftIdentityApplicationOptionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<MicrosoftIdentityApplicationOptions>>();

            var host = new DefaultTokenAcquisitionHost(
                microsoftIdentityOptionsMonitor,
                mergedOptionsMonitor,
                ccaOptionsMonitor,
                microsoftIdentityApplicationOptionsMonitor);

            // Act
            var result = host.GetOptions("test", out string effectiveScheme);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("https://login.microsoftonline.com/", result.Instance);
        }
    }
}