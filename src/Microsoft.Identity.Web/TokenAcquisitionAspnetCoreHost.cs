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
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
        private readonly IOptionsMonitor<MergedOptions> _mergedOptionsMonitor;
        private readonly ILogger _logger;
        private readonly IServiceProvider _serviceProvider;


        /// <summary>
        /// Constructor of the TokenAcquisition service. This requires the Azure AD Options to
        /// configure the confidential client application and a token cache provider.
        /// This constructor is called by ASP.NET Core dependency injection.
        /// </summary>
        /// <param name="httpContextAccessor">Access to the HttpContext of the request.</param>
        /// <param name="mergedOptionsMonitor">Configuration options.</param>
        /// <param name="logger">Logger.</param>
        /// <param name="serviceProvider">Service provider.</param>
        public TokenAcquisitionAspnetCoreHost(
            IHttpContextAccessor httpContextAccessor,
            IOptionsMonitor<MergedOptions> mergedOptionsMonitor,
            ILogger<TokenAcquisition> logger,
            IServiceProvider serviceProvider)
        {
            _httpContextAccessor = httpContextAccessor;
            _mergedOptionsMonitor = mergedOptionsMonitor;
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        public MergedOptions GetOptions(string? authenticationScheme, out string effectiveAuthenticationScheme)
        {
            effectiveAuthenticationScheme = GetEffectiveAuthenticationScheme(authenticationScheme);

            var mergedOptions = _mergedOptionsMonitor.Get(effectiveAuthenticationScheme);

            if (!mergedOptions.MergedWithCca)
            {
                _serviceProvider.GetService<IOptionsMonitor<ConfidentialClientApplicationOptions>>()?.Get(effectiveAuthenticationScheme);
            }

            // Case of an anonymous controller, no [Authorize] attribute will trigger the merge options
            if (string.IsNullOrEmpty(mergedOptions.Instance) || string.IsNullOrEmpty(mergedOptions.ClientId))
            {
                JwtBearerOptions? jwtBearerOptions = _serviceProvider.GetService<IOptionsMonitor<JwtBearerOptions>>()?.Get(effectiveAuthenticationScheme); // supports event handlers
                if (jwtBearerOptions != null)
                {
                    MergedOptions.UpdateMergedOptionsFromJwtBearerOptions(jwtBearerOptions, mergedOptions);
                }
            }

            // Case of an anonymous controller called from a web app
            if (string.IsNullOrEmpty(mergedOptions.Instance) || string.IsNullOrEmpty(mergedOptions.ClientId))
            {
                _serviceProvider.GetService<IOptionsMonitor<OpenIdConnectOptions>>()?.Get(effectiveAuthenticationScheme);
            }

            if (string.IsNullOrEmpty(mergedOptions.Instance) || string.IsNullOrEmpty(mergedOptions.ClientId))
            {
                MicrosoftIdentityOptions? microsoftIdentityOptions = _serviceProvider.GetService<IOptionsMonitor<MicrosoftIdentityOptions>>()?.Get(effectiveAuthenticationScheme);
                if (microsoftIdentityOptions != null)
                {
                    MergedOptions.UpdateMergedOptionsFromMicrosoftIdentityOptions(microsoftIdentityOptions, mergedOptions);
                }
            }

            if (string.IsNullOrEmpty(mergedOptions.Instance))
            {
                var availableSchemes = _serviceProvider.GetService<IAuthenticationSchemeProvider>()?.GetAllSchemesAsync()?.Result?.Select(a => a.Name);
                string msg = string.Format(CultureInfo.InvariantCulture, IDWebErrorMessage.ProvidedAuthenticationSchemeIsIncorrect,
                    authenticationScheme, effectiveAuthenticationScheme, availableSchemes != null ? string.Join(",", availableSchemes) : string.Empty);
                throw new InvalidOperationException(msg);
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
                // Need to lock due to https://docs.microsoft.com/en-us/aspnet/core/performance/performance-best-practices?#do-not-access-httpcontext-from-multiple-threads
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
