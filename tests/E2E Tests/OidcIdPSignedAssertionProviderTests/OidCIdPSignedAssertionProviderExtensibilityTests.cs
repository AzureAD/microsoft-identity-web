// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.Test.Common;
using Microsoft.Identity.Web.Test.Common.Mocks;
using Xunit.Sdk;


namespace CustomSignedAssertionProviderTests
{
    public class OidCIdPSignedAssertionProviderExtensibilityTests
    {
        [Fact]
        public async Task CrossCloudFicIntegrationTest()
        {
            // Arrange
            TokenAcquirerFactory tokenAcquirerFactory = TokenAcquirerFactory.GetDefaultInstance();
            tokenAcquirerFactory.Services.AddOidcSignedAssertionProvider();

            // this is how the authentication options can be configured in code rather than
            // in the appsettings file, though using the appsettings file is recommended
            /*            
            tokenAcquirerFactory.Services.Configure<MicrosoftIdentityApplicationOptions>(options =>
            {
                options.Instance = "https://login.microsoftonline.com/";
                options.TenantId = "msidlab4.onmicrosoft.com";
                options.ClientId = "5e71875b-ae52-4a3c-8b82-f6fdc8e1dbe1";
                options.ClientCredentials = [ new CredentialDescription() {
                    SourceType = CredentialSource.CustomSignedAssertion,
                    CustomSignedAssertionProviderName = "MyCustomExtension"
                }];
            });
            */
            IServiceProvider serviceProvider = tokenAcquirerFactory.Build();
            IAuthorizationHeaderProvider authorizationHeaderProvider = serviceProvider.GetRequiredService<IAuthorizationHeaderProvider>();

            // Act
            string result = await authorizationHeaderProvider.CreateAuthorizationHeaderForAppAsync("https://graph.microsoft.com/.default");

            // Assert
            Assert.NotNull(result);
            Assert.StartsWith("Bearer", result, StringComparison.Ordinal);
        }

        [Fact]
        public async Task CrossCloudFicUnitTest()
        {
            // Arrange
            using (MockHttpClientFactory httpFactoryForTest = new MockHttpClientFactory())
            {

                _ = httpFactoryForTest.AddMockHandler(
                    MockHttpCreator.CreateInstanceDiscoveryMockHandler());
                var credentialRequestHttpHandler = httpFactoryForTest.AddMockHandler(
                    MockHttpCreator.CreateClientCredentialTokenHandler());
                var tokenRequestHttpHandler = httpFactoryForTest.AddMockHandler(
                    MockHttpCreator.CreateClientCredentialTokenHandler());

                // TODO: need to reset the configuration to avoid conflicts with other tests
                TokenAcquirerFactory tokenAcquirerFactory = TokenAcquirerFactory.GetDefaultInstance();
                tokenAcquirerFactory.Services.AddOidcSignedAssertionProvider();
                tokenAcquirerFactory.Services.AddSingleton<IHttpClientFactory>(httpFactoryForTest);

                tokenAcquirerFactory.Services.Configure<MicrosoftIdentityApplicationOptions>("AzureAd2",
                       options =>
                       {
                           options.Instance = "https://login.microsoftonline.us/";
                           options.TenantId = "t1";
                           options.ClientId = "c1";
                           options.ClientCredentials = [ new CredentialDescription() {
                            SourceType = CredentialSource.ClientSecret,
                            ClientSecret = TestConstants.ClientSecret
                        }];
                       });

                tokenAcquirerFactory.Services.Configure<MicrosoftIdentityApplicationOptions>(options =>
                {
                    options.Instance = "https://login.microsoftonline.com/";
                    options.TenantId = "t2";
                    options.ClientId = "c2";
                    options.ExtraQueryParameters = null;
                    options.ClientCredentials = [ new CredentialDescription() {
                    SourceType = CredentialSource.CustomSignedAssertion,
                    CustomSignedAssertionProviderName = "OidIdpSignedAssertion",
                    CustomSignedAssertionProviderData = new Dictionary<string, object>{{
                            "ConfigurationSection", "AzureAd2"
                        }}
                    }];
                });

                IServiceProvider serviceProvider = tokenAcquirerFactory.Build();
                IAuthorizationHeaderProvider authorizationHeaderProvider =
                    serviceProvider.GetRequiredService<IAuthorizationHeaderProvider>();

                // Act
                var result = await authorizationHeaderProvider.CreateAuthorizationHeaderForAppAsync(TestConstants.s_scopeForApp);

                // Assert
                Assert.Equal("api://AzureADTokenExchange/.default", credentialRequestHttpHandler.ActualRequestPostData["scope"]);
                Assert.Equal(TestConstants.s_scopeForApp, tokenRequestHttpHandler.ActualRequestPostData["scope"]);
                Assert.Equal("c1", credentialRequestHttpHandler.ActualRequestPostData["client_id"]);
                Assert.Equal("https://login.microsoftonline.us/t1/oauth2/v2.0/token", credentialRequestHttpHandler.ActualRequestMessage?.RequestUri?.AbsoluteUri);
                Assert.Equal("c2", tokenRequestHttpHandler.ActualRequestPostData["client_id"]);
                Assert.Equal("https://login.microsoftonline.com/t2/oauth2/v2.0/token", tokenRequestHttpHandler.ActualRequestMessage?.RequestUri?.AbsoluteUri);

                string? accessTokenFromRequest1;
                using (JsonDocument document = JsonDocument.Parse(credentialRequestHttpHandler.ResponseString))
                {
                    accessTokenFromRequest1 = document.RootElement.GetProperty("access_token").GetString();
                }

                // the jwt credential from request1 is used as credential on request2
                Assert.Equal(
                    tokenRequestHttpHandler.ActualRequestPostData["client_assertion"],
                    accessTokenFromRequest1);
            }
        }    
    }
}
