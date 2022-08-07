// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Graph;
using Microsoft.Graph.SecurityNamespace;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.TokenCacheProviders.InMemory;

namespace TokenAcquirerTests
{
    public class TokenAcquirer
    {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task AcquireToken_ClientCredentialsAsync(bool withMSIdentityOptions)
        {
            TokenAcquirerFactory tokenAcquirerFactory = TokenAcquirerFactory.GetDefaultInstance();
            IServiceCollection services = tokenAcquirerFactory.Services;

            if (withMSIdentityOptions)
            {
                services.Configure<MicrosoftIdentityOptions>(option =>
                {
                    option.Instance = "https://login.microsoftonline.com/";
                    option.TenantId = "msidentitysamplestesting.onmicrosoft.com";
                    option.ClientId = "6af093f3-b445-4b7a-beae-046864468ad6";
                    option.ClientCertificates = new[] { new CertificateDescription
                {
                    SourceType = CertificateSource.KeyVault,
                    KeyVaultUrl = "https://webappsapistests.vault.azure.net",
                    KeyVaultCertificateName = "Self-Signed-5-5-22"
                } };
                });
            }
            else
            {
                services.Configure<MicrosoftAuthenticationOptions>(option =>
                {
                    option.Instance = "https://login.microsoftonline.com/";
                    option.TenantId = "msidentitysamplestesting.onmicrosoft.com";
                    option.ClientId = "6af093f3-b445-4b7a-beae-046864468ad6";
                    option.ClientCredentials = new[] { new CertificateDescription
                {
                    SourceType = CertificateSource.KeyVault,
                    KeyVaultUrl = "https://webappsapistests.vault.azure.net",
                    KeyVaultCertificateName = "Self-Signed-5-5-22"
                } };
                });
            }

            services.AddInMemoryTokenCaches();
            services.AddMicrosoftGraph();
            var serviceProvider = tokenAcquirerFactory.Build();
            GraphServiceClient graphServiceClient = serviceProvider.GetRequiredService<GraphServiceClient>();
            var users = await graphServiceClient.Users
                .Request()
                .WithAppOnly()
                //     .WithAuthenticationOptions(options => options.ProtocolScheme = "Pop")
                .GetAsync();
            Assert.Equal(50, users.Count);

            // Get the token acquisition service
            ITokenAcquirer tokenAcquirer = serviceProvider.GetRequiredService<ITokenAcquirer>();
            var result = await tokenAcquirer.GetTokenForAppAsync("https://graph.microsoft.com/.default");
            Assert.NotNull(result.AccessToken);
        }
    }
}
