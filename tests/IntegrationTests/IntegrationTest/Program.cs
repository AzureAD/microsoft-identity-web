// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using IntegrationTest.ClientBuilder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web.Test.Common;
using Microsoft.Identity.Web.Test.LabInfrastructure;

namespace IntegrationTest
{
    class Program
    {
        private static HttpClient httpClient = new HttpClient();
        private const string Authority = "https://login.microsoftonline.com/organizations/";
        private static readonly string[] s_scopes = { "User.Read" };

        public static void Main(string[] args)
        {
            RunIntegrationTestLogic();
        }

        public static void RunIntegrationTestLogic()
        {
            KeyVaultSecretsProvider keyVault = new KeyVaultSecretsProvider();

            IServiceCollection services = new ServiceCollection();

            services.AddDistributedMemoryCache();

            services.AddLogging(loggingBuilder =>
            {
                loggingBuilder.AddConsole();
                loggingBuilder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Information);
            });

            services.AddMicrosoftIdentityConfidentialClient((clientApplicationBuilderOptions) =>
            {
                clientApplicationBuilderOptions.ClientId = TestConstants.ConfidentialClientId;
                clientApplicationBuilderOptions.ClientSecret = keyVault.GetSecret(TestConstants.ConfidentialClientKeyVaultUri).Value;
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
            var result = await AcquireTokenForLabUserAsync();
        }

        private static async Task<AuthenticationResult> AcquireTokenForLabUserAsync()
        {
            var labResponse = await LabUserHelper.GetDefaultUserAsync().ConfigureAwait(false);
            var msalPublicClient = PublicClientApplicationBuilder
               .Create(labResponse.App.AppId)
               .WithAuthority(labResponse.Lab.Authority, TestConstants.Organizations)
               .Build();

            AuthenticationResult authResult = await msalPublicClient
                .AcquireTokenByUsernamePassword(s_scopes, labResponse.User.Upn, 
                new NetworkCredential(
                    labResponse.User.Upn, 
                    labResponse.User.GetOrFetchPassword()).SecurePassword)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            return authResult;
        }
    }
}
