# Calling Azure SDKs with MicrosoftIdentityTokenCredential

This guide explains how to use `MicrosoftIdentityTokenCredential` from Microsoft.Identity.Web.Azure to authenticate Azure SDK clients (Storage, KeyVault, ServiceBus, etc.) with Microsoft Identity.

## Overview

The `MicrosoftIdentityTokenCredential` class implements Azure SDK's `TokenCredential` interface, enabling seamless integration between Microsoft.Identity.Web and Azure SDK clients. This allows you to use the same authentication configuration and token caching infrastructure across your entire application.

### Benefits

- **Unified Authentication**: Use the same auth configuration for web apps, APIs, and Azure services
- **Token Caching**: Automatic token caching and refresh
- **Delegated & App Permissions**: Support for both user and application tokens
- **Agent Identities**: Compatible with agent identities feature
- **Managed Identity**: Seamless integration with Azure Managed Identity

## Installation

Install the Azure integration package:

```bash
dotnet add package Microsoft.Identity.Web.Azure
```

Then install the Azure SDK client packages you need:

```bash
# Examples
dotnet add package Azure.Storage.Blobs
dotnet add package Azure.Security.KeyVault.Secrets
dotnet add package Azure.Messaging.ServiceBus
dotnet add package Azure.Data.Tables
```

## ASP.NET Core Setup

### 1. Configure Services

Add Azure token credential support:

```csharp
using Microsoft.Identity.Web;

var builder = WebApplication.CreateBuilder(args);

// Add authentication
builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"))
    .EnableTokenAcquisitionToCallDownstreamApi()
    .AddInMemoryTokenCaches();

// Add Azure token credential support
builder.Services.AddMicrosoftIdentityAzureTokenCredential();

builder.Services.AddControllersWithViews();

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
```

### 2. Configure appsettings.json

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "your-tenant-id",
    "ClientId": "your-client-id",
    "ClientCredentials": [
      {
        "SourceType": "SignedAssertionFromManagedIdentity"
      }
    ]
  }
}
```

## Using MicrosoftIdentityTokenCredential

### Inject and Use with Azure SDK Clients

This code sample shows how to use MicrosoftIdentityTokenCredential with the Blob Storage. The same principle applies to all Azure SDKs

```csharp
using Azure.Storage.Blobs;
using Microsoft.Identity.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Authorize]
public class StorageController : Controller
{
    private readonly MicrosoftIdentityTokenCredential _credential;
    private readonly IConfiguration _configuration;
    
    public StorageController(
        MicrosoftIdentityTokenCredential credential,
        IConfiguration configuration)
    {
        _credential = credential;
        _configuration = configuration;
    }
    
    public async Task<IActionResult> ListBlobs()
    {
        // Create Azure SDK client with credential
        var blobClient = new BlobServiceClient(
            new Uri($"https://{_configuration["StorageAccountName"]}.blob.core.windows.net"),
            _credential);
        
        var container = blobClient.GetBlobContainerClient("mycontainer");
        var blobs = new List<string>();
        
        await foreach (var blob in container.GetBlobsAsync())
        {
            blobs.Add(blob.Name);
        }
        
        return View(blobs);
    }
}
```

## Delegated Permissions (User Tokens)

Call Azure services on behalf of signed-in user.

### Azure Storage Example

```csharp
using Azure.Storage.Blobs;
using Microsoft.Identity.Web;

[Authorize]
public class FileController : Controller
{
    private readonly MicrosoftIdentityTokenCredential _credential;
    
    public FileController(MicrosoftIdentityTokenCredential credential)
    {
        _credential = credential;
    }
    
    public async Task<IActionResult> UploadFile(IFormFile file)
    {
        // Credential will automatically acquire delegated token
        var blobClient = new BlobServiceClient(
            new Uri("https://myaccount.blob.core.windows.net"),
            _credential);
        
        var container = blobClient.GetBlobContainerClient("uploads");
        await container.CreateIfNotExistsAsync();
        
        var blob = container.GetBlobClient(file.FileName);
        await blob.UploadAsync(file.OpenReadStream(), overwrite: true);
        
        return Ok($"File {file.FileName} uploaded");
    }
}
```


## Application Permissions (App-Only Tokens)

Call Azure services with application permissions (no user context).

### Configuration

```csharp
public class AzureService
{
    private readonly MicrosoftIdentityTokenCredential _credential;
    
    public AzureService(MicrosoftIdentityTokenCredential credential)
    {
        _credential = credential;
    }
    
    public async Task<List<string>> ListBlobsAsync()
    {
        // Configure credential for app-only token
        _credential.Options.RequestAppToken = true;
        
        var blobClient = new BlobServiceClient(
            new Uri("https://myaccount.blob.core.windows.net"),
            _credential);
        
        var container = blobClient.GetBlobContainerClient("data");
        var blobs = new List<string>();
        
        await foreach (var blob in container.GetBlobsAsync())
        {
            blobs.Add(blob.Name);
        }
        
        return blobs;
    }
}
```

### Daemon Application Example

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;
using Azure.Storage.Blobs;

class Program
{
    static async Task Main(string[] args)
    {
        // Build service provider
        var tokenAcquirerFactory = TokenAcquirerFactory.GetDefaultInstance();
        tokenAcquirerFactory.Services.AddMicrosoftIdentityAzureTokenCredential();
        var sp = tokenAcquirerFactory.Build();
        
        // Get credential
        var credential = sp.GetRequiredService<MicrosoftIdentityTokenCredential>();
        credential.Options.RequestAppToken = true;
        
        // Use with Azure SDK
        var blobClient = new BlobServiceClient(
            new Uri("https://myaccount.blob.core.windows.net"),
            credential);
        
        var container = blobClient.GetBlobContainerClient("data");
        
        await foreach (var blob in container.GetBlobsAsync())
        {
            Console.WriteLine($"Blob: {blob.Name}");
        }
    }
}
```

## Using with Agent Identities

`MicrosoftIdentityTokenCredential` supports agent identities through the `Options` property:

```csharp
using Microsoft.Identity.Web;

public class AgentService
{
    private readonly MicrosoftIdentityTokenCredential _credential;
    
    public AgentService(MicrosoftIdentityTokenCredential credential)
    {
        _credential = credential;
    }
    
    public async Task<List<string>> ListBlobsForAgentAsync(string agentIdentity)
    {
        // Configure for agent identity
        _credential.Options.WithAgentIdentity(agentIdentity);
        _credential.Options.RequestAppToken = true;
        
        var blobClient = new BlobServiceClient(
            new Uri("https://myaccount.blob.core.windows.net"),
            _credential);
        
        var container = blobClient.GetBlobContainerClient("agent-data");
        var blobs = new List<string>();
        
        await foreach (var blob in container.GetBlobsAsync())
        {
            blobs.Add(blob.Name);
        }
        
        return blobs;
    }
    
    public async Task<string> GetSecretForAgentUserAsync(string agentIdentity, Guid userOid, string secretName)
    {
        // Configure for agent user identity
        _credential.Options.WithAgentUserIdentity(agentIdentity, userOid);
        
        var secretClient = new SecretClient(
            new Uri("https://myvault.vault.azure.net"),
            _credential);
        
        var secret = await secretClient.GetSecretAsync(secretName);
        return secret.Value.Value;
    }
}
```

See [Agent Identities documentation](../scenarios/agent-identities/README.md) for more details.

## FIC+Managed Identity Integration

`MicrosoftIdentityTokenCredential` works seamlessly with FIC+Azure Managed Identity:

### Configuration for Managed Identity

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "your-tenant-id",
    "ClientId": "your-client-id",
    "ClientCredentials": [
      {
        "SourceType": "SignedAssertionFromManagedIdentity"
      }
    ]
  }
}
```

### Using System-Assigned Managed Identity

```csharp
// No additional code needed!
// When deployed to Azure, the credential automatically uses managed identity

public class StorageService
{
    private readonly MicrosoftIdentityTokenCredential _credential;
    
    public StorageService(MicrosoftIdentityTokenCredential credential)
    {
        _credential = credential;
        _credential.Options.RequestAppToken = true;
    }
    
    public async Task<List<string>> ListContainersAsync()
    {
        // Uses managed identity when running in Azure
        var blobClient = new BlobServiceClient(
            new Uri("https://myaccount.blob.core.windows.net"),
            _credential);
        
        var containers = new List<string>();
        await foreach (var container in blobClient.GetBlobContainersAsync())
        {
            containers.Add(container.Name);
        }
        
        return containers;
    }
}
```

### Using User-Assigned Managed Identity

```json
{
  "AzureAd": {
    "ClientCredentials": [
      {
        "SourceType": "SignedAssertionFromManagedIdentity",
        "ManagedIdentityClientId": "user-assigned-identity-client-id"
      }
    ]
  }
}
```

## OWIN Implementation

For ASP.NET applications using OWIN:

```csharp
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.OWIN;
using Owin;

public class Startup
{
    public void Configuration(IAppBuilder app)
    {
     app.SetDefaultSignInAsAuthenticationType(CookieAuthenticationDefaults.AuthenticationType);
     app.UseCookieAuthentication(new CookieAuthenticationOptions());

     OwinTokenAcquirerFactory factory = TokenAcquirerFactory.GetDefaultInstance<OwinTokenAcquirerFactory>();

     app.AddMicrosoftIdentityWebApp(factory);
     factory.Services
        .AddMicrosoftIdentityAzureTokenCredential();
      factory.Build();
    }
}
```

## Best Practices

### 1. Reuse Azure SDK Clients

Azure SDK clients are thread-safe and should be reused, but `MicrosoftIdentityTokenCredential` is a scoped service. You can't use it with AddAzureServices() which creates singletons.

### 2. Use Managed Identity in Production

```csharp
// ✅ Good: Certificateless auth with managed identity
{
  "ClientCredentials": [
    {
      "SourceType": "SignedAssertionFromManagedIdentity"
    }
  ]
}
```

### 3. Handle Azure SDK Exceptions

```csharp
using Azure;

try
{
    var blob = await blobClient.DownloadAsync();
}
catch (RequestFailedException ex) when (ex.Status == 404)
{
    // Blob not found
}
catch (RequestFailedException ex) when (ex.Status == 403)
{
    // Insufficient permissions
}
catch (RequestFailedException ex)
{
    _logger.LogError(ex, "Azure SDK call failed with status {Status}", ex.Status);
}
```

### 5. Use Configuration for URIs

```csharp
// ❌ Bad: Hardcoded URIs
var blobClient = new BlobServiceClient(new Uri("https://myaccount.blob.core.windows.net"), credential);

// ✅ Good: Configuration-driven
var storageUri = _configuration["Azure:Storage:Uri"];
var blobClient = new BlobServiceClient(new Uri(storageUri), credential);
```

## Troubleshooting

### Error: "ManagedIdentityCredential authentication failed"

**Cause**: Managed identity not enabled or misconfigured.

**Solution**:
- Enable managed identity on Azure resource (App Service, VM, etc.)
- For user-assigned identity, specify `ManagedIdentityClientId`
- Verify identity has required role assignments

### Error: "This request is not authorized to perform this operation"

**Cause**: Missing Azure RBAC role assignment.

**Solution**:
- Assign appropriate role to managed identity or user
- Example: "Storage Blob Data Contributor" for blob operations
- Wait up to 5 minutes for role assignments to propagate

### Token Acquisition Fails Locally

**Cause**: Managed identity only works in Azure.

**Solution**: Use different credential source locally:

```json
{
  "ClientCredentials": [
    {
      "SourceType": "ClientSecret",
      "ClientSecret": "secret-for-local-dev"
    }
  ]
}
```

### Scope Errors with Azure Resources

**Cause**: Incorrect scope format.

**Solution**: Use Azure resource-specific scopes:
- Storage: `https://storage.azure.com/user_impersonation` or `.default`
- KeyVault: `https://vault.azure.net/user_impersonation` or `.default`
- Service Bus: `https://servicebus.azure.net/user_impersonation` or `.default`

## Related Documentation

- [Azure SDK for .NET Documentation](https://learn.microsoft.com/dotnet/azure/sdk/overview)
- [Managed Identity Documentation](https://learn.microsoft.com/azure/active-directory/managed-identities-azure-resources/overview)
- [Credentials Configuration](../authentication/credentials/README.md)
- [Agent Identities](../scenarios/agent-identities/README.md)
- [Calling Downstream APIs Overview](README.md)

---

**Next Steps**: Learn about [calling custom APIs](custom-apis.md) with IDownstreamApi and IAuthorizationHeaderProvider.
