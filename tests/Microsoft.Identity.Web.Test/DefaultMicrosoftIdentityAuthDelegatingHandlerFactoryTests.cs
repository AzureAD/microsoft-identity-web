// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Web.TokenCacheProviders.InMemory;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    public class DefaultMicrosoftIdentityAuthDelegatingHandlerFactoryTests
    {
        private IServiceProvider InitializeServiceCollection()
        {
            var services = new ServiceCollection();
            services.AddTokenAcquisition();
            services.AddInMemoryTokenCaches();
            services.AddHttpClient();
            return services.BuildServiceProvider();
        }

            [Fact]
        public void CreateAppHandler_Should_Return_MicrosoftIdentityAppAuthenticationMessageHandler()
        {
            // Arrange
            var factory = new DefaultMicrosoftIdentityAuthenticationDelegatingHandlerFactory(InitializeServiceCollection());
            string serviceName = "test-service";

            // Act
            DelegatingHandler handler = factory.CreateAppHandler(serviceName);

            // Assert
            Assert.IsType<MicrosoftIdentityAppAuthenticationMessageHandler>(handler);
        }

        [Fact]
        public void CreateUserHandler_Should_Return_MicrosoftIdentityUserAuthenticationMessageHandler()
        {
            // Arrange
            var factory = new DefaultMicrosoftIdentityAuthenticationDelegatingHandlerFactory(InitializeServiceCollection());
            string serviceName = "test-service";

            // Act
            DelegatingHandler handler = factory.CreateUserHandler(serviceName);

            // Assert
            Assert.IsType<MicrosoftIdentityUserAuthenticationMessageHandler>(handler);
        }
    }
}
