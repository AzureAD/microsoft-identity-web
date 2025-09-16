// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Identity.Web.Sidecar.Endpoints;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Microsoft.Identity.Web.Sidecar;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"))

            .EnableTokenAcquisitionToCallDownstreamApi()
            .AddInMemoryTokenCaches();

        builder.Services.AddAgentIdentities()
               .AddDownstreamApis(builder.Configuration.GetSection("DownstreamApis"));

        // Health checks:
        // Tag checks that should participate in readiness with "ready".
        builder.Services.AddHealthChecks()
            // Placeholder readiness check; replace or add more (e.g., downstream API, cache, Key Vault, etc.)
            .AddCheck("self", () => HealthCheckResult.Healthy(), tags: new[] { "ready" });

        // Disable claims mapping.
        JwtSecurityTokenHandler.DefaultMapInboundClaims = false;
        JsonWebTokenHandler.DefaultMapInboundClaims = false;

        builder.Services.Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
        {
            options.TokenValidationParameters.RoleClaimType = "roles";
            options.TokenValidationParameters.NameClaimType = "sub";
        });

        builder.Services.AddAuthorization();

// Something to consider:
// #if DEBUG
        // Avoid pulling OpenAPI dependencies into trimmed Release unless needed.
        builder.Services.AddOpenApi();
// #endif

        var app = builder.Build();

        // Liveness: quick probe (no checks executed). Returns 200 if process is alive.
        // httpGet: path: /health port: 8080
        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            Predicate = _ => false // Skip all registered checks for liveness.
        });

        // Readiness: run only checks tagged with "ready".
        // readinessProbe: httpGet: path: /health/ready port: 8080
        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready"),
            ResponseWriter = WriteReadinessResponseAsync
        });

// #if DEBUG
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }
// #endif

        app.AddValidateRequestEndpoints();
        app.AddAuthorizationHeaderRequestEndpoints();
        app.AddDownstreamApiRequestEndpoints();

        app.Run();

        // Local static method keeps scope tight & linker-friendly.
        static Task WriteReadinessResponseAsync(HttpContext context, HealthReport report)
        {
            context.Response.ContentType = "application/json";
            var options = new JsonWriterOptions { Indented = false };
            var writer = new Utf8JsonWriter(context.Response.Body, options);

            writer.WriteStartObject();
            writer.WriteString("status", report.Status.ToString());
            writer.WritePropertyName("results");
            writer.WriteStartArray();

            foreach (var kvp in report.Entries)
            {
                var entry = kvp.Value;
                writer.WriteStartObject();
                writer.WriteString("name", kvp.Key);
                writer.WriteString("status", entry.Status.ToString());
                if (!string.IsNullOrEmpty(entry.Description))
                {
                    writer.WriteString("description", entry.Description);
                }
                writer.WriteString("duration", entry.Duration.ToString());
                if (entry.Exception is { } ex)
                {
                    writer.WriteString("error", ex.Message);
                }
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
            writer.Flush();
            return Task.CompletedTask;
        }
    }
}
