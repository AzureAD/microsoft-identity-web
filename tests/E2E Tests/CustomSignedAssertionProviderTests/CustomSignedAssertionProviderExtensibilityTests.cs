// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.Test.Common;
using Xunit.Sdk;


namespace CustomSignedAssertionProviderTests
{
    [Collection(nameof(TokenAcquirerFactorySingletonProtection))]
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
                    options.TenantId = "id4slab1.onmicrosoft.com";
                options.ClientId = "4ebc2cfc-14bf-4c88-9678-26543ec1c59d";
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
            catch (Exception ex) when (ex is not XunitException)
            {
                Assert.Fail(ex.Message);
            }
        }
    }
}
