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
        private IPublicClientApplication MsalPublicClient;
        private string[] userAccountIdentifiers;

        public TestRunner(IConfiguration configuration)
        {
            Configuration = configuration;
            UsersToSimulate = int.Parse(configuration["UsersToSimulate"]);
            userAccountIdentifiers = new string[UsersToSimulate + 1];
            MsalPublicClient = PublicClientApplicationBuilder
               .Create(Configuration["ClientId"])
               .WithAuthority(TestConstants.AadInstance, TestConstants.Organizations)
               .Build();
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
                    if (DateTime.Now < finishTime)
                    {
                        break;
                    }

                    HttpResponseMessage response;
                    using (HttpRequestMessage httpRequestMessage = new HttpRequestMessage(
                        HttpMethod.Get, Configuration["TestUri"]))
                    {
                        var authResult = await AcquireToken(i);
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

        private async Task<AuthenticationResult> AcquireToken(int userIndex)
        {
            var scopes = new string[] { Configuration["ApiScopes"] };
            var upn = $"{NamePrefix}{userIndex}@{Configuration["TenantDomain"]}";

            AuthenticationResult authResult = null;
            try
            {
                var userIdentifier = userAccountIdentifiers[userIndex];
                
                if (!string.IsNullOrEmpty(userIdentifier))
                {
                    var account = await MsalPublicClient.GetAccountAsync(userIdentifier).ConfigureAwait(false);

                    if (account != null)
                    {
                        try
                        {
                            authResult = await MsalPublicClient.AcquireTokenSilent(scopes, account).ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
                        }
                        catch (MsalUiRequiredException)
                        {
                            // No token for the account. Will proceed below
                        }
                    }
                }

                if (authResult == null)
                {
                    authResult = await MsalPublicClient.AcquireTokenByUsernamePassword(
                        scopes,
                        upn,
                        new NetworkCredential(
                            upn,
                            Configuration["UserPassword"]).SecurePassword)
                        .ExecuteAsync(CancellationToken.None)
                        .ConfigureAwait(false);
                    
                    userAccountIdentifiers[userIndex] = authResult.Account.HomeAccountId.Identifier;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in AcquireToken: {ex}");
            }
            return authResult;
        }
    }
}
