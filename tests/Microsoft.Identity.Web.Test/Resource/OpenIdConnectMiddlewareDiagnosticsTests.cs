// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Web.Resource;
using Microsoft.Identity.Web.Test.Common.Mocks;
using Microsoft.Identity.Web.Test.Common.TestHelpers;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using NSubstitute;
using Xunit;

namespace Microsoft.Identity.Web.Test.Resource
{
    /// <remarks>
    /// See the class level comments in <see cref="Microsoft.Identity.Web.Test.Common.Mocks.LoggerMock"/> for more information.
    /// </remarks>
    public class OpenIdConnectMiddlewareDiagnosticsTests
    {
        private bool _customEventWasRaised;
        private HttpContext _httpContext;
        private ILogger<OpenIdConnectMiddlewareDiagnostics> _logger;
        private OpenIdConnectMiddlewareDiagnostics _openIdDiagnostics;
        private OpenIdConnectOptions _openIdOptions;
        private OpenIdConnectEvents _openIdEvents;
        private AuthenticationProperties _authProperties;
        private Func<BaseContext<OpenIdConnectOptions>, Task> _eventHandler;
        private AuthenticationScheme _authScheme;

        public OpenIdConnectMiddlewareDiagnosticsTests()
        {
            _customEventWasRaised = false;
            _httpContext = HttpContextUtilities.CreateHttpContext();
            _logger = Substitute.For<ILogger<OpenIdConnectMiddlewareDiagnostics>>();
            _openIdDiagnostics = new OpenIdConnectMiddlewareDiagnostics(new LoggerMock<OpenIdConnectMiddlewareDiagnostics>(_logger));
            _openIdOptions = new OpenIdConnectOptions();
            _openIdEvents = new OpenIdConnectEvents();
            _authProperties = new AuthenticationProperties();
            _authScheme = new AuthenticationScheme(OpenIdConnectDefaults.AuthenticationScheme, OpenIdConnectDefaults.AuthenticationScheme, typeof(OpenIdConnectHandler));
            _eventHandler = (context) =>
            {
                _customEventWasRaised = true;
                return Task.CompletedTask;
            };
        }

        [Fact]
        public async Task Subscribe_OnRedirectToIdentityProvider_CompletesSuccessfullyAsync()
        {
            _openIdEvents.OnRedirectToIdentityProvider = _eventHandler;
            _openIdDiagnostics.Subscribe(_openIdEvents);
            await _openIdEvents.RedirectToIdentityProvider(new RedirectContext(_httpContext, _authScheme, _openIdOptions, _authProperties) { ProtocolMessage = new OpenIdConnectMessage() });

            AssertSuccess();
        }

        [Fact]
        public async Task Subscribe_OnMessageReceived_CompletesSuccessfullyAsync()
        {
            _openIdEvents.OnMessageReceived = _eventHandler;
            _openIdDiagnostics.Subscribe(_openIdEvents);
            await _openIdEvents.MessageReceived(new MessageReceivedContext(_httpContext, _authScheme, _openIdOptions, _authProperties) { ProtocolMessage = new OpenIdConnectMessage() });

            AssertSuccess();
        }

        [Fact]
        public async Task Subscribe_OnAuthorizationCodeReceived_CompletesSuccessfullyAsync()
        {
            _openIdEvents.OnAuthorizationCodeReceived = _eventHandler;
            _openIdDiagnostics.Subscribe(_openIdEvents);
            await _openIdEvents.AuthorizationCodeReceived(new AuthorizationCodeReceivedContext(_httpContext, _authScheme, _openIdOptions, _authProperties));

            AssertSuccess();
        }

        [Fact]
        public async Task Subscribe_OnTokenResponseReceived_CompletesSuccessfullyAsync()
        {
            _openIdEvents.OnTokenResponseReceived = _eventHandler;
            _openIdDiagnostics.Subscribe(_openIdEvents);
            await _openIdEvents.TokenResponseReceived(new TokenResponseReceivedContext(_httpContext, _authScheme, _openIdOptions, _httpContext.User, _authProperties));

            AssertSuccess();
        }

        [Fact]
        public async Task Subscribe_OnTokenValidated_CompletesSuccessfullyAsync()
        {
            _openIdEvents.OnTokenValidated = _eventHandler;
            _openIdDiagnostics.Subscribe(_openIdEvents);
            await _openIdEvents.TokenValidated(new TokenValidatedContext(_httpContext, _authScheme, _openIdOptions, _httpContext.User, _authProperties));

            AssertSuccess();
        }

        [Fact]
        public async Task Subscribe_OnUserInformationReceived_CompletesSuccessfullyAsync()
        {
            _openIdEvents.OnUserInformationReceived = _eventHandler;
            _openIdDiagnostics.Subscribe(_openIdEvents);
            await _openIdEvents.UserInformationReceived(new UserInformationReceivedContext(_httpContext, _authScheme, _openIdOptions, _httpContext.User, _authProperties));

            AssertSuccess();
        }

        [Fact]
        public async Task Subscribe_OnAuthenticationFailed_CompletesSuccessfullyAsync()
        {
            _openIdEvents.OnAuthenticationFailed = _eventHandler;
            _openIdDiagnostics.Subscribe(_openIdEvents);
            await _openIdEvents.AuthenticationFailed(new AuthenticationFailedContext(_httpContext, _authScheme, _openIdOptions));

            AssertSuccess();
        }

        [Fact]
        public async Task Subscribe_OnRemoteSignOut_CompletesSuccessfullyAsync()
        {
            _openIdEvents.OnRemoteSignOut = _eventHandler;
            _openIdDiagnostics.Subscribe(_openIdEvents);
            await _openIdEvents.RemoteSignOut(new RemoteSignOutContext(_httpContext, _authScheme, _openIdOptions, new OpenIdConnectMessage()));

            AssertSuccess();
        }

        [Fact]
        public async Task Subscribe_OnRedirectToIdentityProviderForSignOut_CompletesSuccessfullyAsync()
        {
            _openIdEvents.OnRedirectToIdentityProviderForSignOut = _eventHandler;
            _openIdDiagnostics.Subscribe(_openIdEvents);
            await _openIdEvents.RedirectToIdentityProviderForSignOut(new RedirectContext(_httpContext, _authScheme, _openIdOptions, _authProperties));

            AssertSuccess();
        }

        [Fact]
        public async Task Subscribe_OnSignedOutCallbackRedirect_CompletesSuccessfullyAsync()
        {
            _openIdEvents.OnSignedOutCallbackRedirect = _eventHandler;
            _openIdDiagnostics.Subscribe(_openIdEvents);
            await _openIdEvents.SignedOutCallbackRedirect(new RemoteSignOutContext(_httpContext, _authScheme, _openIdOptions, new OpenIdConnectMessage()));

            AssertSuccess();
        }

        private void AssertSuccess()
        {
            Assert.True(_customEventWasRaised);
            _logger.Received().Log(Arg.Any<LogLevel>(), Arg.Any<EventId>(), Arg.Any<object>(), Arg.Any<Exception>(), Arg.Any<Func<object, Exception?, string>>());
        }
    }
}
