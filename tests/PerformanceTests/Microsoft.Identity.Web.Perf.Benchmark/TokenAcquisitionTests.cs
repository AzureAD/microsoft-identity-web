// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using IntegrationTestService;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web.Test.Common;
using Microsoft.Identity.Lab.Api;

namespace Microsoft.Identity.Web.Perf.Benchmark
{
    public class TokenAcquisitionTests
    {
        private readonly WebApplicationFactory<Startup> _factory;
        private HttpClient _client;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. 
        // The GlobalSetup ensures that the _client is not null.
        public TokenAcquisitionTests()
        {
            _factory = new WebApplicationFactory<Startup>();
        }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        [GlobalSetup]
        public async Task GlobalSetupAsync()
        {
            CopyDependencies();
            _client = _factory
            .CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
            });
            string accessToken = (await AcquireTokenForLabUserAsync()).AccessToken;
            _client.DefaultRequestHeaders.Add(
                "Authorization",
                string.Format(
                    CultureInfo.InvariantCulture,
                    "{0} {1}",
                    "Bearer",
                    accessToken));
        }

        [GlobalCleanup]
        public void GlobalCleanup()
        {
            _client.Dispose();
            _factory.Dispose();
        }

        [Benchmark]
        public async Task GetAccessTokenForUserAsync()
        {
            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, TestConstants.SecurePageGetTokenForUserAsync);
            HttpResponseMessage response = await _client.SendAsync(httpRequestMessage);    
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"GetAccessTokenForUserAsync failed. Status code: {response.StatusCode}. Reason phrase: {response.ReasonPhrase}.");
            }
        }

        [Benchmark]
        public async Task GetAccessTokenForAppAsync()
        {
            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, TestConstants.SecurePageGetTokenForAppAsync);
            HttpResponseMessage response = await _client.SendAsync(httpRequestMessage);
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"GetAccessTokenForAppAsync failed. Status code: {response.StatusCode}. Reason phrase: {response.ReasonPhrase}.");
            }
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
                TestConstants.s_oBOApiScope,
                TestConstants.OBOUser,
                labResponse.User.GetOrFetchPassword())
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            return authResult;
        }

        private void CopyDependencies()
        {
            // Copy IntegrationTestService.deps.json file
            var filename = "IntegrationTestService.deps.json";
            var dest = Path.Combine(Directory.GetCurrentDirectory(), filename);
            var source = Path.Combine(Directory.GetCurrentDirectory(), @"..\..\..\..\", filename);
            File.Copy(source, dest, true);
        }
    }
}
