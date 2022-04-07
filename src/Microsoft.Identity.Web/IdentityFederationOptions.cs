// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Options for configuring Identity Federation options.
    /// See https://aka.ms/ms-id-web/identity-federation.
    /// </summary>
    public class IdentityFederationOptions
    {
        /// <summary>
        /// Is IdentityFederation enabled?
        /// </summary>
        /// The default is <c>false.</c>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// The value is used to establish a connection between external workload identities
        /// and Azure Active Directory. If Azure AD is the issuer, this value should be the object
        /// ID of the managed identity service principal in the tenant that will be used to 
        /// impersonate the app.
        /// Can be null if you are using the machine assigned managed identity.
        /// Needs to be assigned if you are using a user assigned managed identity.
        /// </summary>
        public string? SubjectIdentifier { get; set; }
    }
}
