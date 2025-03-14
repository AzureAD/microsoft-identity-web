// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Platforms.Features.DesktopOs.Kerberos;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.Test.Common;
using Microsoft.Identity.Web.Test.Common.Mocks;
using Microsoft.Identity.Web.TestOnly;
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
            catch (Exception ex) when (ex is not XunitException)
            {
                Assert.Fail(ex.Message);
            }
        }

        [Fact]
        public async Task AcquireAppTokenForAtPop_WithCustomSignedAssertion_Successfull()
        {
            // Arrange
            using (MockHttpClientFactory httpFactoryForTest = new MockHttpClientFactory())
            {
                var credentialRequestHttpHandler = httpFactoryForTest.AddMockHandler(
                    MockHttpCreator.CreateClientCredentialTokenHandler(tokenType: "pop"));
                var tokenRequestHttpHandler = httpFactoryForTest.AddMockHandler(
                    MockHttpCreator.CreateClientCredentialTokenHandler(tokenType: "pop"));

                TokenAcquirerFactoryTesting.ResetTokenAcquirerFactoryInTest();
                TokenAcquirerFactory tokenAcquirerFactory = TokenAcquirerFactory.GetDefaultInstance();
                tokenAcquirerFactory.Services.AddCustomSignedAssertionProvider();
                tokenAcquirerFactory.Services.AddSingleton<IHttpClientFactory>(httpFactoryForTest);

                tokenAcquirerFactory.Services.Configure<MicrosoftIdentityApplicationOptions>(options =>
                {
                    options.Instance = "https://login.microsoftonline.com/";
                    options.TenantId = "t2";
                    options.ClientId = "c2";
                    options.ExtraQueryParameters = null;
                    options.ClientCredentials = [ new CredentialDescription() {
                        SourceType = CredentialSource.CustomSignedAssertion,
                        CustomSignedAssertionProviderName = "MyCustomExtension" }];
                });

                IServiceProvider serviceProvider = tokenAcquirerFactory.Build();
                IAuthorizationHeaderProvider authorizationHeaderProvider =
                    serviceProvider.GetRequiredService<IAuthorizationHeaderProvider>();

                ITokenAcquirer tokenAcquirer = tokenAcquirerFactory.GetTokenAcquirer("");
                var tokenResult = await tokenAcquirer.GetTokenForAppAsync(
                    TestConstants.s_scopeForApp,
                    new AcquireTokenOptions()
                    {
                        PopPublicKey = "pop_key",
                        PopClaim = "jwk_claim"
                    });

                // Assert
                Assert.NotNull(tokenResult);
                Assert.NotNull(tokenResult.AccessToken);
                Assert.Equal("pop", tokenResult.TokenType);
            }
        }
    }
}
