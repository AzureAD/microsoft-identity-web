// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using IntegrationTestService;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Client;
using Microsoft.Identity.Lab.Api;
using Microsoft.Identity.Web.Test.Common;
using Microsoft.Identity.Web.Test.Common.TestHelpers;
using Microsoft.Identity.Web.TokenCacheProviders.Distributed;
using Microsoft.Identity.Web.TokenCacheProviders.InMemory;
using Xunit;

namespace Microsoft.Identity.Web.Test.Integration
{
#if !FROM_GITHUB_ACTION
    public class AcquireTokenForUserIntegrationTests : IClassFixture<WebApplicationFactory<IntegrationTestService.Startup>>
    {
        public AcquireTokenForUserIntegrationTests(WebApplicationFactory<Startup> factory)
        {
            _factory = factory;
        }

        private readonly WebApplicationFactory<Startup> _factory;

        [Theory]
        [InlineData(TestConstants.SecurePageGetTokenForUserAsync)]
        [InlineData(TestConstants.SecurePageCallDownstreamWebApi)]
        [InlineData(TestConstants.SecurePageCallDownstreamWebApiGeneric)]
        [InlineData(TestConstants.SecurePageCallMicrosoftGraph)]
        [InlineData(TestConstants.SecurePageCallDownstreamWebApiGenericWithTokenAcquisitionOptions)]
        [InlineData(TestConstants.SecurePageCallMicrosoftGraph, false)]
        [InlineData(TestConstants.SecurePageCallDownstreamWebApi, false)]
        [InlineData(TestConstants.SecurePageCallDownstreamWebApiGeneric, false)]
        public async Task GetTokenForUserAsync(
                string webApiUrl,
                bool addInMemoryTokenCache = true)
        {
            // Arrange
            HttpClient client = CreateHttpClient(addInMemoryTokenCache);

            var result = await AcquireTokenForLabUserAsync();

            // Act
            HttpResponseMessage response = await CreateHttpResponseMessageAsync(webApiUrl, client, result);

            // Assert
            Assert.True(response.IsSuccessStatusCode);
        }

        [Theory]
        [InlineData(TestConstants.SecurePage2GetTokenForUserAsync)]
        [InlineData(TestConstants.SecurePage2CallDownstreamWebApi)]
        [InlineData(TestConstants.SecurePage2CallDownstreamWebApiGeneric)]
        [InlineData(TestConstants.SecurePage2CallMicrosoftGraph)]
        [InlineData(TestConstants.SecurePage2CallDownstreamWebApiGenericWithTokenAcquisitionOptions)]
        [InlineData(TestConstants.SecurePage2CallMicrosoftGraph, false)]
        [InlineData(TestConstants.SecurePage2CallDownstreamWebApi, false)]
        [InlineData(TestConstants.SecurePage2CallDownstreamWebApiGeneric, false)]
#if NET8_0_OR_GREATER
        [InlineData(TestConstants.SecurePage2CallDownstreamWebApiGenericAotInternal)]
        [InlineData(TestConstants.SecurePage2CallDownstreamWebApiGenericAotInternal, false)]
#endif
        public async Task GetTokenForUserWithDifferentAuthSchemeAsync(
               string webApiUrl,
               bool addInMemoryTokenCache = true)
        {
            // Arrange
            HttpClient client = CreateHttpClient(addInMemoryTokenCache);

            var result = await AcquireTokenForLabUserAsync();

            // Act
            HttpResponseMessage response = await CreateHttpResponseMessageAsync(webApiUrl, client, result);

            // Assert
            Assert.True(response.IsSuccessStatusCode);
        }

#if NET7_0
        [Fact]
        public async Task TestSigningKeyIssuerAsync()
        {
            // Arrange
            string authority = "http://localhost:1234";
            Process? p = ExternalApp.Start(
                typeof(AcquireTokenForUserIntegrationTests),
                @"tests\E2E Tests\SimulateOidc\", 
                "SimulateOidc.exe",
                $"--urls={authority}");
            if (p != null && !p.HasExited)
            {
                // The metadata should be served from https://localhost:1234/v2.0/.well-known/openid-configuration
                // HttpClient oidcClient = new HttpClient();
                // string oidcMetadata = await oidcClient.GetStringAsync("https://localhost:1234/v2.0/.well-known/openid-configuration");
                HttpClient client = CreateHttpClient(true,

              // Setting the authority to http://localhost:1234/v2.0 will make the test return a 401, as the signing key
              // issuer (from the metadata document) won't match the issuer. The same test returns a 200 if the authority is
              // the real AAD authority.
              services => services.Configure<JwtBearerOptions>(
                  TestConstants.CustomJwtScheme2,
                  config =>
                  {
                      // Contact the test STS on HTTP to avoid untrusted SSL certs during CI builds.
                      config.Authority = $"{authority}/v2.0";
                      config.RequireHttpsMetadata = false;
                  })
              );

                // Act
                var result = await AcquireTokenForLabUserAsync();
                HttpResponseMessage response = await CreateHttpResponseMessageAsync(
                    TestConstants.SecurePage2GetTokenForUserAsync,
                    client,
                    result);
                p.Kill();

                // Assert
                Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
                Assert.Contains("error=\"invalid_token\", error_description=\"The issuer '(null)' is invalid\"", response.Headers.WwwAuthenticate.Select(h => h.Parameter));
            }
            else
            {
                Assert.Fail($"Could not start the OIDC proxy at {authority}/v2.0/");
            }
        }
#endif


        private static async Task<HttpResponseMessage> CreateHttpResponseMessageAsync(string webApiUrl, HttpClient client, AuthenticationResult result)
        {
            HttpResponseMessage response;
            using (HttpRequestMessage httpRequestMessage = new HttpRequestMessage(
                HttpMethod.Get, webApiUrl))
            {
                if (result != null)
                {
                    httpRequestMessage.Headers.Add(
                        Constants.Authorization,
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "{0} {1}",
                            Constants.Bearer,
                            result.AccessToken));
                }
                response = await client.SendAsync(httpRequestMessage);
            }

            return response;
        }

        private HttpClient CreateHttpClient(
            bool addInMemoryTokenCache,
            Action<IServiceCollection>? additionalAction = null)
        {
            return _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    if (addInMemoryTokenCache)
                    {
                        services.AddInMemoryTokenCaches();
                    }
                    else
                    {
                        services.AddDistributedMemoryCache();
                        services.AddDistributedTokenCaches();
                    }

                    if (additionalAction != null)
                    {
                        additionalAction(services);
                    }

                    services.BuildServiceProvider();
                });
            })
                .CreateClient(new WebApplicationFactoryClientOptions
                {
                    AllowAutoRedirect = false,
                });
        }

        private static async Task<AuthenticationResult> AcquireTokenForLabUserAsync()
        {
            var labResponse = await LabUserHelper.GetSpecificUserAsync(TestConstants.OBOUser);
            var msalPublicClient = PublicClientApplicationBuilder
               .Create(TestConstants.OBOClientSideClientId)
               .WithAuthority(labResponse.Lab.Authority, TestConstants.Organizations)
               .Build();

            AuthenticationResult authResult = await msalPublicClient
                .AcquireTokenByUsernamePassword(
                TestConstants.s_oBOApiScope,
                TestConstants.OBOUser,
                labResponse.User.GetOrFetchPassword())
                .ExecuteAsync(CancellationToken.None)
                ;

            return authResult;
        }
    }
#endif //FROM_GITHUB_ACTION
}
