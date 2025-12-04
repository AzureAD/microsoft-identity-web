This article is for ASP.NET Core using the AddMicrosoftIdentityWebXX methods. If you want to use MSAL.NET directly, see [Token cache serialization for MSAL.NET](https://github.com/AzureAD/microsoft-identity-web/wiki/asp-net#token-cache-serialization-for-msalnet)

### ASP.NET Core web apps and APIs using AddMicrosoftIdentityWebApp / AddMicrosoftIdentityWebApi

For web apps that call web APIs and web APIs that call downstream APIs, the library provides several token cache serialization methods:

| Extension method | Microsoft.Identity.Web sub namespace | Description  |
| ---------------- | --------- | ------------ |
| `AddInMemoryTokenCaches` | `TokenCacheProviders.InMemory` | This implementation is great in samples. It's also good for production applications provided you don't mind if the token cache is lost when the web app is restarted. `AddInMemoryTokenCaches` takes an optional parameter of type `MsalMemoryTokenCacheOptions` that enables you to specify the duration after which the cache entry will expire unless it's used.
| `AddSessionTokenCaches` | `TokenCacheProviders.Session` | This token cache is bound to the user session. This option isn't ideal if the ID token is too large because it contains too many claims as the cookie would be too large.
| `AddDistributedTokenCaches` | `TokenCacheProviders.Distributed` | This token cache is for the ASP.NET Core `IDistributedCache` implementation, therefore enabling you to choose between a distributed memory cache, a Redis cache, a distributed NCache, Azure Cosmos DB or a SQL Server cache. For details about the `IDistributedCache` implementations, see [Distributed Memory Cache](https://docs.microsoft.com/aspnet/core/performance/caching/distributed#distributed-memory-cache) documentation.

### In-memory token cache

To use the in-memory token cache, update `Startup.cs`:

```CSharp
// or use a distributed Token Cache by adding
    services.AddMicrosoftIdentityWebAppAuthentication(Configuration);
            .EnableTokenAcquisitionToCallDownstreamApi(new string[] { scopesToRequest })
               .AddInMemoryTokenCaches();
```

`AddInMemoryTokenCache` also has an override taking an `Action<MemoryCacheOptions>` so that you can specify options of the Memory cache, in particular the size limit.

### Distributed token cache

> [!IMPORTANT]  
> Encryption at rest and secure access to the distributed cache are the responsibility of the application. See [encryption](https://github.com/AzureAD/microsoft-identity-web/wiki/L1-Cache-in-Distributed-(L2)-Token-Cache#encryption)

Examples of possible distributed caches:

```CSharp
// or use a distributed Token Cache by adding
    services.AddMicrosoftIdentityWebAppAuthentication(Configuration);
            .EnableTokenAcquisitionToCallDownstreamApi(new string[] { scopesToRequest })
               .AddDistributedTokenCaches();

// and then choose your implementation

// For instance the distributed in memory cache 
services.AddDistributedMemoryCache() // NOT RECOMMENDED FOR PRODUCTION! Use a persistent cache like Redis

// Or a Redis cache
services.AddStackExchangeRedisCache(options =>
{
 options.Configuration = "localhost";
 options.InstanceName = "SampleInstance";
});

// Or a Cosmos DB cache
services.AddCosmosCache((CosmosCacheOptions cacheOptions) =>
{
    cacheOptions.ContainerName = Configuration["CosmosCacheContainer"];
    cacheOptions.DatabaseName = Configuration["CosmosCacheDatabase"];
    cacheOptions.ClientBuilder = new CosmosClientBuilder(Configuration["CosmosConnectionString"]);
    cacheOptions.CreateIfNotExists = true;
});

// Or even a SQL Server token cache
services.AddDistributedSqlServerCache(options =>
{
 options.ConnectionString = _config["DistCache_ConnectionString"];
 options.SchemaName = "dbo";
 options.TableName = "TestCache";

 // You don't want the SQL token cache to be purged before the access token has expired. Usually
 // access tokens expire after 1 hours (but this can be changed by token lifetime policies), whereas
 // the default sliding expiration for the distributed SQL database is 20 mins. 
 // Use a value which is above 60 mins (or the lifetime of a token in case of longer lived tokens)
 options.DefaultSlidingExpiration = TimeSpan.FromMinutes(90);
});
```

Distributed token caches come with an L1 in-memory cache. For details see [L1 Cache in Distributed (L2) Token Cache](L1-Cache-in-Distributed-(L2)-Token-Cache)

### Session token cache

To use the session token cache, update `Startup.cs`:

- add `using Microsoft.Identity.Web.TokenCacheProviders.Session;`
- in the `ConfigureServices(IServiceCollection services)` method:
   - add the `services.AddSessionTokenCaches();` after `.EnableTokenAcquisitionToCallDownstreamApi();`
- in the `Configure(IApplicationBuilder app, IWebHostEnvironment env)`
   - add `app.UseSession();` before `app.UseAuthentication();`

**Note:** Because session token caches are added with scoped lifetime, they should not be used when `TokenAcquisition` is also used as a singleton (for example, when using Microsoft Graph SDK).

## Managing the eviction

To manage the eviction, you can change the properties of the `MsalDistributedTokenCacheAdapterOptions`. For instance

In `appsettings.json` you could add a new section

```JSon
  "RedisOptions": {
    "AbsoluteExpirationRelativeToNow":  "72:00:00"
  }
```

Which is then referenced in the `startup.cs` file:

```CSharp
services.Configure<MsalDistributedTokenCacheAdapterOptions>(Configuration.GetSection("RedisOptions"));
```

For more details on using these values, see [L2 cache eviction](Handle-L2-cache-eviction).

### Recommended expiration setting

See [Question - Recommended expiration setting](https://github.com/AzureAD/microsoft-identity-web/issues/786) for a discussion on the recommended expiry settings for the serialization. The idea is that if you set a value lower than the expiry of the token, the user will have to re-login, so you probably want to have a higher value. You probably don't want to have an infinite value for users who would never login again. In any case Microsoft.Identity.Web will trigger the user challenge in web apps if needed, so it's your design decision

### Compatibility with ADAL cache

Microsoft.Identity.Web uses MSAL.NET. ADAL.NET was the previous generation of authentication library, and MSAL.NET is capable of reading ADAL cache for migration scenarios. From Microsoft.Identity.Web 1.7.O, reading/writing the ADAL cache is disabled by default, so that your apps are more performant. If you really need to have compatibility with ADAL, just set the `LegacyCacheCompatibilityEnabled` property of `MicrosoftIdentityOptions` to `true` in your configuration.

## InMemory vs DistributedMemory cache options

`Distributed Memory Caching` is not recommended in production. As per the [official docs](https://docs.microsoft.com/en-us/aspnet/core/performance/caching/distributed?view=aspnetcore-6.0#distributed-memory-cache) it's useful for testing a prototyping, but this is simply a `MemoryCache` adapted to implement `IDistributedCache` interface. It is **not** persistent and it is **not** distributed!
