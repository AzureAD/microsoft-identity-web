// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Client;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Net.Http.Headers;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Implementation of ITokenAcquisitionHost in the case of ASP.NET Core
    /// </summary>
    internal class TokenAcquisitionAspnetCoreHost : ITokenAcquisitionHost
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private HttpContext? CurrentHttpContext => _httpContextAccessor.HttpContext;
        private readonly IMergedOptionsStore _mergedOptionsMonitor;
        private readonly IServiceProvider _serviceProvider;


        /// <summary>
        /// Constructor of the TokenAcquisition service. This requires the Azure AD Options to
        /// configure the confidential client application and a token cache provider.
        /// This constructor is called by ASP.NET Core dependency injection.
        /// </summary>
        /// <param name="httpContextAccessor">Access to the HttpContext of the request.</param>
        /// <param name="mergedOptionsMonitor">Configuration options.</param>
        /// <param name="serviceProvider">Service provider.</param>
        public TokenAcquisitionAspnetCoreHost(
            IHttpContextAccessor httpContextAccessor,
            IMergedOptionsStore mergedOptionsMonitor,
            IServiceProvider serviceProvider)
        {
            _httpContextAccessor = httpContextAccessor;
            _mergedOptionsMonitor = mergedOptionsMonitor;
            _serviceProvider = serviceProvider;
        }

        public MergedOptions GetOptions(string? authenticationScheme, out string effectiveAuthenticationScheme)
        {
            effectiveAuthenticationScheme = GetEffectiveAuthenticationScheme(authenticationScheme);

            var mergedOptions = _mergedOptionsMonitor.Get(effectiveAuthenticationScheme);

            // TODO can we factorize somewhere else?
            MergedOptions.ParseAuthorityIfNecessary(mergedOptions);

            if (!mergedOptions.MergedWithCca)
            {
                _serviceProvider.GetService<IOptionsMonitor<ConfidentialClientApplicationOptions>>()?.Get(effectiveAuthenticationScheme);
            }

            _serviceProvider.GetService<IOptionsMonitor<MicrosoftIdentityApplicationOptions>>()?.Get(effectiveAuthenticationScheme);
            _serviceProvider.GetService<IOptionsMonitor<MicrosoftIdentityOptions>>()?.Get(effectiveAuthenticationScheme);

            // Case of an anonymous controller, no [Authorize] attribute will trigger the merge options
            if (string.IsNullOrEmpty(mergedOptions.Instance) || string.IsNullOrEmpty(mergedOptions.ClientId))
            {
                JwtBearerOptions? jwtBearerOptions = _serviceProvider.GetService<IOptionsMonitor<JwtBearerOptions>>()?.Get(effectiveAuthenticationScheme); // supports event handlers
                _serviceProvider.GetService<IOptionsMonitor<JwtBearerOptions>>()?.Get(effectiveAuthenticationScheme);
            }

            // Case of an anonymous controller called from a web app
            if (string.IsNullOrEmpty(mergedOptions.Instance) || string.IsNullOrEmpty(mergedOptions.ClientId))
            {
                _serviceProvider.GetService<IOptionsMonitor<OpenIdConnectOptions>>()?.Get(effectiveAuthenticationScheme);
            }

            // Get the merged options again after the merging has occurred
            mergedOptions = _mergedOptionsMonitor.Get(effectiveAuthenticationScheme);

            if (string.IsNullOrEmpty(mergedOptions.Instance))
            {
                // Check if the issue is that MicrosoftIdentityApplicationOptions are not configured
                // vs. an incorrect authentication scheme
                var microsoftIdentityApplicationOptions = _serviceProvider.GetService<IOptionsMonitor<MicrosoftIdentityApplicationOptions>>()?.Get(effectiveAuthenticationScheme);
                bool isMicrosoftIdentityApplicationOptionsConfigured = microsoftIdentityApplicationOptions != null && 
                    (!string.IsNullOrEmpty(microsoftIdentityApplicationOptions.Instance) || 
                     !string.IsNullOrEmpty(microsoftIdentityApplicationOptions.Authority) ||
                     !string.IsNullOrEmpty(microsoftIdentityApplicationOptions.ClientId));

                if (!isMicrosoftIdentityApplicationOptionsConfigured)
                {
                    string msg = string.Format(CultureInfo.InvariantCulture, IDWebErrorMessage.MicrosoftIdentityApplicationOptionsNotConfigured,
                        effectiveAuthenticationScheme);
                    throw new InvalidOperationException(msg);
                }
                else
                {
                    var availableSchemes = _serviceProvider.GetService<IAuthenticationSchemeProvider>()?.GetAllSchemesAsync()?.Result?.Select(a => a.Name);
                    string msg = string.Format(CultureInfo.InvariantCulture, IDWebErrorMessage.ProvidedAuthenticationSchemeIsIncorrect,
                        authenticationScheme, effectiveAuthenticationScheme, availableSchemes != null ? string.Join(",", availableSchemes) : string.Empty);
                    throw new InvalidOperationException(msg);
                }
            }

            DefaultCertificateLoader.UserAssignedManagedIdentityClientId = mergedOptions.UserAssignedManagedIdentityClientId;
            return mergedOptions;
        }

        public void SetSession(string key, string value)
        {
            CurrentHttpContext?.Session.SetString(key, value);
        }

        /// <inheritdoc/>
        public string GetEffectiveAuthenticationScheme(string? authenticationScheme)
        {
            if (!string.IsNullOrEmpty(authenticationScheme))
            {
                return authenticationScheme;
            }
            else
            {
                IAuthenticationSchemeProvider? authenticationSchemeProvider = _serviceProvider?.GetService<IAuthenticationSchemeProvider>();
                if (authenticationSchemeProvider != null)
                {
                    return authenticationSchemeProvider?.GetDefaultAuthenticateSchemeAsync()?.Result?.Name ??
                        ((CurrentHttpContext?.GetTokenUsedToCallWebAPI() != null)
                        ? JwtBearerDefaults.AuthenticationScheme : OpenIdConnectDefaults.AuthenticationScheme);
                }

                // Will never happen in ASP.NET Core web app, web APIs where services.AddAuthentication is added
                // This will happen in ASP.NET Core daemons
                else
                {
                    return string.Empty;
                }
            }
        }

        public string? GetCurrentRedirectUri(MergedOptions mergedOptions)
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

            return currentUri;
        }

        private string BuildCurrentUriFromRequest(
            HttpContext httpContext,
            HttpRequest request,
            MergedOptions mergedOptions)
        {
            // need to lock to avoid threading issues with code outside of this library
            // https://learn.microsoft.com/aspnet/core/performance/performance-best-practices?#do-not-access-httpcontext-from-multiple-threads
            lock (httpContext)
            {
                return UriHelper.BuildAbsolute(
                    request.Scheme,
                    request.Host,
                    request.PathBase,
                    mergedOptions.CallbackPath.Value ?? string.Empty);
            }
        }

        public void SetHttpResponse(HttpStatusCode statusCode, string wwwAuthenticate)
        {
            var httpResponse = CurrentHttpContext?.Response;
            if (httpResponse == null)
            {
                throw new InvalidOperationException(IDWebErrorMessage.HttpContextAndHttpResponseAreNull);
            }

            var headers = httpResponse.Headers;
            httpResponse.StatusCode = (int)HttpStatusCode.Forbidden;

            headers[HeaderNames.WWWAuthenticate] = wwwAuthenticate;
        }

        public SecurityToken? GetTokenUsedToCallWebAPI()
        {
            return CurrentHttpContext?.GetTokenUsedToCallWebAPI();
        }

        public ClaimsPrincipal? GetUserFromRequest()
        {
            var httpContext = CurrentHttpContext;
            if (httpContext != null)
            {
                // Need to lock due to https://learn.microsoft.com/aspnet/core/performance/performance-best-practices?#do-not-access-httpcontext-from-multiple-threads
                lock (httpContext)
                {
                    return httpContext.User;
                }
            }

            return null;
        }

        public async Task<ClaimsPrincipal?> GetAuthenticatedUserAsync(ClaimsPrincipal? user)
        {
            if (user == null)
            {
                user = GetUserFromRequest();
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
    }
}
