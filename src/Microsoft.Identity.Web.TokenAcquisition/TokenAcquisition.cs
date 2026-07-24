// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensibility;
#if NETCOREAPP
using Microsoft.Identity.Client.KeyAttestation;
#endif
using Microsoft.Identity.Web.Experimental;
using Microsoft.Identity.Web.Extensibility;
using Microsoft.Identity.Web.TestOnly;
using Microsoft.Identity.Web.TokenCacheProviders;
using Microsoft.Identity.Web.TokenCacheProviders.InMemory;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.LoggingExtensions;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Token acquisition service.
    /// </summary>
    /*
     * Used by Microsoft.Identity.Web
     * Any changes to this member (including removal) can cause runtime failures.
     * Treat as a public member.
     */
#if NETSTANDARD2_0 || NET462 || NET472
    internal partial class TokenAcquisition : ITokenAcquisitionInternal, IConfidentialClientApplicationProvider
#else
    internal partial class TokenAcquisition : IConfidentialClientApplicationProvider
#endif
    {
#if NETSTANDARD2_0 || NET462 || NET472
        class OAuthConstants
        {
            public static readonly string CodeVerifierKey = "code_verifier";
        }
#endif
        protected readonly IMsalTokenCacheProvider _tokenCacheProvider;

        /// <summary>
        ///  Important: call GetOrBuildConfidentialClientApplication instead of accessing _applicationsByAuthorityClientId directly.
        ///  Write access to this dictionary is synchronized.
        /// </summary>
        internal readonly ConcurrentDictionary<string, IConfidentialClientApplication?> _applicationsByAuthorityClientId = new();
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _appSemaphores = new();

        /// <summary>
        /// Maximum number of CCA instances to keep in the shared application dictionary
        /// before clearing it as a DOS protection measure. Token data lives in external
        /// caches (MSAL's shared static cache for in-memory providers, or the distributed
        /// cache provider for Redis/SQL/etc.), so clearing the dictionary only discards
        /// lightweight CCA objects — tokens remain accessible to newly-built CCAs.
        /// Eviction is only triggered by agent CCA creation, since normal CCAs are bounded
        /// by the number of configured authentication schemes.
        /// </summary>
        internal int AgentCcaMaxCount { get; set; } = 10000;

        /// <summary>
        /// Maps (agentAppId, user identifier, tenantId) tuples to MSAL account identifiers for the
        /// native User FIC flow. This is needed because AcquireTokenSilent requires an IAccount,
        /// which can only be obtained from GetAccountAsync(identifier) using an identifier that
        /// comes back from a prior token acquisition. In all other ID Web flows, this identifier
        /// is stored in the ClaimsPrincipal (via GetMsalAccountId / oid+tid claims). In the
        /// agentic scenario, however, ClaimsPrincipal is typically null or freshly created per
        /// request (bot/service pattern), so there is no persistent object to write back to.
        /// This dictionary fills that role, keyed by "{agentAppId}:{USER_IDENTIFIER}:{TENANTID}"
        /// where USER_IDENTIFIER is either the normalized UPN or OID.
        /// Entries are cleaned up opportunistically (when GetAccountAsync returns null during
        /// a silent attempt) or when the CCA dictionary is cleared due to size-threshold eviction.
        /// </summary>
        internal readonly ConcurrentDictionary<string, string> _agentUserFicAccountIds = new();

        private static readonly string[] s_ficScopes = new[] { "api://AzureADTokenExchange/.default" };

        private const string TokenBindingParameterName = "IsTokenBinding";
        private const int MaxCertificateRetries = 1;
        protected readonly IMsalHttpClientFactory _httpClientFactory;
        protected readonly ILogger _logger;
        protected readonly IServiceProvider _serviceProvider;
        protected readonly ITokenAcquisitionHost _tokenAcquisitionHost;
        protected readonly ICredentialsProvider _credentialsProvider;
        protected readonly IOptionsMonitor<TokenAcquisitionExtensionOptions>? tokenAcquisitionExtensionOptionsMonitor;

        /// <summary>
        /// Scopes which are already requested by MSAL.NET. They should not be re-requested;.
        /// </summary>
        private readonly string[] _scopesRequestedByMsal = new[] {
            OidcConstants.ScopeOpenId,
            OidcConstants.ScopeProfile,
            OidcConstants.ScopeOfflineAccess,
        };

        /// <summary>
        /// Meta-tenant identifiers which are not allowed in client credentials.
        /// </summary>
        private static readonly HashSet<string> _metaTenantIdentifiers = new HashSet<string>(
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
            _httpClientFactory = serviceProvider.GetService<IMsalHttpClientFactory>() ?? new MsalMtlsHttpClientFactory(httpClientFactory);
            _logger = logger;
            _serviceProvider = serviceProvider;
            _tokenAcquisitionHost = tokenAcquisitionHost;
            tokenAcquisitionExtensionOptionsMonitor = serviceProvider.GetService<IOptionsMonitor<TokenAcquisitionExtensionOptions>>();
            _miHttpFactory = serviceProvider.GetService<IManagedIdentityTestHttpClientFactory>();

            var credentialsProvider = serviceProvider.GetService<ICredentialsProvider>();
            if (credentialsProvider == null)
            {
                var credentialsLoader = serviceProvider.GetService<ICredentialsLoader>() ?? throw new InvalidOperationException("Either ICredentialsProvider or ICredentialsLoader must be registered and neither were.");
                credentialsProvider = new CredentialsProvider(new LogAdapter<CredentialsProvider>(logger), credentialsLoader, [.. serviceProvider.GetServices<ICertificatesObserver>()], tokenAcquisitionHost);
            }

            _credentialsProvider = credentialsProvider;
        }

#if NET6_0_OR_GREATER
        [RequiresUnreferencedCode("Calls Microsoft.Identity.Web.ClientInfo.CreateFromJson(String)")]
#endif
        public async Task<AcquireTokenResult> AddAccountToCacheFromAuthorizationCodeAsync(
            AuthCodeRedemptionParameters authCodeRedemptionParameters)
        {
            return await AddAccountToCacheFromAuthorizationCodeInternalAsync(authCodeRedemptionParameters, retryCount: 0).ConfigureAwait(false);
        }

        private async Task<AcquireTokenResult> AddAccountToCacheFromAuthorizationCodeInternalAsync(
            AuthCodeRedemptionParameters authCodeRedemptionParameters,
            int retryCount)
        {
            _ = Throws.IfNull(authCodeRedemptionParameters.Scopes);
            MergedOptions mergedOptions = _tokenAcquisitionHost.GetOptions(authCodeRedemptionParameters.AuthenticationScheme, out string effectiveAuthenticationScheme);

            CredentialSourceLoaderParameters? loaderParameters = null;
            IConfidentialClientApplication? application = null;
            try
            {
                application = await GetOrBuildConfidentialClientApplicationAsync(mergedOptions, isTokenBinding: false);

                // Do not share the access token with ASP.NET Core otherwise ASP.NET will cache it and will not send the OAuth 2.0 request in
                // case a further call to AcquireTokenByAuthorizationCodeAsync in the future is required for incremental consent (getting a code requesting more scopes)
                // Share the ID token though

                string? backUpAuthRoutingHint = string.Empty;
                if (!string.IsNullOrEmpty(authCodeRedemptionParameters.ClientInfo))
                {
                    ClientInfo? clientInfoFromAuthorize = ClientInfo.CreateFromJson(authCodeRedemptionParameters.ClientInfo);
                    if (clientInfoFromAuthorize != null && clientInfoFromAuthorize.UniqueTenantIdentifier != null && clientInfoFromAuthorize.UniqueObjectIdentifier != null)
                    {
                        backUpAuthRoutingHint = $"oid:{clientInfoFromAuthorize.UniqueObjectIdentifier}@{clientInfoFromAuthorize.UniqueTenantIdentifier}";
                    }
                }

                var builder = application
                    .AcquireTokenByAuthorizationCode(authCodeRedemptionParameters.Scopes.Except(_scopesRequestedByMsal), authCodeRedemptionParameters.AuthCode)
                    .WithSendX5C(mergedOptions.SendX5C)
                    .WithPkceCodeVerifier(authCodeRedemptionParameters.CodeVerifier)
                    .WithCcsRoutingHint(backUpAuthRoutingHint)
                    .WithSpaAuthorizationCode(mergedOptions.WithSpaAuthCode);

                loaderParameters = new CredentialSourceLoaderParameters(application.AppConfig.ClientId, application.Authority)
                {
                    Protocol = ProtocolNames.Bearer,
                };

                if (mergedOptions.ExtraQueryParameters != null)
                {
                    builder.WithExtraQueryParameters(MergeExtraQueryParameters(mergedOptions, null));
                }

                if (!string.IsNullOrEmpty(authCodeRedemptionParameters.Tenant))
                {
                    builder.WithTenantId(authCodeRedemptionParameters.Tenant);
                }

                if (mergedOptions.IsB2C)
                {

                    var authority = $"{mergedOptions.PreparedInstance}{ClaimConstants.Tfp}/{mergedOptions.Domain}/{authCodeRedemptionParameters.UserFlow ?? mergedOptions.DefaultUserFlow}";
                    builder.WithB2CAuthority(authority);
                }

                var result = await builder.ExecuteAsync()
                                          .ConfigureAwait(false);

                if (!string.IsNullOrEmpty(result.SpaAuthCode))
                {
                    _tokenAcquisitionHost.SetSession(Constants.SpaAuthCode, result.SpaAuthCode);
                }

                NotifyCertificateSelection(
                    loaderParameters,
                    mergedOptions,
                    application,
                    true,
                    null);

                return AcquireTokenResultFactory.FromMsal(result);
            }
            catch (MsalServiceException exMsal) when (retryCount < MaxCertificateRetries && IsInvalidClientCertificateOrSignedAssertionError(exMsal))
            {
                Logger.TokenAcquisitionError(
                    _logger,
                    $"Certificate error detected. Retrying with next certificate (attempt {retryCount + 1}/{MaxCertificateRetries}). {exMsal.Message}",
                    exMsal);

                string applicationKey = GetApplicationKey(mergedOptions, isTokenBinding: false);
                NotifyCertificateSelection(loaderParameters, mergedOptions, application!, false, exMsal);
                _applicationsByAuthorityClientId[applicationKey] = null;

                // Retry with incremented counter
                return await AddAccountToCacheFromAuthorizationCodeInternalAsync(authCodeRedemptionParameters, retryCount + 1).ConfigureAwait(false);
            }
            catch (MsalException ex)
            {
                Logger.TokenAcquisitionError(_logger, LogMessages.ExceptionOccurredWhenAddingAnAccountToTheCacheFromAuthCode, ex);
                throw;
            }
        }

        /// <summary>
        /// Builds a cache key for <see cref="IConfidentialClientApplication"/> instances.
        /// The key must include <paramref name="isTokenBinding"/> because bearer and mTLS PoP
        /// flows wire fundamentally different MSAL credential types (string-assertion delegate
        /// vs. certificate/bundle delegate). Reusing a CCA built for one flow in the other
        /// causes silent token-acquisition failures or 307 redirects from the STS.
        /// </summary>
        /// <param name="mergedOptions">Merged configuration options.</param>
        /// <param name="isTokenBinding">Whether mTLS token binding (PoP) is requested.
        /// Callers must pass this explicitly to avoid accidental cache collisions.</param>
        /// <param name="agentAppId">When non-null, appends an agent-specific segment so each
        /// agent CCA gets its own entry in the shared dictionary.</param>
        /// <returns>Concatenated string of authority, client id, azure region, credential id,
        /// token-binding flag, bound-signed-assertion flag, and optional agent app id.</returns>
        internal static string GetApplicationKey(MergedOptions mergedOptions, bool isTokenBinding, string? agentAppId = null)
        {
            // Credential-wiring discriminator: UseBoundCredential changes how the CCA is wired for
            // both signed-assertion credentials (bound ClientSignedAssertion callback vs. string
            // assertion callback) and certificate credentials (WithCertificate + SendCertificateOverMtls
            // vs. plain WithCertificate). Encode the bound state per credential (in place, preserving
            // order) so a bound configuration never collides on the same cache key with an otherwise-
            // identical unbound one — including mixed multi-credential arrays where only the position of
            // the bound flag differs (CredentialDescription.Id itself does not encode UseBoundCredential).
            // Normal (unbound) Bearer/certificate/secret keys stay byte-identical to before, because the
            // marker is only appended for a bound signed-assertion or certificate credential. When
            // isTokenBinding is true the "-tokenBinding" suffix already isolates the mTLS PoP wiring, so
            // the per-credential marker is not applied.
            bool markBoundCredentials = !isTokenBinding;
            string credentialId = string.Join("-", mergedOptions.ClientCredentials?.Select(c =>
                markBoundCredentials
                    && c.UseBoundCredential
                    && (c.CredentialType == CredentialType.SignedAssertion
                        || c.CredentialType == CredentialType.Certificate)
                        ? c.Id + "+bound"
                        : c.Id)
                ?? Enumerable.Empty<string>());

            var keyBuilder = new StringBuilder(
                DefaultTokenAcquirerFactoryImplementation.GetKey(mergedOptions.Authority, mergedOptions.ClientId, mergedOptions.AzureRegion));
            keyBuilder.Append(credentialId);
            if (isTokenBinding)
            {
                keyBuilder.Append("-tokenBinding");
            }

            if (agentAppId is not null)
            {
                keyBuilder.Append(":agent:");
                keyBuilder.Append(agentAppId);
            }
            return keyBuilder.ToString();
        }

        /// <summary>
        /// Typically used from a web app or web API controller, this method retrieves an access token
        /// for a downstream API using;
        /// 1) the token cache (for web apps and web APIs) if a token exists in the cache
        /// 2) or the <a href='https://learn.microsoft.com/azure/active-directory/develop/v2-oauth2-on-behalf-of-flow'>on-behalf-of flow</a>
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
        /// passing the validated token (as a JwtSecurityToken or JSonWebToken). Calling it from a web app supposes that
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
            return await GetAuthenticationResultForUserInternalAsync(
                scopes,
                authenticationScheme,
                tenantId,
                userFlow,
                user,
                tokenAcquisitionOptions,
                retryCount: 0).ConfigureAwait(false);
        }

        private async Task<AuthenticationResult> GetAuthenticationResultForUserInternalAsync(
            IEnumerable<string> scopes,
            string? authenticationScheme,
            string? tenantId,
            string? userFlow,
            ClaimsPrincipal? user,
            TokenAcquisitionOptions? tokenAcquisitionOptions,
            int retryCount)
        {
            _ = Throws.IfNull(scopes);

            MergedOptions mergedOptions = GetMergedOptions(authenticationScheme, tokenAcquisitionOptions);
            user ??= await _tokenAcquisitionHost.GetAuthenticatedUserAsync(user).ConfigureAwait(false);

            if (tokenAcquisitionOptions is not null)
            {
                tokenAcquisitionOptions.ExtraParameters ??= new Dictionary<string, object>();
                tokenAcquisitionOptions.ExtraParameters[Constants.ExtensionOptionsServiceProviderKey] = _serviceProvider;
            }

            // Detect agentic User FIC flow early — before building the blueprint CCA.
            // Agent CCAs are built via the unified builder path with their own ClientId,
            // so the blueprint CCA is only built lazily (inside the assertion callback)
            // when actually needed for Leg 1 token acquisition.
            var agentResult = await TryGetAuthenticationResultForAgentUserFicAsync(
                tenantId, scopes, mergedOptions, tokenAcquisitionOptions).ConfigureAwait(false);
            if (agentResult is not null)
            {
                LogAuthResult(agentResult);
                return agentResult;
            }

            var application = await GetOrBuildConfidentialClientApplicationAsync(mergedOptions, isTokenBinding: false);

            CredentialSourceLoaderParameters loaderParameters = new CredentialSourceLoaderParameters(application.AppConfig.ClientId, application.Authority)
            {
                Protocol = ProtocolNames.Bearer,
            };

            try
            {
                AuthenticationResult? authenticationResult;

                // If the user is not null and has claims xms-username and xms-password, perform ROPC for CCA
                authenticationResult = await TryGetAuthenticationResultForConfidentialClientUsingRopcAsync(
                    application,
                    tenantId,
                    scopes,
                    user,
                    mergedOptions,
                    tokenAcquisitionOptions).ConfigureAwait(false);

                if (authenticationResult != null)
                {
                    LogAuthResult(authenticationResult);
                    return authenticationResult;
                }

                // Access token will return if call is from a web API
                authenticationResult = await GetAuthenticationResultForWebApiToCallDownstreamApiAsync(
                    application,
                    tenantId,
                    scopes,
                    tokenAcquisitionOptions,
                    mergedOptions,
                    user).ConfigureAwait(false);

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
            catch (MsalServiceException exMsal) when (retryCount < MaxCertificateRetries && IsInvalidClientCertificateOrSignedAssertionError(exMsal))
            {
                Logger.TokenAcquisitionError(
                    _logger,
                    $"Certificate error detected. Retrying with next certificate (attempt {retryCount + 1}/{MaxCertificateRetries}). {exMsal.Message}",
                    exMsal);

                string applicationKey = GetApplicationKey(mergedOptions, isTokenBinding: false);
                NotifyCertificateSelection(loaderParameters, mergedOptions, application, false, exMsal);
                _applicationsByAuthorityClientId[applicationKey] = null;

                // Retry with incremented counter
                return await GetAuthenticationResultForUserInternalAsync(
                    scopes,
                    authenticationScheme,
                    tenantId,
                    userFlow,
                    user,
                    tokenAcquisitionOptions,
                    retryCount + 1).ConfigureAwait(false);
            }
            catch (MsalUiRequiredException ex)
            {
                // GetAccessTokenForUserAsync is an abstraction that can be called from a web app or a web API
                // MsalUiRequiredException is already logged by MSAL. Re-logging here would produce duplicates.
                // Case of the web app: we let the MsalUiRequiredException be caught by the
                // AuthorizeForScopesAttribute exception filter so that the user can consent, do 2FA, etc ...
                throw new MicrosoftIdentityWebChallengeUserException(ex, scopes.ToArray(), userFlow);
            }
        }

        // This method mutate the user claims to include claims uid and utid to perform the silent flow for subsequent calls.
        private async Task<AuthenticationResult?> TryGetAuthenticationResultForConfidentialClientUsingRopcAsync(
            IConfidentialClientApplication application,
            string? tenantId,
            IEnumerable<string> scopes,
            ClaimsPrincipal? user,
            MergedOptions mergedOptions,
            TokenAcquisitionOptions? tokenAcquisitionOptions)
        {
            string? username = null;
            string? password = null;

            // Case where the user is passed through the Claims identity
            if (user != null && user.HasClaim(c => c.Type == ClaimConstants.Username) && user.HasClaim(c => c.Type == ClaimConstants.Password))
            {
                username = user.FindFirst(ClaimConstants.Username)?.Value ?? string.Empty;
                password = user.FindFirst(ClaimConstants.Password)?.Value ?? string.Empty;
            }

            if (username == null)
            {
                return null;
            }

            bool forceRefresh = tokenAcquisitionOptions?.ForceRefresh ?? false;

            if (!forceRefresh && user != null && user.GetMsalAccountId() != null)
            {
                try
                {
                    var account = await application.GetAccountAsync(user.GetMsalAccountId()).ConfigureAwait(false);

                    // Silent flow
                    // Note: CachePartitionKeys from TokenAcquisitionOptions is not applied here.
                    // This path is used by ROPC, which does not support TokenAcquisitionOptions.
                    return await application.AcquireTokenSilent(
                        scopes.Except(_scopesRequestedByMsal),
                        account)
                        .ExecuteAsync()
                        .ConfigureAwait(false);
                }
                catch (MsalException ex)
                {
                    // Log a message when the silent flow fails and try acquisition through ROPC.
                    Logger.TokenAcquisitionError(_logger, ex.Message, ex);
                }

            }

            // Check for extension options for the ROPC flow
            TokenAcquisitionExtensionOptions? addInOptions = tokenAcquisitionExtensionOptionsMonitor?.CurrentValue;

            // ROPC flow
            AcquireTokenByUsernameAndPasswordConfidentialParameterBuilder builder = ((IByUsernameAndPassword)application)
                .AcquireTokenByUsernamePassword(
                    scopes.Except(_scopesRequestedByMsal),
                    username,
                    password);

            if (addInOptions != null)
            {
                await addInOptions.InvokeOnBeforeTokenAcquisitionForTestUserAsync(builder, tokenAcquisitionOptions, user!).ConfigureAwait(false);
            }

            builder.WithSendX5C(mergedOptions.SendX5C);

            // Pass the token acquisition options to the builder
            if (tokenAcquisitionOptions != null)
            {
                var dict = MergeExtraQueryParameters(mergedOptions, tokenAcquisitionOptions);
                if (dict != null)
                {
                    builder.WithExtraQueryParameters(dict);
                }

                if (tokenAcquisitionOptions.ExtraHeadersParameters != null)
                {
                    builder.WithExtraHttpHeaders(tokenAcquisitionOptions.ExtraHeadersParameters);
                }
                if (tokenAcquisitionOptions.CorrelationId != null)
                {
                    builder.WithCorrelationId(tokenAcquisitionOptions.CorrelationId.Value);
                }
                builder.WithClaims(tokenAcquisitionOptions.Claims);
                var clientClaims = GetClientClaimsIfExist(tokenAcquisitionOptions);
                if (clientClaims != null)
                {
                    builder.WithExtraClientAssertionClaims(clientClaims);
                }
                if (tokenAcquisitionOptions.PoPConfiguration != null)
                {
                    builder.WithSignedHttpRequestProofOfPossession(tokenAcquisitionOptions.PoPConfiguration);
                }

                if (!string.IsNullOrEmpty(tenantId))
                {
                    builder.WithTenantId(tenantId);
                }
            }

            var authenticationResult = await builder.ExecuteAsync().ConfigureAwait(false);

            if (user != null && user.GetMsalAccountId() == null)
            {
                // Add the account id to the user (in case of ROPC flow)
                user.AddIdentity(new CaseSensitiveClaimsIdentity(new[]
                {
                        new Claim(ClaimConstants.UniqueObjectIdentifier, authenticationResult.Account.HomeAccountId.ObjectId),
                        new Claim(ClaimConstants.UniqueTenantIdentifier, authenticationResult.Account.HomeAccountId.TenantId),
                    }));
            }

            return authenticationResult;
        }

        /// <summary>
        /// Handles agentic User FIC flow using MSAL's native
        /// AcquireTokenByUserFederatedIdentityCredential
        /// API (UPN overload for username-based flows, OID overload for user object ID flows).
        /// This replaces the ROPC piggybacking approach, providing proper token cache behavior
        /// via MSAL's built-in cache.
        ///
        /// The flow follows the multi-CCA pattern:
        ///   Leg 1: Blueprint CCA acquires FMI token (T1) for the agent — handled transparently
        ///          by the agent CCA's assertion callback (see <see cref="GetOrBuildAgentUserFicCcaAsync"/>).
        ///   Leg 2: Agent CCA acquires instance token (T2) via AcquireTokenForClient.
        ///   Leg 3: Agent CCA exchanges T2 + user identifier for a user-scoped token via native UserFIC.
        ///
        /// On subsequent calls, AcquireTokenSilent returns the cached token without network calls.
        /// Unlike other ID Web flows where the MSAL account identifier is stored in the
        /// ClaimsPrincipal (via oid/tid claims), the agentic scenario typically has a null or
        /// request-scoped ClaimsPrincipal — so account identifiers are tracked in
        /// <see cref="_agentUserFicAccountIds"/> instead.
        /// </summary>
        /// <returns>An <see cref="AuthenticationResult"/> if this is an agentic User FIC flow
        /// (UPN or OID); <c>null</c> if not an agentic flow (regular ROPC).</returns>
        private async Task<AuthenticationResult?> TryGetAuthenticationResultForAgentUserFicAsync(
            string? tenantId,
            IEnumerable<string> scopes,
            MergedOptions mergedOptions,
            TokenAcquisitionOptions? tokenAcquisitionOptions)
        {
            var extraParameters = tokenAcquisitionOptions?.ExtraParameters;

            // Detect agentic flow: requires AgentIdentityKey plus either UsernameKey (UPN) or UserIdKey (OID).
            if (extraParameters is null
                || !extraParameters.TryGetValue(Constants.AgentIdentityKey, out object? agentObj))
            {
                return null;
            }

            string? agentAppId = agentObj as string ?? agentObj?.ToString();
            if (string.IsNullOrEmpty(agentAppId))
            {
                return null;
            }

            // Determine user identifier: UPN takes precedence over OID (matching WithAgentUserIdentity behavior).
            string? username = null;
            Guid? userObjectId = null;
            string? userIdentifierForCacheKey = null;

            if (extraParameters.TryGetValue(Constants.UsernameKey, out object? usernameObj)
                && usernameObj is string upn && !string.IsNullOrEmpty(upn))
            {
                username = upn;
                userIdentifierForCacheKey = upn.ToUpperInvariant();
            }
            else if (extraParameters.TryGetValue(Constants.UserIdKey, out object? userIdObj)
                     && Guid.TryParse(userIdObj?.ToString(), out Guid parsedOid))
            {
                userObjectId = parsedOid;
                userIdentifierForCacheKey = parsedOid.ToString("D").ToUpperInvariant();
            }
            else
            {
                // Neither UPN nor valid OID — not a user FIC flow we can handle.
                return null;
            }

            string? authScheme = tokenAcquisitionOptions?.AuthenticationOptionsName;
            string identifierType = username is not null ? "UPN" : "OID";
            Logger.AgentUserFicFlowDetected(_logger, agentAppId!, identifierType);

            var agentCca = await GetOrBuildAgentUserFicCcaAsync(
                agentAppId!, authScheme, mergedOptions).ConfigureAwait(false);

            bool forceRefresh = tokenAcquisitionOptions?.ForceRefresh ?? false;

            // Try silent retrieval first using a stored account identifier from a prior call.
            // Include tenantId in the key so cross-tenant calls don't collide.
            // authenticationScheme is intentionally excluded: a given (agent, user, tenant)
            // tuple maps to a single MSAL account identity regardless of which auth scheme
            // was used. The CCA selected above is already scheme-specific, and GetAccountAsync
            // returns the same account from any CCA that shares the user's cache partition.
            string normalizedTenant = tenantId?.ToUpperInvariant() ?? string.Empty;
            string accountLookupKey = $"{agentAppId}:{userIdentifierForCacheKey}:{normalizedTenant}";
            if (!forceRefresh
                && _agentUserFicAccountIds.TryGetValue(accountLookupKey, out string? cachedAccountId)
                && !string.IsNullOrEmpty(cachedAccountId))
            {
                var account = await agentCca.GetAccountAsync(cachedAccountId).ConfigureAwait(false);
                if (account is not null)
                {
                    try
                    {
                        var silentBuilder = agentCca.AcquireTokenSilent(
                            scopes.Except(_scopesRequestedByMsal),
                            account);
                        if (!string.IsNullOrEmpty(tenantId))
                        {
                            silentBuilder.WithTenantId(tenantId);
                        }

                        var silentResult = await silentBuilder.ExecuteAsync().ConfigureAwait(false);
                        Logger.AgentUserFicSilentSuccess(_logger, agentAppId!, normalizedTenant);
                        return silentResult;
                    }
                    catch (MsalUiRequiredException ex)
                    {
                        // No cached token available — fall back to full 3-leg acquisition below.
                        Logger.AgentUserFicSilentFailure(_logger, agentAppId!, normalizedTenant, ex.ErrorCode ?? ex.GetType().Name, ex);
                    }
                }
                else
                {
                    // Account was evicted from MSAL's cache — remove stale mapping.
                    _agentUserFicAccountIds.TryRemove(accountLookupKey, out _);
                }
            }

            // Leg 2: Get the agent's instance token (T2).
            // The assertion callback handles Leg 1 (blueprint → T1) transparently.
            var leg2Builder = agentCca.AcquireTokenForClient(s_ficScopes);
            if (!string.IsNullOrEmpty(tenantId))
            {
                leg2Builder.WithTenantId(tenantId);
            }

            var leg2 = await leg2Builder.ExecuteAsync().ConfigureAwait(false);

            // Leg 3: Exchange T2 + user identifier for a user-scoped token via native UserFIC.
            // Uses the UPN overload when username is available, OID overload otherwise.
            AcquireTokenByUserFederatedIdentityCredentialParameterBuilder leg3Builder;
            if (username is not null)
            {
                leg3Builder = ((IByUserFederatedIdentityCredential)agentCca)
                    .AcquireTokenByUserFederatedIdentityCredential(
                        scopes.Except(_scopesRequestedByMsal),
                        username,
                        leg2.AccessToken);
            }
            else
            {
                leg3Builder = ((IByUserFederatedIdentityCredential)agentCca)
                    .AcquireTokenByUserFederatedIdentityCredential(
                        scopes.Except(_scopesRequestedByMsal),
                        userObjectId!.Value,
                        leg2.AccessToken);
            }

            if (!string.IsNullOrEmpty(tenantId))
            {
                leg3Builder.WithTenantId(tenantId);
            }

            var result = await leg3Builder.ExecuteAsync().ConfigureAwait(false);

            Logger.AgentUserFicAcquisitionComplete(_logger, agentAppId!, result.AuthenticationResultMetadata.TokenSource.ToString());
            // Store the account identifier for subsequent silent lookups.
            // In other ID Web flows, this is persisted in the ClaimsPrincipal (oid+tid claims).
            // Here, ClaimsPrincipal is unavailable, so we use _agentUserFicAccountIds instead.
            if (result.Account?.HomeAccountId is not null)
            {
                _agentUserFicAccountIds[accountLookupKey] = result.Account.HomeAccountId.Identifier;
            }

            return result;
        }

        /// <summary>
        /// Gets or builds an agent CCA for the native User FIC flow. Delegates to the unified
        /// <see cref="GetOrBuildConfidentialClientApplicationAsync"/> / 
        /// <see cref="BuildConfidentialClientApplicationAsync"/> builder path so that agent CCAs
        /// receive the same configuration as normal CCAs (logging, authority, cache initialization).
        /// Each agent CCA has a unique ClientId (the agent app ID), providing natural cache key
        /// isolation in both the CCA dictionary and MSAL's shared static token cache.
        /// </summary>
        private async Task<IConfidentialClientApplication> GetOrBuildAgentUserFicCcaAsync(
            string agentAppId,
            string? authenticationScheme,
            MergedOptions mergedOptions)
        {
            // Fast path: if the agent CCA is already cached, return it without
            // allocating a closure for the assertion callback. The callback captures
            // authenticationScheme and agentAppId, producing a heap-allocated closure
            // object + delegate on every call — wasteful when the CCA already exists.
            string key = GetApplicationKey(mergedOptions, isTokenBinding: false, agentAppId);
            if (_applicationsByAuthorityClientId.TryGetValue(key, out var cached) && cached != null)
            {
                return cached;
            }

            // Cache miss — build the assertion callback that chains to the blueprint CCA for Leg 1.
            // Capture authenticationScheme so the callback resolves the correct blueprint.
            string? capturedAuthScheme = authenticationScheme;

            Func<AssertionRequestOptions, Task<string>> assertionCallback = async (AssertionRequestOptions options) =>
            {
                // Leg 1: Blueprint acquires FMI token (T1) for this agent.
                // AcquireTokenForClient checks cache first — only the first call
                // or an expired T1 hits the network.
                MergedOptions blueprintOptions = _tokenAcquisitionHost.GetOptions(capturedAuthScheme, out _);
                var blueprintCca = await GetOrBuildConfidentialClientApplicationAsync(
                    blueprintOptions, isTokenBinding: false).ConfigureAwait(false);

                var leg1Builder = blueprintCca
                    .AcquireTokenForClient(s_ficScopes)
                    .WithFmiPath(agentAppId)
                    .WithSendX5C(blueprintOptions.SendX5C);

                // Propagate tenant override to Leg 1 when the caller specifies a tenant
                // (e.g., via WithTenantId on Leg 2/3). MSAL's AssertionRequestOptions
                // provides the resolved TenantId directly from the runtime authority.
                if (!string.IsNullOrEmpty(options.TenantId))
                {
                    leg1Builder.WithTenantId(options.TenantId);
                }

                var leg1 = await leg1Builder
                    .ExecuteAsync(options.CancellationToken)
                    .ConfigureAwait(false);

                return leg1.AccessToken;
            };

            // Delegate to the unified builder path. The agent app ID is incorporated into
            // the cache key automatically, and the CCA gets all the same configuration as
            // normal CCAs (logging, authority, cache initialization, etc.).
            var agentCca = await GetOrBuildConfidentialClientApplicationAsync(
                mergedOptions, isTokenBinding: false, agentAppId, assertionCallback).ConfigureAwait(false);

            Logger.AgentCcaCreated(_logger, agentAppId);
            return agentCca;
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
        /// Resolves the tenant based on if the tenant is already set or the TenantId configured
        /// in the options or the AppHomeTenantId if the TenantId is a meta tenant.
        /// </summary>
        /// <param name="tenant">Provided tenant or null if not provided</param>
        /// <param name="mergedOptions">Merged configuration from which to retrieve tenant value as necessary</param>
        /// <returns>Resolved tenant</returns>
        internal static string? ResolveTenant(string? tenant, MergedOptions mergedOptions)
        {
            if (string.IsNullOrEmpty(tenant))
            {
                tenant = mergedOptions.TenantId;
                if (!string.IsNullOrEmpty(tenant) && _metaTenantIdentifiers.Contains(tenant!) && !string.IsNullOrEmpty(mergedOptions.AppHomeTenantId))
                {
                    tenant = mergedOptions.AppHomeTenantId;
                }
            }

            if (!string.IsNullOrEmpty(tenant) && _metaTenantIdentifiers.Contains(tenant!))
            {
                throw new ArgumentException(IDWebErrorMessage.ClientCredentialTenantShouldBeTenanted, nameof(tenant));
            }

            return tenant;
        }

        /// <summary>
        /// Acquires an authentication result from the authority configured in the app, for the confidential client itself (not on behalf of a user)
        /// using either a client credentials or managed identity flow. See https://aka.ms/msal-net-client-credentials for client credentials or
        /// https://aka.ms/Entra/ManagedIdentityOverview for managed identity.
        /// </summary>
        /// <param name="scope">The scope requested to access a protected API. For these flows (client credentials or managed identity), the scope
        /// should be of the form "{ResourceIdUri/.default}" for instance <c>https://management.azure.net/.default</c> or, for Microsoft
        /// Graph, <c>https://graph.microsoft.com/.default</c> as the requested scopes are defined statically with the application registration
        /// in the portal, and cannot be overridden in the application, as you can request a token for only one resource at a time (use
        /// several calls to get tokens for other resources).</param>
        /// <param name="authenticationScheme">AuthenticationScheme to use.</param>
        /// <param name="tenant">Enables overriding of the tenant/account for the same identity. This is useful
        /// for multi tenant apps or daemons.</param>
        /// <param name="tokenAcquisitionOptions">Options passed-in to create the token acquisition object which calls into MSAL .NET.</param>
        /// <returns>An authentication result for the app itself, based on its scopes.</returns>
        public async Task<AuthenticationResult> GetAuthenticationResultForAppAsync(
            string scope,
            string? authenticationScheme = null,
            string? tenant = null,
            TokenAcquisitionOptions? tokenAcquisitionOptions = null)
        {
            return await GetAuthenticationResultForAppInternalAsync(
                scope,
                authenticationScheme,
                tenant,
                tokenAcquisitionOptions,
                retryCount: 0).ConfigureAwait(false);
        }

        private async Task<AuthenticationResult> GetAuthenticationResultForAppInternalAsync(
            string scope,
            string? authenticationScheme,
            string? tenant,
            TokenAcquisitionOptions? tokenAcquisitionOptions,
            int retryCount)
        {
            _ = Throws.IfNull(scope);

            if (!scope.EndsWith("/.default", true, CultureInfo.InvariantCulture))
            {
                throw new ArgumentException(IDWebErrorMessage.ClientCredentialScopeParameterShouldEndInDotDefault, nameof(scope));
            }

            MergedOptions mergedOptions = GetMergedOptions(authenticationScheme, tokenAcquisitionOptions);

            bool isTokenBinding = tokenAcquisitionOptions?.ExtraParameters?.TryGetValue(TokenBindingParameterName, out var isTokenBindingObject) == true
                && isTokenBindingObject is bool isTokenBindingValue
                && isTokenBindingValue;

            // If using managed identity 
            if (tokenAcquisitionOptions != null && tokenAcquisitionOptions.ManagedIdentity != null)
            {
                try
                {
                    IManagedIdentityApplication managedIdApp = await GetOrBuildManagedIdentityApplicationAsync(
                        mergedOptions,
                        tokenAcquisitionOptions.ManagedIdentity
                    );

                    var miBuilder = managedIdApp.AcquireTokenForManagedIdentity(scope);

                    if (isTokenBinding)
                    {
                        miBuilder = miBuilder.WithMtlsProofOfPossession();
#if NETCOREAPP
                        // Key attestation is only available on modern .NET (issue #3894).
                        miBuilder = miBuilder.WithAttestationSupport();
#endif
                    }

                    if (!string.IsNullOrEmpty(tokenAcquisitionOptions.Claims))
                    {
                        miBuilder.WithClaims(tokenAcquisitionOptions.Claims);
                    }

                    //TODO: Should client assertion claims be supported for managed identity?
                    //var clientClaims = GetClientClaimsIfExist(tokenAcquisitionOptions);
                    //if (clientClaims != null)
                    //{
                    //    miBuilder.WithExtraClientAssertionClaims(clientClaims);
                    //}

                    return await miBuilder.ExecuteAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Logger.TokenAcquisitionError(_logger, ex.Message, ex);
                    throw;
                }
            }

            // For non-managed identity flows we only resolve tenant if the caller explicitly provided an override.
            // This preserves the ability to use an authority-only configuration with meta-tenants like 'common'.
            string? resolvedOverrideTenant = tenant != null ? ResolveTenant(tenant, mergedOptions) : null;

            if (tokenAcquisitionOptions is not null)
            {
                tokenAcquisitionOptions.ExtraParameters ??= new Dictionary<string, object>();
                tokenAcquisitionOptions.ExtraParameters[Constants.ExtensionOptionsServiceProviderKey] = _serviceProvider;
            }

            TokenAcquisitionExtensionOptions? addInOptions = tokenAcquisitionExtensionOptionsMonitor?.CurrentValue;

            // Use MSAL to get the right token to call the API
            var application = await GetOrBuildConfidentialClientApplicationAsync(mergedOptions, isTokenBinding);

            AcquireTokenForClientParameterBuilder builder = application
                   .AcquireTokenForClient(new[] { scope }.Except(_scopesRequestedByMsal))
                   .WithSendX5C(mergedOptions.SendX5C);

            if (isTokenBinding)
            {
                builder.WithMtlsProofOfPossession();
            }

            if (addInOptions != null)
            {
                addInOptions.InvokeOnBeforeTokenAcquisitionForApp(builder, tokenAcquisitionOptions);
            }

            // Apply tenant override only for AAD authorities and only if non-empty
            if (!string.IsNullOrEmpty(mergedOptions.Instance) && 
                !mergedOptions.Instance.Contains(Constants.CiamAuthoritySuffix
#if NET6_0_OR_GREATER
                , StringComparison.OrdinalIgnoreCase
#endif
                ) && !string.IsNullOrEmpty(resolvedOverrideTenant))
            {
                builder.WithTenantId(resolvedOverrideTenant);
            }

            if (tokenAcquisitionOptions != null)
            {
                // Check for client assertion in the extra parameters
                bool optionsHaveClientAssertion = OverrideClientAssertionIfNeeded(tokenAcquisitionOptions, builder);

                // Only add FMI path for signed assertion if we're not using client assertion
                if (!optionsHaveClientAssertion)
                {
                    AddFmiPathForSignedAssertionIfNeeded(tokenAcquisitionOptions, builder);
                }

                var dict = MergeExtraQueryParameters(mergedOptions, tokenAcquisitionOptions);

                if (dict != null)
                {
                    builder.WithExtraQueryParameters(dict);
                }
                if (tokenAcquisitionOptions.ExtraHeadersParameters != null)
                {
                    builder.WithExtraHttpHeaders(tokenAcquisitionOptions.ExtraHeadersParameters);
                }

                AddExtraBodyParametersIfNeeded(tokenAcquisitionOptions, builder);

                // Extra Parameters are not meant to be used by Token but by extensions

                if (tokenAcquisitionOptions.CorrelationId != null)
                {
                    builder.WithCorrelationId(tokenAcquisitionOptions.CorrelationId.Value);
                }
                builder.WithForceRefresh(tokenAcquisitionOptions.ForceRefresh);
                builder.WithClaims(tokenAcquisitionOptions.Claims);

                var clientClaims = GetClientClaimsIfExist(tokenAcquisitionOptions);
                if (clientClaims != null)
                {
                    builder.WithExtraClientAssertionClaims(clientClaims);
                }

                if (!string.IsNullOrEmpty(tokenAcquisitionOptions.FmiPath))
                {
                    builder.WithFmiPath(tokenAcquisitionOptions.FmiPath);
                }
                if (tokenAcquisitionOptions.PoPConfiguration != null)
                {
                    builder.WithSignedHttpRequestProofOfPossession(tokenAcquisitionOptions.PoPConfiguration);
                }
                if (!string.IsNullOrEmpty(tokenAcquisitionOptions.PopPublicKey))
                {
                    _logger.LogInformation("Regular SHR POP with server nonce configured");

                    if (string.IsNullOrEmpty(tokenAcquisitionOptions.PopClaim))
                    {
                        builder.WithProofOfPosessionKeyId(tokenAcquisitionOptions.PopPublicKey, "pop");
                        builder.OnBeforeTokenRequest((data) =>
                        {
                            data.BodyParameters.Add("req_cnf", tokenAcquisitionOptions.PopPublicKey);
                            data.BodyParameters.Add("token_type", "pop");
                            return Task.CompletedTask;
                        });
                    }
                    else
                    {
                        if (mergedOptions.SendX5C)
                        {
                            _logger.LogInformation("MSAuth POP configured with SN/I");
                        }
                        else
                        {
                            _logger.LogWarning("MSAuth POP configured with pinned certificate. This configuration is being deprecated.");
                        }

                        builder.WithAtPop(
                           tokenAcquisitionOptions.PopPublicKey!,
                           tokenAcquisitionOptions.PopClaim!);
                    }
                }
            }

            try
            {
                var result = await builder.ExecuteAsync(tokenAcquisitionOptions != null ? tokenAcquisitionOptions.CancellationToken : CancellationToken.None);
                NotifyCertificateSelection(
                    new CredentialSourceLoaderParameters(
                        mergedOptions.ClientId ?? string.Empty,
                        mergedOptions.Authority ?? string.Empty)
                    {
                        Protocol = isTokenBinding ? ProtocolNames.MtlsPop : ProtocolNames.Bearer,  
                    },
                    mergedOptions,
                    application,
                    true,
                    null);
                return result;
            }
            catch (MsalServiceException exMsal) when (retryCount < MaxCertificateRetries && IsInvalidClientCertificateOrSignedAssertionError(exMsal))
            {
                Logger.TokenAcquisitionError(
                    _logger,
                    $"Certificate error detected. Retrying with next certificate (attempt {retryCount + 1}/{MaxCertificateRetries}). {exMsal.Message}",
                    exMsal);

                // Invalidate the cache entry for the ACTUAL request mode (bearer vs mTLS PoP),
                // so a failed token-binding acquisition invalidates the token-binding CCA key
                // rather than always the Bearer key.
                string applicationKey = GetApplicationKey(mergedOptions, isTokenBinding);
                NotifyCertificateSelection(
                    new CredentialSourceLoaderParameters(
                        mergedOptions.ClientId ?? string.Empty,
                        mergedOptions.Authority ?? string.Empty)
                    {
                        Protocol = isTokenBinding ? ProtocolNames.MtlsPop : ProtocolNames.Bearer,
                    },
                    mergedOptions,
                    application,
                    false,
                    exMsal);
                _applicationsByAuthorityClientId[applicationKey] = null;

                // Retry with incremented counter
                return await GetAuthenticationResultForAppInternalAsync(
                    scope,
                    authenticationScheme,
                    tenant,
                    tokenAcquisitionOptions,
                    retryCount + 1);
            }
            catch (MsalException ex)
            {
                // GetAuthenticationResultForAppAsync is an abstraction that can be called from
                // a web app or a web API
                Logger.TokenAcquisitionError(_logger, ex.Message, ex);
                throw;
            }
        }

        private void AddExtraBodyParametersIfNeeded(TokenAcquisitionOptions tokenAcquisitionOptions, AcquireTokenForClientParameterBuilder builder)
        {
            if (tokenAcquisitionOptions.ExtraParameters != null
                && tokenAcquisitionOptions.ExtraParameters.TryGetValue(Constants.ExtraBodyParametersKey, out object? parameters))
            {
                if (parameters is Dictionary<string, Func<CancellationToken, Task<string>>> keyValuePairs)
                {
                    AbstractConfidentialClientAcquireTokenParameterBuilderExtension.WithExtraBodyParameters(builder, keyValuePairs);
                }
            }
        }

        private MergedOptions GetMergedOptions(string? authenticationScheme, TokenAcquisitionOptions? tokenAcquisitionOptions)
        {
            MergedOptions mergedOptions;

            if (tokenAcquisitionOptions != null
                && tokenAcquisitionOptions.ExtraParameters != null
                && tokenAcquisitionOptions.ExtraParameters.TryGetValue(Constants.MicrosoftIdentityOptionsParameter, out object? identityOptions)
                && identityOptions is MicrosoftEntraApplicationOptions microsoftEntraApplicationOptions)
            {
                MergedOptions parentMergedOptions = _tokenAcquisitionHost.GetOptions(authenticationScheme ?? tokenAcquisitionOptions?.AuthenticationOptionsName, out _);
                mergedOptions = new MergedOptions()
                {
                    ClientId = microsoftEntraApplicationOptions.ClientId ?? parentMergedOptions.ClientId,
                    Authority = microsoftEntraApplicationOptions.Authority != "//v2.0" ? microsoftEntraApplicationOptions.Authority : parentMergedOptions.Authority,
                    ClientCredentials = microsoftEntraApplicationOptions.ClientCredentials ?? parentMergedOptions.ClientCredentials,
                    SendX5C = microsoftEntraApplicationOptions.SendX5C,
                    Instance = microsoftEntraApplicationOptions.Instance ?? parentMergedOptions.Instance,
                    AzureRegion = microsoftEntraApplicationOptions.AzureRegion ?? parentMergedOptions.AzureRegion,
                    TenantId = microsoftEntraApplicationOptions.TenantId ?? parentMergedOptions.TenantId,
                };
            }
            else
            {
                mergedOptions = _tokenAcquisitionHost.GetOptions(authenticationScheme ?? tokenAcquisitionOptions?.AuthenticationOptionsName, out _);
            }

            return mergedOptions;
        }

        private static void AddFmiPathForSignedAssertionIfNeeded(TokenAcquisitionOptions tokenAcquisitionOptions, AcquireTokenForClientParameterBuilder builder)
        {
            if (tokenAcquisitionOptions.ExtraParameters != null)
            {
                if (tokenAcquisitionOptions.ExtraParameters.TryGetValue(Constants.FmiPathForClientAssertion, out object? o))
                {
                    if (o is string fmiPathForClientAssertion && !string.IsNullOrEmpty(fmiPathForClientAssertion))
                    {
                        builder.WithFmiPathForClientAssertion(fmiPathForClientAssertion);
                    }
                }
            }
        }

        private static void AddFmiPathForSignedAssertionIfNeeded(TokenAcquisitionOptions tokenAcquisitionOptions, AcquireTokenOnBehalfOfParameterBuilder builder)
        {
            if (tokenAcquisitionOptions.ExtraParameters != null)
            {
                if (tokenAcquisitionOptions.ExtraParameters.TryGetValue(Constants.FmiPathForClientAssertion, out object? o))
                {
                    if (o is string fmiPathForClientAssertion && !string.IsNullOrEmpty(fmiPathForClientAssertion))
                    {
                        builder.WithFmiPathForClientAssertion(fmiPathForClientAssertion);
                    }
                }
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
        /// 2) or the <a href='https://learn.microsoft.com/azure/active-directory/develop/v2-oauth2-on-behalf-of-flow'>on-behalf-of flow</a>
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
        /// passing the validated token (as a JwtSecurityToken or JSonWebToken). Calling it from a web app supposes that
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
            string? authenticationScheme = null)
        {
            string? userId = user.GetMsalAccountId();
            if (!string.IsNullOrEmpty(userId))
            {
                MergedOptions mergedOptions = _tokenAcquisitionHost.GetOptions(authenticationScheme, out _);

                IConfidentialClientApplication app = await GetOrBuildConfidentialClientApplicationAsync(mergedOptions, isTokenBinding: false);

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

        private bool IsInvalidClientCertificateOrSignedAssertionError(MsalServiceException exMsal)
        {
            // Only check invalid_client errors
            if (!string.Equals(exMsal.ErrorCode, Constants.InvalidClient, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string responseBody = exMsal.ResponseBody;

#if NET6_0_OR_GREATER
            foreach (var errorCode in Constants.CertificateAuthFailureStsErrorCodes)
            {
                if (responseBody.Contains(errorCode, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
#else
            foreach (var errorCode in Constants.CertificateAuthFailureStsErrorCodes)
            {
                if (responseBody.IndexOf(errorCode, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }
            return false;
#endif
        }

        private static string? GetClientClaimsIfExist(TokenAcquisitionOptions? tokenAcquisitionOptions)
        {
            string? clientClaims = null;
            if (tokenAcquisitionOptions is not null && tokenAcquisitionOptions.ExtraParameters is not null &&
                tokenAcquisitionOptions.ExtraParameters.ContainsKey("IDWEB_CLIENT_ASSERTION_CLAIMS"))
            {
                clientClaims = tokenAcquisitionOptions.ExtraParameters["IDWEB_CLIENT_ASSERTION_CLAIMS"] as string;
            }
            return clientClaims;
        }

        /// <inheritdoc/>
        public async Task<IConfidentialClientApplication> GetConfidentialClientApplicationAsync(
            string? authenticationScheme = null)
        {
            MergedOptions mergedOptions = _tokenAcquisitionHost.GetOptions(authenticationScheme, out _);
            return await GetOrBuildConfidentialClientApplicationAsync(mergedOptions, isTokenBinding: false)
                .ConfigureAwait(false);
        }

        internal /* for testing */ async Task<IConfidentialClientApplication> GetOrBuildConfidentialClientApplicationAsync(
            MergedOptions mergedOptions,
            bool isTokenBinding,
            string? agentAppId = null,
            Func<AssertionRequestOptions, Task<string>>? agenticAssertionProvider = null)
        {
            string key = GetApplicationKey(mergedOptions, isTokenBinding, agentAppId);

            // GetOrAddAsync based on https://github.com/dotnet/runtime/issues/83636#issuecomment-1474998680
            // Fast path: check if already created
            if (_applicationsByAuthorityClientId.TryGetValue(key, out var existingApp) && existingApp != null)
                return existingApp;

            // Get or create a semaphore for this specific key
            var semaphore = _appSemaphores.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));

            await semaphore.WaitAsync();
            try
            {
                // Double-check after acquiring the lock
                if (_applicationsByAuthorityClientId.TryGetValue(key, out var app) && app != null)
                    return app;

                // Build and store the application
                var newApp = await BuildConfidentialClientApplicationAsync(
                    mergedOptions, isTokenBinding, agentAppId, agenticAssertionProvider);

                // Recompute the key as BuildConfidentialClientApplicationAsync can cause it to change.
                key = GetApplicationKey(mergedOptions, isTokenBinding, agentAppId);
                _applicationsByAuthorityClientId[key] = newApp;

                // DOS protection: if the dictionary grows beyond the threshold, clear it.
                // All token data lives in external caches (MSAL's shared static cache for
                // in-memory providers, or the distributed cache provider for Redis/SQL/etc.),
                // so clearing the dictionary only discards lightweight CCA objects — tokens
                // remain accessible to newly-built CCAs.
                if (agentAppId is not null && _applicationsByAuthorityClientId.Count > AgentCcaMaxCount)
                {
                    int cleared = _applicationsByAuthorityClientId.Count;
                    _applicationsByAuthorityClientId.Clear();
                    _appSemaphores.Clear();
                    _agentUserFicAccountIds.Clear();
                    Logger.AgentCcaEviction(_logger, cleared);
                }

                return newApp;
            }
            finally
            {
                semaphore.Release();
            }
        }

        /// <summary>
        /// Creates an MSAL confidential client application.
        /// </summary>
        /// <param name="mergedOptions">Merged configuration options.</param>
        /// <param name="isTokenBinding">Whether mTLS token binding (PoP) is requested.</param>
        /// <param name="agentAppId">When non-null, builds an agent CCA with this app ID as
        /// the ClientId and uses <paramref name="agenticAssertionProvider"/> for credentials
        /// instead of the normal client credentials. The rest of the builder configuration
        /// (logging, authority, redirect URI, cache initialization) is shared with the normal
        /// CCA builder path.</param>
        /// <param name="agenticAssertionProvider">Assertion callback for agent CCAs. Required
        /// when <paramref name="agentAppId"/> is non-null.</param>
        private async Task<IConfidentialClientApplication> BuildConfidentialClientApplicationAsync(
            MergedOptions mergedOptions,
            bool isTokenBinding,
            string? agentAppId = null,
            Func<AssertionRequestOptions, Task<string>>? agenticAssertionProvider = null)
        {
            // agentAppId and agenticAssertionProvider must both be null or both be non-null.
            // Agent CCAs require an assertion callback for Leg 1 (FMI token), and the callback
            // is only meaningful in the context of an agent CCA.
            if ((agentAppId is null) != (agenticAssertionProvider is null))
            {
                throw new ArgumentException(
                    "agentAppId and agenticAssertionProvider must both be provided or both be null.");
            }

            bool isAgentCca = agentAppId is not null;

            mergedOptions.PrepareAuthorityInstanceForMsal();

            // Validate that we have enough configuration to build an authority
            // When PreserveAuthority is true, we use Authority directly, so PreparedInstance is not required
            // When IsB2C is true, we still need PreparedInstance
            if (!mergedOptions.PreserveAuthority && 
                string.IsNullOrEmpty(mergedOptions.PreparedInstance) && 
                string.IsNullOrEmpty(mergedOptions.Authority))
            {
                throw new ArgumentException(IDWebErrorMessage.MissingIdentityConfiguration);
            }

            try
            {
                // For agent CCAs, create a fresh ConfidentialClientApplicationOptions with
                // the agent's ClientId. This avoids mutating the cached MergedOptions instance
                // that the blueprint CCA depends on.
                ConfidentialClientApplicationOptions ccaOptions;
                if (isAgentCca)
                {
                    ccaOptions = new ConfidentialClientApplicationOptions();
                    MergedOptions.UpdateConfidentialClientApplicationOptionsFromMergedOptions(mergedOptions, ccaOptions);
                    ccaOptions.ClientId = agentAppId;
                }
                else
                {
                    ccaOptions = mergedOptions.ConfidentialClientApplicationOptions;
                }

                ConfidentialClientApplicationBuilder builder = ConfidentialClientApplicationBuilder
                        .CreateWithApplicationOptions(ccaOptions)
                        .WithHttpClientFactory(_httpClientFactory)
                        .WithLogging(
                            new IdentityLoggerAdapter(_logger),
                            enablePiiLogging: ccaOptions.EnablePiiLogging)
                        .WithExperimentalFeatures();

                // Agent CCAs always use the shared (static) internal cache: tokens survive CCA
                // eviction and are found by newly-built CCAs via AcquireTokenSilent. Developers can
                // also opt into the fast, unbounded static cache for the in-memory provider via
                // MicrosoftIdentityOptions.UseFastUnboundedCache (settable through configuration).
                // MSAL forbids combining the internal shared cache with external serialization, so
                // when it is enabled we must NOT initialize the serialization provider below.
                bool usesSharedInternalCache =
                    isAgentCca ||
                    (mergedOptions.UseFastUnboundedCache &&
                     _tokenCacheProvider is MsalMemoryTokenCacheProvider);
                if (usesSharedInternalCache)
                {
                    builder.WithCacheOptions(CacheOptions.EnableSharedCacheOptions);
                }

                string? currentUri = _tokenAcquisitionHost.GetCurrentRedirectUri(mergedOptions);

                // The redirect URI is not needed for OBO or agent flows
                if (!string.IsNullOrEmpty(currentUri) && !isAgentCca)
                {
                    builder.WithRedirectUri(currentUri);
                }

                // ClientCapabilities are applied once during CCA construction
                // (see UpdateConfidentialClientApplicationOptionsFromMergedOptions).
                // We rely on that path. if it ever regresses the unit test
                // (CrossCloudFicUnitTest) will fail.

                string authority;

                if (mergedOptions.PreserveAuthority && !string.IsNullOrEmpty(mergedOptions.Authority))
                {
                    authority = mergedOptions.Authority!;
                    builder.WithOidcAuthority(authority);
                }
                else if (mergedOptions.IsB2C)
                {
                    // B2C authority construction requires the tenant segment. If Domain was not configured
                    // (scenario: authority-only configuration providing Instance + SignUpSignInPolicyId), derive it.
                    string? domain = mergedOptions.Domain;
                    if (string.IsNullOrEmpty(domain))
                    {
                        // Try tenantId first if provided
                        if (!string.IsNullOrEmpty(mergedOptions.TenantId))
                        {
                            domain = mergedOptions.TenantId;
                        }
                        else if (!string.IsNullOrEmpty(mergedOptions.Instance))
                        {
                            try
                            {
                                // Extract first label from host (e.g. fabrikamb2c from fabrikamb2c.b2clogin.com)
                                var host = new Uri(mergedOptions.Instance).Host;
                                var firstLabel = host.Split('.').FirstOrDefault();
                                if (!string.IsNullOrEmpty(firstLabel))
                                {
                                    domain = firstLabel + ".onmicrosoft.com";
                                }
                            }
                            catch
                            {
                                // Ignore derivation failures; will throw below if still null.
                            }
                        }
                    }

                    if (string.IsNullOrEmpty(domain))
                    {
                        throw new ArgumentException("B2C Domain could not be determined. Provide Domain or TenantId when using B2C authority-only configuration.");
                    }

                    authority = $"{mergedOptions.PreparedInstance}{ClaimConstants.Tfp}/{domain}/{mergedOptions.DefaultUserFlow}";
                    builder.WithB2CAuthority(authority);
                }
                else
                {
                    authority = $"{mergedOptions.PreparedInstance}{mergedOptions.TenantId}/";
                    builder.WithAuthority(authority);
                }

                // Configure credentials: agent CCAs use an assertion callback that chains
                // to the blueprint CCA for Leg 1 (FMI token), while normal CCAs use the
                // standard client credentials (certificate, secret, etc.).
                if (isAgentCca && agenticAssertionProvider is not null)
                {
                    builder.WithClientAssertion(agenticAssertionProvider);
                }
                else
                {
                    await builder.WithClientCredentialsAsync(
                        mergedOptions,
                        _credentialsProvider,
                        new CredentialSourceLoaderParameters(mergedOptions.ClientId!, authority)
                        {
                            Protocol = isTokenBinding ? ProtocolNames.MtlsPop : ProtocolNames.Bearer,
                        },
                        isTokenBinding);
                }

                IConfidentialClientApplication app = builder.Build();

                // Initialize the token cache provider so its serialization callbacks (including the
                // GetSuggestedCacheKey partitioning hook and per-entry expiry) are wired for ALL
                // providers, including MsalMemoryTokenCacheProvider. Previously the in-memory
                // provider was short-circuited to MSAL's opaque static cache, which disabled that
                // hook. We skip this only when the shared internal cache is enabled (agent CCAs or
                // the UseFastUnboundedCache opt-in), because MSAL forbids combining internal caching
                // with external serialization.
                if (!usesSharedInternalCache)
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
                    IDWebErrorMessage.ExceptionAcquiringTokenForConfidentialClient + ex.Message,
                    ex);
                throw;
            }
        }

        /// <summary>
        /// Find the certificate used by the app and fire the event to notify the client app that a certificate was selected/unselected.
        /// </summary>
        /// <param name="sourceLoaderParameters">The source loader parameters.</param>
        /// <param name="mergedOptions">The merged options object.</param>
        /// <param name="app">The confidential app.</param>
        /// <param name="successful">Whether this was successful or not.</param>
        /// <param name="exception">The thrown exception, if any.</param>
        private void NotifyCertificateSelection(
            CredentialSourceLoaderParameters? sourceLoaderParameters,
            MergedOptions mergedOptions,
            IConfidentialClientApplication app,
            bool successful,
            Exception? exception)
        {
            X509Certificate2 selectedCertificate = app.AppConfig.ClientCredentialCertificate;
            CredentialDescription? description = mergedOptions.ClientCredentials?.FirstOrDefault(c => c.Certificate == selectedCertificate);
            if (selectedCertificate != null && description != null)
            {
                _credentialsProvider.NotifyCertificateUsed(
                    sourceLoaderParameters,
                    description,
                    selectedCertificate,
                    successful,
                    exception);
            }
        }

        private async ValueTask<AuthenticationResult?> GetAuthenticationResultForWebApiToCallDownstreamApiAsync(
           IConfidentialClientApplication application,
           string? tenantId,
           IEnumerable<string> scopes,
           TokenAcquisitionOptions? tokenAcquisitionOptions,
           MergedOptions mergedOptions,
           ClaimsPrincipal? userHint)
        {
            // In web API, validatedToken will not be null
            SecurityToken? validatedToken = userHint?.GetBootstrapToken() ?? _tokenAcquisitionHost.GetTokenUsedToCallWebAPI();

            // In the case the token is a JWE (encrypted token), we use the decrypted token.
            string? tokenUsedToCallTheWebApi = GetActualToken(validatedToken);
            string? originalTokenToCallWebApi = tokenUsedToCallTheWebApi;

            AcquireTokenOnBehalfOfParameterBuilder? builder = null;
            TokenAcquisitionExtensionOptions? addInOptions = tokenAcquisitionExtensionOptionsMonitor?.CurrentValue;

            // Case of web APIs: we need to do an on-behalf-of flow, with the token used to call the API
            if (tokenUsedToCallTheWebApi != null)
            {
                if (addInOptions != null && addInOptions.InvokeOnBeforeOnBehalfOfInitializedAsync != null)
                {
                    var oboInitEventArgs = new OnBehalfOfEventArgs
                    {
                        UserAssertionToken = tokenUsedToCallTheWebApi,
                        User = userHint
                    };
                    await addInOptions.InvokeOnBeforeOnBehalfOfInitializedAsync(oboInitEventArgs).ConfigureAwait(false);

                    if (oboInitEventArgs.UserAssertionToken != null)
                    {
                        tokenUsedToCallTheWebApi = oboInitEventArgs.UserAssertionToken;
                    }
                }

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
                    if (sessionKey == Abstractions.AcquireTokenOptions.LongRunningWebApiSessionKeyAuto)
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

                ClaimsPrincipal? userForCcsRouting = _tokenAcquisitionHost.GetUserFromRequest();
                var userTenant = string.Empty;
                if (userForCcsRouting != null)
                {
                    userTenant = userForCcsRouting.GetTenantId();
                    builder.WithCcsRoutingHint(userForCcsRouting.GetObjectId(), userTenant);
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
                    if (addInOptions != null && addInOptions.InvokeOnBeforeTokenAcquisitionForOnBehalfOfAsync != null)
                    {
                        var eventArgs = new OnBehalfOfEventArgs
                        {
                            User = userHint,
                            UserAssertionToken = originalTokenToCallWebApi
                        };

                        await addInOptions.InvokeOnBeforeTokenAcquisitionForOnBehalfOfAsync(builder, tokenAcquisitionOptions, eventArgs).ConfigureAwait(false);
                    }

                    AddFmiPathForSignedAssertionIfNeeded(tokenAcquisitionOptions, builder);

                    var dict = MergeExtraQueryParameters(mergedOptions, tokenAcquisitionOptions);
                    if (dict != null)
                    {
                        const string assertionConstant = "assertion";
                        const string subAssertionConstant = "sub_assertion";

                        // Special case when the OBO inbound token is composite (for instance PFT)
                        if (dict.ContainsKey(assertionConstant) && dict.ContainsKey(subAssertionConstant))
                        {
                            string assertion = dict[assertionConstant].value;
                            string subAssertion = dict[subAssertionConstant].value;

                            // Check assertion and sub_assertion passed from merging extra query parameters to ensure they do not contain unsupported character(s).
                            CheckAssertionsForInjectionAttempt(assertion, subAssertion);

                            builder.OnBeforeTokenRequest((data) =>
                            {
                                // Replace the assertion and adds sub_assertion with the values from the extra query parameters
                                data.BodyParameters[assertionConstant] = assertion;
                                data.BodyParameters.Add(subAssertionConstant, subAssertion);
                                return Task.CompletedTask;
                            });

                            // Remove the assertion and sub_assertion from the extra query parameters
                            // as they are already handled as body parameters.
                            dict.Remove(assertionConstant);
                            dict.Remove(subAssertionConstant);
                        }

                        builder.WithExtraQueryParameters(dict);
                    }
                    if (tokenAcquisitionOptions.ExtraHeadersParameters != null)
                    {
                        builder.WithExtraHttpHeaders(tokenAcquisitionOptions.ExtraHeadersParameters);
                    }
                    if (tokenAcquisitionOptions.CorrelationId != null)
                    {
                        builder.WithCorrelationId(tokenAcquisitionOptions.CorrelationId.Value);
                    }
                    builder.WithForceRefresh(tokenAcquisitionOptions.ForceRefresh);
                    builder.WithClaims(tokenAcquisitionOptions.Claims);
                    var clientClaims = GetClientClaimsIfExist(tokenAcquisitionOptions);
                    if (clientClaims != null)
                    {
                        builder.WithExtraClientAssertionClaims(clientClaims);
                    }
                    if (tokenAcquisitionOptions.PoPConfiguration != null)
                    {
                        builder.WithSignedHttpRequestProofOfPossession(tokenAcquisitionOptions.PoPConfiguration);
                    }
                }

                return await builder.ExecuteAsync(tokenAcquisitionOptions != null ? tokenAcquisitionOptions.CancellationToken : CancellationToken.None)
                                    .ConfigureAwait(false);
            }

            return null;
        }

        /// <summary>
        /// Checks assertion and sub_assertion passed from merging extra query parameters to ensure they do not contain unsupported characters.
        /// </summary>
        /// <param name="assertion">The assertion.</param>
        /// <param name="subAssertion">The sub_assertion.</param>
        private static void CheckAssertionsForInjectionAttempt(string assertion, string subAssertion)
        {
            if (string.IsNullOrEmpty(assertion) || string.IsNullOrEmpty(subAssertion))
            {
#if NETSTANDARD2_0 || NET462 || NET472
                if (string.IsNullOrEmpty(assertion) && assertion.Contains('&')) throw new ArgumentException(IDWebErrorMessage.InvalidAssertion, nameof(assertion));
                if (string.IsNullOrEmpty(subAssertion) && subAssertion.Contains('&')) throw new ArgumentException(IDWebErrorMessage.InvalidSubAssertion, nameof(subAssertion));
#else
                if (string.IsNullOrEmpty(assertion) && assertion.Contains('&', StringComparison.InvariantCultureIgnoreCase))
                    throw new ArgumentException(IDWebErrorMessage.InvalidAssertion, nameof(assertion));
                if (!string.IsNullOrEmpty(subAssertion) && subAssertion.Contains('&', StringComparison.InvariantCultureIgnoreCase))
                    throw new ArgumentException(IDWebErrorMessage.InvalidSubAssertion, nameof(subAssertion));
#endif
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
        private async ValueTask<AuthenticationResult> GetAuthenticationResultForWebAppWithAccountFromCacheAsync(
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
            _ = Throws.IfNull(scopes);

            var builder = application
                    .AcquireTokenSilent(scopes.Except(_scopesRequestedByMsal), account)
                    .WithSendX5C(mergedOptions.SendX5C);

            if (tokenAcquisitionOptions != null)
            {
                var dict = MergeExtraQueryParameters(mergedOptions, tokenAcquisitionOptions);

                if (dict != null)
                {
                    builder.WithExtraQueryParameters(dict);
                }
                if (tokenAcquisitionOptions.ExtraHeadersParameters != null)
                {
                    builder.WithExtraHttpHeaders(tokenAcquisitionOptions.ExtraHeadersParameters);
                }
                if (tokenAcquisitionOptions.CorrelationId != null)
                {
                    builder.WithCorrelationId(tokenAcquisitionOptions.CorrelationId.Value);
                }
                builder.WithForceRefresh(tokenAcquisitionOptions.ForceRefresh);
                builder.WithClaims(tokenAcquisitionOptions.Claims);
                var clientClaims = GetClientClaimsIfExist(tokenAcquisitionOptions);
                if (clientClaims != null)
                {
                    builder.WithExtraClientAssertionClaims(clientClaims);
                }
                if (tokenAcquisitionOptions.PoPConfiguration != null)
                {
                    builder.WithProofOfPossession(tokenAcquisitionOptions.PoPConfiguration);
                }
                if (tokenAcquisitionOptions.CachePartitionKeys != null)
                {
                    foreach (var kvp in tokenAcquisitionOptions.CachePartitionKeys)
                    {
                        builder.WithCachePartitionKey(kvp.Key, kvp.Value);
                    }
                }
            }

            // Acquire an access token as a B2C authority
            if (mergedOptions.IsB2C)
            {
                string b2cAuthority = application.Authority.Replace(
                    new Uri(application.Authority).PathAndQuery,
                    $"/{ClaimConstants.Tfp}/{mergedOptions.Domain}/{userFlow ?? mergedOptions.DefaultUserFlow}"
#if !NETSTANDARD2_0 && !NET462 && !NET472
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

        internal static Dictionary<string, (string value, bool includeInCacheKey)>? MergeExtraQueryParameters(
            MergedOptions mergedOptions,
            TokenAcquisitionOptions? tokenAcquisitionOptions)
        {
            // Return null if both sources are empty
            if (tokenAcquisitionOptions?.ExtraQueryParameters == null && mergedOptions.ExtraQueryParameters == null)
            {
                return null;
            }

            var mergedDict = new Dictionary<string, (string value, bool includeInCacheKey)>(StringComparer.OrdinalIgnoreCase);

            // Add from tokenAcquisitionOptions first (these take precedence)
            if (tokenAcquisitionOptions?.ExtraQueryParameters != null)
            {
                foreach (var pair in tokenAcquisitionOptions.ExtraQueryParameters)
                {
                    mergedDict[pair.Key] = (pair.Value, true);
                }
            }

            // Add from mergedOptions without overriding existing keys
            if (mergedOptions.ExtraQueryParameters != null)
            {
                foreach (var pair in mergedOptions.ExtraQueryParameters)
                {
                    if (!mergedDict.ContainsKey(pair.Key))
                    {
                        mergedDict.Add(pair.Key, (pair.Value, true));
                    }
                }
            }

            return mergedDict;
        }

        protected static bool AcceptedTokenVersionMismatch(MsalUiRequiredException msalServiceException)
        {
            // Normally app developers should not make decisions based on the internal AAD code
            // however until the STS sends sub-error codes for this error, this is the only
            // way to distinguish the case.
            // This is subject to change in the future
            return msalServiceException.Message.Contains(
                ErrorCodes.B2CPasswordResetErrorCode
#if !NETSTANDARD2_0 && !NET462 && !NET472
                , StringComparison.InvariantCulture
#endif
                );
        }

        public string GetEffectiveAuthenticationScheme(string? authenticationScheme)
        {
            return _tokenAcquisitionHost.GetEffectiveAuthenticationScheme(authenticationScheme);
        }

        /// <summary>
        /// Temporary. Replace with Builder.WithClientAssertion when MSAL.NET supports it.
        /// </summary>
        private static bool OverrideClientAssertionIfNeeded<T>(TokenAcquisitionOptions? tokenAcquisitionOptions, AbstractConfidentialClientAcquireTokenParameterBuilder<T> builder)
            where T: AbstractAcquireTokenParameterBuilder<T>
        {
            if (tokenAcquisitionOptions == null || tokenAcquisitionOptions.ExtraParameters == null)
            {
                return false;
            }

            bool hasClientAssertion = false;
            if (tokenAcquisitionOptions.ExtraParameters != null &&
                tokenAcquisitionOptions.ExtraParameters.TryGetValue(Constants.ClientAssertion, out object? clientAssertionObj) &&
                clientAssertionObj is string clientAssertion &&
                !string.IsNullOrEmpty(clientAssertion))
            {
                // Use OnBeforeTokenRequest to add the client_assertion to the request
                builder.OnBeforeTokenRequest(request =>
                {
                    request.BodyParameters["client_assertion"] = clientAssertion;
                    request.BodyParameters["client_assertion_type"] = "urn:ietf:params:oauth:client-assertion-type:jwt-bearer";

                    // Remove client_secret if it exists, as it's not needed when using client_assertion
                    if (request.BodyParameters.ContainsKey("client_secret"))
                    {
                        request.BodyParameters.Remove("client_secret");
                    }

                    return Task.CompletedTask;
                });
                hasClientAssertion = true;
            }

            return hasClientAssertion;
        }

        // Used for backcompat support.
        private class LogAdapter<TCategory>(ILogger innerLogger) : ILogger<TCategory>
        {
            public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) => innerLogger.IsEnabled(logLevel);
            public void Log<TState>(Microsoft.Extensions.Logging.LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
                innerLogger.Log(logLevel, eventId, state, exception, formatter);
            IDisposable ILogger.BeginScope<TState>(TState state) =>
                innerLogger.BeginScope(state)!;
        }
    }
}
