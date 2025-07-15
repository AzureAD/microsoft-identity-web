// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Abstractions;
using NSubstitute;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    public class ServiceCollectionExtensionForAzureCredsTests
    {
        [Fact]
        public void AddMicrosoftIdentityAzureTokenCredential_RegistersExpectedServices()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            var result = services.AddMicrosoftIdentityAzureTokenCredential();

            // Assert
            Assert.Same(services, result);
            var descriptor = services.FirstOrDefault(s => s.ServiceType == typeof(MicrosoftIdentityTokenCredential));
            Assert.NotNull(descriptor);
            Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
        }

        [Fact]
        public void AddMicrosoftIdentityAzureTokenCredential_CanBeResolved()
        {
            // Arrange
            var services = new ServiceCollection();
            
            // Mock the dependencies needed for MicrosoftIdentityTokenCredential
            services.AddScoped<ITokenAcquirerFactory>(sp => Substitute.For<ITokenAcquirerFactory>());
            services.AddScoped<IAuthenticationSchemeInformationProvider>(sp => Substitute.For<IAuthenticationSchemeInformationProvider>());
            
            // Act
            services.AddMicrosoftIdentityAzureTokenCredential();
            var serviceProvider = services.BuildServiceProvider();
            
            // Assert
            var tokenCredential = serviceProvider.GetService<MicrosoftIdentityTokenCredential>();
            Assert.NotNull(tokenCredential);
        }
    }
}
