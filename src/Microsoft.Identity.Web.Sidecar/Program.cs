// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web.Sidecar.Configuration;
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

        // Add the agent identities and downstream APIs
        builder.Services.AddAgentIdentities()
               .AddDownstreamApis(builder.Configuration.GetSection("DownstreamApis"));

        builder.Services.Configure<SidecarOptions>(builder.Configuration.GetSection("Sidecar"));

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
}
