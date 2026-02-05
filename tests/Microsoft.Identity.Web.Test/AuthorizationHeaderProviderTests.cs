// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web.Test.Common;
using Microsoft.Identity.Web.Test.Common.Mocks;
using Microsoft.Identity.Web.TestOnly;
using Microsoft.IdentityModel.Tokens;
using NSubstitute.Extensions;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    [Collection(nameof(TokenAcquirerFactorySingletonProtection))]
    public class AuthorizationHeaderProviderTests
    {
        [Fact]
        public async Task LongRunningSessionForDefaultAuthProviderForUserDefaultKeyTest()
        {
            // Arrange
            // Create a test ClaimsPrincipal
            var claims = new List<Claim>
                            {
                                new Claim(ClaimTypes.Name, "testuser@contoso.com")
                            };

            var identity = new CaseSensitiveClaimsIdentity(claims, "TestAuth");
            identity.BootstrapContext = CreateTestJwt();
            var claimsPrincipal = new ClaimsPrincipal(identity);

            var tokenAcquirerFactory = InitTokenAcquirerFactoryForTest();

            // Configure the extension option such that the event is subscribed to
            // so the test can observe if the service provider is set in the extra parameters
            tokenAcquirerFactory.Services.Configure<TokenAcquisitionExtensionOptions>(options =>
            {
                options.OnBeforeTokenAcquisitionForOnBehalfOf += (builder, options, user) =>
                {
                    //verify that the ClaimsPrincipal passed in the event is the same as the one passed to CreateAuthorizationHeaderForUserAsync and that the BootstrapContext is preserved
                    Assert.Equal(((CaseSensitiveClaimsIdentity)claimsPrincipal.Identity!).BootstrapContext, ((CaseSensitiveClaimsIdentity)user.Identity!).BootstrapContext);
                };
            });
            IServiceProvider serviceProvider = tokenAcquirerFactory.Build();

            IAuthorizationHeaderProvider authorizationHeaderProvider =
                serviceProvider.GetRequiredService<IAuthorizationHeaderProvider>();
            var mockHttpClient = serviceProvider.GetRequiredService<IMsalHttpClientFactory>() as MockHttpClientFactory;

            using (mockHttpClient)
            {


                // Create options with LongRunningWebApiSessionKey
                var options = new AuthorizationHeaderProviderOptions
                {
                    AcquireTokenOptions = new AcquireTokenOptions
                    {
                        //When this is set to Auto, the first call will set the LongRunningWebApiSessionKey in the options to
                        //the hash of the ClaimsPrincipal's access token.
                        LongRunningWebApiSessionKey = TokenAcquisitionOptions.LongRunningWebApiSessionKeyAuto
                    }
                };

                // Act & Assert

                // Step 3: First call with ClaimsPrincipal to initiate LR session
                var scopes = new[] { "User.Read" };
                mockHttpClient!.AddMockHandler(MockHttpCreator.CreateLrOboTokenHandler("User.Read"));
                var result = await authorizationHeaderProvider.CreateAuthorizationHeaderForUserAsync(
                    scopes,
                    options,
                    claimsPrincipal);

                Assert.NotNull(result);
                Assert.NotEqual(options.AcquireTokenOptions.LongRunningWebApiSessionKey, TokenAcquisitionOptions.LongRunningWebApiSessionKeyAuto);
                string key1 = options.AcquireTokenOptions.LongRunningWebApiSessionKey;

                // Step 4: Second call without ClaimsPrincipal should return the token from cache
                result = await authorizationHeaderProvider.CreateAuthorizationHeaderForUserAsync(
                                    scopes,
                                    options);

                Assert.NotNull(result);
                Assert.NotEqual(options.AcquireTokenOptions.LongRunningWebApiSessionKey, TokenAcquisitionOptions.LongRunningWebApiSessionKeyAuto);
                Assert.Equal(key1, options.AcquireTokenOptions.LongRunningWebApiSessionKey);

                // Step 5: First call with ClaimsPrincipal to initiate LR session for CreateAuthorizationHeaderAsync
                scopes = new[] { "User.Write" };
                mockHttpClient!.AddMockHandler(MockHttpCreator.CreateLrOboTokenHandler("User.Write"));
                result = await authorizationHeaderProvider.CreateAuthorizationHeaderAsync(
                    scopes,
                    options,
                    claimsPrincipal);

                Assert.NotNull(result);
                Assert.NotEqual(options.AcquireTokenOptions.LongRunningWebApiSessionKey, TokenAcquisitionOptions.LongRunningWebApiSessionKeyAuto);
                key1 = options.AcquireTokenOptions.LongRunningWebApiSessionKey;

                // Step 6: Second call without ClaimsPrincipal should return the token from cache for CreateAuthorizationHeaderAsync
                result = await authorizationHeaderProvider.CreateAuthorizationHeaderAsync(
                                    scopes,
                                    options);

                Assert.NotNull(result);
                Assert.NotEqual(options.AcquireTokenOptions.LongRunningWebApiSessionKey, TokenAcquisitionOptions.LongRunningWebApiSessionKeyAuto);
                Assert.Equal(key1, options.AcquireTokenOptions.LongRunningWebApiSessionKey);
            }
        }

        [Fact]
        public async Task LongRunningSessionForDefaultAuthProviderForUserTest()
        {
            // Arrange
            var tokenAcquirerFactory = InitTokenAcquirerFactoryForTest();
            IServiceProvider serviceProvider = tokenAcquirerFactory.Build();

            IAuthorizationHeaderProvider authorizationHeaderProvider =
                serviceProvider.GetRequiredService<IAuthorizationHeaderProvider>();
            var mockHttpClient = serviceProvider.GetRequiredService<IMsalHttpClientFactory>() as MockHttpClientFactory;

            using (mockHttpClient)
            {
                // Create a test ClaimsPrincipal
                var claims = new List<Claim>
                            {
                                new Claim(ClaimTypes.Name, "testuser@contoso.com")
                            };

                var identity = new CaseSensitiveClaimsIdentity(claims, "TestAuth");
                identity.BootstrapContext = CreateTestJwt();
                var claimsPrincipal = new ClaimsPrincipal(identity);

                // Create options with LongRunningWebApiSessionKey
                var options = new AuthorizationHeaderProviderOptions
                {
                    AcquireTokenOptions = new AcquireTokenOptions
                    {
                        LongRunningWebApiSessionKey = "oboKey1"
                    }
                };

                // Act & Assert

                // Step 3: First call with ClaimsPrincipal to initiate LR session
                var scopes = new[] { "User.Read" };
                mockHttpClient!.AddMockHandler(MockHttpCreator.CreateLrOboTokenHandler("User.Read"));
                var result = await authorizationHeaderProvider.CreateAuthorizationHeaderForUserAsync(
                    scopes,
                    options,
                    claimsPrincipal);

                Assert.NotNull(result);
                Assert.Equal("oboKey1", options.AcquireTokenOptions.LongRunningWebApiSessionKey);

                // Step 4: Second call without ClaimsPrincipal should return the token from cache
                result = await authorizationHeaderProvider.CreateAuthorizationHeaderForUserAsync(
                                    scopes,
                                    options);

                Assert.NotNull(result);
                Assert.Equal("oboKey1", options.AcquireTokenOptions.LongRunningWebApiSessionKey);

                options = new AuthorizationHeaderProviderOptions
                {
                    AcquireTokenOptions = new AcquireTokenOptions
                    {
                        LongRunningWebApiSessionKey = "oboKey2"
                    }
                };

                // Step 5: First call with ClaimsPrincipal to initiate LR session for CreateAuthorizationHeaderAsync
                scopes = new[] { "User.Write" };
                mockHttpClient!.AddMockHandler(MockHttpCreator.CreateLrOboTokenHandler("User.Write"));
                result = await authorizationHeaderProvider.CreateAuthorizationHeaderAsync(
                    scopes,
                    options,
                    claimsPrincipal);

                Assert.NotNull(result);
                Assert.Equal("oboKey2", options.AcquireTokenOptions.LongRunningWebApiSessionKey);

                // Step 6: Second call without ClaimsPrincipal should return the token from cache for CreateAuthorizationHeaderAsync
                result = await authorizationHeaderProvider.CreateAuthorizationHeaderAsync(
                                    scopes,
                                    options);

                Assert.NotNull(result);
                Assert.Equal("oboKey2", options.AcquireTokenOptions.LongRunningWebApiSessionKey);
            }
        }

        private static string CreateTestJwt()
        {
            var header = new Dictionary<string, object>
                {
                    { "alg", "HS256" },
                    { "typ", "JWT" }
                };

            var payload = new Dictionary<string, object>
                {
                    { "iss", "https://login.microsoftonline.com/test-tenant-id/v2.0" }
                };

            string headerJson = System.Text.Json.JsonSerializer.Serialize(header);
            string payloadJson = System.Text.Json.JsonSerializer.Serialize(payload);

            string headerBase64 = Base64UrlEncoder.Encode(headerJson);
            string payloadBase64 = Base64UrlEncoder.Encode(payloadJson);

            // For testing purposes, we're using a fixed signature
            const string signature = "test_signature";
            string signatureBase64 = Base64UrlEncoder.Encode(signature);

            return $"{headerBase64}.{payloadBase64}.{signatureBase64}";
        }

        private TokenAcquirerFactory InitTokenAcquirerFactoryForTest()
        {
            TokenAcquirerFactoryTesting.ResetTokenAcquirerFactoryInTest();
            TokenAcquirerFactory tokenAcquirerFactory = TokenAcquirerFactory.GetDefaultInstance();
            tokenAcquirerFactory.Services.Configure<MicrosoftIdentityApplicationOptions>(options =>
            {
                options.Instance = "https://login.microsoftonline.com/";
                options.TenantId = "testTenantId";
                options.ClientId = "testClientId";
                options.ClientCredentials = [ new CredentialDescription() {
                                                SourceType = CredentialSource.ClientSecret,
                                                ClientSecret = "test-secret"
                                            }];
            });

            // Add required services
            tokenAcquirerFactory.Services.AddSingleton<IMsalHttpClientFactory, MockHttpClientFactory>();
            tokenAcquirerFactory.Services.AddScoped<IAuthorizationHeaderProvider, DefaultAuthorizationHeaderProvider>();

            return tokenAcquirerFactory;
        }
    }
}
