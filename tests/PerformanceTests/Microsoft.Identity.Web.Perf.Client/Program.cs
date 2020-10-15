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
            Console.WriteLine("Test run start.");
            IConfiguration Configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();
            var options = new TestRunnerOptions();
            Configuration.Bind("TestRunner", options);
            try
            {
                await new TestRunner(options).Run();
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
