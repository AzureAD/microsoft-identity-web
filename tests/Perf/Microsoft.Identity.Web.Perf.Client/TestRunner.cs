using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web.Test.Common;

namespace Microsoft.Identity.Web.Perf.Client
{
    public class TestRunner
    {
        private IConfiguration Configuration;
        private string NamePrefix = "MIWTestUser";
        private int UsersToSimulate;

        public TestRunner(IConfiguration configuration)
        {
            Configuration = configuration;
            UsersToSimulate = int.Parse(configuration["UsersToSimulate"]);
        }

        public async Task Run()
        {
            Console.WriteLine($"Initialzing tokens for {UsersToSimulate} users");

            var client = new HttpClient();
            client.BaseAddress = new Uri(Configuration["IntegrationTestServicesBaseUri"]);

            var durationInMinutes = int.Parse(Configuration["DurationInMinutes"]);
            var finishTime = DateTime.Now.AddMinutes(durationInMinutes);
            while (DateTime.Now < finishTime)
            {
                for (int i = 1; i <= UsersToSimulate; i++)
                {
                    HttpResponseMessage response;
                    using (HttpRequestMessage httpRequestMessage = new HttpRequestMessage(
                        HttpMethod.Get, Configuration["TestUri"]))
                    {
                        var authResult = await AcquireToken($"{NamePrefix}{i}@{Configuration["TenantDomain"]}");
                        httpRequestMessage.Headers.Add(
                            "Authorization",
                            string.Format(
                                CultureInfo.InvariantCulture,
                                "{0} {1}",
                                "Bearer",
                                authResult.AccessToken));

                        response = await client.SendAsync(httpRequestMessage).ConfigureAwait(false);
                    }

                    Console.WriteLine($"After response. User {i}. IsSuccessStatusCode: {response.IsSuccessStatusCode}");
                    if (!response.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"Response was not successfull. Status code: {response.StatusCode}. {response.ReasonPhrase}");
                        Console.WriteLine(response.ReasonPhrase);
                        Console.WriteLine(await response.Content.ReadAsStringAsync());
                    }
                }
            }
        }

        private async Task<AuthenticationResult> AcquireToken(string upn)
        {
            AuthenticationResult authResult = null;
            try
            {
                var msalPublicClient = PublicClientApplicationBuilder
                   .Create(Configuration["ClientId"])
                   .WithAuthority(TestConstants.AadInstance, TestConstants.Organizations)
                   .Build();

                authResult = await msalPublicClient
                    .AcquireTokenByUsernamePassword(
                    new string[] { Configuration["ApiScopes"] },
                    upn,
                    new NetworkCredential(
                        upn,
                        Configuration["UserPassword"]).SecurePassword)
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in AcquireToken: {ex}");
            }
            return authResult;
        }
    }
}
