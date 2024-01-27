// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using BenchmarkDotNet.Attributes;
using daemon_console;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;
using BenchmarkDotNet.Diagnostics.Windows;
using BenchmarkDotNet.Diagnostics.Windows.Configs;

namespace Benchmarks
{
    [Config(typeof(AntiVirusFriendlyConfig))]
    [MemoryDiagnoser]
    //[EtwProfiler]
    public class TokenAcquisitionBenchmark
    {
        static IServiceProvider? _serviceProvider;
        static TokenAcquirerFactory? _tokenAcquirerFactory;
        readonly string[] _downstreamApiOptionsOverride = ["https://graph.microsoft.com/.default"];

        static TokenAcquisitionBenchmark()
        {
            _tokenAcquirerFactory = TokenAcquirerFactory.GetDefaultInstance();
            _tokenAcquirerFactory.Services.AddDownstreamApi("GraphBeta", _tokenAcquirerFactory.Configuration.GetSection("GraphBeta"));
            _serviceProvider = _tokenAcquirerFactory.Build();
        }

        [Benchmark]
        public async Task GetTokenForAppAsync()
        {
            var downstreamApi = _serviceProvider.GetRequiredService<IDownstreamApi>();
            var httpResponseMessage = await downstreamApi.CallApiForAppAsync("GraphBeta", options =>
            {
                options.BaseUrl = "https://graph.microsoft.com/beta";
                options.Scopes = _downstreamApiOptionsOverride;
            }).ConfigureAwait(false);
        }

        [Benchmark]
        public async Task CreateAuthorizationHeader()
        {
            // Get the authorization request creator service
            IAuthorizationHeaderProvider authorizationHeaderProvider = _serviceProvider.GetRequiredService<IAuthorizationHeaderProvider>();
            await authorizationHeaderProvider.CreateAuthorizationHeaderForAppAsync("https://graph.microsoft.com/.default");
        }

        [Benchmark]
        public async Task GetTokenAcquirer()
        {
            // Get the token acquisition service
            ITokenAcquirer tokenAcquirer = _tokenAcquirerFactory.GetTokenAcquirer();
            await tokenAcquirer.GetTokenForAppAsync("https://graph.microsoft.com/.default");
        }
    }
}
