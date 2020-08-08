// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web.Resource;
using Microsoft.Identity.Web.Test.Common;
using Microsoft.Identity.Web.Test.Common.TestHelpers;
using NSubstitute;
using NSubstitute.Extensions;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    public class WebApiExtensionsTests
    {
        private const string ConfigSectionName = "AzureAd-Custom";
        private const string JwtBearerScheme = "Bearer-Custom";
        private static readonly CertificateDescription[] s_tokenDecryptionCertificatesDescription = new[]
        {
            CertificateDescription.FromBase64Encoded(TestConstants.CertificateX5c),
        };
        private readonly IConfigurationSection _configSection;
        private readonly Action<ConfidentialClientApplicationOptions> _configureAppOptions = (options) => { };
        private readonly Action<JwtBearerOptions> _configureJwtOptions = (options) => { };
        private readonly Action<MicrosoftIdentityOptions> _configureMsOptions = (options) =>
        {
            options.Instance = TestConstants.AadInstance;
            options.TenantId = TestConstants.TenantIdAsGuid;
            options.ClientId = TestConstants.ClientId;
            options.TokenDecryptionCertificates = s_tokenDecryptionCertificatesDescription;
        };

        public WebApiExtensionsTests()
        {
            _configSection = GetConfigSection(ConfigSectionName);
        }

        [Fact]
        public void AddMicrosoftWebApi_WithConfigName()
        {
            var config = Substitute.For<IConfiguration>();
            config.Configure().GetSection(ConfigSectionName).Returns(_configSection);

            var services = new ServiceCollection()
                .AddLogging();

            new AuthenticationBuilder(services)
                .AddMicrosoftIdentityPlatformWebApi(config, ConfigSectionName, JwtBearerScheme, true);

            var provider = services.BuildServiceProvider();

            // Config bind actions added correctly
            provider.GetRequiredService<IOptionsFactory<JwtBearerOptions>>().Create(JwtBearerScheme);
            provider.GetRequiredService<IOptionsFactory<MicrosoftIdentityOptions>>().Create(string.Empty);
            config.Received(1).GetSection(ConfigSectionName);

            AddMicrosoftWebApi_TestCommon(services, provider, false);
        }

        [Fact]
        public void AddMicrosoftWebApi_WithConfigActions()
        {
            var services = new ServiceCollection()
                .AddLogging();

            new AuthenticationBuilder(services)
                .AddMicrosoftIdentityPlatformWebApi(_configureJwtOptions, _configureMsOptions, JwtBearerScheme, true);

            var provider = services.BuildServiceProvider();

            // Configure options actions added correctly
            var configuredMsOptions = provider.GetServices<IConfigureOptions<MicrosoftIdentityOptions>>().Cast<ConfigureNamedOptions<MicrosoftIdentityOptions>>();

#if DOTNET_CORE_31
            var configuredJwtOptions = provider.GetServices<IConfigureOptions<JwtBearerOptions>>().Cast<ConfigureNamedOptions<JwtBearerOptions>>();

            Assert.Contains(configuredJwtOptions, o => o.Action == _configureJwtOptions);
#endif
            Assert.Contains(configuredMsOptions, o => o.Action == _configureMsOptions);

            AddMicrosoftWebApi_TestCommon(services, provider);
        }

        [Fact]
        public void AddMicrosoftWebApiAuthentication_WithConfigName()
        {
            var config = Substitute.For<IConfiguration>();
            config.Configure().GetSection(ConfigSectionName).Returns(_configSection);

            var services = new ServiceCollection()
                .AddLogging();

            services.AddMicrosoftIdentityPlatformWebApiAuthentication(config, ConfigSectionName, JwtBearerScheme, true);

            var provider = services.BuildServiceProvider();

            // Config bind actions added correctly
            provider.GetRequiredService<IOptionsFactory<JwtBearerOptions>>().Create(JwtBearerScheme);
            provider.GetRequiredService<IOptionsFactory<MicrosoftIdentityOptions>>().Create(string.Empty);
            config.Received(1).GetSection(ConfigSectionName);

            AddMicrosoftWebApi_TestCommon(services, provider, false);
        }

        private void AddMicrosoftWebApi_TestCommon(IServiceCollection services, ServiceProvider provider, bool checkDecryptCertificate = true)
        {
            // Correct services added
            Assert.Contains(services, s => s.ServiceType == typeof(IHttpContextAccessor));
            Assert.Contains(services, s => s.ServiceType == typeof(IConfigureOptions<MicrosoftIdentityOptions>));
            Assert.Contains(services, s => s.ServiceType == typeof(IConfigureOptions<JwtBearerOptions>));
            Assert.Contains(services, s => s.ServiceType == typeof(IJwtBearerMiddlewareDiagnostics));
            Assert.Equal(ServiceLifetime.Singleton, services.First(s => s.ServiceType == typeof(IJwtBearerMiddlewareDiagnostics)).Lifetime);

            // JWT options added correctly
            var configuredJwtOptions = provider.GetService<IConfigureOptions<JwtBearerOptions>>() as IConfigureNamedOptions<JwtBearerOptions>;

            // Issuer validator and certificate set
            var jwtOptions = provider.GetRequiredService<IOptionsFactory<JwtBearerOptions>>().Create(JwtBearerScheme);

            Assert.NotNull(jwtOptions.Authority);
            Assert.NotNull(jwtOptions.TokenValidationParameters.IssuerValidator);
            if (checkDecryptCertificate)
            {
                Assert.NotNull(jwtOptions.TokenValidationParameters.TokenDecryptionKey);
            }
        }

        [Fact]
        public void AddMicrosoftWebApi_WithConfigName_JwtBearerTokenValidatedEventCalled()
        {
            var config = Substitute.For<IConfiguration>();
            config.Configure().GetSection(ConfigSectionName).Returns(_configSection);

            var tokenValidatedFunc = Substitute.For<Func<TokenValidatedContext, Task>>();

            var services = new ServiceCollection()
                .Configure<JwtBearerOptions>(JwtBearerScheme, (options) =>
                {
                    options.Events ??= new JwtBearerEvents();
                    options.Events.OnTokenValidated += tokenValidatedFunc;
                })
                .AddLogging();

            new AuthenticationBuilder(services)
                .AddMicrosoftIdentityPlatformWebApi(config, ConfigSectionName, JwtBearerScheme, true);

            var provider = services.BuildServiceProvider();

            var jwtOptions = provider.GetRequiredService<IOptionsFactory<JwtBearerOptions>>().Create(JwtBearerScheme);

            AddMicrosoftWebApi_TestJwtBearerTokenValidatedEvent(jwtOptions, tokenValidatedFunc);
        }

        [Fact]
        public void AddMicrosoftWebApi_WithConfigActions_JwtBearerTokenValidatedEventCalled()
        {
            var tokenValidatedFunc = Substitute.For<Func<TokenValidatedContext, Task>>();

            var services = new ServiceCollection()
                .Configure<JwtBearerOptions>(JwtBearerScheme, (options) =>
                {
                    options.Events ??= new JwtBearerEvents();
                    options.Events.OnTokenValidated += tokenValidatedFunc;
                })
                .AddLogging();

            new AuthenticationBuilder(services)
                .AddMicrosoftIdentityPlatformWebApi(_configureJwtOptions, _configureMsOptions, JwtBearerScheme, true);

            var provider = services.BuildServiceProvider();

            var jwtOptions = provider.GetRequiredService<IOptionsFactory<JwtBearerOptions>>().Create(JwtBearerScheme);

            AddMicrosoftWebApi_TestJwtBearerTokenValidatedEvent(jwtOptions, tokenValidatedFunc);
        }

        private async void AddMicrosoftWebApi_TestJwtBearerTokenValidatedEvent(JwtBearerOptions jwtOptions, Func<TokenValidatedContext, Task> tokenValidatedFunc)
        {
            var scopeTypes = new[] { ClaimConstants.Scope, ClaimConstants.Scp, ClaimConstants.Roles, ClaimConstants.Role };
            var expectedExceptionMessage = IDWebErrorMessage.NeitherScopeOrRolesClaimFoundInToken;
            var scopeValue = "scope";

            var httpContext = HttpContextUtilities.CreateHttpContext();
            var authScheme = new AuthenticationScheme(JwtBearerDefaults.AuthenticationScheme, JwtBearerDefaults.AuthenticationScheme, typeof(JwtBearerHandler));
            var tokenValidatedContext = new TokenValidatedContext(httpContext, authScheme, jwtOptions);
            tokenValidatedContext.Principal = new ClaimsPrincipal();

            // No scopes throws exception
            var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(async () => { await jwtOptions.Events.TokenValidated(tokenValidatedContext).ConfigureAwait(false); }).ConfigureAwait(false);
            Assert.Equal(expectedExceptionMessage, exception.Message);

            // At least one scope executes successfully
            foreach (var scopeType in scopeTypes)
            {
                tokenValidatedContext.Principal = new ClaimsPrincipal(
                    new ClaimsIdentity(new Claim[]
                    {
                        new Claim(scopeType, scopeValue),
                    }));
                await jwtOptions.Events.TokenValidated(tokenValidatedContext).ConfigureAwait(false);
            }

            await tokenValidatedFunc.Received(4).Invoke(Arg.Any<TokenValidatedContext>()).ConfigureAwait(false);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void AddMicrosoftWebApi_WithConfigName_SubscribesToDiagnostics(bool subscribeToDiagnostics)
        {
            var config = Substitute.For<IConfiguration>();
            config.Configure().GetSection(ConfigSectionName).Returns(_configSection);

            var diagnostics = Substitute.For<IJwtBearerMiddlewareDiagnostics>();

            var services = new ServiceCollection()
                .AddLogging();

            new AuthenticationBuilder(services)
                .AddMicrosoftIdentityPlatformWebApi(config, ConfigSectionName, JwtBearerScheme, subscribeToDiagnostics);

            services.RemoveAll<IJwtBearerMiddlewareDiagnostics>();
            services.AddSingleton<IJwtBearerMiddlewareDiagnostics>((provider) => diagnostics);

            var provider = services.BuildServiceProvider();

            var jwtOptions = provider.GetRequiredService<IOptionsFactory<JwtBearerOptions>>().Create(JwtBearerScheme);

            if (subscribeToDiagnostics)
            {
                diagnostics.Received().Subscribe(Arg.Any<JwtBearerEvents>());
            }
            else
            {
                diagnostics.DidNotReceive().Subscribe(Arg.Any<JwtBearerEvents>());
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void AddMicrosoftWebApi_WithConfigActions_SubscribesToDiagnostics(bool subscribeToDiagnostics)
        {
            var diagnostics = Substitute.For<IJwtBearerMiddlewareDiagnostics>();

            var services = new ServiceCollection()
                .AddLogging();

            new AuthenticationBuilder(services)
                .AddMicrosoftIdentityPlatformWebApi(_configureJwtOptions, _configureMsOptions, JwtBearerScheme, subscribeToDiagnostics);

            services.RemoveAll<IJwtBearerMiddlewareDiagnostics>();
            services.AddSingleton<IJwtBearerMiddlewareDiagnostics>((provider) => diagnostics);

            var provider = services.BuildServiceProvider();

            var jwtOptions = provider.GetRequiredService<IOptionsFactory<JwtBearerOptions>>().Create(JwtBearerScheme);

            if (subscribeToDiagnostics)
            {
                diagnostics.Received().Subscribe(Arg.Any<JwtBearerEvents>());
            }
            else
            {
                diagnostics.DidNotReceive().Subscribe(Arg.Any<JwtBearerEvents>());
            }
        }

        private IConfigurationSection GetConfigSection(string configSectionName)
        {
            string serializedTokenDecryptionJsonBlob = JsonSerializer.Serialize(
                s_tokenDecryptionCertificatesDescription,
                new JsonSerializerOptions
                {
                    IgnoreNullValues = true,
                    PropertyNameCaseInsensitive = true,
                }).Replace(":2", ": \"Base64Encoded\"");
            var configAsDictionary = new Dictionary<string, string>()
            {
                { configSectionName, null },
                { $"{configSectionName}:Instance", TestConstants.AadInstance },
                { $"{configSectionName}:TenantId", TestConstants.TenantIdAsGuid },
                { $"{configSectionName}:ClientId", TestConstants.TenantIdAsGuid },
                { $"{configSectionName}:TokenDecryptionCertificates", serializedTokenDecryptionJsonBlob },
            };
            var memoryConfigSource = new MemoryConfigurationSource { InitialData = configAsDictionary };
            var configBuilder = new ConfigurationBuilder();
            configBuilder.Add(memoryConfigSource);
            var configSection = configBuilder.Build().GetSection(configSectionName);
            return configSection;
        }

        [Fact]
        public async Task AddMicrosoftWebApiCallsWebApi_WithConfigName()
        {
            var config = Substitute.For<IConfiguration>();
            var tokenValidatedFuncMock = Substitute.For<Func<TokenValidatedContext, Task>>();

            var services = new ServiceCollection()
                .Configure<JwtBearerOptions>(JwtBearerScheme, (options) =>
                {
                    options.Events ??= new JwtBearerEvents();
                    options.Events.OnTokenValidated += tokenValidatedFuncMock;
                });
            new AuthenticationBuilder(services).AddMicrosoftWebApiCallsWebApi(config, ConfigSectionName, JwtBearerScheme);

            var provider = services.BuildServiceProvider();

            // Assert config bind actions added correctly
            provider.GetRequiredService<IOptionsFactory<ConfidentialClientApplicationOptions>>().Create(string.Empty);
            provider.GetRequiredService<IOptionsFactory<MicrosoftIdentityOptions>>().Create(string.Empty);

            config.Received(2).GetSection(ConfigSectionName);

            await AddMicrosoftWebApiCallsWebApi_TestCommon(services, provider, tokenValidatedFuncMock).ConfigureAwait(false);
        }

        [Fact]
        public async Task AddMicrosoftWebApiCallsWebApi_WithConfigActions()
        {
            var tokenValidatedFuncMock = Substitute.For<Func<TokenValidatedContext, Task>>();
            var services = new ServiceCollection()
                .Configure<JwtBearerOptions>(JwtBearerScheme, (options) =>
                {
                    options.Events ??= new JwtBearerEvents();
                    options.Events.OnTokenValidated += tokenValidatedFuncMock;
                });
            new AuthenticationBuilder(services).AddMicrosoftWebApiCallsWebApi(_configureAppOptions, _configureMsOptions, JwtBearerScheme);

            var provider = services.BuildServiceProvider();

            // Assert configure options actions added correctly
            var configuredAppOptions = provider.GetServices<IConfigureOptions<ConfidentialClientApplicationOptions>>().Cast<ConfigureNamedOptions<ConfidentialClientApplicationOptions>>();
            var configuredMsOptions = provider.GetServices<IConfigureOptions<MicrosoftIdentityOptions>>().Cast<ConfigureNamedOptions<MicrosoftIdentityOptions>>();

            Assert.Contains(configuredAppOptions, o => o.Action == _configureAppOptions);
            Assert.Contains(configuredMsOptions, o => o.Action == _configureMsOptions);

            await AddMicrosoftWebApiCallsWebApi_TestCommon(services, provider, tokenValidatedFuncMock).ConfigureAwait(false);
        }

        private async Task AddMicrosoftWebApiCallsWebApi_TestCommon(IServiceCollection services, ServiceProvider provider, Func<TokenValidatedContext, Task> tokenValidatedFuncMock)
        {
            // Assert correct services added
            Assert.Contains(services, s => s.ServiceType == typeof(IHttpContextAccessor));
            Assert.Contains(services, s => s.ServiceType == typeof(ITokenAcquisition));
            Assert.Contains(services, s => s.ServiceType == typeof(IConfigureOptions<ConfidentialClientApplicationOptions>));
            Assert.Contains(services, s => s.ServiceType == typeof(IConfigureOptions<MicrosoftIdentityOptions>));
            Assert.Contains(services, s => s.ServiceType == typeof(IConfigureOptions<JwtBearerOptions>));

            // Assert token validated event added correctly
            var jwtOptions = provider.GetRequiredService<IOptionsFactory<JwtBearerOptions>>().Create(JwtBearerScheme);
            var httpContext = HttpContextUtilities.CreateHttpContext();
            var authScheme = new AuthenticationScheme(JwtBearerDefaults.AuthenticationScheme, JwtBearerDefaults.AuthenticationScheme, typeof(JwtBearerHandler));
            var tokenValidatedContext = new TokenValidatedContext(httpContext, authScheme, jwtOptions)
            {
                SecurityToken = new JwtSecurityToken(),
                Principal = new ClaimsPrincipal(),
            };

            await jwtOptions.Events.TokenValidated(tokenValidatedContext).ConfigureAwait(false);

            // Assert events called
            await tokenValidatedFuncMock.ReceivedWithAnyArgs().Invoke(Arg.Any<TokenValidatedContext>()).ConfigureAwait(false);
            Assert.NotNull(httpContext.GetTokenUsedToCallWebAPI());
        }
    }
}
