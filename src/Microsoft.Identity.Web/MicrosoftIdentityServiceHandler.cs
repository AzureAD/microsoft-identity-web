// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.Circuits;

namespace Microsoft.Identity.Web
{
    internal class MicrosoftIdentityServiceHandler : CircuitHandler
    {
        // Guards the fields below. Authentication state tasks can complete on any thread
        // and in any order, so the user is only replaced by a state that is at least as
        // recent as the one already applied.
        private readonly object _userLock = new object();
        private long _lastIssuedVersion;
        private long _lastAppliedVersion = -1;
        private bool _isClosed;

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
            // Subscribe before reading the initial state so that a change published while
            // the circuit is initializing is not lost.
            Provider.AuthenticationStateChanged += OnAuthenticationStateChanged;

            long initialVersion;
            lock (_userLock)
            {
                initialVersion = _lastIssuedVersion;
            }

            var state = await Provider.GetAuthenticationStateAsync().ConfigureAwait(false);
            TryApplyUser(state.User, initialVersion);
            Service.IsBlazorServer = true;
            Service.BaseUri = Manager.BaseUri.TrimEnd('/');
            Service.NavigationManager = Manager;
            await base.OnCircuitOpenedAsync(circuit, cancellationToken).ConfigureAwait(false);
        }

        public override Task OnCircuitClosedAsync(Circuit circuit, CancellationToken cancellationToken)
        {
            Provider.AuthenticationStateChanged -= OnAuthenticationStateChanged;

            lock (_userLock)
            {
                _isClosed = true;
            }

            return base.OnCircuitClosedAsync(circuit, cancellationToken);
        }

        private void OnAuthenticationStateChanged(Task<AuthenticationState> task)
        {
            long version;
            lock (_userLock)
            {
                version = ++_lastIssuedVersion;
            }

            _ = UpdateUserAsync(task, version);
        }

        private async Task UpdateUserAsync(Task<AuthenticationState> task, long version)
        {
            AuthenticationState state;
            try
            {
                // The task is published by the AuthenticationStateProvider for subscribers to await.
#pragma warning disable VSTHRD003 // Avoid awaiting foreign Tasks
                state = await task.ConfigureAwait(false);
#pragma warning restore VSTHRD003 // Avoid awaiting foreign Tasks
            }
            catch
            {
                // A failed or canceled state change keeps the previously known user.
                return;
            }

            TryApplyUser(state.User, version);
        }

        private void TryApplyUser(ClaimsPrincipal user, long version)
        {
            lock (_userLock)
            {
                if (_isClosed || version < _lastAppliedVersion)
                {
                    return;
                }

                _lastAppliedVersion = version;
                Service.User = user;
            }
        }
    }
}
