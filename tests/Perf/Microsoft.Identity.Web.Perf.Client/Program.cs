using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Identity.Web.Perf.Client
{
    class Program
    {
        // TODO: Use KeyVault for secrets.
        // TODO: Bind configuration to TestRunnerOptions class.
        // TODO: Add ILogger classes (ConsoleLogger, FileLogger, etc.)
        // TODO: Move constants into separate file.
        // TODO: Add ability to parse console args (ex. --verbose).
        static async Task Main(string[] args)
        {
            Console.WriteLine("Test run start.");
            IConfiguration Configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();
            try
            {
                await new TestRunner(Configuration).Run();
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
