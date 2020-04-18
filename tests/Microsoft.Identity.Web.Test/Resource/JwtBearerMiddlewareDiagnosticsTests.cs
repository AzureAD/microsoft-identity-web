// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Web.Resource;
using Microsoft.Identity.Web.Test.Common.Mocks;
using Microsoft.Identity.Web.Test.Common.TestHelpers;
using NSubstitute;
using Xunit;

namespace Microsoft.Identity.Web.Test.Resource
{
    /// <remarks>
    /// See the class level comments in <see cref="Microsoft.Identity.Web.Test.Common.Mocks.LoggerMock"/> for more information.
    /// </remarks>
    public class JwtBearerMiddlewareDiagnosticsTests
    {
        private bool _customEventWasRaised;
        private HttpContext _httpContext;
        private ILogger<JwtBearerMiddlewareDiagnostics> _logger;
        private JwtBearerMiddlewareDiagnostics _jwtDiagnostics;
        private JwtBearerOptions _jwtOptions;
        private JwtBearerEvents _jwtEvents;
        private Func<BaseContext<JwtBearerOptions>, Task> _eventHandler;
        private AuthenticationScheme _authScheme;

        public JwtBearerMiddlewareDiagnosticsTests()
        {
            _customEventWasRaised = false;
            _httpContext = HttpContextUtilities.CreateHttpContext();
            _logger = Substitute.For<ILogger<JwtBearerMiddlewareDiagnostics>>();
            _jwtDiagnostics = new JwtBearerMiddlewareDiagnostics(new LoggerMock<JwtBearerMiddlewareDiagnostics>(_logger));
            _jwtOptions = new JwtBearerOptions();
            _jwtEvents = new JwtBearerEvents();
            _authScheme = new AuthenticationScheme(JwtBearerDefaults.AuthenticationScheme, JwtBearerDefaults.AuthenticationScheme, typeof(JwtBearerHandler));
            _eventHandler = (context) =>
            {
                _customEventWasRaised = true;
                return Task.CompletedTask;
            };
        }

        [Fact]
        public async void Subscribe_OnAuthenticationFailed_CompletesSuccessfully()
        {
            _jwtEvents.OnAuthenticationFailed = _eventHandler;
            _jwtDiagnostics.Subscribe(_jwtEvents);
            await _jwtEvents.AuthenticationFailed(new AuthenticationFailedContext(_httpContext, _authScheme, _jwtOptions)).ConfigureAwait(false);

            AssertSuccess();
        }

        [Fact]
        public async void Subscribe_OnMessageReceived_CompletesSuccessfully()
        {
            _jwtEvents.OnMessageReceived = _eventHandler;
            _jwtDiagnostics.Subscribe(_jwtEvents);
            await _jwtEvents.MessageReceived(new MessageReceivedContext(_httpContext, _authScheme, _jwtOptions)).ConfigureAwait(false);

            AssertSuccess();
        }

        [Fact]
        public async void Subscribe_OnTokenValidated_CompletesSuccessfully()
        {
            _jwtEvents.OnTokenValidated = _eventHandler;
            _jwtDiagnostics.Subscribe(_jwtEvents);
            await _jwtEvents.TokenValidated(new TokenValidatedContext(_httpContext, _authScheme, _jwtOptions)).ConfigureAwait(false);

            AssertSuccess();
        }

        [Fact]
        public async void Subscribe_OnChallenge_CompletesSuccessfully()
        {
            _jwtEvents.OnChallenge = _eventHandler;
            _jwtDiagnostics.Subscribe(_jwtEvents);
            await _jwtEvents.Challenge(new JwtBearerChallengeContext(_httpContext, _authScheme, _jwtOptions, new AuthenticationProperties())).ConfigureAwait(false);

            AssertSuccess();
        }

        [Fact]
        public async void Subscribe_OnAuthenticationFailedDefault_CompletesSuccessfully()
        {
            _jwtEvents = _jwtDiagnostics.Subscribe(null);
            await _jwtEvents.AuthenticationFailed(new AuthenticationFailedContext(_httpContext, _authScheme, _jwtOptions)).ConfigureAwait(false);

            AssertSuccess(false);
        }

        [Fact]
        public async void Subscribe_OnMessageReceivedDefault_CompletesSuccessfully()
        {
            _jwtEvents = _jwtDiagnostics.Subscribe(_jwtEvents);
            await _jwtEvents.MessageReceived(new MessageReceivedContext(_httpContext, _authScheme, _jwtOptions)).ConfigureAwait(false);

            AssertSuccess(false);
        }

        [Fact]
        public async void Subscribe_OnTokenValidatedDefault_CompletesSuccessfully()
        {
            _jwtEvents = _jwtDiagnostics.Subscribe(_jwtEvents);
            await _jwtEvents.TokenValidated(new TokenValidatedContext(_httpContext, _authScheme, _jwtOptions)).ConfigureAwait(false);

            AssertSuccess(false);
        }

        [Fact]
        public async void Subscribe_OnChallengeDefault_CompletesSuccessfully()
        {
            _jwtEvents = _jwtDiagnostics.Subscribe(_jwtEvents);
            await _jwtEvents.Challenge(new JwtBearerChallengeContext(_httpContext, _authScheme, _jwtOptions, new AuthenticationProperties())).ConfigureAwait(false);

            AssertSuccess(false);
        }

        private void AssertSuccess(bool expectedCustomEventWasRaised = true)
        {
            Assert.Equal(expectedCustomEventWasRaised, _customEventWasRaised);
            _logger.Received().Log(Arg.Any<LogLevel>(), Arg.Any<EventId>(), Arg.Any<object>(), Arg.Any<Exception>(), Arg.Any<Func<object, Exception, string>>());
        }
    }
}
