// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Identity.Abstractions
{
    /// <summary>
    /// Options for configuring authentication in a web app, web API or daemon app.
    /// <para>
    /// This class contains generic parameters applying to any OAuth 2.0 identity provider.
    /// For Azure AD specific options see the derived class: <see cref="MicrosoftAuthenticationOptions"/>.
    /// </para>
    /// </summary>
    /// <example></example>
    public class AuthenticationOptions
    {
        /// <summary>
        /// Gets or sets the authority to use when calling the STS. 
        /// If using AzureAD, rather use <see cref="MicrosoftAuthenticationOptions.Instance"/>
        /// and <see cref="MicrosoftAuthenticationOptions.TenantId"/>
        /// </summary>
        public virtual string? Authority { get; set; }

        /// <summary>
        /// Gets or sets the 'client_id' (application ID) as it appears in the 
        /// application registration. This is the string representation of a GUID.
        /// </summary>
        public string? ClientId
        {
            get;
            set;
        }

        /// <summary>
        /// Flag to enable/disable logging of Personally Identifiable Information (PII).
        /// PII logs are never written to default outputs like Console, Logcat or NSLog.
        /// Default is set to <c>false</c>, which ensures that your application is compliant with GDPR. You can set
        /// it to <c>true</c> for advanced debugging requiring PII. See https://aka.ms/msal-net-logging.
        /// </summary>
        public bool EnablePiiLogging { get; set; }

        #region Token Acquisition
        /// <summary>
        /// Does the app provide client credentials.
        /// </summary>
        public bool HasClientCredentials
        {
            get => (ClientCredentials != null && ClientCredentials.Any());
        }

        /// <summary>
        /// Description of the certificates used to prove the identity of the web app or web API.
        /// </summary>
        /// <example> An example in the appsetting.json:
        /// <code>
        /// "ClientCredentials": [
        ///   {
        ///     "SourceType": "StoreWithDistinguishedName",
        ///      "CertificateStorePath": "CurrentUser/My",
        ///      "CertificateDistinguishedName": "CN=WebAppCallingWebApiCert"
        ///     }
        ///    ]
        ///   </code>
        ///   See also https://aka.ms/ms-id-web-certificates.
        ///   </example>
        public IEnumerable<CredentialDescription>? ClientCredentials { get; set; }

        /// <summary>
        /// Specifies if the x5c claim (public key of the certificate) should be sent to the STS.
        /// Sending the x5c enables application developers to achieve easy certificate rollover in Azure AD:
        /// this method will send the public certificate to Azure AD along with the token request,
        /// so that Azure AD can use it to validate the subject name based on a trusted issuer policy.
        /// This saves the application admin from the need to explicitly manage the certificate rollover
        /// (either via the app registration portal or using PowerShell/CLI). 
        /// For details see https://aka.ms/msal-net-sni.
        /// </summary>
        /// The default is <c>false</c>.
        public bool SendX5C { get; set; }

        /// <summary>
        /// If set to <c>true</c>, when the user signs-in in a web app, the application requests an auth code 
        /// for the frontend (single page application using MSAL.js for instance). This will allow the front end
        /// JavaScript code to bypass going to the authoriize endpoint (which requires reloading the page), by 
        /// directly redeeming the auth code to get access tokens to call APIs.
        /// See https://aka.ms/msal-net/spa-auth-code for details.
        /// </summary>
        /// The default is <c>false.</c>
        public bool WithSpaAuthCode { get; set; }
        #endregion Token Acquisition

        #region web API
        /// <summary>
        /// In a web API, audience of the tokens that will be accepted by the web API.
        /// <para>If your web API accepts several audiences, see <see cref="Audiences"/>.</para>
        /// </summary>
        public string? Audience { get; set; }

        /// <summary>
        /// In a web API, accepted audiences for the tokens received by the web API.
        /// <para>See also <see cref="Audience"/>.</para>
        /// </summary>
        public IEnumerable<string>? Audiences { get; set; }

        /// <summary>
        /// Description of the certificates used to decrypt an encrypted token in a web API.
        /// </summary>
        /// <example> An example in the appsetting.json:
        /// <code>
        /// "TokenDecryptionCredentials": [
        ///   {
        ///     "SourceType": "StoreWithDistinguishedName",
        ///      "CertificateStorePath": "CurrentUser/My",
        ///      "CertificateDistinguishedName": "CN=WebAppCallingWebApiCert"
        ///     }
        ///    ]
        ///   </code>
        ///   See also https://aka.ms/ms-id-web-certificates.
        ///   </example>
        public IEnumerable<CredentialDescription>? TokenDecryptionCredentials { get; set; }

        /// <summary>
        /// Web APIs called by daemon applications can validate a token based on roles (representing app permissions), 
        /// or using the ACL-based authorization pattern for the client (daemon) to the web API. If using ACL-based authorization,
        /// the implementation will not throw if roles or scopes are not in the Claims.
        /// For details see https://aka.ms/ms-identity-web/daemon-ACL.
        /// </summary>
        /// The default is <c>false.</c>
        public bool AllowWebApiToBeAuthorizedByACL { get; set; }
        #endregion web API
    }
}
