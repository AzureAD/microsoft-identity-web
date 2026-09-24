// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Microsoft.Identity.Web;

/// <summary>
/// Extension methods for mapping login and logout endpoints that support
/// incremental consent and Conditional Access scenarios.
/// </summary>
/// <remarks>
/// These extension methods are designed for Blazor Server scenarios to provide
/// dedicated login and logout endpoints with support for incremental consent
/// and Conditional Access. The login endpoint accepts query parameters for scopes,
/// loginHint, domainHint, and claims to enable advanced authentication scenarios.
/// Use in conjunction with <see cref="BlazorAuthenticationChallengeHandler"/> for
/// a complete authentication solution in Blazor Server applications.
/// </remarks>
public static class LoginLogoutEndpointRouteBuilderExtensions
{
    /// <summary>
    /// Maps login and logout endpoints under the current route group.
    /// The login endpoint supports incremental consent via scope, loginHint, domainHint, and claims parameters.
    /// Logout uses the framework's antiforgery verdict when available, falling back to token
    /// validation when antiforgery services are registered without middleware.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <returns>The endpoint convention builder for further configuration.</returns>
    [RequiresUnreferencedCode("Minimal APIs perform reflection on delegate types which may be trimmed if not directly referenced.")]
    [RequiresDynamicCode("Minimal APIs require dynamic code generation for delegate binding.")]
    public static IEndpointConventionBuilder MapLoginAndLogout(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("");

        var hasAutomaticCsrfProtection = HasAutomaticCsrfProtection(endpoints.ServiceProvider);
        WarnIfAntiforgeryMissing(endpoints.ServiceProvider, hasAutomaticCsrfProtection);

        // Enhanced login endpoint that supports incremental consent and Conditional Access
        group.MapGet("/login", (
            string? returnUrl,
            string? scope,
            string? loginHint,
            string? domainHint,
            string? claims) =>
        {
            var properties = GetAuthProperties(returnUrl);

            // Add scopes if provided (for incremental consent)
            if (!string.IsNullOrEmpty(scope))
            {
                var scopes = scope.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                properties.SetParameter(OpenIdConnectParameterNames.Scope, scopes);
            }

            // Add login hint (pre-fills username)
            if (!string.IsNullOrEmpty(loginHint))
            {
                properties.SetParameter(OpenIdConnectParameterNames.LoginHint, loginHint);
            }

            // Add domain hint (skips home realm discovery)
            if (!string.IsNullOrEmpty(domainHint))
            {
                properties.SetParameter(OpenIdConnectParameterNames.DomainHint, domainHint);
            }

            // Add claims challenge (for Conditional Access / step-up auth)
            if (!string.IsNullOrEmpty(claims))
            {
                properties.Items["claims"] = claims;
            }

            return TypedResults.Challenge(properties, [OpenIdConnectDefaults.AuthenticationScheme]);
        })
        .AllowAnonymous();

        var logout = group.MapPost("/logout", async (HttpContext context) =>
        {
            var validation = context.Features.Get<IAntiforgeryValidationFeature>();
            if (validation is not null)
            {
                if (!validation.IsValid)
                {
                    return Results.BadRequest();
                }
            }
            else
            {
                // MVC hosts can register antiforgery services without running antiforgery
                // middleware. Preserve token validation when no middleware recorded a verdict.
                var antiforgery = context.RequestServices.GetService<IAntiforgery>();
                if (antiforgery is not null && !await antiforgery.IsRequestValidAsync(context))
                {
                    return Results.BadRequest();
                }
            }

            string? returnUrl = null;
            if (context.Request.HasFormContentType)
            {
                var form = await context.Request.ReadFormAsync();
                returnUrl = form["ReturnUrl"];
            }

            return (IResult)TypedResults.SignOut(GetAuthProperties(returnUrl),
                [CookieAuthenticationDefaults.AuthenticationScheme, OpenIdConnectDefaults.AuthenticationScheme]);
        })
        .RequireAuthorization();

        if (hasAutomaticCsrfProtection)
        {
            logout.WithMetadata(new RequireAntiforgeryTokenAttribute());
        }

        return group;
    }

    // Warn only when neither token-based antiforgery nor automatic CSRF protection is available.
    private static bool HasAutomaticCsrfProtection(IServiceProvider? serviceProvider)
    {
        // The .NET 11 WebApplication builder registers this service when it can inject
        // automatic CSRF middleware. Earlier runtimes and legacy hosts do not.
        var csrfProtectionType = Type.GetType("Microsoft.AspNetCore.Antiforgery.ICsrfProtection, Microsoft.AspNetCore.Http.Abstractions");
        var csrfSetting = serviceProvider?.GetService<IConfiguration>()?["DisableCsrfProtection"];
        return csrfProtectionType is not null
            && serviceProvider?.GetService<IServiceProviderIsService>()?.IsService(csrfProtectionType) is true
            && !string.Equals(csrfSetting, "true", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(csrfSetting, "1", StringComparison.Ordinal);
    }

    private static void WarnIfAntiforgeryMissing(IServiceProvider? serviceProvider, bool hasAutomaticCsrfProtection)
    {
        var isService = serviceProvider?.GetService<IServiceProviderIsService>();
        var antiforgeryRegistered = isService?.IsService(typeof(IAntiforgery)) ?? false;
        if (antiforgeryRegistered || hasAutomaticCsrfProtection)
        {
            return;
        }

        var loggerFactory = serviceProvider?.GetService<ILoggerFactory>();
        var logger = loggerFactory?.CreateLogger(typeof(LoginLogoutEndpointRouteBuilderExtensions).FullName!);
        logger?.LogWarning(
            new EventId(1, "AntiforgeryNotRegistered"),
            "MapLoginAndLogout was called but IAntiforgery is not registered in DI. The /logout " +
            "endpoint will rely on RequireAuthorization and SameSite=Lax cookies as its CSRF gate. " +
            "To enable antiforgery token validation, call services.AddAntiforgery() (and, for " +
            "minimal APIs, app.UseAntiforgery()).");
    }

    /// <summary>
    /// Builds <see cref="AuthenticationProperties"/> with a strictly-local <c>RedirectUri</c>.
    /// Any non-local input (absolute URL, protocol-relative "//host", slash-backslash "/\host",
    /// or anything not starting with a single '/') is coerced to "/". This matches the
    /// semantics of <see cref="Microsoft.AspNetCore.Mvc.IUrlHelper.IsLocalUrl"/> and prevents
    /// open-redirect attacks via the ReturnUrl query/form parameter.
    /// </summary>
    internal static AuthenticationProperties GetAuthProperties(string? returnUrl)
    {
        const string pathBase = "/";
        return new AuthenticationProperties { RedirectUri = RedirectUriHelper.IsLocalUrl(returnUrl) ? returnUrl! : pathBase };
    }
}
