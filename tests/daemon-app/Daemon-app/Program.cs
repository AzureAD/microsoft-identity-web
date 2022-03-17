// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

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
        static void Main(string[] args)
        {
            try
            {
                RunAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.Message);
                Console.ResetColor();
            }

            Console.WriteLine("Press any key to exit");
            Console.ReadKey();
        }

        private static async Task RunAsync()
        {
            // Read the configuration from a file
            IConfiguration Configuration;
            var builder = new ConfigurationBuilder()
             .SetBasePath(System.IO.Directory.GetCurrentDirectory())
             .AddJsonFile("appsettings.json");
            Configuration = builder.Build();

            // Add services
            ServiceCollection services = new ServiceCollection();

            services.Configure<MicrosoftIdentityOptions>(option => Configuration.Bind(option));
            services.AddMicrosoftGraph();
            services.AddDistributedTokenCaches();
            services.AddDistributedMemoryCache(); /* or SQL, Redis, ... */

            var serviceProvider = services.BuildServiceProvider();




#if WithTokenAcquisition
            AuthenticationResult result = null;
            // Get the token acquisition service
            ITokenAcquisition? tokenAcquisition = serviceProvider.GetService<ITokenAcquisition>();
            string scope = Configuration.GetValue<string>("Scopes");
            result = await tokenAcquisition.GetAuthenticationResultForAppAsync(scope, null);
#else
            GraphServiceClient graphServiceClient = serviceProvider.GetRequiredService<GraphServiceClient>();
            var users = await graphServiceClient.Users
                .Request()
                .WithAppOnly()
                .GetAsync();

            Console.WriteLine($"{users.Count} users");
#endif
        }
    }
}
