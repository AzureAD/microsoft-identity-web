// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Microsoft.Identity.Web.Test.Common
{
    public static class TestConstants
    {
        public const string ProductionPrefNetworkEnvironment = "login.microsoftonline.com";
        public const string ProductionPrefNetworkUSEnvironment = "login.microsoftonline.us";
        public const string ProductionNotPrefEnvironmentAlias = "sts.windows.net";

        public const string HttpLocalHost = "https://localhost";

        public const string ApiAudience = "api://" + ApiClientId;
        public const string ApiClientId = "1EE5A092-0DFD-42B6-88E5-C517C0141321";

        public const string UserOne = "User One";
        public const string UserTwo = "User Two";

        public const string ClientId = "87f0ee88-8251-48b3-8825-e0c9563f5234";
        public const string GuestTenantId = "guest-tenant-id";
        public const string HomeTenantId = "home-tenant-id";
        public const string TenantIdAsGuid = "f645ad92-e38d-4d1a-b510-d1b09a74a8ca";
        public const string ObjectIdAsGuid = "6364bb70-9521-3fa8-989d-c2c19ff90223";
        public const string Domain = "contoso.onmicrosoft.com";
        public const string Uid = "my-home-object-id";
        public const string Oid = "my-guest-object-id";
        public const string Utid = "my-home-tenant-id";
        public const string LoginHint = "login_hint";
        public const string DomainHint = "domain_hint";
        public const string Claims = "additional_claims";
        public const string PreferredUsername = "preferred_username";
        public const string Value = "value";

        public const string AadInstance = "https://login.microsoftonline.com";
        public const string AuthorityCommonTenant = AadInstance + "/common/";
        public const string AuthorityOrganizationsTenant = AadInstance + "/organizations/";
        public const string AuthorityOrganizationsUSTenant = "https://" + ProductionPrefNetworkUSEnvironment + "/organizations";
        public const string Organizations = "organizations";

        public const string AuthorityWithTenantSpecified = AadInstance + "/" + TenantIdAsGuid;
        public const string AuthorityCommonTenantWithV2 = AadInstance + "/common/v2.0";
        public const string AuthorityOrganizationsWithV2 = AadInstance + "/organizations/v2.0";
        public const string AuthorityOrganizationsUSWithV2 = AuthorityOrganizationsUSTenant + "/v2.0";
        public const string AuthorityWithTenantSpecifiedWithV2 = AadInstance + "/" + TenantIdAsGuid + "/v2.0";
        public const string AadIssuer = AadInstance + "/" + TenantIdAsGuid + "/v2.0";
        public const string UsGovIssuer = "https://login.microsoftonline.us/" + UsGovTenantId + "/v2.0";
        public const string UsGovTenantId = "72f988bf-86f1-41af-91ab-2d7cd011db47";
        public const string V1Issuer = "https://sts.windows.net/f645ad92-e38d-4d1a-b510-d1b09a74a8ca/";
        public const string GraphBaseUrlBeta = "https://graph.microsoft.com/beta";
        public const string GraphBaseUrl = "https://graph.microsoft.com/v1.0";

        // B2C
        public const string B2CSignUpSignInUserFlow = "b2c_1_susi";
        public const string B2CEditProfileUserFlow = "b2c_1_edit_profile";
        public const string B2CResetPasswordUserFlow = "b2c_1_reset";
        public const string B2CTenant = "fabrikamb2c.onmicrosoft.com";
        public const string B2CTenantAsGuid = "775527ff-9a37-4307-8b3d-cc311f58d925";
        public const string B2CHost = "fabrikamb2c.b2clogin.com";
        public const string B2CInstance = "https://fabrikamb2c.b2clogin.com";
        public const string B2CInstance2 = "https://catb2c.b2clogin.com";
        public const string B2CCustomDomainInstance = "https://public.msidlabb2c.com";
        public const string B2CLoginMicrosoft = "https://login.microsoftonline.com";
        public const string ClientSecret = "catsarecool";

        public const string B2CAuthority = B2CInstance + "/" + B2CTenant + "/" + B2CSignUpSignInUserFlow;
        public const string B2CAuthorityWithV2 = B2CAuthority + "/v2.0";
        public const string B2CCustomDomainAuthority = B2CCustomDomainInstance + "/" + B2CCustomDomainTenant + "/" + B2CCustomDomainUserFlow;
        public const string B2CCustomDomainAuthorityWithV2 = B2CCustomDomainAuthority + "/v2.0";

        public const string B2CIssuer = B2CInstance + "/" + B2CTenantAsGuid + "/v2.0";
        public const string B2CIssuer2 = B2CInstance2 + "/" + B2CTenantAsGuid + "/v2.0";
        public const string B2CCustomDomainIssuer = B2CCustomDomainInstance + "/" + B2CCustomDomainTenant + "/v2.0";
        public const string Scopes = "openid profile offline_access api://someapi";
        public const string B2CIssuerTfp = B2CInstance + "/" + ClaimConstants.Tfp + "/" + B2CTenantAsGuid + "/" + B2CSignUpSignInUserFlow + "/v2.0";
        public const string B2CCustomDomainTenant = "cpimtestpartners.onmicrosoft.com";
        public const string B2CCustomDomainUserFlow = "B2C_1_signupsignin_userflow";

        // CIAM
        public const string CIAMInstance = "https://catsareawesome.ciamlogin.com";
        public const string CIAMTenant = "aaaaaa-43bb-4ff9-89af-30ed8fe31c6d";
        public const string CIAMAuthority = CIAMInstance + "/" + CIAMTenant + "/v2.0";

        // Claims
        public const string ClaimNameTid = "tid";
        public const string ClaimNameIss = "iss";
        public const string ClaimNameTfp = "tfp"; // Trust Framework Policy for B2C (aka userflow/policy)

        public static readonly IEnumerable<string> s_aliases = [ProductionPrefNetworkEnvironment, ProductionNotPrefEnvironmentAlias];

        public static readonly string s_scopeForApp = "https://graph.microsoft.com/.default";

        public static readonly IEnumerable<string> s_userReadScope = ["user.read"];

        public const string InvalidScopeError = "The scope user.read is not valid.";
        public const string InvalidScopeErrorcode = "AADSTS70011";
        public const string InvalidScope = "invalid_scope";
        public const string GraphScopes = "user.write user.read.all";

        // Constants for the lab
        public const string OBOClientKeyVaultUri = "TodoListServiceV2-OBO";
        public const string ConfidentialClientKeyVaultUri = "https://msidlabs.vault.azure.net/secrets/LabVaultAccessCert/";
        public const string ConfidentialClientId = "f62c5ae3-bf3a-4af5-afa8-a68b800396e9";
        public const string ConfidentialClientLabTenant = "72f988bf-86f1-41af-91ab-2d7cd011db47";
        public const string OBOUser = "fIDLAB@msidlab4.com";
        public const string OBOClientSideClientId = "c0485386-1e9a-4663-bc96-7ab30656de7f";
        public static string[] s_oBOApiScope = new string[] { "api://f4aa5217-e87c-42b2-82af-5624dd14ee72/.default" };
        public const string LabClientId = "f62c5ae3-bf3a-4af5-afa8-a68b800396e9";
        public const string MSIDLabLabKeyVaultName = "https://msidlabs.vault.azure.net";
        public const string AzureADIdentityDivisionTestAgentSecret = "LabVaultAccessCert";
        public const string BuildAutomationKeyVaultName = "https://buildautomation.vault.azure.net/";
        public const string LabVaultAppId = "LabVaultAppID";
        public const string LabVaultAppSecret = "LabVaultAppSecret";

        // This value is only for testing purposes. It is for a certificate that is not used for anything other than running tests
        public const string CertificateX5c = @"MIIDHzCCAgegAwIBAgIQM6NFYNBJ9rdOiK+C91ZzFDANBgkqhkiG9w0BAQsFADAgMR4wHAYDVQQDExVBQ1MyQ2xpZW50Q2VydGlmaWNhdGUwHhcNMTIwNTIyMj
            IxMTIyWhcNMzAwNTIyMDcwMDAwWjAgMR4wHAYDVQQDExVBQ1MyQ2xpZW50Q2VydGlmaWNhdGUwggEiMA0GCSqGSIb3DQEBAQUAA4IBDwAwggEKAoIBAQCh7HjK
            YyVMDZDT64OgtcGKWxHmK2wqzi2LJb65KxGdNfObWGxh5HQtjzrgHDkACPsgyYseqxhGxHh8I/TR6wBKx/AAKuPHE8jB4hJ1W6FczPfb7FaMV9xP0qNQrbNGZU
            YbCdy7U5zIw4XrGq22l6yTqpCAh59DLufd4d7x8fCgUDV3l1ZwrncF0QrBRzns/O9Ex9pXsi2DzMa1S1PKR81D9q5QSW7LZkCgSSqI6W0b5iodx/a3RBvW3l7d
            noW2fPqkZ4iMcntGNqgsSGtbXPvUR3fFdjmg+xq9FfqWyNxShlZg4U+wE1v4+kzTJxd9sgD1V0PKgW57zyzdOmTyFPJFAgMBAAGjVTBTMFEGA1UdAQRKMEiAEM
            9qihCt+12P5FrjVMAEYjShIjAgMR4wHAYDVQQDExVBQ1MyQ2xpZW50Q2VydGlmaWNhdGWCEDOjRWDQSfa3ToivgvdWcxQwDQYJKoZIhvcNAQELBQADggEBAIm6
            gBOkSdYjXgOvcJGgE4FJkKAMQzAhkdYq5+stfUotG6vZNL3nVOOA6aELMq/ENhrJLC3rTwLOIgj4Cy+B7BxUS9GxTPphneuZCBzjvqhzP5DmLBs8l8qu10XAsh
            y1NFZmB24rMoq8C+HPOpuVLzkwBr+qcCq7ry2326auogvVMGaxhHlwSLR4Q1OhRjKs8JctCk2+5Qs1NHfawa7jWHxdAK6cLm7Rv/c0ig2Jow7wRaI5ciAcEjX7
            m1t9gRT1mNeeluL4cZa6WyVXqXc6U2wfR5DY6GOMUubN5Nr1n8Czew8TPfab4OG37BuEMNmBpqoRrRgFnDzVtItOnhuFTa0=";

        // This value is only for testing purposes. It is for a certificate that is not used for anything other than running tests and has a private key.
        public const string CertificateX5cWithPrivateKey = @"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";
        public const string CertificateX5cWithPrivateKeyPassword = "SelfSignedTestCert";

        public static string DecryptTokenCertificateDescriptionJson = "{" +
            "\"SourceType\": \"Base64Encoded\"," +
            $"\"Base64EncodedValue\": \"{CertificateX5c}\"," +
            "}"
            ;
        public const string KeyVaultContainer = "https://buildautomation.vault.azure.net";
        public const string KeyVaultReference = "AzureADIdentityDivisionTestAgentCert";

        // Integration tests
        public const string EmptyGetEmpty = "/Empty/GetEmpty";
        public const string TokenAcquisitionGetEmpty = "/TokenAcquisition/GetEmpty";
        public const string GraphClientGetEmpty = "/GraphClient/GetEmpty";
        public const string SecurePageGetEmpty = "/SecurePage/GetEmpty";
        public const string SecurePageGetTokenForUserAsync = "/SecurePage/GetTokenForUserAsync";
        public const string SecurePageGetTokenForAppAsync = "/SecurePage/GetTokenForAppAsync";
        public const string SecurePageCallDownstreamWebApi = "/SecurePage/CallDownstreamWebApiAsync";
        public const string SecurePageCallDownstreamWebApiGeneric = "/SecurePage/CallDownstreamWebApiGenericAsync";
        public const string SecurePageCallDownstreamWebApiGenericWithTokenAcquisitionOptions = "/SecurePage/CallDownstreamWebApiGenericWithTokenAcquisitionOptionsAsync";
        public const string SecurePageCallMicrosoftGraph = "/SecurePage/CallMicrosoftGraph";
        public const string SecurePage2GetEmpty = "/SecurePage2/GetEmpty";
        public const string SecurePage2GetTokenForUserAsync = "/SecurePage2/GetTokenForUserAsync";
        public const string SecurePage2GetTokenForAppAsync = "/SecurePage2/GetTokenForAppAsync";
        public const string SecurePage2CallDownstreamWebApi = "/SecurePage2/CallDownstreamWebApiAsync";
        public const string SecurePage2CallDownstreamWebApiGeneric = "/SecurePage2/CallDownstreamWebApiGenericAsync";
        public const string SecurePage2CallDownstreamWebApiGenericWithTokenAcquisitionOptions = "/SecurePage2/CallDownstreamWebApiGenericWithTokenAcquisitionOptionsAsync";
        public const string SecurePage2CallMicrosoftGraph = "/SecurePage2/CallMicrosoftGraph";
        public const string SectionNameCalledApi = "CalledApi";
        public const string CustomJwtScheme = "customJwt";
        public const string CustomJwtScheme2 = "customJwt2";

        // UI Testing Automation
        public const string WebSubmitId = "idSIButton9";
        public const string WebUPNInputId = "i0116";
        public const string WebPasswordId = "i0118";
        public const string ConsentAcceptId = "idBtn_Accept";
        public const string StaySignedInNoId = "idBtn_Back";
        public const string PhotoLabel = "photo";
        public const string Headless = "headless";
        public const string HeaderText = "Header";
        public const string EmailText = "Email";
        public const string PasswordText = "Password";
        public const string TodoTitle1 = "Testing create todo item";
        public const string TodoTitle2 = "Testing edit todo item";
        public const string LocalhostUrl = @"https://localhost:";
        public const string KestrelEndpointEnvVar = "Kestrel:Endpoints:Http:Url";
        public const string HttpStarColon = "http://*:";
        public const string HttpsStarColon = "https://*:";
        public const string WebAppCrashedString = $"The web app process has exited prematurely.";
        public static readonly string s_todoListClientExe = Path.DirectorySeparatorChar.ToString() + "TodoListClient.exe";
        public static readonly string s_todoListClientPath = Path.DirectorySeparatorChar.ToString() + "Client";
        public static readonly string s_todoListServiceExe = Path.DirectorySeparatorChar.ToString() + "TodoListService.exe";
        public static readonly string s_todoListServicePath = Path.DirectorySeparatorChar.ToString() + "TodoListService";


        // TokenAcqusitionOptions and ManagedIdentityOptions
        public static Guid s_correlationId = new Guid("6347d33d-941a-4c35-9912-a9cf54fb1b3e");
        public const string UserAssignedManagedIdentityClientId = "3b57c42c-3201-4295-ae27-d6baec5b7027";
        public const string UserAssignedManagedIdentityResourceId = "/subscriptions/c1686c51-b717-4fe0-9af3-24a20a41fb0c/" +
            "resourcegroups/MSAL_MSI/providers/Microsoft.ManagedIdentity/userAssignedIdentities/" + "MSAL_MSI_USERID";
        public const BindingFlags StaticPrivateFieldFlags = BindingFlags.GetField | BindingFlags.Static | BindingFlags.NonPublic;
        public const BindingFlags InstancePrivateFieldFlags = BindingFlags.GetField | BindingFlags.Instance | BindingFlags.NonPublic;
        public const BindingFlags StaticPrivateMethodFlags = BindingFlags.InvokeMethod | BindingFlags.Static | BindingFlags.NonPublic;

        // AadIssuerValidation
        public const string AadAuthority = "aadAuthority";
        public const string InvalidAuthorityFormat = "login.microsoft.com";
        public const string ActualIssuer = "actualIssuer";
        public const string SecurityToken = "securityToken";
        public const string ValidationParameters = "validationParameters";

        public const string DiscoveryJsonResponse = @"{
                        ""tenant_discovery_endpoint"":""https://login.microsoftonline.com/tenant/.well-known/openid-configuration"",
                        ""api-version"":""1.1"",
                        ""metadata"":[
                            {
                            ""preferred_network"":""login.microsoftonline.com"",
                            ""preferred_cache"":""login.windows.net"",
                            ""aliases"":[
                                ""login.microsoftonline.com"", 
                                ""login.windows.net"",
                                ""login.microsoft.com"",
                                ""sts.windows.net""]},
                            {
                            ""preferred_network"":""login.partner.microsoftonline.cn"",
                            ""preferred_cache"":""login.partner.microsoftonline.cn"",
                            ""aliases"":[
                                ""login.partner.microsoftonline.cn"",
                                ""login.chinacloudapi.cn""]},
                            {
                            ""preferred_network"":""login.microsoftonline.de"",
                            ""preferred_cache"":""login.microsoftonline.de"",
                            ""aliases"":[
                                    ""login.microsoftonline.de""]},
                            {
                            ""preferred_network"":""login.microsoftonline.us"",
                            ""preferred_cache"":""login.microsoftonline.us"",
                            ""aliases"":[
                                ""login.microsoftonline.us"",
                                ""login.usgovcloudapi.net""]},
                            {
                            ""preferred_network"":""login-us.microsoftonline.com"",
                            ""preferred_cache"":""login-us.microsoftonline.com"",
                            ""aliases"":[
                                ""login-us.microsoftonline.com""]}
                        ]
                }";

        public const string signedAssertionFilePath = "signedAssertion.txt";
    }
}
