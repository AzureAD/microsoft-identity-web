// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web;

namespace CustomSignedAssertionProviderTests
{
    public class CustomSignedAssertionProviderExtensibilityTests
    {
        [Fact]
        public async Task UseSignedAssertionFromCustomSignedAssertionProvider()
        {
            TokenAcquirerFactory tokenAcquirerFactory = TokenAcquirerFactory.GetDefaultInstance();
            tokenAcquirerFactory.Services.Configure<MicrosoftIdentityApplicationOptions>(options =>
            {
                options.Instance = "https://login.microsoftonline.com/";
                options.TenantId = "msidlab4.onmicrosoft.com";
                options.ClientId = "f6b698c0-140c-448f-8155-4aa9bf77ceba";
                options.ClientCredentials = [ new CredentialDescription() {
                    SourceType = CredentialSource.CustomSignedAssertion,
                    CustomSignedAssertionProviderName = "MyCustomExtension"
                }];
            });
            tokenAcquirerFactory.Services.AddCustomSignedAssertionProvider();
            var serviceProvider = tokenAcquirerFactory.Build();

            // Get the authorization request creator service
            IAuthorizationHeaderProvider authorizationHeaderProvider = serviceProvider.GetRequiredService<IAuthorizationHeaderProvider>();

            await Assert.ThrowsAsync<MsalServiceException>(async () =>
            {
                await authorizationHeaderProvider.CreateAuthorizationHeaderForAppAsync("https://graph.microsoft.com/.default");
            });
        }
    }
}
