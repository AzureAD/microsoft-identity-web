# Support for ASP.NET classic, .NET 4.7.2, and .NET Standard 2.0

Starting with version 1.17+, you have the option of using either Microsoft.Identity.Web, which brings specific ASP.NET Core dependencies, or one or both of the following:

**Microsoft.Identity.Web.TokenCache**
- Token cache serializers and adapters for MSAL.NET
- ClaimsPrincipalExtension which add extensions methods to a ClaimsPrincipal. See [Utility classes](utility-classes)

**Microsoft.Identity.Web.Certificate**
- Helper methods to load certificates

By using the **Microsoft.Identity.Web.TokenCache** or **Microsoft.Identity.Web.Certificate** NuGet packages, you have the advantage of fewer dependencies and .NET Standard 2.0 support. See [package dependencies](https://github.com/AzureAD/microsoft-identity-web/wiki/NuGet-package-references) for more info.

## Token cache serialization for MSAL.NET

### Principle

The principle is the same as in ASP.NET Core.

```CSharp
#using Microsoft.Identity.Web
```

```CSharp

 private static IConfidentialClientApplication app;
 public static async Task<IConfidentialClientApplication> BuildConfidentialClientApplication()
 {
  if (app== null)
  {
     // Create the confidential client application
     app= ConfidentialClientApplicationBuilder.Create(clientId)
       // Alternatively to the certificate you can use .WithClientSecret(clientSecret)
       .WithCertificate(certDescription.Certificate)
       .WithTenantId(tenant)
       .Build();

     // Add an in-memory token cache. Other options available: see below
     app.UseInMemoryTokenCaches();
   }
   return clientapp;
  }
```

### Other serialization technologies

#### In memory token cache

```CSharp 
     // Add an in-memory token cache
     app.AddInMemoryTokenCache();
```

#### In memory token cache with MemoryCacheOptions

Available in Microsoft.Identity.Web 1.20, to handle eviction and size options.

```CSharp
    // In memory token caches (App and User caches)
    app.AddInMemoryTokenCache(services =>
    {
        // Configure the memory cache options
        services.Configure<MemoryCacheOptions>(options =>
        {
             options.SizeLimit = 5000000; // in bytes (5 Mb)
         });
     });
```

#### Distributed in memory token cache

```CSharp 
     // In memory distributed token cache
     app.UseDistributedTokenCaches(services =>
     {
       // In net462/net472, requires to reference Microsoft.Extensions.Caching.Memory
       services.AddDistributedMemoryCache();
     });
```

#### SQL server

```CSharp 
     // SQL Server token cache
     app.UseDistributedTokenCaches(services =>
     {
      services.AddDistributedSqlServerCache(options =>
      {
       // In net462/net472, requires to reference Microsoft.Extensions.Caching.Memory

       // Requires to reference Microsoft.Extensions.Caching.SqlServer
       options.ConnectionString = @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=TestCache;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False";
       options.SchemaName = "dbo";
       options.TableName = "TestCache";

       // You don't want the SQL token cache to be purged before the access token has expired. Usually
       // access tokens expire after 1 hour (but this can be changed by token lifetime policies), whereas
       // the default sliding expiration for the distributed SQL database is 20 mins. 
       // Use a value which is above 60 mins (or the lifetime of a token in case of longer lived tokens)
       options.DefaultSlidingExpiration = TimeSpan.FromMinutes(90);
      });
     });
```

#### Redis cache

```CSharp 
     // Redis token cache
     app.UseDistributedTokenCaches(services =>
     {
       // Requires to reference Microsoft.Extensions.Caching.StackExchangeRedis
       services.AddStackExchangeRedisCache(options =>
       {
         options.Configuration = "localhost";
         options.InstanceName = "Redis";
       });
      });
```

#### Cosmos DB

```CSharp 
      // Cosmos DB token cache
      app.UseDistributedTokenCaches(services =>
      {
        // Requires to reference Microsoft.Extensions.Caching.Cosmos (preview)
        services.AddCosmosCache((CosmosCacheOptions cacheOptions) =>
        {
          cacheOptions.ContainerName = Configuration["CosmosCacheContainer"];
          cacheOptions.DatabaseName = Configuration["CosmosCacheDatabase"];
          cacheOptions.ClientBuilder = new CosmosClientBuilder(Configuration["CosmosConnectionString"]);
          cacheOptions.CreateIfNotExists = true;
        });
       });
```

See [Token cache serialization](token-cache-serialization) for details on the other token cache providers/serializers

### Sample
- Using this cache in a .NET Framework and .NET Core (not ASP.NET) application is showed-cased in this sample [ConfidentialClientTokenCache](https://github.com/Azure-Samples/active-directory-dotnet-v1-to-v2/tree/master/ConfidentialClientTokenCache) 
- The following sample is an ASP.NET web app using the same technics: https://github.com/Azure-Samples/ms-identity-aspnet-webapp-openidconnect (See [WebApp/Utils/MsalAppBuilder.cs](https://github.com/Azure-Samples/ms-identity-aspnet-webapp-openidconnect/blob/master/WebApp/Utils/MsalAppBuilder.cs)

## Help loading certificates

Microsoft.Identity.Web 1.6.0 and later expose the `DefaultCertificateLoader` class to .NET framework. 

```CSharp
 // Certificate
 string keyVaultContainer = "https://WebAppsApisTests.vault.azure.net";
 string keyVaultReference = "MsIdWebScenarioTestCert";
 CertificateDescription certDescription = CertificateDescription.FromKeyVault(keyVaultContainer, keyVaultReference);
 ICertificateLoader certificateLoader = new DefaultCertificateLoader();
 certificateLoader.LoadIfNeeded(certDescription);

 // Create the confidential client application
 IConfidentialClientApplication app;
 app = ConfidentialClientApplicationBuilder.Create(clientId)
        .WithCertificate(certDescription.Certificate)
        .WithTenantId(tenant)
        .Build();
```

For details, see:
- the [Using certicates](https://github.com/AzureAD/microsoft-identity-web/wiki/Certificates) article for details.
- the [ConfidentialClientTokenCache](https://github.com/Azure-Samples/active-directory-dotnet-v1-to-v2/tree/master/ConfidentialClientTokenCache) which showcases loading a certificate from KeyVault.

## Some of the samples illustrating token cache serialization or certificates in .NET Framework apps
Sample | Platform | Description
------ | -------- | -----------
[active-directory-dotnet-v1-to-v2](https://github.com/Azure-Samples/active-directory-dotnet-v1-to-v2) | Desktop (Console) | Visual Studio solution illustrating the migration of Azure AD v1.0 applications (using ADAL.NET) to Azure AD v2.0 applications, also named converged applications (using MSAL.NET), in particular [ConfidentialClientTokenCache](https://github.com/Azure-Samples/active-directory-dotnet-v1-to-v2/tree/master/ConfidentialClientTokenCache)
[ms-identity-aspnet-webapp-openidconnect](https://github.com/Azure-Samples/ms-identity-aspnet-webapp-openidconnect) | ASP.NET (net472) | Example of token cache serialization in an ASP.NET MVC application (using MSAL.NET). See in particular [MsalAppBuilder](https://github.com/Azure-Samples/ms-identity-aspnet-webapp-openidconnect/blob/master/WebApp/Utils/MsalAppBuilder.cs)
[active-directory-dotnetcore-daemon-v2](https://github.com/Azure-Samples/active-directory-dotnetcore-daemon-v2/blob/907c54a6b5b945d6b4fca28a01b29dd98d773119/3-Using-KeyVault/daemon-console/Program.cs#L44-L45) | .NET Core (Console) | Part of the daemon tutorial, this chapter shows how to have a daemon using certificates acquired from KeyVault.