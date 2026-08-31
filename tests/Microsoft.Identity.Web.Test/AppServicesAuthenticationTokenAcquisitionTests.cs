// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web.Test.Common.Mocks;
using Microsoft.Identity.Web.TokenCacheProviders;
using Microsoft.IdentityModel.Tokens;
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
        private const string MicrosoftGraphAppId = "00000003-0000-0000-c000-000000000000";
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
        public async Task GetAuthenticationResultForAppAsync_AlwaysThrowsAsync()
        {
            // Arrange
            DefaultHttpContext context = CreateContext("ambient-user-token");
            using var httpClientFactory = new MockHttpClientFactory();
            AppServicesAuthenticationTokenAcquisition acquisition = CreateAcquisition(context, httpClientFactory);

            // Act / Assert
            await Assert.ThrowsAsync<NotSupportedException>(() =>
                acquisition.GetAuthenticationResultForAppAsync(
                    "https://graph.microsoft.com/.default",
                    authenticationScheme: null));
        }

        [Fact]
        public async Task GetAccessTokenForAppAsync_ExplicitTenantUsesTenantAuthorityAsync()
        {
            // Arrange
            using var httpClientFactory = new MockHttpClientFactory();
            MockHttpMessageHandler handler = httpClientFactory.AddMockHandler(CreateAppTokenHandler("app-token"));
            handler.ExpectedUrl = $"https://login.microsoftonline.com/{TenantId}/oauth2/v2.0/token";
            AppServicesAuthenticationTokenAcquisition acquisition = CreateAcquisition(
                new DefaultHttpContext(),
                httpClientFactory);

            // Act
            string accessToken = await acquisition.GetAccessTokenForAppAsync(
                "https://graph.microsoft.com/.default",
                authenticationScheme: null,
                tenant: TenantId);

            // Assert
            Assert.Equal("app-token", accessToken);
        }

        [Fact]
        public async Task GetAccessTokenForAppAsync_OptionsTenantUsesTenantAuthorityAsync()
        {
            // Arrange
            using var httpClientFactory = new MockHttpClientFactory();
            MockHttpMessageHandler handler = httpClientFactory.AddMockHandler(CreateAppTokenHandler("app-token"));
            handler.ExpectedUrl = $"https://login.microsoftonline.com/{TenantId}/oauth2/v2.0/token";
            AppServicesAuthenticationTokenAcquisition acquisition = CreateAcquisition(
                new DefaultHttpContext(),
                httpClientFactory);

            // Act
            string accessToken = await acquisition.GetAccessTokenForAppAsync(
                "https://graph.microsoft.com/.default",
                authenticationScheme: null,
                tokenAcquisitionOptions: new TokenAcquisitionOptions { Tenant = TenantId });

            // Assert
            Assert.Equal("app-token", accessToken);
        }

        [Fact]
        public async Task GetAccessTokenForUserAsync_UnconstrainedOpaqueTokenIsReturnedAsync()
        {
            // Arrange
            using var httpClientFactory = new MockHttpClientFactory();
            AppServicesAuthenticationTokenAcquisition acquisition = CreateAcquisition(
                CreateContext("opaque-token"),
                httpClientFactory);

            // Act
            string accessToken = await acquisition.GetAccessTokenForUserAsync(
                Array.Empty<string>(),
                authenticationScheme: null);

            // Assert
            Assert.Equal("opaque-token", accessToken);
        }

        [Fact]
        public async Task GetAccessTokenForUserAsync_ConstrainedOpaqueTokenIsRejectedAsync()
        {
            // Arrange
            using var httpClientFactory = new MockHttpClientFactory();
            AppServicesAuthenticationTokenAcquisition acquisition = CreateAcquisition(
                CreateContext("opaque-token"),
                httpClientFactory);

            // Act / Assert
            await Assert.ThrowsAsync<NotSupportedException>(() =>
                acquisition.GetAccessTokenForUserAsync(
                    new[] { "User.Read" },
                    authenticationScheme: null));
        }

        [Fact]
        public async Task GetAccessTokenForUserAsync_WhitespaceTenantIsRejectedAsync()
        {
            // Arrange
            using var httpClientFactory = new MockHttpClientFactory();
            AppServicesAuthenticationTokenAcquisition acquisition = CreateAcquisition(
                CreateContext("opaque-token"),
                httpClientFactory);

            // Act / Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                acquisition.GetAccessTokenForUserAsync(
                    Array.Empty<string>(),
                    authenticationScheme: null,
                    tenantId: " "));
        }

        [Fact]
        public async Task GetAccessTokenForUserAsync_ExplicitTenantMatchingTidSucceedsAsync()
        {
            // Arrange
            string accessToken = CreateJwt(MicrosoftGraphAppId, "User.Read");
            using var httpClientFactory = new MockHttpClientFactory();
            AppServicesAuthenticationTokenAcquisition acquisition = CreateAcquisition(
                CreateContext(accessToken),
                httpClientFactory);

            // Act
            string result = await acquisition.GetAccessTokenForUserAsync(
                Array.Empty<string>(),
                authenticationScheme: null,
                tenantId: TenantId);

            // Assert
            Assert.Equal(accessToken, result);
        }

        [Fact]
        public async Task GetAccessTokenForUserAsync_OptionsTenantMatchingTidSucceedsAsync()
        {
            // Arrange
            string accessToken = CreateJwt(MicrosoftGraphAppId, "User.Read");
            using var httpClientFactory = new MockHttpClientFactory();
            AppServicesAuthenticationTokenAcquisition acquisition = CreateAcquisition(
                CreateContext(accessToken),
                httpClientFactory);

            // Act
            string result = await acquisition.GetAccessTokenForUserAsync(
                Array.Empty<string>(),
                authenticationScheme: null,
                tokenAcquisitionOptions: new TokenAcquisitionOptions { Tenant = TenantId });

            // Assert
            Assert.Equal(accessToken, result);
        }

        [Fact]
        public async Task GetAccessTokenForUserAsync_ExplicitTenantMismatchIsRejectedAsync()
        {
            // Arrange
            string accessToken = CreateJwt(MicrosoftGraphAppId, "User.Read");
            using var httpClientFactory = new MockHttpClientFactory();
            AppServicesAuthenticationTokenAcquisition acquisition = CreateAcquisition(
                CreateContext(accessToken),
                httpClientFactory);

            // Act / Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                acquisition.GetAccessTokenForUserAsync(
                    Array.Empty<string>(),
                    authenticationScheme: null,
                    tenantId: "efb0e741-a157-4b84-a3a2-85810c5c3333"));
        }

        [Theory]
        [InlineData(null, false)]
        [InlineData("not-a-guid", true)]
        public async Task GetAccessTokenForUserAsync_ExplicitTenantUnprovableTidIsRejectedAsync(
            string? tokenTenant,
            bool includeTenant)
        {
            // Arrange
            string accessToken = CreateJwt(
                MicrosoftGraphAppId,
                "User.Read",
                tenantId: tokenTenant ?? TenantId,
                includeTenant: includeTenant);
            using var httpClientFactory = new MockHttpClientFactory();
            AppServicesAuthenticationTokenAcquisition acquisition = CreateAcquisition(
                CreateContext(accessToken),
                httpClientFactory);

            // Act / Assert
            await Assert.ThrowsAsync<NotSupportedException>(() =>
                acquisition.GetAccessTokenForUserAsync(
                    Array.Empty<string>(),
                    authenticationScheme: null,
                    tenantId: TenantId));
        }

        [Fact]
        public async Task GetAccessTokenForUserAsync_TenantOnlyDoesNotRequireValidScpAsync()
        {
            // Arrange
            string accessToken = CreateJwtWithPayload(
                $"{{\"aud\":\"{MicrosoftGraphAppId}\",\"scp\":\"User.Read\",\"scp\":\"Mail.Read\"," +
                $"\"tid\":\"{TenantId}\",\"iss\":\"https://login.microsoftonline.com/{TenantId}/v2.0\"}}");
            using var httpClientFactory = new MockHttpClientFactory();
            AppServicesAuthenticationTokenAcquisition acquisition = CreateAcquisition(
                CreateContext(accessToken),
                httpClientFactory);

            // Act
            string result = await acquisition.GetAccessTokenForUserAsync(
                Array.Empty<string>(),
                authenticationScheme: null,
                tenantId: TenantId);

            // Assert
            Assert.Equal(accessToken, result);
        }

        [Theory]
        [InlineData("[\"c8011c90-3395-4c4b-8ef6-7ff7b2c92222\"]")]
        [InlineData("123")]
        [InlineData("\"c8011c90-3395-4c4b-8ef6-7ff7b2c92222\",\"tid\":\"c8011c90-3395-4c4b-8ef6-7ff7b2c92222\"")]
        public async Task GetAccessTokenForUserAsync_NonStringOrDuplicateTidIsRejectedAsync(string tenantValue)
        {
            // Arrange
            string accessToken = CreateJwtWithPayload(
                $"{{\"aud\":\"{MicrosoftGraphAppId}\",\"tid\":{tenantValue}," +
                $"\"iss\":\"https://login.microsoftonline.com/{TenantId}/v2.0\"}}");
            using var httpClientFactory = new MockHttpClientFactory();
            AppServicesAuthenticationTokenAcquisition acquisition = CreateAcquisition(
                CreateContext(accessToken),
                httpClientFactory);

            // Act / Assert
            await Assert.ThrowsAsync<NotSupportedException>(() =>
                acquisition.GetAccessTokenForUserAsync(
                    Array.Empty<string>(),
                    authenticationScheme: null,
                    tenantId: TenantId));
        }

        [Theory]
        [InlineData("api://9b242406-0b86-47e4-8d8b-d654ff153333/.default", "9b242406-0b86-47e4-8d8b-d654ff153333")]
        [InlineData("https://api.contoso.com/orders/.default", "https://api.contoso.com/orders/")]
        public async Task GetAccessTokenForUserAsync_ApprovedAudienceFormsSucceedAsync(
            string requestedScope,
            string audience)
        {
            // Arrange
            string accessToken = CreateJwt(audience, scopes: null);
            using var httpClientFactory = new MockHttpClientFactory();
            AppServicesAuthenticationTokenAcquisition acquisition = CreateAcquisition(
                CreateContext(accessToken),
                httpClientFactory);

            // Act
            string result = await acquisition.GetAccessTokenForUserAsync(
                new[] { requestedScope },
                authenticationScheme: null);

            // Assert
            Assert.Equal(accessToken, result);
        }

        [Theory]
        [InlineData(
            "https://login.microsoftonline.com/" + TenantId + "/v2.0",
            "https://graph.microsoft.us/User.Read")]
        [InlineData(
            "https://login.microsoftonline.us/" + TenantId + "/v2.0",
            "https://graph.microsoft.com/User.Read")]
        [InlineData(
            "https://login.partner.microsoftonline.cn/" + TenantId + "/v2.0",
            "https://graph.microsoft.com/User.Read")]
        [InlineData(
            "https://sts.chinacloudapi.cn/" + TenantId + "/",
            "https://graph.microsoft.us/User.Read")]
        public async Task GetAccessTokenForUserAsync_GraphAliasRejectsCrossCloudIssuerAsync(
            string issuer,
            string requestedScope)
        {
            // Arrange
            string accessToken = CreateJwt(
                MicrosoftGraphAppId,
                "User.Read",
                issuer: issuer);
            using var httpClientFactory = new MockHttpClientFactory();
            AppServicesAuthenticationTokenAcquisition acquisition = CreateAcquisition(
                CreateContext(accessToken),
                httpClientFactory);

            // Act / Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                acquisition.GetAccessTokenForUserAsync(
                    new[] { requestedScope },
                    authenticationScheme: null));
        }

        [Theory]
        [InlineData(
            "https://login.microsoftonline.com/" + TenantId + "/v2.0",
            "https://graph.microsoft.com/User.Read")]
        [InlineData(
            "https://login.microsoftonline.us/" + TenantId + "/v2.0",
            "https://graph.microsoft.us/User.Read")]
        [InlineData(
            "https://login.partner.microsoftonline.cn/" + TenantId + "/v2.0",
            "https://microsoftgraph.chinacloudapi.cn/User.Read")]
        [InlineData(
            "https://sts.chinacloudapi.cn/" + TenantId + "/",
            "https://microsoftgraph.chinacloudapi.cn/User.Read")]
        public async Task GetAccessTokenForUserAsync_GraphAliasAcceptsUnambiguousCloudIssuerAsync(
            string issuer,
            string requestedScope)
        {
            // Arrange
            string accessToken = CreateJwt(
                MicrosoftGraphAppId,
                "User.Read",
                issuer: issuer);
            using var httpClientFactory = new MockHttpClientFactory();
            AppServicesAuthenticationTokenAcquisition acquisition = CreateAcquisition(
                CreateContext(accessToken),
                httpClientFactory);

            // Act
            string result = await acquisition.GetAccessTokenForUserAsync(
                new[] { requestedScope },
                authenticationScheme: null);

            // Assert
            Assert.Equal(accessToken, result);
        }

        [Theory]
        [InlineData("https://graph.microsoft.com/User.Read")]
        [InlineData("https://graph.microsoft.us/User.Read")]
        public async Task GetAccessTokenForUserAsync_GraphAliasAcceptsSharedV1IssuerCompatibilityAsync(
            string requestedScope)
        {
            // Arrange
            string accessToken = CreateJwt(
                MicrosoftGraphAppId,
                "User.Read",
                issuer: $"https://sts.windows.net/{TenantId}/");
            using var httpClientFactory = new MockHttpClientFactory();
            AppServicesAuthenticationTokenAcquisition acquisition = CreateAcquisition(
                CreateContext(accessToken),
                httpClientFactory);

            // Act
            string result = await acquisition.GetAccessTokenForUserAsync(
                new[] { requestedScope },
                authenticationScheme: null);

            // Assert
            Assert.Equal(accessToken, result);
        }

        [Fact]
        public async Task GetAccessTokenForUserAsync_GraphAliasSharedV1IssuerDoesNotBroadenToChinaAsync()
        {
            // Arrange
            string accessToken = CreateJwt(
                MicrosoftGraphAppId,
                "User.Read",
                issuer: $"https://sts.windows.net/{TenantId}/");
            using var httpClientFactory = new MockHttpClientFactory();
            AppServicesAuthenticationTokenAcquisition acquisition = CreateAcquisition(
                CreateContext(accessToken),
                httpClientFactory);

            // Act / Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                acquisition.GetAccessTokenForUserAsync(
                    new[] { "https://microsoftgraph.chinacloudapi.cn/User.Read" },
                    authenticationScheme: null));
        }

        [Fact]
        public async Task GetAccessTokenForUserAsync_UnapprovedGraphAliasIsRejectedAsync()
        {
            // Arrange
            string accessToken = CreateJwt(MicrosoftGraphAppId, "User.Read");
            using var httpClientFactory = new MockHttpClientFactory();
            AppServicesAuthenticationTokenAcquisition acquisition = CreateAcquisition(
                CreateContext(accessToken),
                httpClientFactory);

            // Act / Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                acquisition.GetAccessTokenForUserAsync(
                    new[] { "https://graph.microsoft.de/User.Read" },
                    authenticationScheme: null));
        }

        [Fact]
        public async Task GetAccessTokenForUserAsync_DefaultScopeValidatesAudienceWithoutScpAsync()
        {
            // Arrange
            string accessToken = CreateJwt("https://api.contoso.com", scopes: null);
            using var httpClientFactory = new MockHttpClientFactory();
            AppServicesAuthenticationTokenAcquisition acquisition = CreateAcquisition(
                CreateContext(accessToken),
                httpClientFactory);

            // Act
            string result = await acquisition.GetAccessTokenForUserAsync(
                new[] { "https://api.contoso.com/.default" },
                authenticationScheme: null);

            // Assert
            Assert.Equal(accessToken, result);
        }

        [Fact]
        public async Task GetAccessTokenForUserAsync_AmbiguousAudienceIsRejectedAsync()
        {
            // Arrange
            string accessToken = CreateJwtWithPayload(
                $"{{\"aud\":[\"https://api.contoso.com\",\"https://other.contoso.com\"]," +
                $"\"tid\":\"{TenantId}\",\"iss\":\"https://login.microsoftonline.com/{TenantId}/v2.0\"}}");
            using var httpClientFactory = new MockHttpClientFactory();
            AppServicesAuthenticationTokenAcquisition acquisition = CreateAcquisition(
                CreateContext(accessToken),
                httpClientFactory);

            // Act / Assert
            await Assert.ThrowsAsync<NotSupportedException>(() =>
                acquisition.GetAccessTokenForUserAsync(
                    new[] { "https://api.contoso.com/.default" },
                    authenticationScheme: null));
        }

        [Theory]
        [InlineData("[\"https://api.contoso.com\"]")]
        [InlineData("123")]
        [InlineData("\"https://api.contoso.com\",\"aud\":\"https://api.contoso.com\"")]
        public async Task GetAccessTokenForUserAsync_NonStringOrDuplicateAudienceIsRejectedAsync(
            string audienceValue)
        {
            // Arrange
            string accessToken = CreateJwtWithPayload(
                $"{{\"aud\":{audienceValue},\"tid\":\"{TenantId}\"," +
                $"\"iss\":\"https://login.microsoftonline.com/{TenantId}/v2.0\"}}");
            using var httpClientFactory = new MockHttpClientFactory();
            AppServicesAuthenticationTokenAcquisition acquisition = CreateAcquisition(
                CreateContext(accessToken),
                httpClientFactory);

            // Act / Assert
            await Assert.ThrowsAsync<NotSupportedException>(() =>
                acquisition.GetAccessTokenForUserAsync(
                    new[] { "https://api.contoso.com/.default" },
                    authenticationScheme: null));
        }

        [Theory]
        [InlineData("[\"https://login.microsoftonline.com/tenant/v2.0\"]")]
        [InlineData("123")]
        [InlineData("\"https://login.microsoftonline.com/tenant/v2.0\",\"iss\":\"https://login.microsoftonline.com/tenant/v2.0\"")]
        public async Task GetAccessTokenForUserAsync_GraphAliasRejectsNonStringOrDuplicateIssuerAsync(
            string issuerValue)
        {
            // Arrange
            string accessToken = CreateJwtWithPayload(
                $"{{\"aud\":\"{MicrosoftGraphAppId}\",\"scp\":\"User.Read\"," +
                $"\"tid\":\"{TenantId}\",\"iss\":{issuerValue}}}");
            using var httpClientFactory = new MockHttpClientFactory();
            AppServicesAuthenticationTokenAcquisition acquisition = CreateAcquisition(
                CreateContext(accessToken),
                httpClientFactory);

            // Act / Assert
            await Assert.ThrowsAsync<NotSupportedException>(() =>
                acquisition.GetAccessTokenForUserAsync(
                    new[] { "https://graph.microsoft.com/User.Read" },
                    authenticationScheme: null));
        }

        [Fact]
        public async Task GetAccessTokenForUserAsync_ExactCustomAudienceDoesNotInspectIssuerAsync()
        {
            // Arrange
            string accessToken = CreateJwtWithPayload(
                $"{{\"aud\":\"https://api.contoso.com\",\"iss\":[\"malformed\"],\"tid\":\"{TenantId}\"}}");
            using var httpClientFactory = new MockHttpClientFactory();
            AppServicesAuthenticationTokenAcquisition acquisition = CreateAcquisition(
                CreateContext(accessToken),
                httpClientFactory);

            // Act
            string result = await acquisition.GetAccessTokenForUserAsync(
                new[] { "https://api.contoso.com/.default" },
                authenticationScheme: null);

            // Assert
            Assert.Equal(accessToken, result);
        }

        [Fact]
        public async Task GetAuthenticationResultForUserAsync_UsesActualDelegatedScopesAsync()
        {
            // Arrange
            string accessToken = CreateJwt(MicrosoftGraphAppId, "User.Read Mail.Read");
            using var httpClientFactory = new MockHttpClientFactory();
            AppServicesAuthenticationTokenAcquisition acquisition = CreateAcquisition(
                CreateContext(accessToken),
                httpClientFactory);

            // Act
            AuthenticationResult result = await acquisition.GetAuthenticationResultForUserAsync(
                new[] { "User.Read" },
                authenticationScheme: null);

            // Assert
            Assert.Equal(accessToken, result.AccessToken);
            Assert.Equal(new[] { "Mail.Read", "User.Read" }, result.Scopes.OrderBy(scope => scope));
        }

        [Fact]
        public async Task GetAuthenticationResultForUserAsync_UnconstrainedOpaqueTokenIsReturnedAsync()
        {
            // Arrange
            using var httpClientFactory = new MockHttpClientFactory();
            AppServicesAuthenticationTokenAcquisition acquisition = CreateAcquisition(
                CreateContext("opaque-token"),
                httpClientFactory);

            // Act
            AuthenticationResult result = await acquisition.GetAuthenticationResultForUserAsync(
                Array.Empty<string>(),
                authenticationScheme: null);

            // Assert
            Assert.Equal("opaque-token", result.AccessToken);
            Assert.Empty(result.Scopes);
        }

        [Theory]
        [InlineData("\"scp\":[\"User.Read\",\"Mail.Read\"],")]
        [InlineData("\"scp\":123,")]
        public async Task GetAuthenticationResultForUserAsync_DefaultScopeIgnoresScpAsync(
            string scopeClaim)
        {
            // Arrange
            string accessToken = CreateJwtWithPayload(
                $"{{\"aud\":\"https://api.contoso.com\",{scopeClaim}" +
                $"\"tid\":\"{TenantId}\",\"iss\":\"https://login.microsoftonline.com/{TenantId}/v2.0\"}}");
            using var httpClientFactory = new MockHttpClientFactory();
            AppServicesAuthenticationTokenAcquisition acquisition = CreateAcquisition(
                CreateContext(accessToken),
                httpClientFactory);

            // Act
            AuthenticationResult result = await acquisition.GetAuthenticationResultForUserAsync(
                new[] { "https://api.contoso.com/.default" },
                authenticationScheme: null);

            // Assert
            Assert.Equal(accessToken, result.AccessToken);
            Assert.Empty(result.Scopes);
        }

        [Fact]
        public async Task GetAccessTokenForUserAsync_MissingDelegatedScopeIsRejectedAsync()
        {
            // Arrange
            string accessToken = CreateJwt(MicrosoftGraphAppId, scopes: null);
            using var httpClientFactory = new MockHttpClientFactory();
            AppServicesAuthenticationTokenAcquisition acquisition = CreateAcquisition(
                CreateContext(accessToken),
                httpClientFactory);

            // Act / Assert
            await Assert.ThrowsAsync<NotSupportedException>(() =>
                acquisition.GetAccessTokenForUserAsync(
                    new[] { "User.Read" },
                    authenticationScheme: null));
        }

        [Fact]
        public async Task GetAccessTokenForUserAsync_MismatchedDelegatedScopeIsRejectedAsync()
        {
            // Arrange
            string accessToken = CreateJwt(MicrosoftGraphAppId, "Mail.Read");
            using var httpClientFactory = new MockHttpClientFactory();
            AppServicesAuthenticationTokenAcquisition acquisition = CreateAcquisition(
                CreateContext(accessToken),
                httpClientFactory);

            // Act / Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                acquisition.GetAccessTokenForUserAsync(
                    new[] { "User.Read" },
                    authenticationScheme: null));
        }

        [Theory]
        [InlineData("[\"User.Read\"]")]
        [InlineData("123")]
        [InlineData("\"User.Read\",\"scp\":\"User.Read\"")]
        public async Task GetAccessTokenForUserAsync_NonStringOrDuplicateDelegatedScopeIsRejectedAsync(
            string scopeValue)
        {
            // Arrange
            string accessToken = CreateJwtWithPayload(
                $"{{\"aud\":\"{MicrosoftGraphAppId}\",\"scp\":{scopeValue}," +
                $"\"tid\":\"{TenantId}\",\"iss\":\"https://login.microsoftonline.com/{TenantId}/v2.0\"}}");
            using var httpClientFactory = new MockHttpClientFactory();
            AppServicesAuthenticationTokenAcquisition acquisition = CreateAcquisition(
                CreateContext(accessToken),
                httpClientFactory);

            // Act / Assert
            await Assert.ThrowsAsync<NotSupportedException>(() =>
                acquisition.GetAccessTokenForUserAsync(
                    new[] { "User.Read" },
                    authenticationScheme: null));
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

        private static string CreateJwt(
            string audience,
            string? scopes,
            string tenantId = TenantId,
            string? issuer = null,
            bool includeTenant = true)
        {
            string tenantClaim = includeTenant ? $"\"tid\":\"{tenantId}\"," : string.Empty;
            string scopeClaim = scopes is not null ? $"\"scp\":\"{scopes}\"," : string.Empty;
            issuer ??= $"https://login.microsoftonline.com/{tenantId}/v2.0";
            return CreateJwtWithPayload(
                $"{{\"aud\":\"{audience}\",{scopeClaim}{tenantClaim}\"iss\":\"{issuer}\",\"exp\":4102444800}}");
        }

        private static string CreateJwtWithPayload(string payloadJson)
        {
            string header = Base64UrlEncoder.Encode("{\"alg\":\"none\",\"typ\":\"JWT\"}");
            string payload = Base64UrlEncoder.Encode(Encoding.UTF8.GetBytes(payloadJson));
            return $"{header}.{payload}.";
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
