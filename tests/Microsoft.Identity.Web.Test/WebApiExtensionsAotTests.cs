// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#if NET10_0_OR_GREATER

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web.PostConfigureOptions;
using Microsoft.Identity.Web.Resource;
using Microsoft.Identity.Web.Test.Common;
using Microsoft.Identity.Web.Test.Common.TestHelpers;
using NSubstitute;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    public class WebApiExtensionsAotTests
    {
        private const string ConfigSectionName = "AzureAd";
        private const string JwtBearerScheme = JwtBearerDefaults.AuthenticationScheme;

        [Fact]
        public void AddMicrosoftIdentityWebApiAot_WithConfigSection_RegistersServices()
        {
            // Arrange
            var services = new ServiceCollection().AddLogging();

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
                    null);

            // Assert
            var provider = services.BuildServiceProvider();

            // Verify core services are registered
            Assert.Contains(services, s => s.ServiceType == typeof(IHttpContextAccessor));
            Assert.Contains(services, s => s.ServiceType == typeof(IMergedOptionsStore));
            Assert.Contains(services, s => s.ServiceType == typeof(MicrosoftIdentityIssuerValidatorFactory));

            // Verify MicrosoftIdentityApplicationOptions can be retrieved
            var appOptions = provider.GetRequiredService<IOptionsMonitor<MicrosoftIdentityApplicationOptions>>().Get(JwtBearerScheme);
            Assert.NotNull(appOptions);

            // Verify JWT bearer options can be retrieved
            var jwtOptions = provider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>().Get(JwtBearerScheme);
            Assert.NotNull(jwtOptions);

            // Verify post-configurator is registered
            var postConfigurators = services.Where(s =>
                s.ServiceType == typeof(IPostConfigureOptions<JwtBearerOptions>) &&
                (s.ImplementationFactory != null || s.ImplementationType != null)).ToList();
            Assert.NotEmpty(postConfigurators);
        }

        [Fact]
        public void AddMicrosoftIdentityWebApiAot_WithAction_RegistersServices()
        {
            // Arrange
            var services = new ServiceCollection().AddLogging();

            // Act
            services.AddAuthentication()
                .AddMicrosoftIdentityWebApiAot(options =>
                {
                    options.Instance = TestConstants.AadInstance;
                    options.TenantId = TestConstants.TenantIdAsGuid;
                    options.ClientId = TestConstants.ClientId;
                }, JwtBearerScheme, null);

            // Assert
            var provider = services.BuildServiceProvider();

            // Verify MicrosoftIdentityApplicationOptions are configured
            var appOptions = provider.GetRequiredService<IOptionsMonitor<MicrosoftIdentityApplicationOptions>>().Get(JwtBearerScheme);
            Assert.Equal(TestConstants.AadInstance, appOptions.Instance);
            Assert.Equal(TestConstants.TenantIdAsGuid, appOptions.TenantId);
            Assert.Equal(TestConstants.ClientId, appOptions.ClientId);

            // Verify core services are registered
            Assert.Contains(services, s => s.ServiceType == typeof(IHttpContextAccessor));
            Assert.Contains(services, s => s.ServiceType == typeof(IMergedOptionsStore));
            Assert.Contains(services, s => s.ServiceType == typeof(MicrosoftIdentityIssuerValidatorFactory));
        }

        [Fact]
        public void AddMicrosoftIdentityWebApiAot_WithCustomJwtBearerOptions_AppliesConfiguration()
        {
            // Arrange
            var services = new ServiceCollection().AddLogging();
            bool customOptionsApplied = false;

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
                        customOptionsApplied = true;
                        jwtOptions.TokenValidationParameters.ClockSkew = TimeSpan.FromMinutes(5);
                    });

            // Assert
            var provider = services.BuildServiceProvider();
            var jwtOptions = provider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>().Get(JwtBearerScheme);

            Assert.True(customOptionsApplied);
            Assert.Equal(TimeSpan.FromMinutes(5), jwtOptions.TokenValidationParameters.ClockSkew);
        }

        [Fact]
        public void PostConfigurator_ConfiguresAuthority()
        {
            // Arrange
            var services = new ServiceCollection().AddLogging();
            services.AddAuthentication()
                .AddMicrosoftIdentityWebApiAot(options =>
                {
                    options.Instance = TestConstants.AadInstance;
                    options.TenantId = TestConstants.TenantIdAsGuid;
                    options.ClientId = TestConstants.ClientId;
                }, JwtBearerScheme, null);

            var provider = services.BuildServiceProvider();

            // Act
            var jwtOptions = provider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>().Get(JwtBearerScheme);

            // Assert
            Assert.NotNull(jwtOptions.Authority);
            Assert.Contains(TestConstants.TenantIdAsGuid, jwtOptions.Authority, StringComparison.Ordinal);
            Assert.EndsWith("/v2.0", jwtOptions.Authority, StringComparison.Ordinal);
        }

        [Fact]
        public void PostConfigurator_ConfiguresAudienceValidation()
        {
            // Arrange
            var services = new ServiceCollection().AddLogging();
            services.AddAuthentication()
                .AddMicrosoftIdentityWebApiAot(options =>
                {
                    options.Instance = TestConstants.AadInstance;
                    options.TenantId = TestConstants.TenantIdAsGuid;
                    options.ClientId = TestConstants.ClientId;
                }, JwtBearerScheme, null);

            var provider = services.BuildServiceProvider();

            // Act
            var jwtOptions = provider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>().Get(JwtBearerScheme);

            // Assert
            Assert.NotNull(jwtOptions.TokenValidationParameters);
            Assert.NotNull(jwtOptions.TokenValidationParameters.AudienceValidator);
        }

        [Fact]
        public void PostConfigurator_ChainsOnTokenValidated_ForOboSupport()
        {
            // Arrange
            var services = new ServiceCollection().AddLogging();
            services.AddAuthentication()
                .AddMicrosoftIdentityWebApiAot(options =>
                {
                    options.Instance = TestConstants.AadInstance;
                    options.TenantId = TestConstants.TenantIdAsGuid;
                    options.ClientId = TestConstants.ClientId;
                }, JwtBearerScheme, null);

            var provider = services.BuildServiceProvider();

            // Act
            var jwtOptions = provider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>().Get(JwtBearerScheme);

            // Assert
            Assert.NotNull(jwtOptions.Events);
            Assert.NotNull(jwtOptions.Events.OnTokenValidated);
        }

        [Fact]
        public void PostConfigurator_RespectsCustomerAuthority()
        {
            // Arrange
            var customAuthority = "https://custom.authority.com/tenant-id/v2.0";
            var services = new ServiceCollection().AddLogging();

            services.AddAuthentication()
                .AddMicrosoftIdentityWebApiAot(options =>
                {
                    options.Instance = TestConstants.AadInstance;
                    options.TenantId = TestConstants.TenantIdAsGuid;
                    options.ClientId = TestConstants.ClientId;
                }, JwtBearerScheme, null);

            // Customer configures their own authority AFTER our call
            services.Configure<JwtBearerOptions>(JwtBearerScheme, options =>
            {
                options.Authority = customAuthority;
            });

            var provider = services.BuildServiceProvider();

            // Act
            var jwtOptions = provider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>().Get(JwtBearerScheme);

            // Assert - our PostConfigure should respect the customer's authority
            Assert.Equal(customAuthority, jwtOptions.Authority);
        }

        [Fact]
        public void PostConfigurator_SkipsWhenNotConfigured()
        {
            // Arrange
            var services = new ServiceCollection().AddLogging();

            // Add JWT bearer without using our AOT method
            services.AddAuthentication()
                .AddJwtBearer(JwtBearerScheme);

            // Manually register the post-configurator
            services.AddSingleton<IOptionsMonitor<MicrosoftIdentityApplicationOptions>>(
                sp => new TestOptionsMonitor<MicrosoftIdentityApplicationOptions>(
                    new MicrosoftIdentityApplicationOptions()));

            services.AddSingleton<IPostConfigureOptions<JwtBearerOptions>>(
                sp => new MicrosoftIdentityJwtBearerOptionsPostConfigurator(
                    sp.GetRequiredService<IOptionsMonitor<MicrosoftIdentityApplicationOptions>>(),
                    sp));

            var provider = services.BuildServiceProvider();

            // Act - PostConfigure should skip because ClientId is not set
            var jwtOptions = provider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>().Get(JwtBearerScheme);

            // Assert - Should not throw, and authority should remain null
            Assert.Null(jwtOptions.Authority);
        }

        [Fact]
        public void ValidateRequiredOptions_ThrowsForMissingClientId()
        {
            // Arrange
            var options = new MicrosoftIdentityApplicationOptions
            {
                Instance = TestConstants.AadInstance,
                TenantId = TestConstants.TenantIdAsGuid,
            };

            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                Internal.IdentityOptionsHelpers.ValidateRequiredOptions(options));

            Assert.Contains("ClientId", exception.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void ValidateRequiredOptions_ThrowsForMissingInstance()
        {
            // Arrange
            var options = new MicrosoftIdentityApplicationOptions
            {
                ClientId = TestConstants.ClientId,
                TenantId = TestConstants.TenantIdAsGuid,
                Authority = "",
            };

            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                Internal.IdentityOptionsHelpers.ValidateRequiredOptions(options));

            Assert.Contains("Instance", exception.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void ValidateRequiredOptions_ThrowsForMissingTenantId()
        {
            // Arrange
            var options = new MicrosoftIdentityApplicationOptions
            {
                ClientId = TestConstants.ClientId,
                Instance = TestConstants.AadInstance,
                Authority = "",
            };

            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                Internal.IdentityOptionsHelpers.ValidateRequiredOptions(options));

            Assert.Contains("TenantId", exception.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void ValidateRequiredOptions_ThrowsForMissingDomain_B2C()
        {
            // Arrange
            var options = new MicrosoftIdentityApplicationOptions
            {
                ClientId = TestConstants.ClientId,
                Instance = TestConstants.B2CInstance,
                SignUpSignInPolicyId = TestConstants.B2CSignUpSignInUserFlow,
                Authority = "",
            };

            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                Internal.IdentityOptionsHelpers.ValidateRequiredOptions(options));

            Assert.Contains("Domain", exception.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void ValidateRequiredOptions_PassesWithValidOptions()
        {
            // Arrange
            var options = new MicrosoftIdentityApplicationOptions
            {
                ClientId = TestConstants.ClientId,
                Instance = TestConstants.AadInstance,
                TenantId = TestConstants.TenantIdAsGuid,
            };

            // Act & Assert - should not throw
            Internal.IdentityOptionsHelpers.ValidateRequiredOptions(options);
        }

        [Fact]
        public void BuildAuthority_AAD_BuildsCorrectAuthority()
        {
            // Arrange
            var options = new MicrosoftIdentityApplicationOptions
            {
                Instance = TestConstants.AadInstance,
                TenantId = TestConstants.TenantIdAsGuid,
            };

            // Act
            var authority = Internal.IdentityOptionsHelpers.BuildAuthority(options);

            // Assert
            Assert.Contains(TestConstants.AadInstance.TrimEnd('/'), authority, StringComparison.Ordinal);
            Assert.Contains(TestConstants.TenantIdAsGuid, authority, StringComparison.Ordinal);
            Assert.EndsWith("/v2.0", authority, StringComparison.Ordinal);
        }

        [Fact]
        public void BuildAuthority_B2C_BuildsCorrectAuthority()
        {
            // Arrange
            var options = new MicrosoftIdentityApplicationOptions
            {
                Instance = TestConstants.B2CInstance,
                Domain = TestConstants.B2CTenant,
                SignUpSignInPolicyId = TestConstants.B2CSignUpSignInUserFlow,
            };

            // Act
            var authority = Internal.IdentityOptionsHelpers.BuildAuthority(options);

            // Assert
            Assert.Contains(TestConstants.B2CTenant, authority, StringComparison.Ordinal);
            Assert.Contains(TestConstants.B2CSignUpSignInUserFlow, authority, StringComparison.Ordinal);
            Assert.EndsWith("/v2.0", authority, StringComparison.Ordinal);
        }

        [Fact]
        public async Task ChainTokenStorageHandler_ChainsExistingHandler()
        {
            // Arrange
            bool existingHandlerCalled = false;
            Func<TokenValidatedContext, Task> existingHandler = context =>
            {
                existingHandlerCalled = true;
                return Task.CompletedTask;
            };

            // Act
            var chainedHandler = Internal.IdentityOptionsHelpers.ChainTokenStorageHandler(existingHandler);

            // Assert
            Assert.NotNull(chainedHandler);

            // Create a mock context
            var httpContext = new DefaultHttpContext();
            var tokenValidatedContext = new TokenValidatedContext(
                httpContext,
                new AuthenticationScheme(JwtBearerScheme, null, typeof(JwtBearerHandler)),
                new JwtBearerOptions())
            {
                SecurityToken = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken()
            };

            // Execute the chained handler
            await chainedHandler(tokenValidatedContext);

            // Verify existing handler was called
            Assert.True(existingHandlerCalled);
        }

        [Fact]
        public async Task ChainTokenStorageHandler_WorksWithNullExistingHandler()
        {
            // Arrange & Act
            var chainedHandler = Internal.IdentityOptionsHelpers.ChainTokenStorageHandler(null);

            // Assert
            Assert.NotNull(chainedHandler);

            // Create a mock context
            var httpContext = new DefaultHttpContext();
            var tokenValidatedContext = new TokenValidatedContext(
                httpContext,
                new AuthenticationScheme(JwtBearerScheme, null, typeof(JwtBearerHandler)),
                new JwtBearerOptions())
            {
                SecurityToken = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken()
            };

            // Execute the handler - should not throw
            await chainedHandler(tokenValidatedContext);
        }

        [Fact]
        public void ConfigureAudienceValidation_SetsAudienceValidator()
        {
            // Arrange
            var options = new MicrosoftIdentityApplicationOptions
            {
                ClientId = TestConstants.ClientId,
                Instance = TestConstants.AadInstance,
                TenantId = TestConstants.TenantIdAsGuid,
            };
            var tokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters();

            // Act
            Internal.IdentityOptionsHelpers.ConfigureAudienceValidation(tokenValidationParameters, options);

            // Assert
            Assert.NotNull(tokenValidationParameters.AudienceValidator);
        }

        [Fact]
        public void AddMicrosoftIdentityWebApiAot_WithTokenAcquisition_EnablesObo()
        {
            // Arrange
            var services = new ServiceCollection().AddLogging();

            // Act - The key test: OBO should work without calling EnableTokenAcquisitionToCallDownstreamApi
            services.AddAuthentication()
                .AddMicrosoftIdentityWebApiAot(options =>
                {
                    options.Instance = TestConstants.AadInstance;
                    options.TenantId = TestConstants.TenantIdAsGuid;
                    options.ClientId = TestConstants.ClientId;
                }, JwtBearerScheme, null);

            services.AddTokenAcquisition();

            var provider = services.BuildServiceProvider();

            // Assert
            var jwtOptions = provider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>().Get(JwtBearerScheme);

            // Verify OnTokenValidated is set up for OBO
            Assert.NotNull(jwtOptions.Events);
            Assert.NotNull(jwtOptions.Events.OnTokenValidated);

            // Verify MergedOptions are populated via the merger
            var mergedOptionsStore = provider.GetRequiredService<IMergedOptionsStore>();
            var mergedOptions = mergedOptionsStore.Get(JwtBearerScheme);
            Assert.NotNull(mergedOptions);
            Assert.Equal(TestConstants.ClientId, mergedOptions.ClientId);
        }
    }
}

#endif
