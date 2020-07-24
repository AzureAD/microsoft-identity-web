// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Extensions for IServiceCollection for startup initialization of web APIs.
    /// </summary>
    public static class MicrosoftIdentityBlazorServiceCollectionExtensions
    {
        /// <summary>
        /// Add the incremental consent and conditional access handler for blazor
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
    }

    /// <summary>
    /// Handler for Blazor specific APIs to handle incremental consent
    /// and conditional access.
    /// </summary>
#pragma warning disable SA1402 // File may only contain a single type
    public class MicrosoftIdentityConsentAndConditionalAccessHandler
#pragma warning restore SA1402 // File may only contain a single type
    {
        /// <summary>
        /// Boolean to determine if server is Blazor.
        /// </summary>
        public bool IsBlazorServer { get; set; }

        /// <summary>
        /// Current user.
        /// </summary>
        public ClaimsPrincipal User { get; internal set; }

        /// <summary>
        /// Base uri to use in forming the redirect.
        /// </summary>
        public string? BaseUri { get; internal set; }

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
                    User);

                // string redirectUri, string scope, string loginHint, string domainHint, string claims
                string redirectUri = NavigationManager.Uri;
                List<string> scope = properties.Parameters.ContainsKey(Constants.Scope) ? (List<string>)properties.Parameters[Constants.Scope] : new List<string>();
                string loginHint = properties.Parameters.ContainsKey(Constants.LoginHint) ? (string)properties.Parameters[Constants.LoginHint] : string.Empty;
                string domainHint = properties.Parameters.ContainsKey(Constants.DomainHint) ? (string)properties.Parameters[Constants.DomainHint] : string.Empty;
                string claims = properties.Parameters.ContainsKey(Constants.Claims) ? (string)properties.Parameters[Constants.Claims] : string.Empty;
                string url = $"{NavigationManager.BaseUri}{Constants.BlazorChallengeUri}{redirectUri}"
                + $"&{Constants.Scope}={string.Join(" ", scope)}&{Constants.LoginHint}={loginHint}"
                + $"&{Constants.DomainHint}={domainHint}&{Constants.Claims}={claims}";

                NavigationManager.NavigateTo(url, true);
            }
            else
            {
                throw exception;
            }
        }

#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
        internal NavigationManager NavigationManager { get; set; }
#pragma warning restore CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
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
            Service.BaseUri = Manager.BaseUri;
            Service.NavigationManager = Manager;
            await base.OnCircuitOpenedAsync(circuit, cancellationToken).ConfigureAwait(false);
        }
    }
}
