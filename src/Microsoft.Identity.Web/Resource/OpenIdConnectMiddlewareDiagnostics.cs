// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Microsoft.Identity.Web.Resource
{
    /// <summary>
    /// Diagnostics used in the OpenID Connect middleware
    /// (used in web apps).
    /// </summary>
    public class OpenIdConnectMiddlewareDiagnostics : IOpenIdConnectMiddlewareDiagnostics
    {
        private readonly ILogger _logger;

        /// <summary>
        /// Constructor of the <see cref="OpenIdConnectMiddlewareDiagnostics"/>, used
        /// by dependency injection.
        /// </summary>
        /// <param name="logger">Logger used to log the diagnostics.</param>
        public OpenIdConnectMiddlewareDiagnostics(ILogger<OpenIdConnectMiddlewareDiagnostics> logger)
        {
            _logger = logger;
        }

        /// <summary>
        ///  Invoked before redirecting to the identity provider to authenticate. This can
        ///  be used to set ProtocolMessage.State that will be persisted through the authentication
        ///  process. The ProtocolMessage can also be used to add or customize parameters
        ///  sent to the identity provider.
        /// </summary>
        private Func<RedirectContext, Task> _onRedirectToIdentityProvider = null!;

        /// <summary>
        /// Invoked when a protocol message is first received.
        /// </summary>
        private Func<MessageReceivedContext, Task> _onMessageReceived = null!;

        /// <summary>
        ///  Invoked after security token validation if an authorization code is present
        ///  in the protocol message.
        /// </summary>
        private Func<AuthorizationCodeReceivedContext, Task> _onAuthorizationCodeReceived = null!;

        /// <summary>
        /// Invoked after "authorization code" is redeemed for tokens at the token endpoint.
        /// </summary>
        private Func<TokenResponseReceivedContext, Task> _onTokenResponseReceived = null!;

        /// <summary>
        /// Invoked when an IdToken has been validated and produced an AuthenticationTicket.
        /// </summary>
        private Func<TokenValidatedContext, Task> _onTokenValidated = null!;

        /// <summary>
        /// Invoked when user information is retrieved from the UserInfoEndpoint.
        /// </summary>
        private Func<UserInformationReceivedContext, Task> _onUserInformationReceived = null!;

        /// <summary>
        /// Invoked if exceptions are thrown during request processing. The exceptions will
        /// be re-thrown after this event unless suppressed.
        /// </summary>
        private Func<AuthenticationFailedContext, Task> _onAuthenticationFailed = null!;

        /// <summary>
        /// Invoked when a request is received on the RemoteSignOutPath.
        /// </summary>
        private Func<RemoteSignOutContext, Task> _onRemoteSignOut = null!;

        /// <summary>
        /// Invoked before redirecting to the identity provider to sign out.
        /// </summary>
        private Func<RedirectContext, Task> _onRedirectToIdentityProviderForSignOut = null!;

        /// <summary>
        /// Invoked before redirecting to the Microsoft.AspNetCore.Authentication.OpenIdConnect.OpenIdConnectOptions.SignedOutRedirectUri
        /// at the end of a remote sign-out flow.
        /// </summary>
        private Func<RemoteSignOutContext, Task> _onSignedOutCallbackRedirect = null!;

        /// <summary>
        /// Subscribes to all the OpenIdConnect events, to help debugging, while
        /// preserving the previous handlers (which are called).
        /// </summary>
        /// <param name="events">Events to subscribe to.</param>
        public void Subscribe(OpenIdConnectEvents events)
        {
            events ??= new OpenIdConnectEvents();

            _onRedirectToIdentityProvider = events.OnRedirectToIdentityProvider;
            events.OnRedirectToIdentityProvider = OnRedirectToIdentityProviderAsync;

            _onMessageReceived = events.OnMessageReceived;
            events.OnMessageReceived = OnMessageReceivedAsync;

            _onAuthorizationCodeReceived = events.OnAuthorizationCodeReceived;
            events.OnAuthorizationCodeReceived = OnAuthorizationCodeReceivedAsync;

            _onTokenResponseReceived = events.OnTokenResponseReceived;
            events.OnTokenResponseReceived = OnTokenResponseReceivedAsync;

            _onTokenValidated = events.OnTokenValidated;
            events.OnTokenValidated = OnTokenValidatedAsync;

            _onUserInformationReceived = events.OnUserInformationReceived;
            events.OnUserInformationReceived = OnUserInformationReceivedAsync;

            _onAuthenticationFailed = events.OnAuthenticationFailed;
            events.OnAuthenticationFailed = OnAuthenticationFailedAsync;

            _onRemoteSignOut = events.OnRemoteSignOut;
            events.OnRemoteSignOut = OnRemoteSignOutAsync;

            _onRedirectToIdentityProviderForSignOut = events.OnRedirectToIdentityProviderForSignOut;
            events.OnRedirectToIdentityProviderForSignOut = OnRedirectToIdentityProviderForSignOutAsync;

            _onSignedOutCallbackRedirect = events.OnSignedOutCallbackRedirect;
            events.OnSignedOutCallbackRedirect = OnSignedOutCallbackRedirectAsync;
        }

        private async Task OnRedirectToIdentityProviderAsync(RedirectContext context)
        {
            _logger.LogDebug(string.Format(CultureInfo.InvariantCulture, LogMessages.MethodBegin, nameof(OnRedirectToIdentityProviderAsync)));

            await _onRedirectToIdentityProvider(context).ConfigureAwait(false);

            _logger.LogDebug("   Sending OpenIdConnect message:");
            DisplayProtocolMessage(context.ProtocolMessage);
            _logger.LogDebug(string.Format(CultureInfo.InvariantCulture, LogMessages.MethodEnd, nameof(OnRedirectToIdentityProviderAsync)));
        }

        private void DisplayProtocolMessage(OpenIdConnectMessage message)
        {
            foreach (var property in typeof(OpenIdConnectMessage).GetProperties())
            {
                object? value = property.GetValue(message);
                if (value != null)
                {
                    _logger.LogDebug($"   - {property.Name}={value}");
                }
            }
        }

        private async Task OnMessageReceivedAsync(MessageReceivedContext context)
        {
            _logger.LogDebug(string.Format(CultureInfo.InvariantCulture, LogMessages.MethodBegin, nameof(OnMessageReceivedAsync)));
            _logger.LogDebug("   Received from STS the OpenIdConnect message:");
            DisplayProtocolMessage(context.ProtocolMessage);
            await _onMessageReceived(context).ConfigureAwait(false);
            _logger.LogDebug(string.Format(CultureInfo.InvariantCulture, LogMessages.MethodEnd, nameof(OnMessageReceivedAsync)));
        }

        private async Task OnAuthorizationCodeReceivedAsync(AuthorizationCodeReceivedContext context)
        {
            _logger.LogDebug(string.Format(CultureInfo.InvariantCulture, LogMessages.MethodBegin, nameof(OnAuthorizationCodeReceivedAsync)));
            await _onAuthorizationCodeReceived(context).ConfigureAwait(false);
            _logger.LogDebug(string.Format(CultureInfo.InvariantCulture, LogMessages.MethodEnd, nameof(OnAuthorizationCodeReceivedAsync)));
        }

        private async Task OnTokenResponseReceivedAsync(TokenResponseReceivedContext context)
        {
            _logger.LogDebug(string.Format(CultureInfo.InvariantCulture, LogMessages.MethodBegin, nameof(OnTokenResponseReceivedAsync)));
            await _onTokenResponseReceived(context).ConfigureAwait(false);
            _logger.LogDebug(string.Format(CultureInfo.InvariantCulture, LogMessages.MethodEnd, nameof(OnTokenResponseReceivedAsync)));
        }

        private async Task OnTokenValidatedAsync(TokenValidatedContext context)
        {
            _logger.LogDebug(string.Format(CultureInfo.InvariantCulture, LogMessages.MethodBegin, nameof(OnTokenValidatedAsync)));
            await _onTokenValidated(context).ConfigureAwait(false);
            _logger.LogDebug(string.Format(CultureInfo.InvariantCulture, LogMessages.MethodEnd, nameof(OnTokenValidatedAsync)));
        }

        private async Task OnUserInformationReceivedAsync(UserInformationReceivedContext context)
        {
            _logger.LogDebug(string.Format(CultureInfo.InvariantCulture, LogMessages.MethodBegin, nameof(OnUserInformationReceivedAsync)));
            await _onUserInformationReceived(context).ConfigureAwait(false);
            _logger.LogDebug(string.Format(CultureInfo.InvariantCulture, LogMessages.MethodEnd, nameof(OnUserInformationReceivedAsync)));
        }

        private async Task OnAuthenticationFailedAsync(AuthenticationFailedContext context)
        {
            _logger.LogDebug(string.Format(CultureInfo.InvariantCulture, LogMessages.MethodBegin, nameof(OnAuthenticationFailedAsync)));
            await _onAuthenticationFailed(context).ConfigureAwait(false);
            _logger.LogDebug(string.Format(CultureInfo.InvariantCulture, LogMessages.MethodEnd, nameof(OnAuthenticationFailedAsync)));
        }

        private async Task OnRedirectToIdentityProviderForSignOutAsync(RedirectContext context)
        {
            _logger.LogDebug(string.Format(CultureInfo.InvariantCulture, LogMessages.MethodBegin, nameof(OnRedirectToIdentityProviderForSignOutAsync)));
            await _onRedirectToIdentityProviderForSignOut(context).ConfigureAwait(false);
            _logger.LogDebug(string.Format(CultureInfo.InvariantCulture, LogMessages.MethodEnd, nameof(OnRedirectToIdentityProviderForSignOutAsync)));
        }

        private async Task OnRemoteSignOutAsync(RemoteSignOutContext context)
        {
            _logger.LogDebug(string.Format(CultureInfo.InvariantCulture, LogMessages.MethodBegin, nameof(OnRemoteSignOutAsync)));
            await _onRemoteSignOut(context).ConfigureAwait(false);
            _logger.LogDebug(string.Format(CultureInfo.InvariantCulture, LogMessages.MethodEnd, nameof(OnRemoteSignOutAsync)));
        }

        private async Task OnSignedOutCallbackRedirectAsync(RemoteSignOutContext context)
        {
            _logger.LogDebug(string.Format(CultureInfo.InvariantCulture, LogMessages.MethodBegin, nameof(OnSignedOutCallbackRedirectAsync)));
            await _onSignedOutCallbackRedirect(context).ConfigureAwait(false);
            _logger.LogDebug(string.Format(CultureInfo.InvariantCulture, LogMessages.MethodEnd, nameof(OnSignedOutCallbackRedirectAsync)));
        }
    }
}
