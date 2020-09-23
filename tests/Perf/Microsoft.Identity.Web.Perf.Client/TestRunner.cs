using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
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
        private IPublicClientApplication _msalPublicClient;
        private string[] userAccountIdentifiers;

        public TestRunner(IConfiguration configuration)
        {
            Configuration = configuration;
            UsersToSimulate = int.Parse(configuration["UsersToSimulate"]);
            userAccountIdentifiers = new string[UsersToSimulate + 1];
            _msalPublicClient = PublicClientApplicationBuilder
               .Create(Configuration["ClientId"])
               .WithAuthority(TestConstants.AadInstance, TestConstants.Organizations)
               .Build();
            TokenCacheHelper.EnableSerialization(_msalPublicClient.UserTokenCache);
        }

        public async Task Run()
        {
            Console.WriteLine($"Initialzing tokens for {UsersToSimulate} users");

            // Configuring the http client to trust the self-signed certificate
            var httpClientHandler = new HttpClientHandler();
            httpClientHandler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;

            var client = new HttpClient(httpClientHandler);
            client.BaseAddress = new Uri(Configuration["IntegrationTestServicesBaseUri"]);

            var durationInMinutes = int.Parse(Configuration["DurationInMinutes"]);
            var finishTime = DateTime.Now.AddMinutes(durationInMinutes);
            TimeSpan elapsedTime = TimeSpan.Zero;
            int counter = 0;
            while (DateTime.Now < finishTime)
            { 
                for (int i = 1; i <= UsersToSimulate; i++)
                {
                    if (DateTime.Now < finishTime)
                    {
                        HttpResponseMessage response;
                        using (HttpRequestMessage httpRequestMessage = new HttpRequestMessage(
                            HttpMethod.Get, Configuration["TestUri"]))
                        {
                            var authResult = await AcquireTokenAsync(i);
                            httpRequestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain"));
                            httpRequestMessage.Headers.Add(
                                "Authorization",
                                string.Format(
                                    CultureInfo.InvariantCulture,
                                    "{0} {1}",
                                    "Bearer",
                                    authResult.AccessToken));

                            DateTime start = DateTime.Now;
                            response = await client.SendAsync(httpRequestMessage).ConfigureAwait(false);
                            elapsedTime += DateTime.Now - start;
                            counter++;
                        }

                        Console.WriteLine($"Response received for user {i}. IsSuccessStatusCode: {response.IsSuccessStatusCode}");
                        if (!response.IsSuccessStatusCode)
                        {
                            Console.WriteLine($"Response was not successfull. Status code: {response.StatusCode}. {response.ReasonPhrase}");
                            Console.WriteLine(response.ReasonPhrase);
                            Console.WriteLine(await response.Content.ReadAsStringAsync());
                        }
                    }
                }
            }

            Console.WriteLine($"Total elapse time calling the web API: {elapsedTime} ");
            Console.WriteLine($"Total number of requests: {counter} ");
            Console.WriteLine($"Average time per request: {elapsedTime.Seconds / counter} ");
        }

        private async Task<AuthenticationResult> AcquireTokenAsync(int userIndex)
        {
            var scopes = new string[] { Configuration["ApiScopes"] };
            var upn = $"{NamePrefix}{userIndex}@{Configuration["TenantDomain"]}";

            AuthenticationResult authResult = null;
            try
            {
                var userIdentifier = userAccountIdentifiers[userIndex];                

                try
                {
                    var account = await _msalPublicClient.GetAccountAsync(userIdentifier).ConfigureAwait(false);

                    return await _msalPublicClient.AcquireTokenSilent(scopes, account).ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
                }
                catch (MsalUiRequiredException)
                {
                    authResult = await _msalPublicClient.AcquireTokenByUsernamePassword(
                                                        scopes,
                                                        upn,
                                                        new NetworkCredential(
                                                           upn,
                                                           Configuration["UserPassword"]).SecurePassword)
                                                       .ExecuteAsync(CancellationToken.None)
                                                       .ConfigureAwait(false);

                    userAccountIdentifiers[userIndex] = authResult.Account.HomeAccountId.Identifier;
                    return authResult;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in AcquireTokenAsync: {ex}");
            }
            return authResult;
        }
    }
}
