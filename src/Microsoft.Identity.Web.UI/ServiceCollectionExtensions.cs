// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Identity.Web.UI
{
    /// <summary>
    /// Extension method on <see cref="IMvcBuilder"/> to add UI
    /// for Microsoft.Identity.Web.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds a controller and Razor pages for the accounts management.
        /// </summary>
        /// <param name="builder">MVC builder.</param>
        /// <returns>MVC builder for chaining.</returns>
        public static IMvcBuilder AddMicrosoftIdentityUI(this IMvcBuilder builder)
        {
            _ = Throws.IfNull(builder);

            builder.ConfigureApplicationPartManager(apm =>
            {
                apm.FeatureProviders.Add(new MicrosoftIdentityAccountControllerFeatureProvider());
            });

            builder.Services.ConfigureAll<CookieAuthenticationOptions>(options =>
            {
                if (string.IsNullOrEmpty(options.AccessDeniedPath))
                {
                    options.AccessDeniedPath = new PathString("/MicrosoftIdentity/Account/AccessDenied");
                }
            });

            return builder;
        }
    }
}
