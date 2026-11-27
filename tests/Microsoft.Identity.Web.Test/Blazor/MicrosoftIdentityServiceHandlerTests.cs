// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Microsoft.Identity.Web.Test.Blazor
{
    public class MicrosoftIdentityServiceHandlerTests
    {
        private static readonly ClaimsPrincipal s_alice = CreateUser("alice");
        private static readonly ClaimsPrincipal s_bob = CreateUser("bob");
        private static readonly ClaimsPrincipal s_anonymous = new ClaimsPrincipal(new CaseSensitiveClaimsIdentity());

        [Fact]
        public async Task OnCircuitOpened_SetsInitialUser()
        {
            var (handler, provider, service) = CreateHandler(s_alice);

            await handler.OnCircuitOpenedAsync(null!, CancellationToken.None);

            Assert.Same(s_alice, service.User);
            Assert.True(service.IsBlazorServer);
            Assert.Equal("https://localhost", service.BaseUri);
        }

        [Fact]
        public async Task AuthenticationStateChanged_AfterOpen_UpdatesUser()
        {
            var (handler, provider, service) = CreateHandler(s_alice);
            await handler.OnCircuitOpenedAsync(null!, CancellationToken.None);

            provider.Notify(Task.FromResult(new AuthenticationState(s_bob)));
            Assert.Same(s_bob, service.User);

            provider.Notify(Task.FromResult(new AuthenticationState(s_anonymous)));
            Assert.Same(s_anonymous, service.User);
            Assert.False(service.User.Identity!.IsAuthenticated);
        }

        [Fact]
        public async Task AuthenticationStateChanged_CompletingOutOfOrder_LatestNotificationWins()
        {
            var (handler, provider, service) = CreateHandler(s_alice);
            await handler.OnCircuitOpenedAsync(null!, CancellationToken.None);

            var older = new TaskCompletionSource<AuthenticationState>(TaskCreationOptions.RunContinuationsAsynchronously);
            var newer = new TaskCompletionSource<AuthenticationState>(TaskCreationOptions.RunContinuationsAsynchronously);
            provider.Notify(older.Task);
            provider.Notify(newer.Task);

            newer.SetResult(new AuthenticationState(s_bob));
            await WaitForUserAsync(service, s_bob);

            older.SetResult(new AuthenticationState(s_anonymous));
            await Task.Delay(50);

            Assert.Same(s_bob, service.User);
        }

        [Fact]
        public async Task AuthenticationStateChanged_FailedOrCanceledTask_KeepsPreviousUser()
        {
            var (handler, provider, service) = CreateHandler(s_alice);
            await handler.OnCircuitOpenedAsync(null!, CancellationToken.None);

            provider.Notify(Task.FromException<AuthenticationState>(new InvalidOperationException("boom")));
            Assert.Same(s_alice, service.User);

            provider.Notify(Task.FromCanceled<AuthenticationState>(new CancellationToken(canceled: true)));
            Assert.Same(s_alice, service.User);

            provider.Notify(Task.FromResult(new AuthenticationState(s_bob)));
            Assert.Same(s_bob, service.User);
        }

        [Fact]
        public async Task AuthenticationStateChanged_PendingDuringOpen_DoesNotLeaveUserUnset()
        {
            var initial = new TaskCompletionSource<AuthenticationState>(TaskCreationOptions.RunContinuationsAsynchronously);
            var (handler, provider, service) = CreateHandler(initial.Task);

            Task opening = handler.OnCircuitOpenedAsync(null!, CancellationToken.None);
            var pendingChange = new TaskCompletionSource<AuthenticationState>(TaskCreationOptions.RunContinuationsAsynchronously);
            provider.Notify(pendingChange.Task);

            initial.SetResult(new AuthenticationState(s_alice));
            await opening;

            Assert.Same(s_alice, service.User);

            pendingChange.SetResult(new AuthenticationState(s_bob));
            await WaitForUserAsync(service, s_bob);
        }

        [Fact]
        public async Task AuthenticationStateChanged_CompletedDuringOpen_IsNotOverwrittenByInitialState()
        {
            var initial = new TaskCompletionSource<AuthenticationState>(TaskCreationOptions.RunContinuationsAsynchronously);
            var (handler, provider, service) = CreateHandler(initial.Task);

            Task opening = handler.OnCircuitOpenedAsync(null!, CancellationToken.None);
            provider.Notify(Task.FromResult(new AuthenticationState(s_bob)));

            initial.SetResult(new AuthenticationState(s_alice));
            await opening;

            Assert.Same(s_bob, service.User);
        }

        [Fact]
        public async Task OnCircuitClosed_IgnoresLaterAuthenticationStateChanges()
        {
            var (handler, provider, service) = CreateHandler(s_alice);
            await handler.OnCircuitOpenedAsync(null!, CancellationToken.None);

            var pendingChange = new TaskCompletionSource<AuthenticationState>(TaskCreationOptions.RunContinuationsAsynchronously);
            provider.Notify(pendingChange.Task);

            await handler.OnCircuitClosedAsync(null!, CancellationToken.None);

            pendingChange.SetResult(new AuthenticationState(s_bob));
            provider.Notify(Task.FromResult(new AuthenticationState(s_anonymous)));
            await Task.Delay(50);

            Assert.Same(s_alice, service.User);
        }

        private static (MicrosoftIdentityServiceHandler Handler, TestAuthenticationStateProvider Provider, MicrosoftIdentityConsentAndConditionalAccessHandler Service) CreateHandler(ClaimsPrincipal initialUser)
        {
            return CreateHandler(Task.FromResult(new AuthenticationState(initialUser)));
        }

        private static (MicrosoftIdentityServiceHandler Handler, TestAuthenticationStateProvider Provider, MicrosoftIdentityConsentAndConditionalAccessHandler Service) CreateHandler(Task<AuthenticationState> initialState)
        {
            var provider = new TestAuthenticationStateProvider(initialState);
            var service = new MicrosoftIdentityConsentAndConditionalAccessHandler(new ServiceCollection().BuildServiceProvider());
            var handler = new MicrosoftIdentityServiceHandler(service, provider, new TestNavigationManager());
            return (handler, provider, service);
        }

        private static ClaimsPrincipal CreateUser(string name)
        {
            return new ClaimsPrincipal(new CaseSensitiveClaimsIdentity(new[] { new Claim(ClaimTypes.Name, name) }, "TestAuth"));
        }

        private static async Task WaitForUserAsync(MicrosoftIdentityConsentAndConditionalAccessHandler service, ClaimsPrincipal expected)
        {
            for (int i = 0; i < 100 && !ReferenceEquals(service.User, expected); i++)
            {
                await Task.Delay(10);
            }

            Assert.Same(expected, service.User);
        }

        private sealed class TestAuthenticationStateProvider : AuthenticationStateProvider
        {
            private readonly Task<AuthenticationState> _initialState;

            public TestAuthenticationStateProvider(Task<AuthenticationState> initialState)
            {
                _initialState = initialState;
            }

#pragma warning disable VSTHRD003 // Avoid awaiting foreign Tasks
            public override Task<AuthenticationState> GetAuthenticationStateAsync() => _initialState;
#pragma warning restore VSTHRD003 // Avoid awaiting foreign Tasks

            public void Notify(Task<AuthenticationState> task) => NotifyAuthenticationStateChanged(task);

        }

        private sealed class TestNavigationManager : NavigationManager
        {
            public TestNavigationManager()
            {
                Initialize("https://localhost/", "https://localhost/");
            }
        }
    }
}
