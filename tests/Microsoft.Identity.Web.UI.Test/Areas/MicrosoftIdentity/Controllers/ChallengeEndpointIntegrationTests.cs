// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net;
using System.Net.Http;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Web.UI;
using Xunit;

namespace Microsoft.Identity.Web.UI.Test.Areas.MicrosoftIdentity.Controllers
{
    /// <summary>
    /// End-to-end tests for <c>/MicrosoftIdentity/Account/Challenge</c> validating the
    /// same-origin coercion and open-redirect rejection through a real ASP.NET Core
    /// pipeline. The production authentication handler is replaced by
    /// <see cref="CapturingChallengeHandler"/>, which short-circuits the challenge
    /// into a 200 response and echoes <see cref="AuthenticationProperties.RedirectUri"/>
    /// via the <c>X-Captured-RedirectUri</c> header so the test can assert what the
    /// controller handed to the authentication layer.
    /// </summary>
    public class ChallengeEndpointIntegrationTests
    {
        private const string TestScheme = "OpenIdConnect";
        private const string CapturedRedirectHeader = "X-Captured-RedirectUri";

        private static TestServer CreateServer()
        {
            var builder = new WebHostBuilder()
                .ConfigureServices(services =>
                {
                    services.AddControllersWithViews().AddMicrosoftIdentityUI();

                    // AccountController resolves IOptionsMonitor<MicrosoftIdentityOptions>;
                    // register an empty options entry so DI can satisfy the ctor.
                    services.Configure<MicrosoftIdentityOptions>(TestScheme, _ => { });

                    services.AddAuthentication(TestScheme)
                        .AddScheme<AuthenticationSchemeOptions, CapturingChallengeHandler>(
                            TestScheme, _ => { });

                    services.AddAuthorization();
                })
                .Configure(app =>
                {
                    app.UseRouting();
                    app.UseAuthentication();
                    app.UseAuthorization();
#pragma warning disable ASP0014 // UseEndpoints is intentional for clarity in test host
                    app.UseEndpoints(endpoints => endpoints.MapControllers());
#pragma warning restore ASP0014
                });
            return new TestServer(builder);
        }

        private static async Task<(HttpStatusCode status, string? capturedRedirect)> ChallengeAsync(
            TestServer server,
            string? redirectUri)
        {
            using var client = server.CreateClient();
            var url = $"/MicrosoftIdentity/Account/Challenge/{TestScheme}";
            if (redirectUri != null)
            {
                url += "?redirectUri=" + Uri.EscapeDataString(redirectUri);
            }

            using var response = await client.GetAsync(url).ConfigureAwait(false);
            string? captured = null;
            if (response.Headers.TryGetValues(CapturedRedirectHeader, out var values))
            {
                captured = string.Join(",", values);
            }

            return (response.StatusCode, captured);
        }

        // Local and obviously-non-local inputs: baseline behavior expected by the
        // original IsLocalUrl gate (no regression from the MSRC hardening).
        [Theory]
        [InlineData("/home", "/home")]
        [InlineData("/", "/")]
        [InlineData("/path?q=1&x=2", "/path?q=1&x=2")]
        [InlineData("", "/")]
        [InlineData(null, "/")]
        [InlineData("https://evil.example.com/path", "/")]
        [InlineData("http://evil.example.com", "/")]
        [InlineData("javascript:alert(1)", "/")]
        public async Task Challenge_RedirectUri_Variants_LocalOrRejected(
            string? input,
            string expectedRedirect)
        {
            using var server = CreateServer();
            var (status, captured) = await ChallengeAsync(server, input);

            Assert.Equal(HttpStatusCode.OK, status);
            Assert.Equal(expectedRedirect, captured);
        }

        // Same-origin absolute URLs must be coerced to their PathAndQuery — this is the
        // path taken by MicrosoftIdentityConsentAndConditionalAccessHandler.ChallengeUser,
        // which passes NavigationManager.Uri (always absolute) for Blazor Server step-up.
        // TestServer's default base is http://localhost, so "localhost" is same-origin here.
        [Theory]
        [InlineData("http://localhost/page", "/page")]
        [InlineData("http://localhost/page?q=1", "/page?q=1")]
        [InlineData("http://localhost/", "/")]
        [InlineData("http://LOCALHOST/page", "/page")]            // host is case-insensitive
        public async Task Challenge_SameOriginAbsolute_CoercedToPathAndQuery(
            string input,
            string expectedRedirect)
        {
            using var server = CreateServer();
            var (status, captured) = await ChallengeAsync(server, input);

            Assert.Equal(HttpStatusCode.OK, status);
            Assert.Equal(expectedRedirect, captured);
        }

        // Different origins (host, scheme, or port) must fall through to "/".
        [Theory]
        [InlineData("http://attacker.example.com/page")]
        [InlineData("https://localhost/page")]                    // scheme mismatch
        [InlineData("http://localhost:8080/page")]                // port mismatch
        public async Task Challenge_DifferentOrigin_FallsBackToRoot(string input)
        {
            using var server = CreateServer();
            var (status, captured) = await ChallengeAsync(server, input);

            Assert.Equal(HttpStatusCode.OK, status);
            Assert.Equal("/", captured);
        }

        // The MSRC hardening: same-origin absolute URLs whose PathAndQuery is a
        // protocol-relative URL ("//evil.com/x" or "/\evil.com") must be rejected.
        // Without the IsLocalUrl re-check on PathAndQuery, these would reach the
        // Location header verbatim and a browser would resolve them cross-origin.
        [Theory]
        [InlineData("http://localhost//evil.example.com/x")]      // protocol-relative
        [InlineData("http://localhost/\\evil.example.com")]       // slash+backslash
        public async Task Challenge_SameOriginWithProtocolRelativePath_Rejected(string input)
        {
            using var server = CreateServer();
            var (status, captured) = await ChallengeAsync(server, input);

            Assert.Equal(HttpStatusCode.OK, status);
            Assert.Equal("/", captured);
        }

        // Defense-in-depth against misconfigured reverse proxies (NGINX, IIS ARR, F5)
        // that decode %2f / %5c in Location headers. Browsers per RFC 3986 treat these
        // as literal path chars (direct hit yields 404, not a redirect), but a proxy
        // rewriting the Location header can expand them into "//" or "/\" — the same
        // protocol-relative shape that SameOriginWithProtocolRelativePath_Rejected
        // guards against in decoded form. The hardening rejects both the direct-input
        // branch (IsLocalUrl returns true for "/%2f%2fevil.com") and the same-origin
        // coercion branch.
        [Theory]
        [InlineData("http://localhost/%2f%2fevil.example.com")]   // same-origin + %2f bypass
        [InlineData("http://localhost/%5c%5cevil.example.com")]   // same-origin + %5c bypass
        [InlineData("/%2f%2fevil.example.com")]                    // direct %2f bypass (no host)
        [InlineData("/%5c%5cevil.example.com")]                    // direct %5c bypass (no host)
        [InlineData("/%2F%2Fevil.example.com")]                    // case-insensitive hex
        public async Task Challenge_PercentEncodedSlashBypass_Rejected(string input)
        {
            using var server = CreateServer();
            var (status, captured) = await ChallengeAsync(server, input);

            Assert.Equal(HttpStatusCode.OK, status);
            Assert.Equal("/", captured);
        }

        // Userinfo misdirection: "http://localhost@evil.example.com/x" is parsed by Uri
        // as Host="evil.example.com" (userinfo="localhost"), which is NOT same-origin as
        // the TestServer base of http://localhost. The different-origin branch should
        // fall back to "/" — verifying that IsSameOrigin compares Uri.Host, not the
        // userinfo-containing authority. If this ever regressed to comparing the raw
        // authority string, the attacker-controlled host would pass.
        [Fact]
        public async Task Challenge_UserinfoMisdirection_FallsBackToRoot()
        {
            using var server = CreateServer();
            var (status, captured) = await ChallengeAsync(server, "http://localhost@evil.example.com/x");

            Assert.Equal(HttpStatusCode.OK, status);
            Assert.Equal("/", captured);
        }

        // Triple-slash inputs like "///evil.example.com/x" are ambiguous: Uri.TryCreate
        // with UriKind.Absolute may parse them as file:// URIs with an empty host, which
        // would neither match IsLocalUrl (false — looks absolute to routing) nor
        // IsSameOrigin (scheme mismatch). Either way they must not reach the Location
        // header verbatim. This test locks the fall-through-to-root contract.
        [Fact]
        public async Task Challenge_TripleSlashFileFallthrough_FallsBackToRoot()
        {
            using var server = CreateServer();
            var (status, captured) = await ChallengeAsync(server, "///evil.example.com/x");

            Assert.Equal(HttpStatusCode.OK, status);
            Assert.Equal("/", captured);
        }

        // HTTPS BaseAddress variants: exercises the `request.IsHttps ? 443 : 80` branch
        // of IsSameOrigin to ensure same-origin coercion works on HTTPS hosts and that
        // scheme-mismatched absolute URLs (http against an https request) fall through.
        [Theory]
        [InlineData("https://localhost/page", "/page")]           // same-origin HTTPS
        [InlineData("https://localhost:443/page", "/page")]       // explicit default port
        [InlineData("http://localhost/page", "/")]                // scheme mismatch on HTTPS host
        [InlineData("https://localhost//evil.example.com/x", "/")]// protocol-relative on HTTPS
        public async Task Challenge_HttpsBaseAddress_EnforcesSameOrigin(
            string input,
            string expectedRedirect)
        {
            using var server = CreateServer();
            using var client = server.CreateClient();
            client.BaseAddress = new Uri("https://localhost/");

            var url = $"/MicrosoftIdentity/Account/Challenge/{TestScheme}?redirectUri="
                + Uri.EscapeDataString(input);
            using var response = await client.GetAsync(url);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var captured = response.Headers.TryGetValues(CapturedRedirectHeader, out var values)
                ? string.Join(",", values)
                : null;
            Assert.Equal(expectedRedirect, captured);
        }

        /// <summary>
        /// Replaces the real OpenIdConnect handler for test purposes: instead of issuing
        /// an OIDC redirect, it captures <see cref="AuthenticationProperties.RedirectUri"/>
        /// into a response header and returns 200. This isolates the behavior of the
        /// controller's redirect-URI validation from the authentication middleware chain.
        /// </summary>
        private sealed class CapturingChallengeHandler : AuthenticationHandler<AuthenticationSchemeOptions>
        {
            public CapturingChallengeHandler(
                IOptionsMonitor<AuthenticationSchemeOptions> options,
                ILoggerFactory logger,
                UrlEncoder encoder)
                : base(options, logger, encoder)
            {
            }

            protected override Task<AuthenticateResult> HandleAuthenticateAsync()
                => Task.FromResult(AuthenticateResult.NoResult());

            protected override Task HandleChallengeAsync(AuthenticationProperties properties)
            {
                Response.Headers[CapturedRedirectHeader] = properties?.RedirectUri ?? string.Empty;
                Response.StatusCode = (int)HttpStatusCode.OK;
                return Task.CompletedTask;
            }
        }
    }
}
