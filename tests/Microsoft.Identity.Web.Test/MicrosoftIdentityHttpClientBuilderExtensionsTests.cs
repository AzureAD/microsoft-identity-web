// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Abstractions;
using NSubstitute;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    public class MicrosoftIdentityHttpClientBuilderExtensionsTests
    {
        private readonly IAuthorizationHeaderProvider _mockHeaderProvider;

        public MicrosoftIdentityHttpClientBuilderExtensionsTests()
        {
            _mockHeaderProvider = Substitute.For<IAuthorizationHeaderProvider>();
        }

        [Fact]
        public void AddMicrosoftIdentityMessageHandler_Parameterless_WithNullBuilder_ThrowsArgumentNullException()
        {
            // Arrange
            IHttpClientBuilder? builder = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => builder!.AddMicrosoftIdentityMessageHandler());
        }

        [Fact]
        public void AddMicrosoftIdentityMessageHandler_Parameterless_RegistersHandler()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton(_mockHeaderProvider);
            var builder = services.AddHttpClient("TestClient");

            // Act
            builder.AddMicrosoftIdentityMessageHandler();

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
            var client = httpClientFactory.CreateClient("TestClient");
            
            Assert.NotNull(client);
        }

        [Fact]
        public void AddMicrosoftIdentityMessageHandler_WithOptions_WithNullBuilder_ThrowsArgumentNullException()
        {
            // Arrange
            IHttpClientBuilder? builder = null;
            var options = new MicrosoftIdentityMessageHandlerOptions
            {
                Scopes = { "test.scope" }
            };

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => builder!.AddMicrosoftIdentityMessageHandler(options));
        }

        [Fact]
        public void AddMicrosoftIdentityMessageHandler_WithOptions_WithNullOptions_ThrowsArgumentNullException()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton(_mockHeaderProvider);
            var builder = services.AddHttpClient("TestClient");
            MicrosoftIdentityMessageHandlerOptions? options = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => builder.AddMicrosoftIdentityMessageHandler(options!));
        }

        [Fact]
        public void AddMicrosoftIdentityMessageHandler_WithOptions_RegistersHandlerWithOptions()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton(_mockHeaderProvider);
            var builder = services.AddHttpClient("TestClient");
            var options = new MicrosoftIdentityMessageHandlerOptions
            {
                Scopes = { "https://graph.microsoft.com/.default" }
            };

            // Act
            builder.AddMicrosoftIdentityMessageHandler(options);

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
            var client = httpClientFactory.CreateClient("TestClient");
            
            Assert.NotNull(client);
        }

        [Fact]
        public void AddMicrosoftIdentityMessageHandler_WithDelegate_WithNullBuilder_ThrowsArgumentNullException()
        {
            // Arrange
            IHttpClientBuilder? builder = null;
            Action<MicrosoftIdentityMessageHandlerOptions> configureOptions = options =>
            {
                options.Scopes.Add("test.scope");
            };

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => builder!.AddMicrosoftIdentityMessageHandler(configureOptions));
        }

        [Fact]
        public void AddMicrosoftIdentityMessageHandler_WithDelegate_WithNullDelegate_ThrowsArgumentNullException()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton(_mockHeaderProvider);
            var builder = services.AddHttpClient("TestClient");
            Action<MicrosoftIdentityMessageHandlerOptions>? configureOptions = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => builder.AddMicrosoftIdentityMessageHandler(configureOptions!));
        }

        [Fact]
        public void AddMicrosoftIdentityMessageHandler_WithDelegate_RegistersHandlerWithConfiguredOptions()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton(_mockHeaderProvider);
            var builder = services.AddHttpClient("TestClient");

            // Act
            builder.AddMicrosoftIdentityMessageHandler(options =>
            {
                options.Scopes.Add("https://api.example.com/.default");
                options.RequestAppToken = true;
            });

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
            var client = httpClientFactory.CreateClient("TestClient");
            
            Assert.NotNull(client);
        }

        [Fact]
        public void AddMicrosoftIdentityMessageHandler_WithConfiguration_WithNullBuilder_ThrowsArgumentNullException()
        {
            // Arrange
            IHttpClientBuilder? builder = null;
            var configuration = new ConfigurationBuilder().Build();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                builder!.AddMicrosoftIdentityMessageHandler(configuration, "TestSection"));
        }

        [Fact]
        public void AddMicrosoftIdentityMessageHandler_WithConfiguration_WithNullConfiguration_ThrowsArgumentNullException()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton(_mockHeaderProvider);
            var builder = services.AddHttpClient("TestClient");
            IConfiguration? configuration = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                builder.AddMicrosoftIdentityMessageHandler(configuration!, "TestSection"));
        }

        [Fact]
        public void AddMicrosoftIdentityMessageHandler_WithConfiguration_WithNullSectionName_ThrowsArgumentNullException()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton(_mockHeaderProvider);
            var builder = services.AddHttpClient("TestClient");
            var configuration = new ConfigurationBuilder().Build();
            string? sectionName = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                builder.AddMicrosoftIdentityMessageHandler(configuration, sectionName!));
        }

        [Fact]
        public void AddMicrosoftIdentityMessageHandler_WithConfiguration_RegistersHandlerWithBoundOptions()
        {
            // Arrange
            var configurationData = new Dictionary<string, string?>
            {
                { "DownstreamApi:Scopes:0", "https://graph.microsoft.com/.default" },
                { "DownstreamApi:Scopes:1", "User.Read" }
            };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configurationData)
                .Build();

            var services = new ServiceCollection();
            services.AddSingleton(_mockHeaderProvider);
            var builder = services.AddHttpClient("TestClient");

            // Act
            builder.AddMicrosoftIdentityMessageHandler(
                configuration.GetSection("DownstreamApi"),
                "DownstreamApi");

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
            var client = httpClientFactory.CreateClient("TestClient");
            
            Assert.NotNull(client);
        }

        [Fact]
        public void AddMicrosoftIdentityMessageHandler_ServiceResolution_ResolvesAuthorizationHeaderProvider()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton(_mockHeaderProvider);
            var builder = services.AddHttpClient("TestClient");

            // Act
            builder.AddMicrosoftIdentityMessageHandler(options =>
            {
                options.Scopes.Add("test.scope");
            });

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            
            // Verify that IAuthorizationHeaderProvider can be resolved
            var headerProvider = serviceProvider.GetRequiredService<IAuthorizationHeaderProvider>();
            Assert.NotNull(headerProvider);
            Assert.Same(_mockHeaderProvider, headerProvider);
        }

        [Fact]
        public void AddMicrosoftIdentityMessageHandler_MultipleHandlers_CanBeChained()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton(_mockHeaderProvider);
            var builder = services.AddHttpClient("TestClient");

            // Act - Add multiple handlers to the pipeline
            builder
                .AddMicrosoftIdentityMessageHandler(options =>
                {
                    options.Scopes.Add("test.scope");
                })
                .AddHttpMessageHandler(() => new TestDelegatingHandler());

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
            var client = httpClientFactory.CreateClient("TestClient");
            
            Assert.NotNull(client);
        }

        [Fact]
        public void AddMicrosoftIdentityMessageHandler_ReturnsBuilder_AllowsChaining()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton(_mockHeaderProvider);
            var builder = services.AddHttpClient("TestClient");

            // Act
            var result = builder.AddMicrosoftIdentityMessageHandler();

            // Assert
            Assert.NotNull(result);
            Assert.Same(builder, result);
        }

        [Fact]
        public void AddMicrosoftIdentityMessageHandler_WithConfiguration_EmptyConfiguration_RegistersHandlerWithEmptyOptions()
        {
            // Arrange
            var configuration = new ConfigurationBuilder().Build();
            var services = new ServiceCollection();
            services.AddSingleton(_mockHeaderProvider);
            var builder = services.AddHttpClient("TestClient");

            // Act
            builder.AddMicrosoftIdentityMessageHandler(
                configuration.GetSection("NonExistent"),
                "NonExistent");

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
            var client = httpClientFactory.CreateClient("TestClient");
            
            Assert.NotNull(client);
        }

        [Fact]
        public void AddMicrosoftIdentityMessageHandler_MultipleClients_CanHaveDifferentConfigurations()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton(_mockHeaderProvider);

            // Act - Configure multiple clients with different options
            services.AddHttpClient("Client1")
                .AddMicrosoftIdentityMessageHandler(options =>
                {
                    options.Scopes.Add("scope1");
                });

            services.AddHttpClient("Client2")
                .AddMicrosoftIdentityMessageHandler(options =>
                {
                    options.Scopes.Add("scope2");
                    options.RequestAppToken = true;
                });

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
            
            var client1 = httpClientFactory.CreateClient("Client1");
            var client2 = httpClientFactory.CreateClient("Client2");
            
            Assert.NotNull(client1);
            Assert.NotNull(client2);
        }

        // Test helper class
        private class TestDelegatingHandler : DelegatingHandler
        {
        }
    }
}
