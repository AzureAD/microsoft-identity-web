// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
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
            // Arrange
            string expectedExceptionCode = "AADSTS50027";
            TokenAcquirerFactory tokenAcquirerFactory = TokenAcquirerFactory.GetDefaultInstance();
            tokenAcquirerFactory.Services.AddCustomSignedAssertionProvider();

            // this is how the authentication options can be configured in code rather than
            // in the appsettings file, though using the appsettings file is recommended
            /*            
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
            */
            IServiceProvider serviceProvider = tokenAcquirerFactory.Build();
            IAuthorizationHeaderProvider authorizationHeaderProvider = serviceProvider.GetRequiredService<IAuthorizationHeaderProvider>();

            try
            {
                // Act
                _ = await authorizationHeaderProvider.CreateAuthorizationHeaderForAppAsync("https://graph.microsoft.com/.default");
            }
            catch (MsalServiceException MsalEx)
            {
                // Assert
                Assert.Contains(expectedExceptionCode, MsalEx.Message, StringComparison.InvariantCulture);
            }
            catch (Exception ex) when (e is not XunitException)
            {
                Assert.Fail(ex.Message);
            }
        }
    }
}
