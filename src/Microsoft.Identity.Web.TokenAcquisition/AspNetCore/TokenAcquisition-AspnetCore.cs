// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web.TokenCacheProviders;

namespace Microsoft.Identity.Web
{
    internal class TokenAcquisitionAspNetCore : TokenAcquisition, ITokenAcquisitionInternal
    {
        /// <summary>
        /// Constructor of the TokenAcquisition service. This requires the Azure AD Options to
        /// configure the confidential client application and a token cache provider.
        /// This constructor is called by ASP.NET Core dependency injection.
        /// </summary>
        /// <param name="tokenCacheProvider">The App token cache provider.</param>
        /// <param name="tokenAcquisitionHost">Host of the token acquisition service.</param>
        /// <param name="httpClientFactory">HTTP client factory.</param>
        /// <param name="logger">Logger.</param>
        /// <param name="serviceProvider">Service provider.</param>
        /// <param name="credentialsLoader">Credential loader service.</param>
        public TokenAcquisitionAspNetCore(
            IMsalTokenCacheProvider tokenCacheProvider,
            IHttpClientFactory httpClientFactory,
            ILogger<TokenAcquisition> logger,
            ITokenAcquisitionHost tokenAcquisitionHost,
            IServiceProvider serviceProvider,
            ICredentialsLoader credentialsLoader) :
            base(tokenCacheProvider, tokenAcquisitionHost, httpClientFactory, logger, serviceProvider, credentialsLoader)
        {
        }

        /// <summary>
        /// Used in web APIs (no user interaction).
        /// Replies to the client through the HTTP response by sending a 403 (forbidden) and populating the 'WWW-Authenticate' header so that
        /// the client, in turn, can trigger a user interaction so that the user consents to more scopes.
        /// </summary>
        /// <param name="scopes">Scopes to consent to.</param>
        /// <param name="msalServiceException">The <see cref="MsalUiRequiredException"/> that triggered the challenge.</param>
        /// <param name="httpResponse">The <see cref="HttpResponse"/> to update.</param>
        /// if called from a web app, and JwtBearerDefault.AuthenticationScheme if called from a web API.
        public async Task ReplyForbiddenWithWwwAuthenticateHeaderAsync(
            IEnumerable<string> scopes,
            MsalUiRequiredException msalServiceException,
            HttpResponse? httpResponse = null)
        {
            await ReplyForbiddenWithWwwAuthenticateHeaderAsync(scopes, msalServiceException, null, httpResponse);
        }

        /// <summary>
        /// Used in web APIs (no user interaction).
        /// Replies to the client through the HTTP response by sending a 403 (forbidden) and populating the 'WWW-Authenticate' header so that
        /// the client, in turn, can trigger a user interaction so that the user consents to more scopes.
        /// </summary>
        /// <param name="scopes">Scopes to consent to.</param>
        /// <param name="msalServiceException">The <see cref="MsalUiRequiredException"/> that triggered the challenge.</param>
        /// <param name="authenticationScheme">Authentication scheme. If null, will use OpenIdConnectDefault.AuthenticationScheme
        /// if called from a web app, and JwtBearerDefault.AuthenticationScheme if called from a web API.</param>
        /// <param name="httpResponse">The <see cref="HttpResponse"/> to update.</param>
        public void ReplyForbiddenWithWwwAuthenticateHeader(
            IEnumerable<string> scopes,
            MsalUiRequiredException msalServiceException,
            string? authenticationScheme = JwtBearerDefaults.AuthenticationScheme,
            HttpResponse? httpResponse = null)
        {
            ReplyForbiddenWithWwwAuthenticateHeaderAsync(scopes, msalServiceException, authenticationScheme, httpResponse)
                .GetAwaiter()
                .GetResult();
        }
        
        /// <summary>
        /// Used in web APIs (no user interaction).
        /// Replies to the client through the HTTP response by sending a 403 (forbidden) and populating the 'WWW-Authenticate' header so that
        /// the client, in turn, can trigger a user interaction so that the user consents to more scopes.
        /// </summary>
        /// <param name="scopes">Scopes to consent to.</param>
        /// <param name="msalServiceException">The <see cref="MsalUiRequiredException"/> that triggered the challenge.</param>
        /// <param name="authenticationScheme">Authentication scheme. If null, will use OpenIdConnectDefault.AuthenticationScheme
        /// if called from a web app, and JwtBearerDefault.AuthenticationScheme if called from a web API.</param>
        /// <param name="httpResponse">The <see cref="HttpResponse"/> to update.</param>
        /// if called from a web app, and JwtBearerDefault.AuthenticationScheme if called from a web API.
        private async Task ReplyForbiddenWithWwwAuthenticateHeaderAsync(
            IEnumerable<string> scopes,
            MsalUiRequiredException msalServiceException,
            string? authenticationScheme = JwtBearerDefaults.AuthenticationScheme,
            HttpResponse? httpResponse = null)
        {
            // A user interaction is required, but we are in a web API, and therefore,
            // we need to report back to the client through a 'WWW-Authenticate' header https://tools.ietf.org/html/rfc6750#section-3.1
            const string proposedAction = Constants.Consent;
            if (msalServiceException.ErrorCode == MsalError.InvalidGrantError && AcceptedTokenVersionMismatch(msalServiceException))
            {
                throw msalServiceException;
            }

            MergedOptions mergedOptions = _tokenAcquisitionHost.GetOptions(authenticationScheme, out _);

            var application = await GetOrBuildConfidentialClientApplicationAsync(mergedOptions);

            string consentUrl = $"{application.Authority}/oauth2/v2.0/authorize?client_id={mergedOptions.ClientId}"
                + $"&response_type=code&redirect_uri={application.AppConfig.RedirectUri}"
                + $"&response_mode=query&scope=offline_access%20{string.Join("%20", scopes)}";

            Dictionary<string, string> parameters = new()
                {
                    { Constants.ConsentUrl, consentUrl },
                    { Constants.Claims, msalServiceException.Claims },
                    { Constants.Scopes, string.Join(",", scopes) },
                    { Constants.ProposedAction, proposedAction },
                };

            string parameterString = string.Join(", ", parameters.Select(p => $"{p.Key}=\"{p.Value}\""));

            // TODO: change the mechanism to Challenge.
            _tokenAcquisitionHost.SetHttpResponse(HttpStatusCode.Forbidden, new StringValues($"{Constants.Bearer} {parameterString}")!);
        }

        /// <summary>
        /// This handler is executed after the authorization code is received (once the user signs-in and consents) during the
        /// <a href='https://learn.microsoft.com/azure/active-directory/develop/v2-oauth2-auth-code-flow'>authorization code flow</a> in a web app.
        /// It uses the code to request an access token from the Microsoft identity platform and caches the tokens and an entry about the signed-in user's account in the MSAL's token cache.
        /// The access token (and refresh token) provided in the <see cref="AuthorizationCodeReceivedContext"/>, once added to the cache, are then used to acquire more tokens using the
        /// <a href='https://learn.microsoft.com/azure/active-directory/develop/v2-oauth2-on-behalf-of-flow'>on-behalf-of flow</a> for the signed-in user's account,
        /// in order to call to downstream APIs.
        /// </summary>
        /// <param name="context">The context used when an 'AuthorizationCode' is received over the OpenIdConnect protocol.</param>
        /// <param name="scopes">scopes to request access to.</param>
        /// <param name="authenticationScheme">Authentication scheme to use (by default, OpenIdConnectDefaults.AuthenticationScheme).</param>
        /// <example>
        /// From the configuration of the Authentication of the ASP.NET Core web API:
        /// <code>OpenIdConnectOptions options;</code>
        ///
        /// Subscribe to the authorization code received event:
        /// <code>
        ///  options.Events = new OpenIdConnectEvents();
        ///  options.Events.OnAuthorizationCodeReceived = OnAuthorizationCodeReceived;
        /// }
        /// </code>
        ///
        /// And then in the OnAuthorizationCodeRecieved method, call <see cref="AddAccountToCacheFromAuthorizationCodeAsync(AuthorizationCodeReceivedContext, IEnumerable{string}, string)"/>:
        /// <code>
        /// private async Task OnAuthorizationCodeReceived(AuthorizationCodeReceivedContext context)
        /// {
        ///   var tokenAcquisition = context.HttpContext.RequestServices.GetRequiredService&lt;ITokenAcquisition&gt;();
        ///    await _tokenAcquisition.AddAccountToCacheFromAuthorizationCode(context, new string[] { "user.read" });
        /// }
        /// </code>
        /// </example>
#if NET6_0_OR_GREATER
        [RequiresUnreferencedCode("Calls Microsoft.Identity.Web.TokenAcquisition.AddAccountToCacheFromAuthorizationCodeAsync(AuthCodeRedemptionParameters)")]
#endif
        public async Task AddAccountToCacheFromAuthorizationCodeAsync(
            AuthorizationCodeReceivedContext context,
            IEnumerable<string> scopes,
            string authenticationScheme = OpenIdConnectDefaults.AuthenticationScheme /*= OpenIdConnectDefaults.AuthenticationScheme*/)
        {
            CheckParameters(context, scopes);

            string? clientInfo = context!.ProtocolMessage?.GetParameter(ClaimConstants.ClientInfo);
            context.TokenEndpointRequest!.Parameters.TryGetValue(OAuthConstants.CodeVerifierKey, out string? codeVerifier);
            string authCode = context!.ProtocolMessage!.Code;
            string? userFlow = context.Principal?.GetUserFlowId();

            AcquireTokenResult result = await AddAccountToCacheFromAuthorizationCodeAsync(new AuthCodeRedemptionParameters(
                scopes,
                authCode,
                authenticationScheme,
                clientInfo,
                codeVerifier,
                userFlow,
                context!.ProtocolMessage.DomainHint)).ConfigureAwait(false);
            context.HandleCodeRedemption(result.AccessToken!, result.IdToken!);
        }

        private void CheckParameters(
            AuthorizationCodeReceivedContext context,
            IEnumerable<string> scopes)
        {
            _ = Throws.IfNull(context);
            _ = Throws.IfNull(scopes);
        }
    }
}
