// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web.Test.Common.Mocks;
using Microsoft.Identity.Web.TestOnly;
using Xunit;

namespace Microsoft.Identity.Web.Tests.Certificateless
{
    [Collection("Run tests - serial")]
    public class ManagedIdentityTests
    {
        private const string Scope = "https://management.azure.com/.default";
        private const string UamiClientId = "04ca4d6a-c720-4ba1-aa06-f6634b73fe7a";
        private const string MockToken = "mocked.access.token";
        private const string FirstToken = "mocked.access.token-1";
        private const string SecondToken = "mocked.access.token-2";
        private const string CaeClaims =
            @"{""access_token"":{""nbf"":{""essential"":true,""value"":""1702682181""}}}";
        private const string AccessToken = "token-with-cap";
        private static readonly string[] Capabilities = ["cp1", "cp2"];

        [Fact]
        public async Task ManagedIdentity_ReturnsBearerHeader()
        {
            TokenAcquirerFactoryTesting.ResetTokenAcquirerFactoryInTest();
            var factory = TokenAcquirerFactory.GetDefaultInstance();

            var mockHttp = new MockHttpClientFactory();

            mockHttp.AddMockHandler(
                MockHttpCreator.CreateMsiTokenHandler(accessToken: MockToken));

            // uses ManagedIdentityTestHooks.HttpClientFactoryOverride so that every IMDS / MSI call
            // is routed through mocked responses using IMsalHttpClientFactory.
            TokenAcquirerFactoryTesting.UseTestHttpClientFactory(mockHttp);

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
        public async Task SystemAssigned_MI_Caching_and_Claims()
        {
            // Arrange – factory with 2 queued IMDS responses 
            TokenAcquirerFactoryTesting.ResetTokenAcquirerFactoryInTest();
            var factory = TokenAcquirerFactory.GetDefaultInstance();
            var mockHttp = new MockHttpClientFactory();

            // token1 will be used for the first call to IMDS
            mockHttp.AddMockHandler(MockHttpCreator.CreateMsiTokenHandler(FirstToken));

            // token2 will be used only when we inject claims
            // the call that happens after the first one should be served from cache
            mockHttp.AddMockHandler(MockHttpCreator.CreateMsiTokenHandler(SecondToken));

            // uses ManagedIdentityTestHooks.HttpClientFactoryOverride so that every IMDS / MSI call
            // is routed through mocked responses using IMsalHttpClientFactory.
            TokenAcquirerFactoryTesting.UseTestHttpClientFactory(mockHttp);

            var provider = factory.Build();
            var tokenAcq = provider.GetRequiredService<ITokenAcquisition>();

            var miOpts = new TokenAcquisitionOptions
            {
                ManagedIdentity = new ManagedIdentityOptions()
            };

            // first request — IDP
            var r1 = await tokenAcq.GetAuthenticationResultForAppAsync(Scope, tokenAcquisitionOptions: miOpts);
            Assert.Equal(TokenSource.IdentityProvider, r1.AuthenticationResultMetadata.TokenSource);
            Assert.Equal(FirstToken, r1.AccessToken); // got token1

            // second request — Cache 
            var r2 = await tokenAcq.GetAuthenticationResultForAppAsync(Scope, tokenAcquisitionOptions: miOpts);
            Assert.Equal(TokenSource.Cache, r2.AuthenticationResultMetadata.TokenSource);
            Assert.Equal(FirstToken, r2.AccessToken); // still token1

            // third request with claims — With CAE we will go to IDP 
            var refreshOpts = new TokenAcquisitionOptions
            {
                ManagedIdentity = new ManagedIdentityOptions(),
                Claims = CaeClaims
            };

            var r3 = await tokenAcq.GetAuthenticationResultForAppAsync(Scope, tokenAcquisitionOptions: refreshOpts);
            Assert.Equal(TokenSource.IdentityProvider, r3.AuthenticationResultMetadata.TokenSource);
            Assert.Equal(SecondToken, r3.AccessToken); // new token2 because of CAE
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

            TokenAcquirerFactoryTesting.UseTestHttpClientFactory(mocks);

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
    }
}
