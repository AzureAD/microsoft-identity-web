// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Identity.Web.Test.Common.Mocks;
using Microsoft.IdentityModel.Tokens;
using NSubstitute;
using Xunit;

namespace Microsoft.Identity.Web.Test.Blazor;

public class MicrosoftIdentityServiceHandlerTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CircuitUpdatesUserWhenAuthenticationStateChanges(bool anonymous)
    {
        // Arrange
        var firstUser = new ClaimsPrincipal(new CaseSensitiveClaimsIdentity("test"));
        var secondUser = new ClaimsPrincipal(anonymous
            ? new CaseSensitiveClaimsIdentity()
            : new CaseSensitiveClaimsIdentity("updated"));
        var provider = new TestAuthenticationStateProvider(firstUser);
        using var services = new ServiceCollection().BuildServiceProvider();
        var consentHandler = new MicrosoftIdentityConsentAndConditionalAccessHandler(services);
        var handler = new MicrosoftIdentityServiceHandler(
            consentHandler, provider, new TestNavigationManager(), NullLogger<MicrosoftIdentityServiceHandler>.Instance);

        // Act
        await handler.OnCircuitOpenedAsync(null!, CancellationToken.None);
        Assert.Same(firstUser, consentHandler.User);

        provider.ChangeUser(secondUser);

        // Assert
        Assert.Same(secondUser, consentHandler.User);
        Assert.Equal(!anonymous, consentHandler.User.Identity!.IsAuthenticated);

        await handler.OnCircuitClosedAsync(null!, CancellationToken.None);
        provider.ChangeUser(firstUser);
        Assert.Same(secondUser, consentHandler.User);
    }

    [Fact]
    public async Task PendingAuthenticationChangeDoesNotPreventInitialUser()
    {
        // Arrange
        var firstUser = new ClaimsPrincipal(new CaseSensitiveClaimsIdentity("initial"));
        var secondUser = new ClaimsPrincipal(new CaseSensitiveClaimsIdentity("updated"));
        var initialState = new TaskCompletionSource<AuthenticationState>(TaskCreationOptions.RunContinuationsAsynchronously);
        var changedState = new TaskCompletionSource<AuthenticationState>();
        var provider = new TestAuthenticationStateProvider(firstUser, initialState.Task);
        using var services = new ServiceCollection().BuildServiceProvider();
        var consentHandler = new MicrosoftIdentityConsentAndConditionalAccessHandler(services);
        var handler = new MicrosoftIdentityServiceHandler(
            consentHandler, provider, new TestNavigationManager(), NullLogger<MicrosoftIdentityServiceHandler>.Instance);

        try
        {
            // Act
            var opening = handler.OnCircuitOpenedAsync(null!, CancellationToken.None);
            provider.ChangeUser(changedState.Task);
            initialState.SetResult(new AuthenticationState(firstUser));
            await opening;

            // Assert
            Assert.Same(firstUser, consentHandler.User);

            // Complete on a worker so the handler's ConfigureAwait(false) continuation runs inline.
            await Task.Run(() => changedState.SetResult(new AuthenticationState(secondUser)));
            Assert.Same(secondUser, consentHandler.User);
        }
        finally
        {
            await handler.OnCircuitClosedAsync(null!, CancellationToken.None);
            changedState.TrySetResult(new AuthenticationState(secondUser));
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task InitialStateDoesNotOverwriteAuthenticationChange(bool newerChangePending)
    {
        // Arrange
        var firstUser = new ClaimsPrincipal(new CaseSensitiveClaimsIdentity("initial"));
        var secondUser = new ClaimsPrincipal(new CaseSensitiveClaimsIdentity("updated"));
        var initialState = new TaskCompletionSource<AuthenticationState>(TaskCreationOptions.RunContinuationsAsynchronously);
        var pendingState = new TaskCompletionSource<AuthenticationState>(TaskCreationOptions.RunContinuationsAsynchronously);
        var provider = new TestAuthenticationStateProvider(firstUser, initialState.Task);
        using var services = new ServiceCollection().BuildServiceProvider();
        var consentHandler = new MicrosoftIdentityConsentAndConditionalAccessHandler(services);
        var handler = new MicrosoftIdentityServiceHandler(
            consentHandler, provider, new TestNavigationManager(), NullLogger<MicrosoftIdentityServiceHandler>.Instance);

        try
        {
            // Act
            var opening = handler.OnCircuitOpenedAsync(null!, CancellationToken.None);
            provider.ChangeUser(secondUser);
            if (newerChangePending)
            {
                provider.ChangeUser(pendingState.Task);
            }
            initialState.SetResult(new AuthenticationState(firstUser));
            await opening;

            // Assert
            Assert.Same(secondUser, consentHandler.User);
        }
        finally
        {
            await handler.OnCircuitClosedAsync(null!, CancellationToken.None);
            pendingState.SetResult(new AuthenticationState(firstUser));
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OnlyLatestAuthenticationChangeIsApplied(bool newerCompletesFirst)
    {
        // Arrange
        var firstUser = new ClaimsPrincipal(new CaseSensitiveClaimsIdentity("initial"));
        var olderUser = new ClaimsPrincipal(new CaseSensitiveClaimsIdentity("older"));
        var newerUser = new ClaimsPrincipal(new CaseSensitiveClaimsIdentity("newer"));
        var olderState = new TaskCompletionSource<AuthenticationState>();
        var newerState = new TaskCompletionSource<AuthenticationState>();
        var provider = new TestAuthenticationStateProvider(firstUser);
        using var services = new ServiceCollection().BuildServiceProvider();
        var consentHandler = new MicrosoftIdentityConsentAndConditionalAccessHandler(services);
        var handler = new MicrosoftIdentityServiceHandler(
            consentHandler, provider, new TestNavigationManager(), NullLogger<MicrosoftIdentityServiceHandler>.Instance);
        await handler.OnCircuitOpenedAsync(null!, CancellationToken.None);

        try
        {
            // Act
            provider.ChangeUser(olderState.Task);
            provider.ChangeUser(newerState.Task);
            await Task.Run(() =>
            {
                if (newerCompletesFirst)
                {
                    newerState.SetResult(new AuthenticationState(newerUser));
                }
                else
                {
                    olderState.SetResult(new AuthenticationState(olderUser));
                }
            });

            // Assert
            Assert.Same(newerCompletesFirst ? newerUser : firstUser, consentHandler.User);

            await Task.Run(() =>
            {
                if (newerCompletesFirst)
                {
                    olderState.SetResult(new AuthenticationState(olderUser));
                }
                else
                {
                    newerState.SetResult(new AuthenticationState(newerUser));
                }
            });
            Assert.Same(newerUser, consentHandler.User);
        }
        finally
        {
            await handler.OnCircuitClosedAsync(null!, CancellationToken.None);
        }
    }

    [Fact]
    public async Task PendingAuthenticationChangeDoesNotUpdateClosedCircuit()
    {
        // Arrange
        var firstUser = new ClaimsPrincipal(new CaseSensitiveClaimsIdentity("initial"));
        var secondUser = new ClaimsPrincipal(new CaseSensitiveClaimsIdentity("updated"));
        var changedState = new TaskCompletionSource<AuthenticationState>();
        var provider = new TestAuthenticationStateProvider(firstUser);
        using var services = new ServiceCollection().BuildServiceProvider();
        var consentHandler = new MicrosoftIdentityConsentAndConditionalAccessHandler(services);
        var handler = new MicrosoftIdentityServiceHandler(
            consentHandler, provider, new TestNavigationManager(), NullLogger<MicrosoftIdentityServiceHandler>.Instance);
        await handler.OnCircuitOpenedAsync(null!, CancellationToken.None);

        // Act
        provider.ChangeUser(changedState.Task);
        await handler.OnCircuitClosedAsync(null!, CancellationToken.None);
        await Task.Run(() => changedState.SetResult(new AuthenticationState(secondUser)));

        // Assert
        Assert.Same(firstUser, consentHandler.User);
        provider.ChangeUser(secondUser);
        Assert.Same(firstUser, consentHandler.User);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FailedAuthenticationChangeIsLoggedAndAllowsRecovery(bool canceled)
    {
        // Arrange
        var firstUser = new ClaimsPrincipal(new CaseSensitiveClaimsIdentity("initial"));
        var secondUser = new ClaimsPrincipal(new CaseSensitiveClaimsIdentity("recovered"));
        var changedState = new TaskCompletionSource<AuthenticationState>();
        var failure = new InvalidOperationException("Authentication state failed.");
        var provider = new TestAuthenticationStateProvider(firstUser);
        var logger = Substitute.For<ILogger<MicrosoftIdentityServiceHandler>>();
        using var services = new ServiceCollection().BuildServiceProvider();
        var consentHandler = new MicrosoftIdentityConsentAndConditionalAccessHandler(services);
        var handler = new MicrosoftIdentityServiceHandler(
            consentHandler, provider, new TestNavigationManager(), new LoggerMock<MicrosoftIdentityServiceHandler>(logger));
        await handler.OnCircuitOpenedAsync(null!, CancellationToken.None);

        try
        {
            // Act
            provider.ChangeUser(changedState.Task);
            await Task.Run(() =>
            {
                if (canceled)
                {
                    changedState.SetCanceled();
                }
                else
                {
                    changedState.SetException(failure);
                }
            });

            // Assert
            Assert.Same(firstUser, consentHandler.User);
            logger.Received(1).Log(
                LogLevel.Error,
                Arg.Any<EventId>(),
                Arg.Any<object>(),
                Arg.Is<Exception>(exception => canceled ? exception is TaskCanceledException : exception == failure),
                Arg.Any<Func<object, Exception?, string>>());

            provider.ChangeUser(secondUser);
            Assert.Same(secondUser, consentHandler.User);
        }
        finally
        {
            await handler.OnCircuitClosedAsync(null!, CancellationToken.None);
        }
    }

    [Fact]
    public async Task FailedInitialAuthenticationStateUnsubscribes()
    {
        // Arrange
        var firstUser = new ClaimsPrincipal(new CaseSensitiveClaimsIdentity("initial"));
        var secondUser = new ClaimsPrincipal(new CaseSensitiveClaimsIdentity("updated"));
        var failure = new InvalidOperationException("Initial authentication state failed.");
        var provider = new TestAuthenticationStateProvider(firstUser, Task.FromException<AuthenticationState>(failure));
        using var services = new ServiceCollection().BuildServiceProvider();
        var consentHandler = new MicrosoftIdentityConsentAndConditionalAccessHandler(services) { User = firstUser };
        var handler = new MicrosoftIdentityServiceHandler(
            consentHandler, provider, new TestNavigationManager(), NullLogger<MicrosoftIdentityServiceHandler>.Instance);

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.OnCircuitOpenedAsync(null!, CancellationToken.None));
        provider.ChangeUser(secondUser);

        // Assert
        Assert.Same(failure, exception);
        Assert.Same(firstUser, consentHandler.User);
    }

    private sealed class TestAuthenticationStateProvider(
        ClaimsPrincipal initialUser, Task<AuthenticationState>? initialState = null) : AuthenticationStateProvider
    {
        private ClaimsPrincipal _user = initialUser;

        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => initialState ?? Task.FromResult(new AuthenticationState(_user));

        public void ChangeUser(ClaimsPrincipal user)
        {
            _user = user;
            ChangeUser(Task.FromResult(new AuthenticationState(user)));
        }

        public void ChangeUser(Task<AuthenticationState> stateTask)
            => NotifyAuthenticationStateChanged(stateTask);
    }

    private sealed class TestNavigationManager : NavigationManager
    {
        public TestNavigationManager() => Initialize("https://localhost/", "https://localhost/");

        protected override void NavigateToCore(string uri, bool forceLoad)
            => throw new NotSupportedException();
    }
}
