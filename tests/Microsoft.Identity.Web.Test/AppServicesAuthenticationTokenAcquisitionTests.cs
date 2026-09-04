// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web.Test.Common.Mocks;
using Microsoft.Identity.Web.TokenCacheProviders;
using NSubstitute;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    [CollectionDefinition(nameof(AppServicesAuthenticationTokenAcquisitionTests), DisableParallelization = true)]
    public sealed class AppServicesAuthenticationTokenAcquisitionTestCollection
    {
    }

    [Collection(nameof(AppServicesAuthenticationTokenAcquisitionTests))]
    public class AppServicesAuthenticationTokenAcquisitionTests : IDisposable
    {
        private const string AccessTokenHeader = "X-MS-TOKEN-AAD-ACCESS-TOKEN";
        private const string ClientId = "4f4b47e8-8a8d-4f4f-9a4f-112c6b651111";
        private const string TenantId = "c8011c90-3395-4c4b-8ef6-7ff7b2c92222";
        private readonly string? _originalClientId;
        private readonly string? _originalClientSecret;
        private readonly string? _originalDebugHeaders;
        private readonly string? _originalIssuer;

        public AppServicesAuthenticationTokenAcquisitionTests()
        {
            _originalClientId = Environment.GetEnvironmentVariable("WEBSITE_AUTH_CLIENT_ID");
            _originalClientSecret = Environment.GetEnvironmentVariable("WEBSITE_AUTH_CLIENT_SECRET");
            _originalDebugHeaders = Environment.GetEnvironmentVariable("APP_SERVICES_AUTH_LOCAL_DEBUG");
            _originalIssuer = Environment.GetEnvironmentVariable("WEBSITE_AUTH_OPENID_ISSUER");
            Environment.SetEnvironmentVariable("WEBSITE_AUTH_CLIENT_ID", ClientId);
            Environment.SetEnvironmentVariable("WEBSITE_AUTH_CLIENT_SECRET", "client-secret");
            Environment.SetEnvironmentVariable("APP_SERVICES_AUTH_LOCAL_DEBUG", null);
            Environment.SetEnvironmentVariable("WEBSITE_AUTH_OPENID_ISSUER", "https://login.microsoftonline.com/");
        }

        public void Dispose()
        {
            Environment.SetEnvironmentVariable("WEBSITE_AUTH_CLIENT_ID", _originalClientId);
            Environment.SetEnvironmentVariable("WEBSITE_AUTH_CLIENT_SECRET", _originalClientSecret);
            Environment.SetEnvironmentVariable("APP_SERVICES_AUTH_LOCAL_DEBUG", _originalDebugHeaders);
            Environment.SetEnvironmentVariable("WEBSITE_AUTH_OPENID_ISSUER", _originalIssuer);
        }

        [Fact]
        public async Task GetAccessTokenForAppAsync_ReturnsAppTokenAsync()
        {
            // Arrange
            using var httpClientFactory = new MockHttpClientFactory();
            httpClientFactory.AddMockHandler(CreateAppTokenHandler("app-token"));
            AppServicesAuthenticationTokenAcquisition acquisition = CreateAcquisition(
                new DefaultHttpContext(),
                httpClientFactory);

            // Act
            string accessToken = await acquisition.GetAccessTokenForAppAsync(
                "https://graph.microsoft.com/.default",
                authenticationScheme: null);

            // Assert
            Assert.Equal("app-token", accessToken);
        }

        [Fact]
        public async Task GetAuthenticationResultForAppAsync_ReturnsAppResultAsync()
        {
            // Arrange
            using var httpClientFactory = new MockHttpClientFactory();
            httpClientFactory.AddMockHandler(CreateAppTokenHandler("app-token"));
            AppServicesAuthenticationTokenAcquisition acquisition = CreateAcquisition(
                CreateContext("ambient-user-token"),
                httpClientFactory);

            // Act
            AuthenticationResult result = await acquisition.GetAuthenticationResultForAppAsync(
                "https://graph.microsoft.com/.default",
                authenticationScheme: null);

            // Assert
            Assert.Equal("app-token", result.AccessToken);
            Assert.Null(result.Account);
        }

        [Fact]
        public async Task GetAuthenticationResultForAppAsync_ExplicitTenantUsesTenantAuthorityAsync()
        {
            // Arrange
            using var httpClientFactory = new MockHttpClientFactory();
            MockHttpMessageHandler handler = httpClientFactory.AddMockHandler(CreateAppTokenHandler("app-token"));
            handler.ExpectedUrl = $"https://login.microsoftonline.com/{TenantId}/oauth2/v2.0/token";
            AppServicesAuthenticationTokenAcquisition acquisition = CreateAcquisition(
                new DefaultHttpContext(),
                httpClientFactory);

            // Act
            AuthenticationResult result = await acquisition.GetAuthenticationResultForAppAsync(
                "https://graph.microsoft.com/.default",
                authenticationScheme: null,
                tenant: TenantId);

            // Assert
            Assert.Equal("app-token", result.AccessToken);
        }

        [Fact]
        public async Task GetAuthenticationResultForAppAsync_OptionsTenantUsesTenantAuthorityAsync()
        {
            // Arrange
            using var httpClientFactory = new MockHttpClientFactory();
            MockHttpMessageHandler handler = httpClientFactory.AddMockHandler(CreateAppTokenHandler("app-token"));
            handler.ExpectedUrl = $"https://login.microsoftonline.com/{TenantId}/oauth2/v2.0/token";
            AppServicesAuthenticationTokenAcquisition acquisition = CreateAcquisition(
                new DefaultHttpContext(),
                httpClientFactory);

            // Act
            AuthenticationResult result = await acquisition.GetAuthenticationResultForAppAsync(
                "https://graph.microsoft.com/.default",
                authenticationScheme: null,
                tokenAcquisitionOptions: new TokenAcquisitionOptions { Tenant = TenantId });

            // Assert
            Assert.Equal("app-token", result.AccessToken);
        }

        [Fact]
        public async Task GetAccessTokenForUserAsync_ReturnsOpaqueTokenUnchangedAsync()
        {
            // Arrange
            const string opaqueToken = "opaque-or-encrypted-token";
            using var httpClientFactory = new MockHttpClientFactory();
            AppServicesAuthenticationTokenAcquisition acquisition = CreateAcquisition(
                CreateContext(opaqueToken),
                httpClientFactory);

            // Act
            string accessToken = await acquisition.GetAccessTokenForUserAsync(
                new[] { "User.Read" },
                authenticationScheme: null);

            // Assert
            Assert.Equal(opaqueToken, accessToken);
        }

        [Fact]
        public async Task GetAuthenticationResultForUserAsync_ReturnsOpaqueTokenUnchangedAsync()
        {
            // Arrange
            const string opaqueToken = "opaque-or-encrypted-token";
            using var httpClientFactory = new MockHttpClientFactory();
            AppServicesAuthenticationTokenAcquisition acquisition = CreateAcquisition(
                CreateContext(opaqueToken),
                httpClientFactory);

            // Act
            AuthenticationResult result = await acquisition.GetAuthenticationResultForUserAsync(
                new[] { "User.Read" },
                authenticationScheme: null);

            // Assert
            Assert.Equal(opaqueToken, result.AccessToken);
            Assert.Equal(new[] { "User.Read" }, result.Scopes);
            Assert.Null(result.Account);
        }

        private static AppServicesAuthenticationTokenAcquisition CreateAcquisition(
            HttpContext context,
            IHttpClientFactory httpClientFactory)
        {
            var accessor = new HttpContextAccessor { HttpContext = context };
            return new AppServicesAuthenticationTokenAcquisition(
                Substitute.For<IMsalTokenCacheProvider>(),
                accessor,
                httpClientFactory);
        }

        private static DefaultHttpContext CreateContext(string accessToken)
        {
            var context = new DefaultHttpContext();
            context.Request.Headers[AccessTokenHeader] = accessToken;
            return context;
        }

        private static MockHttpMessageHandler CreateAppTokenHandler(string accessToken)
        {
            return new MockHttpMessageHandler
            {
                ExpectedMethod = HttpMethod.Post,
                ResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        $"{{\"token_type\":\"Bearer\",\"expires_in\":3599,\"access_token\":\"{accessToken}\"}}"),
                },
            };
        }
    }
}
