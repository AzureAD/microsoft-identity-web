// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web.Test.Common;

namespace Microsoft.Identity.Web.Perf.Client
{
    public class TestRunner
    {
        private readonly TestRunnerOptions _options;
        private readonly string[] _userAccountIdentifiers;
        private TimeSpan elapsedTimeInMsalCacheLookup;
        private int userStartIndex;
        private int userEndIndex;

        public TestRunner(TestRunnerOptions options)
        {
            _options = options;
            userStartIndex = options.UserNumberToStart;
            userEndIndex = options.UserNumberToStart + options.UsersCountToTest;
            _userAccountIdentifiers = new string[userEndIndex + 1];
        }

        public async Task Run()
        {
            Console.WriteLine($"Starting testing with {userEndIndex - userStartIndex} users.");

            // Try loading from cache
            ScalableTokenCacheHelper.LoadCache();
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
            client.BaseAddress = new Uri(_options.TestServiceBaseUri);

            DateTime startOverall = DateTime.Now;
            var finishTime = DateTime.Now.AddMinutes(_options.RuntimeInMinutes);
            TimeSpan elapsedTime = TimeSpan.Zero;
            int requestsCounter = 0;
            int authRequestFailureCount = 0;
            int catchAllFailureCount = 0;
            int loop = 0;
            int tokenReturnedFromCache = 0;

            bool cancelProcessing = false;

            StringBuilder exceptionsDuringRun = new StringBuilder();

            while (!cancelProcessing)
            {
                loop++;
                for (int i = userStartIndex; i <= userEndIndex; i++)
                {
                    bool fromCache = false;
                    try
                    {
                        HttpResponseMessage response;
                        using (HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, _options.TestUri))
                        {
                            AuthenticationResult authResult = await AcquireTokenAsync(i);
                            if (authResult == null)
                            {
                                authRequestFailureCount++;
                            }
                            else
                            {
                                httpRequestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain"));
                                httpRequestMessage.Headers.Add(
                                    "Authorization",
                                    string.Format(
                                        CultureInfo.InvariantCulture,
                                        "{0} {1}",
                                        "Bearer",
                                        authResult?.AccessToken));

                                DateTime start = DateTime.Now;
                                response = await client.SendAsync(httpRequestMessage).ConfigureAwait(false);
                                elapsedTime += DateTime.Now - start;
                                requestsCounter++;
                                if (authResult?.AuthenticationResultMetadata.TokenSource == TokenSource.Cache)
                                {
                                    tokenReturnedFromCache++;
                                    fromCache = true;
                                }
                                else
                                {
                                    if (i % 10 == 0)
                                    {
                                        ScalableTokenCacheHelper.PersistCache();
                                    }
                                }

                                if (!response.IsSuccessStatusCode)
                                {
                                    Console.WriteLine($"Response was not successful. Status code: {response.StatusCode}. {response.ReasonPhrase}");
                                    Console.WriteLine(response.ReasonPhrase);
                                    Console.WriteLine(await response.Content.ReadAsStringAsync());
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        catchAllFailureCount++;
                        Console.WriteLine($"Exception in TestRunner at {i} of {userEndIndex} - {userStartIndex}: {ex.Message}");

                        exceptionsDuringRun.AppendLine($"Exception in TestRunner at {i} of {userEndIndex} - {userStartIndex}: {ex.Message}");
                        exceptionsDuringRun.AppendLine($"{ex}");
                    }

                    Console.Title = $"[{userStartIndex} - {userEndIndex}] #: {i}, Loop: {loop}, " +
                        $"Time: {(DateTime.Now - startOverall).TotalMinutes:0.00}, " +
                        $"Req: {requestsCounter}, Cache: {tokenReturnedFromCache}: {fromCache}, " +
                        $"AuthFail: {authRequestFailureCount}, Fail: {catchAllFailureCount}";

                    if (Console.KeyAvailable)
                    {
                        ConsoleKeyInfo keyInfo = Console.ReadKey();
                        if ((keyInfo.Modifiers == ConsoleModifiers.Control && (keyInfo.Key == ConsoleKey.X || keyInfo.Key == ConsoleKey.C)) || keyInfo.Key == ConsoleKey.Escape)
                        {
                            cancelProcessing = true;
                            break;
                        }
                    }
                }

                UpdateConsoleProgress(startOverall, elapsedTime, requestsCounter, tokenReturnedFromCache, authRequestFailureCount, catchAllFailureCount);

                ScalableTokenCacheHelper.PersistCache();

                if (DateTime.Now >= finishTime)
                {
                    cancelProcessing = true;
                }
            }

            File.AppendAllText(System.Reflection.Assembly.GetExecutingAssembly().Location + ".exceptions.log", exceptionsDuringRun.ToString());
            Console.WriteLine("Test run complete");
        }

        private void UpdateConsoleProgress(DateTime startOverall, TimeSpan elapsedTime, int requestsCounter, 
            int tokenReturnedFromCache, int authRequestFailureCount, int catchAllFailureCount)
        {
            Console.WriteLine($"Run-time time: {startOverall} - {DateTime.Now} = {DateTime.Now - startOverall}");
            Console.WriteLine($"WebAPI Time: {elapsedTime}");
            Console.WriteLine($"Total number of users: {userEndIndex - userStartIndex}. [{userEndIndex} - {userStartIndex}]");
            Console.WriteLine($"AuthRequest Failures: {authRequestFailureCount}. Generic failures: {catchAllFailureCount}");
            Console.WriteLine($"Total requests: {requestsCounter}, avg. time per request: {(elapsedTime.TotalSeconds / requestsCounter):0.0000}");
            Console.WriteLine($"Cache requests: {tokenReturnedFromCache}. Avg. cache time: {(elapsedTimeInMsalCacheLookup.TotalSeconds / tokenReturnedFromCache):0.0000}. (Total: {elapsedTimeInMsalCacheLookup})");
        }

        private async Task<AuthenticationResult> AcquireTokenAsync(int userIndex)
        {
            var scopes = new string[] { _options.ApiScopes };
            var upn = $"{_options.UsernamePrefix}{userIndex}@{_options.TenantDomain}";

            var _msalPublicClient = PublicClientApplicationBuilder
                           .Create(_options.ClientId)
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
                                                            _options.UserPassword).SecurePassword)
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

        private static string s_msallogfile = System.Reflection.Assembly.GetExecutingAssembly().Location + ".msalLogs.txt";
        private static StringBuilder s_log = new StringBuilder();
        private static volatile bool s_isLogging = false;
        private static object s_logLock = new object();

        private static void Log(LogLevel level, string message, bool containsPii)
        {
            StringBuilder tempBuilder = new StringBuilder();
            bool writeToDisk = false;
            lock (s_logLock)
            {
                string logs = ($"{level} {message}");
                if (!s_isLogging)
                {
                    s_isLogging = true;
                    writeToDisk = true;
                    tempBuilder.Append(s_log);
                    tempBuilder.Append(logs);
                    s_log.Clear();
                }
                else
                {
                    s_log.Append(logs);
                }
            }
            
            if(!writeToDisk)
            {
                return;
            }

            s_isLogging = true;
            try
            {
                File.AppendAllText(s_msallogfile, tempBuilder.ToString());
                tempBuilder.Clear();
            }
            finally
            {
                s_isLogging = false;
            }
        }
    }
}
