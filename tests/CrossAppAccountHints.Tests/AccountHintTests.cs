// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace CrossAppAccountHints.Tests;

public sealed class AccountHintTests
{
    [Theory]
    [InlineData("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")]
    [InlineData("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb")]
    [InlineData("9188040d-6c67-4c5b-b112-36a304b66dad")]
    public async Task TenantSpecificIssuerCanSignInThroughCommonAuthority(string tenantId)
    {
        // Arrange
        using var app = new HintApp("A", tenantId);
        using HttpClient client = app.CreateBrowser();

        // Act
        HttpResponseMessage response = await app.SignInAsync(client, "alice@example.test");

        // Assert
        Assert.Contains("IdWeb.Poc.A.Auth=", string.Join(";", response.Headers.GetValues("Set-Cookie")), StringComparison.Ordinal);
        Assert.Contains("Authenticated as alice@example.test", await client.GetStringAsync("/"), StringComparison.Ordinal);
    }

    [Fact]
    public async Task ValidatedSignInSharesHintButNotApplicationSession()
    {
        // Arrange
        using var appA = new HintApp("A");
        using var appB = new HintApp("B");
        using HttpClient clientA = appA.CreateBrowser();
        using HttpClient clientB = appB.CreateBrowser();
        HttpResponseMessage signInA = await appA.SignInAsync(clientA, "alice@example.test");
        string hint = Cookie(signInA, "IdWeb.Poc.AccountHints");

        // Act
        string ticketA = Cookie(signInA, "IdWeb.Poc.A.Auth").Split('=', 2)[1];
        clientB.DefaultRequestHeaders.Add("Cookie", $"{hint}; IdWeb.Poc.B.Auth={ticketA}");
        HttpResponseMessage challengeB = await clientB.GetAsync("/");
        Assert.True(challengeB.StatusCode == HttpStatusCode.Redirect, await challengeB.Content.ReadAsStringAsync());
        var parameters = QueryHelpers.ParseQuery(challengeB.Headers.Location!.Query);

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, challengeB.StatusCode);
        Assert.Equal("none", parameters["prompt"]);
        Assert.Equal("alice@example.test", parameters["login_hint"]);
        Assert.Equal("code id_token", parameters["response_type"]);
        Assert.Equal("https://localhost:5002/signin-oidc", parameters["redirect_uri"]);
        Assert.DoesNotContain("IdWeb.Poc.B.Auth=", string.Join(";", challengeB.Headers.GetValues("Set-Cookie")), StringComparison.Ordinal);

        HttpResponseMessage signInB = await appB.CompleteSignInAsync(clientB, challengeB, "alice@example.test");
        Assert.Contains("IdWeb.Poc.B.Auth=", string.Join(";", signInB.Headers.GetValues("Set-Cookie")), StringComparison.Ordinal);
        clientB.DefaultRequestHeaders.Remove("Cookie");
        HttpResponseMessage pageB = await clientB.GetAsync("/");
        Assert.Contains("Authenticated as alice@example.test", await pageB.Content.ReadAsStringAsync(), StringComparison.Ordinal);
        Assert.Equal(0, appA.BackchannelRequests);
        Assert.Equal(0, appB.BackchannelRequests);
    }

    [Theory]
    [InlineData("nonce", "OpenIdConnectProtocolInvalidNonceException")]
    [InlineData("c_hash", "OpenIdConnectProtocolInvalidCHashException")]
    [InlineData("tid", "SecurityTokenInvalidIssuerException")]
    public async Task InvalidProtocolClaimCannotPublishAHintOrSession(string invalidClaim, string expectedException)
    {
        // Arrange
        using var app = new HintApp("A");
        using HttpClient client = app.CreateBrowser();
        HttpResponseMessage challenge = await client.GetAsync("/signin");

        // Act
        HttpResponseMessage response = await app.CompleteSignInAsync(client, challenge, "alice@example.test", invalidClaim);

        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Contains(expectedException, await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
        string cookies = response.Headers.TryGetValues("Set-Cookie", out IEnumerable<string>? values) ? string.Join(";", values) : "";
        Assert.DoesNotContain("IdWeb.Poc.AccountHints=", cookies, StringComparison.Ordinal);
        Assert.DoesNotContain("IdWeb.Poc.A.Auth=", cookies, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("login_required")]
    [InlineData("interaction_required")]
    [InlineData("consent_required")]
    [InlineData("account_selection_required")]
    public async Task ExpectedAutomaticFailureFallsBackWithoutRetrying(string error)
    {
        // Arrange
        using var appA = new HintApp("A");
        using var appB = new HintApp("B");
        using HttpClient clientA = appA.CreateBrowser();
        using HttpClient clientB = appB.CreateBrowser();
        HttpResponseMessage signedIn = await appA.SignInAsync(clientA, "alice@example.test");
        clientB.DefaultRequestHeaders.Add("Cookie", Cookie(signedIn, "IdWeb.Poc.AccountHints"));
        HttpResponseMessage challenge = await clientB.GetAsync("/");
        string state = QueryHelpers.ParseQuery(challenge.Headers.Location!.Query)["state"].ToString();

        // Act
        using var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["state"] = state,
            ["error"] = error,
            ["error_description"] = "Account selection is required.",
        });
        HttpResponseMessage failed = await clientB.PostAsync("/signin-oidc", form);
        HttpResponseMessage page = await clientB.GetAsync(failed.Headers.Location);

        // Assert
        Assert.Equal("/?sso=interaction-required", failed.Headers.Location!.OriginalString);
        Assert.Equal(HttpStatusCode.OK, page.StatusCode);
        Assert.Contains("Anonymous: no local application session", await page.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task LocalSignOutSuppressesAutomaticSignInAndPreservesSiblingSession()
    {
        // Arrange
        using var appA = new HintApp("A");
        using var appB = new HintApp("B");
        using HttpClient clientA = appA.CreateBrowser();
        using HttpClient clientB = appB.CreateBrowser();
        HttpResponseMessage signedIn = await appA.SignInAsync(clientA, "alice@example.test");
        clientB.DefaultRequestHeaders.Add("Cookie", Cookie(signedIn, "IdWeb.Poc.AccountHints"));
        HttpResponseMessage automatic = await clientB.GetAsync("/");
        await appB.CompleteSignInAsync(clientB, automatic, "alice@example.test");
        HttpResponseMessage page = await clientB.GetAsync("/");
        string html = await page.Content.ReadAsStringAsync();
        string token = WebUtility.HtmlDecode(Regex.Match(html, "name=\"__RequestVerificationToken\" value=\"([^\"]+)\"").Groups[1].Value);

        // Act
        using var form = new FormUrlEncodedContent(new Dictionary<string, string> { ["__RequestVerificationToken"] = token });
        HttpResponseMessage signedOut = await clientB.PostAsync("/signout", form);
        HttpResponseMessage anonymous = await clientB.GetAsync("/");
        HttpResponseMessage sibling = await clientA.GetAsync("/");

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, signedOut.StatusCode);
        Assert.Equal(HttpStatusCode.OK, anonymous.StatusCode);
        string anonymousHtml = await anonymous.Content.ReadAsStringAsync();
        Assert.Contains("Anonymous: no local application session", anonymousHtml, StringComparison.Ordinal);
        Assert.Contains("suppressed after local sign-out", anonymousHtml, StringComparison.Ordinal);
        Assert.Contains("alice@example.test", anonymousHtml, StringComparison.Ordinal);
        Assert.Contains("Authenticated as alice@example.test", await sibling.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("not-valid-base64!")]
    [InlineData("expired")]
    [InlineData("malformed-account")]
    public async Task InvalidOrExpiredHintsAreIgnored(string value)
    {
        // Arrange
        using var app = new HintApp("B");
        using HttpClient client = app.CreateBrowser();
        if (value != "not-valid-base64!")
        {
            value = WebEncoders.Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(new
            {
                Version = 1,
                ExpiresUtc = value == "expired" ? DateTimeOffset.UtcNow.AddHours(-1) : DateTimeOffset.UtcNow.AddHours(1),
                Accounts = new[] { new { LoginHint = value == "expired" ? "alice@example.test" : "", DisplayName = "Alice" } },
            }));
        }

        client.DefaultRequestHeaders.Add("Cookie", $"IdWeb.Poc.AccountHints={value}");

        // Act
        HttpResponseMessage page = await client.GetAsync("/");

        // Assert
        Assert.Equal(HttpStatusCode.OK, page.StatusCode);
        Assert.Contains("Anonymous: no local application session", await page.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task MostRecentAccountBecomesPreferredAndRetainsOtherHints()
    {
        // Arrange
        using var app = new HintApp("A");
        using HttpClient client = app.CreateBrowser();
        await app.SignInAsync(client, "alice@example.test");

        // Act
        HttpResponseMessage switchChallenge = await client.GetAsync("/switch-account");
        Assert.Equal("select_account", QueryHelpers.ParseQuery(switchChallenge.Headers.Location!.Query)["prompt"]);
        await app.CompleteSignInAsync(client, switchChallenge, "bob@example.test");
        string page = await client.GetStringAsync("/");

        // Assert
        Assert.Contains("Authenticated as bob@example.test", page, StringComparison.Ordinal);
        Assert.Contains("bob@example.test (preferred)", page, StringComparison.Ordinal);
        Assert.Contains("<li>alice@example.test</li>", page, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ChangedSharedPreferenceDoesNotSwitchAnExistingSession()
    {
        // Arrange
        using var appA = new HintApp("A");
        using var appB = new HintApp("B");
        using HttpClient clientA = appA.CreateBrowser();
        using HttpClient clientB = appB.CreateBrowser();
        HttpResponseMessage signInB = await appB.SignInAsync(clientB, "bob@example.test");
        HttpResponseMessage signInA = await appA.SignInAsync(clientA, "alice@example.test");
        using HttpClient returningClientB = appB.CreateBrowser(handleCookies: false);
        returningClientB.DefaultRequestHeaders.Add("Cookie",
            $"{Cookie(signInB, "IdWeb.Poc.B.Auth")}; {Cookie(signInA, "IdWeb.Poc.AccountHints")}");

        // Act
        HttpResponseMessage response = await returningClientB.GetAsync("/");
        string page = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Authenticated as bob@example.test", page, StringComparison.Ordinal);
        Assert.Contains("alice@example.test (preferred)", page, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SignOutRequiresAntiforgeryToken()
    {
        // Arrange
        using var app = new HintApp("A");
        using HttpClient client = app.CreateBrowser();
        await app.SignInAsync(client, "alice@example.test");

        // Act
        using var form = new FormUrlEncodedContent(new Dictionary<string, string>());
        HttpResponseMessage response = await client.PostAsync("/signout", form);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Authenticated as alice@example.test", await client.GetStringAsync("/"), StringComparison.Ordinal);
    }

    private static string Cookie(HttpResponseMessage response, string name) =>
        response.Headers.GetValues("Set-Cookie").Single(value => value.StartsWith(name + "=", StringComparison.Ordinal)).Split(';')[0];

    private sealed class HintApp(string appId, string tenantId = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa") : WebApplicationFactory<Program>
    {
        private readonly RSA _rsa = RSA.Create(2048);
        private readonly CountingHandler _backchannel = new();
        private const string IssuerTemplate = "https://login.microsoftonline.com/{tenantid}/v2.0";
        private string Issuer => IssuerTemplate.Replace("{tenantid}", tenantId, StringComparison.Ordinal);
        private const string ClientId = "poc-test-client";

        public int BackchannelRequests => _backchannel.Requests;

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("AccountHints:AppId", appId);
            builder.UseContentRoot(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "DevApps", "CrossAppAccountHints")));
            builder.ConfigureServices(services =>
            {
                services.PostConfigure<OpenIdConnectOptions>(OpenIdConnectDefaults.AuthenticationScheme, options =>
                {
                    options.ClientId = ClientId;
                    options.TokenValidationParameters.ValidAudience = ClientId;
                    options.TokenValidationParameters.ValidIssuers = [IssuerTemplate];
                    options.Configuration = new OpenIdConnectConfiguration
                    {
                        Issuer = IssuerTemplate,
                        AuthorizationEndpoint = Issuer + "/authorize",
                    };
                    options.Configuration.SigningKeys.Add(new RsaSecurityKey(_rsa) { KeyId = "poc-test" });
                    options.ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(options.Configuration);
                    options.Backchannel = new HttpClient(_backchannel);
                });
            });
        }

        public HttpClient CreateBrowser(bool handleCookies = true) => CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri(appId == "A" ? "https://localhost:5001" : "https://localhost:5002"),
            AllowAutoRedirect = false,
            HandleCookies = handleCookies,
        });

        public async Task<HttpResponseMessage> SignInAsync(HttpClient client, string username)
        {
            HttpResponseMessage challenge = await client.GetAsync("/signin");
            HttpResponseMessage response = await CompleteSignInAsync(client, challenge, username);
            Assert.True(response.StatusCode == HttpStatusCode.Redirect, await response.Content.ReadAsStringAsync());
            return response;
        }

        public async Task<HttpResponseMessage> CompleteSignInAsync(
            HttpClient client, HttpResponseMessage challenge, string username, string? invalidClaim = null)
        {
            var parameters = QueryHelpers.ParseQuery(challenge.Headers.Location!.Query);
            const string code = "unused-authorization-code";
            byte[] hash = SHA256.HashData(Encoding.ASCII.GetBytes(code));
            var payload = new JwtPayload(Issuer, ClientId, null, DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow.AddMinutes(5))
            {
                ["sub"] = username,
                ["tid"] = invalidClaim == "tid" ? "cccccccc-cccc-cccc-cccc-cccccccccccc" : tenantId,
                ["name"] = username,
                ["preferred_username"] = username,
                ["nonce"] = invalidClaim == "nonce" ? "invalid" : parameters["nonce"].ToString(),
                ["c_hash"] = invalidClaim == "c_hash" ? "invalid" : WebEncoders.Base64UrlEncode(hash[..16]),
                ["iat"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            };
            var key = new RsaSecurityKey(_rsa) { KeyId = "poc-test" };
            string idToken = new JwtSecurityTokenHandler().WriteToken(
                new JwtSecurityToken(new JwtHeader(new SigningCredentials(key, SecurityAlgorithms.RsaSha256)), payload));
            using var form = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["state"] = parameters["state"].ToString(),
                ["code"] = code,
                ["id_token"] = idToken,
            });
            return await client.PostAsync("/signin-oidc", form);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                _rsa.Dispose();
                _backchannel.Dispose();
            }
        }
    }

    private sealed class CountingHandler : HttpMessageHandler
    {
        public int Requests { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests++;
            throw new InvalidOperationException("The sign-in-only POC must not call a token endpoint.");
        }
    }
}
