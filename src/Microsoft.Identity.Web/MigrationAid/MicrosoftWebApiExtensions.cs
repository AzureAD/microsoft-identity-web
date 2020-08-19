// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Client;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Extension methods for web API.
    /// </summary>
    public static class MicrosoftWebApiExtensions
    {
        /// <summary>
        /// Protects the web API with Microsoft identity platform (formerly Azure AD v2.0).
        /// This method expects the configuration file will have a section, named "AzureAd" as default, with the necessary settings to initialize authentication options.
        /// </summary>
        /// <param name="builder">The <see cref="MicrosoftIdentityWebApiAuthenticationBuilderWithConfiguration"/> to which to add this configuration.</param>
        /// <param name="configuration">The configuration instance.</param>
        /// <param name="configSectionName">The section name in the config file (by default "AzureAd").</param>
        /// <param name="jwtBearerScheme">The scheme for the JWT bearer token.</param>
        /// <returns>The authentication builder to chain.</returns>
        [Obsolete("Rather use EnableTokenAcquisitionToCallDownstreamApi()")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static MicrosoftIdentityAppCallsWebApiAuthenticationBuilder AddWebApiCallsWebApi(
            this MicrosoftIdentityWebApiAuthenticationBuilderWithConfiguration builder,
            IConfiguration configuration,
            string configSectionName = Constants.AzureAd,
            string jwtBearerScheme = JwtBearerDefaults.AuthenticationScheme)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.EnableTokenAcquisitionToCallDownstreamApi();
        }

        /// <summary>
        /// Protects the web API with Microsoft identity platform (formerly Azure AD v2.0).
        /// </summary>
        /// <param name="builder">The <see cref="MicrosoftIdentityWebApiAuthenticationBuilderWithConfiguration"/> to which to add this configuration.</param>
        /// <param name="configureConfidentialClientApplicationOptions">The action to configure <see cref="ConfidentialClientApplicationOptions"/>.</param>
        /// <param name="configureMicrosoftIdentityOptions">The action to configure <see cref="MicrosoftIdentityOptions"/>.</param>
        /// <param name="jwtBearerScheme">The scheme for the JWT bearer token.</param>
        /// <returns>The authentication builder to chain.</returns>
        [Obsolete("Rather use EnableTokenAcquisitionToCallDownstreamApi()")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static MicrosoftIdentityAppCallsWebApiAuthenticationBuilder AddWebApiCallsWebApi(
            this MicrosoftIdentityWebApiAuthenticationBuilderWithConfiguration builder,
            Action<ConfidentialClientApplicationOptions> configureConfidentialClientApplicationOptions,
            Action<MicrosoftIdentityOptions> configureMicrosoftIdentityOptions,
            string jwtBearerScheme = JwtBearerDefaults.AuthenticationScheme)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.EnableTokenAcquisitionToCallDownstreamApi(configureConfidentialClientApplicationOptions);
        }
    }
}
