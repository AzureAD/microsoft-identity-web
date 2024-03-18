// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.InProcess.NoEmit;
using Benchmarks;

namespace daemon_console
{
    class Program
    {
        static void Main(string[] args)
        {
            // StepthroughValidateBenchmarks();
#if DEBUG
            var benchmarkConfig = ManualConfig.Union(DefaultConfig.Instance, new DebugInProcessConfig()); // Allows debugging into benchmarks
#else
            var benchmarkConfig = ManualConfig.Union(DefaultConfig.Instance, new BenchmarkConfig());
#endif
            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, benchmarkConfig);
        }
    }
}
