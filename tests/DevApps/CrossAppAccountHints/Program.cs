// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Encodings.Web;
using CrossAppAccountHints;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Validators;

var builder = WebApplication.CreateBuilder(args);
var settings = builder.Configuration.GetSection("AccountHints").Get<AccountHintSettings>() ?? new();
if (settings.AppId is not ("A" or "B"))
{
    throw new InvalidOperationException("AccountHints:AppId must be A or B.");
}

builder.Services.AddDataProtection().SetApplicationName($"CrossAppAccountHints.{settings.AppId}");
builder.Services.AddAntiforgery(options => options.Cookie.Name = $"IdWeb.Poc.{settings.AppId}.Antiforgery");
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = $"IdWeb.Poc.{settings.AppId}.Auth";
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.ExpireTimeSpan = TimeSpan.FromHours(1);
    })
    .AddOpenIdConnect(options =>
    {
        IConfigurationSection azureAd = builder.Configuration.GetSection("AzureAd");
        options.Authority = $"{azureAd["Instance"]!.TrimEnd('/')}/{azureAd["TenantId"]}/v2.0";
        options.ClientId = azureAd["ClientId"]!;
        options.CallbackPath = azureAd["CallbackPath"] ?? "/signin-oidc";
        // Sign-in-only POC: validate the front-channel ID token; discard the code and never call /token.
        options.ResponseType = OpenIdConnectResponseType.CodeIdToken;
        options.ResponseMode = OpenIdConnectResponseMode.FormPost;
        options.Events.OnAuthorizationCodeReceived = context =>
        {
            // The handler has validated the hybrid response and still requires an ID token when redemption is skipped.
            context.HandleCodeRedemption(new OpenIdConnectMessage { IdToken = context.ProtocolMessage.IdToken });
            return Task.CompletedTask;
        };
        options.UsePkce = false;
        options.SaveTokens = false;
        options.MapInboundClaims = false;
        options.TokenValidationParameters.NameClaimType = "name";
        options.TokenValidationParameters.IssuerValidator =
            AadIssuerValidator.GetAadIssuerValidator(options.Authority).Validate;
        options.Scope.Clear();
        options.Scope.Add("openid");
        options.Scope.Add("profile");
        options.CorrelationCookie.Name = $"IdWeb.Poc.{settings.AppId}.Correlation.";
        options.NonceCookie.Name = $"IdWeb.Poc.{settings.AppId}.Nonce.";
        options.PushedAuthorizationBehavior = PushedAuthorizationBehavior.Disable;
    })
    .AddCrossAppAccountHints(CookieAuthenticationDefaults.AuthenticationScheme,
        OpenIdConnectDefaults.AuthenticationScheme, settings);
builder.Services.AddAuthorization();

var app = builder.Build();
app.Use(async (context, next) =>
{
    context.Response.Headers.CacheControl = "no-store";
    context.Response.Headers.ContentSecurityPolicy = "default-src 'none'; form-action 'self'; frame-ancestors 'none'";
    context.Response.Headers.XContentTypeOptions = "nosniff";
    await next(context);
});
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", async (HttpContext context, AccountHintStore store, IAntiforgery antiforgery) =>
{
    if (await context.TryAutomaticSignInAsync(OpenIdConnectDefaults.AuthenticationScheme))
    {
        return Results.Empty;
    }

    string Encode(string? value) => HtmlEncoder.Default.Encode(value ?? "");
    bool authenticated = context.User.Identity?.IsAuthenticated == true;
    AntiforgeryTokenSet tokens = antiforgery.GetAndStoreTokens(context);
    string token = $"<input type=\"hidden\" name=\"{Encode(tokens.FormFieldName)}\" value=\"{Encode(tokens.RequestToken)}\">";
    string hints = string.Join("", store.Read(context.Request).Select((hint, index) =>
        $"<li>{Encode(hint.DisplayName)}{(index == 0 ? " (preferred)" : "")}</li>"));
    string status = authenticated ? $"Authenticated as {Encode(context.User.Identity?.Name)}" : "Anonymous: no local application session";
    string html = $$"""
        <!doctype html>
        <html lang="en"><head><meta charset="utf-8"><title>Account hints - App {{settings.AppId}}</title></head>
        <body>
        <h1>Cross-app account hints: App {{settings.AppId}}</h1>
        <h2>{{status}}</h2>
        <p>Local cookie: IdWeb.Poc.{{settings.AppId}}.Auth.
        Shared hint: IdWeb.Poc.AccountHints. Hints never authenticate a request.</p>
        <p>Automatic SSO: {{(context.Request.Cookies.ContainsKey(store.SuppressionCookieName) ? "suppressed after local sign-out" :
            context.Request.Cookies.ContainsKey(store.AttemptCookieName) ? "already attempted; explicit sign-in is available" : "not attempted")}}.</p>
        <p>{{(context.Request.Query["sso"] == "interaction-required" ? "Entra requires interaction. Choose Sign in to continue." : "")}}</p>
        <h2>Shared account hints (untrusted; most recently signed in first)</h2>
        <ul>{{hints}}</ul>
        <p><a href="/signin">Sign in</a> | <a href="/switch-account">Choose another account</a></p>
        <form method="post" action="/signout">{{token}}<button>Sign out of this app only</button></form>
        <form method="post" action="/forget-hints">{{token}}<button>Forget shared hints</button></form>
        <h2>Try it</h2>
        <ol>
        <li>Sign into A at https://localhost:5001, then open B at https://localhost:5002 in the same browser profile.
        B has no local session; it sends a top-level OIDC request with prompt=none and the shared login_hint.</li>
        <li>After Entra validates that sign-in, B creates its own cookie. No /savedusers, Graph, MSAL or token cache is involved.</li>
        <li>Sign out of B. A remains signed in; B will not immediately sign itself back in.</li>
        <li>Choose another account to update the shared preference. An existing app session is not silently switched.</li>
        </ol>
        <h2>POC boundaries</h2>
        <p>Localhost cookies cross ports, so two localhost ports simulate hint sharing, not separate subdomains.
        Auth cookies have different names and app-specific Data Protection purposes.
        For real sibling hosts, configure AccountHints:CookieDomain to their shared parent domain,
        plus HTTPS certificates, routing and registered redirect URIs.</p>
        <p>This uses a dedicated development registration supporting work/school accounts from any Entra tenant
        and personal Microsoft accounts, through the common authority. Hybrid ID tokens and both localhost callbacks
        are registered. No client credentials are required; issuer validation remains enabled.</p>
        <p>The hint cookie contains account information, is readable/replaceable by participating hosts,
        and is not an authoritative list of Entra sessions. No cross-app signing keys are shared by this feature.
        Production needs a reviewed privacy, integrity, account-selection, expiry and logout contract.
        Do not deploy this POC as a production authentication solution.</p>
        </body></html>
        """;
    return Results.Content(html, "text/html");
});

app.MapGet("/signin", async (HttpContext context, AccountHintStore store) =>
{
    store.ClearLocalMarkers(context);
    await context.ChallengeAsync(OpenIdConnectDefaults.AuthenticationScheme,
        new AuthenticationProperties { RedirectUri = "/" });
});

app.MapGet("/switch-account", async (HttpContext context, AccountHintStore store) =>
{
    store.ClearLocalMarkers(context);
    var properties = new OpenIdConnectChallengeProperties { RedirectUri = "/", Prompt = OpenIdConnectPrompt.SelectAccount };
    await context.ChallengeAsync(OpenIdConnectDefaults.AuthenticationScheme, properties);
});

app.MapPost("/signout", async (HttpContext context, IAntiforgery antiforgery) =>
{
    if (!await antiforgery.IsRequestValidAsync(context))
    {
        return Results.BadRequest();
    }

    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/");
});

app.MapPost("/forget-hints", async (HttpContext context, AccountHintStore store, IAntiforgery antiforgery) =>
{
    if (!await antiforgery.IsRequestValidAsync(context))
    {
        return Results.BadRequest();
    }

    store.Forget(context);
    return Results.Redirect("/");
});

app.Run();

public partial class Program;
