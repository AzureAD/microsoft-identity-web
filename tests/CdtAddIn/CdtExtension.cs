// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.TokenAcquisition;

namespace Microsoft.Identity.ServiceEssentials.CdtAddIn
{
    public static class CdtExtension
    {
        /// <summary>
        /// Acquire a token with contraints.
        /// </summary>
        /// <param name="options">Options</param>
        /// <param name="constraints">Constraints</param>
        public static void WithContraints(this AcquireTokenOptions options, string constraints)
        {
            // Eventually add ExtraParameters to AcquireTokenOptions (for the moment it's only strings)
            options.ExtraQueryParameters["cdtConstaints"] = constraints;
        }

        public static string GetContraints(this AcquireTokenOptions options)
        {
            // Eventually add ExtraParameters to AcquireTokenOptions (for the moment it's only strings)
            if (options.ExtraQueryParameters.TryGetValue("cdtConstaints", out string result))
            {
                return result;
            }
            return null;
        }


        public static ServiceCollection AddConstraintDelegationTokens(this ServiceCollection services)
        {
            services.Configure<TokenAcquisitionAddInOptions>(options =>
            {
                options.OnBeforeTokenAcquisitionForApp += Options_OnBeforeTokenAcquisitionForApp;
            }
            );
            return services;
        }

        private static void Options_OnBeforeTokenAcquisitionForApp(Client.AcquireTokenForClientParameterBuilder builder, AcquireTokenOptions acquireTokenOptions)
        {
            // constraints = 
            // builder.WithConstaints();
        }
    }
}
