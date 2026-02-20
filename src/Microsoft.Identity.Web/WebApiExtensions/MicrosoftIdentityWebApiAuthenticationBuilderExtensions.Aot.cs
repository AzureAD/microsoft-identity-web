// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#if NET10_0_OR_GREATER

using System;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web.Internal;
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

            // Set Authority during Configure phase so that ASP.NET's built-in JwtBearerPostConfigureOptions can create the ConfigurationManager from it.
            builder.Services.AddOptions<JwtBearerOptions>(jwtBearerScheme)
                .Configure<IOptionsMonitor<MicrosoftIdentityApplicationOptions>>((jwtOptions, appOptionsMonitor) =>
                {
                    if (!string.IsNullOrEmpty(jwtOptions.Authority))
                    {
                        return;
                    }

                    var appOptions = appOptionsMonitor.Get(jwtBearerScheme);
                    if (string.IsNullOrEmpty(appOptions.ClientId))
                    {
                        // Skip if not configured via AOT path (no ClientId means not configured)
                        return;
                    }

                    if (!string.IsNullOrEmpty(appOptions.Authority))
                    {
                        var authority = AuthorityHelpers.BuildCiamAuthorityIfNeeded(appOptions.Authority, out _);
                        jwtOptions.Authority = AuthorityHelpers.EnsureAuthorityIsV2(authority ?? appOptions.Authority);
                    }
                    else
                    {
                        jwtOptions.Authority = AuthorityHelpers.EnsureAuthorityIsV2(
                            IdentityOptionsHelpers.BuildAuthority(appOptions));
                    }
                });

            // Register core services
            builder.Services.AddSingleton<IMergedOptionsStore, MergedOptionsStore>();
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddHttpClient();
            builder.Services.TryAddSingleton<MicrosoftIdentityIssuerValidatorFactory>();
            builder.Services.AddRequiredScopeAuthorization();
            builder.Services.AddRequiredScopeOrAppPermissionAuthorization();

            builder.Services.AddOptions<AadIssuerValidatorOptions>();

            // Register the post-configurator for AOT path
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IPostConfigureOptions<JwtBearerOptions>, MicrosoftIdentityJwtBearerOptionsPostConfigurator>());

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
