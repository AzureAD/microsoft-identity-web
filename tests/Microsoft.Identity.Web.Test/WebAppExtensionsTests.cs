// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web.Resource;
using Microsoft.Identity.Web.Test.Common;
using Microsoft.Identity.Web.Test.Common.TestHelpers;
using Microsoft.Identity.Web.TokenCacheProviders;
using Microsoft.Identity.Web.TokenCacheProviders.InMemory;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using NSubstitute;
using NSubstitute.Extensions;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    public class WebAppExtensionsTests
    {
        private const string OidcScheme = "OpenIdConnect-Custom";
        private const string CookieScheme = "Cookies-Custom";

        private const string ConfigSectionName = "AzureAd-Custom";
        private IConfigurationSection _configSection;
        private readonly Action<ConfidentialClientApplicationOptions> _configureAppOptions = (options) => { };
        private readonly IHostEnvironment _env;

        private Action<MicrosoftIdentityOptions> _configureMsOptions = (options) =>
        {
            options.Instance = TestConstants.AadInstance;
            options.TenantId = TestConstants.TenantIdAsGuid;
            options.ClientId = TestConstants.ClientId;
        };

        private readonly Action<MicrosoftGraphOptions> _configureMicrosoftGraphOptions = (options) =>
        {
            options.BaseUrl = TestConstants.GraphBaseUrlBeta;
            options.Scopes = TestConstants.GraphScopes;
        };

        private readonly Action<CookieAuthenticationOptions> _configureCookieOptions = (options) => { };

        public WebAppExtensionsTests()
        {
            _configSection = GetConfigSection(ConfigSectionName);
            _env = new HostingEnvironment { EnvironmentName = Environments.Development };
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void AddMicrosoftIdentityWebApp_WithConfigNameParameters(bool subscribeToDiagnostics)
        {
            var configMock = Substitute.For<IConfiguration>();
            configMock.Configure().GetSection(ConfigSectionName).Returns(_configSection);

            var diagnosticsMock = Substitute.For<IOpenIdConnectMiddlewareDiagnostics>();

            var services = new ServiceCollection();

            services.AddDataProtection();
            services.AddSingleton((provider) => _env);

            new AuthenticationBuilder(services)
                .AddMicrosoftIdentityWebApp(configMock, ConfigSectionName, OidcScheme, CookieScheme, subscribeToDiagnostics);

            var provider = services.BuildServiceProvider();

            // Assert config bind actions added correctly
            provider.GetRequiredService<IOptionsFactory<OpenIdConnectOptions>>().Create(OidcScheme);
            provider.GetRequiredService<IOptionsFactory<MicrosoftIdentityOptions>>().Create(string.Empty);
            configMock.Received(1).GetSection(ConfigSectionName);

            AddMicrosoftIdentityWebApp_TestCommon(services, provider);
            AddMicrosoftIdentityWebApp_TestSubscribesToDiagnostics(services, diagnosticsMock, subscribeToDiagnostics);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void AddMicrosoftIdentityWebAppAuthentication_WithConfigNameParameters(bool subscribeToDiagnostics)
        {
            var configMock = Substitute.For<IConfiguration>();
            configMock.Configure().GetSection(ConfigSectionName).Returns(_configSection);

            var diagnosticsMock = Substitute.For<IOpenIdConnectMiddlewareDiagnostics>();

            var services = new ServiceCollection();

            services.AddDataProtection();
            services.AddSingleton((provider) => _env);

            services.AddMicrosoftIdentityWebAppAuthentication(
                configMock,
                ConfigSectionName,
                OidcScheme,
                CookieScheme,
                subscribeToDiagnostics);

            var provider = services.BuildServiceProvider();

            // Assert config bind actions added correctly
            provider.GetRequiredService<IOptionsFactory<OpenIdConnectOptions>>().Create(OidcScheme);
            provider.GetRequiredService<IOptionsFactory<MicrosoftIdentityOptions>>().Create(string.Empty);
            configMock.Received(1).GetSection(ConfigSectionName);

            AddMicrosoftIdentityWebApp_TestCommon(services, provider);
            AddMicrosoftIdentityWebApp_TestSubscribesToDiagnostics(services, diagnosticsMock, subscribeToDiagnostics);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void AddMicrosoftIdentityWebApp_WithConfigActionParameters(bool subscribeToDiagnostics)
        {
            var diagnosticsMock = Substitute.For<IOpenIdConnectMiddlewareDiagnostics>();

            var services = new ServiceCollection();
            services.AddDataProtection();
            services.AddSingleton((provider) => _env);

            new AuthenticationBuilder(services)
                .AddMicrosoftIdentityWebApp(_configureMsOptions, _configureCookieOptions, OidcScheme, CookieScheme, subscribeToDiagnostics);

            var provider = services.BuildServiceProvider();

            // Assert configure options actions added correctly
            var configuredMsOptions = provider.GetServices<IConfigureOptions<MicrosoftIdentityOptions>>().Cast<ConfigureNamedOptions<MicrosoftIdentityOptions>>();

#if DOTNET_CORE_31
            var configuredCookieOptions = provider.GetServices<IConfigureOptions<CookieAuthenticationOptions>>().Cast<ConfigureNamedOptions<CookieAuthenticationOptions>>();

            Assert.Contains(configuredCookieOptions, o => o.Action == _configureCookieOptions);
#endif

            Assert.Contains(configuredMsOptions, o => o.Action == _configureMsOptions);

            AddMicrosoftIdentityWebApp_TestCommon(services, provider);
            AddMicrosoftIdentityWebApp_TestSubscribesToDiagnostics(services, diagnosticsMock, subscribeToDiagnostics);
        }

        [Fact]
        public async Task AddMicrosoftIdentityWebApp_WithConfigNameParameters_TestRedirectToIdentityProviderEvent()
        {
            var configMock = Substitute.For<IConfiguration>();
            configMock.Configure().GetSection(ConfigSectionName).Returns(_configSection);

            var redirectFunc = Substitute.For<Func<RedirectContext, Task>>();
            var services = new ServiceCollection()
                .PostConfigure<MicrosoftIdentityOptions>((options) =>
                {
                    options.Events ??= new OpenIdConnectEvents();
                    options.Events.OnRedirectToIdentityProvider += redirectFunc;
                });
            services.AddDataProtection();
            services.AddSingleton((provider) => _env);

            new AuthenticationBuilder(services)
                .AddMicrosoftIdentityWebApp(configMock, ConfigSectionName, OidcScheme, CookieScheme, false);

            await AddMicrosoftIdentityWebApp_TestRedirectToIdentityProviderEvent(services, redirectFunc).ConfigureAwait(false);
        }

        [Fact]
        public async Task AddMicrosoftIdentityWebApp_WithConfigActionParameters_TestRedirectToIdentityProviderEvent()
        {
            var redirectFunc = Substitute.For<Func<RedirectContext, Task>>();
            var services = new ServiceCollection()
                .PostConfigure<MicrosoftIdentityOptions>((options) =>
                {
                    options.Events ??= new OpenIdConnectEvents();
                    options.Events.OnRedirectToIdentityProvider += redirectFunc;
                });

            services.AddDataProtection();
            services.AddSingleton((provider) => _env);

            new AuthenticationBuilder(services)
                    .AddMicrosoftIdentityWebApp(_configureMsOptions, _configureCookieOptions, OidcScheme, CookieScheme, false);

            await AddMicrosoftIdentityWebApp_TestRedirectToIdentityProviderEvent(services, redirectFunc).ConfigureAwait(false);
        }

        [Fact]
        public async Task AddMicrosoftIdentityWebApp_WithConfigNameParameters_TestB2cSpecificSetup()
        {
            var configMock = Substitute.For<IConfiguration>();
            _configSection = GetConfigSection(ConfigSectionName, true);
            configMock.Configure().GetSection(ConfigSectionName).Returns(_configSection);

            var remoteFailureFuncMock = Substitute.For<Func<RemoteFailureContext, Task>>();
            var services = new ServiceCollection()
                .PostConfigure<MicrosoftIdentityOptions>((options) =>
                {
                    options.Events ??= new OpenIdConnectEvents();
                    options.Events.OnRemoteFailure += remoteFailureFuncMock;
                });
            services.AddDataProtection();
            services.AddSingleton((provider) => _env);

            new AuthenticationBuilder(services)
                .AddMicrosoftIdentityWebApp(configMock, ConfigSectionName, OidcScheme, CookieScheme, false);

            await AddMicrosoftIdentityWebApp_TestB2cSpecificSetup(services, remoteFailureFuncMock).ConfigureAwait(false);
        }

        [Fact]
        public async Task AddMicrosoftIdentityWebApp_WithConfigActionParameters_B2cSpecificSetup()
        {
            _configureMsOptions = (options) =>
            {
                options.Instance = TestConstants.B2CInstance;
                options.TenantId = TestConstants.TenantIdAsGuid;
                options.ClientId = TestConstants.ClientId;
                options.SignUpSignInPolicyId = TestConstants.B2CSignUpSignInUserFlow;
                options.Domain = TestConstants.B2CTenant;
            };

            var remoteFailureFuncMock = Substitute.For<Func<RemoteFailureContext, Task>>();
            var services = new ServiceCollection()
                .PostConfigure<MicrosoftIdentityOptions>((options) =>
                {
                    options.Events ??= new OpenIdConnectEvents();
                    options.Events.OnRemoteFailure += remoteFailureFuncMock;
                });
            services.AddDataProtection();
            services.AddSingleton((provider) => _env);

            new AuthenticationBuilder(services)
                .AddMicrosoftIdentityWebApp(_configureMsOptions, _configureCookieOptions, OidcScheme, CookieScheme, false);

            await AddMicrosoftIdentityWebApp_TestB2cSpecificSetup(services, remoteFailureFuncMock).ConfigureAwait(false);
        }

        [Fact]
        public async Task AddMicrosoftIdentityWebAppCallsWebApi_WithConfigNameParameters()
        {
            var configMock = Substitute.For<IConfiguration>();
            configMock.Configure().GetSection(ConfigSectionName).Returns(_configSection);
            var initialScopes = new List<string>() { "custom_scope" };
            var tokenAcquisitionMock = Substitute.For<ITokenAcquisitionInternal>();
            var authCodeReceivedFuncMock = Substitute.For<Func<AuthorizationCodeReceivedContext, Task>>();
            var tokenValidatedFuncMock = Substitute.For<Func<TokenValidatedContext, Task>>();
            var redirectFuncMock = Substitute.For<Func<RedirectContext, Task>>();
            var services = new ServiceCollection();

            services.AddSingleton((provider) => _env);

            services.AddAuthentication()
                .AddMicrosoftIdentityWebApp(configMock, ConfigSectionName, OidcScheme)
                .EnableTokenAcquisitionToCallDownstreamApi(initialScopes);
            services.Configure<OpenIdConnectOptions>(OidcScheme, (options) =>
            {
                options.Events ??= new OpenIdConnectEvents();
                options.Events.OnAuthorizationCodeReceived += authCodeReceivedFuncMock;
                options.Events.OnTokenValidated += tokenValidatedFuncMock;
                options.Events.OnRedirectToIdentityProviderForSignOut += redirectFuncMock;
            });

            services.RemoveAll<ITokenAcquisition>();
            services.AddScoped<ITokenAcquisition>((provider) => tokenAcquisitionMock);

            var provider = services.BuildServiceProvider();

            // Assert config bind actions added correctly
            provider.GetRequiredService<IOptionsFactory<ConfidentialClientApplicationOptions>>().Create(string.Empty);
            provider.GetRequiredService<IOptionsFactory<MicrosoftIdentityOptions>>().Create(string.Empty);

            configMock.Received(1).GetSection(ConfigSectionName);

            var oidcOptions = provider.GetRequiredService<IOptionsFactory<OpenIdConnectOptions>>().Create(OidcScheme);

            AddMicrosoftIdentityWebAppCallsWebApi_TestCommon(services, provider, oidcOptions, initialScopes);
            await AddMicrosoftIdentityWebAppCallsWebApi_TestAuthorizationCodeReceivedEvent(provider, oidcOptions, authCodeReceivedFuncMock, tokenAcquisitionMock).ConfigureAwait(false);
            await AddMicrosoftIdentityWebAppCallsWebApi_TestTokenValidatedEvent(provider, oidcOptions, tokenValidatedFuncMock).ConfigureAwait(false);
            await AddMicrosoftIdentityWebAppCallsWebApi_TestRedirectToIdentityProviderForSignOutEvent(provider, oidcOptions, redirectFuncMock, tokenAcquisitionMock).ConfigureAwait(false);
        }

        [Fact]
        public async Task AddMicrosoftIdentityWebAppCallsWebApi_WithConfigActionParameters()
        {
            var initialScopes = new List<string>() { "custom_scope" };
            var tokenAcquisitionMock = Substitute.For<ITokenAcquisitionInternal>();
            var authCodeReceivedFuncMock = Substitute.For<Func<AuthorizationCodeReceivedContext, Task>>();
            var tokenValidatedFuncMock = Substitute.For<Func<TokenValidatedContext, Task>>();
            var redirectFuncMock = Substitute.For<Func<RedirectContext, Task>>();

            var services = new ServiceCollection();
            services.AddSingleton((provider) => _env);

            var builder = services.AddAuthentication()
                .AddMicrosoftIdentityWebApp(_configureMsOptions, null, OidcScheme)
                .EnableTokenAcquisitionToCallDownstreamApi(_configureAppOptions, initialScopes);
            services.Configure<OpenIdConnectOptions>(OidcScheme, (options) =>
            {
                options.Events ??= new OpenIdConnectEvents();
                options.Events.OnAuthorizationCodeReceived += authCodeReceivedFuncMock;
                options.Events.OnTokenValidated += tokenValidatedFuncMock;
                options.Events.OnRedirectToIdentityProviderForSignOut += redirectFuncMock;
            });

            services.RemoveAll<ITokenAcquisition>();
            services.AddScoped<ITokenAcquisition>((provider) => tokenAcquisitionMock);

            var provider = builder.Services.BuildServiceProvider();

            // Assert configure options actions added correctly
            var configuredAppOptions = provider.GetServices<IConfigureOptions<ConfidentialClientApplicationOptions>>().Cast<ConfigureNamedOptions<ConfidentialClientApplicationOptions>>();
            var configuredMsOptions = provider.GetServices<IConfigureOptions<MicrosoftIdentityOptions>>().Cast<ConfigureNamedOptions<MicrosoftIdentityOptions>>();

            Assert.Contains(configuredAppOptions, o => o.Action == _configureAppOptions);
            Assert.Contains(configuredMsOptions, o => o.Action == _configureMsOptions);

            var oidcOptions = provider.GetRequiredService<IOptionsFactory<OpenIdConnectOptions>>().Create(OidcScheme);

            AddMicrosoftIdentityWebAppCallsWebApi_TestCommon(services, provider, oidcOptions, initialScopes);
            await AddMicrosoftIdentityWebAppCallsWebApi_TestAuthorizationCodeReceivedEvent(provider, oidcOptions, authCodeReceivedFuncMock, tokenAcquisitionMock).ConfigureAwait(false);
            await AddMicrosoftIdentityWebAppCallsWebApi_TestTokenValidatedEvent(provider, oidcOptions, tokenValidatedFuncMock).ConfigureAwait(false);
            await AddMicrosoftIdentityWebAppCallsWebApi_TestRedirectToIdentityProviderForSignOutEvent(provider, oidcOptions, redirectFuncMock, tokenAcquisitionMock).ConfigureAwait(false);
        }

        [Fact]
        public void AddMicrosoftIdentityWebAppCallsWebApi_NoScopes()
        {
            // Arrange & Act
            var services = new ServiceCollection();

            services.AddAuthentication()
                .AddMicrosoftIdentityWebApp(Substitute.For<IConfiguration>())
                .EnableTokenAcquisitionToCallDownstreamApi();

            var provider = services.BuildServiceProvider();

            var oidcOptions = provider.GetRequiredService<IOptionsFactory<OpenIdConnectOptions>>().Create(OidcScheme);

            // Assert
            Assert.Equal(OpenIdConnectResponseType.IdToken, oidcOptions.ResponseType);
            Assert.Contains(OidcConstants.ScopeOpenId, oidcOptions.Scope);
            Assert.Contains(OidcConstants.ScopeProfile, oidcOptions.Scope);
        }

        [Theory]
        [InlineData("http://localhost:123")]
        [InlineData("https://localhost:123")]
        public async Task AddMicrosoftIdentityWebApp_RedirectUri(string expectedUri)
        {
            _configureMsOptions = (options) =>
            {
                options.Instance = TestConstants.AadInstance;
                options.TenantId = TestConstants.TenantIdAsGuid;
                options.ClientId = TestConstants.ClientId;
            };

            var services = new ServiceCollection();
            services.AddSingleton((provider) => _env);
            services.AddDataProtection();
            new AuthenticationBuilder(services)
                .AddMicrosoftIdentityWebApp(_configureMsOptions, _configureCookieOptions, OidcScheme, CookieScheme);

            var provider = services.BuildServiceProvider();

            var oidcOptions = provider.GetRequiredService<IOptionsFactory<OpenIdConnectOptions>>().Create(OidcScheme);

            var (httpContext, authScheme, authProperties) = CreateContextParameters(provider);
            var redirectContext = new RedirectContext(httpContext, authScheme, oidcOptions, authProperties)
            {
                ProtocolMessage = new OpenIdConnectMessage()
                {
                    RedirectUri = expectedUri,
                },
            };

            await oidcOptions.Events.RedirectToIdentityProvider(redirectContext).ConfigureAwait(false);
            await oidcOptions.Events.RedirectToIdentityProviderForSignOut(redirectContext).ConfigureAwait(false);

            Assert.Equal(expectedUri, redirectContext.ProtocolMessage.RedirectUri);
        }

        [Fact]
        public void AddMicrosoftIdentityWebApp_AddsInMemoryTokenCaches()
        {
            var services = new ServiceCollection();
            services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
                .AddMicrosoftIdentityWebApp(_configureMsOptions)
                .EnableTokenAcquisitionToCallDownstreamApi(_configureAppOptions)
                .AddInMemoryTokenCaches(
                    options => options.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1),
                    memoryCacheOptions: options => { options.SizeLimit = (long)1e9; });

            // Assert correct services added
            Assert.Contains(services, s => s.ServiceType == typeof(IConfigureOptions<MsalMemoryTokenCacheOptions>));
            Assert.Contains(services, s => s.ServiceType == typeof(IConfigureOptions<MemoryCacheOptions>));
            Assert.Contains(services, s => s.ServiceType == typeof(IMemoryCache));
            Assert.Contains(services, s => s.ServiceType == typeof(IMsalTokenCacheProvider));
        }

        [Fact]
        public void AddMicrosoftGraphUsingFactoryFunction()
        {
            var configMock = Substitute.For<IConfiguration>();
            configMock.Configure().GetSection(ConfigSectionName).Returns(_configSection);

            var services = new ServiceCollection();
            var builder = services.AddAuthentication()
                .AddMicrosoftIdentityWebApp(configMock, ConfigSectionName)
                .EnableTokenAcquisitionToCallDownstreamApi()
                .AddInMemoryTokenCaches();
            string[] initialScopes = new string[] { };

            builder.AddMicrosoftGraph(authProvider => new GraphServiceClient(authProvider), initialScopes);
        }

        [Fact]
        public void AddMicrosoftGraphAppOnlyUsingFactoryFunction()
        {
            var configMock = Substitute.For<IConfiguration>();
            configMock.Configure().GetSection(ConfigSectionName).Returns(_configSection);

            var services = new ServiceCollection();
            var builder = services.AddAuthentication()
                .AddMicrosoftIdentityWebApp(configMock, ConfigSectionName)
                .EnableTokenAcquisitionToCallDownstreamApi()
                .AddInMemoryTokenCaches();
            string[] initialScopes = new string[] { };

            builder.AddMicrosoftGraphAppOnly(authProvider => new GraphServiceClient(authProvider));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void AddMicrosoftGraphOptionsTest(bool useMSGraphOptions)
        {
            var configMock = Substitute.For<IConfiguration>();
            configMock.Configure().GetSection(ConfigSectionName).Returns(_configSection);

            var services = new ServiceCollection();
            var builder = services.AddAuthentication()
                .AddMicrosoftIdentityWebApp(configMock, ConfigSectionName)
                .EnableTokenAcquisitionToCallDownstreamApi()
                .AddInMemoryTokenCaches();

            if (useMSGraphOptions)
            {
                builder.AddMicrosoftGraph(_configureMicrosoftGraphOptions);
            }
            else
            {
                builder.AddMicrosoftGraph();
            }

            var provider = services.BuildServiceProvider();
            // Assert correct services added
            Assert.Contains(services, s => s.ServiceType == typeof(IConfigureOptions<OpenIdConnectOptions>));
            Assert.Contains(services, s => s.ServiceType == typeof(IConfigureOptions<MicrosoftIdentityOptions>));
            Assert.Contains(services, s => s.ServiceType == typeof(IConfigureOptions<MicrosoftGraphOptions>));
            Assert.Equal(ServiceLifetime.Singleton, services.First(s => s.ServiceType == typeof(MicrosoftIdentityIssuerValidatorFactory)).Lifetime);

            // Assert properties set
            var msGraphOptions = provider.GetRequiredService<IOptions<MicrosoftGraphOptions>>();
            GraphServiceClient graphServiceClient = provider.GetRequiredService<GraphServiceClient>();

            if (useMSGraphOptions)
            {
                Assert.Equal(TestConstants.GraphBaseUrlBeta, msGraphOptions.Value.BaseUrl);
                Assert.Equal(TestConstants.GraphScopes, msGraphOptions.Value.Scopes);
                Assert.Equal(msGraphOptions.Value.BaseUrl, graphServiceClient.BaseUrl);
            }
            else
            {
                Assert.Equal(Constants.GraphBaseUrlV1, msGraphOptions.Value.BaseUrl);
                Assert.Equal(Constants.UserReadScope, msGraphOptions.Value.Scopes);
                Assert.Equal(msGraphOptions.Value.BaseUrl, graphServiceClient.BaseUrl);
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void AddDownstreamWebApiOptionsTest(bool useDownstreamWebApiOptions)
        {
            Action<DownstreamWebApiOptions> configureDownstreamWebApiOptions = (options) =>
           {
               options.BaseUrl = TestConstants.GraphBaseUrlBeta;
               options.Scopes = TestConstants.Scopes;
               options.Tenant = TestConstants.TenantIdAsGuid;
           };

            var configMock = Substitute.For<IConfiguration>();
            configMock.Configure().GetSection(ConfigSectionName).Returns(_configSection);

            var services = new ServiceCollection();
            var builder = services.AddAuthentication()
                .AddMicrosoftIdentityWebApp(configMock, ConfigSectionName)
                .EnableTokenAcquisitionToCallDownstreamApi()
                .AddInMemoryTokenCaches();

            if (useDownstreamWebApiOptions)
            {
                builder.AddDownstreamWebApi(ConfigSectionName, configureDownstreamWebApiOptions);
            }
            else
            {
                builder.AddDownstreamWebApi(ConfigSectionName, configMock);
            }

            var provider = services.BuildServiceProvider();
            // Assert correct services added
            Assert.Contains(services, s => s.ServiceType == typeof(IConfigureOptions<OpenIdConnectOptions>));
            Assert.Contains(services, s => s.ServiceType == typeof(IConfigureOptions<MicrosoftIdentityOptions>));
            Assert.Contains(services, s => s.ServiceType == typeof(IConfigureOptions<DownstreamWebApiOptions>));
            Assert.Equal(ServiceLifetime.Singleton, services.First(s => s.ServiceType == typeof(MicrosoftIdentityIssuerValidatorFactory)).Lifetime);

            // Assert properties set
            var downstreamWebApiOptions = provider.GetRequiredService<IOptionsSnapshot<DownstreamWebApiOptions>>();
            provider.GetRequiredService<IDownstreamWebApi>();

            if (useDownstreamWebApiOptions)
            {
                Assert.Equal(TestConstants.GraphBaseUrlBeta, downstreamWebApiOptions.Get(ConfigSectionName).BaseUrl);
                Assert.Equal(TestConstants.Scopes, downstreamWebApiOptions.Get(ConfigSectionName).Scopes);
                Assert.Equal(TestConstants.TenantIdAsGuid, downstreamWebApiOptions.Get(ConfigSectionName).Tenant);
            }
            else
            {
                Assert.Equal(Constants.GraphBaseUrlV1, downstreamWebApiOptions.Get(ConfigSectionName).BaseUrl);
                Assert.Null(downstreamWebApiOptions.Get(ConfigSectionName).Scopes);
                Assert.Null(downstreamWebApiOptions.Get(ConfigSectionName).Tenant);
            }
        }

        private void AddMicrosoftIdentityWebApp_TestCommon(IServiceCollection services, ServiceProvider provider)
        {
            // Assert correct services added
            Assert.Contains(services, s => s.ServiceType == typeof(IConfigureOptions<OpenIdConnectOptions>));
            Assert.Contains(services, s => s.ServiceType == typeof(IConfigureOptions<MicrosoftIdentityOptions>));
            Assert.Contains(services, s => s.ServiceType == typeof(IPostConfigureOptions<CookieAuthenticationOptions>));
            Assert.Equal(ServiceLifetime.Singleton, services.First(s => s.ServiceType == typeof(MicrosoftIdentityIssuerValidatorFactory)).Lifetime);

            // Assert properties set
            var oidcOptions = provider.GetRequiredService<IOptionsFactory<OpenIdConnectOptions>>().Create(OidcScheme);

            Assert.Equal(CookieScheme, oidcOptions.SignInScheme);
            Assert.NotNull(oidcOptions.Authority);
            Assert.NotNull(oidcOptions.TokenValidationParameters.IssuerValidator);
            Assert.Equal(ClaimConstants.PreferredUserName, oidcOptions.TokenValidationParameters.NameClaimType);
        }

        private async Task AddMicrosoftIdentityWebApp_TestRedirectToIdentityProviderEvent(IServiceCollection services, Func<RedirectContext, Task> redirectFunc)
        {
            var provider = services.BuildServiceProvider();

            var oidcOptions = provider.GetRequiredService<IOptionsFactory<OpenIdConnectOptions>>().Create(OidcScheme);

            var (httpContext, authScheme, authProperties) = CreateContextParameters(provider);
            authProperties.Items[OidcConstants.AdditionalClaims] = TestConstants.Claims;
            authProperties.Parameters[OpenIdConnectParameterNames.LoginHint] = TestConstants.LoginHint;
            authProperties.Parameters[OpenIdConnectParameterNames.DomainHint] = TestConstants.DomainHint;

            var redirectContext = new RedirectContext(httpContext, authScheme, oidcOptions, authProperties);
            redirectContext.ProtocolMessage = new OpenIdConnectMessage();

            await oidcOptions.Events.RedirectToIdentityProvider(redirectContext).ConfigureAwait(false);

            // Assert properties set, events called
            await redirectFunc.ReceivedWithAnyArgs().Invoke(Arg.Any<RedirectContext>()).ConfigureAwait(false);
            Assert.NotNull(redirectContext.ProtocolMessage.LoginHint);
            Assert.NotNull(redirectContext.ProtocolMessage.DomainHint);
            Assert.NotNull(redirectContext.ProtocolMessage.Parameters[OidcConstants.AdditionalClaims]);
            Assert.False(redirectContext.Properties.Parameters.ContainsKey(OpenIdConnectParameterNames.LoginHint));
            Assert.False(redirectContext.Properties.Parameters.ContainsKey(OpenIdConnectParameterNames.DomainHint));
        }

        private void AddMicrosoftIdentityWebApp_TestSubscribesToDiagnostics(IServiceCollection services, IOpenIdConnectMiddlewareDiagnostics diagnosticsMock, bool subscribeToDiagnostics)
        {
            services.RemoveAll<IOpenIdConnectMiddlewareDiagnostics>();
            services.AddSingleton((provider) => diagnosticsMock);

            var provider = services.BuildServiceProvider();

            var oidcOptions = provider.GetRequiredService<IOptionsFactory<OpenIdConnectOptions>>().Create(OidcScheme);

            // Assert subscribed to diagnostics
            if (subscribeToDiagnostics)
            {
                diagnosticsMock.ReceivedWithAnyArgs().Subscribe(Arg.Any<OpenIdConnectEvents>());
            }
            else
            {
                diagnosticsMock.DidNotReceiveWithAnyArgs().Subscribe(Arg.Any<OpenIdConnectEvents>());
            }
        }

        private async Task AddMicrosoftIdentityWebApp_TestB2cSpecificSetup(IServiceCollection services, Func<RemoteFailureContext, Task> remoteFailureFuncMock)
        {
            var provider = services.BuildServiceProvider();

            var oidcOptions = provider.GetRequiredService<IOptionsFactory<OpenIdConnectOptions>>().Create(OidcScheme);

            // Assert B2C name claim type
            Assert.Equal(ClaimConstants.Name, oidcOptions.TokenValidationParameters.NameClaimType);

            var (httpContext, authScheme, authProperties) = CreateContextParameters(provider);
            authProperties.Items[OidcConstants.PolicyKey] = TestConstants.B2CEditProfileUserFlow;

            var redirectContext = new RedirectContext(httpContext, authScheme, oidcOptions, authProperties)
            {
                ProtocolMessage = new OpenIdConnectMessage() { IssuerAddress = $"IssuerAddress/{TestConstants.B2CSignUpSignInUserFlow}/" },
            };

            (httpContext, authScheme, authProperties) = CreateContextParameters(provider);

            var remoteFailureContext = new RemoteFailureContext(httpContext, authScheme, new RemoteAuthenticationOptions(), new Exception());

            await oidcOptions.Events.RedirectToIdentityProvider(redirectContext).ConfigureAwait(false);
            await oidcOptions.Events.RemoteFailure(remoteFailureContext).ConfigureAwait(false);

            await remoteFailureFuncMock.ReceivedWithAnyArgs().Invoke(Arg.Any<RemoteFailureContext>()).ConfigureAwait(false);
            // Assert issuer is updated to non-default user flow
            Assert.Contains(TestConstants.B2CEditProfileUserFlow, redirectContext.ProtocolMessage.IssuerAddress);
            Assert.NotNull(redirectContext.ProtocolMessage.Parameters[ClaimConstants.ClientInfo]);
            Assert.Equal(Constants.One, redirectContext.ProtocolMessage.Parameters[ClaimConstants.ClientInfo].ToString(CultureInfo.InvariantCulture));
        }

        private void AddMicrosoftIdentityWebAppCallsWebApi_TestCommon(IServiceCollection services, ServiceProvider provider, OpenIdConnectOptions oidcOptions, IEnumerable<string> initialScopes)
        {
            // Assert correct services added
            Assert.Contains(services, s => s.ServiceType == typeof(IHttpContextAccessor));
            Assert.Contains(services, s => s.ServiceType == typeof(ITokenAcquisition));
            Assert.Contains(services, s => s.ServiceType == typeof(IConfigureOptions<ConfidentialClientApplicationOptions>));
            Assert.Contains(services, s => s.ServiceType == typeof(IConfigureOptions<MicrosoftIdentityOptions>));
            Assert.Contains(services, s => s.ServiceType == typeof(IConfigureOptions<OpenIdConnectOptions>));
            Assert.Equal(ServiceLifetime.Singleton, services.First(s => s.ServiceType == typeof(MicrosoftIdentityIssuerValidatorFactory)).Lifetime);

            // Assert OIDC options added correctly
            var configuredOidcOptions = provider.GetService<IConfigureOptions<OpenIdConnectOptions>>() as ConfigureNamedOptions<OpenIdConnectOptions>;

            Assert.Equal(OidcScheme, configuredOidcOptions.Name);

            // Assert properties set
            Assert.Equal(OpenIdConnectResponseType.Code, oidcOptions.ResponseType);
            Assert.Contains(OidcConstants.ScopeOfflineAccess, oidcOptions.Scope);
            Assert.All(initialScopes, scope => Assert.Contains(scope, oidcOptions.Scope));
        }

        private async Task AddMicrosoftIdentityWebAppCallsWebApi_TestAuthorizationCodeReceivedEvent(
            IServiceProvider provider,
            OpenIdConnectOptions oidcOptions,
            Func<AuthorizationCodeReceivedContext, Task> authCodeReceivedFuncMock,
            ITokenAcquisitionInternal tokenAcquisitionMock)
        {
            var (httpContext, authScheme, authProperties) = CreateContextParameters(provider);

            await oidcOptions.Events.AuthorizationCodeReceived(new AuthorizationCodeReceivedContext(httpContext, authScheme, oidcOptions, authProperties)).ConfigureAwait(false);

            // Assert original AuthorizationCodeReceived event and TokenAcquisition method were called
            await authCodeReceivedFuncMock.ReceivedWithAnyArgs().Invoke(Arg.Any<AuthorizationCodeReceivedContext>()).ConfigureAwait(false);
            await tokenAcquisitionMock.ReceivedWithAnyArgs().AddAccountToCacheFromAuthorizationCodeAsync(Arg.Any<AuthorizationCodeReceivedContext>(), Arg.Any<IEnumerable<string>>()).ConfigureAwait(false);
        }

        private async Task AddMicrosoftIdentityWebAppCallsWebApi_TestTokenValidatedEvent(IServiceProvider provider, OpenIdConnectOptions oidcOptions, Func<TokenValidatedContext, Task> tokenValidatedFuncMock)
        {
            var (httpContext, authScheme, authProperties) = CreateContextParameters(provider);

            var tokenValidatedContext = new TokenValidatedContext(httpContext, authScheme, oidcOptions, httpContext.User, authProperties)
            {
                ProtocolMessage = new OpenIdConnectMessage(
                    new Dictionary<string, string[]>()
                    {
                        { ClaimConstants.ClientInfo, new string[] { Base64UrlHelpers.Encode($"{{\"uid\":\"{TestConstants.Uid}\",\"utid\":\"{TestConstants.Utid}\"}}") } },
                    }),
            };

            await oidcOptions.Events.TokenValidated(tokenValidatedContext).ConfigureAwait(false);

            // Assert original TokenValidated event was called; properties were set
            await tokenValidatedFuncMock.ReceivedWithAnyArgs().Invoke(Arg.Any<TokenValidatedContext>()).ConfigureAwait(false);
            Assert.True(tokenValidatedContext.Principal.HasClaim(c => c.Type == ClaimConstants.UniqueTenantIdentifier));
            Assert.True(tokenValidatedContext.Principal.HasClaim(c => c.Type == ClaimConstants.UniqueObjectIdentifier));
        }

        private async Task AddMicrosoftIdentityWebAppCallsWebApi_TestRedirectToIdentityProviderForSignOutEvent(
            IServiceProvider provider,
            OpenIdConnectOptions oidcOptions,
            Func<RedirectContext, Task> redirectFuncMock,
            ITokenAcquisitionInternal tokenAcquisitionMock)
        {
            var (httpContext, authScheme, authProperties) = CreateContextParameters(provider);

            await oidcOptions.Events.RedirectToIdentityProviderForSignOut(new RedirectContext(httpContext, authScheme, oidcOptions, authProperties)).ConfigureAwait(false);

            // Assert original RedirectToIdentityProviderForSignOut event and TokenAcquisition method were called
            await redirectFuncMock.ReceivedWithAnyArgs().Invoke(Arg.Any<RedirectContext>()).ConfigureAwait(false);
            await tokenAcquisitionMock.ReceivedWithAnyArgs().RemoveAccountAsync(Arg.Any<RedirectContext>()).ConfigureAwait(false);
        }

        private (HttpContext, AuthenticationScheme, AuthenticationProperties) CreateContextParameters(IServiceProvider provider)
        {
            var httpContext = HttpContextUtilities.CreateHttpContext();
            httpContext.RequestServices = provider;

            var authScheme = new AuthenticationScheme(OpenIdConnectDefaults.AuthenticationScheme, OpenIdConnectDefaults.AuthenticationScheme, typeof(OpenIdConnectHandler));
            var authProperties = new AuthenticationProperties();

            return (httpContext, authScheme, authProperties);
        }

        private IConfigurationSection GetConfigSection(string configSectionName, bool includeB2cConfig = false)
        {
            var configAsDictionary = new Dictionary<string, string>()
            {
                { configSectionName, null },
                { $"{configSectionName}:Instance", TestConstants.AadInstance },
                { $"{configSectionName}:TenantId", TestConstants.TenantIdAsGuid },
                { $"{configSectionName}:ClientId", TestConstants.TenantIdAsGuid },
                { $"{configSectionName}:Domain", TestConstants.Domain },
            };

            if (includeB2cConfig)
            {
                configAsDictionary.Add($"{configSectionName}:SignUpSignInPolicyId", TestConstants.B2CSignUpSignInUserFlow);
                configAsDictionary[$"{configSectionName}:Instance"] = TestConstants.B2CInstance;
                configAsDictionary[$"{configSectionName}:Domain"] = TestConstants.B2CTenant;
            }

            var memoryConfigSource = new MemoryConfigurationSource { InitialData = configAsDictionary };
            var configBuilder = new ConfigurationBuilder();
            configBuilder.Add(memoryConfigSource);
            var configSection = configBuilder.Build().GetSection(configSectionName);
            return configSection;
        }

        [Fact]
        public void PreventChangesInOpenIdConnectOptionsToBeOverlooked()
        {
            // If the number of public properties of OpenIdConnectOptions changes,
            // then, the PopulateOpenIdOptionsFromMicrosoftIdentityOptions method
            // needs to be updated. For this uncomment the 2 lines below, and run the test
            // then diff the files to find what are the new properties
            int numberOfProperties = typeof(OpenIdConnectOptions).GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance).Length;

            int expectedNumberOfProperties;
#if DOTNET_CORE_31
            expectedNumberOfProperties = 54;
            // System.IO.File.WriteAllLines(@"c:\temp\core31.txt", typeof(OpenIdConnectOptions).GetProperties().Select(p => p.Name));
#elif DOTNET_50
            expectedNumberOfProperties = 57;
            // System.IO.File.WriteAllLines(@"c:\temp\net5.txt", typeof(OpenIdConnectOptions).GetProperties().Select(p => p.Name));
#endif
            Assert.Equal(expectedNumberOfProperties, numberOfProperties);
        }
    }
}
