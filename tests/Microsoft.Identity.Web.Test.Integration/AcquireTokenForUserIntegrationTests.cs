// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web.Test.Common;
using Microsoft.Identity.Web.Test.LabInfrastructure;
using Xunit;

namespace Microsoft.Identity.Web.Test.Integration
{
    public class AcquireTokenForUserIntegrationTests : IClassFixture<WebApplicationFactory<IntegrationTestService.Startup>>
    {
        private readonly WebApplicationFactory<IntegrationTestService.Startup> _factory;

        public AcquireTokenForUserIntegrationTests(WebApplicationFactory<IntegrationTestService.Startup> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task GetTokenForUserAsync()
        {
            // Arrange
            IServiceProvider serviceProvider = null;
            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    serviceProvider = services.BuildServiceProvider();
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
                HttpMethod.Get, "/SecurePage"))
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
                new string[] { "api://f4aa5217-e87c-42b2-82af-5624dd14ee72/.default" },
                TestConstants.OBOUser,
                new NetworkCredential(
                    TestConstants.OBOUser,
                    labResponse.User.GetOrFetchPassword()).SecurePassword)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            return authResult;
        }
    }
}
