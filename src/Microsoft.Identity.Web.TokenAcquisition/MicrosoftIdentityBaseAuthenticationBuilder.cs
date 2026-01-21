// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Abstractions;
using Microsoft.IdentityModel.LoggingExtensions;
using Microsoft.IdentityModel.Logging;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Base class for web app and web API Microsoft Identity authentication
    /// builders.
    /// </summary>
    public class MicrosoftIdentityBaseAuthenticationBuilder
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="services">The services being configured.</param>
        /// <param name="configurationSection">Optional configuration section.</param>
#if NET6_0_OR_GREATER && !NET8_0_OR_GREATER
        [RequiresUnreferencedCode("Calls Microsoft.Extensions.Configuration.ConfigurationBinder.Bind(IConfiguration, Object).")]
#endif
#if NET8_0_OR_GREATER
        [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "LoggingOptions is a simple POCO compatible with source generators")]
        [UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.", Justification = "LoggingOptions is a simple POCO compatible with source generators")]
#endif
        protected MicrosoftIdentityBaseAuthenticationBuilder(
            IServiceCollection services,
            IConfigurationSection? configurationSection = null)
        {
            Services = services;
            ConfigurationSection = configurationSection;

            LoggingOptions logOptions = new LoggingOptions();
            configurationSection?.Bind(logOptions);
            IdentityModelEventSource.ShowPII = logOptions.EnablePiiLogging;
        }

#if NET8_0_OR_GREATER
        [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "GetValue<string> for primitive types is AOT-safe")]
#endif
        internal static void SetIdentityModelLogger(IServiceProvider serviceProvider)
        {
            if (serviceProvider != null)
            {
                var config = serviceProvider.GetRequiredService<IConfiguration>();
                var loglevel = config.GetValue<string>("Logging:LogLevel:Microsoft.Identity.Web");

                // initialize logger only once
                // If the user has configured LogLevel.None, don't initialize the logger
                if (LogHelper.Logger != NullIdentityModelLogger.Instance ||
                    loglevel == LogLevel.None.ToString())
                    return;

                // check if an ILogger was already created by user
                ILogger? logger = serviceProvider.GetService<ILogger<IdentityLoggerAdapter>>();
                if (logger == null)
                {
                    var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
                    if (loggerFactory != null)
                        logger = loggerFactory.CreateLogger<IdentityLoggerAdapter>();
                }

                // return if user hasn't configured any logging
                if (logger == null)
                    return;

                // initialize Wilson logger
                IIdentityLogger identityLogger = new IdentityLoggerAdapter(logger);
                LogHelper.Logger = identityLogger;
            }
        }

        /// <summary>
        /// The services being configured.
        /// </summary>
        public IServiceCollection Services { get; private set; }

        /// <summary>
        /// Configuration section from which to bind options.
        /// </summary>
        /// <remarks>It can be null if the configuration happens with delegates
        /// rather than configuration.</remarks>
        protected IConfigurationSection? ConfigurationSection { get; set; }
    }
}
