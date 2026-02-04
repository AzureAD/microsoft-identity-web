// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#if NET10_0_OR_GREATER

using System;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Abstractions;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Extensions for <see cref="AuthenticationBuilder"/> for startup initialization of web APIs (AOT-compatible).
    /// </summary>
    public static partial class MicrosoftIdentityWebApiAuthenticationBuilderExtensions
    {
        /// <summary>
        /// Protects the web API with Microsoft identity platform (AOT-compatible).
        /// This method expects the configuration section to have the necessary settings to initialize authentication options.
        /// </summary>
        /// <param name="builder">The <see cref="AuthenticationBuilder"/> to which to add this configuration.</param>
        /// <param name="configurationSection">The configuration section from which to fill-in the options.</param>
        /// <param name="jwtBearerScheme">The JWT bearer scheme name to be used. By default it uses "Bearer".</param>
        /// <param name="configureJwtBearerOptions">Optional action to configure <see cref="JwtBearerOptions"/>.</param>
        /// <returns>The authentication builder to chain.</returns>
        /// <remarks>
        /// This AOT-compatible overload uses <see cref="MicrosoftIdentityApplicationOptions"/> for configuration
        /// and does not require reflection-based configuration binding at runtime when used programmatically.
        /// For full AOT compatibility, prefer the programmatic overload.
        /// </remarks>
        public static AuthenticationBuilder AddMicrosoftIdentityWebApiAot(
            this AuthenticationBuilder builder,
            IConfigurationSection configurationSection,
            string jwtBearerScheme = JwtBearerDefaults.AuthenticationScheme,
            Action<JwtBearerOptions>? configureJwtBearerOptions = null)
        {
            _ = Throws.IfNull(builder);
            _ = Throws.IfNull(configurationSection);

            return builder.AddMicrosoftIdentityWebApiAot(
                options => configurationSection.Bind(options),
                jwtBearerScheme,
                configureJwtBearerOptions);
        }

        /// <summary>
        /// Protects the web API with Microsoft identity platform (AOT-compatible, programmatic configuration).
        /// This method allows programmatic configuration of authentication options without configuration binding.
        /// </summary>
        /// <param name="builder">The <see cref="AuthenticationBuilder"/> to which to add this configuration.</param>
        /// <param name="configureOptions">The action to configure <see cref="MicrosoftIdentityApplicationOptions"/>.</param>
        /// <param name="jwtBearerScheme">The JWT bearer scheme name to be used. By default it uses "Bearer".</param>
        /// <param name="configureJwtBearerOptions">Optional action to configure <see cref="JwtBearerOptions"/>.</param>
        /// <returns>The authentication builder to chain.</returns>
        /// <remarks>
        /// This is the recommended overload for full AOT compatibility as it avoids reflection-based configuration binding.
        /// It integrates with <see cref="ITokenAcquisition"/> for On-Behalf-Of (OBO) scenarios without requiring
        /// additional EnableTokenAcquisitionToCallDownstreamApi calls.
        /// </remarks>
        public static AuthenticationBuilder AddMicrosoftIdentityWebApiAot(
            this AuthenticationBuilder builder,
            Action<MicrosoftIdentityApplicationOptions> configureOptions,
            string jwtBearerScheme = JwtBearerDefaults.AuthenticationScheme,
            Action<JwtBearerOptions>? configureJwtBearerOptions = null)
        {
            _ = Throws.IfNull(builder);
            _ = Throws.IfNull(configureOptions);

            // Configure MicrosoftIdentityApplicationOptions - this is the main configuration entry point for AOT
            builder.Services.Configure<MicrosoftIdentityApplicationOptions>(jwtBearerScheme, configureOptions);

            // Add JWT Bearer authentication
            builder.AddJwtBearer(jwtBearerScheme, options =>
            {
                // Apply any customer-provided JWT Bearer options first
                configureJwtBearerOptions?.Invoke(options);
            });

            // Register core services required for token validation and acquisition
            builder.Services.AddSingleton<IMergedOptionsStore, MergedOptionsStore>();
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddHttpClient();
            builder.Services.TryAddSingleton<MicrosoftIdentityIssuerValidatorFactory>();
            builder.Services.AddRequiredScopeAuthorization();
            builder.Services.AddRequiredScopeOrAppPermissionAuthorization();
            builder.Services.AddOptions<AadIssuerValidatorOptions>();

            // Register post-configurators in the correct order
            // 1. MergedOptions bridge - populates MergedOptions from MicrosoftIdentityApplicationOptions
            if (!HasImplementationType(builder.Services, typeof(MicrosoftIdentityApplicationOptionsToMergedOptionsMerger)))
            {
                builder.Services.TryAddSingleton<IPostConfigureOptions<MicrosoftIdentityApplicationOptions>, MicrosoftIdentityApplicationOptionsToMergedOptionsMerger>();
            }

            // 2. JWT Bearer post-configurator - runs after customer configuration
            if (!HasImplementationType(builder.Services, typeof(MicrosoftIdentityJwtBearerOptionsPostConfigurator)))
            {
                builder.Services.TryAddSingleton<IPostConfigureOptions<JwtBearerOptions>, MicrosoftIdentityJwtBearerOptionsPostConfigurator>();
            }

            return builder;
        }

        private static bool HasImplementationType(IServiceCollection services, Type implementationType)
        {
            return System.Linq.Enumerable.Any(services, s =>
#if NET8_0_OR_GREATER
                s.ServiceKey is null &&
#endif
                s.ImplementationType == implementationType);
        }
    }
}

#endif
