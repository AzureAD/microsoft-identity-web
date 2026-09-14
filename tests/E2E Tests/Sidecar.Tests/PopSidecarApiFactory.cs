// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace Sidecar.Tests;

/// <summary>
/// A sidecar test host that repoints the "Bearer" JwtBearerOptions at an in-process
/// <see cref="MockIdpServer"/> (Entra/ESTS stand-in). The embedded access token inside an SHR is then
/// validated through the sidecar's genuine JwtBearer pipeline - live OIDC discovery + JWKS fetch +
/// signature/issuer/audience/lifetime - with no statically injected signing key. This exercises the
/// real-world validation path and confirms the PoP path reuses the same "Bearer" TVP
/// (issuer/audience/expiry/signing key).
/// </summary>
public class PopSidecarApiFactory : SidecarApiFactory
{
    private readonly MockIdpServer _mockIdp = new();

    /// <summary>The in-process mock IdP, exposed so a test can perform a full token acquisition round-trip.</summary>
    internal MockIdpServer Idp => _mockIdp;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.ConfigureServices(services =>
        {
            // Runs after Microsoft.Identity.Web has built the online TVP, so this override wins.
            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                ConfigurationManager<OpenIdConnectConfiguration> configurationManager =
                    _mockIdp.CreateConfigurationManager();

                // Point JwtBearer's metadata/backchannel at the mock IdP (loopback, plain HTTP).
                options.Authority = null;
                options.MetadataAddress = MockIdpServer.MetadataAddress;
                options.RequireHttpsMetadata = false;
                options.Backchannel = _mockIdp.CreateBackchannel();
                options.ConfigurationManager = configurationManager;

                // Keep the AzureAd-shaped issuer/audience/lifetime checks, but resolve signing keys from
                // the mock's live JWKS via ConfigurationManager (assigned to the TVP so the re-hosted SHR
                // validator - which only sees the TVP - fetches keys the same way). Clear the AAD alias
                // issuer validator (meaningless against a mock tenant) and validate the issuer string.
                TokenValidationParameters parameters = options.TokenValidationParameters;
                parameters.ValidateIssuer = true;
                parameters.ValidIssuer = MockIdpServer.Issuer;
                parameters.IssuerValidator = null;
                parameters.ValidateAudience = true;
                parameters.ValidAudience = PopTestCrypto.TestAudience;
                parameters.ValidateIssuerSigningKey = true;
                parameters.ValidateLifetime = true;
                // PRODUCTION-FAITHFUL: set ConfigurationManager on the OPTIONS only (as stock
                // JwtBearerPostConfigureOptions does from Authority). We deliberately DO NOT assign it
                // onto options.TokenValidationParameters here, so the test proves the embedded-AT key
                // source reaches the re-hosted SHR validator through the real pipeline, not a shortcut.
                parameters.ConfigurationManager = null;
                parameters.IssuerSigningKey = null;
                parameters.IssuerSigningKeys = null;
                parameters.RoleClaimType = "roles";
                parameters.NameClaimType = "sub";
            });
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _mockIdp.Dispose();
        }
    }
}
