# More performant L1/L2 token cache in Microsoft Identity Web > 1.8.0

## What?
Starting with Microsoft Identity Web 1.8.0, when connecting to a [Distributed cache](token-cache-serialization#distributed-token-cache) (L2=Level 2) cache, such as Redis, SQL or Cosmos DB, Microsoft Identity Web will enable an InMemory (L1=Level 1) cache. This enables a more reliable and much more performant cache lookup, as the L2 cache, being distributed, is slower. Moreover, the L2 cache can fail, for example, due to a connectivity issue. The L1 cache will enable your customers to continue to sign-in and call protected web APIs.

## Why?

Distributed token cache are less performant than memory, but they are more persistent. Also, when using a Distributed (L2) cache option, such as Redis or SQL, there can be issues with the L2 cache, such as the L2 cache is offline, and in versions of Microsoft Identity Web < 1.8.0 this would result in an app crash, unless handled by the developer.

## How do I get started?

If you are using Microsoft Identity Web > 1.8.0, and a [Distributed (L2) cache](https://github.com/AzureAD/microsoft-identity-web/wiki/token-cache-serialization#distributed-token-cache), Microsoft Identity Web will, by default, invoke the InMemory (L1) cache. As the developer, there is no work required on your end, this will happen automatically and you will benefit from reliability, increased performance and faster cache lookup with the L1 cache, while knowing the L2 cache is being populated. 

## Control the InMemory (L1) cache

Maybe you want to have fine grained control over the L1 cache? In the `MsalDistributedTokenCacheAdapterOptions`, you can set the `L1CacheOptions` which will be used by the distributed token cache adapter.

Controlling the size of the L1 cache is important. The L2 cache can grow a lot, but you probably want to control the impact of the L1 cache on the memory used by your app. We have defaulted the `SizeLimit` to 500 Mb, but you can set this value as you see fit for your app.

You would provide these options in `Startup.cs`:

```csharp
services.Configure<MsalDistributedTokenCacheAdapterOptions>(options =>
{
     options.L1CacheOptions.SizeLimit = 10 * 1024 * 1024; // 10 Mb
}

```

## Encryption

### Single machine system
```csharp
services.Configure<MsalDistributedTokenCacheAdapterOptions>(options =>
{
     options.Encrypt = true; // works for a single machine system
}

```

### Distributed systems

When using a distributed cache and you encrypt it, you want to share the encryption keys between the instance of your app (which might run on different machines). See [Key encryption at rest in Windows and Azure using ASP.NET Core](https://docs.microsoft.com/aspnet/core/security/data-protection/implementation/key-encryption-at-rest) for details and [Token Cache Troubleshooting](https://github.com/AzureAD/microsoft-identity-web/wiki/Token-Cache-Troubleshooting) for troubleshooting information.

```csharp
// key sharing using Azure
services.AddDataProtection()
        .PersistKeysToAzureBlobStorage(new Uri("<blobUriWithSasToken>"))
        .ProtectKeysWithAzureKeyVault("<keyIdentifier>", "<clientId>", "<clientSecret>");
```

```csharp
// key sharing and protection using certificates
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(@"\\server\share\directory\"))
    .ProtectKeysWithCertificate(
        new X509Certificate2("certificate.pfx", builder.Configuration["CertificatePassword"]))
    .UnprotectKeysWithAnyCertificate(
        new X509Certificate2("certificate_1.pfx", builder.Configuration["CertificatePassword_1"]),
        new X509Certificate2("certificate_2.pfx", builder.Configuration["CertificatePassword_2"]));
```