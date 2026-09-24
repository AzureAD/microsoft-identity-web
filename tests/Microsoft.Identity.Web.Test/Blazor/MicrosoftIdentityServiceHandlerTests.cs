// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Microsoft.Identity.Web.Test.Blazor;

public class MicrosoftIdentityServiceHandlerTests
{
    [Fact]
    public async Task CircuitUpdatesUserWhenAuthenticationStateChanges()
    {
        var firstUser = new ClaimsPrincipal(new CaseSensitiveClaimsIdentity("test"));
        var secondUser = new ClaimsPrincipal(new CaseSensitiveClaimsIdentity("updated"));
        var provider = new TestAuthenticationStateProvider(firstUser);
        using var services = new ServiceCollection().BuildServiceProvider();
        var consentHandler = new MicrosoftIdentityConsentAndConditionalAccessHandler(services);
        var handler = new MicrosoftIdentityServiceHandler(
            consentHandler, provider, new TestNavigationManager(), NullLogger<MicrosoftIdentityServiceHandler>.Instance);

        await handler.OnCircuitOpenedAsync(null!, CancellationToken.None);
        Assert.Same(firstUser, consentHandler.User);

        provider.ChangeUser(secondUser);
        Assert.Same(secondUser, consentHandler.User);

        await handler.OnCircuitClosedAsync(null!, CancellationToken.None);
        provider.ChangeUser(firstUser);
        Assert.Same(secondUser, consentHandler.User);
    }

    private sealed class TestAuthenticationStateProvider(ClaimsPrincipal initialUser) : AuthenticationStateProvider
    {
        private ClaimsPrincipal _user = initialUser;

        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(_user));

        public void ChangeUser(ClaimsPrincipal user)
        {
            _user = user;
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(user)));
        }
    }

    private sealed class TestNavigationManager : NavigationManager
    {
        public TestNavigationManager() => Initialize("https://localhost/", "https://localhost/");

        protected override void NavigateToCore(string uri, bool forceLoad)
            => throw new NotSupportedException();
    }
}
