// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

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
        /// <param name="builder">MVC Builder.</param>
        public static IMvcBuilder AddMicrosoftIdentityUI(this IMvcBuilder builder)
        {
            builder.ConfigureApplicationPartManager(apm =>
            {
                apm.FeatureProviders.Add(new MicrosoftIdentityAccountControllerFeatureProvider());
            });

            return builder;
        }
    }
}
