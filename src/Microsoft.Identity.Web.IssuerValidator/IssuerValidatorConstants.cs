// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// General constants for Microsoft Identity Web Issuer Validator.
    /// </summary>
    internal class IssuerValidatorConstants
    {
        public const string MsaTenantId = "9188040d-6c67-4c5b-b112-36a304b66dad";
        public const string Consumers = "consumers";
        public const string Organizations = "organizations";
        public const string Common = "common";
        public const string OidcEndpoint = "/.well-known/openid-configuration";
        public const string FallbackAuthority = "https://login.microsoftonline.com/";

        /// <summary>
        /// Old TenantId claim: "http://schemas.microsoft.com/identity/claims/tenantid".
        /// </summary>
        public const string TenantId = "http://schemas.microsoft.com/identity/claims/tenantid";

        /// <summary>
        /// New Tenant Id claim: "tid".
        /// </summary>
        public const string Tid = "tid";

        /// <summary>
        /// Tfp claim: "tfp".
        /// </summary>
        public const string Tfp = "tfp";
    }
}
