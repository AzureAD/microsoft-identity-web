# Revised Service Registration Pattern

```csharp
builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"));

builder.Services.AddTokenAcquisition()
    .AddInMemoryTokenCaches();

builder.Services.AddDownstreamApis(builder.Configuration.GetSection("DownstreamApis"));
```