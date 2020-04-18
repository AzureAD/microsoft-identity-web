// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
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
using Microsoft.IdentityModel.Tokens;
using NSubstitute;
using NSubstitute.Extensions;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    public class WebApiExtensionsTests
    {
        private const string _configSectionName = "AzureAd-Custom";
        private const string _jwtBearerScheme = "Bearer-Custom";
        private readonly X509Certificate2 _certificate = new X509Certificate2(Convert.FromBase64String(TestConstants.CertificateX5c));
        private readonly IConfigurationSection _configSection;
        private readonly Action<ConfidentialClientApplicationOptions> _configureAppOptions = (options) => { };
        private readonly Action<JwtBearerOptions> _configureJwtOptions = (options) => { };
        private readonly Action<MicrosoftIdentityOptions> _configureMsOptions = (options) =>
        {
            options.Instance = TestConstants.AadInstance;
            options.TenantId = TestConstants.TenantIdAsGuid;
            options.ClientId = TestConstants.ClientId;
        };

        public WebApiExtensionsTests()
        {
            _configSection = GetConfigSection(_configSectionName);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void AddProtectedWebApi_WithConfigName(bool useServiceCollectionExtension)
        {
            var config = Substitute.For<IConfiguration>();
            config.Configure().GetSection(_configSectionName).Returns(_configSection);

            var services = new ServiceCollection()
                .AddLogging();

            if (useServiceCollectionExtension)
            {
                services.AddProtectedWebApi(config, _configSectionName, _jwtBearerScheme, _certificate, true);
            }
            else
            {
                new AuthenticationBuilder(services)
                    .AddProtectedWebApi(config, _configSectionName, _jwtBearerScheme, _certificate, true);
            }

            var provider = services.BuildServiceProvider();

            // Config bind actions added correctly
            provider.GetRequiredService<IOptionsFactory<JwtBearerOptions>>().Create(_jwtBearerScheme);
            provider.GetRequiredService<IOptionsFactory<MicrosoftIdentityOptions>>().Create(string.Empty);
            config.Received(3).GetSection(_configSectionName);

            AddProtectedWebApi_TestCommon(services, provider);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void AddProtectedWebApi_WithConfigActions(bool useServiceCollectionExtension)
        {
            var services = new ServiceCollection()
                .AddLogging();

            if (useServiceCollectionExtension)
            {
                services.AddProtectedWebApi(_configureJwtOptions, _configureMsOptions, _certificate, _jwtBearerScheme, true);
            }
            else
            {
                new AuthenticationBuilder(services)
                    .AddProtectedWebApi(_configureJwtOptions, _configureMsOptions, _certificate, _jwtBearerScheme, true);
            }

            var provider = services.BuildServiceProvider();

            // Configure options actions added correctly
            var configuredJwtOptions = provider.GetServices<IConfigureOptions<JwtBearerOptions>>().Cast<ConfigureNamedOptions<JwtBearerOptions>>();
            var configuredMsOptions = provider.GetService<IConfigureOptions<MicrosoftIdentityOptions>>() as ConfigureNamedOptions<MicrosoftIdentityOptions>;

            Assert.Contains(configuredJwtOptions, o => o.Action == _configureJwtOptions);
            Assert.Same(_configureMsOptions, configuredMsOptions.Action);

            AddProtectedWebApi_TestCommon(services, provider);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void AddProtectedWebApi_WithConfigName_JwtBearerTokenValidatedEventCalled(bool useServiceCollectionExtension)
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

            if (useServiceCollectionExtension)
            {
                services.AddProtectedWebApi(config, _configSectionName, _jwtBearerScheme, _certificate, true);
            }
            else
            {
                new AuthenticationBuilder(services)
                    .AddProtectedWebApi(config, _configSectionName, _jwtBearerScheme, _certificate, true);
            }

            var provider = services.BuildServiceProvider();

            var jwtOptions = provider.GetRequiredService<IOptionsFactory<JwtBearerOptions>>().Create(_jwtBearerScheme);

            AddProtectedWebApi_TestJwtBearerTokenValidatedEvent(jwtOptions, tokenValidatedFunc);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void AddProtectedWebApi_WithConfigActions_JwtBearerTokenValidatedEventCalled(bool useServiceCollectionExtension)
        {
            var tokenValidatedFunc = Substitute.For<Func<TokenValidatedContext, Task>>();

            var services = new ServiceCollection()
                .Configure<JwtBearerOptions>(_jwtBearerScheme, (options) =>
                {
                    options.Events ??= new JwtBearerEvents();
                    options.Events.OnTokenValidated += tokenValidatedFunc;
                })
                .AddLogging();

            if (useServiceCollectionExtension)
            {
                services.AddProtectedWebApi(_configureJwtOptions, _configureMsOptions, _certificate, _jwtBearerScheme, true);
            }
            else
            {
                new AuthenticationBuilder(services)
                    .AddProtectedWebApi(_configureJwtOptions, _configureMsOptions, _certificate, _jwtBearerScheme, true);
            }

            var provider = services.BuildServiceProvider();

            var jwtOptions = provider.GetRequiredService<IOptionsFactory<JwtBearerOptions>>().Create(_jwtBearerScheme);

            AddProtectedWebApi_TestJwtBearerTokenValidatedEvent(jwtOptions, tokenValidatedFunc);
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public void AddProtectedWebApi_WithConfigName_SubscribesToDiagnostics(bool useServiceCollectionExtension, bool subscribeToDiagnostics)
        {
            var config = Substitute.For<IConfiguration>();
            config.Configure().GetSection(_configSectionName).Returns(_configSection);

            var diagnostics = Substitute.For<IJwtBearerMiddlewareDiagnostics>();

            var services = new ServiceCollection()
                .AddLogging();

            if (useServiceCollectionExtension)
            {
                services.AddProtectedWebApi(config, _configSectionName, _jwtBearerScheme, _certificate, subscribeToDiagnostics);
            }
            else
            {
                new AuthenticationBuilder(services)
                    .AddProtectedWebApi(config, _configSectionName, _jwtBearerScheme, _certificate, subscribeToDiagnostics);
            }

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
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public void AddProtectedWebApi_WithConfigActions_SubscribesToDiagnostics(bool useServiceCollectionExtension, bool subscribeToDiagnostics)
        {
            var diagnostics = Substitute.For<IJwtBearerMiddlewareDiagnostics>();

            var services = new ServiceCollection()
                .AddLogging();

            if (useServiceCollectionExtension)
            {
                services.AddProtectedWebApi(_configureJwtOptions, _configureMsOptions, _certificate, _jwtBearerScheme, subscribeToDiagnostics);
            }
            else
            {
                new AuthenticationBuilder(services)
                    .AddProtectedWebApi(_configureJwtOptions, _configureMsOptions, _certificate, _jwtBearerScheme, subscribeToDiagnostics);
            }

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

        [Fact]
        public void AddProtectedWebApiCallsProtectedWebApi_WithConfigName()
        {
            var config = Substitute.For<IConfiguration>();

            var services = new ServiceCollection()
                .AddProtectedWebApiCallsProtectedWebApi(config, _configSectionName, _jwtBearerScheme);
            var provider = services.BuildServiceProvider();

            // Config bind actions added correctly
            provider.GetRequiredService<IOptionsFactory<ConfidentialClientApplicationOptions>>().Create(string.Empty);
            provider.GetRequiredService<IOptionsFactory<MicrosoftIdentityOptions>>().Create(string.Empty);

            config.Received(2).GetSection(_configSectionName);

            AddProtectedWebApiCallsProtectedWebApi_TestCommon(services, provider);
        }

        [Fact]
        public void AddProtectedWebApiCallsProtectedWebApi_WithConfigActions()
        {
            var services = new ServiceCollection()
                .AddProtectedWebApiCallsProtectedWebApi(_configureAppOptions, _configureMsOptions, _jwtBearerScheme);
            var provider = services.BuildServiceProvider();

            // Configure options actions added correctly
            var configuredAppOptions = provider.GetService<IConfigureOptions<ConfidentialClientApplicationOptions>>() as ConfigureNamedOptions<ConfidentialClientApplicationOptions>;
            var configuredMsOptions = provider.GetService<IConfigureOptions<MicrosoftIdentityOptions>>() as ConfigureNamedOptions<MicrosoftIdentityOptions>;

            Assert.Same(_configureMsOptions, configuredMsOptions.Action);
            Assert.Same(_configureAppOptions, configuredAppOptions.Action);

            AddProtectedWebApiCallsProtectedWebApi_TestCommon(services, provider);
        }

        [Theory]
        [InlineData(TestConstants.AuthorityCommonTenant, TestConstants.AuthorityCommonTenantWithV2)]
        [InlineData(TestConstants.AuthorityOrganizationsUSTenant, TestConstants.AuthorityOrganizationsUSWithV2)]
        [InlineData(TestConstants.AuthorityCommonTenantWithV2, TestConstants.AuthorityCommonTenantWithV2)]
        [InlineData(TestConstants.AuthorityCommonTenantWithV2 + "/", TestConstants.AuthorityCommonTenantWithV2)]
        [InlineData(TestConstants.B2CAuthorityWithV2, TestConstants.B2CAuthorityWithV2)]
        [InlineData(TestConstants.B2CCustomDomainAuthorityWithV2, TestConstants.B2CCustomDomainAuthorityWithV2)]
        [InlineData(TestConstants.B2CAuthority, TestConstants.B2CAuthorityWithV2)]
        [InlineData(TestConstants.B2CCustomDomainAuthority, TestConstants.B2CCustomDomainAuthorityWithV2)]
        public void EnsureAuthorityIsV2_0(string initialAuthority, string expectedAuthority)
        {
            JwtBearerOptions options = new JwtBearerOptions
            {
                Authority = initialAuthority
            };

            options.Authority = AuthorityHelpers.EnsureAuthorityIsV2(options.Authority);
            Assert.Equal(expectedAuthority, options.Authority);
        }

        [Theory]
        [InlineData(TestConstants.HttpLocalHost, null, new string[] { TestConstants.HttpLocalHost })]
        [InlineData(TestConstants.ApiAudience, null, new string[] { TestConstants.ApiAudience })]
        [InlineData(TestConstants.ApiClientId, null, new string[] { TestConstants.ApiAudience, TestConstants.ApiClientId })]
        [InlineData("", null, new string[] { TestConstants.ApiAudience, TestConstants.ApiClientId })]
        [InlineData(null, null, new string[] { TestConstants.ApiAudience, TestConstants.ApiClientId })]
        [InlineData(null, new string[] { TestConstants.ApiAudience }, new string[] { TestConstants.ApiAudience, TestConstants.ApiAudience, TestConstants.ApiClientId })]
        [InlineData(null, new string[] { TestConstants.ApiClientId }, new string[] { TestConstants.ApiAudience, TestConstants.ApiClientId, TestConstants.ApiClientId })]
        [InlineData(TestConstants.HttpLocalHost, new string[] { TestConstants.B2CCustomDomainInstance }, new string[] { TestConstants.HttpLocalHost, TestConstants.B2CCustomDomainInstance })]
        [InlineData(TestConstants.ApiAudience, new string[] { TestConstants.B2CCustomDomainInstance }, new string[] { TestConstants.ApiAudience, TestConstants.B2CCustomDomainInstance })]
        [InlineData(TestConstants.ApiClientId, new string[] { TestConstants.B2CCustomDomainInstance }, new string[] { TestConstants.ApiAudience, TestConstants.ApiClientId, TestConstants.B2CCustomDomainInstance })]
        public void EnsureValidAudiencesContainsApiGuidIfGuidProvided(string initialAudience, string[] initialAudiences, string[] expectedAudiences)
        {
            JwtBearerOptions jwtOptions = new JwtBearerOptions()
            {
                Audience = initialAudience,
                TokenValidationParameters = new TokenValidationParameters()
                {
                    ValidAudiences = initialAudiences
                }
            };
            MicrosoftIdentityOptions msIdentityOptions = new MicrosoftIdentityOptions()
            {
                ClientId = TestConstants.ApiClientId
            };

            WebApiAuthenticationBuilderExtensions.EnsureValidAudiencesContainsApiGuidIfGuidProvided(jwtOptions, msIdentityOptions);

            Assert.Equal(expectedAudiences.Length, jwtOptions.TokenValidationParameters.ValidAudiences.Count());
            Assert.Equal(expectedAudiences.OrderBy(x => x), jwtOptions.TokenValidationParameters.ValidAudiences.OrderBy(x => x));
        }

        private void AddProtectedWebApi_TestCommon(IServiceCollection services, ServiceProvider provider)
        {
            // Correct services added
            Assert.Contains(services, s => s.ServiceType == typeof(IHttpContextAccessor));
            Assert.Contains(services, s => s.ServiceType == typeof(IConfigureOptions<MicrosoftIdentityOptions>));
            Assert.Contains(services, s => s.ServiceType == typeof(IConfigureOptions<JwtBearerOptions>));
            Assert.Contains(services, s => s.ServiceType == typeof(IJwtBearerMiddlewareDiagnostics));
            Assert.Equal(ServiceLifetime.Singleton, services.First(s => s.ServiceType == typeof(IJwtBearerMiddlewareDiagnostics)).Lifetime);

            // JWT options added correctly
            var configuredJwtOptions = provider.GetService<IConfigureOptions<JwtBearerOptions>>() as ConfigureNamedOptions<JwtBearerOptions>;

            Assert.Equal(_jwtBearerScheme, configuredJwtOptions.Name);

            // Issuer validator and certificate set
            var jwtOptions = provider.GetRequiredService<IOptionsFactory<JwtBearerOptions>>().Create(_jwtBearerScheme);

            Assert.NotNull(jwtOptions.TokenValidationParameters.IssuerValidator);
            Assert.NotNull(jwtOptions.TokenValidationParameters.TokenDecryptionKey);
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
                        new Claim(scopeType, scopeValue)
                    }));
                await jwtOptions.Events.TokenValidated(tokenValidatedContext).ConfigureAwait(false);
            }

            await tokenValidatedFunc.Received(4).Invoke(Arg.Any<TokenValidatedContext>()).ConfigureAwait(false);
        }

        private void AddProtectedWebApiCallsProtectedWebApi_TestCommon(IServiceCollection services, ServiceProvider provider)
        {
            // Correct services added
            Assert.Contains(services, s => s.ServiceType == typeof(IHttpContextAccessor));
            Assert.Contains(services, s => s.ServiceType == typeof(ITokenAcquisition));
            Assert.Contains(services, s => s.ServiceType == typeof(IConfigureOptions<ConfidentialClientApplicationOptions>));
            Assert.Contains(services, s => s.ServiceType == typeof(IConfigureOptions<MicrosoftIdentityOptions>));
            Assert.Contains(services, s => s.ServiceType == typeof(IConfigureOptions<JwtBearerOptions>));

            // JWT options added correctly
            var configuredJwtOptions = provider.GetService<IConfigureOptions<JwtBearerOptions>>() as ConfigureNamedOptions<JwtBearerOptions>;

            Assert.Equal(_jwtBearerScheme, configuredJwtOptions.Name);

            // Token validated event added correctly
            var jwtOptions = provider.GetRequiredService<IOptionsFactory<JwtBearerOptions>>().Create(_jwtBearerScheme);
            var httpContext = HttpContextUtilities.CreateHttpContext();
            var authScheme = new AuthenticationScheme(JwtBearerDefaults.AuthenticationScheme, JwtBearerDefaults.AuthenticationScheme, typeof(JwtBearerHandler));
            var tokenValidatedContext = new TokenValidatedContext(httpContext, authScheme, jwtOptions) { SecurityToken = new JwtSecurityToken() };

            jwtOptions.Events.TokenValidated(tokenValidatedContext);

            Assert.NotNull(httpContext.GetTokenUsedToCallWebAPI());
        }

        private IConfigurationSection GetConfigSection(string configSectionName)
        {
            var configAsDictionary = new Dictionary<string, string>()
            {
                { configSectionName, null },
                { $"{configSectionName}:Instance", TestConstants.AadInstance },
                { $"{configSectionName}:TenantId", TestConstants.TenantIdAsGuid },
                { $"{configSectionName}:ClientId", TestConstants.TenantIdAsGuid }
            };
            var memoryConfigSource = new MemoryConfigurationSource { InitialData = configAsDictionary };
            var configBuilder = new ConfigurationBuilder();
            configBuilder.Add(memoryConfigSource);
            var configSection = configBuilder.Build().GetSection(configSectionName);
            return configSection;
        }
    }
}