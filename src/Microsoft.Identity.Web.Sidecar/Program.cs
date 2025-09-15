// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Diagnostics;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;
using Microsoft.IdentityModel.JsonWebTokens;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(
        configureJwtBearerOptions: jwtBearerOptions =>
        {
            builder.Configuration.GetSection("AzureAd").Bind(jwtBearerOptions);
            jwtBearerOptions.Events ??= new();
            jwtBearerOptions.Events.OnTokenValidated = context =>
            {
                Debug.Assert(context.SecurityToken is JsonWebToken, "Token should always be JsonWebToken");
                var token = (JsonWebToken)context.SecurityToken;

                if (context.Principal?.Identities.FirstOrDefault() is null)
                {
                    context.Fail("No principal or no identity");
                    return Task.FromResult(context);
                }

                context.Principal.Identities.First().BootstrapContext = token!.InnerToken is not null ? token.InnerToken: token;
                return Task.FromResult(context);
            };
        },
        configureMicrosoftIdentityOptions: msidOptions => builder.Configuration.GetSection("AzureAd").Bind(msidOptions));

builder.Services.AddAuthorization();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.AddValidateRequestEndpoints();

app.Run();
