// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;

namespace Microsoft.Identity.Web.Internal
{
    /// <summary>
    /// Web API authentication builder.
    /// </summary>
    public static class WebApiBuildersInternal
    {
        /// <summary>
        /// Enables token acquisition which is not specific to JWT, such as when using Microsoft.Identity.Service.Essentials (MISE).
        /// Developers should continue to use `EnableTokenAcquisitionToCallDownstreamApi`.
        /// This API is not considered part of the public API and may change.
        /// </summary>
        /// <param name="configureConfidentialClientApplicationOptions">The action to configure <see cref="ConfidentialClientApplicationOptions"/>.</param>
        /// <param name="authenticationScheme">Authentication scheme.</param>
        /// <param name="services">The services being configured.</param>
        /// <param name="configuration">Configuration.</param>
        /// <returns>The authentication builder to chain.</returns>
        public static MicrosoftIdentityAppCallsWebApiAuthenticationBuilder EnableTokenAcquisition(
            Action<ConfidentialClientApplicationOptions> configureConfidentialClientApplicationOptions,
            string authenticationScheme,
            IServiceCollection services,
            IConfiguration? configuration)
        {
            services.AddOptions<ConfidentialClientApplicationOptions>(authenticationScheme)
                            .Configure<IOptionsMonitor<MergedOptions>>((
                               ccaOptions, mergedOptionsMonitor) =>
                            {
                                configureConfidentialClientApplicationOptions(ccaOptions);
                                MergedOptions mergedOptions = mergedOptionsMonitor.Get(authenticationScheme);
                                MergedOptions.UpdateMergedOptionsFromConfidentialClientApplicationOptions(ccaOptions, mergedOptions);
                            });

            services.AddTokenAcquisition();

            return new MicrosoftIdentityAppCallsWebApiAuthenticationBuilder(
                services,
                configuration as IConfigurationSection);
        }
    }
}
