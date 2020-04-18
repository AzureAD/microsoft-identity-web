// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Web.Resource;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Extensions for AuthenticationBuilder for startup initialization.
    /// </summary>
    public static class WebAppAuthenticationBuilderExtensions
    {
        /// <summary>
        /// Add authentication with Microsoft identity platform.
        /// This method expects the configuration file will have a section, named "AzureAd" as default, with the necessary settings to initialize authentication options.
        /// </summary>
        /// <param name="builder">AuthenticationBuilder to which to add this configuration.</param>
        /// <param name="configuration">The IConfiguration object.</param>
        /// <param name="configSectionName">The configuration section with the necessary settings to initialize authentication options.</param>
        /// <param name="openIdConnectScheme">The OpenIdConnect scheme name to be used. By default it uses "OpenIdConnect".</param>
        /// <param name="cookieScheme">The Cookies scheme name to be used. By default it uses "Cookies".</param>
        /// <param name="subscribeToOpenIdConnectMiddlewareDiagnosticsEvents">
        /// Set to true if you want to debug, or just understand the OpenIdConnect events.
        /// </param>
        /// <returns>The authentication builder for chaining.</returns>
        public static AuthenticationBuilder AddSignIn(
            this AuthenticationBuilder builder,
            IConfiguration configuration,
            string configSectionName = "AzureAd",
            string openIdConnectScheme = OpenIdConnectDefaults.AuthenticationScheme,
            string cookieScheme = CookieAuthenticationDefaults.AuthenticationScheme,
            bool subscribeToOpenIdConnectMiddlewareDiagnosticsEvents = false) =>
                builder.AddSignIn(
                    options => configuration.Bind(configSectionName, options),
                    options => configuration.Bind(configSectionName, options),
                    openIdConnectScheme,
                    cookieScheme,
                    subscribeToOpenIdConnectMiddlewareDiagnosticsEvents);

        /// <summary>
        /// Add authentication with Microsoft identity platform.
        /// This method expects the configuration file will have a section, named "AzureAd" as default, with the necessary settings to initialize authentication options.
        /// </summary>
        /// <param name="builder">AuthenticationBuilder to which to add this configuration.</param>
        /// <param name="configureOpenIdConnectOptions">The IConfiguration object.</param>
        /// <param name="configureMicrosoftIdentityOptions">The configuration section with the necessary settings to initialize authentication options.</param>
        /// <param name="openIdConnectScheme">The OpenIdConnect scheme name to be used. By default it uses "OpenIdConnect".</param>
        /// <param name="cookieScheme">The Cookies scheme name to be used. By default it uses "Cookies".</param>
        /// <param name="subscribeToOpenIdConnectMiddlewareDiagnosticsEvents">
        /// Set to true if you want to debug, or just understand the OpenIdConnect events.
        /// </param>
        /// <returns>The authentication builder for chaining.</returns>
        public static AuthenticationBuilder AddSignIn(
            this AuthenticationBuilder builder,
            Action<OpenIdConnectOptions> configureOpenIdConnectOptions,
            Action<MicrosoftIdentityOptions> configureMicrosoftIdentityOptions,
            string openIdConnectScheme = OpenIdConnectDefaults.AuthenticationScheme,
            string cookieScheme = CookieAuthenticationDefaults.AuthenticationScheme,
            bool subscribeToOpenIdConnectMiddlewareDiagnosticsEvents = false)
        {
            builder.Services.Configure(openIdConnectScheme, configureOpenIdConnectOptions);
            builder.Services.Configure<MicrosoftIdentityOptions>(configureMicrosoftIdentityOptions);

            var microsoftIdentityOptions = new MicrosoftIdentityOptions();
            configureMicrosoftIdentityOptions(microsoftIdentityOptions);

            var b2cOidcHandlers = new AzureADB2COpenIDConnectEventHandlers(openIdConnectScheme, microsoftIdentityOptions);

            builder.Services.AddSingleton<IOpenIdConnectMiddlewareDiagnostics, OpenIdConnectMiddlewareDiagnostics>();
            builder.AddCookie(cookieScheme);
            builder.AddOpenIdConnect(openIdConnectScheme, options =>
            {
                options.SignInScheme = cookieScheme;

                if (string.IsNullOrWhiteSpace(options.Authority))
                {
                    options.Authority = AuthorityHelpers.BuildAuthority(microsoftIdentityOptions);
                }

                // This is a Microsoft identity platform Web app
                options.Authority = AuthorityHelpers.EnsureAuthorityIsV2(options.Authority);

                // B2C doesn't have preferred_username claims
                if (microsoftIdentityOptions.IsB2C)
                {
                    options.TokenValidationParameters.NameClaimType = "name";
                }
                else
                {
                    options.TokenValidationParameters.NameClaimType = "preferred_username";
                }

                // If the developer registered an IssuerValidator, do not overwrite it
                if (options.TokenValidationParameters.IssuerValidator == null)
                {
                    // If you want to restrict the users that can sign-in to several organizations
                    // Set the tenant value in the appsettings.json file to 'organizations', and add the
                    // issuers you want to accept to options.TokenValidationParameters.ValidIssuers collection
                    options.TokenValidationParameters.IssuerValidator = AadIssuerValidator.GetIssuerValidator(options.Authority).Validate;
                }

                // Avoids having users being presented the select account dialog when they are already signed-in
                // for instance when going through incremental consent
                var redirectToIdpHandler = options.Events.OnRedirectToIdentityProvider;
                options.Events.OnRedirectToIdentityProvider = async context =>
                {
                    var login = context.Properties.GetParameter<string>(OpenIdConnectParameterNames.LoginHint);
                    if (!string.IsNullOrWhiteSpace(login))
                    {
                        context.ProtocolMessage.LoginHint = login;
                        context.ProtocolMessage.DomainHint = context.Properties.GetParameter<string>(
                            OpenIdConnectParameterNames.DomainHint);

                        // delete the login_hint and domainHint from the Properties when we are done otherwise
                        // it will take up extra space in the cookie.
                        context.Properties.Parameters.Remove(OpenIdConnectParameterNames.LoginHint);
                        context.Properties.Parameters.Remove(OpenIdConnectParameterNames.DomainHint);
                    }

                    // Additional claims
                    if (context.Properties.Items.ContainsKey(OidcConstants.AdditionalClaims))
                    {
                        context.ProtocolMessage.SetParameter(
                            OidcConstants.AdditionalClaims,
                            context.Properties.Items[OidcConstants.AdditionalClaims]);
                    }

                    if (microsoftIdentityOptions.IsB2C)
                    {
                        context.ProtocolMessage.SetParameter("client_info", "1");

                        // When a new Challenge is returned using any B2C user flow different than susi, we must change
                        // the ProtocolMessage.IssuerAddress to the desired user flow otherwise the redirect would use the susi user flow
                        await b2cOidcHandlers.OnRedirectToIdentityProvider(context).ConfigureAwait(false);
                    }

                    await redirectToIdpHandler(context).ConfigureAwait(false);
                };

                if (microsoftIdentityOptions.IsB2C)
                {
                    var remoteFailureHandler = options.Events.OnRemoteFailure;
                    options.Events.OnRemoteFailure = async context =>
                    {
                        // Handles the error when a user cancels an action on the Azure Active Directory B2C UI.
                        // Handle the error code that Azure Active Directory B2C throws when trying to reset a password from the login page
                        // because password reset is not supported by a "sign-up or sign-in user flow".
                        await b2cOidcHandlers.OnRemoteFailure(context);

                        await remoteFailureHandler(context).ConfigureAwait(false);
                    };
                }

                if (subscribeToOpenIdConnectMiddlewareDiagnosticsEvents)
                {
                    var diags = builder.Services.BuildServiceProvider().GetRequiredService<IOpenIdConnectMiddlewareDiagnostics>();

                    diags.Subscribe(options.Events);
                }
            });

            return builder;
        }
    }
}