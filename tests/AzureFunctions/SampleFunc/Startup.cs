// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Web;

[assembly: FunctionsStartup(typeof(SampleFunc.Startup))]

namespace SampleFunc
{
    public class Startup : FunctionsStartup
    {
        public Startup()
        {
        }

        IConfiguration Configuration { get; set; }


        public override void Configure(IFunctionsHostBuilder builder)
        {
            // Get the azure function application directory. 'C:\whatever' for local and 'd:\home\whatever' for Azure
            var executionContextOptions = builder.Services.BuildServiceProvider()
                .GetService<IOptions<ExecutionContextOptions>>().Value;

            var currentDirectory = executionContextOptions.AppDirectory;

            // Get the original configuration provider from the Azure Function
            var configuration = builder.Services.BuildServiceProvider().GetService<IConfiguration>();

            // Create a new IConfigurationRoot and add our configuration along with Azure's original configuration 
            this.Configuration = new ConfigurationBuilder()
                .SetBasePath(currentDirectory)
                .AddConfiguration(configuration) // Add the original function configuration 
                .AddJsonFile("local.settings.json", optional: false, reloadOnChange: true)
                .Build();

            // Replace the Azure Function configuration with our new one
            builder.Services.AddSingleton(Configuration);

            ConfigureServices(builder.Services);
        }

        private void ConfigureServices(IServiceCollection services)
        {
            services.AddAuthentication(sharedOptions =>
            {
                sharedOptions.DefaultScheme = Constants.Bearer;
                sharedOptions.DefaultChallengeScheme = Constants.Bearer;
            })
                            .AddMicrosoftIdentityWebApi(Configuration)
                                .EnableTokenAcquisitionToCallDownstreamApi()
                                .AddDownstreamWebApi("DownstreamAPI", Configuration.GetSection("DownstreamAPI"))
                                .AddInMemoryTokenCaches();
        }
    }
}
