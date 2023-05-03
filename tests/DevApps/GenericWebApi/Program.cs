using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.TokenCacheProviders.InMemory;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));

// Enable the token acquisition
builder.Services.AddTokenAcquisition();
builder.Services.AddInMemoryTokenCaches();

// Read the web APIs from the appsettings.json
Dictionary<string, DownstreamApiOptions> downstreamApiOptions = new Dictionary<string, DownstreamApiOptions>();
builder.Configuration.GetSection("DownstreamApis").Bind(downstreamApiOptions);
foreach (var options in downstreamApiOptions)
{
    builder.Services.AddDownstreamApi(options.Key, builder.Configuration.GetSection($"DownstreamApis:{options.Key}"));
}

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
