// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.Test.Common;
using Microsoft.Identity.Web.Test.LabInfrastructure;
using Microsoft.Identity.Web.TokenCacheProviders;

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

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                    .AddMicrosoftIdentityWebApi(options =>
                    { 
                    },
                    options =>
                    {
                        options.ClientId = TestConstants.ConfidentialClientId;
                        options.TenantId = TestConstants.ConfidentialClientLabTenant;
                        options.Instance = TestConstants.AadInstance;
                        options.ClientSecret = keyVault.GetSecret(TestConstants.ConfidentialClientKeyVaultUri).Value;
                    })
                    .EnableTokenAcquisitionToCallDownstreamApi(options =>
                    {
                        options.ClientId = TestConstants.ConfidentialClientId;
                        options.TenantId = TestConstants.ConfidentialClientLabTenant;
                        options.Instance = TestConstants.AadInstance;
                        options.ClientSecret = keyVault.GetSecret(TestConstants.ConfidentialClientKeyVaultUri).Value;
                    })
                    .AddInMemoryTokenCaches();

            IServiceProvider serviceProvider = services.BuildServiceProvider();          

            IMsalTokenCacheProvider msalTokenCacheProvider = serviceProvider.GetRequiredService<IMsalTokenCacheProvider>();

            RunTestsAsync(
                msalTokenCacheProvider,
                serviceProvider.GetRequiredService<ILogger<Program>>(),
                serviceProvider).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        public static async Task RunTestsAsync(
            IMsalTokenCacheProvider msalTokenCacheProvider,
            ILogger<Program> logger,
            IServiceProvider serviceProvider)
        {
            var result = await AcquireTokenForLabUserAsync(msalTokenCacheProvider);

            ITokenAcquisition tokenAcquisition = serviceProvider.GetRequiredService<ITokenAcquisition>();


            string token = await tokenAcquisition.GetAccessTokenForUserAsync(
                s_scopes,
                null,
                null,
                ClaimsPrincipalFactory.FromTenantIdAndObjectId(
                    result.Account.HomeAccountId.TenantId,
                    result.Account.HomeAccountId.ObjectId));
        }

        private static async Task<AuthenticationResult> AcquireTokenForLabUserAsync(IMsalTokenCacheProvider msalTokenCacheProvider)
        {
            var labResponse = await LabUserHelper.GetDefaultUserAsync().ConfigureAwait(false);
            var msalPublicClient = PublicClientApplicationBuilder
               .Create(labResponse.App.AppId)
               .WithAuthority(labResponse.Lab.Authority, TestConstants.Organizations)
               .Build();

            await msalTokenCacheProvider.InitializeAsync(msalPublicClient.UserTokenCache);

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
