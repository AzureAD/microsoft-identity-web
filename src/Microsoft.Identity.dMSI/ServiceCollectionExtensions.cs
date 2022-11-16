using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;

namespace Microsoft.Identity.dMSI
{
    /// <summary>
    /// Extensions for IServiceCollection for startup initialization of web APIs.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Add support for dMSI
        /// </summary>
        /// <param name="services">Service collection.</param>
        /// <returns>The service collection.</returns>
        /// <example>
        /// This method is typically called from the <c>ConfigureServices(IServiceCollection services)</c> in Startup.cs.
        /// Note that the implementation of the token cache can be chosen separately.
        ///
        /// <code>
        /// // Token acquisition service and its cache implementation as a session cache
        /// services.AddTokenAcquisition()
        /// .AddDmsiCredentialsLoader();
        /// </code>
        /// </example>
        public static IServiceCollection AddDmsiCredentialsLoader(
            this IServiceCollection services)
        {
            services.AddSingleton<ICredentialsLoader>((sp) =>
            {
                var credentialLoader = new DefaultCertificateLoader();

                credentialLoader.CredentialSourceLoaders.Add(CredentialSource.SignedAssertionFromVault, new FromDmsiCredentialLoader());
                return credentialLoader;
            });

            return services;
        }
    }
}
