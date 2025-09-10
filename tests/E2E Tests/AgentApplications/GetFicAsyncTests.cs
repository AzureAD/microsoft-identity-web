// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
#if !FROM_GITHUB_ACTION

using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.Test.Common;

namespace AgentApplicationsTests
{
    [Collection(nameof(TokenAcquirerFactorySingletonProtection))]
    public class GetFicAsyncTests
    {
        [Fact]
        public async Task GetFicTokensTestsAsync()
        {
            string instance = "https://login.microsoftonline.com/";
            string tenantId = "31a58c3b-ae9c-4448-9e8f-e9e143e800df";         // Replace with your tenant ID
            string agentApplication = "d15884b6-a447-4dd5-a5a5-a668c49f6300"; // Replace with the actual agent application client ID
            string agentIdentity = "d84da24a-2ea2-42b8-b5ab-8637ec208024";    // Replace with the actual agent identity

            IServiceCollection services = new ServiceCollection();

            // Configure the information about the agent application
            services.Configure<MicrosoftIdentityApplicationOptions>(
                options =>
                {
                    options.Instance = instance;
                    options.TenantId = tenantId; // Replace with your tenant ID
                    options.ClientId = agentApplication; // Agent application.
                    options.ClientCredentials = [
                        CertificateDescription.FromStoreWithDistinguishedName(
                            "CN=LabAuth.MSIDLab.com", StoreLocation.LocalMachine, StoreName.My)
                    ];

                });

            IServiceProvider serviceProvider = services.ConfigureServicesForAgentIdentitiesTests();

            // Get a FIC token for the agent application
            ITokenAcquirerFactory tokenAcquirerFactory = serviceProvider.GetRequiredService<ITokenAcquirerFactory>();
            ITokenAcquirer agentApplicationTokenAcquirer = tokenAcquirerFactory.GetTokenAcquirer();
            AcquireTokenResult aaFic = await agentApplicationTokenAcquirer.GetFicTokenAsync(new() { FmiPath = agentIdentity }); // Uses the regular client credentials
            string? clientAssertion = aaFic.AccessToken;

            Assert.NotNull(clientAssertion);

            // Get a FIC token for the agent identity
            ITokenAcquirer agentIdentityTokenAcquirer = tokenAcquirerFactory.GetTokenAcquirer(new MicrosoftIdentityApplicationOptions
            {
                ClientId = agentIdentity,
                Instance = instance,
                TenantId = tenantId
            });
            AcquireTokenResult aidFic = await agentIdentityTokenAcquirer.GetFicTokenAsync(clientAssertion: clientAssertion); // Uses the agent identity
            string? userAssertion = aidFic.AccessToken;

            Assert.NotNull(userAssertion);
        }
    }
}
#endif // !FROM_GITHUB_ACTION
