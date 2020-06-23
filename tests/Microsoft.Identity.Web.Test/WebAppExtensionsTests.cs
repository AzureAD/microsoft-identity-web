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
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web.Resource;
using Microsoft.Identity.Web.Test.Common;
using Microsoft.Identity.Web.Test.Common.TestHelpers;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using NSubstitute;
using NSubstitute.Extensions;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    public class WebAppExtensionsTests
    {
        private const string _oidcScheme = "OpenIdConnect-Custom";
        private const string _cookieScheme = "Cookies-Custom";

        private const string _configSectionName = "AzureAd-Custom";
        private IConfigurationSection _configSection;
        private readonly Action<ConfidentialClientApplicationOptions> _configureAppOptions = (options) => { };
        private readonly Action<OpenIdConnectOptions> _configureOidcOptions = (options) =>
        {
            options.ClientId = TestConstants.ClientId;
        };
        private Action<MicrosoftIdentityOptions> _configureMsOptions = (options) =>
        {
            options.Instance = TestConstants.AadInstance;
            options.TenantId = TestConstants.TenantIdAsGuid;
            options.ClientId = TestConstants.ClientId;
        };

        public WebAppExtensionsTests()
        {
            _configSection = GetConfigSection(_configSectionName);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void AddMicrosoftWebApp_WithConfigNameParameters(bool subscribeToDiagnostics)
        {
            var configMock = Substitute.For<IConfiguration>();
            configMock.Configure().GetSection(_configSectionName).Returns(_configSection);

            var diagnosticsMock = Substitute.For<IOpenIdConnectMiddlewareDiagnostics>();

            var services = new ServiceCollection();

            services.AddDataProtection();

            new AuthenticationBuilder(services)
                .AddMicrosoftWebApp(configMock, _configSectionName, _oidcScheme, _cookieScheme, subscribeToDiagnostics);

            var provider = services.BuildServiceProvider();

            // Assert config bind actions added correctly
            provider.GetRequiredService<IOptionsFactory<OpenIdConnectOptions>>().Create(_oidcScheme);
            provider.GetRequiredService<IOptionsFactory<MicrosoftIdentityOptions>>().Create(string.Empty);
            configMock.Received(3).GetSection(_configSectionName);

            AddMicrosoftWebApp_TestCommon(services, provider);
            AddMicrosoftWebApp_TestSubscribesToDiagnostics(services, diagnosticsMock, subscribeToDiagnostics);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void AddMicrosoftWebApp_WithConfigActionParameters(bool subscribeToDiagnostics)
        {
            var diagnosticsMock = Substitute.For<IOpenIdConnectMiddlewareDiagnostics>();

            var services = new ServiceCollection();
            services.AddDataProtection();

            new AuthenticationBuilder(services)
                .AddMicrosoftWebApp(_configureOidcOptions, _configureMsOptions, _oidcScheme, _cookieScheme, subscribeToDiagnostics);

            var provider = services.BuildServiceProvider();

            // Assert configure options actions added correctly
            var configuredOidcOptions = provider.GetServices<IConfigureOptions<OpenIdConnectOptions>>().Cast<ConfigureNamedOptions<OpenIdConnectOptions>>();
            var configuredMsOptions = provider.GetServices<IConfigureOptions<MicrosoftIdentityOptions>>().Cast<ConfigureNamedOptions<MicrosoftIdentityOptions>>();

            Assert.Contains(configuredOidcOptions, o => o.Action == _configureOidcOptions);
            Assert.Contains(configuredMsOptions, o => o.Action == _configureMsOptions);

            AddMicrosoftWebApp_TestCommon(services, provider);
            AddMicrosoftWebApp_TestSubscribesToDiagnostics(services, diagnosticsMock, subscribeToDiagnostics);
        }

        [Fact]
        public async Task AddMicrosoftWebApp_WithConfigNameParameters_TestRedirectToIdentityProviderEvent()
        {
            var configMock = Substitute.For<IConfiguration>();
            configMock.Configure().GetSection(_configSectionName).Returns(_configSection);

            var redirectFunc = Substitute.For<Func<RedirectContext, Task>>();
            var services = new ServiceCollection()
                .Configure<OpenIdConnectOptions>(_oidcScheme, (options) =>
                {
                    options.Events ??= new OpenIdConnectEvents();
                    options.Events.OnRedirectToIdentityProvider += redirectFunc;
                });
            services.AddDataProtection();

            new AuthenticationBuilder(services)
                .AddMicrosoftWebApp(configMock, _configSectionName, _oidcScheme, _cookieScheme, false);

            await AddMicrosoftWebApp_TestRedirectToIdentityProviderEvent(services, redirectFunc).ConfigureAwait(false);
        }

        [Fact]
        public async Task AddMicrosoftWebApp_WithConfigActionParameters_TestRedirectToIdentityProviderEvent()
        {
            var redirectFunc = Substitute.For<Func<RedirectContext, Task>>();
            var services = new ServiceCollection()
                .Configure<OpenIdConnectOptions>(_oidcScheme, (options) =>
                {
                    options.Events ??= new OpenIdConnectEvents();
                    options.Events.OnRedirectToIdentityProvider += redirectFunc;
                });

            services.AddDataProtection();
            new AuthenticationBuilder(services)
                    .AddMicrosoftWebApp(_configureOidcOptions, _configureMsOptions, _oidcScheme, _cookieScheme, false);

            await AddMicrosoftWebApp_TestRedirectToIdentityProviderEvent(services, redirectFunc).ConfigureAwait(false);
        }

        [Fact]
        public async Task AddMicrosoftWebApp_WithConfigNameParameters_TestB2cSpecificSetup()
        {
            var configMock = Substitute.For<IConfiguration>();
            _configSection = GetConfigSection(_configSectionName, true);
            configMock.Configure().GetSection(_configSectionName).Returns(_configSection);

            var remoteFailureFuncMock = Substitute.For<Func<RemoteFailureContext, Task>>();
            var services = new ServiceCollection()
                .Configure<OpenIdConnectOptions>(_oidcScheme, (options) =>
                {
                    options.Events ??= new OpenIdConnectEvents();
                    options.Events.OnRemoteFailure += remoteFailureFuncMock;
                });
            services.AddDataProtection();

            new AuthenticationBuilder(services)
                .AddMicrosoftWebApp(configMock, _configSectionName, _oidcScheme, _cookieScheme, false);

            await AddMicrosoftWebApp_TestB2cSpecificSetup(services, remoteFailureFuncMock).ConfigureAwait(false);
        }

        [Fact]
        public async Task AddMicrosoftWebApp_WithConfigActionParameters_B2cSpecificSetup()
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
                .Configure<OpenIdConnectOptions>(_oidcScheme, (options) =>
                {
                    options.Events ??= new OpenIdConnectEvents();
                    options.Events.OnRemoteFailure += remoteFailureFuncMock;
                });
            services.AddDataProtection();

            new AuthenticationBuilder(services)
                .AddMicrosoftWebApp(_configureOidcOptions, _configureMsOptions, _oidcScheme, _cookieScheme, false);

            await AddMicrosoftWebApp_TestB2cSpecificSetup(services, remoteFailureFuncMock).ConfigureAwait(false);
        }

        [Fact]
        public async Task AddWebAppCallsProtectedWebApi_WithConfigNameParameters()
        {
            var configMock = Substitute.For<IConfiguration>();
            var initialScopes = new List<string>() { "custom_scope" };
            var tokenAcquisitionMock = Substitute.For<ITokenAcquisition, ITokenAcquisitionInternal>();
            var authCodeReceivedFuncMock = Substitute.For<Func<AuthorizationCodeReceivedContext, Task>>();
            var tokenValidatedFuncMock = Substitute.For<Func<TokenValidatedContext, Task>>();
            var redirectFuncMock = Substitute.For<Func<RedirectContext, Task>>();
            var services = new ServiceCollection();

            var builder = services.AddAuthentication()
                .AddMicrosoftWebAppCallsWebApi(configMock, initialScopes, _configSectionName, _oidcScheme);
            services.Configure<OpenIdConnectOptions>(_oidcScheme, (options) =>
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

            configMock.Received(3).GetSection(_configSectionName);

            var oidcOptions = provider.GetRequiredService<IOptionsFactory<OpenIdConnectOptions>>().Create(_oidcScheme);

            AddWebAppCallsProtectedWebApi_TestCommon(services, provider, oidcOptions, initialScopes);
            await AddWebAppCallsProtectedWebApi_TestAuthorizationCodeReceivedEvent(provider, oidcOptions, authCodeReceivedFuncMock, tokenAcquisitionMock).ConfigureAwait(false);
            await AddWebAppCallsProtectedWebApi_TestTokenValidatedEvent(provider, oidcOptions, tokenValidatedFuncMock).ConfigureAwait(false);
            await AddWebAppCallsProtectedWebApi_TestRedirectToIdentityProviderForSignOutEvent(provider, oidcOptions, redirectFuncMock, tokenAcquisitionMock).ConfigureAwait(false);
        }

        [Fact]
        public async Task AddWebAppCallsProtectedWebApi_WithConfigActionParameters()
        {
            var initialScopes = new List<string>() { "custom_scope" };
            var tokenAcquisitionMock = Substitute.For<ITokenAcquisition, ITokenAcquisitionInternal>();
            var authCodeReceivedFuncMock = Substitute.For<Func<AuthorizationCodeReceivedContext, Task>>();
            var tokenValidatedFuncMock = Substitute.For<Func<TokenValidatedContext, Task>>();
            var redirectFuncMock = Substitute.For<Func<RedirectContext, Task>>();

            var services = new ServiceCollection();

            var builder = services.AddAuthentication()
                .AddMicrosoftWebAppCallsWebApi(initialScopes, _configureMsOptions, _configureAppOptions, _oidcScheme);
            services.Configure<OpenIdConnectOptions>(_oidcScheme, (options) =>
            {
                options.Events ??= new OpenIdConnectEvents();
                options.Events.OnAuthorizationCodeReceived += authCodeReceivedFuncMock;
                options.Events.OnTokenValidated += tokenValidatedFuncMock;
                options.Events.OnRedirectToIdentityProviderForSignOut += redirectFuncMock;
            });

            services.RemoveAll<ITokenAcquisition>();
            services.AddScoped<ITokenAcquisition>((provider) => tokenAcquisitionMock);

            var provider = services.BuildServiceProvider();

            // Assert configure options actions added correctly
            var configuredAppOptions = provider.GetServices<IConfigureOptions<ConfidentialClientApplicationOptions>>().Cast<ConfigureNamedOptions<ConfidentialClientApplicationOptions>>();
            var configuredMsOptions = provider.GetServices<IConfigureOptions<MicrosoftIdentityOptions>>().Cast<ConfigureNamedOptions<MicrosoftIdentityOptions>>();

            Assert.Contains(configuredAppOptions, o => o.Action == _configureAppOptions);
            Assert.Contains(configuredMsOptions, o => o.Action == _configureMsOptions);

            var oidcOptions = provider.GetRequiredService<IOptionsFactory<OpenIdConnectOptions>>().Create(_oidcScheme);

            AddWebAppCallsProtectedWebApi_TestCommon(services, provider, oidcOptions, initialScopes);
            await AddWebAppCallsProtectedWebApi_TestAuthorizationCodeReceivedEvent(provider, oidcOptions, authCodeReceivedFuncMock, tokenAcquisitionMock).ConfigureAwait(false);
            await AddWebAppCallsProtectedWebApi_TestTokenValidatedEvent(provider, oidcOptions, tokenValidatedFuncMock).ConfigureAwait(false);
            await AddWebAppCallsProtectedWebApi_TestRedirectToIdentityProviderForSignOutEvent(provider, oidcOptions, redirectFuncMock, tokenAcquisitionMock).ConfigureAwait(false);
        }

        [Fact]
        public void AddWebAppCallsProtectedWebApi_NoScopes()
        {
            // Arrange & Act
            var services = new ServiceCollection();

            services.AddAuthentication()
                .AddMicrosoftWebAppCallsWebApi(Substitute.For<IConfiguration>());

            var provider = services.BuildServiceProvider();

            var oidcOptions = provider.GetRequiredService<IOptionsFactory<OpenIdConnectOptions>>().Create(_oidcScheme);

            // Assert
            Assert.Equal(OpenIdConnectResponseType.IdToken, oidcOptions.ResponseType);
            Assert.Contains(OidcConstants.ScopeOpenId, oidcOptions.Scope);
            Assert.Contains(OidcConstants.ScopeProfile, oidcOptions.Scope);
        }

        [Theory]
        [InlineData("http://localhost:123")]
        [InlineData("https://localhost:123")]
        public async void AddMicrosoftWebApp_RedirectUri(string expectedUri)
        {
            _configureMsOptions = (options) =>
            {
                options.Instance = TestConstants.AadInstance;
                options.TenantId = TestConstants.TenantIdAsGuid;
                options.ClientId = TestConstants.ClientId;
            };

            var services = new ServiceCollection();
            services.AddDataProtection();
            new AuthenticationBuilder(services)
                .AddMicrosoftWebApp(_configureOidcOptions, _configureMsOptions, _oidcScheme, _cookieScheme);

            var provider = services.BuildServiceProvider();

            var oidcOptions = provider.GetRequiredService<IOptionsFactory<OpenIdConnectOptions>>().Create(_oidcScheme);

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

        private void AddMicrosoftWebApp_TestCommon(IServiceCollection services, ServiceProvider provider)
        {
            // Assert correct services added
            Assert.Contains(services, s => s.ServiceType == typeof(IConfigureOptions<OpenIdConnectOptions>));
            Assert.Contains(services, s => s.ServiceType == typeof(IConfigureOptions<MicrosoftIdentityOptions>));
            Assert.Contains(services, s => s.ServiceType == typeof(IOpenIdConnectMiddlewareDiagnostics));
            Assert.Equal(ServiceLifetime.Singleton, services.First(s => s.ServiceType == typeof(IOpenIdConnectMiddlewareDiagnostics)).Lifetime);
            Assert.Contains(services, s => s.ServiceType == typeof(IPostConfigureOptions<CookieAuthenticationOptions>));

            // Assert properties set
            var oidcOptions = provider.GetRequiredService<IOptionsFactory<OpenIdConnectOptions>>().Create(_oidcScheme);

            Assert.Equal(_cookieScheme, oidcOptions.SignInScheme);
            Assert.NotNull(oidcOptions.Authority);
            Assert.NotNull(oidcOptions.TokenValidationParameters.IssuerValidator);
            Assert.Equal(ClaimConstants.PreferredUserName, oidcOptions.TokenValidationParameters.NameClaimType);
        }

        private async Task AddMicrosoftWebApp_TestRedirectToIdentityProviderEvent(IServiceCollection services, Func<RedirectContext, Task> redirectFunc)
        {
            var provider = services.BuildServiceProvider();

            var oidcOptions = provider.GetRequiredService<IOptionsFactory<OpenIdConnectOptions>>().Create(_oidcScheme);

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

        private void AddMicrosoftWebApp_TestSubscribesToDiagnostics(IServiceCollection services, IOpenIdConnectMiddlewareDiagnostics diagnosticsMock, bool subscribeToDiagnostics)
        {
            services.RemoveAll<IOpenIdConnectMiddlewareDiagnostics>();
            services.AddSingleton<IOpenIdConnectMiddlewareDiagnostics>((provider) => diagnosticsMock);

            var provider = services.BuildServiceProvider();

            var oidcOptions = provider.GetRequiredService<IOptionsFactory<OpenIdConnectOptions>>().Create(_oidcScheme);

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

        private async Task AddMicrosoftWebApp_TestB2cSpecificSetup(IServiceCollection services, Func<RemoteFailureContext, Task> remoteFailureFuncMock)
        {
            var provider = services.BuildServiceProvider();

            var oidcOptions = provider.GetRequiredService<IOptionsFactory<OpenIdConnectOptions>>().Create(_oidcScheme);

            // Assert B2C name claim type
            Assert.Equal(ClaimConstants.Name, oidcOptions.TokenValidationParameters.NameClaimType);

            var (httpContext, authScheme, authProperties) = CreateContextParameters(provider);
            authProperties.Items[OidcConstants.PolicyKey] = TestConstants.B2CEditProfileUserFlow;

            var redirectContext = new RedirectContext(httpContext, authScheme, oidcOptions, authProperties);
            redirectContext.ProtocolMessage = new OpenIdConnectMessage() { IssuerAddress = $"IssuerAddress/{TestConstants.B2CSignUpSignInUserFlow}/" };

            (httpContext, authScheme, authProperties) = CreateContextParameters(provider);

            var remoteFailureContext = new RemoteFailureContext(httpContext, authScheme, new RemoteAuthenticationOptions(), new Exception());

            await oidcOptions.Events.RedirectToIdentityProvider(redirectContext).ConfigureAwait(false);
            await oidcOptions.Events.RemoteFailure(remoteFailureContext).ConfigureAwait(false);

            await remoteFailureFuncMock.ReceivedWithAnyArgs().Invoke(Arg.Any<RemoteFailureContext>()).ConfigureAwait(false);
            // Assert issuer is updated to non-default user flow
            Assert.Contains(TestConstants.B2CEditProfileUserFlow, redirectContext.ProtocolMessage.IssuerAddress);
            Assert.NotNull(redirectContext.ProtocolMessage.Parameters["client_info"]);
            Assert.Equal("1", redirectContext.ProtocolMessage.Parameters["client_info"].ToString(CultureInfo.InvariantCulture));
        }

        private void AddWebAppCallsProtectedWebApi_TestCommon(IServiceCollection services, ServiceProvider provider, OpenIdConnectOptions oidcOptions, IEnumerable<string> initialScopes)
        {
            // Assert correct services added
            Assert.Contains(services, s => s.ServiceType == typeof(IHttpContextAccessor));
            Assert.Contains(services, s => s.ServiceType == typeof(ITokenAcquisition));
            Assert.Contains(services, s => s.ServiceType == typeof(IConfigureOptions<ConfidentialClientApplicationOptions>));
            Assert.Contains(services, s => s.ServiceType == typeof(IConfigureOptions<MicrosoftIdentityOptions>));
            Assert.Contains(services, s => s.ServiceType == typeof(IConfigureOptions<OpenIdConnectOptions>));

            // Assert OIDC options added correctly
            var configuredOidcOptions = provider.GetService<IConfigureOptions<OpenIdConnectOptions>>() as ConfigureNamedOptions<OpenIdConnectOptions>;

            Assert.Equal(_oidcScheme, configuredOidcOptions.Name);

            // Assert properties set
            Assert.Equal(OpenIdConnectResponseType.CodeIdToken, oidcOptions.ResponseType);
            Assert.Contains(OidcConstants.ScopeOfflineAccess, oidcOptions.Scope);
            Assert.All(initialScopes, scope => Assert.Contains(scope, oidcOptions.Scope));
        }

        private async Task AddWebAppCallsProtectedWebApi_TestAuthorizationCodeReceivedEvent(
            IServiceProvider provider,
            OpenIdConnectOptions oidcOptions,
            Func<AuthorizationCodeReceivedContext, Task> authCodeReceivedFuncMock,
            ITokenAcquisition tokenAcquisitionMock)
        {
            var (httpContext, authScheme, authProperties) = CreateContextParameters(provider);

            await oidcOptions.Events.AuthorizationCodeReceived(new AuthorizationCodeReceivedContext(httpContext, authScheme, oidcOptions, authProperties)).ConfigureAwait(false);

            // Assert original AuthorizationCodeReceived event and TokenAcquisition method were called
            await authCodeReceivedFuncMock.ReceivedWithAnyArgs().Invoke(Arg.Any<AuthorizationCodeReceivedContext>()).ConfigureAwait(false);
            await ((ITokenAcquisitionInternal)tokenAcquisitionMock).ReceivedWithAnyArgs().AddAccountToCacheFromAuthorizationCodeAsync(Arg.Any<AuthorizationCodeReceivedContext>(), Arg.Any<IEnumerable<string>>()).ConfigureAwait(false);
        }

        private async Task AddWebAppCallsProtectedWebApi_TestTokenValidatedEvent(IServiceProvider provider, OpenIdConnectOptions oidcOptions, Func<TokenValidatedContext, Task> tokenValidatedFuncMock)
        {
            var (httpContext, authScheme, authProperties) = CreateContextParameters(provider);
            var tokenValidatedContext = new TokenValidatedContext(httpContext, authScheme, oidcOptions, httpContext.User, authProperties);

            await oidcOptions.Events.TokenValidated(tokenValidatedContext).ConfigureAwait(false);

            // Assert original TokenValidated event was called; properties were set
            await tokenValidatedFuncMock.ReceivedWithAnyArgs().Invoke(Arg.Any<TokenValidatedContext>()).ConfigureAwait(false);
            Assert.True(tokenValidatedContext.Principal.HasClaim(c => c.Type == ClaimConstants.UniqueTenantIdentifier));
            Assert.True(tokenValidatedContext.Principal.HasClaim(c => c.Type == ClaimConstants.UniqueObjectIdentifier));
        }

        private async Task AddWebAppCallsProtectedWebApi_TestRedirectToIdentityProviderForSignOutEvent(
            IServiceProvider provider,
            OpenIdConnectOptions oidcOptions,
            Func<RedirectContext, Task> redirectFuncMock,
            ITokenAcquisition tokenAcquisitionMock)
        {
            var (httpContext, authScheme, authProperties) = CreateContextParameters(provider);

            await oidcOptions.Events.RedirectToIdentityProviderForSignOut(new RedirectContext(httpContext, authScheme, oidcOptions, authProperties)).ConfigureAwait(false);

            // Assert original RedirectToIdentityProviderForSignOut event and TokenAcquisition method were called
            await redirectFuncMock.ReceivedWithAnyArgs().Invoke(Arg.Any<RedirectContext>()).ConfigureAwait(false);
            await ((ITokenAcquisitionInternal)tokenAcquisitionMock).ReceivedWithAnyArgs().RemoveAccountAsync(Arg.Any<RedirectContext>()).ConfigureAwait(false);
        }

        private (HttpContext, AuthenticationScheme, AuthenticationProperties) CreateContextParameters(IServiceProvider provider)
        {
            var httpContext = HttpContextUtilities.CreateHttpContext();
            httpContext.RequestServices = provider;
            httpContext.Request.Form = new FormCollection(
                new Dictionary<string, StringValues>() { { ClaimConstants.ClientInfo, Base64UrlHelpers.Encode($"{{\"uid\":\"{TestConstants.Uid}\",\"utid\":\"{TestConstants.Utid}\"}}") } });
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
    }
}
