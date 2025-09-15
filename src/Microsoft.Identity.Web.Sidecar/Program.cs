// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Buffers.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.Resource;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens.Experimental;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"), subscribeToJwtBearerMiddlewareDiagnosticsEvents: true);
builder.Services.AddAuthorization();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

var scopeRequiredByApi = app.Configuration["AzureAd:Scopes"] ?? "";

app.MapGet("/Validate", (HttpContext httpContext) =>
{
    httpContext.VerifyUserHasAnyAcceptedScope(scopeRequiredByApi);

    var claimsPrincipal = httpContext.User;
    var token = claimsPrincipal.GetBootstrapToken();

    JsonWebToken? jsonWebToken = token as JsonWebToken;

    if (jsonWebToken?.InnerToken is not null)
    {
        // In the case the token is a JWE (encrypted token), we use the decrypted token.
        jsonWebToken = jsonWebToken.InnerToken;
    }

    var decodedBody = Base64Url.DecodeFromChars(jsonWebToken?.EncodedPayload);

    var jsonDoc = JsonSerializer.Deserialize<JsonObject>(decodedBody);

    var result = new ValidateResult(
        Protocol: "Bearer",
        Token: jsonWebToken!.EncodedToken,
        Claims: jsonDoc!
    );

    return result;
})
.WithName("Validate")
.WithOpenApi();

app.Run();

internal record ValidateResult(string Protocol, string Token, JsonObject Claims)
{
}
