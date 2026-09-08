// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace Sidecar.Tests;

/// <summary>
/// A self-contained mock Identity Provider (Entra/ESTS stand-in) hosted in-process
/// on the ASP.NET Core <see cref="TestServer"/>. It reproduces the real-world validation surface so the
/// sidecar validates a PoP token's embedded access token through its GENUINE JwtBearer pipeline -
/// live OIDC discovery + JWKS fetch + signature/issuer/audience/lifetime checks - instead of a
/// statically injected signing key.
///
/// Endpoints:
///  - GET  /{tenant}/v2.0/.well-known/openid-configuration : OIDC discovery (issuer + jwks_uri).
///  - GET  /keys                                           : JWKS publishing the public half of
///                                                           <see cref="PopTestCrypto.AccessTokenSigningKey"/>.
///  - POST /{tenant}/oauth2/v2.0/token                     : client-credentials-style endpoint that
///                                                           accepts a <c>req_cnf</c> (the caller's PoP
///                                                           public key) and returns an app-only access
///                                                           token whose <c>cnf</c> binds that key.
///
/// The signing key it publishes is the same key it signs tokens with, so tokens it issues verify
/// against the discovery document exactly as a real Entra-issued token would.
/// </summary>
internal sealed class MockIdpServer : IDisposable
{
    // The tenant + issuer mirror a real Entra v2 token. Only the key-fetch (jwks_uri) and discovery are
    // redirected to this in-process server; the issuer string stays AAD-shaped so tokens look real.
    public const string Tenant = "10c419d4-4a50-45b2-aa4e-919fb84df24f";

    // Host label is irrelevant to TestServer routing (it matches by path), but must be an absolute URL.
    private const string BaseAddress = "https://mock-idp.localhost";

    public static string Issuer => PopTestCrypto.TestIssuer;
    public static string MetadataAddress => $"{BaseAddress}/{Tenant}/v2.0/.well-known/openid-configuration";
    public static string JwksUri => $"{BaseAddress}/keys";
    public static string TokenEndpoint => $"{BaseAddress}/{Tenant}/oauth2/v2.0/token";

    private readonly IHost _host;
    private readonly TestServer _server;

    public MockIdpServer()
    {
        string discoveryJson = BuildDiscoveryDocument();
        string jwksJson = BuildJwks();

        _host = new HostBuilder()
            .ConfigureWebHost(web =>
            {
                web.UseTestServer();
                web.ConfigureServices(services => services.AddRouting());
                web.Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapGet(
                            "/{tenant}/v2.0/.well-known/openid-configuration",
                            async context => await WriteJsonAsync(context, discoveryJson));

                        endpoints.MapGet(
                            "/keys",
                            async context => await WriteJsonAsync(context, jwksJson));

                        endpoints.MapPost(
                            "/{tenant}/oauth2/v2.0/token",
                            IssueTokenAsync);
                    });
                });
            })
            .Build();

        _host.Start();
        _server = _host.GetTestServer();
    }

    /// <summary>The in-memory message handler the sidecar's JwtBearer backchannel talks to.</summary>
    public HttpMessageHandler Handler => _server.CreateHandler();

    /// <summary>An <see cref="HttpClient"/> bound to this mock IdP (used as the JwtBearer backchannel).</summary>
    public HttpClient CreateBackchannel() => _server.CreateClient();

    /// <summary>
    /// A configuration manager that fetches this mock IdP's discovery + JWKS over the in-memory handler.
    /// Assigned to the Bearer <see cref="TokenValidationParameters.ConfigurationManager"/> so BOTH the
    /// JwtBearer handler AND the re-hosted SHR validator resolve signing keys live from JWKS.
    /// </summary>
    public ConfigurationManager<OpenIdConnectConfiguration> CreateConfigurationManager()
    {
        var documentRetriever = new HttpDocumentRetriever(CreateBackchannel()) { RequireHttps = false };
        return new ConfigurationManager<OpenIdConnectConfiguration>(
            MetadataAddress,
            new OpenIdConnectConfigurationRetriever(),
            documentRetriever);
    }

    /// <summary>
    /// Performs a client-credentials-style token request against the mock IdP's token endpoint over HTTP,
    /// presenting <paramref name="reqCnf"/> as the PoP key to bind. Returns the raw access token.
    /// </summary>
    public async Task<string> AcquireAppOnlyTokenAsync(string reqCnf)
    {
        using var client = _server.CreateClient();
        using var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "client_credentials"),
            new KeyValuePair<string, string>("scope", $"{PopTestCrypto.TestAudience}/.default"),
            new KeyValuePair<string, string>("req_cnf", reqCnf),
        });

        using HttpResponseMessage response = await client.PostAsync(TokenEndpoint, content);
        response.EnsureSuccessStatusCode();

        string body = await response.Content.ReadAsStringAsync();
        using JsonDocument document = JsonDocument.Parse(body);
        return document.RootElement.GetProperty("access_token").GetString()!;
    }

    private static async Task IssueTokenAsync(HttpContext context)
    {
        string reqCnf = context.Request.HasFormContentType
            ? context.Request.Form["req_cnf"].ToString()
            : context.Request.Query["req_cnf"].ToString();

        JsonElement cnf;
        if (!string.IsNullOrEmpty(reqCnf))
        {
            // req_cnf is base64url(JSON JWK) supplied by the caller: bind the token to that key.
            string jwkJson = Base64UrlEncoder.Decode(reqCnf);
            using JsonDocument jwkDocument = JsonDocument.Parse(jwkJson);
            cnf = PopTestCrypto.WrapJwkAsCnf(jwkDocument.RootElement);
        }
        else
        {
            // No req_cnf: default-bind to the shared test PoP key.
            cnf = PopTestCrypto.BuildCnf(PopTestCrypto.PopSigningKey, PopTestCrypto.PopKeyId);
        }

        string accessToken = PopTestCrypto.CreateAccessTokenWithCnf(
            Issuer, PopTestCrypto.TestAudience, DateTime.UtcNow.AddMinutes(10), cnf);

        var payload = new Dictionary<string, object>
        {
            ["token_type"] = "PoP",
            ["expires_in"] = 600,
            ["access_token"] = accessToken,
        };

        await WriteJsonAsync(context, JsonSerializer.Serialize(payload));
    }

    private static string BuildDiscoveryDocument()
    {
        var document = new Dictionary<string, object>
        {
            ["issuer"] = Issuer,
            ["jwks_uri"] = JwksUri,
            ["authorization_endpoint"] = $"{BaseAddress}/{Tenant}/oauth2/v2.0/authorize",
            ["token_endpoint"] = TokenEndpoint,
            ["response_types_supported"] = new[] { "token", "id_token" },
            ["subject_types_supported"] = new[] { "pairwise" },
            ["id_token_signing_alg_values_supported"] = new[] { "RS256" },
        };

        return JsonSerializer.Serialize(document);
    }

    private static string BuildJwks()
    {
        Dictionary<string, object> jwk = PopTestCrypto.BuildJwkDict(
            PopTestCrypto.AccessTokenSigningKey, PopTestCrypto.AccessTokenKeyId);
        jwk["use"] = "sig";
        jwk["alg"] = "RS256";

        var jwks = new Dictionary<string, object> { ["keys"] = new[] { jwk } };
        return JsonSerializer.Serialize(jwks);
    }

    private static async Task WriteJsonAsync(HttpContext context, string json)
    {
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(json);
    }

    public void Dispose()
    {
        _server.Dispose();
        _host.Dispose();
    }
}
