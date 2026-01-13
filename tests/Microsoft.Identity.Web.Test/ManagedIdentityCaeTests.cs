// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web.Test.Common.Mocks;
using Microsoft.Identity.Web.TestOnly;
using Xunit;
using Microsoft.Identity.Web.Test;
using Microsoft.Identity.Web.Test.Common;
using NSubstitute;
using System.Collections.Generic;
using System.Net.Http;
using System.Net;
using System.Text;
using System.Security.Claims;
using System.Threading;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.Identity.Web.Tests.Certificateless
{
    [Collection(nameof(TokenAcquirerFactorySingletonProtection))]
    public class ManagedIdentityTests
    {
        private const string Scope = "https://management.azure.com/.default";
        private const string UamiClientId = "04ca4d6a-c720-4ba1-aa06-f6634b73fe7a";
        private const string MockToken = "mocked.access.token";
        private const string CaeClaims =
            @"{""access_token"":{""nbf"":{""essential"":true,""value"":""1702682181""}}}";

        private const string Downstream401Service = "Downstream401";
        private const string FirstToken = "mocked.access.token-1";
        private const string SecondToken = "mocked.access.token-2";
        private const string VaultBaseUrl = "https://my-vault.vault.azure.net/";
        private const string SecretPath = "secrets/mySecret";

        private sealed record VaultSecret(string Value);

        [Fact(Skip = "See https://github.com/AzureAD/microsoft-identity-web/issues/3669")]
        public async Task ManagedIdentity_ReturnsBearerHeader()
        {
            TokenAcquirerFactoryTesting.ResetTokenAcquirerFactoryInTest();
            var factory = TokenAcquirerFactory.GetDefaultInstance();

            var mockHttp = new MockHttpClientFactory();

            mockHttp.AddMockHandler(
                MockHttpCreator.CreateMsiTokenHandler(accessToken: MockToken));

            // Add the mock handler to the DI container
            factory.Services.AddSingleton<IManagedIdentityTestHttpClientFactory>(
                _ => new TestManagedIdentityHttpFactory(mockHttp));

            IAuthorizationHeaderProvider headerProvider = factory.Build()
                                        .GetRequiredService<IAuthorizationHeaderProvider>();

            // basic mi flow where we get a token
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

        [Fact(Skip = "See https://github.com/AzureAD/microsoft-identity-web/issues/3669")]
        public async Task ManagedIdentity_WithClaims_HeaderBypassesCache()
        {
            // Arrange
            TokenAcquirerFactoryTesting.ResetTokenAcquirerFactoryInTest();
            var tokenAcquirerFactory = TokenAcquirerFactory.GetDefaultInstance();
            var mockedHttp = new MockHttpClientFactory();

            // token-1 will be cached, token-2 should be returned when claims force a bypass
            mockedHttp.AddMockHandler(MockHttpCreator.CreateMsiTokenHandler("token1"));
            mockedHttp.AddMockHandler(MockHttpCreator.CreateMsiTokenHandler("token2"));

            tokenAcquirerFactory.Services.AddSingleton<IManagedIdentityTestHttpClientFactory>(
                _ => new TestManagedIdentityHttpFactory(mockedHttp));

            var headerProvider = tokenAcquirerFactory.Build()
                                        .GetRequiredService<IAuthorizationHeaderProvider>();

            // Initial call � no claims, token cached
            string header1 = await headerProvider.CreateAuthorizationHeaderForAppAsync(
                Scope,
                new AuthorizationHeaderProviderOptions
                {
                    AcquireTokenOptions = new AcquireTokenOptions
                    {
                        ManagedIdentity = new ManagedIdentityOptions
                        {
                            UserAssignedClientId = "UamiClientId2"
                        }
                    }
                });
            Assert.Equal("Bearer token1", header1);

            // Same UAMI with CAE claims � should bypass cache
            string header2 = await headerProvider.CreateAuthorizationHeaderForAppAsync(
                Scope,
                new AuthorizationHeaderProviderOptions
                {
                    AcquireTokenOptions = new AcquireTokenOptions
                    {
                        ManagedIdentity = new ManagedIdentityOptions
                        {
                            UserAssignedClientId = "UamiClientId2"
                        },
                        Claims = CaeClaims
                    }
                });
            Assert.Equal("Bearer token2", header2);
        }

        [Fact(Skip = "See https://github.com/AzureAD/microsoft-identity-web/issues/3669")]
        public async Task UserAssigned_MI_Caching_and_Claims()
        {
            // Arrange
            TokenAcquirerFactoryTesting.ResetTokenAcquirerFactoryInTest();
            var factory = TokenAcquirerFactory.GetDefaultInstance();
            var mockHttp = new MockHttpClientFactory();

            // a = token-1        b = cached token (i.e. token-1)
            // c = token-2        d = token-3
            mockHttp.AddMockHandler(MockHttpCreator.CreateMsiTokenHandler("token-1")); // a
            mockHttp.AddMockHandler(MockHttpCreator.CreateMsiTokenHandler("token-2")); // c
            mockHttp.AddMockHandler(MockHttpCreator.CreateMsiTokenHandler("token-3")); // d

            factory.Services.AddSingleton<IManagedIdentityTestHttpClientFactory>(
                _ => new TestManagedIdentityHttpFactory(mockHttp));

            var provider = factory.Build();
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

        [Fact(Skip = "See https://github.com/AzureAD/microsoft-identity-web/issues/3669")]
        public async Task SystemAssigned_MSI_Forwards_ClientCapabilities_InQuery()
        {
            // Arrange
            TokenAcquirerFactoryTesting.ResetTokenAcquirerFactoryInTest();
            var factory = TokenAcquirerFactory.GetDefaultInstance();

            // Mock IMDS/MSI: returns a 200 with "access_token" and records the request
            var captureHandler = MockHttpCreator.CreateMsiTokenHandler(MockToken);

            var mockHttp = new MockHttpClientFactory();
            mockHttp.AddMockHandler(captureHandler);
            factory.Services.AddSingleton<IManagedIdentityTestHttpClientFactory>(
                _ => new TestManagedIdentityHttpFactory(mockHttp));

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

            // Assert - outbound GET includes xms_cc=cp1%2Ccp2
            // This check can be enabled when MSAL enables cae
            //string query = captureHandler.ActualRequestMessage!.RequestUri!.Query;
            //Assert.Contains("xms_cc=cp1%2Ccp2", query, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task DownstreamApi_401Claims_TriggersSingleRetry_AndSucceeds()
        {
            // challenge JSON 
            string challengeB64 = Base64UrlEncoder.Encode(
                                      Encoding.UTF8.GetBytes(CaeClaims));

            // authProvider mock 
            var authProvider = Substitute.For<IAuthorizationHeaderProvider>();
            DownstreamApiOptions? capturedOpts = null;

            authProvider.CreateAuthorizationHeaderAsync(
                    Arg.Any<IEnumerable<string>>(),
                    Arg.Do<DownstreamApiOptions>(o => capturedOpts = o),
                    Arg.Any<ClaimsPrincipal?>(),
                    Arg.Any<CancellationToken>())
                .Returns(ci => $"Bearer {FirstToken}",
                         ci => $"Bearer {SecondToken}");

            // queue handler: 401 w/ claims - 200 OK
            // Id Web will single retry the request on 401
            var queue = new QueueHttpMessageHandler();

            // 401 response with claims
            var r401 = new HttpResponseMessage(HttpStatusCode.Unauthorized);

            // add the claims challenge with error in the header
            r401.Headers.WwwAuthenticate.ParseAdd(
                $"Bearer realm=\"\", error=\"insufficient_claims\", " +
                $"error_description=\"token requires claims\", " +
                $"claims=\"{challengeB64}\"");
            queue.AddHttpResponseMessage(r401);

            queue.AddHttpResponseMessage(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{ \"value\": \"MockSecretValue\" }",
                                            Encoding.UTF8, "application/json")
            });

            // DI container
            var services = new ServiceCollection();
            services.AddHttpClient(Downstream401Service)
                    .ConfigurePrimaryHttpMessageHandler(() => queue);
            services.AddLogging();
            services.AddTokenAcquisition();
            services.AddSingleton(authProvider);

            services.AddDownstreamApi(Downstream401Service, opts =>
            {
                opts.BaseUrl = VaultBaseUrl;
                opts.RelativePath = SecretPath;
                opts.RequestAppToken = true;
                opts.Scopes = [Scope];
            });

            var sp = services.BuildServiceProvider();
            var api = sp.GetRequiredService<IDownstreamApi>();

            //  ACT 
            VaultSecret? secret = await api.GetForAppAsync<VaultSecret>(Downstream401Service);

            // ASSERT
            Assert.NotNull(secret);
            Assert.Equal("MockSecretValue", secret!.Value);             // retry succeeded

            await authProvider.Received(2).CreateAuthorizationHeaderAsync(
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<DownstreamApiOptions>(),
                Arg.Any<ClaimsPrincipal?>(),
                Arg.Any<CancellationToken>());                           // called twice

            Assert.Equal(challengeB64, capturedOpts!.AcquireTokenOptions.Claims);
        }
    }
}
