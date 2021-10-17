// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Web
{
    internal class IssuerValidatorErrorMessage
    {
        // Token Validation IDW10300 = "IDW10300:"
        public const string IssuerMetadataUrlIsRequired = "IDW10301: Azure AD Issuer metadata address URL is required. ";
        public const string NoMetadataDocumentRetrieverProvided = "IDW10302: No metadata document retriever is provided. ";
        public const string IssuerDoesNotMatchValidIssuers = "IDW10303: Issuer: '{0}', does not match any of the valid issuers provided for this application. ";
        public const string B2CTfpIssuerNotSupported = "IDW10304: Microsoft Identity Web does not support a B2C issuer with 'tfp' in the URI. See https://aka.ms/ms-id-web/b2c-issuer for details. ";

        // Protocol IDW10400 = "IDW10400:"
        public const string TenantIdClaimNotPresentInToken = "IDW10401: Neither `tid` nor `tenantId` claim is present in the token obtained from Microsoft identity platform. ";
    }
}
