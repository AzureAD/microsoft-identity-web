// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Threading.Tasks;
using Azure.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.TokenCacheProviders.InMemory;
using Xunit;

namespace TokenAcquirerTests
{
#if !FROM_GITHUB_ACTION
    public class TokenAcquirer
    {
        private static readonly string s_optionName = string.Empty;
        private static readonly CredentialDescription[] s_clientCredentials = new[]
        {
            CertificateDescription.FromKeyVault(
                "https://webappsapistests.vault.azure.net",
                "Self-Signed-5-5-22")
        };

        [IgnoreOnAzureDevopsFact]
        //[Theory]
        //[InlineData(false)]
        //[InlineData(true)]
        public async Task AcquireToken_WithMicrosoftIdentityOptions_ClientCredentialsAsync(/*bool withClientCredentials*/)
        {
            bool withClientCredentials = false; //add as param above
            TokenAcquirerFactory tokenAcquirerFactory = TokenAcquirerFactory.GetDefaultInstance();
            IServiceCollection services = tokenAcquirerFactory.Services;


            services.Configure<MicrosoftIdentityOptions>(s_optionName, option =>
            {
                option.Instance = "https://login.microsoftonline.com/";
                option.TenantId = "msidentitysamplestesting.onmicrosoft.com";
                option.ClientId = "6af093f3-b445-4b7a-beae-046864468ad6";
                if (withClientCredentials)
                {
                    option.ClientCertificates = s_clientCredentials.OfType<CertificateDescription>();
                }
                else
                {
                    option.ClientCredentials = s_clientCredentials;
                }
            });

            await CreateGraphClientAndAssert(tokenAcquirerFactory, services);
        }

        [IgnoreOnAzureDevopsFact]
        //[Fact]
        public async Task AcquireToken_WithMicrosoftAuthenticationOptions_ClientCredentialsAsync()
        {
            TokenAcquirerFactory tokenAcquirerFactory = TokenAcquirerFactory.GetDefaultInstance();
            IServiceCollection services = tokenAcquirerFactory.Services;

            services.Configure<MicrosoftAuthenticationOptions>(s_optionName, option =>
            {
                option.Instance = "https://login.microsoftonline.com/";
                option.TenantId = "msidentitysamplestesting.onmicrosoft.com";
                option.ClientId = "6af093f3-b445-4b7a-beae-046864468ad6";
                option.ClientCredentials = s_clientCredentials;
            });

            await CreateGraphClientAndAssert(tokenAcquirerFactory, services);
        }

        [IgnoreOnAzureDevopsFact]
        // [Fact]
        public async Task AcquireToken_WithFactoryAndMicrosoftAuthenticationOptions_ClientCredentialsAsync()
        {
            TokenAcquirerFactory tokenAcquirerFactory = TokenAcquirerFactory.GetDefaultInstance();
            tokenAcquirerFactory.Services.AddInMemoryTokenCaches();
            tokenAcquirerFactory.Build();

            // Get the token acquirer from the options.
            var tokenAcquirer = tokenAcquirerFactory.GetTokenAcquirer(new MicrosoftAuthenticationOptions
            {
                ClientId = "6af093f3-b445-4b7a-beae-046864468ad6",
                Authority = "https://login.microsoftonline.com/msidentitysamplestesting.onmicrosoft.com",
                ClientCredentials = s_clientCredentials
            });
           
            var result = await tokenAcquirer.GetTokenForAppAsync("https://graph.microsoft.com/.default");
            Assert.False(string.IsNullOrEmpty(result.AccessToken));
        }

        [IgnoreOnAzureDevopsFact]
        // [Fact]
        public async Task AcquireToken_WithFactoryAndAuthorityClientIdCert_ClientCredentialsAsync()
        {
            TokenAcquirerFactory tokenAcquirerFactory = TokenAcquirerFactory.GetDefaultInstance();
            tokenAcquirerFactory.Services.AddInMemoryTokenCaches();
            tokenAcquirerFactory.Build();

            var tokenAcquirer = tokenAcquirerFactory.GetTokenAcquirer(
                authority: "https://login.microsoftonline.com/msidentitysamplestesting.onmicrosoft.com",
                clientId: "6af093f3-b445-4b7a-beae-046864468ad6",
                clientCredentials: s_clientCredentials);

            var result = await tokenAcquirer.GetTokenForAppAsync("https://graph.microsoft.com/.default");
            Assert.False(string.IsNullOrEmpty(result.AccessToken));
        }

        [IgnoreOnAzureDevopsFact]
        //[Fact]
        public async Task AcquireTokenWithPop_ClientCredentialsAsync()
        {
            TokenAcquirerFactory tokenAcquirerFactory = TokenAcquirerFactory.GetDefaultInstance();
            IServiceCollection services = tokenAcquirerFactory.Services;

            services.Configure<MicrosoftAuthenticationOptions>(s_optionName, option =>
            {
                option.Instance = "https://login.microsoftonline.com/";
                option.TenantId = "msidentitysamplestesting.onmicrosoft.com";
                option.ClientId = "6af093f3-b445-4b7a-beae-046864468ad6";
                option.ClientCredentials = s_clientCredentials;
            });

            services.AddInMemoryTokenCaches();
            var serviceProvider = tokenAcquirerFactory.Build();
            var options = serviceProvider.GetRequiredService<IOptions<MicrosoftAuthenticationOptions>>().Value;
            var cert = options.ClientCredentials!.First().Certificate;

            // Get the token acquisition service
            ITokenAcquirer tokenAcquirer = tokenAcquirerFactory.GetTokenAcquirer(string.Empty);
            var result = await tokenAcquirer.GetTokenForAppAsync("https://graph.microsoft.com/.default", new TokenAcquisitionOptions() { PopPublicKey = cert?.GetPublicKeyString() });
            Assert.NotNull(result.AccessToken);
        }

        private static async Task CreateGraphClientAndAssert(TokenAcquirerFactory tokenAcquirerFactory, IServiceCollection services)
        {
            services.AddInMemoryTokenCaches();
            services.AddMicrosoftGraph();
            var serviceProvider = tokenAcquirerFactory.Build();
            GraphServiceClient graphServiceClient = serviceProvider.GetRequiredService<GraphServiceClient>();
            var users = await graphServiceClient.Users
                .Request()
                .WithAppOnly()
                //     .WithAuthenticationOptions(options => options.ProtocolScheme = "Pop")
                .GetAsync();
            Assert.Equal(51, users.Count);

            // Alternatively to calling Microsoft Graph, you can get a token acquirer service
            // and get a token, and use it in an SDK.
            ITokenAcquirer tokenAcquirer = tokenAcquirerFactory.GetTokenAcquirer(s_optionName);
            var result = await tokenAcquirer.GetTokenForAppAsync("https://graph.microsoft.com/.default");
            Assert.NotNull(result.AccessToken);
        }
    }
#endif //FROM_GITHUB_ACTION
}
