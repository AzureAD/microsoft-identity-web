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
        private const string NamePrefix = "MIWTestUser";
        private readonly IConfiguration _configuration;
        private readonly int _usersToSimulate;
        private readonly IPublicClientApplication _msalPublicClient;
        private readonly string[] _userAccountIdentifiers;

        public TestRunner(IConfiguration configuration)
        {
            _configuration = configuration;
            _usersToSimulate = int.Parse(configuration["UsersToSimulate"]);
            _userAccountIdentifiers = new string[_usersToSimulate + 1];
            _msalPublicClient = PublicClientApplicationBuilder
               .Create(_configuration["ClientId"])
               .WithAuthority(TestConstants.AadInstance, TestConstants.Organizations)
               .Build();
            TokenCacheHelper.EnableSerialization(_msalPublicClient.UserTokenCache);
        }

        public async Task Run()
        {
            Console.WriteLine($"Initialzing tokens for {_usersToSimulate} users");

            // Configuring the http client to trust the self-signed certificate
            var httpClientHandler = new HttpClientHandler();
            httpClientHandler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;

            var client = new HttpClient(httpClientHandler);
            client.BaseAddress = new Uri(_configuration["IntegrationTestServicesBaseUri"]);

            var durationInMinutes = int.Parse(_configuration["DurationInMinutes"]);
            var finishTime = DateTime.Now.AddMinutes(durationInMinutes);
            TimeSpan elapsedTime = TimeSpan.Zero;
            int requestsCounter = 0;
            while (DateTime.Now < finishTime)
            { 
                for (int i = 1; i <= _usersToSimulate; i++)
                {
                    if (DateTime.Now < finishTime)
                    {
                        HttpResponseMessage response;
                        using (HttpRequestMessage httpRequestMessage = new HttpRequestMessage(
                            HttpMethod.Get, _configuration["TestUri"]))
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
                            requestsCounter++;
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
            Console.WriteLine($"Total number of requests: {requestsCounter} ");
            Console.WriteLine($"Average time per request: {elapsedTime.Seconds / requestsCounter} ");
        }

        private async Task<AuthenticationResult> AcquireTokenAsync(int userIndex)
        {
            var scopes = new string[] { _configuration["ApiScopes"] };
            var upn = $"{NamePrefix}{userIndex}@{_configuration["TenantDomain"]}";

            AuthenticationResult authResult = null;
            try
            {
                var userIdentifier = _userAccountIdentifiers[userIndex];

                if (!string.IsNullOrEmpty(userIdentifier))
                {
                    var account = await _msalPublicClient.GetAccountAsync(userIdentifier).ConfigureAwait(false);

                    if (account != null)
                    {
                        try
                        {
                            authResult = await _msalPublicClient.AcquireTokenSilent(scopes, account).ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
                        }
                        catch (MsalUiRequiredException)
                        {
                            // No token for the account. Will proceed below
                        }
                    }
                }

                if (authResult == null)
                {
                    authResult = await _msalPublicClient.AcquireTokenByUsernamePassword(
                        scopes,
                        upn,
                        new NetworkCredential(
                            upn,
                            _configuration["UserPassword"]).SecurePassword)
                        .ExecuteAsync(CancellationToken.None)
                        .ConfigureAwait(false);

                    _userAccountIdentifiers[userIndex] = authResult.Account.HomeAccountId.Identifier;
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
