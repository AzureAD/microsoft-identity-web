// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Toolchains.InProcess.NoEmit;
using Perfolizer.Horology;

namespace Benchmarks
{
    // Release configuration for running benchmarks
    public class BenchmarkConfig : ManualConfig
    {
        public BenchmarkConfig()
        {
            AddJob(Job.MediumRun
                .WithToolchain(InProcessNoEmitToolchain.Instance)
                .WithLaunchCount(4)
                .WithMaxAbsoluteError(TimeInterval.FromMilliseconds(10)))
                //.WithOption(ConfigOptions.DisableOptimizationsValidator, true)
                .AddColumn(StatisticColumn.P90, StatisticColumn.P95, StatisticColumn.P100)
                .WithOrderer(new DefaultOrderer(SummaryOrderPolicy.Method))
                .HideColumns(Column.WarmupCount, Column.Type, Column.Job)
                .AddDiagnoser(MemoryDiagnoser.Default); // https://benchmarkdotnet.org/articles/configs/diagnosers.html
                                                        //.AddDiagnoser(new EtwProfiler()) // Uncomment to generate traces / flame graphs. Doc: https://adamsitnik.com/ETW-Profiler/
        }
    }
}
