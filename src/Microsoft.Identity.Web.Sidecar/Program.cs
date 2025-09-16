// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Diagnostics;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.Sidecar.Endpoints;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Microsoft.Identity.Web.Sidecar;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"))
            .EnableTokenAcquisitionToCallDownstreamApi()
            .AddInMemoryTokenCaches();

        builder.Services.AddHealthChecks();

        builder.Services.Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme,
            options =>
            {
                options.Events ??= new();
                options.Events.OnTokenValidated = context =>
                {
                    Debug.Assert(context.SecurityToken is JsonWebToken, "Token should always be JsonWebToken");
                    var token = (JsonWebToken)context.SecurityToken;

                    if (context.Principal?.Identities.FirstOrDefault() is null)
                    {
                        context.Fail("No principal or no identity");
                        return Task.FromResult(context);
                    }

                    context.Principal.Identities.First().BootstrapContext = token.InnerToken is not null ? token.InnerToken : token;
                    return Task.FromResult(context);
                };
            });

        builder.Services.AddAuthorization();

        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        builder.Services.AddOpenApi();

        var app = builder.Build();

        app.MapHealthChecks("/healthz");

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.AddValidateRequestEndpoints();
        app.AddDownstreamApiRequestEndpoints();

        app.Run();
    }
}
