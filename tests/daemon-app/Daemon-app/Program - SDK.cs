// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
//#define UseMicrosoftGraphSdk

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Graph;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.TokenCacheProviders.Distributed;
using Microsoft.Identity.Web.TokenCacheProviders.InMemory;

namespace daemon_console
{
    /// <summary>
    /// This sample shows how to query the Microsoft Graph from a daemon application
    /// which uses application permissions.
    /// For more information see https://aka.ms/msal-net-client-credentials
    /// </summary>
    class Program
    {
        static async Task Main(string[] args)
        {
            TokenAcquirerFactory tokenAcquirerFactory = TokenAcquirerFactory.GetDefaultInstance();
            tokenAcquirerFactory.Services.AddInMemoryTokenCaches();
            tokenAcquirerFactory.Build();

            /*
            var tokenAcquirer = tokenAcquirerFactory.GetTokenAcquirer(new MicrosoftAuthenticationOptions
            {
                ClientId = "6af093f3-b445-4b7a-beae-046864468ad6",
                Authority = "https://login.microsoftonline.com/msidentitysamplestesting.onmicrosoft.com",
                ClientCredentials = new[]
                {
                    new CredentialDescription()
                    {
                        SourceType = CredentialSource.KeyVault,
                        KeyVaultUrl = "https://webappsapistests.vault.azure.net",
                        KeyVaultCertificateName = "Self-Signed-5-5-22",
                    }
                }
            });
            */
            // Or
            var tokenAcquirer = tokenAcquirerFactory.GetTokenAcquirer(
                authority: "https://login.microsoftonline.com/msidentitysamplestesting.onmicrosoft.com",
                clientId: "6af093f3-b445-4b7a-beae-046864468ad6",
                region: null,
                certificate: CertificateDescription.FromKeyVault("https://webappsapistests.vault.azure.net", "Self-Signed-5-5-22")
                );

            // Get the token acquisition service
            var result = await tokenAcquirer.GetTokenForAppAsync("https://graph.microsoft.com/.default");
            Console.WriteLine($"Token expires on {result.ExpiresOn}");

            result = await tokenAcquirer.GetTokenForAppAsync("https://graph.microsoft.com/.default");
            Console.WriteLine($"Token expires on {result.ExpiresOn}");
        }
    }
}
