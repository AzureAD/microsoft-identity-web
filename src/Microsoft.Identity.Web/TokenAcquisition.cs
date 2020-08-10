// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web.TokenCacheProviders;
using Microsoft.Net.Http.Headers;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Token acquisition service.
    /// </summary>
    internal class TokenAcquisition : ITokenAcquisition, ITokenAcquisitionInternal
    {
        private readonly MicrosoftIdentityOptions _microsoftIdentityOptions;
        private readonly ConfidentialClientApplicationOptions _applicationOptions;

        private readonly IMsalTokenCacheProvider _tokenCacheProvider;

        private IConfidentialClientApplication? _application;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private HttpContext? CurrentHttpContext => _httpContextAccessor.HttpContext;
        private readonly IMsalHttpClientFactory _httpClientFactory;
        private readonly ILogger _logger;
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Constructor of the TokenAcquisition service. This requires the Azure AD Options to
        /// configure the confidential client application and a token cache provider.
        /// This constructor is called by ASP.NET Core dependency injection.
        /// </summary>
        /// <param name="tokenCacheProvider">The App token cache provider.</param>
        /// <param name="httpContextAccessor">Access to the HttpContext of the request.</param>
        /// <param name="microsoftIdentityOptions">Configuration options.</param>
        /// <param name="applicationOptions">MSAL.NET configuration options.</param>
        /// <param name="httpClientFactory">HTTP client factory.</param>
        /// <param name="logger">Logger.</param>
        /// <param name="serviceProvider">Service provider.</param>
        public TokenAcquisition(
            IMsalTokenCacheProvider tokenCacheProvider,
            IHttpContextAccessor httpContextAccessor,
            IOptions<MicrosoftIdentityOptions> microsoftIdentityOptions,
            IOptions<ConfidentialClientApplicationOptions> applicationOptions,
            IHttpClientFactory httpClientFactory,
            ILogger<TokenAcquisition> logger,
            IServiceProvider serviceProvider)
        {
            _httpContextAccessor = httpContextAccessor;
            _microsoftIdentityOptions = microsoftIdentityOptions.Value;
            _applicationOptions = applicationOptions.Value;
            _tokenCacheProvider = tokenCacheProvider;
            _httpClientFactory = new MsalAspNetCoreHttpClientFactory(httpClientFactory);
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Scopes which are already requested by MSAL.NET. They should not be re-requested;.
        /// </summary>
        private readonly string[] _scopesRequestedByMsal = new string[]
        {
            OidcConstants.ScopeOpenId,
            OidcConstants.ScopeProfile,
            OidcConstants.ScopeOfflineAccess,
        };

        /// <summary>
        /// This handler is executed after the authorization code is received (once the user signs-in and consents) during the
        /// <a href='https://docs.microsoft.com/azure/active-directory/develop/v2-oauth2-auth-code-flow'>Authorization code flow grant flow</a> in a web app.
        /// It uses the code to request an access token from the Microsoft Identity platform and caches the tokens and an entry about the signed-in user's account in the MSAL's token cache.
        /// The access token (and refresh token) provided in the <see cref="AuthorizationCodeReceivedContext"/>, once added to the cache, are then used to acquire more tokens using the
        /// <a href='https://docs.microsoft.com/azure/active-directory/develop/v2-oauth2-on-behalf-of-flow'>on-behalf-of flow</a> for the signed-in user's account,
        /// in order to call to downstream APIs.
        /// </summary>
        /// <param name="context">The context used when an 'AuthorizationCode' is received over the OpenIdConnect protocol.</param>
        /// <param name="scopes">scopes to request access to.</param>
        /// <example>
        /// From the configuration of the Authentication of the ASP.NET Core Web API:
        /// <code>OpenIdConnectOptions options;</code>
        ///
        /// Subscribe to the authorization code received event:
        /// <code>
        ///  options.Events = new OpenIdConnectEvents();
        ///  options.Events.OnAuthorizationCodeReceived = OnAuthorizationCodeReceived;
        /// }
        /// </code>
        ///
        /// And then in the OnAuthorizationCodeRecieved method, call <see cref="AddAccountToCacheFromAuthorizationCodeAsync"/>:
        /// <code>
        /// private async Task OnAuthorizationCodeReceived(AuthorizationCodeReceivedContext context)
        /// {
        ///   var tokenAcquisition = context.HttpContext.RequestServices.GetRequiredService&lt;ITokenAcquisition&gt;();
        ///    await _tokenAcquisition.AddAccountToCacheFromAuthorizationCode(context, new string[] { "user.read" });
        /// }
        /// </code>
        /// </example>
        public async Task AddAccountToCacheFromAuthorizationCodeAsync(
            AuthorizationCodeReceivedContext context,
            IEnumerable<string> scopes)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (scopes == null)
            {
                throw new ArgumentNullException(nameof(scopes));
            }

            try
            {
                string? userFlow = context.Principal?.GetUserFlowId();
                _application = await GetOrBuildConfidentialClientApplicationAsync().ConfigureAwait(false);

                // Do not share the access token with ASP.NET Core otherwise ASP.NET will cache it and will not send the OAuth 2.0 request in
                // case a further call to AcquireTokenByAuthorizationCodeAsync in the future is required for incremental consent (getting a code requesting more scopes)
                // Share the ID Token though
                var builder = _application
                    .AcquireTokenByAuthorizationCode(scopes.Except(_scopesRequestedByMsal), context.ProtocolMessage.Code)
                    .WithSendX5C(_microsoftIdentityOptions.SendX5C);

                if (_microsoftIdentityOptions.IsB2C)
                {
                    var authority = $"{_applicationOptions.Instance}{Constants.Tfp}/{_microsoftIdentityOptions.Domain}/{userFlow ?? _microsoftIdentityOptions.DefaultUserFlow}";
                    builder.WithB2CAuthority(authority);
                }

                var result = await builder.ExecuteAsync()
                                          .ConfigureAwait(false);

                context.HandleCodeRedemption(null, result.IdToken);
            }
            catch (MsalException ex)
            {
                _logger.LogInformation(
                    ex,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        LogMessages.ExceptionOccurredWhenAddingAnAccountToTheCacheFromAuthCode));
                throw;
            }
        }

        /// <summary>
        /// Typically used from a Web App or WebAPI controller, this method retrieves an access token
        /// for a downstream API using;
        /// 1) the token cache (for Web Apps and Web APIs) if a token exists in the cache
        /// 2) or the <a href='https://docs.microsoft.com/azure/active-directory/develop/v2-oauth2-on-behalf-of-flow'>on-behalf-of flow</a>
        /// in Web APIs, for the user account that is ascertained from claims are provided in the <see cref="HttpContext.User"/>
        /// instance of the current HttpContext.
        /// </summary>
        /// <param name="scopes">Scopes to request for the downstream API to call.</param>
        /// <param name="tenant">Enables overriding of the tenant/account for the same identity. This is useful in the
        /// cases where a given account is a guest in other tenants, and you want to acquire tokens for a specific tenant, like where the user is a guest in.</param>
        /// <returns>An access token to call the downstream API and populated with this downstream API's scopes.</returns>
        /// <remarks>Calling this method from a Web API supposes that you have previously called,
        /// in a method called by JwtBearerOptions.Events.OnTokenValidated, the HttpContextExtensions.StoreTokenUsedToCallWebAPI method
        /// passing the validated token (as a JwtSecurityToken). Calling it from a Web App supposes that
        /// you have previously called AddAccountToCacheFromAuthorizationCodeAsync from a method called by
        /// OpenIdConnectOptions.Events.OnAuthorizationCodeReceived.</remarks>
        [Obsolete("This method has been deprecated, please use the GetAccessTokenForUserAsync() method instead.")]
        public async Task<string> GetAccessTokenOnBehalfOfUserAsync(
            IEnumerable<string> scopes,
            string? tenant = null)
        {
            return await GetAccessTokenForUserAsync(scopes, tenant).ConfigureAwait(false);
        }

        /// <summary>
        /// Typically used from a Web App or WebAPI controller, this method retrieves an access token
        /// for a downstream API using;
        /// 1) the token cache (for Web Apps and Web APis) if a token exists in the cache
        /// 2) or the <a href='https://docs.microsoft.com/azure/active-directory/develop/v2-oauth2-on-behalf-of-flow'>on-behalf-of flow</a>
        /// in Web APIs, for the user account that is ascertained from claims are provided in the <see cref="HttpContext.User"/>
        /// instance of the current HttpContext.
        /// </summary>
        /// <param name="scopes">Scopes to request for the downstream API to call.</param>
        /// <param name="tenant">Enables overriding of the tenant/account for the same identity. This is useful in the
        /// cases where a given account is guest in other tenants, and you want to acquire tokens for a specific tenant, like where the user is a guest in.</param>
        /// <param name="userFlow">Azure AD B2C user flow to target.</param>
        /// <param name="user">Optional claims principal representing the user. If not provided, will use the signed-in
        /// user (in a web app), or the user for which the token was received (in a Web API)
        /// cases where a given account is guest in other tenants, and you want to acquire tokens for a specific tenant, like where the user is a guest in.</param>
        /// <returns>An access token to call the downstream API and populated with this downstream API's scopes.</returns>
        /// <remarks>Calling this method from a web API supposes that you have previously called,
        /// in a method called by JwtBearerOptions.Events.OnTokenValidated, the HttpContextExtensions.StoreTokenUsedToCallWebAPI method
        /// passing the validated token (as a JwtSecurityToken). Calling it from a Web App supposes that
        /// you have previously called AddAccountToCacheFromAuthorizationCodeAsync from a method called by
        /// OpenIdConnectOptions.Events.OnAuthorizationCodeReceived.</remarks>
        public async Task<string> GetAccessTokenForUserAsync(
            IEnumerable<string> scopes,
            string? tenant = null,
            string? userFlow = null,
            ClaimsPrincipal? user = null)
        {
            if (scopes == null)
            {
                throw new ArgumentNullException(nameof(scopes));
            }

            user = await GetAuthenticatedUserAsync(user).ConfigureAwait(false);

            _application = await GetOrBuildConfidentialClientApplicationAsync().ConfigureAwait(false);
            string authority = CreateAuthorityBasedOnTenantIfProvided(_application, tenant);
            string? accessToken;

            try
            {
                // Access token will return if call is from a web API
                accessToken = await GetTokenForWebApiToCallDownstreamApiAsync(
                    _application,
                    authority,
                    scopes).ConfigureAwait(false);

                if (!string.IsNullOrEmpty(accessToken))
                {
                    return accessToken;
                }

                // If access token is null, this is a web app
                return await GetAccessTokenForWebAppWithAccountFromCacheAsync(
                     _application,
                     user,
                     scopes,
                     authority,
                     userFlow)
                     .ConfigureAwait(false);
            }
            catch (MsalUiRequiredException ex)
            {
                // GetAccessTokenForUserAsync is an abstraction that can be called from a Web App or a Web API
                _logger.LogInformation(ex.Message);

                // Case of the Web App: we let the MsalUiRequiredException be caught by the
                // AuthorizeForScopesAttribute exception filter so that the user can consent, do 2FA, etc ...
                throw new MicrosoftIdentityWebChallengeUserException(ex, scopes.ToArray());
            }
        }

        private async Task<string?> GetTokenForWebApiToCallDownstreamApiAsync(
            IConfidentialClientApplication application,
            string authority,
            IEnumerable<string> scopes)
        {
            try
            {
                // In web API, validatedToken will not be null
                JwtSecurityToken? validatedToken = CurrentHttpContext?.GetTokenUsedToCallWebAPI();

                // Case of web APIs: we need to do an on-behalf-of flow, with the token used to call the API
                if (validatedToken != null)
                {
                    // In the case the token is a JWE (encrypted token), we use the decrypted token.
                    string tokenUsedToCallTheWebApi = validatedToken.InnerToken == null ? validatedToken.RawData
                                                : validatedToken.InnerToken.RawData;
                    var result = await application
                                        .AcquireTokenOnBehalfOf(scopes.Except(_scopesRequestedByMsal), new UserAssertion(tokenUsedToCallTheWebApi))
                                        .WithSendX5C(_microsoftIdentityOptions.SendX5C)
                                        .WithAuthority(authority)
                                        .ExecuteAsync()
                                        .ConfigureAwait(false);
                    return result.AccessToken;
                }

                return null;
            }
            catch (MsalUiRequiredException ex)
            {
                _logger.LogInformation(string.Format(CultureInfo.InvariantCulture, LogMessages.ErrorAcquiringTokenForDownstreamWebApi, ex.Message));
                throw ex;
            }
        }

        /// <summary>
        /// Acquires a token from the authority configured in the app, for the confidential client itself (not on behalf of a user)
        /// using the client credentials flow. See https://aka.ms/msal-net-client-credentials.
        /// </summary>
        /// <param name="scopes">scopes requested to access a protected API. For this flow (client credentials), the scopes
        /// should be of the form "{ResourceIdUri/.default}" for instance <c>https://management.azure.net/.default</c> or, for Microsoft
        /// Graph, <c>https://graph.microsoft.com/.default</c> as the requested scopes are defined statically with the application registration
        /// in the portal, and cannot be overridden in the application.</param>
        /// <returns>An access token for the app itself, based on its scopes.</returns>
        public async Task<string> GetAccessTokenForAppAsync(IEnumerable<string> scopes)
        {
            if (scopes == null)
            {
                throw new ArgumentNullException(nameof(scopes));
            }

            // Use MSAL to get the right token to call the API
            _application = await GetOrBuildConfidentialClientApplicationAsync().ConfigureAwait(false);

            AuthenticationResult result;
            result = await _application
                   .AcquireTokenForClient(scopes.Except(_scopesRequestedByMsal))
                   .WithSendX5C(_microsoftIdentityOptions.SendX5C)
                   .ExecuteAsync()
                   .ConfigureAwait(false);

            return result.AccessToken;
        }

        /// <summary>
        /// Removes the account associated with context.HttpContext.User from the MSAL.NET cache.
        /// </summary>
        /// <param name="context">RedirectContext passed-in to a <see cref="OpenIdConnectEvents.OnRedirectToIdentityProviderForSignOut"/>
        /// OpenID Connect event.</param>
        /// <returns>A <see cref="Task"/> that represents a completed account removal operation.</returns>
        public async Task RemoveAccountAsync(RedirectContext context)
        {
            ClaimsPrincipal user = context.HttpContext.User;
            string? userId = user.GetMsalAccountId();
            if (!string.IsNullOrEmpty(userId))
            {
                IConfidentialClientApplication app = await GetOrBuildConfidentialClientApplicationAsync().ConfigureAwait(false);

                if (_microsoftIdentityOptions.IsB2C)
                {
                    await _tokenCacheProvider.ClearAsync(userId).ConfigureAwait(false);
                }
                else
                {
                    string? identifier = context.HttpContext.User.GetMsalAccountId();
                    IAccount account = await app.GetAccountAsync(identifier).ConfigureAwait(false);

                    if (account != null)
                    {
                        await app.RemoveAsync(account).ConfigureAwait(false);
                        await _tokenCacheProvider.ClearAsync(userId).ConfigureAwait(false);
                    }
                }
            }
        }

        /// <summary>
        /// Creates an MSAL confidential client application if needed.
        /// </summary>
        private async Task<IConfidentialClientApplication> GetOrBuildConfidentialClientApplicationAsync()
        {
            if (_application == null)
            {
                _application = await BuildConfidentialClientApplicationAsync().ConfigureAwait(false);
            }

            return _application;
        }

        /// <summary>
        /// Creates an MSAL confidential client application.
        /// </summary>
        private async Task<IConfidentialClientApplication> BuildConfidentialClientApplicationAsync()
        {
            var request = CurrentHttpContext?.Request;
            string? currentUri = null;
            if (request != null)
            {
                currentUri = UriHelper.BuildAbsolute(
                    request.Scheme,
                    request.Host,
                    request.PathBase,
                    _microsoftIdentityOptions.CallbackPath.Value ?? string.Empty);
            }

            if (!_applicationOptions.Instance.EndsWith("/", StringComparison.InvariantCulture))
            {
                _applicationOptions.Instance += "/";
            }

            MicrosoftIdentityOptionsValidation.ValidateEitherClientCertificateOrClientSecret(
                _applicationOptions.ClientSecret,
                _microsoftIdentityOptions.ClientCertificates);

            try
            {
                var builder = ConfidentialClientApplicationBuilder
                        .CreateWithApplicationOptions(_applicationOptions)
                        .WithHttpClientFactory(_httpClientFactory);

                // The redirect URI is not needed for OBO
                if (!string.IsNullOrEmpty(currentUri))
                {
                    builder.WithRedirectUri(currentUri);
                }

                string authority;

                if (_microsoftIdentityOptions.IsB2C)
                {
                    authority = $"{_applicationOptions.Instance}{Constants.Tfp}/{_microsoftIdentityOptions.Domain}/{_microsoftIdentityOptions.DefaultUserFlow}";
                    builder.WithB2CAuthority(authority);
                }
                else
                {
                    authority = $"{_applicationOptions.Instance}{_applicationOptions.TenantId}/";
                    builder.WithAuthority(authority);
                }

                if (_microsoftIdentityOptions.ClientCertificates != null)
                {
                    X509Certificate2? certificate = DefaultCertificateLoader.LoadFirstCertificate(_microsoftIdentityOptions.ClientCertificates);
                    builder.WithCertificate(certificate);
                }

                IConfidentialClientApplication app = builder.Build();
                // Initialize token cache providers
                await _tokenCacheProvider.InitializeAsync(app.AppTokenCache).ConfigureAwait(false);
                await _tokenCacheProvider.InitializeAsync(app.UserTokenCache).ConfigureAwait(false);
                return app;
            }
            catch (Exception ex)
            {
                _logger.LogInformation(
                    ex,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        IDWebErrorMessage.ExceptionAcquiringTokenForConfidentialClient));
                throw;
            }
        }

        /// <summary>
        /// Gets an access token for a downstream API on behalf of the user described by its claimsPrincipal.
        /// </summary>
        /// <param name="application"><see cref="IConfidentialClientApplication"/>.</param>
        /// <param name="claimsPrincipal">Claims principal for the user on behalf of whom to get a token.</param>
        /// <param name="scopes">Scopes for the downstream API to call.</param>
        /// <param name="authority">(optional) Authority based on a specific tenant for which to acquire a token to access the scopes
        /// on behalf of the user described in the claimsPrincipal.</param>
        /// <param name="userFlow">Azure AD B2C user flow to target.</param>
        private async Task<string> GetAccessTokenForWebAppWithAccountFromCacheAsync(
            IConfidentialClientApplication application,
            ClaimsPrincipal? claimsPrincipal,
            IEnumerable<string> scopes,
            string? authority,
            string? userFlow = null)
        {
            IAccount? account = null;
            if (_microsoftIdentityOptions.IsB2C && !string.IsNullOrEmpty(userFlow))
            {
                string? nameIdentifierId = claimsPrincipal?.GetNameIdentifierId();
                string? utid = claimsPrincipal?.GetHomeTenantId();
                string? b2cAccountIdentifier = string.Format(CultureInfo.InvariantCulture, "{0}-{1}.{2}", nameIdentifierId, userFlow, utid);
                account = await application.GetAccountAsync(b2cAccountIdentifier).ConfigureAwait(false);
            }
            else
            {
                string? accountIdentifier = claimsPrincipal?.GetMsalAccountId();

                if (accountIdentifier != null)
                {
                    account = await application.GetAccountAsync(accountIdentifier).ConfigureAwait(false);
                }
            }

            return await GetAccessTokenForWebAppWithAccountFromCacheAsync(
                application,
                account,
                scopes,
                authority).ConfigureAwait(false);
        }

        /// <summary>
        /// Gets an access token for a downstream API on behalf of the user which account is passed as an argument.
        /// </summary>
        /// <param name="application"><see cref="IConfidentialClientApplication"/>.</param>
        /// <param name="account">User IAccount for which to acquire a token.
        /// See <see cref="Microsoft.Identity.Client.AccountId.Identifier"/>.</param>
        /// <param name="scopes">Scopes for the downstream API to call.</param>
        /// <param name="authority">Authority based on a specific tenant for which to acquire a token to access the scopes
        /// on behalf of the user.</param>
        /// <param name="userFlow">Azure AD B2C user flow.</param>
        private async Task<string> GetAccessTokenForWebAppWithAccountFromCacheAsync(
            IConfidentialClientApplication application,
            IAccount? account,
            IEnumerable<string> scopes,
            string? authority,
            string? userFlow = null)
        {
            if (scopes == null)
            {
                throw new ArgumentNullException(nameof(scopes));
            }

            AuthenticationResult result;
            // Acquire an access token as a B2C authority
            if (_microsoftIdentityOptions.IsB2C)
            {
                string b2cAuthority = application.Authority.Replace(
                    new Uri(application.Authority).PathAndQuery,
                    $"/{Constants.Tfp}/{_microsoftIdentityOptions.Domain}/{userFlow ?? _microsoftIdentityOptions.DefaultUserFlow}");

                result = await application
                    .AcquireTokenSilent(scopes.Except(_scopesRequestedByMsal), account)
                    .WithB2CAuthority(b2cAuthority)
                    .WithSendX5C(_microsoftIdentityOptions.SendX5C)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                return result.AccessToken;
            }

            result = await application
                .AcquireTokenSilent(scopes.Except(_scopesRequestedByMsal), account)
                .WithAuthority(authority)
                .WithSendX5C(_microsoftIdentityOptions.SendX5C)
                .ExecuteAsync()
                .ConfigureAwait(false);
            return result.AccessToken;
        }

        /// <summary>
        /// Used in web APIs (which therefore cannot have an interaction with the user).
        /// Replies to the client through the HTTP response by sending a 403 (forbidden) and populating 'WWW-Authenticate' header so that
        /// the client can trigger an interaction with the user so that the user consents to more scopes.
        /// </summary>
        /// <param name="scopes">Scopes to consent to.</param>
        /// <param name="msalServiceException">The <see cref="MsalUiRequiredException"/> that triggered the challenge.</param>
        /// <param name="httpResponse">The <see cref="HttpResponse"/> to update.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task ReplyForbiddenWithWwwAuthenticateHeaderAsync(IEnumerable<string> scopes, MsalUiRequiredException msalServiceException, HttpResponse? httpResponse = null)
        {
            // A user interaction is required, but we are in a web API, and therefore, we need to report back to the client through a 'WWW-Authenticate' header https://tools.ietf.org/html/rfc6750#section-3.1
            string proposedAction = Constants.Consent;
            if (msalServiceException.ErrorCode == MsalError.InvalidGrantError)
            {
                if (AcceptedTokenVersionMismatch(msalServiceException))
                {
                    throw msalServiceException;
                }
            }

            _application = await GetOrBuildConfidentialClientApplicationAsync().ConfigureAwait(false);

            string consentUrl = $"{_application.Authority}/oauth2/v2.0/authorize?client_id={_applicationOptions.ClientId}"
                + $"&response_type=code&redirect_uri={_application.AppConfig.RedirectUri}"
                + $"&response_mode=query&scope=offline_access%20{string.Join("%20", scopes)}";

            IDictionary<string, string> parameters = new Dictionary<string, string>()
                {
                    { Constants.ConsentUrl, consentUrl },
                    { Constants.Claims, msalServiceException.Claims },
                    { Constants.Scopes, string.Join(",", scopes) },
                    { Constants.ProposedAction, proposedAction },
                };

            string parameterString = string.Join(", ", parameters.Select(p => $"{p.Key}=\"{p.Value}\""));

            httpResponse ??= CurrentHttpContext?.Response;

            if (httpResponse == null)
            {
                throw new InvalidOperationException(IDWebErrorMessage.HttpContextAndHttpResponseAreNull);
            }

            var headers = httpResponse.Headers;
            httpResponse.StatusCode = (int)HttpStatusCode.Forbidden;

            headers[HeaderNames.WWWAuthenticate] = new StringValues($"{Constants.Bearer} {parameterString}");
        }

        private static bool AcceptedTokenVersionMismatch(MsalUiRequiredException msalServiceException)
        {
            // Normally app developers should not make decisions based on the internal AAD code
            // however until the STS sends sub-error codes for this error, this is the only
            // way to distinguish the case.
            // This is subject to change in the future
            return msalServiceException.Message.Contains(ErrorCodes.B2CPasswordResetErrorCode, StringComparison.InvariantCulture);
        }

        private async Task<ClaimsPrincipal?> GetAuthenticatedUserAsync(ClaimsPrincipal? user)
        {
            if (user == null && _httpContextAccessor.HttpContext?.User != null)
            {
                user = _httpContextAccessor.HttpContext.User;
            }

            if (user == null)
            {
                try
                {
                    AuthenticationStateProvider? authenticationStateProvider =
                        _serviceProvider.GetService(typeof(AuthenticationStateProvider))
                        as AuthenticationStateProvider;

                    if (authenticationStateProvider != null)
                    {
                        // AuthenticationState provider is only available in Blazor
                        AuthenticationState state = await authenticationStateProvider.GetAuthenticationStateAsync().ConfigureAwait(false);
                        user = state.User;
                    }
                }
                catch
                {
                }
            }

            return user;
        }

        internal /*for tests*/ string CreateAuthorityBasedOnTenantIfProvided(
            IConfidentialClientApplication application,
            string? tenant)
        {
            string authority;
            if (!string.IsNullOrEmpty(tenant))
            {
                authority = application.Authority.Replace(new Uri(application.Authority).PathAndQuery, $"/{tenant}/");
            }
            else
            {
                authority = application.Authority;
            }

            return authority;
        }
    }
}
