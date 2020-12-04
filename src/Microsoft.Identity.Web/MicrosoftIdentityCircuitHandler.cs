// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.Http;
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

    /// <summary>
    /// Handler for Blazor specific APIs to handle incremental consent
    /// and conditional access.
    /// </summary>
#pragma warning disable SA1402 // File may only contain a single type
    public class MicrosoftIdentityConsentAndConditionalAccessHandler
#pragma warning restore SA1402 // File may only contain a single type
    {
        private ClaimsPrincipal? _user;
        private string? _baseUri;
#pragma warning disable CS8602 // Dereference of a possibly null reference. HttpContext will not be null in this case.
        private readonly IHttpContextAccessor? _httpContextAccessor;
#pragma warning restore CS8602 // Dereference of a possibly null reference. HttpContext will not be null in this case.

        /// <summary>
        /// Initializes a new instance of the <see cref="MicrosoftIdentityConsentAndConditionalAccessHandler"/> class.
        /// </summary>
        /// <param name="serviceProvider">Service provider to get the HttpContextAccessor for the current HttpContext, when available.</param>
        public MicrosoftIdentityConsentAndConditionalAccessHandler(IServiceProvider serviceProvider)
        {
            _httpContextAccessor = serviceProvider.GetService<IHttpContextAccessor>();
        }

        /// <summary>
        /// Boolean to determine if server is Blazor.
        /// </summary>
        public bool IsBlazorServer { get; set; }

        /// <summary>
        /// Current user.
        /// </summary>
        public ClaimsPrincipal User
        {
            get
            {
                return _user ??
#pragma warning disable CS8602 // Dereference of a possibly null reference. HttpContext will not be null in this case.
                    (!IsBlazorServer ? _httpContextAccessor.HttpContext.User :
#pragma warning restore CS8602 // Dereference of a possibly null reference. HttpContext will not be null in this case.
                    throw new InvalidOperationException(IDWebErrorMessage.BlazorServerUserNotSet));
            }
            set
            {
                _user = value;
            }
        }

        /// <summary>
        /// Base URI to use in forming the redirect.
        /// </summary>
        public string? BaseUri
        {
            get
            {
                return _baseUri ??
#pragma warning disable CS8602 // Dereference of a possibly null reference. HttpContext will not be null in this case
                    (!IsBlazorServer ? CreateBaseUri(_httpContextAccessor.HttpContext.Request) :
#pragma warning restore CS8602 // Dereference of a possibly null reference. HttpContext will not be null in this case
                    throw new InvalidOperationException(IDWebErrorMessage.BlazorServerBaseUriNotSet));
            }
            set
            {
                _baseUri = value;
            }
        }

        private static string CreateBaseUri(HttpRequest request)
        {
            string baseUri = string.Format(
                CultureInfo.InvariantCulture,
                "{0}://{1}/{2}",
                request.Scheme,
                request.Host.ToString(),
                request.PathBase.ToString().TrimStart('/'));
            return baseUri.TrimEnd('/');
        }

        /// <summary>
        /// For Blazor/Razor pages to process the exception from
        /// a user challenge.
        /// </summary>
        /// <param name="exception">Exception.</param>
        public void HandleException(Exception exception)
        {
            MicrosoftIdentityWebChallengeUserException? microsoftIdentityWebChallengeUserException =
                   exception as MicrosoftIdentityWebChallengeUserException;

            if (microsoftIdentityWebChallengeUserException == null)
            {
#pragma warning disable CA1062 // Validate arguments of public methods
                microsoftIdentityWebChallengeUserException = exception.InnerException as MicrosoftIdentityWebChallengeUserException;
#pragma warning restore CA1062 // Validate arguments of public methods
            }

            if (microsoftIdentityWebChallengeUserException != null &&
               IncrementalConsentAndConditionalAccessHelper.CanBeSolvedByReSignInOfUser(microsoftIdentityWebChallengeUserException.MsalUiRequiredException))
            {
                var properties = IncrementalConsentAndConditionalAccessHelper.BuildAuthenticationProperties(
                    microsoftIdentityWebChallengeUserException.Scopes,
                    microsoftIdentityWebChallengeUserException.MsalUiRequiredException,
                    User,
                    microsoftIdentityWebChallengeUserException.Userflow);

                List<string> scopes = properties.Parameters.ContainsKey(Constants.Scope) ? (List<string>)properties.Parameters[Constants.Scope]! : new List<string>();
                string claims = properties.Parameters.ContainsKey(Constants.Claims) ? (string)properties.Parameters[Constants.Claims]! : string.Empty;
                string userflow = properties.Items.ContainsKey(OidcConstants.PolicyKey) ? properties.Items[OidcConstants.PolicyKey]! : string.Empty;

                ChallengeUser(
                    scopes.ToArray(),
                    claims,
                    userflow);
            }
            else
            {
                throw exception;
            }
        }

        /// <summary>
        /// Forces the user to consent to specific scopes and perform
        /// Conditional Access to get specific claims. Use on a Razor/Blazor
        /// page or controller to proactively ensure the scopes and/or claims
        /// before acquiring a token. The other mechanism <see cref="HandleException(Exception)"/>
        /// ensures claims and scopes requested by Azure AD after a failed token acquisition attempt.
        /// See https://aka.ms/ms-id-web/ca_incremental-consent for details.
        /// </summary>
        /// <param name="scopes">Scopes to request.</param>
        /// <param name="claims">Claims to ensure.</param>
        /// <param name="userflow">Userflow being invoked for AAD B2C.</param>
        public void ChallengeUser(
            string[]? scopes,
            string? claims = null,
            string? userflow = null)
        {
            IEnumerable<string> effectiveScopes = scopes ?? new string[0];

            string[] additionalBuiltInScopes =
            {
                 OidcConstants.ScopeOpenId,
                 OidcConstants.ScopeOfflineAccess,
                 OidcConstants.ScopeProfile,
            };

            effectiveScopes = effectiveScopes.Union(additionalBuiltInScopes).ToArray();

            string redirectUri;
            if (IsBlazorServer)
            {
                redirectUri = NavigationManager.Uri;
            }
            else
            {
#pragma warning disable CS8602 // Dereference of a possibly null reference. HttpContext will not be null in this case.
                var request = _httpContextAccessor.HttpContext.Request;
#pragma warning restore CS8602 // Dereference of a possibly null reference. HttpContext will not be null in this case.
                redirectUri = string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}/{1}",
                    CreateBaseUri(request),
                    request.Path.ToString().TrimStart('/'));
            }

            string url = $"{BaseUri}/{Constants.BlazorChallengeUri}{redirectUri}"
                + $"&{Constants.Scope}={string.Join(" ", effectiveScopes!)}&{Constants.LoginHint}={User.GetLoginHint()}"
                + $"&{Constants.DomainHint}={User.GetDomainHint()}&{Constants.Claims}={claims}"
                + $"&{OidcConstants.PolicyKey}={userflow}";

            if (IsBlazorServer)
            {
                NavigationManager.NavigateTo(url, true);
            }
            else
            {
#pragma warning disable CS8602 // Dereference of a possibly null reference. HttpContext will not be null in this case.
                _httpContextAccessor.HttpContext.Response.Redirect(url);
#pragma warning restore CS8602 // Dereference of a possibly null reference. HttpContext will not be null in this case.
            }
        }

        internal NavigationManager NavigationManager { get; set; } = null!;
    }

#pragma warning disable SA1402 // File may only contain a single type
    internal class MicrosoftIdentityServiceHandler : CircuitHandler
#pragma warning restore SA1402 // File may only contain a single type
    {
        public MicrosoftIdentityServiceHandler(MicrosoftIdentityConsentAndConditionalAccessHandler service, AuthenticationStateProvider provider, NavigationManager manager)
        {
            Service = service;
            Provider = provider;
            Manager = manager;
        }

        public MicrosoftIdentityConsentAndConditionalAccessHandler Service { get; }

        public AuthenticationStateProvider Provider { get; }

        public NavigationManager Manager { get; }

        public override async Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken cancellationToken)
        {
            var state = await Provider.GetAuthenticationStateAsync().ConfigureAwait(false);
            Service.User = state.User;
            Service.IsBlazorServer = true;
            Service.BaseUri = Manager.BaseUri.TrimEnd('/');
            Service.NavigationManager = Manager;
            await base.OnCircuitOpenedAsync(circuit, cancellationToken).ConfigureAwait(false);
        }
    }
}
