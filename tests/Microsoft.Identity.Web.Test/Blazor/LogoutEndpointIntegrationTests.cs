// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Microsoft.Identity.Web.Test.Blazor
{
    /// <summary>
    /// End-to-end tests for the <c>/logout</c> endpoint mapped by
    /// <see cref="LoginLogoutEndpointRouteBuilderExtensions.MapLoginAndLogout"/>
    /// validating the defense-in-depth gating through a real ASP.NET Core pipeline:
    /// <list type="bullet">
    /// <item>RequireAuthorization must reject unauthenticated POSTs (401).</item>
    /// <item>Antiforgery must reject authenticated POSTs that lack a valid token (400).</item>
    /// <item>Authenticated POSTs with a valid token must succeed.</item>
    /// </list>
    /// This closes the MSRC CSRF-on-logout class by exercising the full middleware chain
    /// (routing, authentication, authorization, antiforgery, endpoint execution), not
    /// just the isolated <c>GetAuthProperties</c> helper.
    /// </summary>
    public class LogoutEndpointIntegrationTests
    {
        private const string TestScheme = "Test";
        private const string AntiforgeryFormFieldName = "__RequestVerificationToken";

        private static TestServer CreateServer(AuthState authState, bool wireAntiforgery = true)
            => CreateServer(authState, addAntiforgeryServices: wireAntiforgery, useAntiforgeryMiddleware: wireAntiforgery);

        private static TestServer CreateServer(
            AuthState authState,
            bool addAntiforgeryServices,
            bool useAntiforgeryMiddleware)
        {
#pragma warning disable ASPDEPR004 // WebHostBuilder is deprecated — acceptable for TestServer-based test hosts.
            var builder = new WebHostBuilder()
                .ConfigureServices(services =>
                {
                    services.AddSingleton(authState);
                    services.AddRouting();
                    if (addAntiforgeryServices)
                    {
                        services.AddAntiforgery(options =>
                        {
                            options.FormFieldName = AntiforgeryFormFieldName;
                        });
                    }

                    // Register the two schemes the endpoint signs out of with a single stub
                    // that handles both authentication and sign-out. Aliasing the default scheme
                    // to the Cookie scheme name lets RequireAuthorization discover our principal.
                    services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                        .AddScheme<AuthenticationSchemeOptions, StubCookieAndOidcHandler>(
                            CookieAuthenticationDefaults.AuthenticationScheme, _ => { })
                        .AddScheme<AuthenticationSchemeOptions, StubCookieAndOidcHandler>(
                            OpenIdConnectDefaults.AuthenticationScheme, _ => { });

                    services.AddAuthorization();
                })
                .Configure(app =>
                {
                    app.UseRouting();
                    app.UseAuthentication();
                    app.UseAuthorization();
                    if (useAntiforgeryMiddleware)
                    {
                        app.UseAntiforgery();
                    }

#pragma warning disable ASP0014 // UseEndpoints is intentional for the test host
                    app.UseEndpoints(endpoints =>
                    {
                        if (addAntiforgeryServices)
                        {
                            // Test-only: surface an antiforgery token pair to the client so the
                            // "valid token" test can build a legitimate POST. Only mapped when
                            // antiforgery services are registered — otherwise minimal-API parameter
                            // inference fails because IAntiforgery isn't in DI.
                            endpoints.MapGet("/_testing/antiforgery-token", (HttpContext ctx, IAntiforgery af) =>
                            {
                                var tokens = af.GetAndStoreTokens(ctx);
                                return Results.Text(tokens.RequestToken ?? string.Empty);
                            });
                        }

                        endpoints.MapLoginAndLogout();
                    });
#pragma warning restore ASP0014
                });
#pragma warning disable ASPDEPR008 // TestServer(IWebHostBuilder) is deprecated — still the canonical net8/9/10 test-host ctor.
            return new TestServer(builder);
#pragma warning restore ASPDEPR008
#pragma warning restore ASPDEPR004
        }

        private static async Task<(string Token, string Cookie)> GetAntiforgeryTokenAndCookieAsync(HttpClient client)
        {
            using var response = await client.GetAsync("/_testing/antiforgery-token");
            response.EnsureSuccessStatusCode();
            var token = (await response.Content.ReadAsStringAsync()).Trim();

            // TestServer's default HttpClient does not track cookies; we extract the
            // antiforgery cookie from Set-Cookie and replay it on the POST.
            var setCookieValues = response.Headers.TryGetValues("Set-Cookie", out var values)
                ? values
                : Array.Empty<string>();
            string? antiforgeryCookie = null;
            foreach (var setCookie in setCookieValues)
            {
                if (setCookie.Contains(".AspNetCore.Antiforgery.", StringComparison.Ordinal))
                {
                    // Reduce "name=value; path=/; ..." to just "name=value" for the Cookie header.
                    var semicolonIndex = setCookie.IndexOf(';', StringComparison.Ordinal);
                    antiforgeryCookie = semicolonIndex >= 0 ? setCookie.Substring(0, semicolonIndex) : setCookie;
                    break;
                }
            }

            Assert.NotNull(antiforgeryCookie);
            return (token, antiforgeryCookie!);
        }

        // ─── F3 blocker test 1: RequireAuthorization gate ────────────────────────────

        /// <summary>
        /// Unauthenticated POST to /logout must be rejected by <c>RequireAuthorization()</c>
        /// with <c>401 Unauthorized</c> before any endpoint logic or antiforgery check runs.
        /// This is the first defense-in-depth layer closing the MSRC CSRF-on-logout class.
        /// </summary>
        [Fact]
        public async Task Logout_Unauthenticated_Returns401()
        {
            using var server = CreateServer(new AuthState { Authenticated = false });
            using var client = server.CreateClient();

            using var response = await client.PostAsync(
                "/logout",
                new FormUrlEncodedContent(new Dictionary<string, string>()));

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        // ─── F3 blocker test 2: Antiforgery truth probe ──────────────────────────────

        /// <summary>
        /// MSRC truth probe. Authenticated POST to /logout with form content type but no
        /// antiforgery token must be rejected with <c>400 Bad Request</c>. If this test
        /// fails (e.g. returns 200/302), antiforgery middleware is not actually firing on
        /// the endpoint — which would mean the SameSite=None consumer protection
        /// rests solely on <c>RequireAuthorization()</c> and the hybrid defense-in-depth
        /// argument collapses. That would require adding <c>.RequireAntiforgery()</c>
        /// explicitly on the MapPost endpoint.
        /// </summary>
        [Fact]
        public async Task Logout_AuthenticatedWithoutAntiforgeryToken_Returns400()
        {
            using var server = CreateServer(new AuthState { Authenticated = true });
            using var client = server.CreateClient();

            using var response = await client.PostAsync(
                "/logout",
                new FormUrlEncodedContent(new Dictionary<string, string>()));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        // ─── Happy path: authenticated + valid antiforgery token → success ───────────

        /// <summary>
        /// Authenticated POST to /logout carrying a valid antiforgery cookie + form field
        /// pair must succeed. Exercises the full path through the middleware chain and
        /// into the endpoint's <c>SignOut</c> call.
        /// </summary>
        [Fact]
        public async Task Logout_AuthenticatedWithValidToken_Succeeds()
        {
            using var server = CreateServer(new AuthState { Authenticated = true });
            using var client = server.CreateClient();

            var (token, antiforgeryCookie) = await GetAntiforgeryTokenAndCookieAsync(client);

            using var request = new HttpRequestMessage(HttpMethod.Post, "/logout")
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    [AntiforgeryFormFieldName] = token,
                    ["ReturnUrl"] = "/",
                }),
            };
            request.Headers.Add("Cookie", antiforgeryCookie);

            using var response = await client.SendAsync(request);

            // SignOut returns 200 OK when the sign-out handlers don't issue a redirect.
            // The stub handler is a no-op, so we expect a non-error response.
            Assert.True(
                (int)response.StatusCode < 400,
                $"Expected success, got {(int)response.StatusCode} {response.StatusCode}");
        }

        // ─── Graceful-degradation tests (Opus 4.7 ship-readiness Q4 additions) ───────

        /// <summary>
        /// Graceful-degradation contract. When a host reuses <c>MapLoginAndLogout</c> without
        /// wiring <c>AddAntiforgery()</c>/<c>UseAntiforgery()</c> (non-Blazor hosts, custom
        /// pipelines), the endpoint must not regress: the <c>IAntiforgeryValidationFeature</c>
        /// is never attached, the explicit feature check in the handler is a no-op, and
        /// <c>RequireAuthorization()</c> remains the primary CSRF gate (backed by cookie
        /// <c>SameSite=Lax</c> semantics). An authenticated POST with no antiforgery token
        /// must therefore succeed, not 400, not 500. This locks the claim made in the 4.8.0
        /// changelog that the F3 hardening is zero-impact for non-Blazor consumers.
        /// </summary>
        [Fact]
        public async Task Logout_WithoutAntiforgeryWired_AuthenticatedUser_Succeeds()
        {
            using var server = CreateServer(new AuthState { Authenticated = true }, wireAntiforgery: false);
            using var client = server.CreateClient();

            using var response = await client.PostAsync(
                "/logout",
                new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["ReturnUrl"] = "/",
                }));

            Assert.True(
                (int)response.StatusCode < 400,
                $"Expected success (no antiforgery wired → graceful degradation), got {(int)response.StatusCode} {response.StatusCode}");
        }

        /// <summary>
        /// Validation-failure contract. With antiforgery wired but the submitted token
        /// malformed, the <c>AntiforgeryMiddleware</c> catches the <c>AntiforgeryValidationException</c>
        /// and sets <c>IAntiforgeryValidationFeature.IsValid = false</c> without short-circuiting.
        /// The endpoint's explicit feature check must observe this and return <c>400 Bad Request</c>
        /// <em>before</em> any form read. If the explicit check were missing, <c>FormFeature</c>
        /// (via <c>HasFormContentType</c>/<c>ReadFormAsync</c>) would throw
        /// <c>InvalidOperationException</c> from <c>HandleUncheckedAntiforgeryValidationFeature</c>,
        /// bubbling as 500. Asserting 400 (not 500) here is the evidence that the explicit
        /// feature check — rather than <c>[FromForm]</c>-driven form binding — is the correct
        /// gate, and backs the rationale in the PR description.
        /// </summary>
        [Fact]
        public async Task Logout_WithAntiforgeryWired_ValidationFails_Returns400NotTheFormFeature500()
        {
            using var server = CreateServer(new AuthState { Authenticated = true });
            using var client = server.CreateClient();            using var request = new HttpRequestMessage(HttpMethod.Post, "/logout")
            {
                // Intentionally malformed token: present but not a valid antiforgery value,
                // and no matching cookie. This forces AntiforgeryMiddleware to call
                // ValidateRequestAsync, catch AntiforgeryValidationException, and set
                // IAntiforgeryValidationFeature.IsValid = false.
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    [AntiforgeryFormFieldName] = "this-is-not-a-valid-antiforgery-token",
                    ["ReturnUrl"] = "/",
                }),
            };

            using var response = await client.SendAsync(request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.NotEqual(HttpStatusCode.InternalServerError, response.StatusCode);
        }

        /// <summary>
        /// MVC-shape regression guard. A common production configuration is to call
        /// <c>AddControllersWithViews()</c> / <c>AddRazorPages()</c> / <c>AddMvc()</c>
        /// (all of which transitively register <c>IAntiforgery</c> in DI) but <em>not</em>
        /// <c>app.UseAntiforgery()</c>, because those stacks validate tokens at filter time
        /// rather than through middleware. An earlier iteration of this MSRC fix coupled
        /// validation to <c>IAntiforgeryMetadata</c>, which triggered ASP.NET Core's startup
        /// verifier — "endpoint contains anti-forgery metadata, but a middleware was not
        /// found" — breaking every MVC host that reused <c>MapLoginAndLogout</c>. This test
        /// simulates that shape (services present, middleware absent) and asserts both:
        /// (a) the app starts without throwing, and (b) a missing / invalid token is rejected
        /// as <c>400 Bad Request</c> via the in-handler <c>IAntiforgery.IsRequestValidAsync</c>
        /// call — not as a 500 from <c>FormFeature</c> and not as a silent 200. This locks
        /// in the pipeline-shape-independent semantics promised by the 4.8.0 changelog.
        /// </summary>
        [Fact]
        public async Task Logout_AntiforgeryServicesRegistered_ButMiddlewareMissing_StillValidates()
        {
            using var server = CreateServer(
                new AuthState { Authenticated = true },
                addAntiforgeryServices: true,
                useAntiforgeryMiddleware: false);
            using var client = server.CreateClient();

            using var request = new HttpRequestMessage(HttpMethod.Post, "/logout")
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["ReturnUrl"] = "/",
                }),
            };

            using var response = await client.SendAsync(request);

            // No valid antiforgery token supplied → handler's explicit IsRequestValidAsync
            // call returns false → 400. Critically: not a startup throw, not a 500 from
            // form binding, not a silent 200 CSRF bypass.
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        // ─── Helpers ─────────────────────────────────────────────────────────────────

        internal sealed class AuthState
        {
            public bool Authenticated { get; set; }
        }

        /// <summary>
        /// Minimal test authentication + sign-out handler. Registered under both
        /// <see cref="CookieAuthenticationDefaults.AuthenticationScheme"/> and
        /// <see cref="OpenIdConnectDefaults.AuthenticationScheme"/> so the endpoint's
        /// <c>SignOut(..., [Cookies, OIDC])</c> call has registered handlers to dispatch
        /// to. <c>HandleAuthenticateAsync</c> consults the per-server <see cref="AuthState"/>
        /// so individual tests can toggle the authenticated state independently.
        /// </summary>
        private sealed class StubCookieAndOidcHandler
            : AuthenticationHandler<AuthenticationSchemeOptions>, IAuthenticationSignOutHandler
        {
            public StubCookieAndOidcHandler(
                IOptionsMonitor<AuthenticationSchemeOptions> options,
                ILoggerFactory logger,
                UrlEncoder encoder)
                : base(options, logger, encoder)
            {
            }

            protected override Task<AuthenticateResult> HandleAuthenticateAsync()
            {
                var state = Context.RequestServices.GetRequiredService<AuthState>();
                if (!state.Authenticated)
                {
                    return Task.FromResult(AuthenticateResult.NoResult());
                }

                var identity = new CaseSensitiveClaimsIdentity(
                    new[] { new Claim(ClaimTypes.NameIdentifier, "test-user") },
                    Scheme.Name);
                var principal = new ClaimsPrincipal(identity);
                var ticket = new AuthenticationTicket(principal, Scheme.Name);
                return Task.FromResult(AuthenticateResult.Success(ticket));
            }

            public Task SignOutAsync(AuthenticationProperties? properties)
            {
                // No-op: confirm sign-out was dispatched without performing any real work
                // (no cookie clearing, no end-session redirect). The test asserts on HTTP
                // status, not on side effects.
                return Task.CompletedTask;
            }
        }
    }
}
