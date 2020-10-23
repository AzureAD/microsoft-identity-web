// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Logging;

namespace Microsoft.Identity.Web.Resource
{
    /// <summary>
    /// Diagnostics for the JwtBearer middleware (used in web APIs).
    /// </summary>
    public class JwtBearerMiddlewareDiagnostics : IJwtBearerMiddlewareDiagnostics
    {
        private readonly ILogger _logger;

        /// <summary>
        /// Constructor for a <see cref="JwtBearerMiddlewareDiagnostics"/>. This constructor
        /// is used by dependency injection.
        /// </summary>
        /// <param name="logger">Logger.</param>
        public JwtBearerMiddlewareDiagnostics(ILogger<JwtBearerMiddlewareDiagnostics> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Invoked if exceptions are thrown during request processing. The exceptions will be re-thrown after this event unless suppressed.
        /// </summary>
        private Func<AuthenticationFailedContext, Task> s_onAuthenticationFailed = null!;

        /// <summary>
        /// Invoked when a protocol message is first received.
        /// </summary>
        private Func<MessageReceivedContext, Task> s_onMessageReceived = null!;

        /// <summary>
        /// Invoked after the security token has passed validation and a ClaimsIdentity has been generated.
        /// </summary>
        private Func<TokenValidatedContext, Task> s_onTokenValidated = null!;

        /// <summary>
        /// Invoked before a challenge is sent back to the caller.
        /// </summary>
        private Func<JwtBearerChallengeContext, Task> s_onChallenge = null!;

        /// <summary>
        /// Subscribes to all the JwtBearer events, to help debugging, while
        /// preserving the previous handlers (which are called).
        /// </summary>
        /// <param name="events">Events to subscribe to.</param>
        /// <returns><see cref="JwtBearerEvents"/> for chaining.</returns>
        public JwtBearerEvents Subscribe(JwtBearerEvents events)
        {
            events ??= new JwtBearerEvents();

            s_onAuthenticationFailed = events.OnAuthenticationFailed;
            events.OnAuthenticationFailed = OnAuthenticationFailedAsync;

            s_onMessageReceived = events.OnMessageReceived;
            events.OnMessageReceived = OnMessageReceivedAsync;

            s_onTokenValidated = events.OnTokenValidated;
            events.OnTokenValidated = OnTokenValidatedAsync;

            s_onChallenge = events.OnChallenge;
            events.OnChallenge = OnChallengeAsync;

            return events;
        }

        private async Task OnMessageReceivedAsync(MessageReceivedContext context)
        {
            _logger.LogDebug(string.Format(CultureInfo.InvariantCulture, LogMessages.MethodBegin, nameof(OnMessageReceivedAsync)));

            // Place a breakpoint here and examine the bearer token (context.Request.Headers.HeaderAuthorization / context.Request.Headers["Authorization"])
            // Use https://jwt.ms to decode the token and observe claims
            await s_onMessageReceived(context).ConfigureAwait(false);
            _logger.LogDebug(string.Format(CultureInfo.InvariantCulture, LogMessages.MethodEnd, nameof(OnMessageReceivedAsync)));
        }

        private async Task OnAuthenticationFailedAsync(AuthenticationFailedContext context)
        {
            _logger.LogDebug(string.Format(CultureInfo.InvariantCulture, LogMessages.MethodBegin, nameof(OnAuthenticationFailedAsync)));

            // Place a breakpoint here and examine context.Exception
            await s_onAuthenticationFailed(context).ConfigureAwait(false);
            _logger.LogDebug(string.Format(CultureInfo.InvariantCulture, LogMessages.MethodEnd, nameof(OnAuthenticationFailedAsync)));
        }

        private async Task OnTokenValidatedAsync(TokenValidatedContext context)
        {
            _logger.LogDebug(string.Format(CultureInfo.InvariantCulture, LogMessages.MethodBegin, nameof(OnTokenValidatedAsync)));
            await s_onTokenValidated(context).ConfigureAwait(false);
            _logger.LogDebug(string.Format(CultureInfo.InvariantCulture, LogMessages.MethodEnd, nameof(OnTokenValidatedAsync)));
        }

        private async Task OnChallengeAsync(JwtBearerChallengeContext context)
        {
            _logger.LogDebug(string.Format(CultureInfo.InvariantCulture, LogMessages.MethodBegin, nameof(OnChallengeAsync)));
            await s_onChallenge(context).ConfigureAwait(false);
            _logger.LogDebug(string.Format(CultureInfo.InvariantCulture, LogMessages.MethodEnd, nameof(OnChallengeAsync)));
        }
    }
}
