// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
// #define UseMicrosoftGraphSdk

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;
#if UseMicrosoftGraphSdk
using Microsoft.Graph;
#endif

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
            tokenAcquirerFactory.Services
#if UseMicrosoftGraphSdk
                .AddMicrosoftGraph();
#else
                .AddDownstreamRestApi("GraphBeta", tokenAcquirerFactory.Configuration.GetSection("GraphBeta"));
#endif

            // Add a distributed cache if you wish
            // services.AddDistributedTokenCaches();

            var serviceProvider = tokenAcquirerFactory.Build();

#if UseMicrosoftGraphSdk
            GraphServiceClient graphServiceClient = serviceProvider.GetRequiredService<GraphServiceClient>();
            var users = await graphServiceClient.Users
                .Request()
                .WithAppOnly()
                // Change the protocol if you wish .WithAuthenticationOptions(options => options.ProtocolScheme = "Bearer")
                .GetAsync();
            Console.WriteLine($"{users.Count} users");
#else
            // Call downstream web API
            var downstreamRestApi = serviceProvider.GetRequiredService<IDownstreamRestApi>();
            var httpResponseMessage = await downstreamRestApi.CallRestApiForAppAsync("GraphBeta", options => 
            {
                options.BaseUrl = "https://graph.microsoft.com/beta";
                options.Scopes = new string[] { "https://graph.microsoft.com/.default" };
            }).ConfigureAwait(false);

            // Get the token acquisition service
            ITokenAcquirer tokenAcquirer = tokenAcquirerFactory.GetTokenAcquirer();
            var result = await tokenAcquirer.GetTokenForAppAsync("https://graph.microsoft.com/.default");
            Console.WriteLine($"Token expires on {result.ExpiresOn}");

            // Get the authorization request creator service
            IAuthorizationHeaderProvider authorizationHeaderProvider = serviceProvider.GetRequiredService<IAuthorizationHeaderProvider>();
            string authorizationHeader = await authorizationHeaderProvider.CreateAuthorizationHeaderForAppAsync("https://graph.microsoft.com/.default");
            Console.WriteLine(authorizationHeader.Substring(0, authorizationHeader.IndexOf(" ")+4)+"...");
#endif
        }
    }
}
