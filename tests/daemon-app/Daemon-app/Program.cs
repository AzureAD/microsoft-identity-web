// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
//#define UseMicrosoftGraphSdk

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Graph;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.TokenCacheProviders.Distributed;

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
            IConfiguration configuration = ReadConfiguration();

            // Add services
            ServiceCollection services = new ServiceCollection();

            services.Configure<MicrosoftIdentityOptions>(option => configuration.Bind(option));
            services.AddMicrosoftGraph(); // or services.AddTokenAcquisition() if you don't need graph

            // Add a cache
            services.AddDistributedTokenCaches();
            services.AddDistributedMemoryCache(); /* or SQL, Redis, ... */

            var serviceProvider = services.BuildServiceProvider();

#if UseMicrosoftGraphSdk
            GraphServiceClient graphServiceClient = serviceProvider.GetRequiredService<GraphServiceClient>();
            var users = await graphServiceClient.Users
                .Request()
                .WithAppOnly()
                .GetAsync();
            Console.WriteLine($"{users.Count} users");
#else
            // Get the token acquisition service
            ITokenAcquirer tokenAcquirer = serviceProvider.GetRequiredService<ITokenAcquirer>();
            string scope = configuration.GetValue<string>("Scopes");
            var result = await tokenAcquirer.GetAuthenticationResultForAppAsync("https://graph.microsoft.com/.default");
            Console.WriteLine($"Token expires on {result.ExpiresOn}");

#endif
        }

        private static IConfiguration ReadConfiguration()
        {
            // Read the configuration from a file
            IConfiguration Configuration;
            var builder = new ConfigurationBuilder()
             .SetBasePath(System.IO.Directory.GetCurrentDirectory())
             .AddJsonFile("appsettings.json");
            Configuration = builder.Build();
            return Configuration;
        }
    }
}
