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
        public async Task Subscribe_OnAuthenticationFailed_CompletesSuccessfullyAsync()
        {
            _jwtEvents.OnAuthenticationFailed = _eventHandler;
            _jwtDiagnostics.Subscribe(_jwtEvents);
            await _jwtEvents.AuthenticationFailed(new AuthenticationFailedContext(_httpContext, _authScheme, _jwtOptions));

            AssertSuccess();
        }

        [Fact]
        public async Task Subscribe_OnMessageReceived_CompletesSuccessfullyAsync()
        {
            _jwtEvents.OnMessageReceived = _eventHandler;
            _jwtDiagnostics.Subscribe(_jwtEvents);
            await _jwtEvents.MessageReceived(new MessageReceivedContext(_httpContext, _authScheme, _jwtOptions));

            AssertSuccess();
        }

        [Fact]
        public async Task Subscribe_OnTokenValidated_CompletesSuccessfullyAsync()
        {
            _jwtEvents.OnTokenValidated = _eventHandler;
            _jwtDiagnostics.Subscribe(_jwtEvents);
            await _jwtEvents.TokenValidated(new TokenValidatedContext(_httpContext, _authScheme, _jwtOptions));

            AssertSuccess();
        }

        [Fact]
        public async Task Subscribe_OnChallenge_CompletesSuccessfullyAsync()
        {
            _jwtEvents.OnChallenge = _eventHandler;
            _jwtDiagnostics.Subscribe(_jwtEvents);
            await _jwtEvents.Challenge(new JwtBearerChallengeContext(_httpContext, _authScheme, _jwtOptions, new AuthenticationProperties()));

            AssertSuccess();
        }

        [Fact]
        public async Task Subscribe_OnAuthenticationFailedDefault_CompletesSuccessfullyAsync()
        {
            _jwtEvents = _jwtDiagnostics.Subscribe(null!);
            await _jwtEvents.AuthenticationFailed(new AuthenticationFailedContext(_httpContext, _authScheme, _jwtOptions));

            AssertSuccess(false);
        }

        [Fact]
        public async Task Subscribe_OnMessageReceivedDefault_CompletesSuccessfullyAsync()
        {
            _jwtEvents = _jwtDiagnostics.Subscribe(_jwtEvents);
            await _jwtEvents.MessageReceived(new MessageReceivedContext(_httpContext, _authScheme, _jwtOptions));

            AssertSuccess(false);
        }

        [Fact]
        public async Task Subscribe_OnTokenValidatedDefault_CompletesSuccessfullyAsync()
        {
            _jwtEvents = _jwtDiagnostics.Subscribe(_jwtEvents);
            await _jwtEvents.TokenValidated(new TokenValidatedContext(_httpContext, _authScheme, _jwtOptions));

            AssertSuccess(false);
        }

        [Fact]
        public async Task Subscribe_OnChallengeDefault_CompletesSuccessfullyAsync()
        {
            _jwtEvents = _jwtDiagnostics.Subscribe(_jwtEvents);
            await _jwtEvents.Challenge(new JwtBearerChallengeContext(_httpContext, _authScheme, _jwtOptions, new AuthenticationProperties()));

            AssertSuccess(false);
        }

        private void AssertSuccess(bool expectedCustomEventWasRaised = true)
        {
            Assert.Equal(expectedCustomEventWasRaised, _customEventWasRaised);
            _logger.Received().Log(Arg.Any<LogLevel>(), Arg.Any<EventId>(), Arg.Any<object>(), Arg.Any<Exception>(), Arg.Any<Func<object, Exception?, string>>());
        }
    }
}
