// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace MtlsSample
{
    public class MtlsAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public const string ProtocolScheme = "MTLS";

        public MtlsAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder)
            : base(options, logger, encoder)
        {
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            Logger.LogInformation("MtlsAuthenticationHandler invoked");

            var certificate = await Request.HttpContext.Connection.GetClientCertificateAsync();

            if (certificate == null)
            {
                Logger.LogWarning("No certificate found");
                return AuthenticateResult.NoResult();
            }

            if (!certificate.MatchesHostname("LabAuth.MSIDLab.com"))
            {
                Logger.LogWarning($"Certificate has wrong subject name: {certificate.Subject}");
                return AuthenticateResult.Fail($"Certificate has wrong subject name: {certificate.Subject}");
            }

            // Create claims principal from the certificate
            var identity = new CaseSensitiveClaimsIdentity(
                [new("SubjectName", certificate.GetNameInfo(X509NameType.SimpleName, false))],
                ProtocolScheme);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, ProtocolScheme);

            return AuthenticateResult.Success(ticket);
        }
    }
}
