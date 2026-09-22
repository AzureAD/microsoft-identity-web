// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Net.Http;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Web.Test.Common;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    public class HttpClientBuilderExtensionsTests
    {
        private const string HttpClientName = "test-client";

        private readonly IConfigurationSection _configSection;

        public HttpClientBuilderExtensionsTests()
        {
            _configSection = GetConfigSection();
        }

        private IConfigurationSection GetConfigSection()
        {
            var key = "test-service";

            var builder = new ConfigurationBuilder();
            builder.AddInMemoryCollection(
                new Dictionary<string, string?>()
                {
                    { $"{key}:Scopes", TestConstants.Scopes },
                    { $"{key}:Tenant", TestConstants.TenantIdAsGuid },
                    { $"{key}:UserFlow", TestConstants.B2CSignUpSignInUserFlow },
                    { $"{key}:IsProofOfPossessionRequest", "false" },
                    { $"{key}:AuthenticationScheme", JwtBearerDefaults.AuthenticationScheme },
                });

            return builder.Build().GetSection(key);
        }

        protected internal class CustomMicrosoftIdentityAuthenticationDelegatingHandlerFactory : IMicrosoftIdentityAuthenticationDelegatingHandlerFactory
        {
            protected internal class CustomMicrosoftIdentityAuthenticationAppHandler : DelegatingHandler
            {
            }

            protected internal class CustomMicrosoftIdentityAuthenticationUserHandler : DelegatingHandler
            {
            }

            public DelegatingHandler CreateAppHandler(string? serviceName)
            {
                return new CustomMicrosoftIdentityAuthenticationAppHandler();
            }

            public DelegatingHandler CreateUserHandler(string? serviceName)
            {
                return new CustomMicrosoftIdentityAuthenticationUserHandler();
            }
        }

        protected internal class TypedClient
        {
            public TypedClient(HttpClient client)
            {
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void AddMicrosoftIdentityAuthenticationHandler_WithConfiguration(bool useApp)
        {
            // arrange
            var services = new ServiceCollection();

            // act
            if (useApp)
            {
                services.AddHttpClient(HttpClientName)
                    .AddMicrosoftIdentityAppAuthenticationHandler(_configSection);
            }
            else
            {
                services.AddHttpClient(HttpClientName)
                    .AddMicrosoftIdentityUserAuthenticationHandler(_configSection);
            }

            // assert
            Assert.Contains(services, s => s.ServiceType == typeof(IConfigureOptions<MicrosoftIdentityAuthenticationMessageHandlerOptions>));

            var provider = services.BuildServiceProvider();
            var options = provider.GetRequiredService<IOptionsSnapshot<MicrosoftIdentityAuthenticationMessageHandlerOptions>>();

            Assert.Equal(TestConstants.Scopes, options.Get(HttpClientName).Scopes);
            Assert.Equal(TestConstants.TenantIdAsGuid, options.Get(HttpClientName).Tenant);
            Assert.Equal(TestConstants.B2CSignUpSignInUserFlow, options.Get(HttpClientName).UserFlow);
            Assert.False(options.Get(HttpClientName).IsProofOfPossessionRequest);
            Assert.Equal(JwtBearerDefaults.AuthenticationScheme, options.Get(HttpClientName).AuthenticationScheme);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void AddMicrosoftIdentityAuthenticationHandler_WithOptions(bool useApp)
        {
            // arrange
            var services = new ServiceCollection();
            Action<MicrosoftIdentityAuthenticationMessageHandlerOptions> configureOptions = options =>
            {
                options.Scopes = TestConstants.GraphScopes;
                options.Tenant = TestConstants.TenantIdAsGuid;
                options.UserFlow = TestConstants.B2CResetPasswordUserFlow;
                options.IsProofOfPossessionRequest = true;
                options.AuthenticationScheme = JwtBearerDefaults.AuthenticationScheme;
            };

            // act
            if (useApp)
            {
                services.AddHttpClient(HttpClientName)
                    .AddMicrosoftIdentityAppAuthenticationHandler(configureOptions);
            }
            else
            {
                services.AddHttpClient(HttpClientName)
                    .AddMicrosoftIdentityUserAuthenticationHandler(configureOptions);
            }

            // assert
            Assert.Contains(services, s => s.ServiceType == typeof(IConfigureOptions<MicrosoftIdentityAuthenticationMessageHandlerOptions>));

            var provider = services.BuildServiceProvider();
            var options = provider.GetRequiredService<IOptionsSnapshot<MicrosoftIdentityAuthenticationMessageHandlerOptions>>();

            Assert.Equal(TestConstants.GraphScopes, options.Get(HttpClientName).Scopes);
            Assert.Equal(TestConstants.TenantIdAsGuid, options.Get(HttpClientName).Tenant);
            Assert.Equal(TestConstants.B2CResetPasswordUserFlow, options.Get(HttpClientName).UserFlow);
            Assert.True(options.Get(HttpClientName).IsProofOfPossessionRequest);
            Assert.Equal(JwtBearerDefaults.AuthenticationScheme, options.Get(HttpClientName).AuthenticationScheme);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void AddMicrosoftIdentityAuthenticationHandler_WithTypedHttpClient(bool useApp)
        {
            // arrange
            var clientName = typeof(TypedClient).Name;
            var services = new ServiceCollection();
            Action<MicrosoftIdentityAuthenticationMessageHandlerOptions> configureOptions = options =>
            {
                options.Scopes = TestConstants.GraphScopes;
                options.Tenant = TestConstants.TenantIdAsGuid;
                options.UserFlow = TestConstants.B2CResetPasswordUserFlow;
                options.IsProofOfPossessionRequest = true;
                options.AuthenticationScheme = JwtBearerDefaults.AuthenticationScheme;
            };

            // act
            if (useApp)
            {
                services.AddHttpClient<TypedClient>()
                    .AddMicrosoftIdentityAppAuthenticationHandler(configureOptions);
            }
            else
            {
                services.AddHttpClient<TypedClient>()
                    .AddMicrosoftIdentityUserAuthenticationHandler(configureOptions);
            }

            // assert
            Assert.Contains(services, s => s.ServiceType == typeof(IConfigureOptions<MicrosoftIdentityAuthenticationMessageHandlerOptions>));

            var provider = services.BuildServiceProvider();
            var options = provider.GetRequiredService<IOptionsSnapshot<MicrosoftIdentityAuthenticationMessageHandlerOptions>>();

            Assert.Equal(TestConstants.GraphScopes, options.Get(clientName).Scopes);
            Assert.Equal(TestConstants.TenantIdAsGuid, options.Get(clientName).Tenant);
            Assert.Equal(TestConstants.B2CResetPasswordUserFlow, options.Get(clientName).UserFlow);
            Assert.True(options.Get(clientName).IsProofOfPossessionRequest);
            Assert.Equal(JwtBearerDefaults.AuthenticationScheme, options.Get(clientName).AuthenticationScheme);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void AddMicrosoftIdentityAuthenticationHandler_WithCustomFactory(bool useApp)
        {
            // arrange
            var services = new ServiceCollection();
            Action<MicrosoftIdentityAuthenticationMessageHandlerOptions> configureOptions = options =>
            {
                options.Scopes = TestConstants.GraphScopes;
                options.Tenant = TestConstants.TenantIdAsGuid;
                options.UserFlow = TestConstants.B2CResetPasswordUserFlow;
                options.IsProofOfPossessionRequest = true;
                options.AuthenticationScheme = JwtBearerDefaults.AuthenticationScheme;
            };

            // act
            // Register our custom type first to ensure our extension methods don't override user behavior
            services.AddSingleton<IMicrosoftIdentityAuthenticationDelegatingHandlerFactory, CustomMicrosoftIdentityAuthenticationDelegatingHandlerFactory>();
            if (useApp)
            {
                services.AddHttpClient(HttpClientName)
                    .AddMicrosoftIdentityAppAuthenticationHandler(configureOptions);
            }
            else
            {
                services.AddHttpClient(HttpClientName)
                    .AddMicrosoftIdentityUserAuthenticationHandler(configureOptions);
            }

            // assert

            var provider = services.BuildServiceProvider();
            var factory = provider.GetRequiredService<IMicrosoftIdentityAuthenticationDelegatingHandlerFactory>();
            Assert.Equal(typeof(CustomMicrosoftIdentityAuthenticationDelegatingHandlerFactory), factory.GetType());

            var appHandler = factory.CreateAppHandler(string.Empty);
            Assert.Equal(typeof(CustomMicrosoftIdentityAuthenticationDelegatingHandlerFactory.CustomMicrosoftIdentityAuthenticationAppHandler), appHandler.GetType());

            var userHandler = factory.CreateUserHandler(string.Empty);
            Assert.Equal(typeof(CustomMicrosoftIdentityAuthenticationDelegatingHandlerFactory.CustomMicrosoftIdentityAuthenticationUserHandler), userHandler.GetType());
        }
    }
}
