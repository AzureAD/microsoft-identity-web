// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Authentication;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Extension methods related to App Services authentication (Easy Auth).
    /// </summary>
    public static class AppServiceAuthenticationBuilderExtensions
    {
        /// <summary>
        /// Add authentication with App Services.
        /// </summary>
        /// <param name="builder">Authentication builder.</param>
        /// <returns>The builder, to chain commands.</returns>
        public static AuthenticationBuilder AddAppServiceAuthentication(
             this AuthenticationBuilder builder)
        {
            if (builder is null)
            {
                throw new System.ArgumentNullException(nameof(builder));
            }

            builder.AddScheme<AppServiceAuthenticationOptions, AppServiceAuthenticationHandler>(
                AppServiceAuthenticationDefaults.AuthenticationScheme,
                AppServiceAuthenticationDefaults.AuthenticationScheme,
                options => { });

            return builder;
        }
    }
}
