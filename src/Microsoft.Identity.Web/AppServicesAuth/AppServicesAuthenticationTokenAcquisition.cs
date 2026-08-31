// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web.TokenCacheProviders;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Implementation of ITokenAcquisition for App Services authentication (EasyAuth).
    /// </summary>
    public class AppServicesAuthenticationTokenAcquisition : ITokenAcquisition
    {
        private const string AppServicesAuthAccessTokenHeader = "X-MS-TOKEN-AAD-ACCESS-TOKEN";
        private const string MicrosoftGraphAppId = "00000003-0000-0000-c000-000000000000";
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
        public async Task<string> GetAccessTokenForAppAsync(
            string scope,
            string? authenticationScheme,
            string? tenant = null,
            TokenAcquisitionOptions? tokenAcquisitionOptions = null)
        {
            // We could use MSI
            _ = Throws.IfNull(scope);

            string? explicitTenant = GetConsistentExplicitTenant(tenant, tokenAcquisitionOptions?.Tenant);
            AcquireTokenForClientParameterBuilder builder = GetOrCreateApplication()
                .AcquireTokenForClient(new[] { scope });
            if (explicitTenant is not null)
            {
                builder.WithTenantId(explicitTenant);
            }

            AuthenticationResult result = await builder.ExecuteAsync()
                .ConfigureAwait(false);

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
            string[] requestedScopes = MaterializeScopes(scopes);
            string? explicitTenant = GetConsistentExplicitTenant(tenantId, tokenAcquisitionOptions?.Tenant);
            string accessToken = GetCurrentAccessToken();

            if (explicitTenant is not null || requestedScopes.Length > 0)
            {
                ValidateUserToken(accessToken, requestedScopes, explicitTenant);
            }

            return Task.FromResult(accessToken);
        }

        private string GetAccessToken(IHeaderDictionary? headers)
        {
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
        public Task<AuthenticationResult> GetAuthenticationResultForUserAsync(
            IEnumerable<string> scopes,
            string? authenticationScheme,
            string? tenantId = null,
            string? userFlow = null,
            ClaimsPrincipal? user = null,
            TokenAcquisitionOptions? tokenAcquisitionOptions = null)
        {
            string[] requestedScopes = MaterializeScopes(scopes);
            string? explicitTenant = GetConsistentExplicitTenant(tenantId, tokenAcquisitionOptions?.Tenant);
            string accessToken = GetCurrentAccessToken();
            IReadOnlyCollection<string> actualScopes = explicitTenant is not null || requestedScopes.Length > 0
                ? ValidateUserToken(accessToken, requestedScopes, explicitTenant)
                : Array.Empty<string>();

            string? idToken = AppServicesAuthenticationInformation.GetIdToken(CurrentHttpContext?.Request?.Headers!);
            ClaimsPrincipal? userClaims = AppServicesAuthenticationInformation.GetUser(CurrentHttpContext?.Request?.Headers!);
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
                actualScopes,
                tokenAcquisitionOptions != null && tokenAcquisitionOptions.CorrelationId != null ? tokenAcquisitionOptions.CorrelationId.Value : Guid.Empty);
            return Task.FromResult(authenticationResult);
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
        public Task<AuthenticationResult> GetAuthenticationResultForAppAsync(
            string scope,
            string? authenticationScheme,
            string? tenant = null,
            TokenAcquisitionOptions? tokenAcquisitionOptions = null)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public string GetEffectiveAuthenticationScheme(string? authenticationScheme)
        {
            throw new NotSupportedException();
        }

        private string GetCurrentAccessToken()
        {
            HttpContext? httpContext = CurrentHttpContext;
            if (httpContext is null)
            {
                return string.Empty;
            }

            // HttpContext must not be accessed concurrently from multiple threads.
            lock (httpContext)
            {
                return GetAccessToken(httpContext.Request.Headers);
            }
        }

        private static string[] MaterializeScopes(IEnumerable<string> scopes)
        {
            _ = Throws.IfNull(scopes);
            string[] requestedScopes = scopes.ToArray();
            if (requestedScopes.Any(string.IsNullOrWhiteSpace))
            {
                throw new ArgumentException("Requested scopes cannot contain an empty value.", nameof(scopes));
            }

            return requestedScopes;
        }

        private static string? GetConsistentExplicitTenant(string? directTenant, string? optionsTenant)
        {
            if (directTenant is not null && string.IsNullOrWhiteSpace(directTenant))
            {
                throw new ArgumentException("An explicit tenant cannot be empty.", nameof(directTenant));
            }

            if (optionsTenant is not null && string.IsNullOrWhiteSpace(optionsTenant))
            {
                throw new ArgumentException("An explicit tenant cannot be empty.", nameof(optionsTenant));
            }

            if (directTenant is not null &&
                optionsTenant is not null &&
                !string.Equals(directTenant, optionsTenant, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Conflicting explicit tenant values were supplied.", nameof(directTenant));
            }

            return directTenant ?? optionsTenant;
        }

        private static IReadOnlyCollection<string> ValidateUserToken(
            string accessToken,
            IReadOnlyCollection<string> requestedScopes,
            string? explicitTenant)
        {
            using JsonDocument payload = ReadTokenPayload(accessToken);
            JsonElement payloadRoot = payload.RootElement;

            if (explicitTenant is not null)
            {
                ValidateTenant(payloadRoot, explicitTenant);
            }

            bool delegatedScopeProofRequired = requestedScopes.Any(
                requestedScope => !TrySplitResourceScope(requestedScope, out _, out string? permission) ||
                    !string.Equals(permission, ".default", StringComparison.Ordinal));
            string[] actualScopes = delegatedScopeProofRequired
                ? GetActualScopes(payloadRoot)
                : Array.Empty<string>();
            foreach (string requestedScope in requestedScopes)
            {
                if (TrySplitResourceScope(requestedScope, out string? resource, out string? permission))
                {
                    ValidateAudience(payloadRoot, resource!);
                    if (!string.Equals(permission, ".default", StringComparison.Ordinal))
                    {
                        ValidateDelegatedScope(actualScopes, permission!, requestedScope);
                    }
                }
                else
                {
                    ValidateDelegatedScope(actualScopes, requestedScope, requestedScope);
                }
            }

            return actualScopes;
        }

        private static JsonDocument ReadTokenPayload(string accessToken)
        {
            int firstSeparator = accessToken.IndexOf(".", StringComparison.Ordinal);
            int secondSeparator = firstSeparator < 0
                ? -1
                : accessToken.IndexOf(".", firstSeparator + 1, StringComparison.Ordinal);
            if (firstSeparator <= 0 ||
                secondSeparator <= firstSeparator + 1 ||
                accessToken.IndexOf(".", secondSeparator + 1, StringComparison.Ordinal) >= 0)
            {
                throw new NotSupportedException(
                    "The EasyAuth access token is not a readable JWT, so the requested token properties cannot be verified.");
            }

            try
            {
                byte[] payload = Base64UrlEncoder.DecodeBytes(
                    accessToken.Substring(firstSeparator + 1, secondSeparator - firstSeparator - 1));
                return JsonDocument.Parse(payload);
            }
            catch (JsonException exception)
            {
                throw new NotSupportedException(
                    "The EasyAuth access token is not a readable JWT, so the requested token properties cannot be verified.",
                    exception);
            }
            catch (FormatException exception)
            {
                throw new NotSupportedException(
                    "The EasyAuth access token is not a readable JWT, so the requested token properties cannot be verified.",
                    exception);
            }
        }

        private static void ValidateTenant(JsonElement payload, string explicitTenant)
        {
            string tenantValue = GetSingleStringPayloadMember(payload, ClaimConstants.Tid, "tenant ID");
            if (!Guid.TryParse(tenantValue, out Guid tokenTenant) ||
                !Guid.TryParse(explicitTenant, out Guid requestedTenant))
            {
                throw new NotSupportedException(
                    "The EasyAuth access token does not contain one valid tenant ID that can be compared with the explicit tenant.");
            }

            if (tokenTenant != requestedTenant)
            {
                throw new ArgumentException(
                    "The explicit tenant does not match the EasyAuth access token tenant.",
                    nameof(explicitTenant));
            }
        }

        private static string[] GetActualScopes(JsonElement payload)
        {
            string scopeValue = GetSingleStringPayloadMember(payload, ClaimConstants.Scp, "delegated scope");
            return scopeValue.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Distinct(StringComparer.Ordinal)
                .ToArray();
        }

        private static string GetSingleStringPayloadMember(
            JsonElement payload,
            string memberName,
            string description,
            bool requireNonEmpty = false)
        {
            if (payload.ValueKind != JsonValueKind.Object)
            {
                throw new NotSupportedException(
                    $"The EasyAuth access token payload does not contain one valid {description} string.");
            }

            JsonElement value = default;
            int matchCount = 0;
            foreach (JsonProperty property in payload.EnumerateObject())
            {
                if (property.NameEquals(memberName))
                {
                    value = property.Value;
                    matchCount++;
                }
            }

            if (matchCount != 1 ||
                value.ValueKind != JsonValueKind.String ||
                (requireNonEmpty && string.IsNullOrEmpty(value.GetString())))
            {
                throw new NotSupportedException(
                    $"The EasyAuth access token payload does not contain one valid {description} string.");
            }

            return value.GetString()!;
        }

        private static void ValidateDelegatedScope(
            IReadOnlyCollection<string> actualScopes,
            string permission,
            string requestedScope)
        {
            if (actualScopes.Count == 0)
            {
                throw new NotSupportedException(
                    "The EasyAuth access token does not contain delegated scopes that can be compared with the request.");
            }

            if (!actualScopes.Contains(permission, StringComparer.Ordinal))
            {
                throw new ArgumentException(
                    $"The EasyAuth access token does not contain requested scope '{requestedScope}'.",
                    nameof(requestedScope));
            }
        }

        private static void ValidateAudience(JsonElement payload, string requestedResource)
        {
            string audience = GetSingleStringPayloadMember(
                payload,
                "aud",
                "audience",
                requireNonEmpty: true);
            if (ResourcesMatchExactly(requestedResource, audience) ||
                ResourcesMatchByApplicationId(requestedResource, audience))
            {
                return;
            }

            if (ResourcesMatchMicrosoftGraphAlias(payload, requestedResource, audience))
            {
                return;
            }

            throw new ArgumentException(
                $"The EasyAuth access token audience does not match requested resource '{requestedResource}'.",
                nameof(requestedResource));
        }

        private static bool TrySplitResourceScope(
            string requestedScope,
            out string? resource,
            out string? permission)
        {
            resource = null;
            permission = null;
            int separator = requestedScope.LastIndexOf('/');
            if (separator <= 0 || separator == requestedScope.Length - 1)
            {
                return false;
            }

            string candidateResource = requestedScope.Substring(0, separator);
            if (!Uri.TryCreate(candidateResource, UriKind.Absolute, out _) &&
                !Guid.TryParse(candidateResource, out _))
            {
                return false;
            }

            resource = candidateResource;
            permission = requestedScope.Substring(separator + 1);
            return true;
        }

        private static bool ResourcesMatchExactly(string requestedResource, string audience)
        {
            return TryNormalizeResource(requestedResource, out string? normalizedRequested) &&
                TryNormalizeResource(audience, out string? normalizedAudience) &&
                string.Equals(normalizedRequested, normalizedAudience, StringComparison.Ordinal);
        }

        private static bool ResourcesMatchByApplicationId(string requestedResource, string audience)
        {
            return TryGetApplicationId(requestedResource, out Guid requestedAppId) &&
                TryGetApplicationId(audience, out Guid audienceAppId) &&
                requestedAppId == audienceAppId;
        }

        private static bool ResourcesMatchMicrosoftGraphAlias(
            JsonElement payload,
            string requestedResource,
            string audience)
        {
            bool requestedIsGraphUri = TryGetMicrosoftGraphCloud(requestedResource, out string? requestedCloud);
            bool audienceIsGraphUri = TryGetMicrosoftGraphCloud(audience, out string? audienceCloud);
            bool requestedIsGraphAppId = TryGetApplicationId(requestedResource, out Guid requestedAppId) &&
                requestedAppId == Guid.Parse(MicrosoftGraphAppId);
            bool audienceIsGraphAppId = TryGetApplicationId(audience, out Guid audienceAppId) &&
                audienceAppId == Guid.Parse(MicrosoftGraphAppId);

            string? resourceCloud = requestedIsGraphUri ? requestedCloud : audienceCloud;
            if (resourceCloud is null ||
                !(requestedIsGraphUri && audienceIsGraphAppId ||
                  requestedIsGraphAppId && audienceIsGraphUri))
            {
                return false;
            }

            string issuer = GetSingleStringPayloadMember(
                payload,
                "iss",
                "issuer",
                requireNonEmpty: true);
            string graphResource = requestedIsGraphUri ? requestedResource : audience;
            if (IsSharedV1Issuer(issuer))
            {
                return IsSharedV1GraphResource(graphResource);
            }

            return TryGetIssuerCloud(issuer, out string? issuerCloud) &&
                string.Equals(resourceCloud, issuerCloud, StringComparison.Ordinal);
        }

        private static bool IsSharedV1Issuer(string issuer)
        {
            return Uri.TryCreate(issuer, UriKind.Absolute, out Uri? issuerUri) &&
                string.Equals(issuerUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) &&
                issuerUri.IsDefaultPort &&
                string.Equals(issuerUri.Host, "sts.windows.net", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsSharedV1GraphResource(string resource)
        {
            return TryNormalizeResource(resource, out string? normalized) &&
                (string.Equals(normalized, "https://graph.microsoft.com", StringComparison.Ordinal) ||
                 string.Equals(normalized, "https://graph.microsoft.us", StringComparison.Ordinal));
        }

        private static bool TryNormalizeResource(string resource, out string? normalized)
        {
            normalized = null;
            if (Guid.TryParse(resource.TrimEnd('/'), out Guid appId))
            {
                normalized = appId.ToString("D", CultureInfo.InvariantCulture);
                return true;
            }

            if (!Uri.TryCreate(resource, UriKind.Absolute, out Uri? uri) ||
                !string.IsNullOrEmpty(uri.Query) ||
                !string.IsNullOrEmpty(uri.Fragment) ||
                !string.IsNullOrEmpty(uri.UserInfo))
            {
                return false;
            }

            string port = uri.IsDefaultPort ? string.Empty : $":{uri.Port}";
            normalized = $"{uri.Scheme.ToLowerInvariant()}://{uri.Host.ToLowerInvariant()}{port}{uri.AbsolutePath.TrimEnd('/')}";
            return true;
        }

        private static bool TryGetApplicationId(string resource, out Guid applicationId)
        {
            string normalized = resource.TrimEnd('/');
            if (Guid.TryParse(normalized, out applicationId))
            {
                return true;
            }

            const string AppIdUriPrefix = "api://";
            return normalized.StartsWith(AppIdUriPrefix, StringComparison.OrdinalIgnoreCase) &&
                Guid.TryParse(normalized.Substring(AppIdUriPrefix.Length), out applicationId);
        }

        private static bool TryGetMicrosoftGraphCloud(string resource, out string? cloud)
        {
            cloud = null;
            if (!TryNormalizeResource(resource, out string? normalized))
            {
                return false;
            }

            switch (normalized)
            {
                case "https://graph.microsoft.com":
                    cloud = "Public";
                    return true;
                case "https://graph.microsoft.us":
                case "https://dod-graph.microsoft.us":
                    cloud = "USGovernment";
                    return true;
                case "https://microsoftgraph.chinacloudapi.cn":
                    cloud = "China";
                    return true;
                default:
                    return false;
            }
        }

        private static bool TryGetIssuerCloud(string issuer, out string? cloud)
        {
            cloud = null;
            if (!Uri.TryCreate(issuer, UriKind.Absolute, out Uri? issuerUri) ||
                !string.Equals(issuerUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ||
                !issuerUri.IsDefaultPort)
            {
                return false;
            }

            switch (issuerUri.Host.ToLowerInvariant())
            {
                case "login.microsoftonline.com":
                    cloud = "Public";
                    return true;
                case "login.microsoftonline.us":
                    cloud = "USGovernment";
                    return true;
                case "login.partner.microsoftonline.cn":
                case "sts.chinacloudapi.cn":
                    cloud = "China";
                    return true;
                default:
                    return false;
            }
        }
    }
}
