// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Graph;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.TokenCacheProviders.InMemory;
using Constants = Microsoft.Identity.Web.Constants;

namespace AgentApplicationsTests
{
    [Collection(nameof(TokenAcquirerFactorySingletonProtection))]
    public class GetFicAsyncTests
    {
        [Fact]
        public async Task AgentUserIdentityGetsTokenForGraphAsync()
        {
            IServiceCollection services = new ServiceCollection();
            IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();

            configuration["AzureAd:Instance"] = "https://login.microsoftonline.com/";
            configuration["AzureAd:TenantId"] = "31a58c3b-ae9c-4448-9e8f-e9e143e800df";
            configuration["AzureAd:ClientId"] = "d15884b6-a447-4dd5-a5a5-a668c49f6300"; // Agent application.
            configuration["AzureAd:ClientCredentials:0:SourceType"] = "StoreWithDistinguishedName";
            configuration["AzureAd:ClientCredentials:0:CertificateStorePath"] = "LocalMachine/My";
            configuration["AzureAd:ClientCredentials:0:CertificateDistinguishedName"] = "CN=LabAuth.MSIDLab.com";
            string agentIdentity = "d84da24a-2ea2-42b8-b5ab-8637ec208024"; // Replace with the actual agent identity
            string userUpn = "aui1@msidlabtoint.onmicrosoft.com";          // Replace with the actual user upn.

            services.AddSingleton(configuration);
            services.AddTokenAcquisition(true);
            services.AddInMemoryTokenCaches();
            services.AddHttpClient();
            services.Configure<MicrosoftIdentityOptions>(configuration.GetSection("AzureAd"));
            services.AddAgentIdentities();
            IServiceProvider serviceProvider = services.BuildServiceProvider();

            ITokenAcquirerFactory tokenAcquirerFactory = serviceProvider.GetRequiredService<ITokenAcquirerFactory>();
            ITokenAcquirer agentApplicationTokenAcquirer = tokenAcquirerFactory.GetTokenAcquirer();
            AcquireTokenResult aaFic = await agentApplicationTokenAcquirer.GetFicAsync(new() { FmiPath = agentIdentity }); // Uses the regular client credentials
            string? clientAssertion = aaFic.AccessToken;

            Assert.NotNull(clientAssertion);

            ITokenAcquirer agentIdentityTokenAcquirer = tokenAcquirerFactory.GetTokenAcquirer(new MicrosoftEntraApplicationOptions
            {
                ClientId = agentIdentity,
                Instance = configuration["AzureAd:Instance"],
                TenantId = configuration["AzureAd:TenantId"]
            });
            AcquireTokenResult aidFic = await agentIdentityTokenAcquirer.GetFicAsync(clientAssertion: clientAssertion); // Uses the agent identity
            string? userAssertion = aidFic.AccessToken;

            Assert.NotNull(userAssertion);

            AcquireTokenResult token = await agentApplicationTokenAcquirer.GetTokenForUserAsync(["https://graph.microsoft.com/user.read"],
                new AcquireTokenOptions()
                {
                    ExtraParameters = new Dictionary<string, object>()
                    {
                        { "xms_username",  userUpn },
                        { "xms_password", "default" }
                    }
                }.WithClientAssertion(userAssertion));
            Assert.NotNull(token);
        }
    }
}
