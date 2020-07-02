// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Web
{
    internal static class ErrorMessage
    {
        public const string TenantIdClaimNotPresentInToken = "Neither `tid` nor `tenantId` claim is present in the token obtained from Microsoft identity platform.";
        public const string TokenIsNotJWTToken = "Token is not JWT token.";
        public const string ProvideEitherScopeKeySectionOrScopes = "Either provide the '{0}' or the '{1}' to the 'AuthorizeForScopes'. ";
        public const string ScopeKeySectionIsProvidedButNotPresentInTheServicesCollection = "The {0} is provided but the IConfiguration instance is not present in the services collection";
        public const string NoScopesProvided = "no scopes provided in scopes...";
        public const string NeitherScopeOrRolesClaimFoundInToken = "Neither scope or roles claim was found in the bearer token.";
        public const string IssuerMetadataURLIsRequired = "Azure AD Issuer metadata address URL is required";
        public const string NoMetadataDocumentRetrieverProvided = "No metadata document retriever is provided";
        public const string IssuerDoesNotMatchValidIssuers = "Issuer: '{0}', does not match any of the valid issuers provided for this application.";
        public const string ClientInfoReturnedFromServerIsNull = "client info returned from the server is null";
        public const string MissingClientCredentials = "missing_client_credentials";
        public const string DuplicateClientCredentials = "duplicate_client_credentials";
        public const string ClientSecretAndCertficateNull =
                "Both client secret & client certificate cannot be null or whitespace, " +
                "and ONE, must be included in the configuration of the web app when calling a web API. " +
                "For instance, in the appsettings.json file. ";
        public const string BothClientSecretAndCertificateProvided = "Both Client secret & client certificate, " +
                   "cannot be included in the configuration of the web app when calling a web API. ";
        public const string ExceptionAcquiringTokenForConfidentialClient = "Exception acquiring token for a confidential client. ";
    }
}
