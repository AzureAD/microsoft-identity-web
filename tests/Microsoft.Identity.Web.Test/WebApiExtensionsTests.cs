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
        private const string _configSectionName = "AzureAd-Custom";
        private const string _jwtBearerScheme = "Bearer-Custom";
        private static readonly X509Certificate2 _certificate = new X509Certificate2(Convert.FromBase64String(TestConstants.CertificateX5c));
        private static readonly CertificateDescription[] TokenDecryptionCertificatesDescription = new[]
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
            options.TokenDecryptionCertificates = TokenDecryptionCertificatesDescription;
        };

        public WebApiExtensionsTests()
        {
            _configSection = GetConfigSection(_configSectionName);
        }

        [Fact]
        public void AddProtectedWebApi_WithConfigName()
        {
            var config = Substitute.For<IConfiguration>();
            config.Configure().GetSection(_configSectionName).Returns(_configSection);

            var services = new ServiceCollection()
                .AddLogging();

            new AuthenticationBuilder(services)
                .AddMicrosoftWebApi(config, _configSectionName, _jwtBearerScheme, true);

            var provider = services.BuildServiceProvider();

            // Config bind actions added correctly
            provider.GetRequiredService<IOptionsFactory<JwtBearerOptions>>().Create(_jwtBearerScheme);
            provider.GetRequiredService<IOptionsFactory<MicrosoftIdentityOptions>>().Create(string.Empty);
            config.Received(3).GetSection(_configSectionName);

            // Ideally we'd want to configure the GetValue for "TokenDecryptionCertificates" but that's not possible!
            // config.Configure().GetSection(_configSectionName).GetValue<IEnumerable<CertificateDescription>>("TokenDecryptionCertificates").Returns(TokenDecryptionCertificatesDescription);
            // Therefore not testing the token decryption key in this case by section in this case (it's a test issue, not a
            // product issue)
            AddProtectedWebApi_TestCommon(services, provider, false);
        }

        [Fact]
        public void AddProtectedWebApi_WithConfigActions()
        {
            var services = new ServiceCollection()
                .AddLogging();

            new AuthenticationBuilder(services)
                .AddMicrosoftWebApi(_configureJwtOptions, _configureMsOptions, _jwtBearerScheme, true);

            var provider = services.BuildServiceProvider();

            // Configure options actions added correctly
            var configuredJwtOptions = provider.GetServices<IConfigureOptions<JwtBearerOptions>>().Cast<ConfigureNamedOptions<JwtBearerOptions>>();
            var configuredMsOptions = provider.GetServices<IConfigureOptions<MicrosoftIdentityOptions>>().Cast<ConfigureNamedOptions<MicrosoftIdentityOptions>>();

            Assert.Contains(configuredJwtOptions, o => o.Action == _configureJwtOptions);
            Assert.Contains(configuredMsOptions, o => o.Action == _configureMsOptions);

            AddProtectedWebApi_TestCommon(services, provider);
        }

        private void AddProtectedWebApi_TestCommon(IServiceCollection services, ServiceProvider provider, bool checkDecryptCertificate = true)
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
            var jwtOptions = provider.GetRequiredService<IOptionsFactory<JwtBearerOptions>>().Create(_jwtBearerScheme);

            Assert.NotNull(jwtOptions.Authority);
            Assert.NotNull(jwtOptions.TokenValidationParameters.IssuerValidator);
            if (checkDecryptCertificate)
            {
                Assert.NotNull(jwtOptions.TokenValidationParameters.TokenDecryptionKey);
            }
        }

        [Fact]
        public void AddProtectedWebApi_WithConfigName_JwtBearerTokenValidatedEventCalled()
        {
            var config = Substitute.For<IConfiguration>();
            config.Configure().GetSection(_configSectionName).Returns(_configSection);

            var tokenValidatedFunc = Substitute.For<Func<TokenValidatedContext, Task>>();

            var services = new ServiceCollection()
                .Configure<JwtBearerOptions>(_jwtBearerScheme, (options) =>
                {
                    options.Events ??= new JwtBearerEvents();
                    options.Events.OnTokenValidated += tokenValidatedFunc;
                })
                .AddLogging();

            new AuthenticationBuilder(services)
                .AddMicrosoftWebApi(config, _configSectionName, _jwtBearerScheme, true);

            var provider = services.BuildServiceProvider();

            var jwtOptions = provider.GetRequiredService<IOptionsFactory<JwtBearerOptions>>().Create(_jwtBearerScheme);

            AddProtectedWebApi_TestJwtBearerTokenValidatedEvent(jwtOptions, tokenValidatedFunc);
        }

        [Fact]
        public void AddProtectedWebApi_WithConfigActions_JwtBearerTokenValidatedEventCalled()
        {
            var tokenValidatedFunc = Substitute.For<Func<TokenValidatedContext, Task>>();

            var services = new ServiceCollection()
                .Configure<JwtBearerOptions>(_jwtBearerScheme, (options) =>
                {
                    options.Events ??= new JwtBearerEvents();
                    options.Events.OnTokenValidated += tokenValidatedFunc;
                })
                .AddLogging();

            new AuthenticationBuilder(services)
                .AddMicrosoftWebApi(_configureJwtOptions, _configureMsOptions, _jwtBearerScheme, true);

            var provider = services.BuildServiceProvider();

            var jwtOptions = provider.GetRequiredService<IOptionsFactory<JwtBearerOptions>>().Create(_jwtBearerScheme);

            AddProtectedWebApi_TestJwtBearerTokenValidatedEvent(jwtOptions, tokenValidatedFunc);
        }

        private async void AddProtectedWebApi_TestJwtBearerTokenValidatedEvent(JwtBearerOptions jwtOptions, Func<TokenValidatedContext, Task> tokenValidatedFunc)
        {
            var scopeTypes = new[] { ClaimConstants.Scope, ClaimConstants.Scp, ClaimConstants.Roles, ClaimConstants.Role };
            var expectedExceptionMessage = "Neither scope or roles claim was found in the bearer token.";
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
        public void AddProtectedWebApi_WithConfigName_SubscribesToDiagnostics(bool subscribeToDiagnostics)
        {
            var config = Substitute.For<IConfiguration>();
            config.Configure().GetSection(_configSectionName).Returns(_configSection);

            var diagnostics = Substitute.For<IJwtBearerMiddlewareDiagnostics>();

            var services = new ServiceCollection()
                .AddLogging();

            new AuthenticationBuilder(services)
                .AddMicrosoftWebApi(config, _configSectionName, _jwtBearerScheme, subscribeToDiagnostics);

            services.RemoveAll<IJwtBearerMiddlewareDiagnostics>();
            services.AddSingleton<IJwtBearerMiddlewareDiagnostics>((provider) => diagnostics);

            var provider = services.BuildServiceProvider();

            var jwtOptions = provider.GetRequiredService<IOptionsFactory<JwtBearerOptions>>().Create(_jwtBearerScheme);

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
        public void AddProtectedWebApi_WithConfigActions_SubscribesToDiagnostics(bool subscribeToDiagnostics)
        {
            var diagnostics = Substitute.For<IJwtBearerMiddlewareDiagnostics>();

            var services = new ServiceCollection()
                .AddLogging();

            new AuthenticationBuilder(services)
                .AddMicrosoftWebApi(_configureJwtOptions, _configureMsOptions, _jwtBearerScheme, subscribeToDiagnostics);

            services.RemoveAll<IJwtBearerMiddlewareDiagnostics>();
            services.AddSingleton<IJwtBearerMiddlewareDiagnostics>((provider) => diagnostics);

            var provider = services.BuildServiceProvider();

            var jwtOptions = provider.GetRequiredService<IOptionsFactory<JwtBearerOptions>>().Create(_jwtBearerScheme);

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
                TokenDecryptionCertificatesDescription,
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
        public async Task AddProtectedWebApiCallsProtectedWebApi_WithConfigName()
        {
            var config = Substitute.For<IConfiguration>();
            var tokenValidatedFuncMock = Substitute.For<Func<TokenValidatedContext, Task>>();

            var services = new ServiceCollection()
                .Configure<JwtBearerOptions>(_jwtBearerScheme, (options) =>
                {
                    options.Events ??= new JwtBearerEvents();
                    options.Events.OnTokenValidated += tokenValidatedFuncMock;
                });
            new AuthenticationBuilder(services).AddMicrosoftWebApiCallsWebApi(config, _configSectionName, _jwtBearerScheme);

            var provider = services.BuildServiceProvider();

            // Assert config bind actions added correctly
            provider.GetRequiredService<IOptionsFactory<ConfidentialClientApplicationOptions>>().Create(string.Empty);
            provider.GetRequiredService<IOptionsFactory<MicrosoftIdentityOptions>>().Create(string.Empty);

            config.Received(3).GetSection(_configSectionName);

            await AddProtectedWebApiCallsProtectedWebApi_TestCommon(services, provider, tokenValidatedFuncMock).ConfigureAwait(false);
        }

        [Fact]
        public async Task AddProtectedWebApiCallsProtectedWebApi_WithConfigActions()
        {
            var tokenValidatedFuncMock = Substitute.For<Func<TokenValidatedContext, Task>>();
            var services = new ServiceCollection()
                .Configure<JwtBearerOptions>(_jwtBearerScheme, (options) =>
                {
                    options.Events ??= new JwtBearerEvents();
                    options.Events.OnTokenValidated += tokenValidatedFuncMock;
                });
            new AuthenticationBuilder(services).AddMicrosoftWebApiCallsWebApi(_configureAppOptions, _configureMsOptions, _jwtBearerScheme);

            var provider = services.BuildServiceProvider();

            // Assert configure options actions added correctly
            var configuredAppOptions = provider.GetServices<IConfigureOptions<ConfidentialClientApplicationOptions>>().Cast<ConfigureNamedOptions<ConfidentialClientApplicationOptions>>();
            var configuredMsOptions = provider.GetServices<IConfigureOptions<MicrosoftIdentityOptions>>().Cast<ConfigureNamedOptions<MicrosoftIdentityOptions>>();

            Assert.Contains(configuredAppOptions, o => o.Action == _configureAppOptions);
            Assert.Contains(configuredMsOptions, o => o.Action == _configureMsOptions);

            await AddProtectedWebApiCallsProtectedWebApi_TestCommon(services, provider, tokenValidatedFuncMock).ConfigureAwait(false);
        }

        private async Task AddProtectedWebApiCallsProtectedWebApi_TestCommon(IServiceCollection services, ServiceProvider provider, Func<TokenValidatedContext, Task> tokenValidatedFuncMock)
        {
            // Assert correct services added
            Assert.Contains(services, s => s.ServiceType == typeof(IHttpContextAccessor));
            Assert.Contains(services, s => s.ServiceType == typeof(ITokenAcquisition));
            Assert.Contains(services, s => s.ServiceType == typeof(IConfigureOptions<ConfidentialClientApplicationOptions>));
            Assert.Contains(services, s => s.ServiceType == typeof(IConfigureOptions<MicrosoftIdentityOptions>));
            Assert.Contains(services, s => s.ServiceType == typeof(IConfigureOptions<JwtBearerOptions>));

            // Assert JWT options added correctly
            var configuredJwtOptions = provider.GetService<IConfigureOptions<JwtBearerOptions>>() as ConfigureNamedOptions<JwtBearerOptions>;

            Assert.Equal(_jwtBearerScheme, configuredJwtOptions.Name);

            // Assert token validated event added correctly
            var jwtOptions = provider.GetRequiredService<IOptionsFactory<JwtBearerOptions>>().Create(_jwtBearerScheme);
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
