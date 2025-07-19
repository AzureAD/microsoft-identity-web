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
            var tokenAcquirer = tokenAcquirerFactory.GetTokenAcquirer(new MicrosoftIdentityApplicationOptions
            {
                ClientId = "f6b698c0-140c-448f-8155-4aa9bf77ceba",
                Authority = "https://login.microsoftonline.com/msidlab4.onmicrosoft.com",
                ClientCredentials = new[]
                {
                    new CredentialDescription()
                    {
                        SourceType = CredentialSource.StoreWithDistinguishedName,
                        CertificateStorePath = "LocalMachine/My",
                        CertificateDistinguishedName = "CN=LabAuth.MSIDLab.com"
                    }
                }
            });
            */
            // Or
            var tokenAcquirer = tokenAcquirerFactory.GetTokenAcquirer(
                authority: "https://login.microsoftonline.com/msidlab4.onmicrosoft.com",
                clientId: "f6b698c0-140c-448f-8155-4aa9bf77ceba",
                clientCredentials: new[]
                {
                    new CredentialDescription()
                    {
                        SourceType = CredentialSource.StoreWithDistinguishedName,
                        CertificateStorePath = "LocalMachine/My",
                        CertificateDistinguishedName = "CN=LabAuth.MSIDLab.com"
                    }
                }
                );

            // Get the token acquisition service
            var result = await tokenAcquirer.GetTokenForAppAsync("https://graph.microsoft.com/.default");
            Console.WriteLine($"Token expires on {result.ExpiresOn}");

            result = await tokenAcquirer.GetTokenForAppAsync("https://graph.microsoft.com/.default");
            Console.WriteLine($"Token expires on {result.ExpiresOn}");
        }
    }
}
