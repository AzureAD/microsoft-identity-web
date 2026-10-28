// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
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

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void AddMicrosoftIdentityMessageHandler_WithConfiguration_WithNullBuilder_ThrowsArgumentNullException(bool useLegacyOverload)
        {
            // Arrange
            IHttpClientBuilder? builder = null;
            var configuration = new ConfigurationBuilder().Build();

            // Act & Assert
#pragma warning disable CS0618 // Verify compatibility of the obsolete overload.
            var exception = Assert.Throws<ArgumentNullException>(() =>
                useLegacyOverload
                    ? builder!.AddMicrosoftIdentityMessageHandler(configuration, "TestSection")
                    : builder!.AddMicrosoftIdentityMessageHandler(configuration));
#pragma warning restore CS0618
            Assert.Equal("builder", exception.ParamName);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void AddMicrosoftIdentityMessageHandler_WithConfiguration_WithNullConfiguration_ThrowsArgumentNullException(bool useLegacyOverload)
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton(_mockHeaderProvider);
            var builder = services.AddHttpClient("TestClient");
            IConfiguration? configuration = null;

            // Act & Assert
#pragma warning disable CS0618 // Verify compatibility of the obsolete overload.
            var exception = Assert.Throws<ArgumentNullException>(() =>
                useLegacyOverload
                    ? builder.AddMicrosoftIdentityMessageHandler(configuration!, "TestSection")
                    : builder.AddMicrosoftIdentityMessageHandler(configuration!));
#pragma warning restore CS0618
            Assert.Equal("configuration", exception.ParamName);
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
#pragma warning disable CS0618 // Verify compatibility of the obsolete overload.
            var exception = Assert.Throws<ArgumentNullException>(() =>
                builder.AddMicrosoftIdentityMessageHandler(configuration, sectionName!));
#pragma warning restore CS0618
            Assert.Equal("sectionName", exception.ParamName);
        }

        [Theory]
        [InlineData("MyApi", false)]
        [InlineData("MyApi", true)]
        [InlineData("anything", false)]
        [InlineData("", false)]
        public async Task AddMicrosoftIdentityMessageHandler_WithConfiguration_RegistersHandlerWithBoundOptions(
            string sectionName,
            bool hasNamedChild)
        {
            // Arrange
            var configurationData = new Dictionary<string, string?>
            {
                { "DownstreamApis:Scopes:0", "wrong.scope" },
                { "DownstreamApis:MyApi:Scopes:0", "api.scope" }
            };

            if (hasNamedChild)
            {
                configurationData.Add("DownstreamApis:MyApi:MyApi:Scopes:0", "wrong.child.scope");
            }

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configurationData)
                .Build();

            // Act and Assert
            await AssertConfigurationBindsScopeAsync(
                configuration.GetSection("DownstreamApis:MyApi"),
                sectionName);
        }

        [Theory]
        [InlineData("")]
        [InlineData("DownstreamApis:MyApi")]
        public async Task AddMicrosoftIdentityMessageHandler_WithConfiguration_BindsConfigurationDirectly(string sectionPath)
        {
            // Arrange
            var configurationData = new Dictionary<string, string?>
            {
                { "Scopes:0", "api.scope" },
                { "DownstreamApis:MyApi:Scopes:0", "api.scope" },
                { "DownstreamApis:MyApi:MyApi:Scopes:0", "wrong.child.scope" }
            };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configurationData)
                .Build();

            // Act and Assert
            await AssertConfigurationBindsScopeAsync(
                string.IsNullOrEmpty(sectionPath) ? configuration : configuration.GetRequiredSection(sectionPath),
                sectionName: null);
        }

        [Fact]
        public async Task AddMicrosoftIdentityMessageHandler_WithConfiguration_DoesNotSelectChildImplicitly()
        {
            // Arrange
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "DownstreamApis:MyApi:Scopes:0", "api.scope" }
                })
                .Build();

            // Act and Assert
            await Assert.ThrowsAsync<MicrosoftIdentityAuthenticationException>(() =>
                AssertConfigurationBindsScopeAsync(configuration.GetSection("DownstreamApis"), sectionName: null));
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
#pragma warning disable CS0618 // Verify compatibility of the obsolete overload.
            builder.AddMicrosoftIdentityMessageHandler(
                configuration.GetSection("NonExistent"),
                "NonExistent");
#pragma warning restore CS0618

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

        private async Task AssertConfigurationBindsScopeAsync(
            IConfiguration configuration,
            string? sectionName)
        {
            var services = new ServiceCollection();
            var headerProvider = Substitute.For<IAuthorizationHeaderProvider>();
            headerProvider.CreateAuthorizationHeaderAsync(
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<AuthorizationHeaderProviderOptions?>(),
                Arg.Any<ClaimsPrincipal?>(),
                Arg.Any<CancellationToken>())
                .Returns(Task.FromResult("placeholder"));
            services.AddSingleton(headerProvider);

            var builder = services.AddHttpClient("TestClient")
                .ConfigurePrimaryHttpMessageHandler(() => new TestHttpMessageHandler());

            if (sectionName is null)
            {
                builder.AddMicrosoftIdentityMessageHandler(configuration);
            }
            else
            {
#pragma warning disable CS0618 // Verify compatibility of the obsolete overload.
                builder.AddMicrosoftIdentityMessageHandler(configuration, sectionName);
#pragma warning restore CS0618
            }

            using var serviceProvider = services.BuildServiceProvider();
            var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
            using var client = httpClientFactory.CreateClient("TestClient");

            using var response = await client.GetAsync("https://example.com");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            await headerProvider.Received(1).CreateAuthorizationHeaderAsync(
                Arg.Is<IEnumerable<string>>(scopes => scopes.SequenceEqual(new[] { "api.scope" })),
                Arg.Any<AuthorizationHeaderProviderOptions?>(),
                Arg.Any<ClaimsPrincipal?>(),
                Arg.Any<CancellationToken>());
        }

        // Test helper class
        private class TestDelegatingHandler : DelegatingHandler
        {
        }

        private class TestHttpMessageHandler : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
            }
        }
    }
}
