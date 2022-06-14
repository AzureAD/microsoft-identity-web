// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Options for configuring authentication using Azure Active Directory. It has both AAD and B2C configuration attributes.
    /// </summary>
    public class ApplicationIdentityOptions
    {
        /// <summary>
        /// Gets or sets the Authority to use when making OpenIdConnect calls.
        /// </summary>
        public string? Authority
        {
            get { return _authority ?? $"{Instance}{TenantId}/v2.0"; }
            set { _authority = value; }
        }
        private string? _authority;

        /// <summary>
        /// Gets or sets the 'client_id' (application ID) as appears in the 
        /// application registration.
        /// </summary>
        public string? ClientId
        {
            get;
            set;
        }

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

        #region AADB2C
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
        /// Gets the default user flow (which is signUpsignIn).
        /// </summary>
        public string? DefaultUserFlow => SignUpSignInPolicyId;

        /// <summary>
        /// Is considered B2C if the attribute SignUpSignInPolicyId is defined.
        /// </summary>
        internal bool IsB2C
        {
            get => !string.IsNullOrWhiteSpace(DefaultUserFlow);
        }

        #endregion

        #region Token Acquisition
        /// <summary>
        /// Client secret used to authenticate a confidential client app to AAD
        /// (alternatively use client certificates)
        /// </summary>
        public string? ClientSecret { get; set; }

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
        public IEnumerable<CredentialDescription>? ClientCertificates { get; set; }

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
        public IEnumerable<CredentialDescription>? TokenDecryptionCertificates { get; set; }

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
        #endregion

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
        /// See https://docs.microsoft.com/azure/active-directory/managed-identities-azure-resources/how-to-manage-ua-identity-portal.
        /// </summary>
        public string? UserAssignedManagedIdentityClientId { get; set; }


        // TODO: reconcile with the CredentialDescription.
/*
        /// <summary>
        /// Options for configuring certificateless
        /// </summary>
        public CertificatelessOptions? ClientCredentialsUsingManagedIdentity { get; set; }
*/

        /// <summary>
        /// Sets the ResetPassword route path.
        /// Defaults to /MicrosoftIdentity/Account/ResetPassword,
        /// which is the value used by Microsoft.Identity.Web.UI.
        /// </summary>
        public string ResetPasswordPath { get; set; } = "/MicrosoftIdentity/Account/ResetPassword";

        /// <summary>
        /// Sets the Error route path.
        /// Defaults to the value /MicrosoftIdentity/Account/Error,
        /// which is the value used by Microsoft.Identity.Web.UI.
        /// </summary>
        public string ErrorPath { get; set; } = "/MicrosoftIdentity/Account/Error";
    }
}
