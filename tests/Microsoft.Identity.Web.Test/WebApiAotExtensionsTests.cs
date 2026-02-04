// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#if NET10_0_OR_GREATER

using System;
using System.Linq;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web.Test.Common;
using NSubstitute;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    public class WebApiAotExtensionsTests
    {
        private const string ConfigSectionName = "AzureAd";
        private const string JwtBearerScheme = "Bearer";
        private readonly IConfigurationSection _configSection;

        public WebApiAotExtensionsTests()
        {
            _configSection = GetConfigSection(ConfigSectionName);
        }

        private static IConfigurationSection GetConfigSection(string key)
        {
            var configAsDictionary = new System.Collections.Generic.Dictionary<string, string?>()
            {
                { key, null },
                { $"{key}:Instance", TestConstants.AadInstance },
                { $"{key}:TenantId", TestConstants.TenantIdAsGuid },
                { $"{key}:ClientId", TestConstants.ClientId },
            };
            var memoryConfigSource = new MemoryConfigurationSource { InitialData = configAsDictionary };
            var configBuilder = new ConfigurationBuilder();
            configBuilder.Add(memoryConfigSource);
            var configSection = configBuilder.Build().GetSection(key);
            return configSection;
        }

        [Fact]
        public void AddMicrosoftIdentityWebApiAot_WithConfigurationSection_RegistersServices()
        {
            // Arrange
            var services = new ServiceCollection()
                .AddLogging();

            // Act
            services.AddAuthentication()
                .AddMicrosoftIdentityWebApiAot(_configSection, JwtBearerScheme);

            // Assert
            var provider = services.BuildServiceProvider();

            // Verify core services are registered
            Assert.Contains(services, s => s.ServiceType == typeof(IHttpContextAccessor));
            Assert.Contains(services, s => s.ServiceType == typeof(IMergedOptionsStore));
            Assert.Contains(services, s => s.ServiceType == typeof(MicrosoftIdentityIssuerValidatorFactory));
            
            // Verify post-configurators are registered
            Assert.Contains(services, s => s.ImplementationType == typeof(MicrosoftIdentityApplicationOptionsToMergedOptionsMerger));
            Assert.Contains(services, s => s.ImplementationType == typeof(MicrosoftIdentityJwtBearerOptionsPostConfigurator));

            // Verify options can be retrieved
            var jwtOptions = provider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>().Get(JwtBearerScheme);
            Assert.NotNull(jwtOptions);
        }

        [Fact]
        public void AddMicrosoftIdentityWebApiAot_WithProgrammaticConfiguration_RegistersServices()
        {
            // Arrange
            var services = new ServiceCollection()
                .AddLogging();

            // Act
            services.AddAuthentication()
                .AddMicrosoftIdentityWebApiAot(options =>
                {
                    options.Instance = TestConstants.AadInstance;
                    options.TenantId = TestConstants.TenantIdAsGuid;
                    options.ClientId = TestConstants.ClientId;
                }, JwtBearerScheme);

            // Assert
            var provider = services.BuildServiceProvider();

            // Verify MicrosoftIdentityApplicationOptions is configured
            var appOptions = provider.GetRequiredService<IOptionsMonitor<MicrosoftIdentityApplicationOptions>>().Get(JwtBearerScheme);
            Assert.Equal(TestConstants.AadInstance, appOptions.Instance);
            Assert.Equal(TestConstants.TenantIdAsGuid, appOptions.TenantId);
            Assert.Equal(TestConstants.ClientId, appOptions.ClientId);

            // Verify core services are registered
            Assert.Contains(services, s => s.ServiceType == typeof(IMergedOptionsStore));
            Assert.Contains(services, s => s.ImplementationType == typeof(MicrosoftIdentityApplicationOptionsToMergedOptionsMerger));
            Assert.Contains(services, s => s.ImplementationType == typeof(MicrosoftIdentityJwtBearerOptionsPostConfigurator));
        }

        [Fact]
        public void AddMicrosoftIdentityWebApiAot_WithCustomJwtBearerOptions_AppliesCustomConfiguration()
        {
            // Arrange
            var services = new ServiceCollection()
                .AddLogging();
            var customClockSkew = TimeSpan.FromMinutes(10);

            // Act
            services.AddAuthentication()
                .AddMicrosoftIdentityWebApiAot(
                    options =>
                    {
                        options.Instance = TestConstants.AadInstance;
                        options.TenantId = TestConstants.TenantIdAsGuid;
                        options.ClientId = TestConstants.ClientId;
                    },
                    JwtBearerScheme,
                    jwtOptions =>
                    {
                        jwtOptions.TokenValidationParameters.ClockSkew = customClockSkew;
                    });

            // Assert
            var provider = services.BuildServiceProvider();
            var jwtOptions = provider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>().Get(JwtBearerScheme);
            
            Assert.Equal(customClockSkew, jwtOptions.TokenValidationParameters.ClockSkew);
        }

        [Fact]
        public void AddMicrosoftIdentityWebApiAot_ConfigurationSectionDelegatesToProgrammaticOverload()
        {
            // Arrange
            var services = new ServiceCollection()
                .AddLogging();

            // Act
            services.AddAuthentication()
                .AddMicrosoftIdentityWebApiAot(_configSection);

            // Assert
            var provider = services.BuildServiceProvider();
            
            // Verify that configuration was bound properly through delegation
            var appOptions = provider.GetRequiredService<IOptionsMonitor<MicrosoftIdentityApplicationOptions>>().Get(JwtBearerDefaults.AuthenticationScheme);
            Assert.Equal(TestConstants.AadInstance, appOptions.Instance);
            Assert.Equal(TestConstants.TenantIdAsGuid, appOptions.TenantId);
            Assert.Equal(TestConstants.ClientId, appOptions.ClientId);
        }

        [Fact]
        public void AddMicrosoftIdentityWebApiAot_PostConfiguratorPopulatesMergedOptions()
        {
            // Arrange
            var services = new ServiceCollection()
                .AddLogging();

            // Act
            services.AddAuthentication()
                .AddMicrosoftIdentityWebApiAot(options =>
                {
                    options.Instance = TestConstants.AadInstance;
                    options.TenantId = TestConstants.TenantIdAsGuid;
                    options.ClientId = TestConstants.ClientId;
                }, JwtBearerScheme);

            // Assert
            var provider = services.BuildServiceProvider();
            
            // Trigger post-configuration by getting the options
            var appOptions = provider.GetRequiredService<IOptionsMonitor<MicrosoftIdentityApplicationOptions>>().Get(JwtBearerScheme);
            
            // Verify MergedOptions was populated
            var mergedOptionsStore = provider.GetRequiredService<IMergedOptionsStore>();
            var mergedOptions = mergedOptionsStore.Get(JwtBearerScheme);
            
            Assert.Equal(TestConstants.ClientId, mergedOptions.ClientId);
            Assert.Equal(TestConstants.TenantIdAsGuid, mergedOptions.TenantId);
            Assert.Equal(TestConstants.AadInstance, mergedOptions.Instance);
        }

        [Fact]
        public void AddMicrosoftIdentityWebApiAot_ThrowsOnNullBuilder()
        {
            // Arrange
            AuthenticationBuilder? builder = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                builder!.AddMicrosoftIdentityWebApiAot(options => { }));
        }

        [Fact]
        public void AddMicrosoftIdentityWebApiAot_ThrowsOnNullConfigureOptions()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = services.AddAuthentication();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                builder.AddMicrosoftIdentityWebApiAot((Action<MicrosoftIdentityApplicationOptions>)null!));
        }

        [Fact]
        public void AddMicrosoftIdentityWebApiAot_ThrowsOnNullConfigurationSection()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = services.AddAuthentication();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                builder.AddMicrosoftIdentityWebApiAot((IConfigurationSection)null!));
        }
    }
}

#endif
