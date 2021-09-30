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
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.DependencyInjection;
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
    internal partial class TokenAcquisition : ITokenAcquisitionInternal
    {
        private readonly IOptionsMonitor<MergedOptions> _mergedOptionsMonitor;
        private readonly IMsalTokenCacheProvider _tokenCacheProvider;

        private readonly object _applicationSyncObj = new object();

        /// <summary>
        ///  Please call GetOrBuildConfidentialClientApplication instead of accessing this field directly.
        /// </summary>
        private IConfidentialClientApplication? _application;
        private bool _retryClientCertificate;
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
        /// <param name="mergedOptionsMonitor">Configuration options.</param>
        /// <param name="httpClientFactory">HTTP client factory.</param>
        /// <param name="logger">Logger.</param>
        /// <param name="serviceProvider">Service provider.</param>
        public TokenAcquisition(
            IMsalTokenCacheProvider tokenCacheProvider,
            IHttpContextAccessor httpContextAccessor,
            IOptionsMonitor<MergedOptions> mergedOptionsMonitor,
            IHttpClientFactory httpClientFactory,
            ILogger<TokenAcquisition> logger,
            IServiceProvider serviceProvider)
        {
            _httpContextAccessor = httpContextAccessor;
            _mergedOptionsMonitor = mergedOptionsMonitor;
            _tokenCacheProvider = tokenCacheProvider;
            _httpClientFactory = new MsalAspNetCoreHttpClientFactory(httpClientFactory);
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        internal MergedOptions GetOptions(string authenticationScheme)
        {
            return _mergedOptionsMonitor.Get(authenticationScheme);
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
        /// Meta-tenant identifiers which are not allowed in client credentials.
        /// </summary>
        private readonly ISet<string> _metaTenantIdentifiers = new HashSet<string>(
            new[]
            {
                Constants.Common,
                Constants.Organizations,
            },
            StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// This handler is executed after the authorization code is received (once the user signs-in and consents) during the
        /// <a href='https://docs.microsoft.com/azure/active-directory/develop/v2-oauth2-auth-code-flow'>authorization code flow</a> in a web app.
        /// It uses the code to request an access token from the Microsoft identity platform and caches the tokens and an entry about the signed-in user's account in the MSAL's token cache.
        /// The access token (and refresh token) provided in the <see cref="AuthorizationCodeReceivedContext"/>, once added to the cache, are then used to acquire more tokens using the
        /// <a href='https://docs.microsoft.com/azure/active-directory/develop/v2-oauth2-on-behalf-of-flow'>on-behalf-of flow</a> for the signed-in user's account,
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
            IEnumerable<string> scopes,
            string authenticationScheme /*= OpenIdConnectDefaults.AuthenticationScheme*/)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (scopes == null)
            {
                throw new ArgumentNullException(nameof(scopes));
            }

            authenticationScheme = GetEffectiveAuthenticationScheme(authenticationScheme);
            MergedOptions mergedOptions = GetOptions(authenticationScheme);

            try
            {
                var application = GetOrBuildConfidentialClientApplication(mergedOptions);

                context.TokenEndpointRequest.Parameters.TryGetValue(OAuthConstants.CodeVerifierKey, out string? codeVerifier);
                // Do not share the access token with ASP.NET Core otherwise ASP.NET will cache it and will not send the OAuth 2.0 request in
                // case a further call to AcquireTokenByAuthorizationCodeAsync in the future is required for incremental consent (getting a code requesting more scopes)
                // Share the ID token though

                string? clientInfo = context!.ProtocolMessage?.GetParameter(ClaimConstants.ClientInfo);
                string? backUpAuthRoutingHint = string.Empty;
                if (!string.IsNullOrEmpty(clientInfo))
                {
                    ClientInfo? clientInfoFromAuthorize = ClientInfo.CreateFromJson(clientInfo);
                    if (clientInfoFromAuthorize != null && clientInfoFromAuthorize.UniqueTenantIdentifier != null && clientInfoFromAuthorize.UniqueObjectIdentifier != null)
                    {
                        backUpAuthRoutingHint = $"oid:{clientInfoFromAuthorize.UniqueObjectIdentifier}@{clientInfoFromAuthorize.UniqueTenantIdentifier}";
                    }
                }

                var builder = application
                    .AcquireTokenByAuthorizationCode(scopes.Except(_scopesRequestedByMsal), context!.ProtocolMessage!.Code)
                    .WithSendX5C(mergedOptions.SendX5C)
                    .WithPkceCodeVerifier(codeVerifier)
                    .WithCcsRoutingHint(backUpAuthRoutingHint);

                if (mergedOptions.IsB2C)
                {
                    string? userFlow = context.Principal?.GetUserFlowId();
                    var authority = $"{mergedOptions.Instance}{ClaimConstants.Tfp}/{mergedOptions.Domain}/{userFlow ?? mergedOptions.DefaultUserFlow}";
                    builder.WithB2CAuthority(authority);
                }

                var result = await builder.ExecuteAsync()
                                          .ConfigureAwait(false);

                context.HandleCodeRedemption(null, result.IdToken);
            }
            catch (MsalServiceException exMsal) when (IsInvalidClientCertificateError(exMsal))
            {
                DefaultCertificateLoader.ResetCertificates(mergedOptions.ClientCertificates);
                _application = null;

                // Retry
                _retryClientCertificate = true;
                await AddAccountToCacheFromAuthorizationCodeAsync(context, scopes, authenticationScheme).ConfigureAwait(false);
            }
            catch (MsalException ex)
            {
                Logger.TokenAcquisitionError(_logger, LogMessages.ExceptionOccurredWhenAddingAnAccountToTheCacheFromAuthCode, ex);
                throw;
            }
            finally
            {
                _retryClientCertificate = false;
            }
        }

        /// <summary>
        /// Typically used from a web app or web API controller, this method retrieves an access token
        /// for a downstream API using;
        /// 1) the token cache (for web apps and web APIs) if a token exists in the cache
        /// 2) or the <a href='https://docs.microsoft.com/azure/active-directory/develop/v2-oauth2-on-behalf-of-flow'>on-behalf-of flow</a>
        /// in web APIs, for the user account that is ascertained from claims provided in the <see cref="HttpContext.User"/>
        /// instance of the current HttpContext.
        /// </summary>
        /// <param name="scopes">Scopes to request for the downstream API to call.</param>
        /// <param name="authenticationScheme">Authentication scheme. If null, will use OpenIdConnectDefault.AuthenticationScheme
        /// if called from a web app, and JwtBearerDefault.AuthenticationScheme if called from a web APIs.</param>
        /// <param name="tenantId">Enables overriding of the tenant/account for the same identity. This is useful in the
        /// cases where a given account is a guest in other tenants, and you want to acquire tokens for a specific tenant, like where the user is a guest.</param>
        /// <param name="userFlow">Azure AD B2C user flow to target.</param>
        /// <param name="user">Optional claims principal representing the user. If not provided, will use the signed-in
        /// user (in a web app), or the user for which the token was received (in a web API)
        /// cases where a given account is a guest in other tenants, and you want to acquire tokens for a specific tenant, like where the user is a guest.</param>
        /// <param name="tokenAcquisitionOptions">Options passed-in to create the token acquisition options object which calls into MSAL .NET.</param>
        /// <returns>An access token to call the downstream API and populated with this downstream API's scopes.</returns>
        /// <remarks>Calling this method from a web API supposes that you have previously called,
        /// in a method called by JwtBearerOptions.Events.OnTokenValidated, the HttpContextExtensions.StoreTokenUsedToCallWebAPI method
        /// passing the validated token (as a JwtSecurityToken). Calling it from a web app supposes that
        /// you have previously called AddAccountToCacheFromAuthorizationCodeAsync from a method called by
        /// OpenIdConnectOptions.Events.OnAuthorizationCodeReceived.</remarks>
        public async Task<AuthenticationResult> GetAuthenticationResultForUserAsync(
            IEnumerable<string> scopes,
            string? authenticationScheme = null,
            string? tenantId = null,
            string? userFlow = null,
            ClaimsPrincipal? user = null,
            TokenAcquisitionOptions? tokenAcquisitionOptions = null)
        {
            if (scopes == null)
            {
                throw new ArgumentNullException(nameof(scopes));
            }

            authenticationScheme = GetEffectiveAuthenticationScheme(authenticationScheme);
            MergedOptions mergedOptions = GetOptions(authenticationScheme);

            user = await GetAuthenticatedUserAsync(user).ConfigureAwait(false);

            var application = GetOrBuildConfidentialClientApplication(mergedOptions);

            string authority = CreateAuthorityBasedOnTenantIfProvided(application, tenantId);

            try
            {
                AuthenticationResult? authenticationResult;
                // Access token will return if call is from a web API
                authenticationResult = await GetAuthenticationResultForWebApiToCallDownstreamApiAsync(
                    application,
                    authority,
                    scopes,
                    tokenAcquisitionOptions,
                    mergedOptions).ConfigureAwait(false);

                if (authenticationResult != null)
                {
                    LogAuthResult(authenticationResult);
                    return authenticationResult;
                }

                // If access token is null, this is a web app
                authenticationResult = await GetAuthenticationResultForWebAppWithAccountFromCacheAsync(
                     application,
                     user,
                     scopes,
                     authority,
                     mergedOptions,
                     userFlow,
                     null)
                     .ConfigureAwait(false);
                LogAuthResult(authenticationResult);
                return authenticationResult;
            }
            catch (MsalServiceException exMsal) when (IsInvalidClientCertificateError(exMsal))
            {
                DefaultCertificateLoader.ResetCertificates(mergedOptions.ClientCertificates);
                _application = null;

                // Retry
                _retryClientCertificate = true;
                return await GetAuthenticationResultForUserAsync(scopes, tenantId: tenantId, userFlow: userFlow, user: user, tokenAcquisitionOptions: tokenAcquisitionOptions).ConfigureAwait(false);
            }
            catch (MsalUiRequiredException ex)
            {
                // GetAccessTokenForUserAsync is an abstraction that can be called from a web app or a web API
                Logger.TokenAcquisitionError(_logger, ex.Message, ex);

                // Case of the web app: we let the MsalUiRequiredException be caught by the
                // AuthorizeForScopesAttribute exception filter so that the user can consent, do 2FA, etc ...
                throw new MicrosoftIdentityWebChallengeUserException(ex, scopes.ToArray(), userFlow);
            }
            finally
            {
                _retryClientCertificate = false;
            }
        }

        private void LogAuthResult(AuthenticationResult? authenticationResult)
        {
            if (authenticationResult != null)
            {
                Logger.TokenAcquisitionMsalAuthenticationResultTime(
                _logger,
                authenticationResult.AuthenticationResultMetadata.DurationTotalInMs,
                authenticationResult.AuthenticationResultMetadata.DurationInHttpInMs,
                authenticationResult.AuthenticationResultMetadata.DurationInCacheInMs,
                authenticationResult.AuthenticationResultMetadata.TokenSource.ToString(),
                authenticationResult.CorrelationId.ToString(),
                null);
            }
        }

        /// <summary>
        /// Acquires an authentication result from the authority configured in the app, for the confidential client itself (not on behalf of a user)
        /// using the client credentials flow. See https://aka.ms/msal-net-client-credentials.
        /// </summary>
        /// <param name="scope">The scope requested to access a protected API. For this flow (client credentials), the scope
        /// should be of the form "{ResourceIdUri/.default}" for instance <c>https://management.azure.net/.default</c> or, for Microsoft
        /// Graph, <c>https://graph.microsoft.com/.default</c> as the requested scopes are defined statically with the application registration
        /// in the portal, and cannot be overridden in the application, as you can request a token for only one resource at a time (use
        /// several calls to get tokens for other resources).</param>
        /// <param name="authenticationScheme">AuthenticationScheme to use.</param>
        /// <param name="tenant">Enables overriding of the tenant/account for the same identity. This is useful
        /// for multi tenant apps or daemons.</param>
        /// <param name="tokenAcquisitionOptions">Options passed-in to create the token acquisition object which calls into MSAL .NET.</param>
        /// <returns>An authentication result for the app itself, based on its scopes.</returns>
        public Task<AuthenticationResult> GetAuthenticationResultForAppAsync(
            string scope,
            string? authenticationScheme = null,
            string? tenant = null,
            TokenAcquisitionOptions? tokenAcquisitionOptions = null)
        {
            if (string.IsNullOrEmpty(scope))
            {
                throw new ArgumentNullException(nameof(scope));
            }

            if (!scope.EndsWith("/.default", true, CultureInfo.InvariantCulture))
            {
                throw new ArgumentException(IDWebErrorMessage.ClientCredentialScopeParameterShouldEndInDotDefault, nameof(scope));
            }

            authenticationScheme = GetEffectiveAuthenticationScheme(authenticationScheme);
            MergedOptions mergedOptions = GetOptions(authenticationScheme);

            // Case of an anonymous controller, no [Authorize] attribute will trigger the merge options
            if (string.IsNullOrEmpty(mergedOptions.Instance))
            {
                var mergedOptionsMonitor = _serviceProvider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>();
                mergedOptionsMonitor.Get(JwtBearerDefaults.AuthenticationScheme);
            }

            if (string.IsNullOrEmpty(tenant))
            {
                tenant = mergedOptions.TenantId;
            }

            if (!string.IsNullOrEmpty(tenant) && _metaTenantIdentifiers.Contains(tenant))
            {
                throw new ArgumentException(IDWebErrorMessage.ClientCredentialTenantShouldBeTenanted, nameof(tenant));
            }

            // Use MSAL to get the right token to call the API
            var application = GetOrBuildConfidentialClientApplication(mergedOptions);
            string authority = CreateAuthorityBasedOnTenantIfProvided(application, tenant);

            var builder = application
                   .AcquireTokenForClient(new string[] { scope }.Except(_scopesRequestedByMsal))
                   .WithSendX5C(mergedOptions.SendX5C)
                   .WithAuthority(authority);

            if (tokenAcquisitionOptions != null)
            {
                builder.WithExtraQueryParameters(tokenAcquisitionOptions.ExtraQueryParameters);
                builder.WithCorrelationId(tokenAcquisitionOptions.CorrelationId);
                builder.WithForceRefresh(tokenAcquisitionOptions.ForceRefresh);
                builder.WithClaims(tokenAcquisitionOptions.Claims);
                if (tokenAcquisitionOptions.PoPConfiguration != null)
                {
                    builder.WithProofOfPossession(tokenAcquisitionOptions.PoPConfiguration);
                }
            }

            try
            {
                return builder.ExecuteAsync(tokenAcquisitionOptions != null ? tokenAcquisitionOptions.CancellationToken : CancellationToken.None);
            }
            catch (MsalServiceException exMsal) when (IsInvalidClientCertificateError(exMsal))
            {
                DefaultCertificateLoader.ResetCertificates(mergedOptions.ClientCertificates);
                _application = null;

                // Retry
                _retryClientCertificate = true;
                return GetAuthenticationResultForAppAsync(scope, tenant: tenant, tokenAcquisitionOptions: tokenAcquisitionOptions);
            }
            finally
            {
                _retryClientCertificate = false;
            }
        }

        /// <summary>
        /// Acquires a token from the authority configured in the app, for the confidential client itself (not on behalf of a user)
        /// using the client credentials flow. See https://aka.ms/msal-net-client-credentials.
        /// </summary>
        /// <param name="scope">The scope requested to access a protected API. For this flow (client credentials), the scope
        /// should be of the form "{ResourceIdUri/.default}" for instance <c>https://management.azure.net/.default</c> or, for Microsoft
        /// Graph, <c>https://graph.microsoft.com/.default</c> as the requested scopes are defined statically with the application registration
        /// in the portal, and cannot be overridden in the application, as you can request a token for only one resource at a time (use
        /// several calls to get tokens for other resources).</param>
        /// <param name="authenticationScheme">AuthenticationScheme to use.</param>
        /// <param name="tenant">Enables overriding of the tenant/account for the same identity. This is useful
        /// for multi tenant apps or daemons.</param>
        /// <param name="tokenAcquisitionOptions">Options passed-in to create the token acquisition object which calls into MSAL .NET.</param>
        /// <returns>An access token for the app itself, based on its scopes.</returns>
        public async Task<string> GetAccessTokenForAppAsync(
            string scope,
            string? authenticationScheme = null,
            string? tenant = null,
            TokenAcquisitionOptions? tokenAcquisitionOptions = null)
        {
            AuthenticationResult authResult = await GetAuthenticationResultForAppAsync(
                scope,
                authenticationScheme,
                tenant,
                tokenAcquisitionOptions).ConfigureAwait(false);
            return authResult.AccessToken;
        }

        /// <summary>
        /// Typically used from a web app or web API controller, this method retrieves an access token
        /// for a downstream API using;
        /// 1) the token cache (for web apps and web APIs) if a token exists in the cache
        /// 2) or the <a href='https://docs.microsoft.com/azure/active-directory/develop/v2-oauth2-on-behalf-of-flow'>on-behalf-of flow</a>
        /// in web APIs, for the user account that is ascertained from the claims provided in the <see cref="HttpContext.User"/>
        /// instance of the current HttpContext.
        /// </summary>
        /// <param name="scopes">Scopes to request for the downstream API to call.</param>
        /// <param name="authenticationScheme">Authentication scheme. If null, will use OpenIdConnectDefault.AuthenticationScheme
        /// if called from a web app, and JwtBearerDefault.AuthenticationScheme if called from a web API.</param>
        /// <param name="tenantId">Enables overriding of the tenant/account for the same identity. This is useful in the
        /// cases where a given account is a guest in other tenants, and you want to acquire tokens for a specific tenant.</param>
        /// <param name="userFlow">Azure AD B2C user flow to target.</param>
        /// <param name="user">Optional claims principal representing the user. If not provided, will use the signed-in
        /// user (in a web app), or the user for which the token was received (in a web API)
        /// cases where a given account is a guest in other tenants, and you want to acquire tokens for a specific tenant.</param>
        /// <param name="tokenAcquisitionOptions">Options passed-in to create the token acquisition object which calls into MSAL .NET.</param>
        /// <returns>An access token to call the downstream API and populated with this downstream API's scopes.</returns>
        /// <remarks>Calling this method from a web API supposes that you have previously called,
        /// in a method called by JwtBearerOptions.Events.OnTokenValidated, the HttpContextExtensions.StoreTokenUsedToCallWebAPI method
        /// passing the validated token (as a JwtSecurityToken). Calling it from a web app supposes that
        /// you have previously called AddAccountToCacheFromAuthorizationCodeAsync from a method called by
        /// OpenIdConnectOptions.Events.OnAuthorizationCodeReceived.</remarks>
        public async Task<string> GetAccessTokenForUserAsync(
        IEnumerable<string> scopes,
        string? authenticationScheme = null,
        string? tenantId = null,
        string? userFlow = null,
        ClaimsPrincipal? user = null,
        TokenAcquisitionOptions? tokenAcquisitionOptions = null)
        {
            AuthenticationResult result =
                await GetAuthenticationResultForUserAsync(
                scopes,
                authenticationScheme,
                tenantId,
                userFlow,
                user,
                tokenAcquisitionOptions).ConfigureAwait(false);
            return result.AccessToken;
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
        public Task ReplyForbiddenWithWwwAuthenticateHeaderAsync(
            IEnumerable<string> scopes,
            MsalUiRequiredException msalServiceException,
            HttpResponse? httpResponse = null)
        {
            ReplyForbiddenWithWwwAuthenticateHeader(scopes, msalServiceException, null, httpResponse);
            return Task.CompletedTask;
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
            // A user interaction is required, but we are in a web API, and therefore, we need to report back to the client through a 'WWW-Authenticate' header https://tools.ietf.org/html/rfc6750#section-3.1
            string proposedAction = Constants.Consent;
            if (msalServiceException.ErrorCode == MsalError.InvalidGrantError && AcceptedTokenVersionMismatch(msalServiceException))
            {
                throw msalServiceException;
            }

            authenticationScheme = GetEffectiveAuthenticationScheme(authenticationScheme);
            MergedOptions mergedOptions = GetOptions(authenticationScheme);

            var application = GetOrBuildConfidentialClientApplication(mergedOptions);

            string consentUrl = $"{application.Authority}/oauth2/v2.0/authorize?client_id={mergedOptions.ClientId}"
                + $"&response_type=code&redirect_uri={application.AppConfig.RedirectUri}"
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

        /// <summary>
        /// Removes the account associated with context.HttpContext.User from the MSAL.NET cache.
        /// </summary>
        /// <param name="context">RedirectContext passed-in to a <see cref="OpenIdConnectEvents.OnRedirectToIdentityProviderForSignOut"/>
        /// OpenID Connect event.</param>
        /// <param name="authenticationScheme">Authentication scheme. If null, will use OpenIdConnectDefault.AuthenticationScheme
        /// if called from a web app, and JwtBearerDefault.AuthenticationScheme if called from a web API.</param>
        /// <returns>A <see cref="Task"/> that represents a completed account removal operation.</returns>
        public async Task RemoveAccountAsync(
            RedirectContext context,
            string? authenticationScheme)
        {
            ClaimsPrincipal user = context.HttpContext.User;
            string? userId = user.GetMsalAccountId();
            if (!string.IsNullOrEmpty(userId))
            {
                authenticationScheme = GetEffectiveAuthenticationScheme(authenticationScheme);
                MergedOptions mergedOptions = GetOptions(authenticationScheme);

                IConfidentialClientApplication app = GetOrBuildConfidentialClientApplication(mergedOptions);

                if (mergedOptions.IsB2C)
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

        /// <inheritdoc/>
        public string GetEffectiveAuthenticationScheme(string? authenticationScheme)
        {
            if (authenticationScheme != null)
            {
                return authenticationScheme;
            }
            else
            {
                return _serviceProvider.GetService<IAuthenticationSchemeProvider>()?.GetDefaultAuthenticateSchemeAsync()?.Result?.Name ??
                    ((CurrentHttpContext?.GetTokenUsedToCallWebAPI() != null)
                    ? JwtBearerDefaults.AuthenticationScheme : OpenIdConnectDefaults.AuthenticationScheme);
            }
        }

        private bool IsInvalidClientCertificateError(MsalServiceException exMsal)
        {
            return !_retryClientCertificate &&
                string.Equals(exMsal.ErrorCode, Constants.InvalidClient, StringComparison.OrdinalIgnoreCase) &&
                exMsal.Message.Contains(Constants.InvalidKeyError, StringComparison.OrdinalIgnoreCase);
        }

        private string BuildCurrentUriFromRequest(
            HttpContext httpContext,
            HttpRequest request,
            MergedOptions mergedOptions)
        {
            // need to lock to avoid threading issues with code outside of this library
            // https://docs.microsoft.com/en-us/aspnet/core/performance/performance-best-practices?#do-not-access-httpcontext-from-multiple-threads
            lock (httpContext)
            {
                return UriHelper.BuildAbsolute(
                    request.Scheme,
                    request.Host,
                    request.PathBase,
                    mergedOptions.CallbackPath.Value ?? string.Empty);
            }
        }

        internal /* for testing */ IConfidentialClientApplication GetOrBuildConfidentialClientApplication(
           MergedOptions mergedOptions)
        {
            if (_application == null)
            {
                lock (_applicationSyncObj)
                {
                    if (_application == null)
                    {
                        _application = BuildConfidentialClientApplication(mergedOptions);
                    }
                }
            }

            return _application;
        }

        /// <summary>
        /// Creates an MSAL confidential client application.
        /// </summary>
        private IConfidentialClientApplication BuildConfidentialClientApplication(MergedOptions mergedOptions)
        {
            var httpContext = CurrentHttpContext;
            var request = httpContext?.Request;
            string? currentUri = null;

            if (!string.IsNullOrEmpty(mergedOptions.ConfidentialClientApplicationOptions.RedirectUri))
            {
                currentUri = mergedOptions.ConfidentialClientApplicationOptions.RedirectUri;
            }

            if (request != null && string.IsNullOrEmpty(currentUri))
            {
                currentUri = BuildCurrentUriFromRequest(
                    httpContext!,
                    request,
                    mergedOptions);
            }

            mergedOptions.PrepareAuthorityInstanceForMsal();

            MergedOptionsValidation.ValidateEitherClientCertificateOrClientSecret(
                 mergedOptions.ClientSecret,
                 mergedOptions.ClientCertificates);

            try
            {
                var builder = ConfidentialClientApplicationBuilder
                        .CreateWithApplicationOptions(mergedOptions.ConfidentialClientApplicationOptions)
                        .WithHttpClientFactory(_httpClientFactory)
                        .WithLogging(
                            Log,
                            ConvertMicrosoftExtensionsLogLevelToMsal(_logger),
                            enablePiiLogging: mergedOptions.ConfidentialClientApplicationOptions.EnablePiiLogging)
                        .WithExperimentalFeatures();

                // The redirect URI is not needed for OBO
                if (!string.IsNullOrEmpty(currentUri))
                {
                    builder.WithRedirectUri(currentUri);
                }

                string authority;

                if (mergedOptions.IsB2C)
                {
                    authority = $"{mergedOptions.Instance}{ClaimConstants.Tfp}/{mergedOptions.Domain}/{mergedOptions.DefaultUserFlow}";
                    builder.WithB2CAuthority(authority);
                }
                else
                {
                    authority = $"{mergedOptions.Instance}{mergedOptions.TenantId}/";
                    builder.WithAuthority(authority);
                }

                if (mergedOptions.ClientCertificates != null)
                {
                    X509Certificate2? certificate = DefaultCertificateLoader.LoadFirstCertificate(mergedOptions.ClientCertificates);
                    if (certificate == null)
                    {
                        Logger.TokenAcquisitionError(
                            _logger,
                            IDWebErrorMessage.ClientCertificatesHaveExpiredOrCannotBeLoaded,
                            null);
                        throw new ArgumentException(
                            IDWebErrorMessage.ClientCertificatesHaveExpiredOrCannotBeLoaded,
                            nameof(mergedOptions.ClientCertificates));
                    }

                    builder.WithCertificate(certificate);
                }

                IConfidentialClientApplication app = builder.Build();
                _application = app;
                // Initialize token cache providers
                _tokenCacheProvider.Initialize(app.AppTokenCache);
                _tokenCacheProvider.Initialize(app.UserTokenCache);
                return app;
            }
            catch (Exception ex)
            {
                Logger.TokenAcquisitionError(
                    _logger,
                    IDWebErrorMessage.ExceptionAcquiringTokenForConfidentialClient,
                    ex);
                throw;
            }
        }

        private async Task<AuthenticationResult?> GetAuthenticationResultForWebApiToCallDownstreamApiAsync(
           IConfidentialClientApplication application,
           string authority,
           IEnumerable<string> scopes,
           TokenAcquisitionOptions? tokenAcquisitionOptions,
           MergedOptions mergedOptions)
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

                    var builder = application
                                    .AcquireTokenOnBehalfOf(
                                        scopes.Except(_scopesRequestedByMsal),
                                        new UserAssertion(tokenUsedToCallTheWebApi))
                                    .WithSendX5C(mergedOptions.SendX5C)
                                    .WithAuthority(authority);

                    if (tokenAcquisitionOptions != null)
                    {
                        builder.WithExtraQueryParameters(tokenAcquisitionOptions.ExtraQueryParameters);
                        builder.WithCorrelationId(tokenAcquisitionOptions.CorrelationId);
                        builder.WithForceRefresh(tokenAcquisitionOptions.ForceRefresh);
                        builder.WithClaims(tokenAcquisitionOptions.Claims);
                        if (tokenAcquisitionOptions.PoPConfiguration != null)
                        {
                            builder.WithProofOfPossession(tokenAcquisitionOptions.PoPConfiguration);
                        }
                    }

                    ClaimsPrincipal? user = GetUserFromHttpContext();
                    if (user != null)
                    {
                        builder.WithCcsRoutingHint(user.GetObjectId(), user.GetTenantId());
                    }

                    return await builder.ExecuteAsync(tokenAcquisitionOptions != null ? tokenAcquisitionOptions.CancellationToken : CancellationToken.None)
                                        .ConfigureAwait(false);
                }

                return null;
            }
            catch (MsalUiRequiredException ex)
            {
                Logger.TokenAcquisitionError(
                    _logger,
                    LogMessages.ErrorAcquiringTokenForDownstreamWebApi + ex.Message,
                    ex);
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
        /// <param name="mergedOptions">Merged options.</param>
        /// <param name="userFlow">Azure AD B2C user flow to target.</param>
        /// <param name="tokenAcquisitionOptions">Options passed-in to create the token acquisition object which calls into MSAL .NET.</param>
        private async Task<AuthenticationResult> GetAuthenticationResultForWebAppWithAccountFromCacheAsync(
            IConfidentialClientApplication application,
            ClaimsPrincipal? claimsPrincipal,
            IEnumerable<string> scopes,
            string? authority,
            MergedOptions mergedOptions,
            string? userFlow = null,
            TokenAcquisitionOptions? tokenAcquisitionOptions = null)
        {
            IAccount? account = null;
            if (mergedOptions.IsB2C && !string.IsNullOrEmpty(userFlow))
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

            return await GetAuthenticationResultForWebAppWithAccountFromCacheAsync(
                application,
                account,
                scopes,
                authority,
                mergedOptions,
                userFlow,
                tokenAcquisitionOptions).ConfigureAwait(false);
        }

        /// <summary>
        /// Gets an access token for a downstream API on behalf of the user whose account is passed as an argument.
        /// </summary>
        /// <param name="application"><see cref="IConfidentialClientApplication"/>.</param>
        /// <param name="account">User IAccount for which to acquire a token.
        /// See <see cref="Microsoft.Identity.Client.AccountId.Identifier"/>.</param>
        /// <param name="scopes">Scopes for the downstream API to call.</param>
        /// <param name="authority">Authority based on a specific tenant for which to acquire a token to access the scopes
        /// on behalf of the user.</param>
        /// <param name="mergedOptions">Merged options.</param>
        /// <param name="userFlow">Azure AD B2C user flow.</param>
        /// <param name="tokenAcquisitionOptions">Options passed-in to create the token acquisition object which calls into MSAL .NET.</param>
        private Task<AuthenticationResult> GetAuthenticationResultForWebAppWithAccountFromCacheAsync(
            IConfidentialClientApplication application,
            IAccount? account,
            IEnumerable<string> scopes,
            string? authority,
            MergedOptions mergedOptions,
            string? userFlow = null,
            TokenAcquisitionOptions? tokenAcquisitionOptions = null)
        {
            if (scopes == null)
            {
                throw new ArgumentNullException(nameof(scopes));
            }

            var builder = application
                    .AcquireTokenSilent(scopes.Except(_scopesRequestedByMsal), account)
                    .WithSendX5C(mergedOptions.SendX5C);

            if (tokenAcquisitionOptions != null)
            {
                builder.WithExtraQueryParameters(tokenAcquisitionOptions.ExtraQueryParameters);
                builder.WithCorrelationId(tokenAcquisitionOptions.CorrelationId);
                builder.WithForceRefresh(tokenAcquisitionOptions.ForceRefresh);
                builder.WithClaims(tokenAcquisitionOptions.Claims);
                if (tokenAcquisitionOptions.PoPConfiguration != null)
                {
                    builder.WithProofOfPossession(tokenAcquisitionOptions.PoPConfiguration);
                }
            }

            // Acquire an access token as a B2C authority
            if (mergedOptions.IsB2C)
            {
                string b2cAuthority = application.Authority.Replace(
                    new Uri(application.Authority).PathAndQuery,
                    $"/{ClaimConstants.Tfp}/{mergedOptions.Domain}/{userFlow ?? mergedOptions.DefaultUserFlow}",
                    StringComparison.OrdinalIgnoreCase);

                builder.WithB2CAuthority(b2cAuthority)
                       .WithSendX5C(mergedOptions.SendX5C);
            }
            else
            {
                builder.WithAuthority(authority);
            }

            return builder.ExecuteAsync(tokenAcquisitionOptions != null ? tokenAcquisitionOptions.CancellationToken : CancellationToken.None);
        }

        private static bool AcceptedTokenVersionMismatch(MsalUiRequiredException msalServiceException)
        {
            // Normally app developers should not make decisions based on the internal AAD code
            // however until the STS sends sub-error codes for this error, this is the only
            // way to distinguish the case.
            // This is subject to change in the future
            return msalServiceException.Message.Contains(
                ErrorCodes.B2CPasswordResetErrorCode,
                StringComparison.InvariantCulture);
        }

        private ClaimsPrincipal? GetUserFromHttpContext()
        {
            var httpContext = CurrentHttpContext;
            if (httpContext != null)
            {
                // Need to lock due to https://docs.microsoft.com/en-us/aspnet/core/performance/performance-best-practices?#do-not-access-httpcontext-from-multiple-threads
                lock (httpContext)
                {
                    return httpContext.User;
                }
            }

            return null;
        }

        private async Task<ClaimsPrincipal?> GetAuthenticatedUserAsync(ClaimsPrincipal? user)
        {
            if (user == null)
            {
                user = GetUserFromHttpContext();
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
                    // do nothing.
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
                authority = application.Authority.Replace(
                    new Uri(application.Authority).PathAndQuery,
                    $"/{tenant}/",
                    StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                authority = application.Authority;
            }

            return authority;
        }

        private void Log(
          Client.LogLevel level,
          string message,
          bool containsPii)
        {
            switch (level)
            {
                case Client.LogLevel.Error:
                    _logger.LogError(message);
                    break;
                case Client.LogLevel.Warning:
                    _logger.LogWarning(message);
                    break;
                case Client.LogLevel.Info:
                    _logger.LogInformation(message);
                    break;
                case Client.LogLevel.Verbose:
                    _logger.LogDebug(message);
                    break;
                default:
                    break;
            }
        }

        private Client.LogLevel? ConvertMicrosoftExtensionsLogLevelToMsal(ILogger logger)
        {
            if (logger.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug)
                || logger.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Trace))
            {
                return Client.LogLevel.Verbose;
            }
            else if (logger.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Information))
            {
                return Client.LogLevel.Info;
            }
            else if (logger.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Warning))
            {
                return Client.LogLevel.Warning;
            }
            else if (logger.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Error)
                || logger.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Critical))
            {
                return Client.LogLevel.Error;
            }
            else
            {
                return null;
            }
        }
    }
}
