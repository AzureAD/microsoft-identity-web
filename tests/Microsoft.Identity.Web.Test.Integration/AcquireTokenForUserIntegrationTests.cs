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

#pragma warning disable CS0618 // Obsolete
            AuthenticationResult authResult = await msalPublicClient
                .AcquireTokenByUsernamePassword(
                TestConstants.s_oBOApiScope,
                TestConstants.OBOUser,
                labResponse.User.GetOrFetchPassword())
                .ExecuteAsync(CancellationToken.None)
                ;
#pragma warning restore CS0618 // Obsolete

            return authResult;
        }
    }
#endif //FROM_GITHUB_ACTION
}
