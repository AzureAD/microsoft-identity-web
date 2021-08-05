// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// General constants for Microsoft Identity Web.
    /// </summary>
    public static class Constants
    {
        /// <summary>
        /// LoginHint.
        /// Represents the preferred_username claim in the ID token.
        /// </summary>
        public const string LoginHint = "login_hint";

        /// <summary>
        /// DomainHint.
        /// Determined by the tenant Id.
        /// </summary>
        public const string DomainHint = "domain_hint";

        /// <summary>
        /// Claims.
        /// Determined from the signed-in user.
        /// </summary>
        public const string Claims = "claims";

        /// <summary>
        /// Bearer.
        /// Predominant type of access token used with OAuth 2.0.
        /// </summary>
        public const string Bearer = "Bearer";

        /// <summary>
        /// AzureAd.
        /// Configuration section name for AzureAd.
        /// </summary>
        public const string AzureAd = "AzureAd";

        /// <summary>
        /// AzureAdB2C.
        /// Configuration section name for AzureAdB2C.
        /// </summary>
        public const string AzureAdB2C = "AzureAdB2C";

        /// <summary>
        /// Scope.
        /// </summary>
        public const string Scope = "scope";

        /// <summary>
        /// Policy for B2C user flows.
        /// The name of the policy to check against a specific user flow.
        /// </summary>
        public const string Policy = "policy";

        // IssuerMetadata
        internal const string TenantDiscoveryEndpoint = "tenant_discovery_endpoint";
        internal const string ApiVersion = "api-version";
        internal const string Metadata = "metadata";

        // Metadata
        internal const string PreferredNetwork = "preferred_network";
        internal const string PreferredCache = "preferred_cache";
        internal const string Aliases = "aliases";

        // AadIssuerValidator
#pragma warning disable S1075 // URIs should not be hardcoded
        internal const string AzureADIssuerMetadataUrl = "https://login.microsoftonline.com/common/discovery/instance?authorization_endpoint=https://login.microsoftonline.com/common/oauth2/v2.0/authorize&api-version=1.1";
#pragma warning restore S1075 // URIs should not be hardcoded
        internal const string FallbackAuthority = "https://login.microsoftonline.com/";
        internal const string OidcEndpoint = "/.well-known/openid-configuration";

        // RegisterValidAudience
        internal const string Version = "ver";
        internal const string V1 = "1.0";
        internal const string V2 = "2.0";

        // ClaimsPrincipalExtension
        internal const string MsaTenantId = "9188040d-6c67-4c5b-b112-36a304b66dad";
        internal const string Consumers = "consumers";
        internal const string Organizations = "organizations";
        internal const string Common = "common";

        // ClientInfo
        internal const string ClientInfo = "client_info";
        internal const string One = "1";

        // Certificates
        internal const string MediaTypePksc12 = "application/x-pkcs12";
        internal const string PersonalUserCertificateStorePath = "CurrentUser/My";

        // Miscellaneous
        internal const string UserAgent = "User-Agent";
        internal const string JwtSecurityTokenUsedToCallWebApi = "JwtSecurityTokenUsedToCallWebAPI";
        internal const string PreferredUserName = "preferred_username";
        internal const string NameClaim = "name";
        internal const string Consent = "consent";
        internal const string ConsentUrl = "consentUri";
        internal const string Scopes = "scopes";
        internal const string ProposedAction = "proposedAction";
        internal const string Authorization = "Authorization";
        internal const string ApplicationJson = "application/json";
        internal const string ISessionStore = "ISessionStore";
        internal const string True = "True";
        internal const string InvalidClient = "invalid_client";
        internal const string InvalidKeyError = "AADSTS700027";

        // Blazor challenge URI
        internal const string BlazorChallengeUri = "MicrosoftIdentity/Account/Challenge?redirectUri=";

        // Microsoft Graph
        internal const string UserReadScope = "user.read";
        internal const string GraphBaseUrlV1 = "https://graph.microsoft.com/v1.0";
        internal const string DefaultGraphScope = "https://graph.microsoft.com/.default";

        // Telemetry headers
        internal const string TelemetryHeaderKey = "x-client-brkrver";
        internal const string IDWebSku = "IDWeb.";

        // Authorize for scopes attributes
        internal const string XReturnUrl = "x-ReturnUrl";
        internal const string XRequestedWith = "X-Requested-With";
        internal const string XmlHttpRequest = "XMLHttpRequest";
        internal const string RequiredScopesSetting = "@setting";

        // AccountController.Challenge parameters
        internal const string LoginHintParameter = "loginHint";
        internal const string DomainHintParameter = "domainHint";

        // CCS
        internal const string XAnchorMailbox = "x-anchormailbox";
        internal const string Upn = "upn";
    }
}
