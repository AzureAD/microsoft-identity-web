using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using IntegrationTestService;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web.Test.Common;
using Microsoft.Identity.Web.Test.LabInfrastructure;

namespace Microsoft.Identity.Web.Perf.Benchmark
{
    public class TokenAcquisitionTests
    {
        private WebApplicationFactory<Startup> _factory;
        private HttpClient _client;

        public TokenAcquisitionTests()
        {
            _factory = new WebApplicationFactory<Startup>();
        }

        [GlobalSetup]
        public void GlobalSetup()
        {
            CopyDependencies();
            _client = _factory
            .CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
            });
            string accessToken = AcquireTokenForLabUserAsync().GetAwaiter().GetResult().AccessToken;
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
        public void GetAccessTokenForUserAsync()
        {
            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, TestConstants.SecurePageGetTokenForUserAsync);
            HttpResponseMessage response = _client.SendAsync(httpRequestMessage).GetAwaiter().GetResult();
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception("Failed.");
            }
        }

        [Benchmark]
        public void GetAccessTokenForAppAsync()
        {
            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, TestConstants.SecurePageGetTokenForAppAsync);
            HttpResponseMessage response = _client.SendAsync(httpRequestMessage).GetAwaiter().GetResult();
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception("Failed.");
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
                TestConstants.OBOApiScope,
                TestConstants.OBOUser,
                new NetworkCredential(
                    TestConstants.OBOUser,
                    labResponse.User.GetOrFetchPassword()).SecurePassword)
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
