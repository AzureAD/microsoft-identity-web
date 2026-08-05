// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Identity.Abstractions;
#if !NETSTANDARD2_0 && !NET462 && !NET472
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
#endif

#if NETSTANDARD2_0 || NET462 || NET472
using PathString = System.String;
#endif

namespace Microsoft.Identity.Web
{
#if NETSTANDARD2_0 || NET462 || NET472
    /// <summary>
    /// OpenIdConnect options
    /// </summary>
    public class OpenIdConnectOptions
    {
        /// <summary>
        /// Gets or sets the Authority to use when making OpenIdConnect calls.
        /// </summary>
        public string? Authority
        {
            get;
            set;
        }

        /// <summary>
        /// Client secret used to authenticate a confidential client app to AAD
        /// (alternatively use client certificates)
        /// </summary>
        public string? ClientSecret { get; set; }

        /// <summary>
        /// Gets or sets the 'client_id' (application ID) as appears in the 
        /// application registration.
        /// </summary>
        public string? ClientId
        {
            get;
            set;
        }
    }
#endif

    /// <summary>
    /// Options for configuring authentication using Azure Active Directory. It has both AAD and B2C configuration attributes.
    /// </summary>
    public class MicrosoftIdentityOptions : OpenIdConnectOptions
    {
        /// <summary>
        /// Gets or sets the Azure Active Directory instance, e.g. "https://login.microsoftonline.com".
        /// </summary>
        public string Instance { get; set; } = null!;

        /// <summary>
        /// Gets or sets the tenant ID.
        /// </summary>
        public string? TenantId { get; set; }

        /// <summary>
        /// Gets or sets the domain of the Azure Active Directory tenant, e.g. contoso.onmicrosoft.com.
        /// </summary>
        public string? Domain { get; set; }

        /// <summary>
        /// Gets or sets the edit profile user flow name for B2C, e.g. b2c_1_edit_profile.
        /// </summary>
        public string? EditProfilePolicyId { get; set; }

        /// <summary>
        /// Gets or sets the sign up or sign in user flow name for B2C, e.g. b2c_1_susi.
        /// </summary>
        public string? SignUpSignInPolicyId { get; set; }

        /// <summary>
        /// Gets or sets the reset password user flow name for B2C, e.g. B2C_1_password_reset.
        /// </summary>
        public string? ResetPasswordPolicyId { get; set; }

        /// <summary>
        /// Gets the default user flow (which is signUpSignIn).
        /// </summary>
        public string? DefaultUserFlow => SignUpSignInPolicyId;

        /// <summary>
        /// When set to <see langword="true"/>, the in-memory token cache uses MSAL's internal
        /// shared (static) cache instead of the <c>IMemoryCache</c> serialization provider.
        /// <para>
        /// Defaults to <see langword="false"/> (recommended). When <see langword="false"/>, tokens
        /// are stored through the in-memory serialization provider, so the cache honors per-entry
        /// expiry, the memory cache size limit, and cache-key partitioning.
        /// </para>
        /// <para>
        /// Set to <see langword="true"/> for the fast, process-wide static cache. It avoids
        /// per-request serialization but is <b>unbounded</b> (no eviction of expired tokens) and
        /// ignores the expiry/size options and the partitioning hook. This can be set via
        /// configuration (e.g. <c>"AzureAd": { "UseFastUnboundedCache": true }</c>).
        /// </para>
        /// </summary>
        public bool UseFastUnboundedCache { get; set; }

        /// <summary>
        /// When set to <see langword="true"/>, the application token cache (client credentials,
        /// i.e. <c>AcquireTokenForClient</c>) is partitioned by the audience (resource) of the
        /// requested token.
        /// <para>
        /// Applications that acquire app tokens for many resources using the same client and tenant
        /// otherwise accumulate all of those tokens in a single cache partition, which makes cache
        /// lookups O(n) in the number of resources. Enabling this adds the resource (derived from the
        /// requested <c>&lt;resource&gt;/.default</c> scope) as a non-protocol-affecting cache key
        /// component, so each resource gets its own partition and cache reads stay O(1).
        /// </para>
        /// <para>
        /// Defaults to <see langword="false"/>. This can be set via configuration, e.g.
        /// <c>"AzureAd": { "PartitionAppTokenCacheByAudience": true }</c>.
        /// </para>
        /// </summary>
        public bool PartitionAppTokenCacheByAudience { get; set; }

        /// <summary>
        /// When set to <see langword="true"/>, MSAL's internal in-memory token cache is disabled.
        /// Microsoft Identity Web builds the confidential client application with
        /// <see cref="Client.CacheOptions.DisableInternalCacheOptions"/> and does not wire an external
        /// (serialized) token cache. Use this in advanced confidential-client scenarios where the
        /// application manages its own token lifecycle.
        /// <para>
        /// When enabled, MSAL does not read from or write to any token cache, and the token-cache
        /// serialization callbacks are not invoked. Flows that rely on the cache throw: both
        /// <c>AcquireTokenSilent</c> and the long-running on-behalf-of process throw a
        /// <see cref="Client.MsalUiRequiredException"/> with error code
        /// <see cref="Client.MsalError.InternalCacheDisabled"/>. Retrieve the refresh token from the
        /// authentication result with
        /// <see cref="Client.Extensibility.AuthenticationResultExtensions.GetRefreshToken(Client.AuthenticationResult)"/>
        /// and redeem it with <c>AcquireTokenByRefreshToken</c> on subsequent requests.
        /// </para>
        /// <para>
        /// This is a confidential-client-only option and cannot be combined with the shared internal
        /// cache (<see cref="UseFastUnboundedCache"/> with the in-memory provider, or agent
        /// applications). Defaults to <see langword="false"/>. It can be set via configuration, e.g.
        /// <c>"AzureAd": { "DisableInternalCache": true }</c>.
        /// </para>
        /// </summary>
        public bool DisableInternalCache { get; set; }

        /// <summary>
        /// Enables legacy ADAL cache serialization and deserialization.
        /// Performance improvements when working with MSAL only apps.
        /// Set to true if you have a shared cache with ADAL apps.
        /// </summary>
        /// The default is <c>false.</c>
        public bool LegacyCacheCompatibilityEnabled { get; set; }

        /// <summary>
        /// Is considered B2C if the attribute SignUpSignInPolicyId is defined.
        /// </summary>
        internal bool IsB2C
        {
            get => !string.IsNullOrWhiteSpace(DefaultUserFlow);
        }

        /// <summary>
        /// Is considered to have client credentials if the attribute ClientCertificates
        /// or ClientSecret is defined.
        /// </summary>
        internal bool HasClientCredentials
        {
            get => !string.IsNullOrWhiteSpace(ClientSecret) || (ClientCertificates != null && ClientCertificates.Any());
        }

        /// <summary>
        /// Description of the certificates used to prove the identity of the web app or web API.
        /// </summary>
        /// <example> An example in the appsetting.json:
        /// <code>
        /// "ClientCertificates": [
        ///   {
        ///     "SourceType": "StoreWithDistinguishedName",
        ///      "CertificateStorePath": "CurrentUser/My",
        ///      "CertificateDistinguishedName": "CN=WebAppCallingWebApiCert"
        ///     }
        ///    ]
        ///   </code>
        ///   See also https://aka.ms/ms-id-web-certificates.
        ///   </example>
        public IEnumerable<CertificateDescription>? ClientCertificates { get; set; }

        /// <summary>
        /// Description of the certificates used to decrypt an encrypted token in a web API.
        /// </summary>
        /// <example> An example in the appsetting.json:
        /// <code>
        /// "TokenDecryptionCertificates": [
        ///   {
        ///     "SourceType": "StoreWithDistinguishedName",
        ///      "CertificateStorePath": "CurrentUser/My",
        ///      "CertificateDistinguishedName": "CN=WebAppCallingWebApiCert"
        ///     }
        ///    ]
        ///   </code>
        ///   See also https://aka.ms/ms-id-web-certificates.
        ///   </example>
        public IEnumerable<CertificateDescription>? TokenDecryptionCertificates { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public IEnumerable<CredentialDescription>? ClientCredentials { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public IEnumerable<CredentialDescription>? TokenDecryptionCredentials { get; set; }

        /// <summary>
        /// Specifies if the x5c claim (public key of the certificate) should be sent to the STS.
        /// Sending the x5c enables application developers to achieve easy certificate rollover in Azure AD:
        /// this method will send the public certificate to Azure AD along with the token request,
        /// so that Azure AD can use it to validate the subject name based on a trusted issuer policy.
        /// This saves the application admin from the need to explicitly manage the certificate rollover
        /// (either via portal or PowerShell/CLI operation). For details see https://aka.ms/msal-net-sni.
        /// </summary>
        /// The default is <c>false.</c>
        public bool SendX5C { get; set; }

        /// <summary>
        /// Requests an auth code for the frontend (SPA using MSAL.js for instance). 
        /// See https://aka.ms/msal-net/spa-auth-code for details.
        /// </summary>
        /// The default is <c>false.</c>
        public bool WithSpaAuthCode { get; set; }

        /// <summary>
        /// Daemon applications can validate a token based on roles, or using the ACL-based authorization
        /// pattern to control tokens without a roles claim. If using ACL-based authorization,
        /// Microsoft Identity Web will not throw if roles or scopes are not in the Claims.
        /// For details see https://aka.ms/ms-identity-web/daemon-ACL.
        /// </summary>
        /// The default is <c>false.</c>
        public bool AllowWebApiToBeAuthorizedByACL { get; set; }

        /// <summary>
        /// Used, when deployed to Azure, to specify explicitly a user assigned managed identity.
        /// See https://learn.microsoft.com/azure/active-directory/managed-identities-azure-resources/how-to-manage-ua-identity-portal.
        /// </summary>
        public string? UserAssignedManagedIdentityClientId { get; set; }

        /// <summary>
        /// Options for configuring certificateless
        /// </summary>
        public CertificatelessOptions? ClientCredentialsUsingManagedIdentity { get; set; }

        /// <summary>
        /// Sets the ResetPassword route path.
        /// Defaults to /MicrosoftIdentity/Account/ResetPassword,
        /// which is the value used by Microsoft.Identity.Web.UI.
        /// </summary>
        public PathString? ResetPasswordPath { get; set; } = Constants.ResetPasswordPath;

        /// <summary>
        /// Sets the Error route path.
        /// Defaults to the value /MicrosoftIdentity/Account/Error,
        /// which is the value used by Microsoft.Identity.Web.UI.
        /// </summary>
        public PathString? ErrorPath { get; set; } = Constants.ErrorPath;

        /// <summary>
        /// Sets query parameters for the query string in the HTTP request to the IdP.
        /// </summary>
        public IDictionary<string, string>? ExtraQueryParameters { get; set; }
    }
}
