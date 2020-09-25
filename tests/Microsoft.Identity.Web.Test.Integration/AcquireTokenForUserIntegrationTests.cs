// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using IntegrationTestService;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web.Test.Common;
using Microsoft.Identity.Web.Test.LabInfrastructure;
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

        [Theory(Skip = "re-enable after merge to master as the API needs to be different for integration and perf"+
            " because of the variability on cache, and the tenant")]
        [InlineData(TestConstants.SecurePageGetTokenForAppAsync)]
        [InlineData(TestConstants.SecurePageGetTokenForUserAsync)]
        [InlineData(TestConstants.SecurePageCallDownstreamWebApi)]
        [InlineData(TestConstants.SecurePageCallDownstreamWebApiGeneric)]
       // [InlineData(TestConstants.SecurePageCallMicrosoftGraph)]
        [InlineData(TestConstants.SecurePageCallDownstreamWebApiGenericWithTokenAcquisitionOptions)]
       // [InlineData(TestConstants.SecurePageCallMicrosoftGraph, false)]
       // [InlineData(TestConstants.SecurePageCallDownstreamWebApi, false)]
       // [InlineData(TestConstants.SecurePageCallDownstreamWebApiGeneric, false)]
        public async Task GetTokenForUserAsync(
                string webApiUrl,
                bool addInMemoryTokenCache = true)
        {
            // Arrange
            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    if (!addInMemoryTokenCache)
                    {
                        services.AddDistributedMemoryCache();
                        services.AddDistributedTokenCaches();
                    }
                    else
                    {
                        services.AddInMemoryTokenCaches();
                    }

                    services.BuildServiceProvider();
                });
            })
            .CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
            });

            var result = await AcquireTokenForLabUserAsync().ConfigureAwait(false);

            // Act
            HttpResponseMessage response;
            using (HttpRequestMessage httpRequestMessage = new HttpRequestMessage(
                HttpMethod.Get, webApiUrl))
            {
                httpRequestMessage.Headers.Add(
                    Constants.Authorization,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "{0} {1}",
                        Constants.Bearer,
                        result.AccessToken));
                response = await client.SendAsync(httpRequestMessage).ConfigureAwait(false);
            }

            // Assert
            Assert.True(response.IsSuccessStatusCode);
        }

        private static async Task<AuthenticationResult> AcquireTokenForLabUserAsync()
        {
            var labResponse = await LabUserHelper.GetSpecificUserAsync(TestConstants.OBOUser).ConfigureAwait(false);
            var msalPublicClient = PublicClientApplicationBuilder
               .Create(TestConstants.OBOClientSideClientId)
               .WithAuthority(labResponse.Lab.Authority, TestConstants.Organizations)
               .Build();

            AuthenticationResult authResult = await msalPublicClient
                .AcquireTokenByUsernamePassword(
                TestConstants.OBOApiScope,
                TestConstants.OBOUser,
                new NetworkCredential(
                    TestConstants.OBOUser,
                    labResponse.User.GetOrFetchPassword()).SecurePassword)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            return authResult;
        }
    }
#endif //FROM_GITHUB_ACTION
}
