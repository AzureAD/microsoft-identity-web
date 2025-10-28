# Token Cache Troubleshooting

This guide helps you diagnose and resolve common token caching issues in Microsoft.Identity.Web applications.

---

## 📋 Quick Issue Index

- [L2 Cache Not Being Written](#l2-cache-not-being-written)
- [Deserialization Errors with Encryption](#deserialization-errors-with-encryption)
- [Memory Cache Growing Too Large](#memory-cache-growing-too-large)
- [Frequent MFA Prompts](#frequent-mfa-prompts)
- [Cache Connection Failures](#cache-connection-failures)
- [Token Cache Empty After Restart](#token-cache-empty-after-restart)
- [Session Cache Cookie Too Large](#session-cache-cookie-too-large)

---

## L2 Cache Not Being Written

### Symptoms

- Distributed cache (Redis, SQL, Cosmos DB) appears empty
- No entries visible in cache monitoring tools
- Application works but cache doesn't persist across restarts

###

 Root Causes

1. **L2 cache connection failure** - Most common cause
2. **Misconfigured cache options**
3. **Encryption key issues**
4. **Network connectivity problems**

### Diagnosis

Check application logs for errors similar to:

```text
fail: Microsoft.Identity.Web.TokenCacheProviders.Distributed.MsalDistributedTokenCacheAdapter[0]
      [MsIdWeb] DistributedCache: Connection issue. InRetry? False Error message:
      It was not possible to connect to the redis server(s).
      UnableToConnect on localhost:5002/Interactive, Initializing/NotStarted,
      last: NONE, origin: BeginConnectAsync, outstanding: 0, last-read: 2s ago,
      last-write: 2s ago, keep-alive: 60s, state: Connecting, mgr: 10 of 10 available,
      last-heartbeat: never, global: 9s ago, v: 2.2.4.27433
```

### Solution

**1. Verify cache configuration:**

```csharp
// Check connection string is correct
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "MyApp_";
});
```

**2. Test cache connectivity:**

```csharp
// Add this temporary code to test cache
var cache = app.Services.GetRequiredService<IDistributedCache>();
try
{
    await cache.SetStringAsync("test-key", "test-value");
    var value = await cache.GetStringAsync("test-key");
    Console.WriteLine($"Cache test successful: {value}");
}
catch (Exception ex)
{
    Console.WriteLine($"Cache test failed: {ex.Message}");
}
```

**3. Handle L2 failures gracefully:**

```csharp
builder.Services.Configure<MsalDistributedTokenCacheAdapterOptions>(options =>
{
    options.OnL2CacheFailure = (exception) =>
    {
        // Log the failure
        Console.WriteLine($"L2 cache failed: {exception.Message}");

        if (exception is StackExchange.Redis.RedisConnectionException)
        {
            // Attempt reconnection logic here if needed
            return true; // Retry the operation
        }

        return false; // Don't retry for other exceptions
    };
});
```

### Important Notes

**✅ L1 Cache Provides Resilience:**
- When L2 cache fails, Microsoft.Identity.Web automatically falls back to L1 (in-memory) cache
- Users can continue to sign in and call APIs without disruption
- L2 cache becomes eventually consistent when back online

**Verification:**
1. Check that `AddDistributedTokenCaches()` is called
2. Verify `IDistributedCache` implementation is registered
3. Confirm connection string and credentials are correct
4. Test network connectivity to cache endpoint

---

## Deserialization Errors with Encryption

### Symptoms

```text
ErrorCode: json_parse_failed
Microsoft.Identity.Client.MsalClientException:
MSAL V3 Deserialization failed to parse the cache contents.
```

Or:

```text
ErrorCode: json_parse_failed
Microsoft.Identity.Client.MsalClientException:
IDW10802: Exception occurred while deserializing token cache.
See https://aka.ms/msal-net-token-cache-serialization
```

### Root Causes

1. **Encryption keys not shared** across distributed system (most common)
2. **Certificate rotation** without proper key migration
3. **Mismatched encryption configuration** between servers

### Solution

**Critical:** Distributed systems do NOT share encryption keys by default!

#### Azure-Based Key Sharing (Recommended)

```csharp
using Microsoft.AspNetCore.DataProtection;
using Azure.Identity;

builder.Services.AddDataProtection()
    .PersistKeysToAzureBlobStorage(
        new Uri(builder.Configuration["DataProtection:BlobStorageUri"]))
    .ProtectKeysWithAzureKeyVault(
        new Uri(builder.Configuration["DataProtection:KeyVaultKeyUri"]),
        new DefaultAzureCredential());

// Enable encryption in token cache
builder.Services.Configure<MsalDistributedTokenCacheAdapterOptions>(options =>
{
    options.Encrypt = true;
});
```

**appsettings.json:**
```json
{
  "DataProtection": {
    "BlobStorageUri": "https://<storage-account>.blob.core.windows.net/<container>/<blob>",
    "KeyVaultKeyUri": "https://<key-vault>.vault.azure.net/keys/<key-name>"
  }
}
```

#### Certificate-Based Key Sharing

```csharp
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(@"\\server\share\keys"))
    .ProtectKeysWithCertificate(
        new X509Certificate2("current-cert.pfx", Configuration["CurrentCertPassword"]))
    .UnprotectKeysWithAnyCertificate(
        new X509Certificate2("current-cert.pfx", Configuration["CurrentCertPassword"]),
        new X509Certificate2("previous-cert.pfx", Configuration["PreviousCertPassword"]));
```

**✅ Certificate Rotation Best Practice:**
- Always include both **current** and **previous** certificates in `UnprotectKeysWithAnyCertificate()`
- This allows decryption of data protected with the old certificate during rotation

#### Testing Encryption Across Servers

```csharp
// Test on Server 1: Encrypt data
var protectionProvider = app.Services.GetRequiredService<IDataProtectionProvider>();
var protector = protectionProvider.CreateProtector("TestPurpose");
string protectedData = protector.Protect("test-message");
Console.WriteLine($"Protected: {protectedData}");

// Test on Server 2: Decrypt data
var protectionProvider2 = app.Services.GetRequiredService<IDataProtectionProvider>();
var protector2 = protectionProvider2.CreateProtector("TestPurpose");
string unprotectedData = protector2.Unprotect(protectedData);
Console.WriteLine($"Unprotected: {unprotectedData}");
```

### Reference

- [Data Protection in ASP.NET Core](https://learn.microsoft.com/aspnet/core/security/data-protection/introduction)
- [Key encryption at rest](https://learn.microsoft.com/aspnet/core/security/data-protection/implementation/key-encryption-at-rest)

---

## Memory Cache Growing Too Large

### Symptoms

- Application memory usage continuously increases
- Out of memory exceptions
- Server performance degrades over time
- Cache grows to gigabytes

### Root Causes

**Token Accumulation - Different Scenarios:**

#### Web Apps (user sign-in/sign-out):
- **User tokens:** ~7 KB each - Removed on sign-out via `RemoveAccountAsync()` ✅
- Memory growth is typically manageable since user tokens are cleaned up

#### Web APIs (OBO flow):
- **OBO tokens:** ~7 KB each - **NOT automatically removed** ❌
- Web APIs don't have user sign-in/sign-out—they receive tokens from client apps
- When web APIs call downstream APIs on behalf of users (OBO flow), OBO tokens accumulate

**Why OBO tokens accumulate in web API caches:**
1. User signs in to web app → web app gets user token
2. Web app calls web API → web API acquires OBO token to call downstream API
3. User signs out of web app → web app removes its user token via `RemoveAccountAsync()`
4. **Problem:** The OBO token in the web API's cache is NOT removed
5. User signs in again → new OBO token created, old one remains
6. Without eviction policies, these accumulate indefinitely in the web API's cache

**App tokens:**
- ~2 KB each - Short-lived, automatically managed ✅
- Minimal impact on memory

### Token Size Calculation Examples

**Scenario 1: Web API with OBO flow (most problematic):**
```
10,000 users × 3 downstream APIs × 7 KB per OBO token = 210 MB (current active OBO tokens)
After 5 user sign-in/sign-out cycles in web app: 1,050 MB (orphaned OBO tokens in web API)
With overhead: ~1.2-1.5 GB in the web API's cache
```

**Why this happens:**
- Each user sign-in/sign-out cycle in the **web app** creates new OBO tokens in the **web API**
- The web API has no knowledge of user sign-out events from the web app
- Old OBO tokens remain in the web API's cache indefinitely

**Scenario 2: Web app (without calling APIs with OBO):**
```
10,000 concurrent users × 7 KB per user token = 70 MB
(User tokens cleaned up on sign-out via RemoveAccountAsync)
With overhead: ~100-150 MB
```

### Solution

#### 1. Set L1 Cache Size Limit

```csharp
builder.Services.Configure<MsalDistributedTokenCacheAdapterOptions>(options =>
{
    // Limit L1 cache to 500 MB (default)
    options.L1CacheOptions.SizeLimit = 500 * 1024 * 1024;

    // For smaller deployments, reduce further
    options.L1CacheOptions.SizeLimit = 100 * 1024 * 1024; // 100 MB
});
```

#### 2. Configure Eviction Policies (Critical for Web APIs with OBO)

**These policies apply to ALL cache entries, including orphaned OBO tokens in web APIs:**

```csharp
// In your Web API's Program.cs or Startup.cs
builder.Services.Configure<MsalDistributedTokenCacheAdapterOptions>(options =>
{
    // Remove ALL entries (including OBO tokens) after 72 hours regardless of usage
    options.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(72);

    // Remove ALL entries (including OBO tokens) not accessed in 2 hours
    // RECOMMENDED for web APIs: Use SlidingExpiration
    options.SlidingExpiration = TimeSpan.FromHours(2);
});
```

**Why SlidingExpiration is recommended for web APIs with OBO:**
- Active users' OBO tokens remain cached (good performance for ongoing requests)
- Inactive users' orphaned OBO tokens are automatically removed after inactivity
- Default OBO token lifetime is 1 hour; set expiration to 2+ hours
- Balances cache hit rate with memory management

**Or via configuration:**

```json
{
  "TokenCacheOptions": {
    "AbsoluteExpirationRelativeToNow": "72:00:00",
    "SlidingExpiration": "02:00:00"
  }
}
```

```csharp
builder.Services.Configure<MsalDistributedTokenCacheAdapterOptions>(
    builder.Configuration.GetSection("TokenCacheOptions"));
```

#### 3. Disable L1 Cache (if needed)

If you cannot control memory growth and have session affinity:

```csharp
builder.Services.Configure<MsalDistributedTokenCacheAdapterOptions>(options =>
{
    // Forces all cache operations to L2 (slower but no memory growth)
    options.DisableL1Cache = true;
});
```

**Trade-off:**
- L1 cache access: <10ms
- L2 cache access: ~30-50ms
- Network call to Entra ID: >100ms

### Monitoring

Add logging to track cache size:

```csharp
var cache = app.Services.GetRequiredService<IMemoryCache>();
var cacheStats = cache.GetCurrentStatistics();
Console.WriteLine($"Current entries: {cacheStats?.CurrentEntryCount}");
Console.WriteLine($"Current size: {cacheStats?.CurrentEstimatedSize} bytes");
```

---

## Frequent MFA Prompts

### Symptoms

- Users prompted for MFA on every request or frequently
- MFA completed successfully but prompt appears again
- Occurs in multi-server deployments

### Root Cause

**Session affinity not configured** in load balancer:

1. Request → Server 1: MFA needed, user completes MFA, tokens cached in Server 1's L1 cache
2. Next request → Server 2: Reads its own L1 cache, finds old tokens (without MFA claims)
3. Result: User prompted for MFA again

### Solution

#### Option A: Enable Session Affinity (Recommended)

Configure your load balancer to route requests from the same user to the same server:

**Azure App Service:**
- Enable "ARR Affinity" (enabled by default)

**Azure Application Gateway:**
```json
{
  "backendHttpSettings": {
    "affinityCookieName": "ApplicationGatewayAffinity",
    "cookieBasedAffinity": "Enabled"
  }
}
```

**NGINX:**
```nginx
upstream backend {
    ip_hash; # Routes same IP to same server
    server server1.example.com;
    server server2.example.com;
}
```

#### Option B: Disable L1 Cache

If session affinity is not possible:

```csharp
builder.Services.Configure<MsalDistributedTokenCacheAdapterOptions>(options =>
{
    // All servers use L2 cache directly (consistent but slower)
    options.DisableL1Cache = true;
});
```

**Performance Impact:**
- L1: <10ms per cache operation
- L2: ~30-50ms per cache operation
- Trade-off for consistency across servers

### Verification

Test your load balancer configuration:

```bash
# Send multiple requests, check which server responds
for i in {1..10}; do
  curl -b cookies.txt -c cookies.txt https://your-app.com/api/test
done
```

Check for consistent `Server` or `X-Server-ID` headers.

---

## Cache Connection Failures

### Symptoms

```text
fail: Microsoft.Identity.Web.TokenCacheProviders.Distributed.MsalDistributedTokenCacheAdapter[0]
      [MsIdWeb] DistributedCache: Connection issue.
      RedisConnectionException: No connection is available to service this operation
```

### Common Causes

1. **Redis server not running**
2. **Incorrect connection string**
3. **Firewall blocking connection**
4. **SSL/TLS configuration mismatch**
5. **Connection pool exhausted**

### Solutions

#### 1. Verify Redis Connectivity

```bash
# Test Redis connection
redis-cli -h <host> -p <port> -a <password> ping
```

Expected: `PONG`

#### 2. Check Connection String

**Local Redis:**
```json
{
  "ConnectionStrings": {
    "Redis": "localhost:6379"
  }
}
```

**Azure Cache for Redis:**
```json
{
  "ConnectionStrings": {
    "Redis": "<cache-name>.redis.cache.windows.net:6380,password=<key>,ssl=True,abortConnect=False"
  }
}
```

**Connection string parameters:**
- `ssl=True` - Required for Azure Redis
- `abortConnect=False` - Allows retry on connection failure
- `connectTimeout=5000` - Connection timeout in milliseconds
- `syncTimeout=5000` - Operation timeout in milliseconds

#### 3. Handle Transient Failures

```csharp
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "MyApp_";

    // Configure connection options
    options.ConfigurationOptions = new StackExchange.Redis.ConfigurationOptions
    {
        AbortOnConnectFail = false,
        ConnectTimeout = 5000,
        SyncTimeout = 5000,
        EndPoints = { "<host>:<port>" },
        Password = "<password>",
        Ssl = true
    };
});

// Handle L2 cache failures
builder.Services.Configure<MsalDistributedTokenCacheAdapterOptions>(options =>
{
    options.OnL2CacheFailure = (exception) =>
    {
        // Log for monitoring
        Console.WriteLine($"Redis cache failure: {exception.Message}");

        // Retry for connection issues
        if (exception is StackExchange.Redis.RedisConnectionException)
        {
            return true; // Retry
        }

        return false; // Don't retry other exceptions
    };
});
```

#### 4. Monitor Cache Health

```csharp
// Add health check
builder.Services.AddHealthChecks()
    .AddRedis(builder.Configuration.GetConnectionString("Redis"));
```

---

## Token Cache Empty After Restart

### Symptoms

- Users must re-authenticate after application restart
- Cache appears empty after server restart
- Happens in production despite distributed cache

### Root Causes

1. **Using in-memory cache** instead of distributed cache
2. **L2 cache not properly configured**
3. **Distributed memory cache** (not persistent)

### Solution

#### Verify Distributed Cache Configuration

**❌ Wrong - Using in-memory:**
```csharp
// This cache is lost on restart
.AddInMemoryTokenCaches()
```

**❌ Wrong - Distributed memory (not persistent):**
```csharp
// This is NOT persistent across restarts
builder.Services.AddDistributedMemoryCache();
.AddDistributedTokenCaches()
```

**✅ Correct - Persistent distributed cache:**
```csharp
// Redis - persists across restarts
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "MyApp_";
});
.AddDistributedTokenCaches()
```

Or:

```csharp
// SQL Server - persists across restarts
builder.Services.AddDistributedSqlServerCache(options =>
{
    options.ConnectionString = builder.Configuration.GetConnectionString("TokenCacheDb");
    options.SchemaName = "dbo";
    options.TableName = "TokenCache";
    options.DefaultSlidingExpiration = TimeSpan.FromMinutes(90);
});
.AddDistributedTokenCaches()
```

---

## Session Cache Cookie Too Large

### Symptoms

```text
Error: Headers too large
HTTP 400 Bad Request
Cookie size exceeds maximum allowed
```

### Root Cause

Session cache stores tokens in cookies. Large ID tokens (many claims) cause cookies to exceed browser limits (typically 4KB per cookie).

### Solution

**❌ Don't use session cache** - Use distributed cache instead:

```csharp
// Replace this:
.AddSessionTokenCaches()

// With this:
.AddDistributedTokenCaches()

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "MyApp_";
});
```

### Why Session Cache Is Not Recommended

1. **Cookie size limitations** - Easily exceeded with many claims
2. **Scope issues** - Cannot use with singleton services (e.g., Microsoft Graph SDK)
3. **Performance** - Cookies sent with every request
4. **Security** - Sensitive data in cookies
5. **Scale** - Doesn't work well in load-balanced scenarios

---

## Advanced Debugging

### Enable Detailed Logging

**appsettings.json:**
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.Identity.Web": "Debug",
      "Microsoft.Identity.Web.TokenCacheProviders": "Trace"
    }
  }
}
```

### Inspect Cache Contents

```csharp
// For distributed cache
var cache = app.Services.GetRequiredService<IDistributedCache>();
var keys = /* implementation-specific way to list keys */;

foreach (var key in keys)
{
    var value = await cache.GetStringAsync(key);
    Console.WriteLine($"Key: {key}, Size: {value?.Length} bytes");
}
```

### Test Cache Serialization

```csharp
// Get the token cache adapter
var adapter = app.Services.GetRequiredService<IMsalTokenCacheProvider>();

// This will log serialization/deserialization details
// Look for errors in application logs
```

---

## Getting Help

If you're still experiencing issues:

1. **Check logs** - Enable Debug/Trace logging for Microsoft.Identity.Web
2. **Verify configuration** - Review all cache-related configuration
3. **Test connectivity** - Ensure cache infrastructure is accessible
4. **Monitor performance** - Use Application Insights or similar tools
5. **Review documentation** - [Token Cache Overview](README.md)

### Reporting Issues

When reporting token cache issues, include:

- Microsoft.Identity.Web version
- Cache implementation (Redis, SQL Server, etc.)
- Configuration code
- Error messages and stack traces
- Application logs with Debug level
- Infrastructure details (Azure, on-premises, etc.)

---

## Related Documentation

- [Token Cache Overview](README.md)
- [Distributed Cache Configuration](distributed.md)
- [Cache Eviction Strategies](eviction.md)
- [Data Protection & Encryption](https://learn.microsoft.com/aspnet/core/security/data-protection/)

---

**Last Updated:** October 27, 2025
**Microsoft.Identity.Web Version:** 3.14.1+
