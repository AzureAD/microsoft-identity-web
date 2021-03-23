// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web.TokenCacheProviders;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Implementation of ITokenAcquisition for App Services authentication (EasyAuth).
    /// </summary>
    public class AppServicesAuthenticationTokenAcquisition : ITokenAcquisition, IDisposable
    {
        private readonly SemaphoreSlim _confidentialClientApplicationSyncObj = new SemaphoreSlim(/* max concurrent locks */ 1);
        /// <summary>
        /// Do no read or set this directly.
        /// Instead, use GetOrCreateApplication which is thread-safe
        /// </summary>
        private IConfidentialClientApplication? _confidentialClientApplication;
        private bool _disposedValue;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IMsalHttpClientFactory _httpClientFactory;
        private readonly IMsalTokenCacheProvider _tokenCacheProvider;

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
                await _confidentialClientApplicationSyncObj.WaitAsync();

                try
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
                }
                finally
                {
                    _confidentialClientApplicationSyncObj.Release();
                }
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
        public Task<string> GetAccessTokenForUserAsync(
            IEnumerable<string> scopes,
            string? tenantId = null,
            string? userFlow = null,
            ClaimsPrincipal? user = null,
            TokenAcquisitionOptions? tokenAcquisitionOptions = null)
        {
            var httpContext = CurrentHttpContext;
            string accessToken;
            if (httpContext != null)
            {
                // Need to lock due to https://docs.microsoft.com/en-us/aspnet/core/performance/performance-best-practices?#do-not-access-httpcontext-from-multiple-threads
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
        public Task<AuthenticationResult> GetAuthenticationResultForUserAsync(IEnumerable<string> scopes, string? tenantId = null, string? userFlow = null, ClaimsPrincipal? user = null, TokenAcquisitionOptions? tokenAcquisitionOptions = null)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task ReplyForbiddenWithWwwAuthenticateHeaderAsync(IEnumerable<string> scopes, MsalUiRequiredException msalServiceException, HttpResponse? httpResponse = null)
        {
            // Not implemented for the moment
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _confidentialClientApplicationSyncObj.Dispose();
                }

                _disposedValue = true;
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

    }
}
