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
    public class AppServicesAuthenticationTokenAcquisition : ITokenAcquisition
    {
        private IConfidentialClientApplication? _confidentialClientApplication;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IMsalHttpClientFactory _httpClientFactory;
        private readonly IMsalTokenCacheProvider _tokenCacheProvider;

        class Account : IAccount
        {
            public Account(ClaimsPrincipal claimsPrincipal)
            {
                _claimsPrincipal = claimsPrincipal;
            }
            ClaimsPrincipal _claimsPrincipal;

            public string Username => _claimsPrincipal.GetDisplayName();

            public string Environment => _claimsPrincipal.FindFirstValue("iss");

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
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            _httpClientFactory = new MsalAspNetCoreHttpClientFactory(httpClientFactory);
            _tokenCacheProvider = tokenCacheProvider;
        }

        private async Task<IConfidentialClientApplication> GetOrCreateApplication()
        {
            if (_confidentialClientApplication == null)
            {
                ConfidentialClientApplicationOptions options = new ConfidentialClientApplicationOptions()
                {
                    ClientId = AppServicesAuthenticationInformation.ClientId,
                    ClientSecret = AppServicesAuthenticationInformation.ClientSecret,
                    Instance = AppServicesAuthenticationInformation.Issuer,
                };
                _confidentialClientApplication = ConfidentialClientApplicationBuilder.CreateWithApplicationOptions(options)
                    .WithHttpClientFactory(_httpClientFactory)
                    .Build();
                await _tokenCacheProvider.InitializeAsync(_confidentialClientApplication.AppTokenCache).ConfigureAwait(false);
                await _tokenCacheProvider.InitializeAsync(_confidentialClientApplication.UserTokenCache).ConfigureAwait(false);
            }

            return _confidentialClientApplication;
        }

        /// <inheritdoc/>
        public async Task<string> GetAccessTokenForAppAsync(
            string scope,
            string? tenant = null,
            TokenAcquisitionOptions? tokenAcquisitionOptions = null)
        {
            // We could use MSI
            if (scope is null)
            {
                throw new ArgumentNullException(nameof(scope));
            }

            var app = await GetOrCreateApplication().ConfigureAwait(false);
            AuthenticationResult result = await app.AcquireTokenForClient(new string[] { scope })
                .ExecuteAsync()
                .ConfigureAwait(false);

            return result.AccessToken;
        }

        /// <inheritdoc/>
        public async Task<string> GetAccessTokenForUserAsync(
            IEnumerable<string> scopes,
            string? tenantId = null,
            string? userFlow = null,
            ClaimsPrincipal? user = null,
            TokenAcquisitionOptions? tokenAcquisitionOptions = null)
        {
            string accessToken = GetAccessToken(CurrentHttpContext?.Request.Headers);

            return await Task.FromResult(accessToken).ConfigureAwait(false);
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
                accessToken = AppServicesAuthenticationInformation.SimulateGetttingHeaderFromDebugEnvironmentVariable(AppServicesAuthAccessTokenHeader);
            }
#endif
            if (!string.IsNullOrEmpty(accessToken))
            {
                return accessToken;
            }

            return string.Empty;
        }

        /// <inheritdoc/>
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public async Task<AuthenticationResult> GetAuthenticationResultForUserAsync(IEnumerable<string> scopes, string? tenantId = null, string? userFlow = null, ClaimsPrincipal? user = null, TokenAcquisitionOptions? tokenAcquisitionOptions = null)
        {
            string? idToken = AppServicesAuthenticationInformation.GetIdToken(CurrentHttpContext?.Request?.Headers);
            ClaimsPrincipal? userClaims = AppServicesAuthenticationInformation.GetUser(CurrentHttpContext?.Request?.Headers);
            string accessToken = await GetAccessTokenForUserAsync(scopes, tenantId, userFlow, user, tokenAcquisitionOptions);
            string expiration = userClaims.FindFirstValue("exp");
            DateTimeOffset dateTimeOffset = (expiration != null)
                ? DateTimeOffset.FromUnixTimeSeconds(long.Parse(expiration, CultureInfo.InvariantCulture))
                : DateTimeOffset.Now;

            AuthenticationResult authenticationResult = new AuthenticationResult(
                accessToken,
                isExtendedLifeTimeToken: false,
                userClaims?.GetDisplayName(),
                dateTimeOffset,
                dateTimeOffset,
                userClaims?.GetTenantId(),
                new Account(userClaims),
                idToken,
                scopes,
                tokenAcquisitionOptions.CorrelationId);
            return authenticationResult;
        }

        /// <inheritdoc/>
        public async Task ReplyForbiddenWithWwwAuthenticateHeaderAsync(IEnumerable<string> scopes, MsalUiRequiredException msalServiceException, HttpResponse? httpResponse = null)
        {
            // Not implemented for the moment
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<AuthenticationResult> GetAuthenticationResultForAppAsync(string scope, string? tenant = null, TokenAcquisitionOptions? tokenAcquisitionOptions = null)
        {
            throw new NotImplementedException();
        }
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously

    }
}
