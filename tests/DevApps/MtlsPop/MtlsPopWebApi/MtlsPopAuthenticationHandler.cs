// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace MtlsPopSample
{
    public class MtlsPopAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public const string ProtocolScheme = "MTLS_POP";

        public MtlsPopAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder)
            : base(options, logger, encoder)
        {
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            Logger.LogInformation("MtlsPopAuthenticationHandler invoked");

            var authHeader = Request.Headers.Authorization.FirstOrDefault();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith($"{ProtocolScheme} ", StringComparison.OrdinalIgnoreCase))
            {
                Logger.LogWarning("No MTLS_POP authorization header found");
                return AuthenticateResult.NoResult();
            }

            var authToken = authHeader.Substring($"{ProtocolScheme} ".Length).Trim();
            if (string.IsNullOrEmpty(authToken))
            {
                Logger.LogWarning("MTLS_POP authorization header is empty");
                return AuthenticateResult.Fail("Empty MTLS_POP token");
            }

            try
            {
                var handler = new JsonWebTokenHandler();
                var token = handler.ReadJsonWebToken(authToken);

                var cnfClaim = token.Claims.FirstOrDefault(c => c.Type == "cnf");
                if (cnfClaim == null)
                {
                    Logger.LogWarning("mTLS PoP token does not contain 'cnf' claim");
                    return AuthenticateResult.Fail("Missing 'cnf' claim in MTLS_POP token");
                }

                Logger.LogInformation($"The 'cnf' claim value: {cnfClaim.Value}");

                var cnfJson = JsonDocument.Parse(cnfClaim.Value);
                if (!cnfJson.RootElement.TryGetProperty("x5t#S256", out var x5tS256Element))
                {
                    Logger.LogWarning("The 'cnf' claim does not contain 'x5t#S256' property");
                    return AuthenticateResult.Fail("Missing 'x5t#S256' property in mTLS PoP 'cnf' claim");
                }

                var x5tS256 = x5tS256Element.GetString();
                if (string.IsNullOrEmpty(x5tS256))
                {
                    Logger.LogWarning("The 'cnf' claim contains an empty 'x5t#S256' property");
                    return AuthenticateResult.Fail("Empty 'x5t#S256' property in mTLS PoP 'cnf' claim");
                }

                Logger.LogInformation($"Token bound to certificate with x5t#S256: {x5tS256}");

                var clientCert = Context.Connection.ClientCertificate;
                if (clientCert != null)
                {
                    var certThumbprint = GetCertificateThumbprint(clientCert);
                    Logger.LogInformation($"Client cert thumbprint: {certThumbprint}");

                    if (!string.Equals(certThumbprint, x5tS256, StringComparison.OrdinalIgnoreCase))
                    {
                        Logger.LogWarning($"Mismatch between cert thumbprint and 'x5t#S256' from mTLS PoP 'cnf' claim property: cert thumbprint - {certThumbprint}, x5t#S256 = {x5tS256}");
                        return AuthenticateResult.Fail("Certificate thumbprint mismatch with mTLS PoP 'cnf' claim");
                    }

                    Logger.LogInformation("mTLS PoP token validation successful");
                }
                else
                {
                    Logger.LogInformation("No client certificate in request - skipping certificate binding verification");
                }

                // Create claims principal from the token
                var claims = token.Claims.Select(c => new Claim(c.Type, c.Value)).ToList();
                var identity = new CaseSensitiveClaimsIdentity(claims, ProtocolScheme);
                var principal = new ClaimsPrincipal(identity);
                var ticket = new AuthenticationTicket(principal, ProtocolScheme);

                return AuthenticateResult.Success(ticket);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error validating mTLS PoP token");
                return AuthenticateResult.Fail($"mTLS PoP validation error: {ex.Message}");
            }
        }

        private static string GetCertificateThumbprint(X509Certificate2 certificate)
        {
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(certificate.RawData);
            return Base64UrlEncoder.Encode(hash);
        }
    }
}
