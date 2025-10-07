#if MISE
using Microsoft.Identity.ServiceEssentials;
#else
using Microsoft.AspNetCore.Authentication.JwtBearer;
#endif
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.TokenCacheProviders.InMemory;

var builder = WebApplication.CreateBuilder(args);

#if MISE
// MISE 
builder.Services.AddAuthentication(MiseAuthenticationDefaults.AuthenticationScheme)
    .AddMiseWithDefaultModules(builder.Configuration);
//builder.Services.AddAgentIdentities(); // not necessary if you use MISE v.20
#else

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
	.AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));
builder.Services.AddTokenAcquisition(true);
builder.Services.AddAgentIdentities();
builder.Services.AddMicrosoftGraph(); // Add Microsoft Graph client
builder.Services.AddDownstreamApis(builder.Configuration.GetSection("DownstreamApis"));
builder.Services.AddInMemoryTokenCaches();
#endif


// To be able to call Microsoft Graph API.
builder.Services.AddMicrosoftGraph();

// To enable Azure SDKs
builder.Services.AddMicrosoftIdentityAzureTokenCredential();

builder.Services.AddControllers();
var app = builder.Build();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
