# Adding Support for Symmetric Keys in Microsoft.Identity.Web

## Overview
This proposal outlines the addition of symmetric key support for signing credentials in Microsoft.Identity.Web, allowing keys to be loaded from Key Vault or Base64 encoded strings while maintaining clean abstractions.

## Requirements
1. Support symmetric keys from:
   - Azure Key Vault secrets
   - Base64 encoded strings
2. Avoid circular dependencies with Microsoft.IdentityModel
3. Follow existing patterns in the codebase
4. Maintain backward compatibility

## Developer Experience
The implementation provides a straightforward and type-safe approach to working with symmetric keys while maintaining clean separation of concerns:

### Key Management
When working with symmetric keys, developers can utilize two primary sources:

1. **Azure Key Vault Integration**
   ```csharp
   var credentials = new CredentialDescription
   {
       SourceType = CredentialSource.SymmetricKeyFromKeyVault,
       KeyVaultUrl = "https://your-vault.vault.azure.net",
       KeyVaultSecretName = "your-secret-name"
   };
   ```

2. **Direct Base64 Encoded Keys**
   ```csharp
   var credentials = new CredentialDescription
   {
       SourceType = CredentialSource.SymmetricKeyBase64Encoded,
       Base64EncodedValue = "your-base64-encoded-key"
   };
   ```

### Implementation Details
- The DefaultCredentialLoader automatically selects the appropriate loader based on the SourceType
- Key material is loaded and converted to a SymmetricSecurityKey
- The security key is stored in the CachedValue property of CredentialDescription
- This design maintains independence from Microsoft.IdentityModel types in the abstractions layer
- The implementation follows the same pattern as certificate handling for consistency

## Design

### 1. New CredentialSource Values(Abstractions Layer)
```csharp
public enum CredentialSource
{
    // Existing values
    Certificate = 0,
    KeyVault = 1,
    Base64Encoded = 2,
    Path = 3,
    StoreWithThumbprint = 4,
    StoreWithDistinguishedName = 5,

    // New values
    SymmetricKeyFromKeyVault = 6,
    SymmetricKeyBase64Encoded = 7
}
```

### 2. SymmetricKeyDescription Class(IdWeb Layer)
Following the same pattern as CertificateDescription:

```csharp
public class SymmetricKeyDescription : CredentialDescription
{
    public static SymmetricKeyDescription FromKeyVault(string keyVaultUrl, string secretName)
    {
        return new SymmetricKeyDescription
        {
            SourceType = CredentialSource.SymmetricKeyFromKeyVault,
            KeyVaultUrl = keyVaultUrl,
            KeyVaultSecretName = secretName
        };
    }

    public static SymmetricKeyDescription FromBase64Encoded(string base64EncodedValue)
    {
        return new SymmetricKeyDescription
        {
            SourceType = CredentialSource.SymmetricKeyBase64Encoded,
            Base64EncodedValue = base64EncodedValue
        };
    }
}
```

### 3. New Loader Classes(IdWeb Layer)
Internal implementation in Microsoft.Identity.Web:

```csharp
internal class KeyVaultSymmetricKeyLoader : ICredentialSourceLoader
{
    private readonly SecretClient _secretClient;

    public KeyVaultSymmetricKeyLoader(SecretClient secretClient)
    {
        _secretClient = secretClient ?? throw new ArgumentNullException(nameof(secretClient));
    }

    public async Task LoadIfNeededAsync(CredentialDescription description, CredentialSourceLoaderParameters? parameters)
    {
        _ = Throws.IfNull(description);

        if (description.CachedValue != null)
            return;

        if (string.IsNullOrEmpty(description.KeyVaultUrl))
            throw new ArgumentException("KeyVaultUrl is required for KeyVault source");

        if (string.IsNullOrEmpty(description.KeyVaultSecretName))
            throw new ArgumentException("KeyVaultSecretName is required for KeyVault source");

        // Load secret from Key Vault
        var secret = await _secretClient.GetSecretAsync(description.KeyVaultSecretName).ConfigureAwait(false);
        if (secret?.Value == null)
            throw new InvalidOperationException($"Secret {description.KeyVaultSecretName} not found in Key Vault");

        try
        {
            // Convert secret value to bytes and create SymmetricSecurityKey
            var keyBytes = Convert.FromBase64String(secret.Value.Value);
            description.CachedValue = new SymmetricSecurityKey(keyBytes);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to create symmetric key from Key Vault secret: {ex.Message}", ex);
        }
    }
}

internal class Base64EncodedSymmetricKeyLoader : ICredentialSourceLoader
{
    public async Task LoadIfNeededAsync(CredentialDescription description, CredentialSourceLoaderParameters? parameters)
    {
        _ = Throws.IfNull(description);

        if (description.CachedValue != null)
            return;

        if (string.IsNullOrEmpty(description.Base64EncodedValue))
            throw new ArgumentException("Base64EncodedValue is required for Base64Encoded source");

        try
        {
            // Convert Base64 string to bytes and create SymmetricSecurityKey
            var keyBytes = Convert.FromBase64String(description.Base64EncodedValue);
            description.CachedValue = new SymmetricSecurityKey(keyBytes);
        }
        catch (Exception ex)
        {
            throw new FormatException("Invalid Base64 string for symmetric key", ex);
        }

        await Task.CompletedTask.ConfigureAwait(false);
    }
}
```

### 4. DefaultCredentialsLoader Changes(IdWeb Layer)
Update the loader to handle both certificate and symmetric key scenarios:

```csharp
public partial class DefaultCredentialsLoader : ICredentialsLoader, ISigningCredentialsLoader
{
    public DefaultCredentialsLoader(ILogger<DefaultCredentialsLoader>? logger)
    {
        _logger = logger ?? new NullLogger<DefaultCredentialsLoader>();

        CredentialSourceLoaders = new Dictionary<CredentialSource, ICredentialSourceLoader>
        {
            // Existing certificate loaders
            { CredentialSource.KeyVault, new KeyVaultCertificateLoader() },
            { CredentialSource.Path, new FromPathCertificateLoader() },
            { CredentialSource.StoreWithThumbprint, new StoreWithThumbprintCertificateLoader() },
            { CredentialSource.StoreWithDistinguishedName, new StoreWithDistinguishedNameCertificateLoader() },
            { CredentialSource.Base64Encoded, new Base64EncodedCertificateLoader() },

            // New symmetric key loaders
            { CredentialSource.SymmetricKeyFromKeyVault, new KeyVaultSymmetricKeyLoader(_secretClient) },
            { CredentialSource.SymmetricKeyBase64Encoded, new Base64EncodedSymmetricKeyLoader() }
        };
    }

    public async Task<SigningCredentials?> LoadSigningCredentialsAsync(
        CredentialDescription credentialDescription,
        CredentialSourceLoaderParameters? parameters = null)
    {
        _ = Throws.IfNull(credentialDescription);

        try
        {
            await LoadCredentialsIfNeededAsync(credentialDescription, parameters);

            if (credentialDescription.Certificate != null)
            {
                return new X509SigningCredentials(
                    credentialDescription.Certificate,
                    credentialDescription.Algorithm);
            }
            else if (credentialDescription.CachedValue is SymmetricSecurityKey key)
            {
                return new SigningCredentials(key, credentialDescription.Algorithm);
            }

            return null;
        }
        catch (Exception ex)
        {
            Logger.CredentialLoadingFailure(_logger, credentialDescription, ex);
            throw;
        }
    }
}
```
