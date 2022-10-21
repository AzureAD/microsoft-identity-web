// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Extensions for IServerSideBlazorBuilder for startup initialization of web APIs.
    /// </summary>
    public static class MicrosoftIdentityBlazorServiceCollectionExtensions
    {
        /// <summary>
        /// Add the incremental consent and conditional access handler for Blazor
        /// server side pages.
        /// </summary>
        /// <param name="builder">Service side blazor builder.</param>
        /// <returns>The builder.</returns>
        public static IServerSideBlazorBuilder AddMicrosoftIdentityConsentHandler(
            this IServerSideBlazorBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.Services.TryAddEnumerable(ServiceDescriptor.Scoped<CircuitHandler, MicrosoftIdentityServiceHandler>());
            builder.Services.TryAddScoped<MicrosoftIdentityConsentAndConditionalAccessHandler>();
            return builder;
        }

        /// <summary>
        /// Add the incremental consent and conditional access handler for
        /// web app pages, Razor pages, controllers, views, etc...
        /// </summary>
        /// <param name="services">Service collection.</param>
        /// <returns>The service collection.</returns>
        public static IServiceCollection AddMicrosoftIdentityConsentHandler(
            this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.TryAddEnumerable(ServiceDescriptor.Scoped<CircuitHandler, MicrosoftIdentityServiceHandler>());
            services.TryAddScoped<MicrosoftIdentityConsentAndConditionalAccessHandler>();
            return services;
        }
    }
}
