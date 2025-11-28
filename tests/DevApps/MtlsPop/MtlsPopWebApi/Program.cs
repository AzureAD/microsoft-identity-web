// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Identity.Web;

namespace MtlsPopSample
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllers();

            // Learn more about configuring OpenAPI at https://learn.microsoft.com/aspnet/core/fundamentals/openapi/aspnetcore-openapi
            builder.Services.AddEndpointsApiExplorer();

            builder.Services.AddMicrosoftIdentityWebApiAuthentication(builder.Configuration);

            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("MtlsPop", policy =>
                    policy.Requirements.Add(new MtlsPopRequirement()));
            });

            builder.Services.AddSingleton<IAuthorizationHandler, MtlsPopAuthorizationHandler>();

            var app = builder.Build();

            app.UseHttpsRedirection();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}

/*

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Configure Microsoft Identity Web API authentication
builder.Services.AddMicrosoftIdentityWebApiAuthentication(builder.Configuration, "AzureAd", "Bearer", true);

// Configure custom mTLS PoP token validation
builder.Services.Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
{
    var existingOnTokenValidated = options.Events?.OnTokenValidated;

    options.Events ??= new JwtBearerEvents();

    options.Events.OnTokenValidated = async context =>
    {
        // Call the existing handler first (if any)
        if (existingOnTokenValidated != null)
        {
            await existingOnTokenValidated(context);
        }

        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();

        try
        {
            // Get the JWT token
            var token = context.SecurityToken as JwtSecurityToken;
            if (token == null)
            {
                logger.LogWarning("SecurityToken is not a JwtSecurityToken");
                context.Fail("Invalid token type");
                return;
            }

            // Check for cnf claim (confirmation claim for mTLS PoP)
            var cnfClaim = token.Claims.FirstOrDefault(c => c.Type == "cnf");
            if (cnfClaim == null)
            {
                logger.LogWarning("Token does not contain cnf claim - not an mTLS PoP token");
                context.Fail("Missing cnf claim - mTLS PoP token required");
                return;
            }

            logger.LogInformation("Found cnf claim in token: {CnfValue}", cnfClaim.Value);

            // Parse the cnf claim to get x5t#S256
            var cnfJson = System.Text.Json.JsonDocument.Parse(cnfClaim.Value);
            if (!cnfJson.RootElement.TryGetProperty("x5t#S256", out var x5tS256Element))
            {
                logger.LogWarning("cnf claim does not contain x5t#S256 property");
                context.Fail("Invalid cnf claim - missing x5t#S256");
                return;
            }

            var x5tS256 = x5tS256Element.GetString();
            if (string.IsNullOrEmpty(x5tS256))
            {
                logger.LogWarning("x5t#S256 value is empty");
                context.Fail("Invalid x5t#S256 value");
                return;
            }

            logger.LogInformation("Token bound to certificate with x5t#S256: {X5tS256}", x5tS256);

            // Get client certificate from request (if mTLS is configured)
            var clientCert = context.HttpContext.Connection.ClientCertificate;
            if (clientCert != null)
            {
                // Compute the SHA256 thumbprint of the client certificate
                var certThumbprint = ComputeCertificateThumbprint(clientCert);

                logger.LogInformation("Client certificate x5t#S256: {CertThumbprint}", certThumbprint);

                // Verify that the certificate in the request matches the one in the token
                if (!string.Equals(certThumbprint, x5tS256, StringComparison.OrdinalIgnoreCase))
                {
                    logger.LogWarning("Certificate mismatch - Token x5t#S256: {TokenThumbprint}, Client cert x5t#S256: {CertThumbprint}",
                        x5tS256, certThumbprint);
                    context.Fail("Certificate thumbprint mismatch");
                    return;
                }

                logger.LogInformation("mTLS PoP token validation successful - certificate binding verified");
            }
            else
            {
                logger.LogInformation("No client certificate in request - mTLS PoP token accepted based on cnf claim presence");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error validating mTLS PoP token");
            context.Fail($"mTLS PoP validation error: {ex.Message}");
        }

        await Task.CompletedTask;
    };
});

var app = builder.Build();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

static string ComputeCertificateThumbprint(X509Certificate2 certificate)
{
    using var sha256 = SHA256.Create();
    var hash = sha256.ComputeHash(certificate.RawData);
    return Base64UrlEncoder.Encode(hash);
}
*/
