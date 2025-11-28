// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;

namespace MtlsPopSample
{
    public class MtlsPopRequirement : IAuthorizationRequirement
    {
    }

    public class MtlsPopAuthorizationHandler : AuthorizationHandler<MtlsPopRequirement>
    {
        private const string ProtocolName = "MTLS_POP";

        private readonly ILogger<MtlsPopAuthorizationHandler> _logger;

        public MtlsPopAuthorizationHandler(ILogger<MtlsPopAuthorizationHandler> logger)
        {
            _logger = logger;
        }

        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, MtlsPopRequirement requirement)
        {
            _logger.LogInformation("MtlsPopAuthorizationHandler invoked");

            if (context.Resource is not HttpContext httpContext)
            {
                _logger.LogWarning("Resource is not HttpContext");
                return Task.CompletedTask;
            }

            var authHeader = httpContext.Request.Headers.Authorization.FirstOrDefault();
            var authToken = !string.IsNullOrEmpty(authHeader) && authHeader.StartsWith($"{ProtocolName} ", StringComparison.OrdinalIgnoreCase)
                ? authHeader.Substring($"{ProtocolName} ".Length).Trim()
                : null;

            if (string.IsNullOrEmpty(authToken))
            {
                _logger.LogWarning("No auth token found");
                return Task.CompletedTask;
            }

            try
            {
                var handler = new JwtSecurityTokenHandler();
                var token = handler.ReadJwtToken(authToken);

                var cnfClaim = token.Claims.FirstOrDefault(c => c.Type == "cnf");
                if (cnfClaim == null)
                {
                    _logger.LogWarning("mTLS PoP token does not contain 'cnf' claim");
                    context.Fail(new AuthorizationFailureReason(this, "Missing 'cnf' claim in  token"));
                    return Task.CompletedTask;
                }

                _logger.LogInformation($"The 'cnf' claim value: {cnfClaim.Value}");

                var cnfJson = JsonDocument.Parse(cnfClaim.Value);
                if (!cnfJson.RootElement.TryGetProperty("x5t#S256", out var x5tS256Element))
                {
                    _logger.LogWarning("The 'cnf' claim does not contain 'x5t#S256' property");
                    context.Fail(new AuthorizationFailureReason(this, "Missing 'x5t#S256' property in mTLS PoP 'cnf' claim"));
                    return Task.CompletedTask;
                }

                var x5tS256 = x5tS256Element.GetString();
                if (string.IsNullOrEmpty(x5tS256))
                {
                    _logger.LogWarning("The 'cnf' claim contains an empty 'x5t#S256' property");
                    context.Fail(new AuthorizationFailureReason(this, "Empty 'x5t#S256' property in mTLS PoP 'cnf' claim"));
                    return Task.CompletedTask;
                }

                _logger.LogInformation($"Token bound to certificate with x5t#S256: {x5tS256}");

                var clientCert = httpContext.Connection.ClientCertificate;
                if (clientCert != null)
                {
                    var certThumbprint = GetCertificateThumbprint(clientCert);
                    _logger.LogInformation($"Client cert thumprint: {certThumbprint}");

                    if (!string.Equals(certThumbprint, x5tS256, StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogWarning($"Mismatch between cert thumbprint and 'x5t#S256' from mTLS PoP 'cnf' claim property: cert thumbprint - {certThumbprint}, x5t#S256 = {x5tS256}");
                        context.Fail(new AuthorizationFailureReason(this, "Cert thumbprint and mTLS PoP 'cnf' claim 'x5t#S256' property mismatch"));
                        return Task.CompletedTask;
                    }

                    _logger.LogInformation("mTLS PoP token validation successful");
                }
                else
                {
                    _logger.LogInformation("No client certificate in request");
                }

                context.Succeed(requirement);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating mTLS PoP token");
                context.Fail(new AuthorizationFailureReason(this, $"mTLS PoP validation error: {ex.Message}"));
            }

            return Task.CompletedTask;
        }

        private static string GetCertificateThumbprint(X509Certificate2 certificate)
        {
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(certificate.RawData);
            return Base64UrlEncoder.Encode(hash);
        }
    }
}
