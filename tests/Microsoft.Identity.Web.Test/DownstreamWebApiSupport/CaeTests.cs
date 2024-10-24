// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web.Test.Common;
using Microsoft.Identity.Web.Test.Common.Mocks;
using Microsoft.Net.Http.Headers;
using NSubstitute;
using Xunit;

namespace Microsoft.Identity.Web.Test.DownstreamWebApiSupport
{

    public class CaeTests
    {
        private const string WwwAuthenticateValue =
            "Bearer realm=\"\", authorization_uri=\"https://login.microsoftonline.com/common/oauth2/authorize\", " +
            "client_id=\"00000003-0000-0000-c000-000000000000\", " +
            "errorDescription=\"Continuous access evaluation resulted in challenge with result: InteractionRequired and code: TokenIssuedBeforeRevocationTimestamp\", " +
            "error=\"insufficient_claims\", " +
            "claims=\"eyJhY2Nlc3NfdG9rZW4iOnsibmJmIjp7ImVzc2VudGlhbCI6dHJ1ZSwgInZhbHVlIjoiMTcwMjY4MjE4MSJ9fX0=\"";
        private const string ParsedClaims = @"{""access_token"":{""nbf"":{""essential"":true, ""value"":""1702682181""}}}";

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        private ServiceProvider _provider;
        private IDownstreamApi _downstreamApi;
        private IAuthorizationHeaderProvider _authorizationHeaderProvider;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        [Fact]
        public async Task DownstreamApi_GetForApp_Retries401WithClaimsResponseOnceAsync()
        {
            BuildRequiredServices("GraphApp", downstreamApiOptions =>
            {
                downstreamApiOptions.Scopes = [TestConstants.s_scopeForApp];
                downstreamApiOptions.BaseUrl = TestConstants.GraphBaseUrl;
            }, addUnauthorizedResponse: true, withClaims: true);

            var appVal = await _downstreamApi.GetForAppAsync<EmptyClass>("GraphApp");

            await _authorizationHeaderProvider.ReceivedWithAnyArgs(2).CreateAuthorizationHeaderAsync(Enumerable.Empty<string>());
            await _authorizationHeaderProvider.Received().CreateAuthorizationHeaderAsync(
                Arg.Any<IEnumerable<string>>(), Arg.Is<DownstreamApiOptions>(o => o.AcquireTokenOptions.Claims!.Equals(ParsedClaims, StringComparison.Ordinal)));
        }

        [Fact]
        public async Task DownstreamApi_GetForUser_Retries401WithClaimsResponseOnceAsync()
        {
            BuildRequiredServices("GraphUser", downstreamApiOptions =>
            {
                downstreamApiOptions.Scopes = TestConstants.s_userReadScope;
                downstreamApiOptions.BaseUrl = TestConstants.GraphBaseUrl;
            }, addUnauthorizedResponse: true, withClaims: true);

            var userVal = await _downstreamApi.GetForUserAsync<EmptyClass>("GraphUser");

            await _authorizationHeaderProvider.ReceivedWithAnyArgs(2).CreateAuthorizationHeaderAsync(Enumerable.Empty<string>());
            await _authorizationHeaderProvider.Received().CreateAuthorizationHeaderAsync(
                Arg.Any<IEnumerable<string>>(), Arg.Is<DownstreamApiOptions>(o => o.AcquireTokenOptions.Claims!.Equals(ParsedClaims, StringComparison.Ordinal)));
        }

        [Fact]
        public async Task DownstreamApi_GetForApp_DoesntRetrySucessfullResponseAsync()
        {
            BuildRequiredServices("GraphApp", downstreamApiOptions =>
            {
                downstreamApiOptions.Scopes = [TestConstants.s_scopeForApp];
                downstreamApiOptions.BaseUrl = TestConstants.GraphBaseUrl;
            }, addUnauthorizedResponse: false, withClaims: false);

            var appVal = await _downstreamApi.GetForAppAsync<EmptyClass>("GraphApp");

            await _authorizationHeaderProvider.ReceivedWithAnyArgs(1).CreateAuthorizationHeaderAsync(Enumerable.Empty<string>());
            await _authorizationHeaderProvider.Received().CreateAuthorizationHeaderAsync(
               Arg.Any<IEnumerable<string>>(), Arg.Is<DownstreamApiOptions>(o => o.AcquireTokenOptions.Claims == null));
        }

        [Fact]
        public async Task DownstreamApi_GetForUser_DoesntRetrySucessfullResponseAsync()
        {
            BuildRequiredServices("GraphUser", downstreamApiOptions =>
            {
                downstreamApiOptions.Scopes = [TestConstants.s_scopeForApp];
                downstreamApiOptions.BaseUrl = TestConstants.GraphBaseUrl;
            }, addUnauthorizedResponse: false, withClaims: false);

            var userVal = await _downstreamApi.GetForUserAsync<EmptyClass>("GraphUser");

            await _authorizationHeaderProvider.ReceivedWithAnyArgs(1).CreateAuthorizationHeaderAsync(Enumerable.Empty<string>());
            await _authorizationHeaderProvider.Received().CreateAuthorizationHeaderAsync(
                Arg.Any<IEnumerable<string>>(), Arg.Is<DownstreamApiOptions>(o => o.AcquireTokenOptions.Claims == null));
        }

        [Fact]
        public async Task DownstreamApi_GetForApp_DoesntRetry401WithoutClaimsResponseAsync()
        {
            BuildRequiredServices("GraphApp", downstreamApiOptions =>
            {
                downstreamApiOptions.Scopes = [TestConstants.s_scopeForApp];
                downstreamApiOptions.BaseUrl = TestConstants.GraphBaseUrl;
            }, addUnauthorizedResponse: true, withClaims: false);

            await Assert.ThrowsAsync<HttpRequestException>(() => _downstreamApi.GetForAppAsync<EmptyClass>("GraphApp"));

            await _authorizationHeaderProvider.ReceivedWithAnyArgs(1).CreateAuthorizationHeaderAsync(Enumerable.Empty<string>());
            await _authorizationHeaderProvider.Received().CreateAuthorizationHeaderAsync(
                Arg.Any<IEnumerable<string>>(), Arg.Is<DownstreamApiOptions>(o => o.AcquireTokenOptions.Claims == null));
        }

        [Fact]
        public async Task DownstreamApi_GetForUser_DoesntRetry401WithoutClaimsResponseAsync()
        {
            BuildRequiredServices("GraphUser", downstreamApiOptions =>
            {
                downstreamApiOptions.Scopes = [TestConstants.s_scopeForApp];
                downstreamApiOptions.BaseUrl = TestConstants.GraphBaseUrl;
            }, addUnauthorizedResponse: true, withClaims: false);

            await Assert.ThrowsAsync<HttpRequestException>(() => _downstreamApi.GetForUserAsync<EmptyClass>("GraphUser"));

            await _authorizationHeaderProvider.ReceivedWithAnyArgs(1).CreateAuthorizationHeaderAsync(Enumerable.Empty<string>());
            await _authorizationHeaderProvider.Received().CreateAuthorizationHeaderAsync(
                Arg.Any<IEnumerable<string>>(), Arg.Is<DownstreamApiOptions>(o => o.AcquireTokenOptions.Claims == null));
        }

        private void BuildRequiredServices(string serviceName, Action<DownstreamApiOptions> configureOptions, bool addUnauthorizedResponse, bool withClaims)
        {
            _authorizationHeaderProvider = Substitute.For<IAuthorizationHeaderProvider>();
            _authorizationHeaderProvider.CreateAuthorizationHeaderForAppAsync(string.Empty).ReturnsForAnyArgs("Bearer eyJhY2Nlc3NfdG9rZW4iOg==");
            _authorizationHeaderProvider.CreateAuthorizationHeaderForUserAsync(Enumerable.Empty<string>()).ReturnsForAnyArgs("Bearer eyJhY2Nlc3NfdG9rZW4iOg==");
            _authorizationHeaderProvider.CreateAuthorizationHeaderAsync(Enumerable.Empty<string>()).ReturnsForAnyArgs("Bearer eyJhY2Nlc3NfdG9rZW4iOg==");

            var httpMessageHandler = new QueueHttpMessageHandler();

            if (addUnauthorizedResponse)
            {
                var unauthorizedResponse = new HttpResponseMessage(HttpStatusCode.Unauthorized)
                {
                    Content = new StringContent("{}"),
                };
                
                if (withClaims)
                {
                    unauthorizedResponse.Headers.Add(HeaderNames.WWWAuthenticate, WwwAuthenticateValue);
                }

                httpMessageHandler.AddHttpResponseMessage(unauthorizedResponse);
            }

            httpMessageHandler.AddHttpResponseMessage(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}"),
            });

            var services = new ServiceCollection();
            var httpClientBuilder = services.AddHttpClient(serviceName);
            httpClientBuilder.ConfigurePrimaryHttpMessageHandler(() => httpMessageHandler);

            services.AddTokenAcquisition();
            services.AddLogging();
            services.AddSingleton(_authorizationHeaderProvider);
            services.AddDownstreamApi(serviceName, configureOptions);
            _provider = services.BuildServiceProvider();

            _downstreamApi = _provider.GetRequiredService<IDownstreamApi>();
        }

        // Placeholder for a generic type
        private class EmptyClass { }
    }
}
