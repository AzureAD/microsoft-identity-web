// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.SignedHttpRequest;
using Microsoft.IdentityModel.Tokens;

namespace Sidecar.Tests;

/// <summary>
/// SPIKE (throwaway) test helper: mints a self-contained ARM-shaped Signed HTTP Request (SHR) PoP
/// token without any live AAD dependency.
///
/// Two RSA keys are used, mirroring a real PoP flow:
///  - <see cref="AccessTokenSigningKey"/> (key A) signs the embedded (app-only) access token. Its
///    public half is trusted by the overridden Bearer TokenValidationParameters (see
///    <see cref="PopSidecarApiFactory"/>), proving the inner AT is validated via the reused AzureAd TVP.
///  - <see cref="PopSigningKey"/> (key B) signs the outer SHR and is advertised in the access token's
///    <c>cnf.jwk</c> claim so the SHR handler can bind the two together.
/// </summary>
internal static class PopTestCrypto
{
    public const string TestIssuer = "https://sts.windows.net/10c419d4-4a50-45b2-aa4e-919fb84df24f/";
    public const string TestAudience = "aab5089d-e764-47e3-9f28-cc11c2513821";
    public const string AccessTokenKeyId = "test-at-key";
    public const string PopKeyId = "test-pop-key";

    // Cached immutable key material. RSA instances are NOT thread-safe and xUnit runs test classes in
    // parallel, so we never share an instance: each accessor below hands out a FRESH RSA created from
    // these cached parameters, used only on the calling thread.
    private static readonly RSAParameters s_accessTokenPrivate;
    private static readonly RSAParameters s_popPrivate;
    private static readonly RSAParameters s_untrustedPrivate;

    static PopTestCrypto()
    {
        using var accessTokenKey = RSA.Create(2048);
        s_accessTokenPrivate = accessTokenKey.ExportParameters(true);

        using var popKey = RSA.Create(2048);
        s_popPrivate = popKey.ExportParameters(true);

        using var untrustedKey = RSA.Create(2048);
        s_untrustedPrivate = untrustedKey.ExportParameters(true);
    }

    /// <summary>Signs the embedded (app-only) access token; its public half is published via mock JWKS.</summary>
    public static RSA AccessTokenSigningKey => RSA.Create(s_accessTokenPrivate);

    /// <summary>Signs the outer SHR and is advertised in the access token's <c>cnf.jwk</c> claim.</summary>
    public static RSA PopSigningKey => RSA.Create(s_popPrivate);

    /// <summary>A key that is neither published via JWKS nor advertised in cnf - used for negative tests.</summary>
    public static RSA UntrustedKey => RSA.Create(s_untrustedPrivate);

    /// <summary>The public signing key the Bearer TVP must trust to validate the embedded access token.</summary>
    public static RsaSecurityKey AccessTokenPublicKey =>
        new(s_accessTokenPrivate) { KeyId = AccessTokenKeyId };

    /// <summary>
    /// Mints an app-only access token bound (via cnf.jwk) to <paramref name="cnfKey"/>.
    /// </summary>
    public static string CreateAccessToken(
        string issuer,
        string audience,
        DateTime expires,
        RSA cnfKey,
        string cnfKeyId) =>
        CreateAccessTokenWithCnf(issuer, audience, expires, BuildCnf(cnfKey, cnfKeyId));

    /// <summary>
    /// Mints an app-only access token carrying the supplied <c>cnf</c> confirmation claim (or none when
    /// <paramref name="cnf"/> is null). Signed by <see cref="AccessTokenSigningKey"/>, whose public half
    /// the mock IdP publishes via JWKS - so the sidecar validates this token through the live
    /// discovery/JWKS path, not a statically injected key.
    /// </summary>
    public static string CreateAccessTokenWithCnf(
        string issuer,
        string audience,
        DateTime expires,
        JsonElement? cnf)
    {
        var handler = new JsonWebTokenHandler();
        DateTime now = DateTime.UtcNow;

        var claims = new Dictionary<string, object>
        {
            ["sub"] = "11111111-1111-1111-1111-111111111111",
            ["oid"] = "11111111-1111-1111-1111-111111111111",
            ["appid"] = audience,
            ["idtyp"] = "app",
            ["roles"] = new[] { "Sidecar.Access" },
        };

        if (cnf is not null)
        {
            claims["cnf"] = cnf.Value;
        }

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = issuer,
            Audience = audience,
            IssuedAt = now,
            NotBefore = now.AddMinutes(-5),
            Expires = expires,
            SigningCredentials = new SigningCredentials(
                new RsaSecurityKey(AccessTokenSigningKey) { KeyId = AccessTokenKeyId },
                SecurityAlgorithms.RsaSha256),
            Claims = claims,
        };

        return handler.CreateToken(descriptor);
    }

    /// <summary>
    /// Mints an app-only access token with NO <c>cnf</c> claim - used to prove PoP binding is required
    /// (SHR validation must reject a token that advertises no confirmation key to bind against).
    /// </summary>
    public static string CreateAccessTokenWithoutCnf(string issuer, string audience, DateTime expires) =>
        CreateAccessTokenWithCnf(issuer, audience, expires, cnf: null);

    /// <summary>
    /// Mints a DELEGATED (user) access token bound (via cnf.jwk) to <paramref name="cnfKey"/>: it
    /// carries the <c>scp</c> claim and <c>idtyp "user"</c> (and no app <c>roles</c>). Used to prove the
    /// sidecar's app-only guard rejects a delegated token wrapped in an otherwise-valid SHR - inbound
    /// SHR PoP is scoped to app-only (client-credentials) tokens.
    /// </summary>
    public static string CreateDelegatedAccessToken(
        string issuer,
        string audience,
        DateTime expires,
        RSA cnfKey,
        string cnfKeyId)
    {
        var handler = new JsonWebTokenHandler();
        DateTime now = DateTime.UtcNow;

        var claims = new Dictionary<string, object>
        {
            ["sub"] = "22222222-2222-2222-2222-222222222222",
            ["oid"] = "22222222-2222-2222-2222-222222222222",
            ["appid"] = audience,
            ["idtyp"] = "user",
            ["scp"] = "user_impersonation",
            ["cnf"] = BuildCnf(cnfKey, cnfKeyId),
        };

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = issuer,
            Audience = audience,
            IssuedAt = now,
            NotBefore = now.AddMinutes(-5),
            Expires = expires,
            SigningCredentials = new SigningCredentials(
                new RsaSecurityKey(AccessTokenSigningKey) { KeyId = AccessTokenKeyId },
                SecurityAlgorithms.RsaSha256),
            Claims = claims,
        };

        return handler.CreateToken(descriptor);
    }

    /// <summary>
    /// Creates an SHR over the given method + URI, signed by <paramref name="popSigningKey"/>.
    /// When <paramref name="tsOverrideEpochSeconds"/> is supplied, that exact (signed) ts is embedded
    /// instead of the current time - used to deterministically produce an expired-ts token.
    /// </summary>
    public static string CreateSignedHttpRequest(
        string accessToken,
        string method,
        string uri,
        RSA popSigningKey,
        string popKeyId,
        long? tsOverrideEpochSeconds = null)
    {
        var handler = new SignedHttpRequestHandler();

        var httpRequestData = new HttpRequestData
        {
            Method = method,
            Uri = new Uri(uri, UriKind.Absolute),
        };

        var creationParameters = new SignedHttpRequestCreationParameters
        {
            CreateM = true,
            CreateU = true,
            CreateP = true,
            CreateTs = tsOverrideEpochSeconds is null,
            CreateH = false,
            CreateQ = false,
            CreateB = false,
            CreateNonce = false,
            CreateCnf = false,
        };

        var descriptor = new SignedHttpRequestDescriptor(
            accessToken,
            httpRequestData,
            new SigningCredentials(new RsaSecurityKey(popSigningKey) { KeyId = popKeyId }, SecurityAlgorithms.RsaSha256),
            creationParameters);

        if (tsOverrideEpochSeconds is not null)
        {
            descriptor.AdditionalPayloadClaims = new Dictionary<string, object>
            {
                ["ts"] = tsOverrideEpochSeconds.Value,
            };
        }

        return handler.CreateSignedHttpRequest(descriptor);
    }

    /// <summary>Builds the public RSA JWK dictionary (kty/n/e/kid) for the given key.</summary>
    public static Dictionary<string, object> BuildJwkDict(RSA rsa, string keyId)
    {
        RSAParameters publicParams = rsa.ExportParameters(false);
        return new Dictionary<string, object>
        {
            ["kty"] = "RSA",
            ["n"] = Base64UrlEncoder.Encode(publicParams.Modulus!),
            ["e"] = Base64UrlEncoder.Encode(publicParams.Exponent!),
            ["kid"] = keyId,
        };
    }

    /// <summary>Builds a <c>cnf</c> confirmation claim (<c>{"jwk": {...}}</c>) advertising the given key.</summary>
    public static JsonElement BuildCnf(RSA rsa, string keyId) =>
        ToJsonElement(new Dictionary<string, object> { ["jwk"] = BuildJwkDict(rsa, keyId) });

    /// <summary>Wraps a raw JWK element as a <c>cnf</c> confirmation claim (<c>{"jwk": jwk}</c>).</summary>
    public static JsonElement WrapJwkAsCnf(JsonElement jwk)
    {
        using var document = JsonDocument.Parse($"{{\"jwk\":{jwk.GetRawText()}}}");
        return document.RootElement.Clone();
    }

    /// <summary>
    /// The client-side <c>req_cnf</c> value (base64url(JSON JWK)) a caller presents to the mock token
    /// endpoint to request a token bound to its PoP key - mirrors the ESTS PoP acquisition contract.
    /// </summary>
    public static string BuildReqCnf(RSA rsa, string keyId) =>
        Base64UrlEncoder.Encode(JsonSerializer.Serialize(BuildJwkDict(rsa, keyId)));

    private static JsonElement ToJsonElement(object value)
    {
        using var document = JsonDocument.Parse(JsonSerializer.Serialize(value));
        return document.RootElement.Clone();
    }
}
