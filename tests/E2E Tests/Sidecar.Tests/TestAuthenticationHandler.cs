// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Sidecar.Tests;

// Test Authentication Handler for mocking authentication
public class TestAuthenticationSchemeOptions : AuthenticationSchemeOptions { }

public class TestAuthenticationHandler : AuthenticationHandler<TestAuthenticationSchemeOptions>
{
    public static void AddAlwaysSucceedTestAuthentication(IServiceCollection services)
    {
        services.AddAuthentication("Test")
            .AddScheme<TestAuthenticationSchemeOptions, TestAuthenticationHandler>(
                "Test", options => { });
    }

    public TestAuthenticationHandler(IOptionsMonitor<TestAuthenticationSchemeOptions> options,
        ILoggerFactory logger, System.Text.Encodings.Web.UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "TestUser"),
            new Claim(ClaimTypes.NameIdentifier, "123"),
        };

        var identity = new Microsoft.IdentityModel.Tokens.CaseSensitiveClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Test");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
