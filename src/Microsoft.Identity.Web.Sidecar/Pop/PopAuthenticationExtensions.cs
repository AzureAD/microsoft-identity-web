// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Identity.Web.Sidecar.Pop;

/// <summary>
/// Wires inbound SHR PoP into the sidecar's authentication pipeline without changing the Bearer scheme
/// or the global default authentication scheme.
/// </summary>
/// <remarks>
/// Registers a second authentication handler under the "PoP" scheme and a named authorization policy
/// (<see cref="PopConstants.ValidatePolicyName"/>) that accepts both "Bearer" and "PoP". Only the
/// <c>/Validate</c> endpoint opts into that policy, so every other endpoint - and the default scheme -
/// is unchanged. A Bearer request is validated by JwtBearer while the PoP handler no-ops; a PoP request
/// is validated by the PoP handler while JwtBearer no-ops.
/// </remarks>
internal static class PopAuthenticationExtensions
{
    public static AuthenticationBuilder AddInboundShrPop(this AuthenticationBuilder builder)
    {
        builder.Services.AddSingleton<ShrPopValidationService>();

        builder.AddScheme<PopAuthenticationSchemeOptions, PopAuthenticationHandler>(
            PopConstants.SchemeName,
            static _ => { });

        // Accept both Bearer and PoP on /Validate. The global default scheme stays "Bearer".
        builder.Services
            .AddAuthorizationBuilder()
            .AddPolicy(PopConstants.ValidatePolicyName, policy =>
            {
                policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme, PopConstants.SchemeName);
                policy.RequireAuthenticatedUser();
            });

        return builder;
    }
}
