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
            try
            {
                await new TestRunner(Configuration).Run();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
            Console.WriteLine("Test run completed. Press any key to exit.");
            Console.ReadKey();
        }
    }
}
