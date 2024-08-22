// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web.OWIN;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Microsoft.IdentityModel.Validators;
using Microsoft.Owin.Security.Jwt;
using Microsoft.Owin.Security.OAuth;
using Microsoft.Owin.Security.OpenIdConnect;
using Microsoft.Identity.Web.Diagnostics;
using Owin;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Extension methods on an ASP.NET application to add a web app or web API.
    /// </summary>
    public static class AppBuilderExtension
    {
        /// <summary>
        /// Adds a protected web API.
        /// </summary>
        /// <param name="app">Application builder.</param>
        /// <param name="tokenAcquirerFactory">Token acquirer factory.</param>
        /// <param name="configureMicrosoftIdentityApplicationOptions">Configure Microsoft authentication options.</param>
        /// <param name="updateOptions">Update the OWIN options if you want to finesse the token validation.</param>
        /// <param name="configurationSection">Configuration section in which to read the options.</param>
        /// <returns>The app builder to chain.</returns>
        public static IAppBuilder AddMicrosoftIdentityWebApi(
            this IAppBuilder app,
            OwinTokenAcquirerFactory tokenAcquirerFactory,
            Action<MicrosoftIdentityApplicationOptions>? configureMicrosoftIdentityApplicationOptions = null,
            Action<OAuthBearerAuthenticationOptions>? updateOptions = null,
            string configurationSection = "AzureAd")
        {
            var services = tokenAcquirerFactory.Services;
            var configuration = tokenAcquirerFactory.Configuration;

            // Configure the Microsoft authentication options
            services.Configure(
                string.Empty,
                configureMicrosoftIdentityApplicationOptions ?? (option =>
                {
                    configuration?.GetSection(configurationSection).Bind(option);
                }));
            services.Configure<ConfidentialClientApplicationOptions>(
                string.Empty,
                (option =>
                {
                    configuration?.GetSection(configurationSection).Bind(option);
                }));

            string instance = configuration.GetValue<string>($"{configurationSection}:Instance");
            string tenantId = configuration.GetValue<string>($"{configurationSection}:TenantId");
            string clientId = configuration.GetValue<string>($"{configurationSection}:ClientId");
            string audience = configuration.GetValue<string>($"{configurationSection}:Audience");
            string authority = instance + tenantId + "/v2.0";
            TokenValidationParameters tokenValidationParameters = new()
            {
                ValidAudience = audience ?? clientId,
                IssuerValidator = AadIssuerValidator.GetAadIssuerValidator(authority).Validate,
                SaveSigninToken = true,
            };
            tokenValidationParameters.EnableAadSigningKeyIssuerValidation();

            OAuthBearerAuthenticationOptions options = new()
            {
                AccessTokenFormat = new JwtFormat(tokenValidationParameters, new OpenIdConnectCachingSecurityTokenProvider(authority + "/.well-known/openid-configuration"))
            };

            if (updateOptions != null)
            {
                updateOptions(options);
            }

            return app.UseOAuthBearerAuthentication(options);
        }

        /// <summary>
        /// Adds a protected web app.
        /// </summary>
        /// <param name="app">Application builder.</param>
        /// <param name="tokenAcquirerFactory">Token acquirer factory.</param>
        /// <param name="configureMicrosoftIdentityApplicationOptions">Configure Microsoft authentication options.</param>
        /// <param name="updateOptions">Update the OWIN options if you want to finesse the OpenIdConnect options.</param>
        /// <param name="configurationSection">Configuration section in which to read the options.</param>
        /// <returns>The app builder to chain.</returns>
        public static IAppBuilder AddMicrosoftIdentityWebApp(
            this IAppBuilder app,
            OwinTokenAcquirerFactory tokenAcquirerFactory,
            Action<MicrosoftIdentityApplicationOptions>? configureMicrosoftIdentityApplicationOptions = null,
            Action<OpenIdConnectAuthenticationOptions>? updateOptions = null,
            string configurationSection = "AzureAd")
        {
            var services = tokenAcquirerFactory.Services;
            var configuration = tokenAcquirerFactory.Configuration;

            // Configure the Microsoft authentication options
            services.Configure(
                string.Empty,
                configureMicrosoftIdentityApplicationOptions ?? (option =>
                {
                    configuration?.GetSection(configurationSection).Bind(option);
                }));
            services.Configure<ConfidentialClientApplicationOptions>(
                string.Empty,
                (option =>
                {
                    configuration?.GetSection(configurationSection).Bind(option);
                }));

            string instance = configuration.GetValue<string>($"{configurationSection}:Instance");
            string tenantId = configuration.GetValue<string>($"{configurationSection}:TenantId");
            string clientId = configuration.GetValue<string>($"{configurationSection}:ClientId");
            string postLogoutRedirectUri = configuration.GetValue<string>($"{configurationSection}:SignedOutCallbackPath");
            string authority = instance + tenantId + "/v2.0";

            OpenIdConnectAuthenticationOptions options = new()
            {
                ClientId = clientId,
                Authority = authority,
                PostLogoutRedirectUri = postLogoutRedirectUri,
                Scope = "openid profile offline_access user.read",
                ResponseType = "code",

                Notifications = new OpenIdConnectAuthenticationNotifications()
                {
                    RedirectToIdentityProvider = (context) =>
                    {
                        var loginHint = context.ProtocolMessage.GetParameter(OpenIdConnectParameterNames.LoginHint);
                        if (!string.IsNullOrWhiteSpace(loginHint))
                        {
                            context.ProtocolMessage.LoginHint = loginHint;

                            context.ProtocolMessage.SetParameter(Constants.XAnchorMailbox, $"{Constants.Upn}:{loginHint}");
                            // delete the login_hint from the Properties when we are done otherwise
                            // it will take up extra space in the cookie.
                            context.ProtocolMessage.Parameters.Remove(OpenIdConnectParameterNames.LoginHint);
                        }

                        var domainHint = context.ProtocolMessage.GetParameter(OpenIdConnectParameterNames.DomainHint);
                        if (!string.IsNullOrWhiteSpace(domainHint))
                        {
                            context.ProtocolMessage.DomainHint = domainHint;

                            // delete the domain_hint from the Properties when we are done otherwise
                            // it will take up extra space in the cookie.
                            context.ProtocolMessage.Parameters.Remove(OpenIdConnectParameterNames.DomainHint);
                        }
                        context.ProtocolMessage.SetParameter(ClaimConstants.ClientInfo, Constants.One);
                        context.ProtocolMessage.SetParameter(Constants.TelemetryHeaderKey, IdHelper.CreateTelemetryInfo());
                        return Task.CompletedTask;
                    },

                    SecurityTokenValidated = (context) =>
                    {
                        HttpContextBase httpContext = context.OwinContext.Get<HttpContextBase>(typeof(HttpContextBase).FullName);

                        if (httpContext.Session is null)
                        {
                            var clientInfo = context.OwinContext.Get<string>(ClaimConstants.ClientInfo);

                            if (!string.IsNullOrEmpty(clientInfo))
                            {
                                ClientInfo? clientInfoFromServer = ClientInfo.CreateFromJson(clientInfo);

                                if (clientInfoFromServer != null && clientInfoFromServer.UniqueTenantIdentifier != null && clientInfoFromServer.UniqueObjectIdentifier != null)
                                {
                                    context.AuthenticationTicket.Identity.AddClaim(new Claim(ClaimConstants.UniqueTenantIdentifier, clientInfoFromServer.UniqueTenantIdentifier));
                                    context.AuthenticationTicket.Identity.AddClaim(new Claim(ClaimConstants.UniqueObjectIdentifier, clientInfoFromServer.UniqueObjectIdentifier));
                                }
                                context.OwinContext.Environment.Remove(ClaimConstants.ClientInfo);
                            }
                        }
                        else if (httpContext.Session[ClaimConstants.ClientInfo] is string clientInfo && !string.IsNullOrEmpty(clientInfo))
                        {
                            ClientInfo? clientInfoFromServer = ClientInfo.CreateFromJson(clientInfo);

                            if (clientInfoFromServer != null && clientInfoFromServer.UniqueTenantIdentifier != null && clientInfoFromServer.UniqueObjectIdentifier != null)
                            {
                                context.AuthenticationTicket.Identity.AddClaim(new Claim(ClaimConstants.UniqueTenantIdentifier, clientInfoFromServer.UniqueTenantIdentifier));
                                context.AuthenticationTicket.Identity.AddClaim(new Claim(ClaimConstants.UniqueObjectIdentifier, clientInfoFromServer.UniqueObjectIdentifier));
                            }
                            httpContext.Session.Remove(ClaimConstants.ClientInfo);
                        }

                        Claim? nameClaim = context.AuthenticationTicket.Identity.FindFirst("preferred_username")
                                          ?? context.AuthenticationTicket.Identity.FindFirst("name");

                        if (!string.IsNullOrEmpty(nameClaim?.Value))
                        {
                            context.AuthenticationTicket.Identity.AddClaim(new Claim(ClaimTypes.Name, nameClaim?.Value, string.Empty));
                        }
                        
                        return Task.CompletedTask;
                    },
                }
            };

            options.TokenValidationParameters.IssuerValidator = AadIssuerValidator.GetAadIssuerValidator(options.Authority).Validate;

            options.Notifications.AuthorizationCodeReceived = async context =>
            {
                context.TokenEndpointRequest.Parameters.TryGetValue("code_verifier", out string codeVerifier);
                var tokenAcquisition = tokenAcquirerFactory?.ServiceProvider?.GetRequiredService<ITokenAcquisitionInternal>();
                var msIdentityOptions = tokenAcquirerFactory?.ServiceProvider?.GetRequiredService<IOptions<MicrosoftIdentityOptions>>();
                var result = await (tokenAcquisition!.AddAccountToCacheFromAuthorizationCodeAsync(
                    new AuthCodeRedemptionParameters(
                    new string[] { options.Scope },
                    context.Code,
                    string.Empty,
                    context.ProtocolMessage.GetParameter(ClaimConstants.ClientInfo),
                    codeVerifier,
                    msIdentityOptions?.Value.DefaultUserFlow,
                    context.ProtocolMessage.DomainHint))).ConfigureAwait(false);
                HttpContextBase httpContext = context.OwinContext.Get<HttpContextBase>(typeof(HttpContextBase).FullName);
                if (httpContext.Session is null)
                {
                    context.OwinContext.Set(ClaimConstants.ClientInfo, context.ProtocolMessage.GetParameter(ClaimConstants.ClientInfo));
                }
                else
                {
                    httpContext.Session.Add(ClaimConstants.ClientInfo, context.ProtocolMessage.GetParameter(ClaimConstants.ClientInfo));
                }
                context.HandleCodeRedemption(result.AccessToken, result.IdToken);
            };

            updateOptions?.Invoke(options);

            return app.UseOpenIdConnectAuthentication(options);
        }
    }
}
