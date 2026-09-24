// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.Extensions.Logging;

namespace Microsoft.Identity.Web
{
    internal class MicrosoftIdentityServiceHandler : CircuitHandler
    {
        private readonly ILogger<MicrosoftIdentityServiceHandler> _logger;
        private bool _circuitClosed;

        public MicrosoftIdentityServiceHandler(
            MicrosoftIdentityConsentAndConditionalAccessHandler service,
            AuthenticationStateProvider provider,
            NavigationManager manager,
            ILogger<MicrosoftIdentityServiceHandler> logger)
        {
            Service = service;
            Provider = provider;
            Manager = manager;
            _logger = logger;
        }

        public MicrosoftIdentityConsentAndConditionalAccessHandler Service { get; }

        public AuthenticationStateProvider Provider { get; }

        public NavigationManager Manager { get; }

        public override async Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken cancellationToken)
        {
            Provider.AuthenticationStateChanged += OnAuthenticationStateChanged;
            try
            {
                var state = await Provider.GetAuthenticationStateAsync().ConfigureAwait(false);
                Service.User = state.User;
                Service.IsBlazorServer = true;
                Service.BaseUri = Manager.BaseUri.TrimEnd('/');
                Service.NavigationManager = Manager;
                await base.OnCircuitOpenedAsync(circuit, cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                Provider.AuthenticationStateChanged -= OnAuthenticationStateChanged;
                throw;
            }
        }

        public override Task OnCircuitClosedAsync(Circuit circuit, CancellationToken cancellationToken)
        {
            _circuitClosed = true;
            Provider.AuthenticationStateChanged -= OnAuthenticationStateChanged;
            return base.OnCircuitClosedAsync(circuit, cancellationToken);
        }

        private void OnAuthenticationStateChanged(Task<AuthenticationState> stateTask)
            => _ = UpdateUserAsync(stateTask);

        private async Task UpdateUserAsync(Task<AuthenticationState> stateTask)
        {
            try
            {
#pragma warning disable VSTHRD003 // The framework supplies this task; ConfigureAwait(false) avoids capturing its context.
                var state = await stateTask.ConfigureAwait(false);
#pragma warning restore VSTHRD003
                if (!_circuitClosed)
                {
                    Service.User = state.User;
                }
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Unable to update the Blazor authentication state.");
            }
        }
    }
}
