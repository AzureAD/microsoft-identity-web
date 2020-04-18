// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Logging;

namespace Microsoft.Identity.Web.Resource
{
    /// <summary>
    /// Diagnostics for the JwtBearer middleware (used in Web APIs).
    /// </summary>
    public class JwtBearerMiddlewareDiagnostics : IJwtBearerMiddlewareDiagnostics
    {
        private readonly ILogger _logger;

        public JwtBearerMiddlewareDiagnostics(ILogger<JwtBearerMiddlewareDiagnostics> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Invoked if exceptions are thrown during request processing. The exceptions will be re-thrown after this event unless suppressed.
        /// </summary>
        private Func<AuthenticationFailedContext, Task> s_onAuthenticationFailed;

        /// <summary>
        /// Invoked when a protocol message is first received.
        /// </summary>
        private Func<MessageReceivedContext, Task> s_onMessageReceived;

        /// <summary>
        /// Invoked after the security token has passed validation and a ClaimsIdentity has been generated.
        /// </summary>
        private Func<TokenValidatedContext, Task> s_onTokenValidated;

        /// <summary>
        /// Invoked before a challenge is sent back to the caller.
        /// </summary>
        private Func<JwtBearerChallengeContext, Task> s_onChallenge;

        /// <summary>
        /// Subscribes to all the JwtBearer events, to help debugging, while
        /// preserving the previous handlers (which are called).
        /// </summary>
        /// <param name="events">Events to subscribe to.</param>
        public JwtBearerEvents Subscribe(JwtBearerEvents events)
        {
            if (events == null)
            {
                events = new JwtBearerEvents();
            }

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
            _logger.LogDebug($"1. Begin {nameof(OnMessageReceivedAsync)}");

            // Place a breakpoint here and examine the bearer token (context.Request.Headers.HeaderAuthorization / context.Request.Headers["Authorization"])
            // Use https://jwt.ms to decode the token and observe claims
            await s_onMessageReceived(context).ConfigureAwait(false);
            _logger.LogDebug($"1. End - {nameof(OnMessageReceivedAsync)}");
        }

        private async Task OnAuthenticationFailedAsync(AuthenticationFailedContext context)
        {
            _logger.LogDebug($"99. Begin {nameof(OnAuthenticationFailedAsync)}");

            // Place a breakpoint here and examine context.Exception
            await s_onAuthenticationFailed(context).ConfigureAwait(false);
            _logger.LogDebug($"99. End - {nameof(OnAuthenticationFailedAsync)}");
        }

        private async Task OnTokenValidatedAsync(TokenValidatedContext context)
        {
            _logger.LogDebug($"2. Begin {nameof(OnTokenValidatedAsync)}");
            await s_onTokenValidated(context).ConfigureAwait(false);
            _logger.LogDebug($"2. End - {nameof(OnTokenValidatedAsync)}");
        }

        private async Task OnChallengeAsync(JwtBearerChallengeContext context)
        {
            _logger.LogDebug($"55. Begin {nameof(OnChallengeAsync)}");
            await s_onChallenge(context).ConfigureAwait(false);
            _logger.LogDebug($"55. End - {nameof(OnChallengeAsync)}");
        }
    }
}
