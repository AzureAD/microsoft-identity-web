// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Web.Sidecar.Configuration;
using Microsoft.Identity.Web.Sidecar.Endpoints;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Microsoft.Identity.Web.Sidecar;

public class Program
{

    // Adding these the time to merge Andy's PR. Then will do the work to remove reflexion usage
#pragma warning disable IL2123 // RequiresUnreferencedCodeAttribute cannot be placed on entry point
#pragma warning disable IL3057 // RequiresDynamicCodeAttribute cannot be placed on entry point
    [RequiresUnreferencedCode("EnableTokenAcquisitionToCallDownstreamApis uses reflection")]
    [RequiresDynamicCode("EnableTokenAcquisitionToCallDownstreamApis uses reflection")]
    public static void Main(string[] args)
#pragma warning restore IL3057
#pragma warning restore IL2123
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

        // Add the agent identities and downstream APIs
        builder.Services.AddAgentIdentities()
               .AddDownstreamApis(builder.Configuration.GetSection("DownstreamApis"));

        builder.Services.Configure<SidecarOptions>(builder.Configuration.GetSection("Sidecar"));

        builder.Services.ConfigureHttpClientDefaults(http =>
            http.ConfigurePrimaryHttpMessageHandler(serviceProvider =>
                new SocketsHttpHandler
                {
                    AllowAutoRedirect = serviceProvider.GetRequiredService<IOptions<SidecarOptions>>()
                        .Value.AllowOutboundRedirects,
                }));

        builder.Services.AddHealthChecks();

        ConfigureAuthN(builder);

        builder.Services.AddAuthorization();

        builder.Services.AddOpenApi(options =>
        {
            options.AddOperationTransformer(new OptionsOverrideOperationTransformer());
        });

        var app = builder.Build();

        if (!app.Environment.IsDevelopment())
        {
            // Loopback-only outside development; health endpoint excepted for probes.
            app.UseLocalCallerRestriction();
        }

        // Register auth explicitly so it runs after the loopback check.
        app.UseAuthentication();
        app.UseAuthorization();

        // Single endpoint for both liveness and readiness
        // as no checks are performed as part of startup.
        app.MapHealthChecks(LocalCallerRestriction.HealthEndpointPath);

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
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
}
