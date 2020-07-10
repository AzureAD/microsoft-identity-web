// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Web
{
    internal static class IDWebErrorMessage
    {
        // general
        // public const string IDW10000 = "IDW10000:";

        // Configuration IDW10100 = "IDW10100:"
        public const string ProvideEitherScopeKeySectionOrScopes = "IDW10101: Either provide the '{0}' or the '{1}' to the 'AuthorizeForScopes'. ";
        public const string ScopeKeySectionIsProvidedButNotPresentInTheServicesCollection = "IDW10102: The {0} is provided but the IConfiguration instance is not present in the services collection. ";
        public const string NoScopesProvided = "IDW10103: No scopes provided in scopes... ";
        public const string ClientSecretAndCertficateNull =
               "IDW10104: Both client secret & client certificate cannot be null or whitespace, " +
               "and ONE, must be included in the configuration of the web app when calling a web API. " +
               "For instance, in the appsettings.json file. ";
        public const string BothClientSecretAndCertificateProvided = "IDW10105: Both Client secret & client certificate, " +
                   "cannot be included in the configuration of the web app when calling a web API. ";

        // Authorization IDW10200 = "IDW10200:"
        public const string NeitherScopeOrRolesClaimFoundInToken = "IDW10201: Neither scope or roles claim was found in the bearer token. ";

        // Token Validation IDW10300 = "IDW10300:"
        public const string IssuerMetadataURLIsRequired = "IDW10301: Azure AD Issuer metadata address URL is required. ";
        public const string NoMetadataDocumentRetrieverProvided = "IDW10302: No metadata document retriever is provided. ";
        public const string IssuerDoesNotMatchValidIssuers = "IDW10303: Issuer: '{0}', does not match any of the valid issuers provided for this application. ";

        // Protocol IDW10400 = "IDW10400:"
        public const string TenantIdClaimNotPresentInToken = "IDW10401: Neither `tid` nor `tenantId` claim is present in the token obtained from Microsoft identity platform. ";
        public const string ClientInfoReturnedFromServerIsNull = "IDW10402: Client info returned from the server is null. ";
        public const string TokenIsNotJWTToken = "IDW10403: Token is not JWT token. ";

        // MSAL IDW10500 = "IDW10500:"
        public const string ExceptionAcquiringTokenForConfidentialClient = "IDW10501: Exception acquiring token for a confidential client. ";
    }
}
