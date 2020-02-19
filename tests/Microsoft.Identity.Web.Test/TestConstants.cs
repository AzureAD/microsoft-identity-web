// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace Microsoft.Identity.Web.Test
{
    internal static class TestConstants
    {
        public const string ProductionPrefNetworkEnvironment = "login.microsoftonline.com";
        public const string ProductionPrefNetworkUSEnvironment = "login.microsoftonline.us";
        public const string ProductionNotPrefEnvironmentAlias = "sts.windows.net";

        public const string HttpLocalHost = "https://localhost";
        
        public const string ApiAudience = "api://" + ApiClientId;
        public const string ApiClientId = "1EE5A092-0DFD-42B6-88E5-C517C0141321";

        public const string TenantId = "some-tenant-id";
        public const string TenantIdAsGuid = "da41245a5-11b3-996c-00a8-4d99re19f292";
        public const string Domain = "contoso.onmicrosoft.com";

        public const string AadInstance = "https://login.microsoftonline.com";
        public const string AuthorityCommonTenant = AadInstance + "/common/";
        public const string AuthorityOrganizationsTenant = AadInstance + "/organizations/";
        public const string AuthorityOrganizationsUSTenant = "https://" + ProductionPrefNetworkUSEnvironment + "/organizations";

        public const string AuthorityWithTenantSpecified = AadInstance + "/" + TenantId;
        public const string AuthorityCommonTenantWithV2 = AadInstance + "/common/v2.0";
        public const string AuthorityOrganizationsWithV2 = AadInstance + "/organizations/v2.0";
        public const string AuthorityOrganizationsUSWithV2 = AuthorityOrganizationsUSTenant + "/v2.0";
        public const string AuthorityWithTenantSpecifiedWithV2 = AadInstance + "/" + TenantId + "/v2.0";
        public const string AadIssuer = AadInstance + "/" + TenantIdAsGuid + "/v2.0";
        
        // B2C
        public const string B2CSuSiUserFlow = "b2c_1_susi";
        public const string B2CTenant = "fabrikamb2c.onmicrosoft.com";
        public const string B2CTenantAsGuid = "775527ff-9a37-4307-8b3d-cc311f58d925";
        public const string B2CHost = "fabrikamb2c.b2clogin.com";
        public const string B2CInstance = "https://fabrikamb2c.b2clogin.com";
        public const string B2CInstance2 = "https://catb2c.b2clogin.com";
        public const string B2CCustomDomainInstance = "https://catsAreAmazing.com";

        public const string B2CAuthority = B2CInstance + "/" + B2CTenant + "/" + B2CSuSiUserFlow;
        public const string B2CAuthorityWithV2 = B2CAuthority + "/v2.0";
        public const string B2CCustomDomainAuthority = B2CCustomDomainInstance + "/" + B2CTenant + "/" + B2CSuSiUserFlow;
        public const string B2CCustomDomainAuthorityWithV2 = B2CCustomDomainAuthority + "/v2.0";

        public const string B2CIssuer = B2CInstance + "/" + B2CTenantAsGuid + "/v2.0";       
        public const string B2CIssuer2 = B2CInstance2 + "/" + B2CTenantAsGuid + "/v2.0";       
        public const string B2CCustomDomainIssuer = B2CCustomDomainInstance + "/" + B2CTenantAsGuid + "/v2.0";        

        // Claims
        public const string ClaimNameTid = "tid";
        public const string ClaimNameIss = "iss";
        public const string ClaimNameTfp = "tfp"; //Trust Framework Policy for B2C (aka userflow/policy)

        public static readonly IEnumerable<string> s_aliases = new[]
        {
            ProductionPrefNetworkEnvironment,
            ProductionNotPrefEnvironmentAlias
        };
    }
}