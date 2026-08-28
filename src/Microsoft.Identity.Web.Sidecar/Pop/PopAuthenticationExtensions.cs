// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Identity.Web.Sidecar.Pop;

/// <summary>
/// SPIKE (throwaway): wires inbound SHR PoP into the sidecar's authentication pipeline WITHOUT
/// changing the Bearer path or the global default authentication scheme.
///
/// It registers a second authentication handler under the "PoP" scheme and a named authorization
/// policy (<see cref="PopConstants.ValidatePolicyName"/>) that accepts BOTH "Bearer" and "PoP". Only
/// the <c>/Validate</c> endpoint opts into that policy, so every other endpoint - and the default
/// scheme - remains exactly as before. For a Bearer request the PoP handler no-ops (wrong scheme) and
/// JwtBearer validates as usual; for a PoP request the JwtBearer handler no-ops and the PoP handler
/// runs the re-hosted MISE SHR validation.
/// </summary>
internal static class PopAuthenticationExtensions
{
    public static AuthenticationBuilder AddInboundShrPop(this AuthenticationBuilder builder)
    {
        builder.Services.AddSingleton<ShrPopValidationService>();

        builder.AddScheme<PopAuthenticationSchemeOptions, PopAuthenticationHandler>(
            PopConstants.SchemeName,
            static _ => { });

        // Scope PoP to /Validate via a policy that authenticates Bearer OR PoP. The global default
        // authentication scheme is intentionally left as "Bearer" so nothing else changes.
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
