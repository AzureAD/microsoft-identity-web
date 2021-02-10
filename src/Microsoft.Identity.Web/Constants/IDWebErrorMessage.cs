// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Constants related to the error messages.
    /// </summary>
    internal static class IDWebErrorMessage
    {
        // General IDW10000 = "IDW10000:"
        public const string HttpContextIsNull = "IDW10001: HttpContext is null. ";
        public const string HttpContextAndHttpResponseAreNull = "IDW10002: Current HttpContext and HttpResponse arguments are null. Pass an HttpResponse argument. ";

        // Configuration IDW10100 = "IDW10100:"
        public const string ProvideEitherScopeKeySectionOrScopes = "IDW10101: Either provide the '{0}' or the '{1}' to the 'AuthorizeForScopes'. ";
        public const string ScopeKeySectionIsProvidedButNotPresentInTheServicesCollection = "IDW10102: The {0} is provided but the IConfiguration instance is not present in the services collection. ";
        public const string NoScopesProvided = "IDW10103: No scopes provided in scopes... ";
        public const string ClientSecretAndCertficateNull =
               "IDW10104: Both client secret and client certificate cannot be null or whitespace, " +
               "and only ONE must be included in the configuration of the web app when calling a web API. " +
               "For instance, in the appsettings.json file. ";
        public const string BothClientSecretAndCertificateProvided = "IDW10105: Both client secret and client certificate, " +
                   "cannot be included in the configuration of the web app when calling a web API. ";
        public const string ConfigurationOptionRequired = "IDW10106: The '{0}' option must be provided. ";
        public const string ScopesNotConfiguredInConfigurationOrViaDelegate = "IDW10107: Scopes need to be passed-in either by configuration or by the delegate overriding it. ";
        public const string MissingRequiredScopesForAuthorizationFilter = "IDW10108: RequiredScope Attribute does not contain a value. The scopes need to be set on the controller, the page or action. See https://aka.ms/ms-id-web/required-scope-attribute. ";

        // Authorization IDW10200 = "IDW10200:"
        public const string NeitherScopeOrRolesClaimFoundInToken = "IDW10201: Neither scope or roles claim was found in the bearer token. ";
        public const string MissingRoles = "IDW10202: The 'roles' or 'role' claim does not contain roles '{0}' or was not found. ";
        public const string MissingScopes = "IDW10203: The 'scope' or 'scp' claim does not contain scopes '{0}' or was not found. ";
        public const string UnauthenticatedUser = "IDW10204: The user is unauthenticated. The HttpContext does not contain any claims. ";
        public const string BlazorServerBaseUriNotSet = "IDW10205: Using Blazor server but the base URI was not properly set. ";
        public const string BlazorServerUserNotSet = "IDW10206: Using Blazor server but the user was not properly set. ";
        public const string CalledApiScopesAreNull = "IDW10207: The CalledApiScopes cannot be null. ";
        public const string ScopesRequiredToCallMicrosoftGraph = "IDW10208: You need to either pass-in scopes to AddMicrosoftGraph, in the appsettings.json file, or with .WithScopes() on the Graph queries. See https://aka.ms/ms-id-web/microsoftGraph. ";

        // Token Validation IDW10300 = "IDW10300:"
        public const string IssuerMetadataUrlIsRequired = "IDW10301: Azure AD Issuer metadata address URL is required. ";
        public const string NoMetadataDocumentRetrieverProvided = "IDW10302: No metadata document retriever is provided. ";
        public const string IssuerDoesNotMatchValidIssuers = "IDW10303: Issuer: '{0}', does not match any of the valid issuers provided for this application. ";
        public const string B2CTfpIssuerNotSupported = "IDW10304: Microsoft Identity Web does not support a B2C issuer with 'tfp' in the URI. See https://aka.ms/ms-id-web/b2c-issuer for details. ";

        // Protocol IDW10400 = "IDW10400:"
        public const string TenantIdClaimNotPresentInToken = "IDW10401: Neither `tid` nor `tenantId` claim is present in the token obtained from Microsoft identity platform. ";
        public const string ClientInfoReturnedFromServerIsNull = "IDW10402: Client info returned from the server is null. ";
        public const string TokenIsNotJwtToken = "IDW10403: Token is not a JWT token. ";
        public const string ClientCredentialScopeParameterShouldEndInDotDefault =
            "IDW10404: 'scope' parameter should be of the form 'AppIdUri/.default'. See https://aka.ms/ms-id-web/daemon-scenarios. ";
        public const string ClientCredentialTenantShouldBeTenanted =
            "IDW10405: 'tenant' parameter should be a tenant ID or domain name, not 'common', or 'organizations'. See https://aka.ms/ms-id-web/daemon-scenarios. ";

        // MSAL IDW10500 = "IDW10500:"
        public const string ExceptionAcquiringTokenForConfidentialClient = "IDW10501: Exception acquiring token for a confidential client. ";
        public const string MicrosoftIdentityWebChallengeUserException = "IDW10502: An MsalUiRequiredException was thrown due to a challenge for the user. " +
           "See https://aka.ms/ms-id-web/ca_incremental-consent. ";

        // Encoding IDW10600 = "IDW10600:"
        public const string InvalidBase64UrlString = "IDW10601: Invalid Base64URL string. ";

        // Certificates IDW10700 = "IDW10700:"
        public const string OnlyPkcs12IsSupported = "IDW10701: Only PKCS #12 content type is supported. Found Content-Type: {0}. ";
        public const string IncorrectNumberOfUriSegments = "IDW10702: Number of URI segments is incorrect: {0}, URI: {1}. ";
        public const string InvalidCertificateStorePath = "IDW10703: Certificate store path must be of the form 'StoreLocation/StoreName'. " +
            "StoreLocation must be one of 'CurrentUser', 'CurrentMachine'. " +
            "StoreName must be empty or one of '{0}'. ";

        // Obsolete messages IDW10800 = "IDW10800:"
        public const string AadIssuerValidatorGetIssuerValidatorIsObsolete = "IDW10800: Use MicrosoftIdentityIssuerValidatorFactory.GetAadIssuerValidator. See https://aka.ms/ms-id-web/1.2.0";
        public const string VerifyUserHasAnyAcceptedScopeIsObsolete = "IDW10801: Use the RequiredScope Attribute on the controller, the page or the action. See https://aka.ms/ms-id-web/required-scope-attribute";
    }
}
