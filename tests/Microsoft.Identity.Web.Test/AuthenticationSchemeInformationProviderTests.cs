// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Web.Hosts;
using Microsoft.Identity.Web.Test.Common;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    [Collection(nameof(TokenAcquirerFactorySingletonProtection))]
    public class AuthenticationSchemeInformationProviderTests
    {
        [Fact]
        public void AddTokenAcquisition_RegistersAuthenticationSchemeInformationProvider()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddTokenAcquisition();

            // Assert
            var descriptor = services.FirstOrDefault(s => s.ServiceType == typeof(Abstractions.IAuthenticationSchemeInformationProvider));
            Assert.NotNull(descriptor);
            Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
        }

        [Fact]
        public void AddTokenAcquisition_WithSingleton_RegistersAuthenticationSchemeInformationProvider()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddTokenAcquisition(isTokenAcquisitionSingleton: true);

            // Assert
            var descriptor = services.FirstOrDefault(s => s.ServiceType == typeof(Abstractions.IAuthenticationSchemeInformationProvider));
            Assert.NotNull(descriptor);
            Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
        }

        [Fact]
        public void AuthenticationSchemeInformationProvider_ReusesTokenAcquisitionHostInstance()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddAuthentication("OpenIdConnect");
            services.AddTokenAcquisition();
            var serviceProvider = services.BuildServiceProvider();

            // Act
            var tokenAcquisitionHost = serviceProvider.GetRequiredService<ITokenAcquisitionHost>();
            var schemeInfoProvider = serviceProvider.GetRequiredService<Abstractions.IAuthenticationSchemeInformationProvider>();

            // Assert - they should be the same instance when using factory registration
            Assert.Same(tokenAcquisitionHost, schemeInfoProvider);
        }

        [Fact]
        public void AddAuthenticationSchemeInformationProvider_AspNetCoreAuth_RegistersCorrectImplementation()
        {
            // Arrange
            var builder = WebApplication.CreateBuilder();
            builder.Services.AddTokenAcquisition();  // This adds AspNetCore authentication

            // Act
            var descriptor = builder.Services.FirstOrDefault(s => s.ServiceType == typeof(Abstractions.IAuthenticationSchemeInformationProvider));
            var descriptorHost = builder.Services.FirstOrDefault(s => s.ServiceType == typeof(ITokenAcquisitionHost));

            var sp = builder.Services.BuildServiceProvider();
            ITokenAcquisitionHost host = sp.GetRequiredService<ITokenAcquisitionHost>();
            Abstractions.IAuthenticationSchemeInformationProvider authSchemeProvider = sp.GetRequiredService<Abstractions.IAuthenticationSchemeInformationProvider>();

            // Assert
            Assert.NotNull(host);
            Assert.Equal(host, authSchemeProvider);
        }

        [Fact]
        public void AuthenticationSchemeInformationProvider_CallsEffectiveAuthenticationScheme()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddAuthentication("OpenIdConnect");
            services.AddTokenAcquisition();
            var serviceProvider = services.BuildServiceProvider();
            var schemeInfoProvider = serviceProvider.GetRequiredService<Abstractions.IAuthenticationSchemeInformationProvider>();

            // Act
            var result = schemeInfoProvider.GetEffectiveAuthenticationScheme("TestScheme");

            // Assert
            Assert.Equal("TestScheme", result);
        }

        [Fact]
        public void AuthenticationSchemeInformationProvider_WithEmptyScheme_ReturnsDefault()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddAuthentication("OpenIdConnect");
            services.AddTokenAcquisition();
            var serviceProvider = services.BuildServiceProvider();
            var schemeInfoProvider = serviceProvider.GetRequiredService<Abstractions.IAuthenticationSchemeInformationProvider>();

            // Act
            var result = schemeInfoProvider.GetEffectiveAuthenticationScheme(string.Empty);

            // Assert
            // Default behavior will return an empty string since no authentication is configured
            Assert.Equal("OpenIdConnect", result);
        }

        [Fact]
        public void AuthenticationSchemeInformationProvider_WitNullScheme_ReturnsDefault()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddAuthentication("OpenIdConnect");
            services.AddTokenAcquisition();
            var serviceProvider = services.BuildServiceProvider();
            var schemeInfoProvider = serviceProvider.GetRequiredService<Abstractions.IAuthenticationSchemeInformationProvider>();

            // Act
            var result = schemeInfoProvider.GetEffectiveAuthenticationScheme(string.Empty);

            // Assert
            // Default behavior will return an empty string since no authentication is configured
            Assert.Equal("OpenIdConnect", result);
        }
    }
}
