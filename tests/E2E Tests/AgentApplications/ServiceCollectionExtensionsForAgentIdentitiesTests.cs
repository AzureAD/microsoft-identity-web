// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.TokenCacheProviders.InMemory;

#if !FROM_GITHUB_ACTION
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Client;
using Microsoft.IdentityModel.Tokens;
#endif

namespace AgentApplicationsTests
{
    public static class ServiceCollectionExtensionsForAgentIdentitiesTests
    {
        public static IServiceProvider ConfigureServicesForAgentIdentitiesTests(this IServiceCollection services)
        {
            services.AddSingleton(new ConfigurationBuilder().Build());
            services.AddTokenAcquisition(true);
            services.AddInMemoryTokenCaches();
            services.AddHttpClient();
            services.AddMicrosoftGraph();        // If you want to call Microsoft Graph
            services.AddAgentIdentities();
            return services.BuildServiceProvider();
        }

#if !FROM_GITHUB_ACTION
        internal static void ConfigureAgentApplication(
            this IServiceCollection services, string name, string clientId, string tenantId)
        {
            services.Configure<MicrosoftIdentityApplicationOptions>(name, options =>
            {
                options.Instance = "https://login.microsoftonline.com/";
                options.TenantId = tenantId;
                options.ClientId = clientId;
                options.ClientCredentials = [
                    CertificateDescription.FromStoreWithDistinguishedName(
                        "CN=LabAuth.MSIDLab.com", StoreLocation.LocalMachine, StoreName.My)
                ];
            });
        }

        internal static void AssertTokenIdentity(
            string authorizationHeader, string tenantId, string clientId,
            string? parentBlueprint)
        {
            Assert.True(authorizationHeader.StartsWith("Bearer ", StringComparison.Ordinal),
                "Expected a bearer authorization header.");
            var token = new JwtSecurityTokenHandler().ReadJwtToken(authorizationHeader.Substring("Bearer ".Length));
            var identity = new CaseSensitiveClaimsIdentity(token.Claims);

            Assert.Equal(tenantId, identity.FindFirst("tid")?.Value);
            Assert.Equal(clientId, identity.FindFirst("appid")?.Value ?? identity.FindFirst("azp")?.Value);
            Assert.Equal(parentBlueprint, identity.GetParentAgentBlueprint());
            Assert.False(identity.IsAgentUserIdentity());
        }

        internal static string AssertEntraAuthenticationDenied(MsalServiceException exception)
        {
            Assert.True(exception.StatusCode is 400 or 401,
                "Expected an authentication rejection, not a service availability failure.");
            Assert.True(exception.ErrorCode is "invalid_client" or "unauthorized_client" or "invalid_grant",
                "Expected an OAuth client or grant authorization rejection.");
            Match denial = Regex.Match(exception.Message, @"\bAADSTS[0-9]+\b", RegexOptions.CultureInvariant);
            Assert.True(denial.Success, "Expected an Entra authorization error code.");

            return denial.Value;
        }
#endif
    }
}
