// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web.TokenCacheProviders;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Implementation of ITokenAcquisition for App Services authentication (EasyAuth).
    /// </summary>
    /// <remarks>
    /// User token methods return the provider access token supplied by App Services authentication
    /// for the current request. App token methods acquire an app-only token through MSAL using the
    /// configured application credentials.
    /// </remarks>
    public class AppServicesAuthenticationTokenAcquisition : ITokenAcquisition
    {
        private readonly object _applicationSyncObj = new object();

        /// <summary>
        ///  Please call GetOrCreateApplication instead of accessing this field directly.
        /// </summary>
        private IConfidentialClientApplication? _application;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IMsalHttpClientFactory _httpClientFactory;
        private readonly IMsalTokenCacheProvider _tokenCacheProvider;

        internal class Account : IAccount
        {
            public Account(ClaimsPrincipal claimsPrincipal)
            {
                _claimsPrincipal = claimsPrincipal;
            }

            private readonly ClaimsPrincipal _claimsPrincipal;

#pragma warning disable CS8603 // Possible null reference return.
            public string Username => _claimsPrincipal.GetDisplayName();
#pragma warning restore CS8603 // Possible null reference return.

            public string? Environment => _claimsPrincipal.FindFirstValue("iss");

            public AccountId HomeAccountId => new AccountId(
                    $"{_claimsPrincipal.GetObjectId()}.{_claimsPrincipal.GetTenantId()}",
                    _claimsPrincipal.GetObjectId(),
                    _claimsPrincipal.GetTenantId());
        }

        private HttpContext? CurrentHttpContext
        {
            get
            {
                return _httpContextAccessor.HttpContext;
            }
        }

        /// <summary>
        /// Constructor of the AppServicesAuthenticationTokenAcquisition.
        /// </summary>
        /// <param name="tokenCacheProvider">The App token cache provider.</param>
        /// <param name="httpContextAccessor">Access to the HttpContext of the request.</param>
        /// <param name="httpClientFactory">HTTP client factory.</param>
        public AppServicesAuthenticationTokenAcquisition(
            IMsalTokenCacheProvider tokenCacheProvider,
            IHttpContextAccessor httpContextAccessor,
            IHttpClientFactory httpClientFactory)
        {
            _httpContextAccessor = Throws.IfNull(httpContextAccessor);
            _httpClientFactory = new MsalAspNetCoreHttpClientFactory(httpClientFactory);
            _tokenCacheProvider = tokenCacheProvider;
        }

        private IConfidentialClientApplication GetOrCreateApplication()
        {
            if (_application == null)
            {
                lock (_applicationSyncObj)
                {
                    if (_application == null)
                    {
                        var options = new ConfidentialClientApplicationOptions
                        {
                            ClientId = AppServicesAuthenticationInformation.ClientId,
                            ClientSecret = AppServicesAuthenticationInformation.ClientSecret,
                            Instance = AppServicesAuthenticationInformation.Issuer,
                        };
                        _application = ConfidentialClientApplicationBuilder.CreateWithApplicationOptions(options)
                            .WithHttpClientFactory(_httpClientFactory)
                            .Build();
                        _tokenCacheProvider.Initialize(_application.AppTokenCache);
                        _tokenCacheProvider.Initialize(_application.UserTokenCache);
                    }
                }
            }

            return _application;
        }

        /// <inheritdoc/>
        /// <remarks>
        /// Acquires an app-only token through the client credentials flow. This method does not
        /// return the provider token associated with the signed-in user.
        /// </remarks>
        public async Task<string> GetAccessTokenForAppAsync(
            string scope,
            string? authenticationScheme,
            string? tenant = null,
            TokenAcquisitionOptions? tokenAcquisitionOptions = null)
        {
            AuthenticationResult result = await GetAuthenticationResultForAppInternalAsync(
                scope,
                tenant,
                tokenAcquisitionOptions).ConfigureAwait(false);

            return result.AccessToken;
        }

        /// <inheritdoc/>
        public Task<string> GetAccessTokenForUserAsync(
            IEnumerable<string> scopes,
            string? authenticationScheme,
            string? tenantId = null,
            string? userFlow = null,
            ClaimsPrincipal? user = null,
            TokenAcquisitionOptions? tokenAcquisitionOptions = null)
        {
            var httpContext = CurrentHttpContext;
            string accessToken;
            if (httpContext != null)
            {
                // Need to lock due to https://learn.microsoft.com/aspnet/core/performance/performance-best-practices?#do-not-access-httpcontext-from-multiple-threads
                lock (httpContext)
                {
                    accessToken = GetAccessToken(httpContext.Request.Headers);
                }
            }
            else
            {
                accessToken = string.Empty;
            }

            return Task.FromResult(accessToken);
        }

        private string GetAccessToken(IHeaderDictionary? headers)
        {
            const string AppServicesAuthAccessTokenHeader = "X-MS-TOKEN-AAD-ACCESS-TOKEN";

            string? accessToken = null;
            if (headers != null)
            {
                accessToken = headers[AppServicesAuthAccessTokenHeader];
            }
#if DEBUG
            if (string.IsNullOrEmpty(accessToken))
            {
                accessToken = AppServicesAuthenticationInformation.SimulateGettingHeaderFromDebugEnvironmentVariable(AppServicesAuthAccessTokenHeader);
            }
#endif
            if (!string.IsNullOrEmpty(accessToken))
            {
                return accessToken;
            }

            return string.Empty;
        }

        /// <inheritdoc/>
        public async Task<AuthenticationResult> GetAuthenticationResultForUserAsync(
            IEnumerable<string> scopes,
            string? authenticationScheme,
            string? tenantId = null,
            string? userFlow = null,
            ClaimsPrincipal? user = null,
            TokenAcquisitionOptions? tokenAcquisitionOptions = null)
        {
            string? idToken = AppServicesAuthenticationInformation.GetIdToken(CurrentHttpContext?.Request?.Headers!);
            ClaimsPrincipal? userClaims = AppServicesAuthenticationInformation.GetUser(CurrentHttpContext?.Request?.Headers!);
            string accessToken = await GetAccessTokenForUserAsync(
                scopes,
                authenticationScheme: authenticationScheme,
                tenantId: tenantId,
                userFlow: userFlow,
                user: user,
                tokenAcquisitionOptions: tokenAcquisitionOptions).ConfigureAwait(false);
            string? expiration = userClaims?.FindFirstValue("exp");
            DateTimeOffset dateTimeOffset = (expiration != null)
                ? DateTimeOffset.FromUnixTimeSeconds(long.Parse(expiration, CultureInfo.InvariantCulture))
                : DateTimeOffset.Now;

            string? displayName;
            Account? account;
            if (userClaims != null)
            {
                displayName = userClaims.GetDisplayName();
                tenantId = userClaims.GetTenantId();
                account = new Account(userClaims);
            }
            else
            {
                displayName = null;
                tenantId = null;
                account = null;
            }

            AuthenticationResult authenticationResult = new AuthenticationResult(
                accessToken,
                isExtendedLifeTimeToken: false,
                displayName,
                dateTimeOffset,
                dateTimeOffset,
                tenantId,
                account,
                idToken,
                scopes,
                tokenAcquisitionOptions != null && tokenAcquisitionOptions.CorrelationId != null ? tokenAcquisitionOptions.CorrelationId.Value : Guid.Empty);
            return authenticationResult;
        }

        /// <inheritdoc/>
        public Task ReplyForbiddenWithWwwAuthenticateHeaderAsync(
            IEnumerable<string> scopes,
            MsalUiRequiredException msalServiceException,
            HttpResponse? httpResponse = null)
        {
            // Not supported for the moment
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public void ReplyForbiddenWithWwwAuthenticateHeader(
            IEnumerable<string> scopes,
            MsalUiRequiredException msalServiceException,
            string? authenticationScheme,
            HttpResponse? httpResponse = null)
        {
            // Not supported for the moment
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        /// <remarks>
        /// Acquires an app-only token through the client credentials flow. This method does not
        /// return the provider token associated with the signed-in user.
        /// </remarks>
        public Task<AuthenticationResult> GetAuthenticationResultForAppAsync(
            string scope,
            string? authenticationScheme,
            string? tenant = null,
            TokenAcquisitionOptions? tokenAcquisitionOptions = null)
        {
            return GetAuthenticationResultForAppInternalAsync(
                scope,
                tenant,
                tokenAcquisitionOptions);
        }

        private async Task<AuthenticationResult> GetAuthenticationResultForAppInternalAsync(
            string scope,
            string? tenant,
            TokenAcquisitionOptions? tokenAcquisitionOptions)
        {
            _ = Throws.IfNull(scope);

            AcquireTokenForClientParameterBuilder builder = GetOrCreateApplication()
                .AcquireTokenForClient(new[] { scope });

            string? effectiveTenant = tenant ?? tokenAcquisitionOptions?.Tenant;
            if (!string.IsNullOrEmpty(effectiveTenant))
            {
                builder.WithTenantId(effectiveTenant);
            }

            return await builder.ExecuteAsync().ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public string GetEffectiveAuthenticationScheme(string? authenticationScheme)
        {
            throw new NotSupportedException();
        }
    }
}
