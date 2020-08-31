// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net.Http;
using System.Threading.Tasks;
using IntegrationTest.ClientBuilder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;

namespace IntegrationTest
{
    class Program
    {
        private static HttpClient httpClient = new HttpClient();

        static void Main(string[] args)
        {
            IServiceCollection services = new ServiceCollection();

            services.AddDistributedMemoryCache();

            services.AddLogging(loggingBuilder =>
            {
                loggingBuilder.AddConsole();
                loggingBuilder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Information);
            });

            services.AddMicrosoftIdentityConfidentialClient((clientApplicationBuilderOptions) =>
            {
                clientApplicationBuilderOptions.ClientId = "";
                clientApplicationBuilderOptions.ClientSecret = "";
            });

            IServiceProvider serviceProvider = services.BuildServiceProvider();

            IConfidentialClientApplication confidentialClientApplication = serviceProvider.GetRequiredService<IConfidentialClientApplication>();

            RunTestsAsync(
                confidentialClientApplication,
                serviceProvider.GetRequiredService<ILogger<Program>>()).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        public static async Task RunTestsAsync(
            IConfidentialClientApplication confidentialClientApplication,
            ILogger<Program> logger)
        {
            throw new NotImplementedException();
        }
    }
}
