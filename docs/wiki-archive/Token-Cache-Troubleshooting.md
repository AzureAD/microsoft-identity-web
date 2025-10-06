## I configured a distributed (L2) cache but nothings gets written to it

This is most likely a configuration issue. When the L2 cache fails, Microsoft Identity Web will log an error, but proceed with the L1 cache. However, you might want to handle the error as soon as possible, so as to make sure persistence happens even if the app restarts. The error you'll see in the logs is similar to this one:

```Text
fail: Microsoft.Identity.Web.TokenCacheProviders.Distributed.MsalDistributedTokenCacheAdapter[0]
      [MsIdWeb] DistributedCache: Connection issue. InRetry? False Error message: It was not possible to connect to the redis server(s). UnableToConnect on localhost:5002/Interactive, Initializing/NotStarted, last: NONE, origin: BeginConnectAsync, outstanding: 0, last-read: 2s ago, last-write: 2s ago, keep-alive: 60s, state: Connecting, mgr: 10 of 10 available, last-heartbeat: never, global: 9s ago, v: 2.2.4.27433
```

However, because of the L1 cache support, the end user will have no disruption to their sign-in experience, being able to sign-in and call a downstream web API. The L2 cache, when back online, will be eventually consistent with the L1 cache.

As part of the `MsalDistributedTokenCacheAdapterOptions`, you can also take advantage of the `OnL2CacheFailure` property, which you'll add to the `Startup.cs` and can add custom code for handling the above error by examining the exception. You can tell the distributed cache adapter to retry (return `true`), or not (return `false`).

```csharp
services.Configure<MsalDistributedTokenCacheAdapterOptions>(options =>
{
    options.L1CacheOptions.SizeLimit = 10 * 1024 * 1024; // 10 Mb
    options.OnL2CacheFailure = (ex) =>
    {
        if (ex is StackExchange.Redis.RedisConnectionException)
        {
            // Attempt to act on the redis cache if at all possible?
            // Put here your reconnected code
            return true; // Retry
        } 
        return false;  // Don't retry.
    };
});
```


## I'm using encryption and I'm getting deserialization errors 

Deserialization errors could be:
```text
ErrorCode: json_parse_failed
Microsoft.Identity.Client.MsalClientException: MSAL V3 Deserialization failed to parse the cache contents. Is this possibly an earlier format needed for DeserializeMsalV2? (See https://aka.ms/msal-net-3x-cache-breaking-change).
```
```text
ErrorCode: json_parse_failed
Microsoft.Identity.Client.MsalClientException: IDW10802: Exception occurred while deserializing token cache. See https://aka.ms/msal-net-token-cache-serialization general guidance and https://aka.ms/ms-id-web/token-cache-troubleshooting for token cache troubleshooting information.
```

This is most likely a configuration problem related to encryption. **Be aware that distributed systems do not share encryption keys by default!** See 
[Key encryption at rest in Windows and Azure using ASP.NET Core](https://docs.microsoft.com/aspnet/core/security/data-protection/implementation/key-encryption-at-rest).

```csharp
// Example key sharing using Azure
services.AddDataProtection()
        .PersistKeysToAzureBlobStorage(new Uri("<blobUriWithSasToken>"))
        .ProtectKeysWithAzureKeyVault("<keyIdentifier>", "<clientId>", "<clientSecret>");
```

To help with certificate rotation, pass new and old certificate to [UnprotectKeysWithAnyCertificate](https://docs.microsoft.com/en-us/aspnet/core/security/data-protection/configuration/overview?view=aspnetcore-6.0#unprotectkeyswithanycertificate). Otherwise, if unprotecting with new certificate the data protected with the old certificate will not work and result in deserialization errors.
```csharp
// Example key sharing and protection using certificates
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(@"\\server\share\directory\"))
    .ProtectKeysWithCertificate(
        new X509Certificate2("certificate.pfx", builder.Configuration["CertificatePassword"]))
    .UnprotectKeysWithAnyCertificate(
        new X509Certificate2("certificate_1.pfx", builder.Configuration["CertificatePassword_1"]),
        new X509Certificate2("certificate_2.pfx", builder.Configuration["CertificatePassword_2"]));
```

To investigate encryption issues on a distributed system, try the following:

```csharp
// 1. configure the data protection 

// 2. get the data protector
 IDataProtectionProvider? dataProtectionProvider = serviceProvider.GetService(typeof(IDataProtectionProvider)) as IDataProtectionProvider;
var protector = dataProtectionProvider?.CreateProtector(DefaultPurpose);

// 3. use protector to encrypt and decrypt data
protector.Protect(message); // on machine 1
protector.Unprotect(message); // on machine 2
```

## My memory cache (L1) grows too much and crashes my server

App tokens are about 2KB in size. There will be a token for each tenant you need to access and for each resource you need to access. App tokens are automatically evicted.
User tokens are about 7KB in size. There will be a token for each: (user, tenant, resource). User tokens are not automatically evicted.

It is recommended to set eviction policies on both L1 and L2 caches.

See https://github.com/AzureAD/microsoft-identity-web/wiki/L1-Cache-in-Distributed-(L2)-Token-Cache#control-the-inmemory-l1-cache

## My users get prompted for MFA often even after they completed MFA

This can occur if your distributed system does not have session affinity. If you have 2 servers, the following can happen: 

1. Request goes to server 1. MFA is needed. User is prompted. User completes MFA. New tokens with MFA claims are stored in L1 and in L2 cache.
2. Request for the same user now goes to server 2. Server 2 reads its own L1 cache, where it finds a token without MFA claims. This leads to user being prompted for MFA again.

To fix this either:

- ensure session affinity is defined in your system, i.e. the same user hits the same server
- if not possible, it's better to disable the L1 cache. Note that L2 caches are slower, for example an L1 access is under 10ms while an L2 cache access is over 30ms. 

```csharp
services.Configure<MsalDistributedTokenCacheAdapterOptions>(options =>
{
     options.DisableL1Cache = true;
}
```

Note: a similar incident was reported where refresh tokens were expiring, prompting users to re-auth repeatedly.