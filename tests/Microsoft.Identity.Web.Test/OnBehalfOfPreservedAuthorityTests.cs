// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web.Test.Common.Mocks;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    /// <summary>
    /// A web API configured with an Authority on its own (which AddMicrosoftIdentityWebApi preserves, so MSAL builds
    /// the application with WithOidcAuthority) calls a downstream API on behalf of a user whose token has a tid claim.
    /// Applying that tid as a request-level tenant made MSAL throw authority_type_mismatch.
    /// </summary>
    public class OnBehalfOfPreservedAuthorityTests
    {
        private const string Authority = "https://idp.contoso.com/test-tenant-id/v2.0";
        private const string TokenEndpoint = "https://idp.contoso.com/test-tenant-id/oauth2/v2.0/token";

        [Fact]
        public async Task OnBehalfOf_PreservedAuthority_UserWithTenantClaim_UsesTheAuthorityAsIsAsync()
        {
            // Arrange: a web API, as configured from an "AzureAd" section with Authority, ClientId and a secret.
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AzureAd:Authority"] = Authority,
                ["AzureAd:ClientId"] = "testClientId",
                ["AzureAd:ClientCredentials:0:SourceType"] = "ClientSecret",
                ["AzureAd:ClientCredentials:0:ClientSecret"] = "test-secret",
            }).Build();

            var mockHttpClient = new MockHttpClientFactory();
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<IConfiguration>(configuration);
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddMicrosoftIdentityWebApi(configuration.GetSection("AzureAd"))
                .EnableTokenAcquisitionToCallDownstreamApi()
                .AddInMemoryTokenCaches();
            services.AddSingleton<IMsalHttpClientFactory>(mockHttpClient);
            using ServiceProvider provider = services.BuildServiceProvider();

            // The JWT bearer handler configures its options when a request is authenticated; this is where
            // Microsoft.Identity.Web decides to preserve the Authority.
            provider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>().Get(JwtBearerDefaults.AuthenticationScheme);

            // The authenticated user of the current request, with the token that called the web API.
            var identity = new CaseSensitiveClaimsIdentity(
                [
                    new Claim("tid", "test-tenant-id"),
                    new Claim("oid", "test-object-id"),
                ],
                "TestAuth")
            {
                BootstrapContext = CreateTestJwt(),
            };
            var user = new ClaimsPrincipal(identity);
            var httpContext = new DefaultHttpContext { User = user };
            httpContext.Request.Scheme = "https";
            httpContext.Request.Host = new HostString("api.contoso.com");
            provider.GetRequiredService<IHttpContextAccessor>().HttpContext = httpContext;

            using (mockHttpClient)
            {
                // The generic authority's discovery, then the on-behalf-of request at its token endpoint.
                mockHttpClient.AddMockHandler(new MockHttpMessageHandler
                {
                    ExpectedUrl = Authority + "/.well-known/openid-configuration",
                    ExpectedMethod = HttpMethod.Get,
                    ResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(
                            "{\"issuer\":\"" + Authority + "\"," +
                            "\"authorization_endpoint\":\"https://idp.contoso.com/test-tenant-id/oauth2/v2.0/authorize\"," +
                            "\"token_endpoint\":\"" + TokenEndpoint + "\"}"),
                    },
                });
                MockHttpMessageHandler oboHandler = MockHttpCreator.CreateClientCredentialTokenHandler();
                oboHandler.ExpectedUrl = TokenEndpoint;
                oboHandler.ExpectedPostData = new Dictionary<string, string>
                {
                    { "grant_type", "urn:ietf:params:oauth:grant-type:jwt-bearer" },
                    { "requested_token_use", "on_behalf_of" },
                };
                mockHttpClient.AddMockHandler(oboHandler);

                // Act
                ITokenAcquisition tokenAcquisition = provider.GetRequiredService<ITokenAcquisition>();
                string accessToken = await tokenAcquisition.GetAccessTokenForUserAsync(
                    ["api://downstream/.default"],
                    user: user);

                // Assert
                Assert.Equal("header.payload.signature", accessToken);
            }
        }

        private static string CreateTestJwt()
        {
            string header = Base64UrlEncoder.Encode("{\"alg\":\"HS256\",\"typ\":\"JWT\"}");
            string payload = Base64UrlEncoder.Encode("{\"iss\":\"" + Authority + "\",\"tid\":\"test-tenant-id\"}");
            return $"{header}.{payload}.{Base64UrlEncoder.Encode("test_signature")}";
        }
    }
}
