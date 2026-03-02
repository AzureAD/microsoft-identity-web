// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Identity.Web.Sidecar.Endpoints;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Microsoft.Identity.Web.Sidecar;

public class Program
{

    // Adding these the time to merge Andy's PR. Then will do the work to remove reflexion usage
    [RequiresUnreferencedCode("EnableTokenAcquisitionToCallDownstreamApis uses reflection")]
    [RequiresDynamicCode("EnableTokenAcquisitionToCallDownstreamApis uses reflection")]
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateSlimBuilder(args);

        builder.Services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
        });

        builder.Services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"), subscribeToJwtBearerMiddlewareDiagnosticsEvents: true)
            .EnableTokenAcquisitionToCallDownstreamApi()
            .AddInMemoryTokenCaches();

        builder.Services.PostConfigure<MicrosoftIdentityOptions>(options =>
        {
            options.AllowWebApiToBeAuthorizedByACL = true;
        });

        if (!builder.Environment.IsDevelopment())
        {
            // When not in a development environment, only allow connecting over
            // localhost.
            // AddHostFiltering is needed because this is using SlimBuilder which doesn't include
            // that middleware by default.
            builder.Services.AddHostFiltering(options =>
            {
                options.AllowedHosts = ["localhost"];
            });
        }

        ConfigureDataProtection(builder);

        // Add the agent identities and downstream APIs
        builder.Services.AddAgentIdentities()
               .AddDownstreamApis(builder.Configuration.GetSection("DownstreamApis"));

        builder.Services.AddHealthChecks();

        ConfigureAuthN(builder);

        builder.Services.AddAuthorization();

        builder.Services.AddOpenApi(options =>
        {
            options.AddOperationTransformer(new OptionsOverrideOperationTransformer());
        });

        var app = builder.Build();

        // Single endpoint for both liveness and readiness
        // as no checks are performed as part of startup.
        app.MapHealthChecks("/healthz");

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }
        else
        {
            app.UseHostFiltering();
        }

        app.AddValidateRequestEndpoints();
        app.AddAuthorizationHeaderRequestEndpoints();
        app.AddDownstreamApiRequestEndpoints();

        app.SetNoCachingMiddleware();

        app.Run();
    }

    private static void ConfigureAuthN(WebApplicationBuilder builder)
    {
        // Disable claims mapping.
        JwtSecurityTokenHandler.DefaultMapInboundClaims = false;
        JsonWebTokenHandler.DefaultMapInboundClaims = false;
        builder.Services.Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme,
            options =>
            {
                // Enable the right role claim type.
                options.TokenValidationParameters.RoleClaimType = "roles";
                options.TokenValidationParameters.NameClaimType = "sub";
            });
    }

    private static void ConfigureDataProtection(WebApplicationBuilder builder)
    {
        var dataProtectionBuilder = builder.Services.AddDataProtection()
            .SetApplicationName("Microsoft.Identity.Web.Sidecar");

        // Configure based on environment
        if (builder.Environment.IsProduction())
        {
            // Production configuration for Linux containers
            var keysPath = Environment.GetEnvironmentVariable("DATA_PROTECTION_KEYS_PATH") ?? "/app/keys";

            // Ensure the directory exists
            Directory.CreateDirectory(keysPath);

            dataProtectionBuilder.PersistKeysToFileSystem(new DirectoryInfo(keysPath));

            // Optional: Configure key encryption if certificate is available
            var certPath = Environment.GetEnvironmentVariable("DATA_PROTECTION_CERT_PATH");
            if (!string.IsNullOrEmpty(certPath) && File.Exists(certPath))
            {
                var certPassword = Environment.GetEnvironmentVariable("DATA_PROTECTION_CERT_PASSWORD");
#pragma warning disable SYSLIB0057 // Type or member is obsolete, No overload for new API accepts a password.
                var cert = new X509Certificate2(certPath, certPassword);
#pragma warning restore SYSLIB0057 // Type or member is obsolete
                dataProtectionBuilder.ProtectKeysWithCertificate(cert);
            }
        }
        else
        {
            // Development configuration
            var keysPath = Path.Combine(builder.Environment.ContentRootPath, "keys");
            Directory.CreateDirectory(keysPath);
            dataProtectionBuilder.PersistKeysToFileSystem(new DirectoryInfo(keysPath));
        }
    }
}
