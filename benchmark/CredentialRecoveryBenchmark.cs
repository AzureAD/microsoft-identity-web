// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Net;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.Extensibility;
using Microsoft.Identity.Web.TokenCacheProviders.InMemory;

namespace Benchmarks
{
    /// <summary>
    /// Measures credential selection and cached acquisition without credentials or external network access.
    /// </summary>
    [MemoryDiagnoser]
    public class CredentialRecoveryBenchmark
    {
        private const int SuccessfulBatchSize = 4096;
        private const int FallbackBatchSize = 512;
        private const string Scope = "https://graph.microsoft.com/.default";
        private BenchmarkServices _cached = null!;
        private BenchmarkServices _failed = null!;
        private IConfidentialClientApplication _cachedApplication = null!;
        private BenchmarkServices[] _cold = [];

        /// <summary>
        /// Warms a successful fallback and performs the first unsuccessful selection outside measurement.
        /// </summary>
        [GlobalSetup]
        public async Task SetupAsync()
        {
            _cached = CreateServices(includeAssertionFile: true, includeSecret: true);
            _failed = CreateServices(includeAssertionFile: true, includeSecret: false);
            _cachedApplication = await _cached.ApplicationProvider.GetConfidentialClientApplicationAsync();
            await _cached.Acquisition.GetAuthenticationResultForAppAsync(Scope);
            await RepeatedSelectionFailureAsync();
        }

        /// <summary>
        /// Measures reuse of the application selected after assertion-file fallback.
        /// </summary>
        [Benchmark]
        public Task<IConfidentialClientApplication> CachedApplicationAfterFallbackAsync() =>
            _cached.ApplicationProvider.GetConfidentialClientApplicationAsync();

        /// <summary>
        /// Measures the normal access-token cache-hit path after successful fallback.
        /// </summary>
        [Benchmark]
        public Task<AuthenticationResult> CachedTokenAfterFallbackAsync() =>
            _cached.Acquisition.GetAuthenticationResultForAppAsync(Scope);

        /// <summary>
        /// Prepares independent applications outside the cold-success measurement.
        /// </summary>
        [IterationSetup(Target = nameof(ColdSuccessfulSelectionAsync))]
        public void SetupColdSuccess() => SetupCold(includeAssertionFile: false, SuccessfulBatchSize);

        /// <summary>
        /// Prepares independent applications outside the cold-fallback measurement.
        /// </summary>
        [IterationSetup(Target = nameof(ColdFallbackSelectionAsync))]
        public void SetupColdFallback() => SetupCold(includeAssertionFile: true, FallbackBatchSize);

        /// <summary>
        /// Measures first selection and application construction with an available secret.
        /// </summary>
        [Benchmark(OperationsPerInvoke = SuccessfulBatchSize)]
        public Task ColdSuccessfulSelectionAsync() => BuildColdApplicationsAsync();

        /// <summary>
        /// Measures first selection after a real assertion-file loading failure.
        /// </summary>
        [Benchmark(OperationsPerInvoke = FallbackBatchSize)]
        public Task ColdFallbackSelectionAsync() => BuildColdApplicationsAsync();

        /// <summary>
        /// Measures subsequent selection when no credential has succeeded.
        /// </summary>
        [Benchmark]
        public async Task RepeatedSelectionFailureAsync()
        {
            try
            {
                await _failed.ApplicationProvider.GetConfidentialClientApplicationAsync();
            }
            catch (ArgumentException exception) when (exception.Message.StartsWith("IDW10109", StringComparison.Ordinal))
            {
                return;
            }

            throw new InvalidOperationException("The unavailable assertion file unexpectedly produced an application.");
        }

        /// <summary>
        /// Disposes the cold applications outside the measured iteration.
        /// </summary>
        [IterationCleanup(Targets = new[] { nameof(ColdSuccessfulSelectionAsync), nameof(ColdFallbackSelectionAsync) })]
        public void CleanupCold()
        {
            foreach (BenchmarkServices services in _cold)
            {
                services.Dispose();
            }

            _cold = [];
        }

        /// <summary>
        /// Verifies application reuse and absence of further HTTP calls, then disposes services.
        /// </summary>
        [GlobalCleanup]
        public async Task CleanupAsync()
        {
            try
            {
                if (!ReferenceEquals(
                        _cachedApplication,
                        await _cached.ApplicationProvider.GetConfidentialClientApplicationAsync())
                    || _cached.HttpFactory.RequestCount != 1
                    || _failed.HttpFactory.RequestCount != 0)
                {
                    throw new InvalidOperationException("Cached acquisition rebuilt the application or made additional HTTP calls.");
                }
            }
            finally
            {
                CleanupCold();
                _cached.Dispose();
                _failed.Dispose();
            }
        }

        private void SetupCold(bool includeAssertionFile, int batchSize)
        {
            _cold = Enumerable.Range(0, batchSize)
                .Select(_ => CreateServices(includeAssertionFile, includeSecret: true))
                .ToArray();
        }

        private async Task BuildColdApplicationsAsync()
        {
            foreach (BenchmarkServices services in _cold)
            {
                await services.ApplicationProvider.GetConfidentialClientApplicationAsync();
            }
        }

        private static BenchmarkServices CreateServices(bool includeAssertionFile, bool includeSecret)
        {
            var credentials = new List<CredentialDescription>();
            if (includeAssertionFile)
            {
                credentials.Add(new CredentialDescription
                {
                    SourceType = CredentialSource.SignedAssertionFilePath,
                    SignedAssertionFileDiskPath = Path.Combine(Path.GetTempPath(), $"idweb-benchmark-{Guid.NewGuid():N}.jwt"),
                });
            }
            if (includeSecret)
            {
                credentials.Add(new CredentialDescription
                {
                    SourceType = CredentialSource.ClientSecret,
                    ClientSecret = "benchmark-only-placeholder",
                });
            }

            var httpFactory = new TokenHttpClientFactory();
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddTokenAcquisition(isTokenAcquisitionSingleton: true);
            services.AddInMemoryTokenCaches();
            services.AddSingleton<IHttpClientFactory>(httpFactory);
            services.Configure<MicrosoftIdentityApplicationOptions>(options =>
            {
                options.Instance = "https://login.microsoftonline.com/";
                options.TenantId = "11111111-1111-1111-1111-111111111111";
                options.ClientId = Guid.NewGuid().ToString();
                options.ClientCredentials = credentials;
            });
            return new BenchmarkServices(services.BuildServiceProvider(), httpFactory);
        }

        private sealed class BenchmarkServices(ServiceProvider services, TokenHttpClientFactory httpFactory) : IDisposable
        {
            public IConfidentialClientApplicationProvider ApplicationProvider { get; } =
                services.GetRequiredService<IConfidentialClientApplicationProvider>();

            public ITokenAcquisition Acquisition { get; } = services.GetRequiredService<ITokenAcquisition>();

            public TokenHttpClientFactory HttpFactory { get; } = httpFactory;

            public void Dispose()
            {
                services.Dispose();
                HttpFactory.Dispose();
            }
        }

        private sealed class TokenHttpClientFactory : IHttpClientFactory, IDisposable
        {
            private readonly TokenHandler _handler = new();
            private readonly HttpClient _client;

            public TokenHttpClientFactory() => _client = new HttpClient(_handler);

            public int RequestCount => _handler.RequestCount;

            public HttpClient CreateClient(string name) => _client;

            public void Dispose() => _client.Dispose();
        }

        private sealed class TokenHandler : HttpMessageHandler
        {
            public int RequestCount { get; private set; }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                if (request.Method != HttpMethod.Post
                    || request.RequestUri?.AbsoluteUri != "https://login.microsoftonline.com/11111111-1111-1111-1111-111111111111/oauth2/v2.0/token")
                {
                    throw new InvalidOperationException($"Unexpected benchmark HTTP request: {request.Method} {request.RequestUri}");
                }

                RequestCount++;
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"token_type\":\"Bearer\",\"expires_in\":86400,\"access_token\":\"benchmark-access-token\"}"),
                });
            }
        }
    }
}
