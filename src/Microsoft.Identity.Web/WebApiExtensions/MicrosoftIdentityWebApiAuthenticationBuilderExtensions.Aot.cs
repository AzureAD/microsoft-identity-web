// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#if NET10_0_OR_GREATER

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web.PostConfigureOptions;
using Microsoft.Identity.Web.Resource;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Extensions for <see cref="AuthenticationBuilder"/> for startup initialization of web APIs (AOT-compatible).
    /// </summary>
    public static partial class MicrosoftIdentityWebApiAuthenticationBuilderExtensions
    {
        /// <summary>
        /// Protects the web API with Microsoft identity platform (AOT-compatible).
        /// This method expects the configuration will have a section with the necessary settings to initialize authentication options.
        /// Note: For true AOT compatibility, use the programmatic overload. This overload uses configuration binding which requires source generation.
        /// </summary>
        /// <param name="builder">The <see cref="AuthenticationBuilder"/> to which to add this configuration.</param>
        /// <param name="configurationSection">The configuration section from which to fill-in the options.</param>
        /// <param name="jwtBearerScheme">The JWT bearer scheme name to be used. By default it uses "Bearer".</param>
        /// <param name="configureJwtBearerOptions">Optional action to configure <see cref="JwtBearerOptions"/>.</param>
        /// <returns>The authentication builder to chain.</returns>
        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Configuration binding is expected to use source generation in .NET 10")]
        [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Configuration binding is expected to use source generation in .NET 10")]
        public static AuthenticationBuilder AddMicrosoftIdentityWebApiAot(
            this AuthenticationBuilder builder,
            IConfigurationSection configurationSection,
            string jwtBearerScheme,
            Action<JwtBearerOptions>? configureJwtBearerOptions)
        {
            _ = Throws.IfNull(builder);
            _ = Throws.IfNull(configurationSection);

            // Delegate to Action<> overload
            return builder.AddMicrosoftIdentityWebApiAot(
                options => configurationSection.Bind(options),
                jwtBearerScheme,
                configureJwtBearerOptions);
        }

        /// <summary>
        /// Protects the web API with Microsoft identity platform (AOT-compatible).
        /// </summary>
        /// <param name="builder">The <see cref="AuthenticationBuilder"/> to which to add this configuration.</param>
        /// <param name="configureOptions">The action to configure <see cref="MicrosoftIdentityApplicationOptions"/>.</param>
        /// <param name="jwtBearerScheme">The JWT bearer scheme name to be used. By default it uses "Bearer".</param>
        /// <param name="configureJwtBearerOptions">Optional action to configure <see cref="JwtBearerOptions"/>.</param>
        /// <returns>The authentication builder to chain.</returns>
        public static AuthenticationBuilder AddMicrosoftIdentityWebApiAot(
            this AuthenticationBuilder builder,
            Action<MicrosoftIdentityApplicationOptions> configureOptions,
            string jwtBearerScheme,
            Action<JwtBearerOptions>? configureJwtBearerOptions)
        {
            _ = Throws.IfNull(builder);
            _ = Throws.IfNull(configureOptions);

            // Register MicrosoftIdentityApplicationOptions
            builder.Services.Configure(jwtBearerScheme, configureOptions);

            // Add JWT Bearer with optional custom configuration
            if (configureJwtBearerOptions != null)
            {
                builder.AddJwtBearer(jwtBearerScheme, configureJwtBearerOptions);
            }
            else
            {
                builder.AddJwtBearer(jwtBearerScheme);
            }

            // Register core services
            builder.Services.AddSingleton<IMergedOptionsStore, MergedOptionsStore>();
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddHttpClient();
            builder.Services.TryAddSingleton<MicrosoftIdentityIssuerValidatorFactory>();
            builder.Services.AddRequiredScopeAuthorization();
            
            // Note: AddRequiredScopeOrAppPermissionAuthorization is not fully AOT-compatible
            // It has RequiresUnreferencedCode/RequiresDynamicCode attributes due to ConfigurationBinder usage
            // For full AOT scenarios, customers should use RequiredScopeAuthorization instead
            
            builder.Services.AddOptions<AadIssuerValidatorOptions>();

            // Register the post-configurator for AOT path
            builder.Services.TryAddEnumerable(
                ServiceDescriptor.Singleton<IPostConfigureOptions<JwtBearerOptions>>(
                    sp => new MicrosoftIdentityJwtBearerOptionsPostConfigurator(
                        sp.GetRequiredService<IOptionsMonitor<MicrosoftIdentityApplicationOptions>>(),
                        sp)));

            // Register the merger to bridge MicrosoftIdentityApplicationOptions to MergedOptions
            // This ensures TokenAcquisition works without modification
            if (!HasImplementationType(builder.Services, typeof(MicrosoftIdentityApplicationOptionsMerger)))
            {
                builder.Services.TryAddSingleton<IPostConfigureOptions<MicrosoftIdentityApplicationOptions>, 
                    MicrosoftIdentityApplicationOptionsMerger>();
            }

            return builder;
        }
    }
}

#endif
