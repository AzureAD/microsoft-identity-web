// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;

namespace Benchmarks
{
    [MemoryDiagnoser]
    public class TokenAcquisitionBenchmark
    {
        static readonly IServiceProvider? s_serviceProvider;
        static readonly TokenAcquirerFactory? s_tokenAcquirerFactory;
        readonly string[] _downstreamApiOptionsOverride = ["https://graph.microsoft.com/.default"];

        static TokenAcquisitionBenchmark()
        {
            s_tokenAcquirerFactory = TokenAcquirerFactory.GetDefaultInstance();
            s_tokenAcquirerFactory.Services.AddDownstreamApi("GraphBeta", s_tokenAcquirerFactory.Configuration.GetSection("GraphBeta"));
            s_serviceProvider = s_tokenAcquirerFactory.Build();
        }

        //[Benchmark]
        //public async Task GetTokenForAppAsync()
        //{
        //    var downstreamApi = s_serviceProvider.GetRequiredService<IDownstreamApi>();
        //    var httpResponseMessage = await downstreamApi.CallApiForAppAsync("GraphBeta", options =>
        //    {
        //        options.BaseUrl = "https://graph.microsoft.com/beta";
        //        options.Scopes = _downstreamApiOptionsOverride;
        //    }).ConfigureAwait(false);
        //}

        [Benchmark]
        public async Task CreateAuthorizationHeaderAsync()
        {
            // Get the authorization request creator service
            IAuthorizationHeaderProvider authorizationHeaderProvider = s_serviceProvider!.GetRequiredService<IAuthorizationHeaderProvider>();
            await authorizationHeaderProvider.CreateAuthorizationHeaderForAppAsync("https://graph.microsoft.com/.default").ConfigureAwait(false);
        }

        [Benchmark]
        public async Task GetTokenAcquirerAsync()
        {
            // Get the token acquisition service
            ITokenAcquirer tokenAcquirer = s_tokenAcquirerFactory!.GetTokenAcquirer();
            await tokenAcquirer.GetTokenForAppAsync("https://graph.microsoft.com/.default");
        }
    }
}
