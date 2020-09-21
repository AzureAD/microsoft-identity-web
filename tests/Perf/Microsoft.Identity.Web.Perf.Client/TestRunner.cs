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
        private string[] Tokens;
        private string NamePrefix = "MIWTestUser";
        private int UsersToSimulate;

        public TestRunner(IConfiguration configuration)
        {
            Configuration = configuration;
            UsersToSimulate = Int32.Parse(configuration["UsersToSimulate"]);
            Tokens = new string[UsersToSimulate + 1];
        }

        public async Task Run()
        {
            Console.WriteLine($"Initialzing tokens for {UsersToSimulate} users");
            await InitTokens();
            
            var client = new HttpClient();
            client.BaseAddress = new Uri(Configuration["IntegrationTestServicesBaseUri"]);

            var durationInMinutes = Int32.Parse(Configuration["DurationInMinutes"]);
            var finishTime = DateTime.Now.AddMinutes(durationInMinutes);
            while (DateTime.Now < finishTime)
            {
                for (int i = 1; i <= UsersToSimulate; i++)
                {
                    HttpResponseMessage response;
                    using (HttpRequestMessage httpRequestMessage = new HttpRequestMessage(
                        HttpMethod.Get, Configuration["TestUri"]))
                    {
                        httpRequestMessage.Headers.Add(
                            "Authorization",
                            string.Format(
                                CultureInfo.InvariantCulture,
                                "{0} {1}",
                                "Bearer",
                                Tokens[i]));

                        response = await client.SendAsync(httpRequestMessage).ConfigureAwait(false);
                    }

                    Console.WriteLine($"Response {i} {response.IsSuccessStatusCode}");
                    if (!response.IsSuccessStatusCode)
                    {
                        throw new Exception("Response was not success.");
                    }
                }
            }
        }

        private async Task InitTokens()
        {
            for (int suffix = 1; suffix <= UsersToSimulate; suffix++)
            {
                var result = await AcquireToken($"{NamePrefix}{suffix}@{Configuration["TenantDomain"]}");
                Tokens[suffix] = result.AccessToken;
            }
        }

        private async Task<AuthenticationResult> AcquireToken(string upn)
        {
            var msalPublicClient = PublicClientApplicationBuilder
               .Create(Configuration["ClientId"])
               .WithAuthority(TestConstants.AadInstance, TestConstants.Organizations)
               .Build();
            AuthenticationResult authResult = null;
            try
            {
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
