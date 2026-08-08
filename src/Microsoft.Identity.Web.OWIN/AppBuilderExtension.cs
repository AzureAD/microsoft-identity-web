// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.Security.Authentication;
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
using Microsoft.Owin;
using Microsoft.Owin.Security.Jwt;
using Microsoft.Owin.Security.Notifications;
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

            string? instance = configuration.GetValue<string>($"{configurationSection}:Instance");
            string? tenantId = configuration.GetValue<string>($"{configurationSection}:TenantId");
            string? clientId = configuration.GetValue<string>($"{configurationSection}:ClientId");
            string? audience = configuration.GetValue<string>($"{configurationSection}:Audience");
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

            string? instance = configuration.GetValue<string>($"{configurationSection}:Instance");
            string? tenantId = configuration.GetValue<string>($"{configurationSection}:TenantId");
            string? clientId = configuration.GetValue<string>($"{configurationSection}:ClientId");
            string? postLogoutRedirectUri = configuration.GetValue<string>($"{configurationSection}:SignedOutCallbackPath");
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

                        if (context.ProtocolMessage.IssuerAddress != null && context.ProtocolMessage.IssuerAddress.EndsWith("/authorize", StringComparison.OrdinalIgnoreCase))
                        {
                            context.ProtocolMessage.RedirectUri = context.Request.Uri.ToString();
                        }

                        return Task.CompletedTask;
                    },

                    SecurityTokenValidated = (context) =>
                    {
                        HttpContextBase httpContext = context.OwinContext.Get<HttpContextBase>(typeof(HttpContextBase).FullName);

                        (string? homeObjectId, string? homeTenantId) = GetAndRemoveHomeAccountIdentifier(context.OwinContext, httpContext);
                        AddHomeAccountIdentifierClaims(context.AuthenticationTicket.Identity, homeObjectId, homeTenantId);

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
                var redemption = await (tokenAcquisition!.AddAccountToCacheFromAuthorizationCodeAndGetAccountAsync(
                    new AuthCodeRedemptionParameters(
                    new string[] { options.Scope },
                    context.Code,
                    string.Empty,
                    context.ProtocolMessage.GetParameter(ClaimConstants.ClientInfo),
                    codeVerifier,
                    msIdentityOptions?.Value.DefaultUserFlow,
                    context.ProtocolMessage.DomainHint))).ConfigureAwait(false);
                HttpContextBase httpContext = context.OwinContext.Get<HttpContextBase>(typeof(HttpContextBase).FullName);
                StoreHomeAccountIdentifier(context.OwinContext, httpContext, redemption.HomeObjectId, redemption.HomeTenantId);
                context.HandleCodeRedemption(redemption.Result.AccessToken, redemption.Result.IdToken);
            };

            updateOptions?.Invoke(options);

            return app.UseOpenIdConnectAuthentication(options);
        }

        /// <summary>
        /// The SDK injects 2 claims named uuid and utid into the ClaimsPrincipal, from ESTS's client_info. These represent 
        /// the home oid and home tid and together they form the AccountId, which MSAL uses as cache key for web site 
        /// scenarios. In case the app owner defines claims with the same name to be added to the ID Token, this creates 
        /// a conflict with the reserved claims Id.Web injects, and it is better to throw a meaningful error. See issue 2968 for details.
        /// </summary>
        /// <param name="identity">The <see cref="ClaimsIdentity"/> associated to the logged-in user</param>
        /// <param name="claimType">The claim type</param>
        /// <param name="claimValue">The claim value</param>
        /// <exception cref="AuthenticationException">The <see cref="ClaimsIdentity"/> contains internal claims that are used internal use by this library</exception>
        private static void RejectInternalClaims(ClaimsIdentity identity, string claimType, string claimValue)
        {
            var identityClaim = identity.FindFirst(c => c.Type == claimType);
            if (identityClaim != null && !string.Equals(claimValue, identityClaim.Value, StringComparison.OrdinalIgnoreCase))
            {
                throw new AuthenticationException(string.Format(CultureInfo.InvariantCulture, IDWebErrorMessage.InternalClaimDetected, claimType));
            }
        }

        /// <summary>
        /// Stashes the home account identifier (uid/utid) taken from the token that was actually
        /// redeemed, so that the account-identifier claims can later be stamped from a value that is
        /// consistent with the acquired token. Nothing is stored when either value is absent.
        /// </summary>
        /// <param name="owinContext">The current OWIN context.</param>
        /// <param name="httpContext">The current <see cref="HttpContextBase"/>.</param>
        /// <param name="homeObjectId">Home account object identifier ("uid") from the redeemed token.</param>
        /// <param name="homeTenantId">Home account tenant identifier ("utid") from the redeemed token.</param>
        internal static void StoreHomeAccountIdentifier(IOwinContext owinContext, HttpContextBase httpContext, string? homeObjectId, string? homeTenantId)
        {
            if (string.IsNullOrEmpty(homeObjectId) || string.IsNullOrEmpty(homeTenantId))
            {
                return;
            }

            if (httpContext.Session is null)
            {
                owinContext.Set(Constants.HomeAccountObjectIdCacheKey, homeObjectId);
                owinContext.Set(Constants.HomeAccountTenantIdCacheKey, homeTenantId);
            }
            else
            {
                httpContext.Session[Constants.HomeAccountObjectIdCacheKey] = homeObjectId;
                httpContext.Session[Constants.HomeAccountTenantIdCacheKey] = homeTenantId;
            }
        }

        /// <summary>
        /// Reads and removes the home account identifier (uid/utid) previously stashed during
        /// authorization-code redemption, from the OWIN context (no session) or the session.
        /// </summary>
        /// <param name="owinContext">The current OWIN context.</param>
        /// <param name="httpContext">The current <see cref="HttpContextBase"/>.</param>
        /// <returns>The home account object identifier and tenant identifier, either of which can be <see langword="null"/>.</returns>
        internal static (string? homeObjectId, string? homeTenantId) GetAndRemoveHomeAccountIdentifier(IOwinContext owinContext, HttpContextBase httpContext)
        {
            string? homeObjectId;
            string? homeTenantId;

            if (httpContext.Session is null)
            {
                homeObjectId = owinContext.Get<string>(Constants.HomeAccountObjectIdCacheKey);
                homeTenantId = owinContext.Get<string>(Constants.HomeAccountTenantIdCacheKey);
                owinContext.Environment.Remove(Constants.HomeAccountObjectIdCacheKey);
                owinContext.Environment.Remove(Constants.HomeAccountTenantIdCacheKey);
            }
            else
            {
                homeObjectId = httpContext.Session[Constants.HomeAccountObjectIdCacheKey] as string;
                homeTenantId = httpContext.Session[Constants.HomeAccountTenantIdCacheKey] as string;
                httpContext.Session.Remove(Constants.HomeAccountObjectIdCacheKey);
                httpContext.Session.Remove(Constants.HomeAccountTenantIdCacheKey);
            }

            return (homeObjectId, homeTenantId);
        }

        /// <summary>
        /// Stamps the home account identifier claims (uid/utid) on the identity, first ensuring they do
        /// not conflict with same-named claims the app may have injected (see issue 2968). Both claims are
        /// added only when both values are present.
        /// </summary>
        /// <param name="identity">The <see cref="ClaimsIdentity"/> associated to the logged-in user.</param>
        /// <param name="homeObjectId">Home account object identifier ("uid") from the redeemed token.</param>
        /// <param name="homeTenantId">Home account tenant identifier ("utid") from the redeemed token.</param>
        internal static void AddHomeAccountIdentifierClaims(ClaimsIdentity identity, string? homeObjectId, string? homeTenantId)
        {
            if (homeTenantId != null)
            {
                RejectInternalClaims(identity, ClaimConstants.UniqueTenantIdentifier, homeTenantId);
            }

            if (homeObjectId != null)
            {
                RejectInternalClaims(identity, ClaimConstants.UniqueObjectIdentifier, homeObjectId);
            }

            if (homeTenantId != null && homeObjectId != null)
            {
                identity.AddClaim(new Claim(ClaimConstants.UniqueTenantIdentifier, homeTenantId));
                identity.AddClaim(new Claim(ClaimConstants.UniqueObjectIdentifier, homeObjectId));
            }
        }
    }
}
