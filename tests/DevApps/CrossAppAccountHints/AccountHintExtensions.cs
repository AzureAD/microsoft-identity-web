// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace CrossAppAccountHints;

internal static class AccountHintExtensions
{
    private const string AutomaticAttempt = "CrossAppAccountHints.Automatic";

    public static AuthenticationBuilder AddCrossAppAccountHints(
        this AuthenticationBuilder builder,
        string cookieScheme,
        string oidcScheme,
        AccountHintSettings settings)
    {
        builder.Services.AddSingleton(settings);
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddSingleton<AccountHintStore>();
        builder.Services.Configure<CookieAuthenticationOptions>(cookieScheme, options =>
        {
            var signedIn = options.Events.OnSignedIn;
            options.Events.OnSignedIn = async context =>
            {
                await signedIn(context);
                var store = context.HttpContext.RequestServices.GetRequiredService<AccountHintStore>();
                store.Remember(context.HttpContext, context.Principal!);
                store.ClearLocalMarkers(context.HttpContext);
            };

            var signingOut = options.Events.OnSigningOut;
            options.Events.OnSigningOut = async context =>
            {
                await signingOut(context);
                context.HttpContext.RequestServices.GetRequiredService<AccountHintStore>().Suppress(context.HttpContext);
            };
        });

        builder.Services.Configure<OpenIdConnectOptions>(oidcScheme, options =>
        {
            var redirect = options.Events.OnRedirectToIdentityProvider;
            options.Events.OnRedirectToIdentityProvider = async context =>
            {
                if (context.Properties.Items.ContainsKey(AutomaticAttempt))
                {
                    context.ProtocolMessage.Prompt = OpenIdConnectPrompt.None;
                    context.ProtocolMessage.LoginHint =
                        context.Properties.GetParameter<string>(OpenIdConnectParameterNames.LoginHint);
                }

                await redirect(context);
            };

            var remoteFailure = options.Events.OnRemoteFailure;
            options.Events.OnRemoteFailure = async context =>
            {
                if (context.Properties?.Items.ContainsKey(AutomaticAttempt) == true &&
                    context.Failure is OpenIdConnectProtocolException protocolException &&
                    protocolException.Data["error"] is string error &&
                    error is "login_required" or "interaction_required" or "consent_required" or "account_selection_required")
                {
                    context.HandleResponse();
                    context.Response.Redirect("/?sso=interaction-required");
                    return;
                }

                await remoteFailure(context);
            };
        });

        return builder;
    }

    public static async Task<bool> TryAutomaticSignInAsync(this HttpContext context, string oidcScheme)
    {
        var store = context.RequestServices.GetRequiredService<AccountHintStore>();
        if (context.User.Identity?.IsAuthenticated == true ||
            context.Request.Cookies.ContainsKey(store.SuppressionCookieName) ||
            context.Request.Cookies.ContainsKey(store.AttemptCookieName))
        {
            return false;
        }

        AccountHint? hint = store.Read(context.Request).FirstOrDefault();
        if (hint is null)
        {
            return false;
        }

        store.MarkAttempt(context);
        var properties = new AuthenticationProperties { RedirectUri = "/" };
        properties.Items[AutomaticAttempt] = "true";
        properties.SetParameter(OpenIdConnectParameterNames.LoginHint, hint.LoginHint);
        await context.ChallengeAsync(oidcScheme, properties);
        return true;
    }
}
