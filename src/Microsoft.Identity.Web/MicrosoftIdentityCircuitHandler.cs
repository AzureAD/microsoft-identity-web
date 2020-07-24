using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Identity.Client;

namespace Microsoft.Identity.Web
{

    /// <summary>
    /// Extensions for IServiceCollection for startup initialization of Web APIs.
    /// </summary>
    public static class MicrosoftIdentityBlazorServiceCollectionExtensions
    {
        /// <summary>
        /// Add the incremental consent and conditional access handler for blazor
        /// server side pages
        /// </summary>
        /// <param name="builder">Service side blazor builder.</param>
        /// <returns>The builder.</returns>
        public static IServerSideBlazorBuilder AddMicrosoftIdentityConsentHandler(
            this IServerSideBlazorBuilder builder)
        {
            builder.Services.TryAddEnumerable(ServiceDescriptor.Scoped<CircuitHandler, MicrosoftIdentityServiceHandler>());
            builder.Services.TryAddScoped<MicrosoftIdentityConsentAndConditionalAccessHandler>();
            return builder;
        }
    }

    public class MicrosoftIdentityConsentAndConditionalAccessHandler
    {
        public bool IsBlazorServer { get; set; }
        public ClaimsPrincipal User { get; internal set; }
        public string BaseUri { get; internal set; }
        public void HandleException(Exception ex)
        {
            MsalUiRequiredException? msalUiRequiredException =
                   (ex as MsalUiRequiredException)
                   ?? (ex?.InnerException as MsalUiRequiredException);

            if (msalUiRequiredException != null &&
               IncrementalConsentAndConditionalAccessHelper.CanBeSolvedByReSignInOfUser(msalUiRequiredException))
            {
                var properties = IncrementalConsentAndConditionalAccessHelper.BuildAuthenticationProperties(new string[] { "user.read" },
                    msalUiRequiredException, User);

                // string redirectUri, string scope, string loginHint, string domainHint, string claims
                string redirectUri = NavigationManager.Uri;
                List<string> scope = properties.Parameters.ContainsKey("scope") ? (List<string>)properties.Parameters["scope"] : new List<string>();
                string loginHint = properties.Parameters.ContainsKey("loginHint") ? (string)properties.Parameters["loginHint"] : string.Empty;
                string domainHint = properties.Parameters.ContainsKey("domainHint") ? (string)properties.Parameters["domainHint"] : string.Empty;
                string claims = properties.Parameters.ContainsKey("claims") ? (string)properties.Parameters["claims"] : string.Empty;
                string url = $"{NavigationManager.BaseUri}MicrosoftIdentity/Account/Challenge?redirectUri={redirectUri}"
                + $"&scope={string.Join(" ", scope)}&loginHint={loginHint}"
                + $"&domainHint={domainHint}&claims={claims}";

                // url = "https://localhost:44357/MicrosoftIdentity/Account/Challenge?redirectUri=https:%2F%2Flocalhost:44357%2F%2FcallWebApi&scope=user.read%20openid%20offline_access%20profile&loginHint=&domainHint=&claims=";
                NavigationManager.NavigateTo(url, true);

            }
            else
            {
                throw ex;
            }
        }

        internal NavigationManager NavigationManager { get; set; }
    }

    internal class MicrosoftIdentityServiceHandler : CircuitHandler
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
