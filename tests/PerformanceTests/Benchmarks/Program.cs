// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.InProcess.NoEmit;

namespace daemon_console
{
    class Program
    {
        static void Main(string[] args)
        {
            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
        }      
    }

    public class AntiVirusFriendlyConfig : ManualConfig
    {
        public AntiVirusFriendlyConfig()
        {
            AddColumn(StatisticColumn.P90);
            AddColumn(StatisticColumn.P95);
            AddColumn(StatisticColumn.P100);

            AddJob(Job.MediumRun
                .WithToolchain(InProcessNoEmitToolchain.Instance).WithGcServer(true));
        }
    }
}
