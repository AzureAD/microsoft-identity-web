// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
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
        private TimeSpan elapsedTimeInMsalCacheLookup;
        private int numberOfMsalCacheLookups;

        public TestRunner(IConfiguration configuration)
        {
            _configuration = configuration;
            _usersToSimulate = int.Parse(configuration["UsersToSimulate"]);
            _userAccountIdentifiers = new string[_usersToSimulate + 1];

        }

        public async Task Run()
        {
            Console.WriteLine($"Starting testing with {_usersToSimulate} users.");

            IDictionary<int, string> accounts = ScalableTokenCacheHelper.GetAccountIdsByUserNumber();
            foreach (var account in accounts)
            {
                if (account.Key < _userAccountIdentifiers.Length)
                {
                    _userAccountIdentifiers[account.Key] = account.Value;
                }
            }

            // Configuring the http client to trust the self-signed certificate
            var httpClientHandler = new HttpClientHandler();
            httpClientHandler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;

            var client = new HttpClient(httpClientHandler);
            client.BaseAddress = new Uri(_configuration["IntegrationTestServicesBaseUri"]);

            var durationInMinutes = int.Parse(_configuration["DurationInMinutes"]);
            DateTime startOverall = DateTime.Now;
            var finishTime = DateTime.Now.AddMinutes(durationInMinutes);
            TimeSpan elapsedTime = TimeSpan.Zero;
            int requestsCounter = 0;
            int loop = 0;
            int tokenReturnedFromCache = 0;
            while (DateTime.Now < finishTime)
            {
                loop++;
                for (int i = 1; i <= _usersToSimulate; i++)
                {
                    if (DateTime.Now < finishTime)
                    {
                        bool fromCache = false;
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
                            if (authResult.AuthenticationResultMetadata.TokenSource == TokenSource.Cache)
                            {
                                tokenReturnedFromCache++;
                                fromCache = true;
                            }
                        }

                        Console.WriteLine($"Response received for user {i}. Loop Number {loop}. IsSuccessStatusCode: {response.IsSuccessStatusCode}. MSAL Token cache used: {fromCache}");

                        if (!response.IsSuccessStatusCode)
                        {
                            Console.WriteLine($"Response was not successful. Status code: {response.StatusCode}. {response.ReasonPhrase}");
                            Console.WriteLine(response.ReasonPhrase);
                            Console.WriteLine(await response.Content.ReadAsStringAsync());
                        }
                    }
                }
            }

            Console.WriteLine($"Total elapse time calling the web API: {elapsedTime} ");
            Console.WriteLine($"Total number of requests: {requestsCounter} ");
            Console.WriteLine($"Average time per request: {elapsedTime.TotalSeconds / requestsCounter} ");
            Console.WriteLine($"Time spent in MSAL cache lookup: {elapsedTimeInMsalCacheLookup} ");
            Console.WriteLine($"Number of MSAL cache look-ups: {numberOfMsalCacheLookups} ");
            Console.WriteLine($"Average time per lookup: {elapsedTimeInMsalCacheLookup.TotalSeconds / numberOfMsalCacheLookups}");
            var totalAccounts = await _msalPublicClient.GetAccountsAsync().ConfigureAwait(false);
            Console.WriteLine($"Total number of accounts in the MSAL cache: {totalAccounts.Count()}");
            Console.WriteLine($"Total number of tokens returned from the MSAL cache based on auth result: {tokenReturnedFromCache}");
            Console.WriteLine($"Start time: {startOverall}");
            Console.WriteLine($"End time: {DateTime.Now}");
        }

        private async Task<AuthenticationResult> AcquireTokenAsync(int userIndex)
        {
            var scopes = new string[] { _configuration["ApiScopes"] };
            var upn = $"{NamePrefix}{userIndex}@{_configuration["TenantDomain"]}";

            var _msalPublicClient = PublicClientApplicationBuilder
                           .Create(_configuration["ClientId"])
                           .WithAuthority(TestConstants.AadInstance, TestConstants.Organizations)
                           .WithLogging(Log, LogLevel.Info, false)
                           .Build();
            ScalableTokenCacheHelper.EnableSerialization(_msalPublicClient.UserTokenCache);

            AuthenticationResult authResult = null;
            try
            {
                try
                {
                    var identifier = _userAccountIdentifiers[userIndex];
                    IAccount account = null;
                    if (identifier != null)
                    {
                        DateTime start = DateTime.Now;
                        account = await _msalPublicClient.GetAccountAsync(identifier).ConfigureAwait(false);
                        elapsedTimeInMsalCacheLookup += DateTime.Now - start;
                        numberOfMsalCacheLookups++;
                    }

                    authResult = await _msalPublicClient.AcquireTokenSilent(scopes, account).ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
                    return authResult;
                }
                catch (MsalUiRequiredException)
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
                    return authResult;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in AcquireTokenAsync: {ex}");
            }
            return authResult;
        }

        private static void Log(LogLevel level, string message, bool containsPii)
        {
            string logs = ($"{level} {message}");
            StringBuilder sb = new StringBuilder();
            sb.Append(logs);
            File.AppendAllText(System.Reflection.Assembly.GetExecutingAssembly().Location + ".msalLogs.txt", sb.ToString());
            sb.Clear();
        }
    }
}
