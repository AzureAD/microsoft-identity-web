// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web.TokenCacheProviders;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Implementation of ITokenAcquisition for App services authentication (EasyAuth).
    /// </summary>
    public class AppServicesAuthenticationTokenAcquisition : ITokenAcquisition
    {
        private IConfidentialClientApplication _confidentialClientApplication;
        private IHttpContextAccessor _httpContextAccessor;
        private IMsalHttpClientFactory _httpClientFactory;
        private IMsalTokenCacheProvider _tokenCacheProvider;

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
        public AppServicesAuthenticationTokenAcquisition(IMsalTokenCacheProvider tokenCacheProvider,  IHttpContextAccessor httpContextAccessor, IHttpClientFactory httpClientFactory)
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
                    ClientId = AppServiceAuthenticationInformation.ClientId,
                    ClientSecret = AppServiceAuthenticationInformation.ClientSecret,
                    Instance = AppServiceAuthenticationInformation.Issuer,
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
            if (scope is null)
            {
                throw new ArgumentNullException(nameof(scope));
            }

            AuthenticationResult result = await _confidentialClientApplication.AcquireTokenForClient(new string[] { scope })
                .ExecuteAsync()
                .ConfigureAwait(false);

            return result.AccessToken;
        }

        /// <inheritdoc/>
        public async Task<string> GetAccessTokenForUserAsync(IEnumerable<string> scopes, string? tenantId = null, string? userFlow = null, ClaimsPrincipal? user = null, TokenAcquisitionOptions? tokenAcquisitionOptions = null)
        {
            if (scopes is null)
            {
                throw new ArgumentNullException(nameof(scopes));
            }

            AuthenticationResult result = await GetAuthenticationResultForUserAsync(
                scopes,
                tenantId,
                userFlow,
                user,
                tokenAcquisitionOptions).ConfigureAwait(false);
            return result.AccessToken;
        }

        private string? GetRefreshToken(IHeaderDictionary? headers)
        {
            const string easyAuthRefreshTokenHeader = "X-MS-TOKEN-AAD-REFRESH-TOKEN";

            string? refreshToken = null;
            if (headers != null)
            {
                refreshToken = headers[easyAuthRefreshTokenHeader];
            }
#if DEBUG
            if (string.IsNullOrEmpty(refreshToken))
            {
                refreshToken = AppServiceAuthenticationInformation.GetDebugHeader(easyAuthRefreshTokenHeader);
            }
#endif
            return refreshToken;
        }

        /// <inheritdoc/>
        public async Task<AuthenticationResult> GetAuthenticationResultForUserAsync(IEnumerable<string> scopes, string? tenantId = null, string? userFlow = null, ClaimsPrincipal? user = null, TokenAcquisitionOptions? tokenAcquisitionOptions = null)
        {
            if (scopes is null)
            {
                throw new ArgumentNullException(nameof(scopes));
            }

            string? refreshToken = GetRefreshToken(CurrentHttpContext?.Request?.Headers);
            if (refreshToken != null)
            {
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
                IByRefreshToken byRefreshToken = _confidentialClientApplication as IByRefreshToken;
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
                AuthenticationResult result = await byRefreshToken.AcquireTokenByRefreshToken(scopes, refreshToken)
                    .ExecuteAsync()
                    .ConfigureAwait(false);
                return result;
            }
            else
            {
                return null;
            }
        }

        /// <inheritdoc/>
        public async Task ReplyForbiddenWithWwwAuthenticateHeaderAsync(IEnumerable<string> scopes, MsalUiRequiredException msalServiceException, HttpResponse? httpResponse = null)
        {
            // Not implmented for the moment
            throw new NotImplementedException();
        }
    }
}
