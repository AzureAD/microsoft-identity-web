// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Graph;
using Microsoft.Identity.Web;

namespace WebAppCallsMicrosoftGraph
{
    public static class MicrosoftGraphServiceExtensions
    {
        public static void AddMicrosoftGraph(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<IGraphServiceClient, IGraphServiceClient>(serviceProvider =>
            {
                var tokenAquisitionService = serviceProvider.GetService<ITokenAcquisition>();
                return new GraphServiceClient(new WebSignInCredential(tokenAquisitionService));
            });
        }
    }
}
