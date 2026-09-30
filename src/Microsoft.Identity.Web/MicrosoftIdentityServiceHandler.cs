// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.Circuits;

namespace Microsoft.Identity.Web
{
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
#pragma warning disable BL0013 // Refreshing the cached circuit user is tracked in #4075.
            var state = await Provider.GetAuthenticationStateAsync().ConfigureAwait(false);
#pragma warning restore BL0013
            Service.User = state.User;
            Service.IsBlazorServer = true;
            Service.BaseUri = Manager.BaseUri.TrimEnd('/');
            Service.NavigationManager = Manager;
            await base.OnCircuitOpenedAsync(circuit, cancellationToken).ConfigureAwait(false);
        }
    }
}
