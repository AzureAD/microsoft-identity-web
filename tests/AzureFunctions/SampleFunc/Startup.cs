// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Web;

[assembly: FunctionsStartup(typeof(SampleFunc.Startup))]

namespace SampleFunc
{
    public class Startup : FunctionsStartup
    {
        public Startup()
        {
        }

        public override void Configure(IFunctionsHostBuilder builder)
        {
            // This is configuration from environment variables, settings.json etc.
            var configuration = builder.GetContext().Configuration;

            builder.Services.AddAuthentication(sharedOptions =>
            {
                sharedOptions.DefaultScheme = Constants.Bearer;
                sharedOptions.DefaultChallengeScheme = Constants.Bearer;
            })
                .AddMicrosoftIdentityWebApi(configuration)
                    .EnableTokenAcquisitionToCallDownstreamApi()
                    .AddInMemoryTokenCaches();
        }
    }
}
