// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Advanced;
using Microsoft.Identity.Web.TokenCacheProviders;
using Microsoft.Identity.Web.TokenCacheProviders.InMemory;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Token acquisition service.
    /// </summary>
    internal partial class TokenAcquisition : ITokenAcquirer
    {
#if NET472 || NET462
        class OAuthConstants
        {
            public static readonly string CodeVerifierKey = "code_verifier";
        }
#endif

        protected readonly IMsalTokenCacheProvider _tokenCacheProvider;

        private readonly object _applicationSyncObj = new object();

        /// <summary>
        ///  Please call GetOrBuildConfidentialClientApplication instead of accessing this field directly.
        /// </summary>
        private ConcurrentDictionary<string, IConfidentialClientApplication?> _applicationsByAuthorityClientId = new ConcurrentDictionary<string, IConfidentialClientApplication?>();
        private bool _retryClientCertificate;
        protected readonly IMsalHttpClientFactory _httpClientFactory;
        protected readonly ILogger _logger;
        private readonly IServiceProvider _serviceProvider;
        protected readonly ITokenAcquisitionHost _tokenAcquisitionHost;

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
        /// Constructor of the TokenAcquisition service. This requires the Azure AD Options to
        /// configure the confidential client application and a token cache provider.
        /// This constructor is called by ASP.NET Core dependency injection.
        /// </summary>
        /// <param name="tokenCacheProvider">The App token cache provider.</param>
        /// <param name="tokenAcquisitionHost">Host of the token acquisition.</param>
        /// <param name="httpClientFactory">HTTP client factory.</param>
        /// <param name="logger">Logger.</param>
        /// <param name="serviceProvider">Service provider.</param>
        public TokenAcquisition(
            IMsalTokenCacheProvider tokenCacheProvider,
            ITokenAcquisitionHost tokenAcquisitionHost,
            IHttpClientFactory httpClientFactory,
            ILogger<TokenAcquisition> logger,
            IServiceProvider serviceProvider)
        {
            _tokenCacheProvider = tokenCacheProvider;
            _httpClientFactory = new MsalAspNetCoreHttpClientFactory(httpClientFactory);
            _logger = logger;
            _serviceProvider = serviceProvider;
            _tokenAcquisitionHost = tokenAcquisitionHost;
        }

        public async Task<string> AddAccountToCacheFromAuthorizationCodeAsync(IEnumerable<string> scopes, string authCode, string authenticationScheme, string? clientInfo, string? codeVerifier, string? userFlow)
        {
            if (scopes == null)
            {
                throw new ArgumentNullException(nameof(scopes));
            }
            MergedOptions mergedOptions = _tokenAcquisitionHost.GetOptions(authenticationScheme, out string effectiveAuthenticationScheme);

            try
            {
                var application = GetOrBuildConfidentialClientApplication(mergedOptions);

                // Do not share the access token with ASP.NET Core otherwise ASP.NET will cache it and will not send the OAuth 2.0 request in
                // case a further call to AcquireTokenByAuthorizationCodeAsync in the future is required for incremental consent (getting a code requesting more scopes)
                // Share the ID token though

                string? backUpAuthRoutingHint = string.Empty;
                if (!string.IsNullOrEmpty(clientInfo))
                {
                    ClientInfo? clientInfoFromAuthorize = ClientInfo.CreateFromJson(clientInfo!);
                    if (clientInfoFromAuthorize != null && clientInfoFromAuthorize.UniqueTenantIdentifier != null && clientInfoFromAuthorize.UniqueObjectIdentifier != null)
                    {
                        backUpAuthRoutingHint = $"oid:{clientInfoFromAuthorize.UniqueObjectIdentifier}@{clientInfoFromAuthorize.UniqueTenantIdentifier}";
                    }
                }

                var builder = application
                    .AcquireTokenByAuthorizationCode(scopes.Except(_scopesRequestedByMsal), authCode)
                    .WithSendX5C(mergedOptions.SendX5C)
                    .WithPkceCodeVerifier(codeVerifier)
                    .WithCcsRoutingHint(backUpAuthRoutingHint)
                    .WithSpaAuthorizationCode(mergedOptions.WithSpaAuthCode);

                if (mergedOptions.IsB2C)
                {

                    var authority = $"{mergedOptions.Instance}{ClaimConstants.Tfp}/{mergedOptions.Domain}/{userFlow ?? mergedOptions.DefaultUserFlow}";
                    builder.WithB2CAuthority(authority);
                }

                var result = await builder.ExecuteAsync()
                                          .ConfigureAwait(false);

                if (!string.IsNullOrEmpty(result.SpaAuthCode))
                {
                    _tokenAcquisitionHost.SetSession(Constants.SpaAuthCode, result.SpaAuthCode);
                }

                return result.IdToken;
            }
            catch (MsalServiceException exMsal) when (IsInvalidClientCertificateError(exMsal))
            {
                DefaultCertificateLoader.ResetCertificates(mergedOptions.ClientCertificates);
                _applicationsByAuthorityClientId[GetApplicationKey(mergedOptions)] = null;

                // Retry
                _retryClientCertificate = true;
                await AddAccountToCacheFromAuthorizationCodeAsync(scopes, authCode, authenticationScheme, clientInfo, codeVerifier, userFlow).ConfigureAwait(false);
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

            return authenticationScheme;
        }

        private static string GetApplicationKey(MergedOptions mergedOptions)
        {
            return mergedOptions.Instance! + mergedOptions.ClientId;
        }

        /// <summary>
        /// Typically used from a web app or web API controller, this method retrieves an access token
        /// for a downstream API using;
        /// 1) the token cache (for web apps and web APIs) if a token exists in the cache
        /// 2) or the <a href='https://docs.microsoft.com/azure/active-directory/develop/v2-oauth2-on-behalf-of-flow'>on-behalf-of flow</a>
        /// in web APIs, for the user account that is ascertained from claims provided in the current claims principal.
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

            MergedOptions mergedOptions = _tokenAcquisitionHost.GetOptions(authenticationScheme, out _);

            user = await _tokenAcquisitionHost.GetAuthenticatedUserAsync(user).ConfigureAwait(false);

            var application = GetOrBuildConfidentialClientApplication(mergedOptions);

            try
            {
                AuthenticationResult? authenticationResult;
                // Access token will return if call is from a web API
                authenticationResult = await GetAuthenticationResultForWebApiToCallDownstreamApiAsync(
                    application,
                    tenantId,
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
                     tenantId,
                     mergedOptions,
                     userFlow,
                     tokenAcquisitionOptions)
                     .ConfigureAwait(false);
                LogAuthResult(authenticationResult);
                return authenticationResult;
            }
            catch (MsalServiceException exMsal) when (IsInvalidClientCertificateError(exMsal))
            {
                DefaultCertificateLoader.ResetCertificates(mergedOptions.ClientCertificates);
                _applicationsByAuthorityClientId[GetApplicationKey(mergedOptions)] = null;

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
                authenticationResult.AuthenticationResultMetadata.CacheRefreshReason.ToString(),
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

            MergedOptions mergedOptions = _tokenAcquisitionHost.GetOptions(authenticationScheme, out _);


            if (string.IsNullOrEmpty(tenant))
            {
                tenant = mergedOptions.TenantId;
            }

            if (!string.IsNullOrEmpty(tenant) && _metaTenantIdentifiers.Contains(tenant!))
            {
                throw new ArgumentException(IDWebErrorMessage.ClientCredentialTenantShouldBeTenanted, nameof(tenant));
            }

            // Use MSAL to get the right token to call the API
            var application = GetOrBuildConfidentialClientApplication(mergedOptions);

            var builder = application
                   .AcquireTokenForClient(new string[] { scope }.Except(_scopesRequestedByMsal))
                   .WithSendX5C(mergedOptions.SendX5C)
                   .WithTenantId(tenant);

            if (tokenAcquisitionOptions != null)
            {
                if (tokenAcquisitionOptions.ExtraQueryParameters != null)
                {
                    builder.WithExtraQueryParameters(new Dictionary<string, string>(tokenAcquisitionOptions.ExtraQueryParameters));
                }
                if (tokenAcquisitionOptions.ExtraHeadersParameters != null)
                {
                    builder.WithExtraHttpHeaders(tokenAcquisitionOptions.ExtraHeadersParameters);
                }
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
                _applicationsByAuthorityClientId[GetApplicationKey(mergedOptions)] = null;

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
        /// in web APIs, for the user account that is ascertained from the claims provided in the current claims principal.
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
        /// Removes the account associated with context.HttpContext.User from the MSAL.NET cache.
        /// </summary>
        /// <param name="user">User</param>
        /// <param name="authenticationScheme">Authentication scheme. If null, will use OpenIdConnectDefault.AuthenticationScheme
        /// if called from a web app, and JwtBearerDefault.AuthenticationScheme if called from a web API.</param>
        /// <returns>A <see cref="Task"/> that represents a completed account removal operation.</returns>
        public async Task RemoveAccountAsync(
            ClaimsPrincipal user,
            string? authenticationScheme)
        {
            string? userId = user.GetMsalAccountId();
            if (!string.IsNullOrEmpty(userId))
            {
                MergedOptions mergedOptions = _tokenAcquisitionHost.GetOptions(authenticationScheme, out _);

                IConfidentialClientApplication app = GetOrBuildConfidentialClientApplication(mergedOptions);

                if (mergedOptions.IsB2C)
                {
                    await _tokenCacheProvider.ClearAsync(userId!).ConfigureAwait(false);
                }
                else
                {
                    string? identifier = user.GetMsalAccountId();
                    IAccount account = await app.GetAccountAsync(identifier).ConfigureAwait(false);

                    if (account != null)
                    {
                        await app.RemoveAsync(account).ConfigureAwait(false);
                        await _tokenCacheProvider.ClearAsync(userId!).ConfigureAwait(false);
                    }
                }
            }
        }

        private bool IsInvalidClientCertificateError(MsalServiceException exMsal)
        {
            return !_retryClientCertificate &&
                string.Equals(exMsal.ErrorCode, Constants.InvalidClient, StringComparison.OrdinalIgnoreCase) &&
#if !NET472 && !NET462
                exMsal.Message.Contains(Constants.InvalidKeyError, StringComparison.OrdinalIgnoreCase);
#else
                exMsal.Message.Contains(Constants.InvalidKeyError);
#endif
        }

        internal /* for testing */ IConfidentialClientApplication GetOrBuildConfidentialClientApplication(
           MergedOptions mergedOptions)
        {
            IConfidentialClientApplication? application;
            if (!_applicationsByAuthorityClientId.TryGetValue(GetApplicationKey(mergedOptions), out application) || application == null)
            {
                lock (_applicationSyncObj)
                {
                    application = BuildConfidentialClientApplication(mergedOptions);
                    _applicationsByAuthorityClientId.TryAdd(GetApplicationKey(mergedOptions), application);
                }
            }

            return application;
        }

        /// <summary>
        /// Creates an MSAL confidential client application.
        /// </summary>
        private IConfidentialClientApplication BuildConfidentialClientApplication(MergedOptions mergedOptions)
        {
            string? currentUri = _tokenAcquisitionHost.GetCurrentRedirectUri(mergedOptions);
            mergedOptions.PrepareAuthorityInstanceForMsal();

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

                if (mergedOptions.ClientCredentialsUsingManagedIdentity != null && mergedOptions.ClientCredentialsUsingManagedIdentity.IsEnabled)
                {
                    builder.WithExtraQueryParameters("dc=ESTS-PUB-WUS2-AZ1-FD000-TEST2");
                }

                if (_tokenCacheProvider is MsalMemoryTokenCacheProvider)
                {
                    builder.WithCacheOptions(CacheOptions.EnableSharedCacheOptions);
                }

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

                // Case where no client certificates and not client secret was provided, but the user assigned
                // managed identity was set. In that case we leverage the MsiSignedAssertion to be cert-less.
                if (string.IsNullOrWhiteSpace(mergedOptions.ClientSecret)
                    && (mergedOptions.ClientCertificates == null || !mergedOptions.ClientCertificates.Any())
                    && mergedOptions.ClientCredentialsUsingManagedIdentity != null
                    && mergedOptions.ClientCredentialsUsingManagedIdentity.IsEnabled)
                {
                    builder.WithClientAssertion(new ManagedIdentityClientAssertion(mergedOptions.ClientCredentialsUsingManagedIdentity.ManagedIdentityObjectId).GetSignedAssertion);
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

                // Initialize token cache providers
                if (!(_tokenCacheProvider is MsalMemoryTokenCacheProvider))
                {
                    _tokenCacheProvider.Initialize(app.AppTokenCache);
                    _tokenCacheProvider.Initialize(app.UserTokenCache);
                }

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
           string? tenantId,
           IEnumerable<string> scopes,
           TokenAcquisitionOptions? tokenAcquisitionOptions,
           MergedOptions mergedOptions)
        {
            try
            {
                // In web API, validatedToken will not be null
                SecurityToken? validatedToken = _tokenAcquisitionHost.GetTokenUsedToCallWebAPI();

                // In the case the token is a JWE (encrypted token), we use the decrypted token.
                string? tokenUsedToCallTheWebApi = GetActualToken(validatedToken);

                AcquireTokenOnBehalfOfParameterBuilder? builder = null;

                // Case of web APIs: we need to do an on-behalf-of flow, with the token used to call the API
                if (tokenUsedToCallTheWebApi != null)
                {
                    if (string.IsNullOrEmpty(tokenAcquisitionOptions?.LongRunningWebApiSessionKey))
                    {
                        builder = application
                                        .AcquireTokenOnBehalfOf(
                                            scopes.Except(_scopesRequestedByMsal),
                                            new UserAssertion(tokenUsedToCallTheWebApi));
                    }
                    else
                    {
                        string? sessionKey = tokenAcquisitionOptions!.LongRunningWebApiSessionKey;
                        if (sessionKey == TokenAcquisitionOptions.LongRunningWebApiSessionKeyAuto)
                        {
                            sessionKey = null;
                        }

                        builder = (application as ILongRunningWebApi)?
                                       .InitiateLongRunningProcessInWebApi(
                                           scopes.Except(_scopesRequestedByMsal),
                                           tokenUsedToCallTheWebApi,
                                           ref sessionKey);
                        tokenAcquisitionOptions.LongRunningWebApiSessionKey = sessionKey;
                    }
                }
                else if (!string.IsNullOrEmpty(tokenAcquisitionOptions?.LongRunningWebApiSessionKey))
                {
                    string sessionKey = tokenAcquisitionOptions!.LongRunningWebApiSessionKey!;
                    builder = (application as ILongRunningWebApi)?
                                   .AcquireTokenInLongRunningProcess(
                                       scopes.Except(_scopesRequestedByMsal),
                                       sessionKey);
                }

                if (builder != null)
                {
                    builder.WithSendX5C(mergedOptions.SendX5C);

                    ClaimsPrincipal? user = _tokenAcquisitionHost.GetUserFromRequest();
                    var userTenant = string.Empty;
                    if (user != null)
                    {
                        userTenant = user.GetTenantId();
                        builder.WithCcsRoutingHint(user.GetObjectId(), userTenant);
                    }
                    if (!string.IsNullOrEmpty(tenantId))
                    {
                        builder.WithTenantId(tenantId);
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(userTenant))
                        {
                            builder.WithTenantId(userTenant);
                        }
                    }
                    if (tokenAcquisitionOptions != null)
                    {
                        if (tokenAcquisitionOptions.ExtraQueryParameters != null)
                        {
                            builder.WithExtraQueryParameters(new Dictionary<string, string>(tokenAcquisitionOptions.ExtraQueryParameters));
                        }
                        if (tokenAcquisitionOptions.ExtraHeadersParameters != null)
                        {
                            builder.WithExtraHttpHeaders(tokenAcquisitionOptions.ExtraHeadersParameters);
                        }
                        builder.WithCorrelationId(tokenAcquisitionOptions.CorrelationId);
                        builder.WithForceRefresh(tokenAcquisitionOptions.ForceRefresh);
                        builder.WithClaims(tokenAcquisitionOptions.Claims);
                        if (tokenAcquisitionOptions.PoPConfiguration != null)
                        {
                            builder.WithProofOfPossession(tokenAcquisitionOptions.PoPConfiguration);
                        }
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

        private static string? GetActualToken(SecurityToken? validatedToken)
        {
            JwtSecurityToken? jwtSecurityToken = validatedToken as JwtSecurityToken;
            if (jwtSecurityToken != null)
            {
                // In the case the token is a JWE (encrypted token), we use the decrypted token.
                return jwtSecurityToken.InnerToken == null ? jwtSecurityToken.RawData
                                            : jwtSecurityToken.InnerToken.RawData;
            }

            JsonWebToken? jsonWebToken = validatedToken as JsonWebToken;
            if (jsonWebToken != null)
            {
                // In the case the token is a JWE (encrypted token), we use the decrypted token.
                return jsonWebToken.InnerToken == null ? jsonWebToken.EncodedToken
                                            : jsonWebToken.InnerToken.EncodedToken;
            }

            return null;
        }

        /// <summary>
        /// Gets an access token for a downstream API on behalf of the user described by its claimsPrincipal.
        /// </summary>
        /// <param name="application"><see cref="IConfidentialClientApplication"/>.</param>
        /// <param name="claimsPrincipal">Claims principal for the user on behalf of whom to get a token.</param>
        /// <param name="scopes">Scopes for the downstream API to call.</param>
        /// <param name="tenantId">(optional) TenantID based on a specific tenant for which to acquire a token to access the scopes
        /// on behalf of the user described in the claimsPrincipal.</param>
        /// <param name="mergedOptions">Merged options.</param>
        /// <param name="userFlow">Azure AD B2C user flow to target.</param>
        /// <param name="tokenAcquisitionOptions">Options passed-in to create the token acquisition object which calls into MSAL .NET.</param>
        private async Task<AuthenticationResult> GetAuthenticationResultForWebAppWithAccountFromCacheAsync(
            IConfidentialClientApplication application,
            ClaimsPrincipal? claimsPrincipal,
            IEnumerable<string> scopes,
            string? tenantId,
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
                tenantId,
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
        /// <param name="tenantId">TenantID based on a specific tenant for which to acquire a token to access the scopes
        /// on behalf of the user.</param>
        /// <param name="mergedOptions">Merged options.</param>
        /// <param name="userFlow">Azure AD B2C user flow.</param>
        /// <param name="tokenAcquisitionOptions">Options passed-in to create the token acquisition object which calls into MSAL .NET.</param>
        private Task<AuthenticationResult> GetAuthenticationResultForWebAppWithAccountFromCacheAsync(
            IConfidentialClientApplication application,
            IAccount? account,
            IEnumerable<string> scopes,
            string? tenantId,
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
                if (tokenAcquisitionOptions.ExtraQueryParameters != null)
                {
                    builder.WithExtraQueryParameters(new Dictionary<string, string>(tokenAcquisitionOptions.ExtraQueryParameters));
                }
                if (tokenAcquisitionOptions.ExtraHeadersParameters != null)
                {
                    builder.WithExtraHttpHeaders(tokenAcquisitionOptions.ExtraHeadersParameters);
                }
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
                    $"/{ClaimConstants.Tfp}/{mergedOptions.Domain}/{userFlow ?? mergedOptions.DefaultUserFlow}"
#if !NET472 && !NET462
                    , StringComparison.OrdinalIgnoreCase
#endif
                    );

                builder.WithB2CAuthority(b2cAuthority)
                       .WithSendX5C(mergedOptions.SendX5C);
            }
            else if (!string.IsNullOrEmpty(tenantId))
            {
                builder.WithTenantId(tenantId);
            }

            return builder.ExecuteAsync(tokenAcquisitionOptions != null ? tokenAcquisitionOptions.CancellationToken : CancellationToken.None);
        }

        protected static bool AcceptedTokenVersionMismatch(MsalUiRequiredException msalServiceException)
        {
            // Normally app developers should not make decisions based on the internal AAD code
            // however until the STS sends sub-error codes for this error, this is the only
            // way to distinguish the case.
            // This is subject to change in the future
            return msalServiceException.Message.Contains(
                ErrorCodes.B2CPasswordResetErrorCode
#if !NET472 && !NET462
                , StringComparison.InvariantCulture
#endif
                );
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

        public string GetEffectiveAuthenticationScheme(string? authenticationScheme)
        {
            return _tokenAcquisitionHost.GetEffectiveAuthenticationScheme(authenticationScheme);
        }

        async Task<AcquireTokenResult> ITokenAcquirer.GetTokenForUserAsync(IEnumerable<string> scopes, AcquireTokenOptions? tokenAcquisitionOptions, ClaimsPrincipal? user, CancellationToken cancellationToken)
        {
            var result = await GetAuthenticationResultForUserAsync(
                scopes,
                tokenAcquisitionOptions?.AuthenticationScheme,
                tokenAcquisitionOptions?.Tenant,
                tokenAcquisitionOptions?.UserFlow,
                user,
                (tokenAcquisitionOptions == null) ? null : new TokenAcquisitionOptions()
                {
                    AuthenticationScheme = tokenAcquisitionOptions?.AuthenticationScheme,
                    CancellationToken = cancellationToken,
                    Claims = tokenAcquisitionOptions!.Claims,
                    CorrelationId = tokenAcquisitionOptions!.CorrelationId,
                    ExtraQueryParameters = tokenAcquisitionOptions.ExtraQueryParameters,
                    ForceRefresh = tokenAcquisitionOptions.ForceRefresh,
                    LongRunningWebApiSessionKey = tokenAcquisitionOptions.LongRunningWebApiSessionKey,
                    Tenant = tokenAcquisitionOptions.Tenant,
                    UserFlow = tokenAcquisitionOptions.UserFlow,
                    // TODO: PopKeyId?
                }).ConfigureAwait(false);

            return new AcquireTokenResult(
                result.AccessToken,
                result.ExpiresOn,
                result.TenantId,
                result.IdToken,
                result.Scopes,
                result.CorrelationId);
        }

        async Task<AcquireTokenResult> ITokenAcquirer.GetTokenForAppAsync(string scope, AcquireTokenOptions? tokenAcquisitionOptions, CancellationToken cancellationToken)
        {
            var result = await GetAuthenticationResultForAppAsync(
                scope, 
                tokenAcquisitionOptions?.AuthenticationScheme, 
                tokenAcquisitionOptions?.Tenant,
                (tokenAcquisitionOptions == null) ? null : new TokenAcquisitionOptions()
                {
                    AuthenticationScheme = tokenAcquisitionOptions?.AuthenticationScheme,
                    CancellationToken = cancellationToken,
                    Claims = tokenAcquisitionOptions!.Claims,
                    CorrelationId = tokenAcquisitionOptions.CorrelationId,
                    ExtraQueryParameters = tokenAcquisitionOptions.ExtraQueryParameters,
                    ForceRefresh = tokenAcquisitionOptions.ForceRefresh,
                    LongRunningWebApiSessionKey = tokenAcquisitionOptions.LongRunningWebApiSessionKey,
                    Tenant = tokenAcquisitionOptions.Tenant,
                    UserFlow = tokenAcquisitionOptions.UserFlow,
                    // TODO: PopKeyId?
                }).ConfigureAwait(false);

            return new AcquireTokenResult(
                result.AccessToken,
                result.ExpiresOn,
                result.TenantId,
                result.IdToken,
                result.Scopes,
                result.CorrelationId);
        }
    }
}
