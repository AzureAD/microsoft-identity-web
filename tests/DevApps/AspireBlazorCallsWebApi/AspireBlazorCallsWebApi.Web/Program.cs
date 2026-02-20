using AspireBlazorCallsWebApi.Web;
using AspireBlazorCallsWebApi.Web.Components;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add Microsoft Identity Web authentication
builder.Services.AddMicrosoftIdentityWebAppAuthentication(builder.Configuration, "AzureAd")
    .EnableTokenAcquisitionToCallDownstreamApi()
    .AddInMemoryTokenCaches();

builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

// Register the Blazor authentication challenge handler
builder.Services.AddScoped<BlazorAuthenticationChallengeHandler>();

// Configure HttpClient for calling the API
builder.Services.AddHttpClient<WeatherApiClient>(client =>
{
    // This URL uses "https+http://" to indicate HTTPS is preferred over HTTP.
    // Learn more about service discovery scheme resolution at https://aka.ms/dotnet/sdschemes.
    client.BaseAddress = new Uri("https+http://apiservice");
})
.AddMicrosoftIdentityAppAuthenticationHandler("AzureAd", options =>
{
    var scopes = builder.Configuration.GetSection("WeatherApi:Scopes").Get<string[]>()
        ?? ["api://a021aff4-57ad-453a-bae8-e4192e5860f3/access_as_user"];
    options.Scopes = string.Join(" ", scopes);
});

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.UseAuthentication();
app.UseAuthorization();

// Map authentication endpoints with support for incremental consent and Conditional Access
var authGroup = app.MapGroup("/authentication");
authGroup.MapLoginAndLogout();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
