// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Identity.Web.Perf.Client
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Test run start. Press Esc to stop.");
            try
            {
                IConfiguration configuration = new ConfigurationBuilder()
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .Build();
                var options = new TestRunnerOptions();
                configuration.Bind("TestRunner", options);

                await new TestRunner(options).RunAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            Console.WriteLine("Test run completed. Press any key to exit.");
            Console.ReadKey();
        }
    }
}
