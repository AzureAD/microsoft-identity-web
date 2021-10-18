using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;

namespace Microsoft.Identity.Web.Internal
{
    public class WebApiBuildersInternal
    {
        public static MicrosoftIdentityAppCallsWebApiAuthenticationBuilder EnableTokenAcquisitionWithMise(
            Action<ConfidentialClientApplicationOptions> configureConfidentialClientApplicationOptions,
            string authenticationScheme,
            IServiceCollection services,
            IConfiguration config)
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
                config as IConfigurationSection);
        }
    }
}
