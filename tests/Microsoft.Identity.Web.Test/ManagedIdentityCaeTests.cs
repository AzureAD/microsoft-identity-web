// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web.Test.Common.Mocks;
using Microsoft.Identity.Web.TestOnly;
using Xunit;
using System.Text;

namespace Microsoft.Identity.Web.Tests.Certificateless
{
    [Collection("Run tests - serial")]
    public class ManagedIdentityTests
    {
        private const string Scope = "https://management.azure.com/.default";
        private const string UamiClientId = "04ca4d6a-c720-4ba1-aa06-f6634b73fe7a";
        private const string MockToken = "mocked.access.token";
        private const string CaeClaims =
            @"{""access_token"":{""nbf"":{""essential"":true,""value"":""1702682181""}}}";

        [Fact]
        public async Task ManagedIdentity_ReturnsBearerHeader()
        {
            TokenAcquirerFactoryTesting.ResetTokenAcquirerFactoryInTest();
            var factory = TokenAcquirerFactory.GetDefaultInstance();

            var mockHttp = new MockHttpClientFactory();

            mockHttp.AddMockHandler(
                MockHttpCreator.CreateMsiTokenHandler(accessToken: MockToken));

            // uses TokenAcquirerTestHooks.HttpClientFactoryOverride so that every IMDS / MSI call
            // is routed through mocked responses using IMsalHttpClientFactory.
            TokenAcquirerTestHooks.UseTestHttpClientFactory(mockHttp);

            IAuthorizationHeaderProvider headerProvider = factory.Build()
                                        .GetRequiredService<IAuthorizationHeaderProvider>();

            // basic mi flow
            string header = await headerProvider.CreateAuthorizationHeaderForAppAsync(
                                Scope,
                                new AuthorizationHeaderProviderOptions
                                {
                                    AcquireTokenOptions = new AcquireTokenOptions
                                    {
                                        ManagedIdentity = new ManagedIdentityOptions { UserAssignedClientId = UamiClientId },
                                    }
                                });

            Assert.Equal($"Bearer {MockToken}", header);
        }

        [Fact]
        public async Task UserAssigned_MI_Caching_and_Claims()
        {
            // Arrange
            TokenAcquirerFactoryTesting.ResetTokenAcquirerFactoryInTest();
            var fac = TokenAcquirerFactory.GetDefaultInstance();
            var mocks = new MockHttpClientFactory();

            // a = token-1        b = cached token (i.e. token-1)
            // c = token-2        d = token-3
            mocks.AddMockHandler(MockHttpCreator.CreateMsiTokenHandler("token-1")); // a
            mocks.AddMockHandler(MockHttpCreator.CreateMsiTokenHandler("token-2")); // c
            mocks.AddMockHandler(MockHttpCreator.CreateMsiTokenHandler("token-3")); // d

            TokenAcquirerTestHooks.UseTestHttpClientFactory(mocks);

            var provider = fac.Build();
            var tokens = provider.GetRequiredService<ITokenAcquisition>();

            // helper
            static TokenAcquisitionOptions Uami(string id, string? claims = null) => new()
            {
                ManagedIdentity = new ManagedIdentityOptions { UserAssignedClientId = id },
                Claims = claims
            };

            // scenario a : first call directed to IdP for uamiA
            var r1 = await tokens.GetAuthenticationResultForAppAsync(
                         Scope, tokenAcquisitionOptions: Uami("uamiA"));
            Assert.Equal(TokenSource.IdentityProvider, r1.AuthenticationResultMetadata.TokenSource);
            Assert.Equal("token-1", r1.AccessToken);

            // scenario b : same uamiA and no claims gets a cached token
            var r2 = await tokens.GetAuthenticationResultForAppAsync(
                         Scope, tokenAcquisitionOptions: Uami("uamiA"));
            Assert.Equal(TokenSource.Cache, r2.AuthenticationResultMetadata.TokenSource);
            Assert.Equal("token-1", r2.AccessToken);

            // scenario c : same uamiA + CAE claims gets a token from IdP (bypasses cache)
            var r3 = await tokens.GetAuthenticationResultForAppAsync(
                         Scope, tokenAcquisitionOptions: Uami("uamiA", CaeClaims));
            Assert.Equal(TokenSource.IdentityProvider, r3.AuthenticationResultMetadata.TokenSource);
            Assert.Equal("token-2", r3.AccessToken);

            // scenario d : different UAMI (say uamiB) gets a token from IdP
            var r4 = await tokens.GetAuthenticationResultForAppAsync(
                         Scope, tokenAcquisitionOptions: Uami("uamiB"));
            Assert.Equal(TokenSource.IdentityProvider, r4.AuthenticationResultMetadata.TokenSource);
            Assert.Equal("token-3", r4.AccessToken);
        }

        [Fact]
        public async Task SystemAssigned_MSI_Forwards_ClientCapabilities_InQuery()
        {
            // Arrange
            TokenAcquirerFactoryTesting.ResetTokenAcquirerFactoryInTest();
            var factory = TokenAcquirerFactory.GetDefaultInstance();

            // Mock IMDS/MSI: returns a 200 with "access_token" and records the request
            var captureHandler = MockHttpCreator.CreateMsiTokenHandler(MockToken);

            var mockHttp = new MockHttpClientFactory();
            mockHttp.AddMockHandler(captureHandler);
            TokenAcquirerTestHooks.UseTestHttpClientFactory(mockHttp);

            // Enable capabilities cp1,cp2
            factory.Services.Configure<MicrosoftIdentityApplicationOptions>(opts =>
                opts.ClientCapabilities = ["cp1", "cp2"]);

            var tokenAcquirer = factory.Build()
                                       .GetRequiredService<ITokenAcquisition>();

            // Act 
            var result = await tokenAcquirer.GetAuthenticationResultForAppAsync(
                             Scope,
                             tokenAcquisitionOptions: new TokenAcquisitionOptions
                             {
                                 ManagedIdentity = new ManagedIdentityOptions(), // system-assigned
                                 Claims = CaeClaims
                             });

            // Assert
            Assert.Equal(MockToken, result.AccessToken);

            // Assert – outbound GET includes xms_cc=cp1%2Ccp2
            // This check can be enabled when MSAL enables cae
            //string query = captureHandler.ActualRequestMessage!.RequestUri!.Query;
            //Assert.Contains("xms_cc=cp1%2Ccp2", query, StringComparison.OrdinalIgnoreCase);
        }
    }
}
