// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web.Hosts;
using Microsoft.Identity.Web.OWIN;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Microsoft.IdentityModel.Validators;
using Microsoft.Owin.Security.Jwt;
using Microsoft.Owin.Security.OAuth;
using Microsoft.Owin.Security.OpenIdConnect;
using Owin;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Extension methods on an ASP.NET application to add a web app or web API.
    /// </summary>
    public static class AppBuilderExtension
    {
        static internal TokenAcquirerFactory? TokenAcquirerFactory { get; set; }
        /// <summary>
        /// Configuration.
        /// </summary>
        static internal IConfiguration? Configuration { get { return TokenAcquirerFactory?.Configuration; } }

        /// <summary>
        /// Service provider.
        /// </summary>
        static internal IServiceProvider? ServiceProvider { get { return TokenAcquirerFactory?.ServiceProvider; } }

        /// <summary>
        /// Adds a protected web API.
        /// </summary>
        /// <param name="app">Application builder.</param>
        /// <param name="configureServices">Configure the services (if you want to call downstream web APIs).</param>
        /// <param name="configureMicrosoftAuthenticationOptions">Configure Microsoft authentication options.</param>
        /// <param name="updateOptions">Update the OWIN options if you want to finesse the token validation.</param>
        /// <param name="configurationSection">Configuration section in which to read the options.</param>
        /// <returns>The app builder to chain.</returns>
        public static IAppBuilder AddMicrosoftIdentityWebApi(
            this IAppBuilder app,
            Action<IServiceCollection>? configureServices = null,
            Action<MicrosoftAuthenticationOptions>? configureMicrosoftAuthenticationOptions = null,
            Action<OAuthBearerAuthenticationOptions>? updateOptions = null,
            string configurationSection = "AzureAd")
        {
            TokenAcquirerFactory = TokenAcquirerFactory.GetDefaultInstance<OwinTokenAcquirerFactory>();
            var services = TokenAcquirerFactory.Services;

            // Configure the Microsoft authentication options
            services.Configure(
                string.Empty,
                configureMicrosoftAuthenticationOptions ?? (option =>
            {
                Configuration?.GetSection(configurationSection).Bind(option);
            }));
            services.Configure<ConfidentialClientApplicationOptions>(
                string.Empty,
                (option =>
                {
                    Configuration?.GetSection(configurationSection).Bind(option);
                }));

            configureServices?.Invoke(services);

            // Replace the genenric host by an OWIN host
            ServiceDescriptor? tokenAcquisitionhost = services.FirstOrDefault(s => s.ServiceType == typeof(ITokenAcquisitionHost));
            if (tokenAcquisitionhost != null)
            {
                services.Remove(tokenAcquisitionhost);

                if (tokenAcquisitionhost.Lifetime == ServiceLifetime.Singleton)
                {
                    // The service was already added, but not with the right lifetime
                    services.AddSingleton<ITokenAcquisitionHost, OwinTokenAcquisitionHost>();
                }
                else
                {
                    // The service is already added with the right lifetime
                    services.AddScoped<ITokenAcquisitionHost, OwinTokenAcquisitionHost>();
                }
            }

            TokenAcquirerFactory.Build();
            string instance = Configuration.GetValue<string>($"{configurationSection}:Instance");
            string tenantId = Configuration.GetValue<string>($"{configurationSection}:TenantId");
            string clientId = Configuration.GetValue<string>($"{configurationSection}:ClientId");
            string audience = Configuration.GetValue<string>($"{configurationSection}:Audience");
            string authority = instance + tenantId + "/v2.0";
            TokenValidationParameters tokenValidationParameters = new TokenValidationParameters
            {
                ValidAudience = audience ?? clientId,
                IssuerValidator = AadIssuerValidator.GetAadIssuerValidator(authority).Validate,
                SaveSigninToken = true,
            };
            OAuthBearerAuthenticationOptions options = new OAuthBearerAuthenticationOptions
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
        /// <param name="configureServices">Configure the services (if you want to call downstream web APIs).</param>
        /// <param name="configureMicrosoftAuthenticationOptions">Configure Microsoft authentication options.</param>
        /// <param name="updateOptions">Update the OWIN options if you want to finesse the OpenIdConnect options.</param>
        /// <param name="configurationSection">Configuration section in which to read the options.</param>
        /// <returns>The app builder to chain.</returns>
        public static IAppBuilder AddMicrosoftIdentityWebApp(
            this IAppBuilder app,
            Action<IServiceCollection>? configureServices = null,
            Action<MicrosoftAuthenticationOptions>? configureMicrosoftAuthenticationOptions = null,
            Action<OpenIdConnectAuthenticationOptions>? updateOptions = null,
            string configurationSection = "AzureAd")
        {
            TokenAcquirerFactory = TokenAcquirerFactory.GetDefaultInstance<OwinTokenAcquirerFactory>();
            var services = TokenAcquirerFactory.Services;

            // Configure the Microsoft authentication options
            services.Configure(
                string.Empty,
                configureMicrosoftAuthenticationOptions ?? (option =>
                {
                    Configuration?.GetSection(configurationSection).Bind(option);
                }));
            services.Configure<ConfidentialClientApplicationOptions>(
                string.Empty,
                (option =>
                {
                    Configuration?.GetSection(configurationSection).Bind(option);
                }));

            configureServices?.Invoke(services);

            // Replace the genenric host by an OWIN host
            ServiceDescriptor? tokenAcquisitionhost = services.FirstOrDefault(s => s.ServiceType == typeof(ITokenAcquisitionHost));
            if (tokenAcquisitionhost != null)
            {
                services.Remove(tokenAcquisitionhost);

                if (tokenAcquisitionhost.Lifetime == ServiceLifetime.Singleton)
                {
                    // The service was already added, but not with the right lifetime
                    services.AddSingleton<ITokenAcquisitionHost, OwinTokenAcquisitionHost>();
                }
                else
                {
                    // The service is already added with the right lifetime
                    services.AddScoped<ITokenAcquisitionHost, OwinTokenAcquisitionHost>();
                }
            }

            TokenAcquirerFactory.Build();
            string instance = Configuration.GetValue<string>($"{configurationSection}:Instance");
            string tenantId = Configuration.GetValue<string>($"{configurationSection}:TenantId");
            string clientId = Configuration.GetValue<string>($"{configurationSection}:ClientId");
            string postLogoutRedirectUri = Configuration.GetValue<string>($"{configurationSection}:SignedOutCallbackPath");
            string authority = instance + tenantId + "/v2.0";

            OpenIdConnectAuthenticationOptions options = new OpenIdConnectAuthenticationOptions
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

                        string? clientInfo = httpContext.Session[ClaimConstants.ClientInfo] as string;

                        if (!string.IsNullOrEmpty(clientInfo))
                        {
                            ClientInfo? clientInfoFromServer = ClientInfo.CreateFromJson(clientInfo);

                            if (clientInfoFromServer != null && clientInfoFromServer.UniqueTenantIdentifier != null && clientInfoFromServer.UniqueObjectIdentifier != null)
                            {
                                context.AuthenticationTicket.Identity.AddClaim(new Claim(ClaimConstants.UniqueTenantIdentifier, clientInfoFromServer.UniqueTenantIdentifier));
                                context.AuthenticationTicket.Identity.AddClaim(new Claim(ClaimConstants.UniqueObjectIdentifier, clientInfoFromServer.UniqueObjectIdentifier));
                            }
                            httpContext.Session.Remove(ClaimConstants.ClientInfo);
                        }

                        string name = context.AuthenticationTicket.Identity.FindFirst("preferred_username").Value;
                        context.AuthenticationTicket.Identity.AddClaim(new Claim(ClaimTypes.Name, name, string.Empty));
                        return Task.CompletedTask;
                    },
                }
            };

            options.Notifications.AuthorizationCodeReceived = async context =>
            {
                context.TokenEndpointRequest.Parameters.TryGetValue("code_verifier", out string codeVerifier);
                var tokenAcquisition = TokenAcquirerFactory?.ServiceProvider?.GetRequiredService<ITokenAcquisitionInternal>();
                var msIdentityOptions = TokenAcquirerFactory?.ServiceProvider?.GetRequiredService<IOptions<MicrosoftIdentityOptions>>();
                var idToken = await (tokenAcquisition!.AddAccountToCacheFromAuthorizationCodeAsync(
                    new string[] { options.Scope },
                    context.Code,
                    string.Empty,
                    context.ProtocolMessage.GetParameter(ClaimConstants.ClientInfo),
                    codeVerifier,
                    msIdentityOptions?.Value.DefaultUserFlow)).ConfigureAwait(false);
                HttpContextBase httpContext = context.OwinContext.Get<HttpContextBase>(typeof(HttpContextBase).FullName);
                httpContext.Session.Add(ClaimConstants.ClientInfo, context.ProtocolMessage.GetParameter(ClaimConstants.ClientInfo));
                context.HandleCodeRedemption(null, idToken);
            };

            updateOptions?.Invoke(options);

            return app.UseOpenIdConnectAuthentication(options);
        }
    }
}
