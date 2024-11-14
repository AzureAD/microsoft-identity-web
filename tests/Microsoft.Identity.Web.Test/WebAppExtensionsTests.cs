// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web.Resource;
using Microsoft.Identity.Web.Test.Common;
using Microsoft.Identity.Web.Test.Common.TestHelpers;
using Microsoft.Identity.Web.TokenCacheProviders;
using Microsoft.Identity.Web.TokenCacheProviders.InMemory;
using Microsoft.Identity.Web.Util;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using NSubstitute;
using NSubstitute.Extensions;
using Xunit;
using TokenValidatedContext = Microsoft.AspNetCore.Authentication.OpenIdConnect.TokenValidatedContext;

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
            ResetAppServiceEnv();
            _configSection = GetConfigSection(ConfigSectionName);
            _env = new HostingEnvironment { EnvironmentName = Environments.Development };
        }

        private void ResetAppServiceEnv()
        {
            Environment.SetEnvironmentVariable(AppServicesAuthenticationInformation.AppServicesAuthEnabledEnvironmentVariable, string.Empty);
            Environment.SetEnvironmentVariable(AppServicesAuthenticationInformation.AppServicesAuthIdentityProviderEnvironmentVariable, string.Empty);
            Environment.SetEnvironmentVariable(AppServicesAuthenticationInformation.AppServicesAuthClientIdEnvironmentVariable, string.Empty);
            Environment.SetEnvironmentVariable(AppServicesAuthenticationInformation.AppServicesAuthClientSecretEnvironmentVariable, string.Empty);
            Environment.SetEnvironmentVariable(AppServicesAuthenticationInformation.AppServicesAuthLogoutPathEnvironmentVariable, string.Empty);
            Environment.SetEnvironmentVariable(AppServicesAuthenticationInformation.AppServicesWebSiteAuthApiPrefix, string.Empty);
            Environment.SetEnvironmentVariable(AppServicesAuthenticationInformation.AppServicesAuthIdentityProviderEnvironmentVariable, string.Empty);
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
            services.AddSingleton(configMock);

            services.AddDataProtection();
            services.AddSingleton((provider) => _env);

            services.AddAuthentication()
                .AddMicrosoftIdentityWebApp(
                configMock,
                ConfigSectionName,
                OidcScheme,
                CookieScheme,
                subscribeToOpenIdConnectMiddlewareDiagnosticsEvents: subscribeToDiagnostics,
                displayName: "cat");

            var provider = services.BuildServiceProvider();

            // Assert config bind actions added correctly
            provider.GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>().Get(OidcScheme);
            provider.GetRequiredService<IOptionsMonitor<MicrosoftIdentityOptions>>().Get(OidcScheme);
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

            services.AddSingleton(configMock);
            services.AddDataProtection();
            services.AddSingleton((provider) => _env);

            services.AddMicrosoftIdentityWebAppAuthentication(
                configMock,
                ConfigSectionName,
                OidcScheme,
                CookieScheme,
                subscribeToOpenIdConnectMiddlewareDiagnosticsEvents: subscribeToDiagnostics);

            var provider = services.BuildServiceProvider();

            // Assert config bind actions added correctly
            provider.GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>().Get(OidcScheme);
            provider.GetRequiredService<IOptionsMonitor<MicrosoftIdentityOptions>>().Get(OidcScheme);
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
            var configMock = Substitute.For<IConfiguration>();

            var services = new ServiceCollection();
            services.AddSingleton(configMock);
            services.AddDataProtection();
            services.AddSingleton((provider) => _env);

            services.AddAuthentication()
                .AddMicrosoftIdentityWebApp(
                _configureMsOptions,
                _configureCookieOptions,
                OidcScheme,
                CookieScheme,
                subscribeToOpenIdConnectMiddlewareDiagnosticsEvents: subscribeToDiagnostics);

            var provider = services.BuildServiceProvider();

            // Assert configure options actions added correctly
            var configuredMsOptions = provider.GetServices<IConfigureOptions<MicrosoftIdentityOptions>>().Cast<ConfigureNamedOptions<MicrosoftIdentityOptions>>();

            Assert.Contains(configuredMsOptions, o => o.Action == _configureMsOptions);

            AddMicrosoftIdentityWebApp_TestCommon(services, provider);
            AddMicrosoftIdentityWebApp_TestSubscribesToDiagnostics(services, diagnosticsMock, subscribeToDiagnostics);
        }

        [Fact]
        public Task AddMicrosoftIdentityWebApp_WithConfigAuthority_TestCorrectMetadataAddressAsync()
        {
            // Arrange
            string authority = "https://login.microsoftonline.com/some-tenant-id/v2.0";
            string appId = "some-client-id";
            string expectedMetadataAddress = $"{authority}/.well-known/openid-configuration?appId={appId}";
            var configMock = Substitute.For<IConfiguration>();
            configMock.Configure().GetSection(ConfigSectionName).Returns(_configSection);

            var services = new ServiceCollection()
                .AddSingleton(configMock)
                .PostConfigure<MicrosoftIdentityOptions>(OidcScheme, (options) =>
                {
                    options.Authority = authority;
                    options.ExtraQueryParameters = new Dictionary<string, string>
                        {
                            {"appId", appId}
                        };
                });

            // Act
            services.AddAuthentication()
                .AddMicrosoftIdentityWebApp(configMock, ConfigSectionName, OidcScheme, CookieScheme, subscribeToOpenIdConnectMiddlewareDiagnosticsEvents: false);
            var provider = services.BuildServiceProvider();
            var providedOptions = provider.GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>().Get(OidcScheme);

            // Assert
            Assert.Equal(expectedMetadataAddress, providedOptions.MetadataAddress);
            return Task.CompletedTask;
        }

        [Fact]
        public async Task AddMicrosoftIdentityWebApp_WithConfigNameParameters_TestRedirectToIdentityProviderEventAsync()
        {
            var configMock = Substitute.For<IConfiguration>();
            configMock.Configure().GetSection(ConfigSectionName).Returns(_configSection);

            var redirectFunc = Substitute.For<Func<RedirectContext, Task>>();
            var services = new ServiceCollection()
                .AddSingleton(configMock)
                .PostConfigure<MicrosoftIdentityOptions>(OidcScheme, (options) =>
                {
                    options.Events ??= new OpenIdConnectEvents();
                    options.Events.OnRedirectToIdentityProvider += redirectFunc;
                });
            services.AddDataProtection();
            services.AddSingleton((provider) => _env);

            services.AddAuthentication()
                .AddMicrosoftIdentityWebApp(configMock, ConfigSectionName, OidcScheme, CookieScheme, subscribeToOpenIdConnectMiddlewareDiagnosticsEvents: false);

            await AddMicrosoftIdentityWebApp_TestRedirectToIdentityProviderEventAsync(services, redirectFunc);
        }

        [Fact]
        public async Task AddMicrosoftIdentityWebApp_WithConfigActionParameters_TestRedirectToIdentityProviderEventAsync()
        {
            var configMock = Substitute.For<IConfiguration>();
            var redirectFunc = Substitute.For<Func<RedirectContext, Task>>();
            var services = new ServiceCollection()
                .AddSingleton(configMock)
                .PostConfigure<MicrosoftIdentityOptions>(OidcScheme, (options) =>
                {
                    options.Events ??= new OpenIdConnectEvents();
                    options.Events.OnRedirectToIdentityProvider += redirectFunc;
                });

            services.AddDataProtection();
            services.AddSingleton((provider) => _env);

            services.AddAuthentication()
                    .AddMicrosoftIdentityWebApp(_configureMsOptions, _configureCookieOptions, OidcScheme, CookieScheme, subscribeToOpenIdConnectMiddlewareDiagnosticsEvents: false);

            await AddMicrosoftIdentityWebApp_TestRedirectToIdentityProviderEventAsync(services, redirectFunc);
        }

        [Fact]
        public async Task AddMicrosoftIdentityWebApp_WithConfigNameParameters_TestB2cSpecificSetupAsync()
        {
            var configMock = Substitute.For<IConfiguration>();
            _configSection = GetConfigSection(ConfigSectionName, true);
            configMock.Configure().GetSection(ConfigSectionName).Returns(_configSection);

            var remoteFailureFuncMock = Substitute.For<Func<RemoteFailureContext, Task>>();
            var services = new ServiceCollection()
                .AddSingleton(configMock)
                .PostConfigure<MicrosoftIdentityOptions>(OidcScheme, (options) =>
                {
                    options.Events ??= new OpenIdConnectEvents();
                    options.Events.OnRemoteFailure += remoteFailureFuncMock;
                });
            services.AddDataProtection();
            services.AddSingleton((provider) => _env);

            services.AddAuthentication()
                .AddMicrosoftIdentityWebApp(configMock, ConfigSectionName, OidcScheme, CookieScheme, subscribeToOpenIdConnectMiddlewareDiagnosticsEvents: false);

            await AddMicrosoftIdentityWebApp_TestB2cSpecificSetupAsync(services, remoteFailureFuncMock);
        }

        [Fact]
        public async Task AddMicrosoftIdentityWebApp_WithConfigActionParameters_B2cSpecificSetupAsync()
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
            var configMock = Substitute.For<IConfiguration>();

            var services = new ServiceCollection()
                .AddSingleton(configMock)
                .PostConfigure<MicrosoftIdentityOptions>(OidcScheme, (options) =>
                {
                    options.Events ??= new OpenIdConnectEvents();
                    options.Events.OnRemoteFailure += remoteFailureFuncMock;
                });
            services.AddDataProtection();
            services.AddSingleton((provider) => _env);

            services.AddAuthentication()
                .AddMicrosoftIdentityWebApp(_configureMsOptions, _configureCookieOptions, OidcScheme, CookieScheme, subscribeToOpenIdConnectMiddlewareDiagnosticsEvents: false);

            await AddMicrosoftIdentityWebApp_TestB2cSpecificSetupAsync(services, remoteFailureFuncMock);
        }

        [Fact]
        public async Task AddMicrosoftIdentityWebAppCallsWebApi_WithConfigNameParametersAsync()
        {
            var configMock = Substitute.For<IConfiguration>();
            configMock.Configure().GetSection(ConfigSectionName).Returns(_configSection);
            var initialScopes = new List<string>() { "custom_scope" };
            var tokenAcquisitionMock = Substitute.For<ITokenAcquisitionInternal>();
            var authCodeReceivedFuncMock = Substitute.For<Func<AuthorizationCodeReceivedContext, Task>>();
            var tokenValidatedFuncMock = Substitute.For<Func<TokenValidatedContext, Task>>();
            var redirectFuncMock = Substitute.For<Func<RedirectContext, Task>>();
            var services = new ServiceCollection();

            services.AddSingleton((provider) => _env)
                    .AddSingleton(configMock);

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
            provider.GetRequiredService<IOptionsMonitor<ConfidentialClientApplicationOptions>>().Get(OidcScheme);
            provider.GetRequiredService<IOptionsMonitor<MicrosoftIdentityOptions>>().Get(OidcScheme);

            configMock.Received(1).GetSection(ConfigSectionName);

            var oidcOptions = provider.GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>().Get(OidcScheme);

            AddMicrosoftIdentityWebAppCallsWebApi_TestCommon(services, provider, oidcOptions, initialScopes);
            await AddMicrosoftIdentityWebAppCallsWebApi_TestAuthorizationCodeReceivedEventAsync(provider, oidcOptions, authCodeReceivedFuncMock, tokenAcquisitionMock);
            await AddMicrosoftIdentityWebAppCallsWebApi_TestTokenValidatedEventAsync(provider, oidcOptions, tokenValidatedFuncMock);
            await AddMicrosoftIdentityWebAppCallsWebApi_TestRedirectToIdentityProviderForSignOutEventAsync(provider, oidcOptions, redirectFuncMock, tokenAcquisitionMock);
        }

        [Fact]
        public async Task AddMicrosoftIdentityWebAppCallsWebApi_WithConfigActionParametersAsync()
        {
            var configMock = Substitute.For<IConfiguration>();
            var initialScopes = new List<string>() { "custom_scope" };
            var tokenAcquisitionMock = Substitute.For<ITokenAcquisitionInternal>();
            var authCodeReceivedFuncMock = Substitute.For<Func<AuthorizationCodeReceivedContext, Task>>();
            var tokenValidatedFuncMock = Substitute.For<Func<TokenValidatedContext, Task>>();
            var redirectFuncMock = Substitute.For<Func<RedirectContext, Task>>();

            var services = new ServiceCollection();
            services.AddSingleton(configMock);
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
            await AddMicrosoftIdentityWebAppCallsWebApi_TestAuthorizationCodeReceivedEventAsync(provider, oidcOptions, authCodeReceivedFuncMock, tokenAcquisitionMock);
            await AddMicrosoftIdentityWebAppCallsWebApi_TestTokenValidatedEventAsync(provider, oidcOptions, tokenValidatedFuncMock);
            await AddMicrosoftIdentityWebAppCallsWebApi_TestRedirectToIdentityProviderForSignOutEventAsync(provider, oidcOptions, redirectFuncMock, tokenAcquisitionMock);
        }

        [Fact]
        public void AddMicrosoftIdentityWebAppCallsWebApi_NoScopes()
        {
            // Arrange & Act
            var configMock = Substitute.For<IConfiguration>();
            var services = new ServiceCollection();
            services.AddSingleton(configMock);

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
        public async Task AddMicrosoftIdentityWebApp_RedirectUriAsync(string expectedUri)
        {
            var configMock = Substitute.For<IConfiguration>();

            _configureMsOptions = (options) =>
            {
                options.Instance = TestConstants.AadInstance;
                options.TenantId = TestConstants.TenantIdAsGuid;
                options.ClientId = TestConstants.ClientId;
            };

            var services = new ServiceCollection();
            services.AddSingleton(configMock);
            services.AddSingleton((provider) => _env);
            services.AddDataProtection();
            services.AddAuthentication()
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

            await oidcOptions.Events.RedirectToIdentityProvider(redirectContext);
            await oidcOptions.Events.RedirectToIdentityProviderForSignOut(redirectContext);

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

        [Theory]
        [InlineData("tRue", "azureactivedirectory")]
        [InlineData("true", "azureactivedirectory")]
        [InlineData("tRue", AppServicesAuthenticationInformation.AppServicesAuthAzureActiveDirectory)]
        [InlineData("true", AppServicesAuthenticationInformation.AppServicesAuthAzureActiveDirectory)]
        [InlineData("tRue", AppServicesAuthenticationInformation.AppServicesAuthAAD)]
        [InlineData("true", AppServicesAuthenticationInformation.AppServicesAuthAAD)]
        // Regression for https://github.com/AzureAD/microsoft-identity-web/issues/1163
        public void AppServices_EnvironmentTest(string appServicesEnvEnabledValue, string idpEnvValue)
        {
            try
            {
                // Arrange
                Environment.SetEnvironmentVariable(
                    AppServicesAuthenticationInformation.AppServicesAuthEnabledEnvironmentVariable, appServicesEnvEnabledValue);

                Environment.SetEnvironmentVariable(
                    AppServicesAuthenticationInformation.AppServicesAuthIdentityProviderEnvironmentVariable,
                    idpEnvValue);

                var services = new ServiceCollection();

                // Act
                services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
                    .AddMicrosoftIdentityWebApp(_configureMsOptions);

                // Assert
                Assert.Contains(services, s => s.ServiceType == typeof(AppServicesAuthenticationHandler));
            }
            finally
            {
                ResetAppServiceEnv();
            }
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

        [Fact]
        public void AddJwtBearerMergedOptionsTest()
        {
            //Action<JwtBearerMergedOptions> configureJwtBearerMergedOptions = (options) =>
            //{

            //};
        }

#pragma warning disable CS0618 // Type or member is obsolete
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
                Assert.Equal(TestConstants.GraphBaseUrlBeta + ('/'), downstreamWebApiOptions.Get(ConfigSectionName).GetApiUrl());
            }
            else
            {
                Assert.Equal(Constants.GraphBaseUrlV1, downstreamWebApiOptions.Get(ConfigSectionName).BaseUrl);
                Assert.Null(downstreamWebApiOptions.Get(ConfigSectionName).Scopes);
                Assert.Null(downstreamWebApiOptions.Get(ConfigSectionName).Tenant);
            }
        }
#pragma warning restore CS0618 // Type or member is obsolete

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ConsentHandlerExtensionsTests(bool withBlazor)
        {
            var configMock = Substitute.For<IConfiguration>();
            configMock.Configure().GetSection(ConfigSectionName).Returns(_configSection);

            var services = new ServiceCollection();
            services.AddSingleton(configMock);

            services.AddAuthentication()
                .AddMicrosoftIdentityWebApp(
                configMock,
                ConfigSectionName);
            if (withBlazor)
            {
                services.AddServerSideBlazor()
                          .AddMicrosoftIdentityConsentHandler();
            }
            else
            {
                services.AddMicrosoftIdentityConsentHandler();
            }
            services.AddHttpContextAccessor();

            var provider = services.BuildServiceProvider();
            Assert.Contains(services, s => s.ServiceType == typeof(CircuitHandler));
            Assert.Contains(services, s => s.ServiceType == typeof(MicrosoftIdentityConsentAndConditionalAccessHandler));

            MicrosoftIdentityConsentAndConditionalAccessHandler accessHandler = new(provider);
            Assert.False(accessHandler.IsBlazorServer);
        }

        private void AddMicrosoftIdentityWebApp_TestCommon(IServiceCollection services, ServiceProvider provider)
        {
            // Assert correct services added
            Assert.Contains(services, s => s.ServiceType == typeof(IConfigureOptions<OpenIdConnectOptions>));
            Assert.Contains(services, s => s.ServiceType == typeof(IConfigureOptions<MicrosoftIdentityOptions>));
            Assert.Contains(services, s => s.ServiceType == typeof(IPostConfigureOptions<CookieAuthenticationOptions>));
            Assert.DoesNotContain(services, s => s.ServiceType == typeof(AppServicesAuthenticationHandler));

            Assert.Equal(ServiceLifetime.Singleton, services.First(s => s.ServiceType == typeof(MicrosoftIdentityIssuerValidatorFactory)).Lifetime);

            // Assert properties set
            var oidcOptions = provider.GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>().Get(OidcScheme);

            Assert.Equal(CookieScheme, oidcOptions.SignInScheme);
            Assert.NotNull(oidcOptions.Authority);
            Assert.NotNull(oidcOptions.TokenValidationParameters.IssuerValidator);
            Assert.Equal(ClaimConstants.PreferredUserName, oidcOptions.TokenValidationParameters.NameClaimType);
        }

        private async Task AddMicrosoftIdentityWebApp_TestRedirectToIdentityProviderEventAsync(IServiceCollection services, Func<RedirectContext, Task> redirectFunc)
        {
            var provider = services.BuildServiceProvider();

            var oidcOptions = provider.GetRequiredService<IOptionsFactory<OpenIdConnectOptions>>().Create(OidcScheme);

            var (httpContext, authScheme, authProperties) = CreateContextParameters(provider);
            authProperties.Items[OidcConstants.AdditionalClaims] = TestConstants.Claims;
            authProperties.Parameters[OpenIdConnectParameterNames.LoginHint] = TestConstants.LoginHint;
            authProperties.Parameters[OpenIdConnectParameterNames.DomainHint] = TestConstants.DomainHint;

            var redirectContext = new RedirectContext(httpContext, authScheme, oidcOptions, authProperties);
            redirectContext.ProtocolMessage = new OpenIdConnectMessage();

            await oidcOptions.Events.RedirectToIdentityProvider(redirectContext);

            // Assert properties set, events called
            await redirectFunc.ReceivedWithAnyArgs().Invoke(Arg.Any<RedirectContext>());
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

        private async Task AddMicrosoftIdentityWebApp_TestB2cSpecificSetupAsync(IServiceCollection services, Func<RemoteFailureContext, Task> remoteFailureFuncMock)
        {
            var provider = services.BuildServiceProvider();

            var oidcOptions = provider.GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>().Get(OidcScheme);

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

            await oidcOptions.Events.RedirectToIdentityProvider(redirectContext);
            await oidcOptions.Events.RemoteFailure(remoteFailureContext);

            await remoteFailureFuncMock.ReceivedWithAnyArgs().Invoke(Arg.Any<RemoteFailureContext>());
            // Assert issuer is updated to non-default user flow
            Assert.Contains(TestConstants.B2CEditProfileUserFlow, redirectContext.ProtocolMessage.IssuerAddress, System.StringComparison.OrdinalIgnoreCase);
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

            Assert.Equal(OidcScheme, configuredOidcOptions?.Name);

            // Assert properties set
            Assert.Equal(OpenIdConnectResponseType.Code, oidcOptions.ResponseType);
            Assert.Contains(OidcConstants.ScopeOfflineAccess, oidcOptions.Scope);
            Assert.All(initialScopes, scope => Assert.Contains(scope, oidcOptions.Scope));
        }

        private async Task AddMicrosoftIdentityWebAppCallsWebApi_TestAuthorizationCodeReceivedEventAsync(
            IServiceProvider provider,
            OpenIdConnectOptions oidcOptions,
            Func<AuthorizationCodeReceivedContext, Task> authCodeReceivedFuncMock,
            ITokenAcquisitionInternal tokenAcquisitionMock)
        {
            var (httpContext, authScheme, authProperties) = CreateContextParameters(provider);

            await oidcOptions.Events.AuthorizationCodeReceived(new AuthorizationCodeReceivedContext(httpContext, authScheme, oidcOptions, authProperties));

            // Assert original AuthorizationCodeReceived event and TokenAcquisition method were called
            await authCodeReceivedFuncMock.ReceivedWithAnyArgs().Invoke(Arg.Any<AuthorizationCodeReceivedContext>());
            await tokenAcquisitionMock.ReceivedWithAnyArgs().AddAccountToCacheFromAuthorizationCodeAsync(Arg.Any<AuthorizationCodeReceivedContext>(), Arg.Any<IEnumerable<string>>());
        }

        private async Task AddMicrosoftIdentityWebAppCallsWebApi_TestTokenValidatedEventAsync(IServiceProvider provider, OpenIdConnectOptions oidcOptions, Func<TokenValidatedContext, Task> tokenValidatedFuncMock)
        {
            var (httpContext, authScheme, authProperties) = CreateContextParameters(provider);

            var tokenValidatedContext = new TokenValidatedContext(httpContext, authScheme, oidcOptions, httpContext.User, authProperties)
            {
                ProtocolMessage = new OpenIdConnectMessage(
                    new Dictionary<string, string[]>()
                    {
                        { ClaimConstants.ClientInfo, new string[] { Base64UrlHelpers.Encode($"{{\"uid\":\"{TestConstants.Uid}\",\"utid\":\"{TestConstants.Utid}\"}}")! } },
                    }),
            };

            await oidcOptions.Events.TokenValidated(tokenValidatedContext);

            // Assert original TokenValidated event was called; properties were set
            await tokenValidatedFuncMock.ReceivedWithAnyArgs().Invoke(Arg.Any<TokenValidatedContext>());
            Assert.True(tokenValidatedContext?.Principal?.HasClaim(c => c.Type == ClaimConstants.UniqueTenantIdentifier));
            Assert.True(tokenValidatedContext?.Principal?.HasClaim(c => c.Type == ClaimConstants.UniqueObjectIdentifier));
        }

        private async Task AddMicrosoftIdentityWebAppCallsWebApi_TestRedirectToIdentityProviderForSignOutEventAsync(
            IServiceProvider provider,
            OpenIdConnectOptions oidcOptions,
            Func<RedirectContext, Task> redirectFuncMock,
            ITokenAcquisitionInternal tokenAcquisitionMock)
        {
            var (httpContext, authScheme, authProperties) = CreateContextParameters(provider);

            await oidcOptions.Events.RedirectToIdentityProviderForSignOut(new RedirectContext(httpContext, authScheme, oidcOptions, authProperties));

            // Assert original RedirectToIdentityProviderForSignOut event and TokenAcquisition method were called
            await redirectFuncMock.ReceivedWithAnyArgs().Invoke(Arg.Any<RedirectContext>());
            await tokenAcquisitionMock.ReceivedWithAnyArgs().RemoveAccountAsync(Arg.Any<ClaimsPrincipal>());
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
            var configAsDictionary = new Dictionary<string, string?>()
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
            // then, the PopulateOpenIdOptionsFromMergedOptions method
            // needs to be updated. For this uncomment the variable "filePath" and line 893 below, then run the test
            // and diff the files to find what are the new properties.
            int numberOfProperties = typeof(OpenIdConnectOptions).GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance).Length;
#if NET9_0_OR_GREATER
            int expectedNumberOfProperties = 62;
            //string filePath = @"C:\temp\net9.txt";
#elif NET8_0
            int expectedNumberOfProperties = 60;
            //string filePath = @"C:\temp\net8.txt";
#else
            int expectedNumberOfProperties = 57;
            //string filePath = @"C:\temp\net7Below.txt";
#endif
            //System.IO.File.WriteAllLines(filePath, typeof(OpenIdConnectOptions).GetProperties().Select(p => p.Name));
            Assert.Equal(expectedNumberOfProperties, numberOfProperties);
        }

        [Fact]
        public void PreventChangesInJwtBearerOptionsToBeOverlooked()
        {
            int numProps = typeof(JwtBearerOptions).GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance).Length;
#if NET8_0_OR_GREATER
            int expectedNumberOfProperties = 32;
#else
            int expectedNumberOfProperties = 29;
#endif
            Assert.Equal(expectedNumberOfProperties, numProps);
        }
    }
}
