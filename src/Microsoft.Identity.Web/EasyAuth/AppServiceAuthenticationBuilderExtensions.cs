// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Extension methods.
    /// </summary>
    public static class AppServiceAuthenticationBuilderExtensions
    {
        /// <summary>
        /// Add the appServiceAuthentication.
        /// </summary>
        /// <param name="builder">Authentication builder</param>
        /// <param name="configureOptions">Delegate to configure the options</param>
        /// <returns>the builder to chain</returns>
        public static AuthenticationBuilder AddAppServiceAuthentication(
            this AuthenticationBuilder builder,
            Action<AppServiceAuthenticationOptions> configureOptions)
        {
            builder.AddScheme<AppServiceAuthenticationOptions, AppServiceAuthenticationHandler>(
                AppServiceAuthenticationDefaults.AuthenticationScheme,
                AppServiceAuthenticationDefaults.AuthenticationScheme,
                configureOptions);

            builder.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme);
            builder.AddCookie(Microsoft.AspNetCore.Authentication.OpenIdConnect.OpenIdConnectDefaults.AuthenticationScheme);

            return builder;
        }
    }

}
