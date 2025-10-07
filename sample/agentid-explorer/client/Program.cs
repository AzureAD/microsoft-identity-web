// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Mvc.Authorization;
using client.Data;
using Microsoft.Extensions.ObjectPool;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"))
    .EnableTokenAcquisitionToCallDownstreamApi(new string[] { /*"https://graph.microsoft.com/.default",*/ /*"api://d15884b6-a447-4dd5-a5a5-a668c49f6300/.default" */})
    .AddInMemoryTokenCaches();
builder.Services.AddMicrosoftGraphBeta(); // Add Microsoft Graph client
builder.Services.AddMicrosoftGraph(); // Add Microsoft Graph client
builder.Services.AddDownstreamApis(builder.Configuration.GetSection("DownstreamApis"));

builder.Services.Configure<OpenIdConnectOptions>(OpenIdConnectDefaults.AuthenticationScheme, options =>
{
    options.Prompt = "select_account"; // This will prompt the user to select an account every time they sign in
    options.Scope.Add("Application.ReadWrite.All");
    options.Scope.Add("User.Read");
    options.Scope.Add("api://d15884b6-a447-4dd5-a5a5-a668c49f6300/access_agent"); // Add the scopes you need
}); 

builder.Services.AddControllersWithViews()
    .AddMicrosoftIdentityUI();

builder.Services.AddAuthorization(options =>
{
    // By default, all incoming requests will be authorized according to the default policy
    options.FallbackPolicy = options.DefaultPolicy;
});

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor()
    .AddMicrosoftIdentityConsentHandler();
builder.Services.AddScoped<AgentApplicationService>(); // Register our agent application service
builder.Services.AddScoped<AgentIdentityService>(); // Register our new agent identity service
builder.Services.AddScoped<ApiService>(); // Register our new API service

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
