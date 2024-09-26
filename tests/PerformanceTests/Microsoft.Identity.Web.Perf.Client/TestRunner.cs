// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
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
        private readonly Metrics _globalMetrics;
        private static readonly object s_metricsLock = new object();

        private readonly DateTime _processingStartTime;
        private readonly DateTime _processingEndTime;
        private readonly HttpClient _httpClient;

        public TestRunner(TestRunnerOptions options)
        {
            _options = options;
            _processingStartTime = DateTime.Now;
            _processingEndTime = DateTime.Now.AddMinutes(options.RuntimeInMinutes);
            _userAccountIdentifiers = new string[options.NumberOfUsersToTest + _options.StartUserIndex];
            _globalMetrics = new Metrics();

            // Configuring the HTTP client to trust the self-signed certificate
            var httpClientHandler = new HttpClientHandler();
            httpClientHandler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;

            _httpClient = new HttpClient(httpClientHandler);
            _httpClient.BaseAddress = new Uri(options.TestServiceBaseUri);
        }

        public async Task RunAsync()
        {
            Console.WriteLine($"Starting test with {_options.NumberOfUsersToTest} users from {_options.StartUserIndex} to {_options.StartUserIndex + _options.NumberOfUsersToTest - 1} using {_options.NumberOfParallelTasks} parallel tasks.");

            if (_options.RunIndefinitely)
            {
                Console.WriteLine("Running indefinitely.");
            }
            else
            {
                Console.WriteLine($"Running for {_options.RuntimeInMinutes} minutes.");
            }

            // Try loading from cache
            TokenCacheHelper.LoadCache();
            IDictionary<int, string> accounts = TokenCacheHelper.GetAccountIdsByUserNumber();
            foreach (var account in accounts)
            {
                if (account.Key < _userAccountIdentifiers.Length)
                {
                    _userAccountIdentifiers[account.Key] = account.Value;
                }
            }

            IEnumerable<Task> CreateTasks()
            {
                var tokenSource = new CancellationTokenSource();

                if (!_options.RunIndefinitely)
                {
                    yield return CreateStopProcessingByTimeoutTaskAsync(tokenSource);
                }
                yield return CreateStopProcessingByUserRequestTaskAsync(tokenSource);
                foreach (Task task in CreateSendRequestsTasks(tokenSource))
                {
                    yield return task;
                }
            }

            await Task.WhenAll(CreateTasks());

            DisplayProgress(true);
        }

        /// <summary>
        /// Until cancelled, continously checks if processing duration has elapsed.
        /// If so, cancells other tasks.
        /// </summary>
        /// <returns>A <see cref="Task"/> that represents this check operation.</returns>
        private Task CreateStopProcessingByTimeoutTaskAsync(CancellationTokenSource tokenSource)
        {
            return Task.Run(async () =>
            {
                while (!tokenSource.Token.IsCancellationRequested && DateTime.Now < _processingEndTime)
                {
                    try
                    {
                        await Task.Delay(_options.TimeCheckDelayInMilliseconds, tokenSource.Token);
                    }
                    catch (TaskCanceledException)
                    {
                        // Ignore this exception. In this case task cancellation is the same as running to completion.
                    }
                }

#if NET8_0_OR_GREATER
                await tokenSource.CancelAsync();
#else
                tokenSource.Cancel();
#endif
                Console.WriteLine("Processing stopped. Duration elapsed.");
            }, tokenSource.Token);
        }

        /// <summary>
        /// Until cancelled, continously checks if user requested cancellation.
        /// If so, cancells other tasks.
        /// </summary>
        /// <returns>A <see cref="Task"/> that represents this check operation.</returns>
        private Task CreateStopProcessingByUserRequestTaskAsync(CancellationTokenSource tokenSource)
        {
            return Task.Run(async () =>
            {
                while (!tokenSource.Token.IsCancellationRequested)
                {
                    try
                    {
                        await Task.Delay(_options.UserInputCheckDelayInMilliseconds, tokenSource.Token);
                    }
                    catch (TaskCanceledException)
                    {
                        // Ignore this exception. In this case task cancellation is the same as running to completion.
                    }

                    if (Console.KeyAvailable)
                    {
                        ConsoleKeyInfo keyInfo = Console.ReadKey(true);
                        if (keyInfo.Key == ConsoleKey.Escape)
                        {
#if NET8_0_OR_GREATER
                            await tokenSource.CancelAsync();
#else
                            tokenSource.Cancel();
#endif
                            Console.WriteLine("Processing stopped. User cancelled.");
                        }
                    }
                }
            }, tokenSource.Token);
        }

        /// <summary>
        /// Creates tasks that send requests.
        /// Divides the number of users to test equally by the specified number of parallel tasks.
        /// </summary>
        /// <returns>A <see cref="Task"/> that represents this send request operation.</returns>
        private IEnumerable<Task> CreateSendRequestsTasks(CancellationTokenSource tokenSource)
        {
            var batchSize = _options.NumberOfUsersToTest < _options.NumberOfParallelTasks
                ? 1
                : (_options.NumberOfUsersToTest / _options.NumberOfParallelTasks);

            var numberOfParallelTasks = _options.NumberOfUsersToTest < _options.NumberOfParallelTasks
                ? _options.NumberOfUsersToTest
                : _options.NumberOfParallelTasks;

            foreach (int batchNumber in Enumerable.Range(0, numberOfParallelTasks))
            {
                var startIndex = batchNumber * batchSize + _options.StartUserIndex;
                var endIndex = startIndex + batchSize - 1;
                endIndex = (endIndex + batchSize) <= _options.StartUserIndex + _options.NumberOfUsersToTest ? endIndex : _options.StartUserIndex + _options.NumberOfUsersToTest - 1;
                yield return Task.Factory.StartNew(() => SendRequestsAsync(startIndex, endIndex, tokenSource.Token), tokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default).Unwrap();
            }
        }

        private async Task SendRequestsAsync(int userStartIndex, int userEndIndex, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var localMetrics = new Metrics();
                var stopwatch = new Stopwatch();
                var exceptionsDuringRun = new StringBuilder();

                for (int userIndex = userStartIndex; userIndex <= userEndIndex && !cancellationToken.IsCancellationRequested; userIndex++)
                {
                    try
                    {
                        HttpResponseMessage response;
                        using (HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, _options.TestUri))
                        {
                            AuthenticationResult authResult = await AcquireTokenAsync(userIndex, localMetrics);
                            if (authResult == null)
                            {
                                localMetrics.TotalAcquireTokenFailures++;
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

                                stopwatch.Start();
                                response = await _httpClient.SendAsync(httpRequestMessage).ConfigureAwait(false);
                                stopwatch.Stop();
                                localMetrics.TotalRequests++;
                                if (authResult?.AuthenticationResultMetadata.TokenSource == TokenSource.Cache)
                                {
                                    localMetrics.TotalTokensReturnedFromCache++;
                                }
                                else
                                {
                                    if (userIndex % 10 == 0)
                                    {
                                        TokenCacheHelper.PersistCache();
                                    }
                                }

                                if (!response.IsSuccessStatusCode)
                                {
                                    var sb = new StringBuilder();
                                    sb.AppendLine((string)$"Response was not successful. Status code: {response.StatusCode}. {response.ReasonPhrase}");
                                    sb.AppendLine(await response.Content.ReadAsStringAsync());
                                    Console.WriteLine(sb);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        localMetrics.TotalExceptions++;
                        Console.WriteLine((string)$"Exception in TestRunner at {userIndex} of {userEndIndex} - {userStartIndex}: {ex.Message}");

                        exceptionsDuringRun.AppendLine((string)$"Exception in TestRunner at {userIndex} of {userEndIndex} - {userStartIndex}: {ex.Message}");
                        exceptionsDuringRun.AppendLine((string)$"{ex}");
                    }
                }
                localMetrics.TotalRequestTimeInMilliseconds += stopwatch.Elapsed.TotalMilliseconds;

                UpdateGlobalMetrics(localMetrics);
                DisplayProgress();

                TokenCacheHelper.PersistCache();

                Logger.PersistExceptions(exceptionsDuringRun);
            }
        }

        private void UpdateGlobalMetrics(Metrics localMetrics)
        {
            lock (s_metricsLock)
            {
                _globalMetrics.TotalRequests += localMetrics.TotalRequests;
                _globalMetrics.TotalRequestTimeInMilliseconds += localMetrics.TotalRequestTimeInMilliseconds;
                _globalMetrics.TotalTokensReturnedFromCache += localMetrics.TotalTokensReturnedFromCache;
                _globalMetrics.TotalMsalLookupTimeInMilliseconds += localMetrics.TotalMsalLookupTimeInMilliseconds;
                _globalMetrics.TotalAcquireTokenFailures += localMetrics.TotalAcquireTokenFailures;
                _globalMetrics.TotalExceptions += localMetrics.TotalExceptions;
            }
        }

        private void DisplayProgress(bool summary=false)
        {
            var stringBuilder = new StringBuilder();
            if (summary)
            {
                stringBuilder.AppendLine("------------------------------------------------------------------------");
                stringBuilder.AppendLine("Summary");
            }
            stringBuilder.AppendLine("------------------------------------------------------------------------");
            stringBuilder.AppendLine((string)$"Run time: {_processingStartTime} - {DateTime.Now} = {DateTime.Now - _processingStartTime}.");
            stringBuilder.AppendLine((string)$"Total number of users: {_options.NumberOfUsersToTest} users from {_options.StartUserIndex} to {_options.StartUserIndex + _options.NumberOfUsersToTest - 1}.");

            lock (s_metricsLock)
            {
                stringBuilder.AppendLine((string)$"Loop: {_globalMetrics.TotalRequests / _options.NumberOfUsersToTest}");
                stringBuilder.AppendLine((string)$"Total requests: {_globalMetrics.TotalRequests}.");
                stringBuilder.AppendLine((string)$"Average request time: {_globalMetrics.AverageRequestTimeInMilliseconds:0.0000} ms.");
#if DEBUGGING_RUNNER
                stringBuilder.AppendLine((string)$"Cache requests: {_globalMetrics.TotalTokensReturnedFromCache}.");
                stringBuilder.AppendLine((string)$"Average cache time: {_globalMetrics.AverageMsalLookupTimeInMilliseconds:0.0000} ms.");
                stringBuilder.AppendLine((string)$"AuthRequest failures: {_globalMetrics.TotalAcquireTokenFailures}. Generic failures: {_globalMetrics.TotalExceptions}.");
#endif
            }

            Console.WriteLine(stringBuilder);
        }

        private async Task<AuthenticationResult> AcquireTokenAsync(int userIndex, Metrics localMetrics)
        {
            var scopes = new string[] { _options.ApiScopes };
            var upn = $"{_options.UsernamePrefix}{userIndex}@{_options.TenantDomain}";

            var builder = PublicClientApplicationBuilder
                           .Create(_options.ClientId)
                           .WithAuthority(TestConstants.AadInstance, TestConstants.Organizations);

            if (_options.EnableMsalLogging)
            {
                builder.WithLogging(Logger.Log, LogLevel.Error, false);
            }

            var msalPublicClient = builder.Build();

            TokenCacheHelper.EnableSerialization(msalPublicClient.UserTokenCache);

            AuthenticationResult authResult = null;
            try
            {
                try
                {
                    var identifier = _userAccountIdentifiers[userIndex];
                    IAccount account = null;
                    if (identifier != null)
                    {
                        var stopwatch = new Stopwatch();
                        stopwatch.Start();
                        account = await msalPublicClient.GetAccountAsync(identifier).ConfigureAwait(false);
                        stopwatch.Stop();
                        localMetrics.TotalMsalLookupTimeInMilliseconds += stopwatch.Elapsed.TotalMilliseconds;
                    }

                    authResult = await msalPublicClient.AcquireTokenSilent(scopes, account).ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
                    return authResult;
                }
                catch (MsalUiRequiredException)
                {
                    // Delay to prevent spamming the STS with calls.
                    await Task.Delay(_options.RequestDelayInMilliseconds);

                    authResult = await msalPublicClient.AcquireTokenByUsernamePassword(
                                                        scopes,
                                                        upn,
                                                        _options.UserPassword)
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
    }
}
