// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Graph;
using Microsoft.Identity.Abstractions;
using NSubstitute;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    public class TokenAcquisitionAuthenticationProviderTests
    {
        private const string AuthorizationHeader = "Bearer acquired-token";
        private const string ExistingAuthorizationHeader = "Bearer existing-token";
        private const string GraphBaseUrl = "https://graph.microsoft.com/v1.0";

        [Theory]
        [InlineData(GraphBaseUrl, "https://graph.microsoft.com/beta/me")]
        [InlineData(GraphBaseUrl, "https://graph.microsoft.com:443/other")]
        [InlineData("https://graph.contoso.test:8443/v1.0", "https://graph.contoso.test:8443/beta/me")]
        [InlineData("https://bücher.example/v1.0", "https://xn--bcher-kva.example/beta/me")]
        [InlineData("https://configured-user@graph.microsoft.com/v1.0", "https://request-user@graph.microsoft.com/beta/me")]
        public async Task AuthenticateRequestAsync_SameCanonicalOrigin_AddsAuthorizationAsync(
            string baseUrl,
            string requestUrl)
        {
            IAuthorizationHeaderProvider authorizationHeaderProvider = CreateAuthorizationHeaderProvider();
            var provider = CreateProvider(authorizationHeaderProvider, baseUrl);
            using var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);

            await provider.AuthenticateRequestAsync(request);

            Assert.Equal(AuthorizationHeader, request.Headers.Authorization?.ToString());
            await AssertAuthorizationHeaderRequestedAsync(authorizationHeaderProvider);
        }

        [Theory]
        [InlineData("https://example.com/v1.0/me")]
        [InlineData("http://graph.microsoft.com/v1.0/me")]
        [InlineData("https://graph.microsoft.com:8443/v1.0/me")]
        [InlineData("https://graph.microsoft.com.example/v1.0/me")]
        [InlineData("https://graph.microsoft.com@evil.example/v1.0/me")]
        public async Task AuthenticateRequestAsync_InvalidInitialDestination_RejectsBeforeTokenAcquisitionAsync(
            string requestUrl)
        {
            IAuthorizationHeaderProvider authorizationHeaderProvider = CreateAuthorizationHeaderProvider();
            var provider = CreateProvider(authorizationHeaderProvider);
            using var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
            request.Headers.Authorization = AuthenticationHeaderValue.Parse(ExistingAuthorizationHeader);

            await Assert.ThrowsAsync<InvalidOperationException>(() => provider.AuthenticateRequestAsync(request));

            Assert.Equal(ExistingAuthorizationHeader, request.Headers.Authorization?.ToString());
            await AssertAuthorizationHeaderNotRequestedAsync(authorizationHeaderProvider);
        }

        [Fact]
        public async Task AuthenticateRequestAsync_NullInitialDestination_RejectsBeforeTokenAcquisitionAsync()
        {
            IAuthorizationHeaderProvider authorizationHeaderProvider = CreateAuthorizationHeaderProvider();
            var provider = CreateProvider(authorizationHeaderProvider);
            using var request = new HttpRequestMessage();

            await Assert.ThrowsAsync<InvalidOperationException>(() => provider.AuthenticateRequestAsync(request));

            await AssertAuthorizationHeaderNotRequestedAsync(authorizationHeaderProvider);
        }

        [Fact]
        public async Task AuthenticateRequestAsync_RelativeInitialDestination_RejectsBeforeTokenAcquisitionAsync()
        {
            IAuthorizationHeaderProvider authorizationHeaderProvider = CreateAuthorizationHeaderProvider();
            var provider = CreateProvider(authorizationHeaderProvider);
            using var request = new HttpRequestMessage(HttpMethod.Get, new Uri("me", UriKind.Relative));

            await Assert.ThrowsAsync<InvalidOperationException>(() => provider.AuthenticateRequestAsync(request));

            await AssertAuthorizationHeaderNotRequestedAsync(authorizationHeaderProvider);
        }

        [Fact]
        public async Task AuthenticateRequestAsync_CustomizerRunsAfterAuthorizationAndCanChangePathAsync()
        {
            IAuthorizationHeaderProvider authorizationHeaderProvider = CreateAuthorizationHeaderProvider();
            var provider = CreateProvider(authorizationHeaderProvider);
            using var request = new HttpRequestMessage(HttpMethod.Get, GraphBaseUrl + "/me");
            bool observedAuthorization = false;
            SetCustomizer(request, message =>
            {
                observedAuthorization = message.Headers.Authorization?.ToString() == AuthorizationHeader;
                message.RequestUri = new Uri("https://graph.microsoft.com/beta/users");
            });

            await provider.AuthenticateRequestAsync(request);

            Assert.True(observedAuthorization);
            Assert.Equal("https://graph.microsoft.com/beta/users", request.RequestUri?.AbsoluteUri);
            Assert.Equal(AuthorizationHeader, request.Headers.Authorization?.ToString());
        }

        [Theory]
        [InlineData("https://example.com/v1.0/me")]
        [InlineData("http://graph.microsoft.com/v1.0/me")]
        public async Task AuthenticateRequestAsync_CustomizerChangesOrigin_ClearsAuthorizationAsync(
            string finalRequestUrl)
        {
            IAuthorizationHeaderProvider authorizationHeaderProvider = CreateAuthorizationHeaderProvider();
            var provider = CreateProvider(authorizationHeaderProvider);
            using var request = new HttpRequestMessage(HttpMethod.Get, GraphBaseUrl + "/me");
            SetCustomizer(request, message => message.RequestUri = new Uri(finalRequestUrl));

            await Assert.ThrowsAsync<InvalidOperationException>(() => provider.AuthenticateRequestAsync(request));

            Assert.Null(request.Headers.Authorization);
            await AssertAuthorizationHeaderRequestedAsync(authorizationHeaderProvider);
        }

        [Fact]
        public async Task AuthenticateRequestAsync_CustomizerClearsDestination_ClearsAuthorizationAsync()
        {
            IAuthorizationHeaderProvider authorizationHeaderProvider = CreateAuthorizationHeaderProvider();
            var provider = CreateProvider(authorizationHeaderProvider);
            using var request = new HttpRequestMessage(HttpMethod.Get, GraphBaseUrl + "/me");
            SetCustomizer(request, message => message.RequestUri = null);

            await Assert.ThrowsAsync<InvalidOperationException>(() => provider.AuthenticateRequestAsync(request));

            Assert.Null(request.Headers.Authorization);
            await AssertAuthorizationHeaderRequestedAsync(authorizationHeaderProvider);
        }

        [Fact]
        public async Task AuthenticateRequestAsync_CustomizerThrows_ClearsAuthorizationAndRethrowsSameExceptionAsync()
        {
            var expectedException = new FormatException("Callback failed.");
            IAuthorizationHeaderProvider authorizationHeaderProvider = CreateAuthorizationHeaderProvider();
            var provider = CreateProvider(authorizationHeaderProvider);
            using var request = new HttpRequestMessage(HttpMethod.Get, GraphBaseUrl + "/me");
            SetCustomizer(request, _ => throw expectedException);

            FormatException actualException = await Assert.ThrowsAsync<FormatException>(
                () => provider.AuthenticateRequestAsync(request));

            Assert.Same(expectedException, actualException);
            Assert.Null(request.Headers.Authorization);
        }

        [Fact]
        public async Task AuthenticateRequestAsync_TokenAcquisitionThrows_DoesNotClearExistingAuthorizationAsync()
        {
            var expectedException = new InvalidOperationException("Token acquisition failed.");
            IAuthorizationHeaderProvider authorizationHeaderProvider = CreateAuthorizationHeaderProvider();
            authorizationHeaderProvider.CreateAuthorizationHeaderAsync(
                    Arg.Any<IEnumerable<string>>(),
                    Arg.Any<AuthorizationHeaderProviderOptions>(),
                    Arg.Any<ClaimsPrincipal?>(),
                    Arg.Any<CancellationToken>())
                .Returns(Task.FromException<string>(expectedException));
            var provider = CreateProvider(authorizationHeaderProvider);
            using var request = new HttpRequestMessage(HttpMethod.Get, GraphBaseUrl + "/me");
            request.Headers.Authorization = AuthenticationHeaderValue.Parse(ExistingAuthorizationHeader);

            InvalidOperationException actualException = await Assert.ThrowsAsync<InvalidOperationException>(
                () => provider.AuthenticateRequestAsync(request));

            Assert.Same(expectedException, actualException);
            Assert.Equal(ExistingAuthorizationHeader, request.Headers.Authorization?.ToString());
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("/v1.0")]
        [InlineData("http://graph.microsoft.com/v1.0")]
        public void Constructor_InvalidBaseUrl_Throws(string? baseUrl)
        {
            IAuthorizationHeaderProvider authorizationHeaderProvider = CreateAuthorizationHeaderProvider();

            Assert.Throws<ArgumentException>(
                () => CreateProvider(authorizationHeaderProvider, baseUrl!));
        }

        [Fact]
        public async Task AuthenticateRequestAsync_UnboundFactoryProvider_FailsClosedAsync()
        {
            IAuthorizationHeaderProvider authorizationHeaderProvider = CreateAuthorizationHeaderProvider();
            var provider = new TokenAcquisitionAuthenticationProvider(
                authorizationHeaderProvider,
                new TokenAcquisitionAuthenticationProviderOption { Scopes = ["User.Read"] });
            using var request = new HttpRequestMessage(HttpMethod.Get, GraphBaseUrl + "/me");

            await Assert.ThrowsAsync<InvalidOperationException>(() => provider.AuthenticateRequestAsync(request));

            await AssertAuthorizationHeaderNotRequestedAsync(authorizationHeaderProvider);
        }

        [Fact]
        public void BindBaseUrl_NullFactoryOrigin_Throws()
        {
            IAuthorizationHeaderProvider authorizationHeaderProvider = CreateAuthorizationHeaderProvider();
            var provider = new TokenAcquisitionAuthenticationProvider(
                authorizationHeaderProvider,
                new TokenAcquisitionAuthenticationProviderOption { Scopes = ["User.Read"] });

            Assert.Throws<ArgumentException>(() => provider.BindBaseUrl(null));
        }

        private static IAuthorizationHeaderProvider CreateAuthorizationHeaderProvider()
        {
            var authorizationHeaderProvider = Substitute.For<IAuthorizationHeaderProvider>();
            authorizationHeaderProvider.CreateAuthorizationHeaderAsync(
                    Arg.Any<IEnumerable<string>>(),
                    Arg.Any<AuthorizationHeaderProviderOptions>(),
                    Arg.Any<ClaimsPrincipal?>(),
                    Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(AuthorizationHeader));
            return authorizationHeaderProvider;
        }

        private static TokenAcquisitionAuthenticationProvider CreateProvider(
            IAuthorizationHeaderProvider authorizationHeaderProvider,
            string baseUrl = GraphBaseUrl)
        {
            return new TokenAcquisitionAuthenticationProvider(
                authorizationHeaderProvider,
                new TokenAcquisitionAuthenticationProviderOption { Scopes = ["User.Read"] },
                baseUrl);
        }

        private static void SetCustomizer(HttpRequestMessage request, Action<HttpRequestMessage> customize)
        {
            var authenticationHandlerOption = new AuthenticationHandlerOption
            {
                AuthenticationProviderOption = new TokenAcquisitionAuthenticationProviderOption
                {
                    AuthorizationHeaderProviderOptions = options =>
                        options.CustomizeHttpRequestMessage = customize,
                },
            };
            GraphRequestContext requestContext = request.GetRequestContext();
            requestContext.MiddlewareOptions[typeof(AuthenticationHandlerOption).Name] = authenticationHandlerOption;
#pragma warning disable CS0618 // Microsoft Graph SDK 4 stores its request context in this property bag.
            request.Properties["GraphRequestContext"] = requestContext;
#pragma warning restore CS0618
        }

        private static Task AssertAuthorizationHeaderRequestedAsync(
            IAuthorizationHeaderProvider authorizationHeaderProvider)
        {
            return authorizationHeaderProvider.Received(1).CreateAuthorizationHeaderAsync(
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<AuthorizationHeaderProviderOptions>(),
                Arg.Any<ClaimsPrincipal?>(),
                Arg.Any<CancellationToken>());
        }

        private static Task AssertAuthorizationHeaderNotRequestedAsync(
            IAuthorizationHeaderProvider authorizationHeaderProvider)
        {
            return authorizationHeaderProvider.DidNotReceive().CreateAuthorizationHeaderAsync(
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<AuthorizationHeaderProviderOptions>(),
                Arg.Any<ClaimsPrincipal?>(),
                Arg.Any<CancellationToken>());
        }
    }
}
