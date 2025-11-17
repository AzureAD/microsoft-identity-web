# Certificate-Based Authentication

Certificates provide strong cryptographic proof of your application's identity when authenticating to Microsoft Entra ID (formerly Azure AD). Microsoft.Identity.Web supports multiple ways to load and use certificates, from production-ready Key Vault integration to development-friendly file-based approaches.

## Overview

### What is Certificate-Based Authentication?

Certificate-based authentication uses public-key cryptography to prove your application's identity. Your application signs a JSON Web Token (JWT) with its private key, and Microsoft Entra ID verifies the signature using the corresponding public key from your app registration.

### Why Use Certificates?

**Strong security:**
- ✅ Stronger than client secrets (asymmetric vs symmetric keys)
- ✅ Private key never transmitted over the network
- ✅ Cryptographic proof of identity
- ✅ Meets compliance requirements (FIPS, etc.)

**Production-ready:**
- ✅ Supported by security teams and IT operations
- ✅ Integrates with enterprise PKI infrastructure
- ✅ Hardware Security Module (HSM) support
- ✅ Industry-standard credential type

### Certificate vs Certificateless

| Aspect | Certificates | Certificateless (FIC+MSI) |
|--------|-------------|---------------------------|
| **Management** | Manual or automated | Fully automatic |
| **Rotation** | Required (manual or with tools) | Automatic |
| **Azure dependency** | No (works anywhere) | Yes (Azure only) |
| **Cost** | Certificate costs | No certificate costs |
| **Compliance** | Often required | May not meet all requirements |
| **Setup complexity** | Moderate to high | Low to moderate |

**When to use certificates:**
- ✅ Compliance requires certificate-based authentication
- ✅ Running outside Azure (on-premises, other clouds)
- ✅ Existing PKI infrastructure
- ✅ Organization policy mandates certificates

**When to use certificateless:**
- ✅ Running on Azure
- ✅ Want to minimize management overhead
- ✅ No specific certificate requirements

See [Certificateless Authentication](./certificateless.md) for the alternative approach.

---

## Certificate Types Supported

Microsoft.Identity.Web supports four ways to load certificates:

1. **[Azure Key Vault](#azure-key-vault)** ⭐ - Recommended for production
2. **[Certificate Store](#certificate-store)** - Windows production environments
3. **[File Path](#file-path)** - Development and simple deployments
4. **[Base64 Encoded](#base64-encoded)** - Configuration-embedded certificates

---

## Azure Key Vault

**Recommended for:** Production applications requiring centralized certificate management

### Why Key Vault?

**Centralized management:**
- ✅ Single source of truth for certificates
- ✅ Centralized access control and auditing
- ✅ Automatic certificate renewal support
- ✅ Versioning and rollback capabilities

**Security benefits:**
- ✅ Certificates never stored on disk
- ✅ Access controlled by Azure RBAC or access policies
- ✅ Activity logging and monitoring
- ✅ Integration with managed identities

**Operational benefits:**
- ✅ Works across platforms (Windows, Linux, containers)
- ✅ Share certificates across multiple applications
- ✅ No certificate management on app servers
- ✅ Simplified rotation and renewal

### Prerequisites

1. **Azure Key Vault** with a certificate
2. **Access permissions** for your application to read the certificate
3. **Network connectivity** from your application to Key Vault

### Configuration

#### JSON Configuration (appsettings.json)

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "your-tenant-id",
    "ClientId": "your-client-id",
    "ClientCredentials": [
      {
        "SourceType": "KeyVault",
        "KeyVaultUrl": "https://your-keyvault.vault.azure.net",
        "KeyVaultCertificateName": "YourCertificateName"
      }
    ]
  }
}
```

#### C# Code Configuration

```csharp
using Microsoft.Identity.Abstractions;

// Using property initialization
var credentialDescription = new CredentialDescription
{
    SourceType = CredentialSource.KeyVault,
    KeyVaultUrl = "https://your-keyvault.vault.azure.net",
    KeyVaultCertificateName = "YourCertificateName"
};

// Using helper method
var credentialDescription = CredentialDescription.FromKeyVault(
    "https://your-keyvault.vault.azure.net",
    "YourCertificateName");
```

#### ASP.NET Core Integration

```csharp
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;
using Microsoft.Identity.Abstractions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(options =>
    {
        options.Instance = "https://login.microsoftonline.com/";
        options.TenantId = "your-tenant-id";
        options.ClientId = "your-client-id";
        options.ClientCredentials = new[]
        {
            CredentialDescription.FromKeyVault(
                "https://your-keyvault.vault.azure.net",
                "YourCertificateName")
        };
    });
```

### Setup Guide

#### Step 1: Create or Import Certificate in Key Vault

**Using Azure Portal:**
1. Navigate to your Key Vault
2. Select **Certificates** from the left menu
3. Click **Generate/Import**
4. Choose **Generate** (for new) or **Import** (for existing)
5. Configure certificate properties:
   - **Name:** Descriptive name (e.g., "MyApp-Prod-Cert")
   - **Type:** Self-signed or CA-issued
   - **Subject:** CN=YourAppName
   - **Validity period:** 12-24 months
   - **Content type:** PFX
6. Click **Create**

**Using Azure CLI:**

```bash
# Generate self-signed certificate in Key Vault
az keyvault certificate create \
    --vault-name <keyvault-name> \
    --name <certificate-name> \
    --policy "$(az keyvault certificate get-default-policy)"

# Import existing certificate
az keyvault certificate import \
    --vault-name <keyvault-name> \
    --name <certificate-name> \
    --file /path/to/certificate.pfx \
    --password <cert-password>
```

#### Step 2: Grant Access to Your Application

**Option A: Using Managed Identity (Recommended)**

```bash
# Get your app's managed identity principal ID
PRINCIPAL_ID=$(az webapp identity show \
    --name <app-name> \
    --resource-group <resource-group> \
    --query principalId -o tsv)

# Grant access to certificates
az keyvault set-policy \
    --name <keyvault-name> \
    --object-id $PRINCIPAL_ID \
    --certificate-permissions get \
    --secret-permissions get
```

**Option B: Using Service Principal**

```bash
# Grant access using service principal
az keyvault set-policy \
    --name <keyvault-name> \
    --spn <application-client-id> \
    --certificate-permissions get \
    --secret-permissions get
```

**Option C: Using Azure RBAC**

```bash
# Get your app's managed identity principal ID
PRINCIPAL_ID=$(az webapp identity show \
    --name <app-name> \
    --resource-group <resource-group> \
    --query principalId -o tsv)

# Assign Key Vault Secrets User role
az role assignment create \
    --role "Key Vault Secrets User" \
    --assignee $PRINCIPAL_ID \
    --scope /subscriptions/<subscription-id>/resourceGroups/<resource-group>/providers/Microsoft.KeyVault/vaults/<keyvault-name>
```

#### Step 3: Upload Public Key to App Registration

1. Navigate to **Microsoft Entra ID** > **App registrations**
2. Select your application
3. Click **Certificates & secrets**
4. Under **Certificates** tab, click **Upload certificate**
5. Download the public key (.cer) from Key Vault:
   ```bash
   az keyvault certificate download \
       --vault-name <keyvault-name> \
       --name <certificate-name> \
       --file certificate.cer \
       --encoding DER
   ```
6. Upload the .cer file
7. Add a description and click **Add**

### Automatic Certificate Renewal

**Key Vault supports automatic certificate renewal:**

1. Configure renewal policy in Key Vault:
   ```bash
   az keyvault certificate set-attributes \
       --vault-name <keyvault-name> \
       --name <certificate-name> \
       --policy @policy.json
   ```

2. Example policy.json:
   ```json
   {
     "lifetimeActions": [
       {
         "trigger": {
           "daysBeforeExpiry": 30
         },
         "action": {
           "actionType": "AutoRenew"
         }
       }
     ],
     "issuerParameters": {
       "name": "Self"
     }
   }
   ```

3. Update app registration with new public key when renewed
4. Microsoft.Identity.Web automatically picks up the latest version from Key Vault

---

## Certificate Store

**Recommended for:** Production Windows applications using enterprise certificate management

### Why Certificate Store?

**Windows integration:**
- ✅ Native Windows certificate management
- ✅ IT-managed certificate lifecycle
- ✅ Hardware Security Module (HSM) support
- ✅ Group Policy deployment

**Enterprise scenarios:**
- ✅ Existing PKI infrastructure
- ✅ Centralized certificate management
- ✅ Compliance requirements
- ✅ On-premises deployments

### Certificate Store Locations

| Store Path | Description | Use When |
|------------|-------------|----------|
| **CurrentUser/My** | Current user's personal certificates | Service runs as user account |
| **LocalMachine/My** | Computer's personal certificates | Service runs as system account or service identity |
| **CurrentUser/Root** | Trusted root CAs (user) | Validating certificate chains |
| **LocalMachine/Root** | Trusted root CAs (computer) | System-level certificate trust |

### Configuration: Using Thumbprint

**Best for:** Static certificate deployment

#### JSON Configuration

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "your-tenant-id",
    "ClientId": "your-client-id",
    "ClientCredentials": [
      {
        "SourceType": "StoreWithThumbprint",
        "CertificateStorePath": "CurrentUser/My",
        "CertificateThumbprint": "A1B2C3D4E5F6789012345678901234567890ABCD"
      }
    ]
  }
}
```

#### C# Code Configuration

```csharp
using Microsoft.Identity.Abstractions;

var credentialDescription = CredentialDescription.FromCertificateStore(
    "CurrentUser/My",
    thumbprint: "A1B2C3D4E5F6789012345678901234567890ABCD");
```

**Note:** Thumbprint changes when certificate is renewed, requiring configuration updates.

### Configuration: Using Distinguished Name

**Best for:** Automatic certificate rotation

#### JSON Configuration

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "your-tenant-id",
    "ClientId": "your-client-id",
    "ClientCredentials": [
      {
        "SourceType": "StoreWithDistinguishedName",
        "CertificateStorePath": "CurrentUser/My",
        "CertificateDistinguishedName": "CN=MyAppCertificate"
      }
    ]
  }
}
```

#### C# Code Configuration

```csharp
using Microsoft.Identity.Abstractions;

var credentialDescription = CredentialDescription.FromCertificateStore(
    "CurrentUser/My",
    distinguishedName: "CN=MyAppCertificate");
```

**Benefit:** When certificate is renewed with the same distinguished name, Microsoft.Identity.Web automatically uses the newest certificate without configuration changes.

### Setup Guide

#### Step 1: Generate or Import Certificate

**Option A: Generate Self-Signed Certificate (Development)**

```powershell
# PowerShell: Generate self-signed certificate
$cert = New-SelfSignedCertificate `
    -Subject "CN=MyAppCertificate" `
    -CertStoreLocation "Cert:\CurrentUser\My" `
    -KeyExportPolicy Exportable `
    -KeySpec Signature `
    -KeyLength 2048 `
    -KeyAlgorithm RSA `
    -HashAlgorithm SHA256 `
    -NotAfter (Get-Date).AddYears(2)

# Export public key for app registration
Export-Certificate -Cert $cert -FilePath "MyAppCertificate.cer"

# View thumbprint
$cert.Thumbprint
```

**Option B: Import Existing Certificate**

```powershell
# PowerShell: Import PFX certificate
$pfxPath = "C:\path\to\certificate.pfx"
$pfxPassword = ConvertTo-SecureString -String "your-password" -Force -AsPlainText

Import-PfxCertificate `
    -FilePath $pfxPath `
    -CertStoreLocation "Cert:\CurrentUser\My" `
    -Password $pfxPassword
```

**Option C: Enterprise PKI Deployment**

Use Group Policy or SCCM to deploy certificates to target machines.

#### Step 2: Grant Application Access to Private Key

```powershell
# PowerShell: Grant IIS App Pool identity access to private key
$cert = Get-ChildItem -Path "Cert:\LocalMachine\My" | Where-Object {$_.Thumbprint -eq "YOUR_THUMBPRINT"}

$rsaCert = [System.Security.Cryptography.X509Certificates.RSACertificateExtensions]::GetRSAPrivateKey($cert)
$fileName = $rsaCert.Key.UniqueName

$path = "$env:ALLUSERSPROFILE\Microsoft\Crypto\RSA\MachineKeys\$fileName"

# Grant Read permission to IIS App Pool identity
icacls $path /grant "IIS APPPOOL\YourAppPoolName:R"
```

#### Step 3: Upload Public Key to App Registration

1. Navigate to **Microsoft Entra ID** > **App registrations**
2. Select your application
3. Click **Certificates & secrets**
4. Under **Certificates** tab, click **Upload certificate**
5. Upload the .cer file exported in Step 1
6. Add a description and click **Add**

### Certificate Rotation

**Using Distinguished Name (Recommended):**

1. Deploy new certificate with same CN to certificate store
2. Ensure new certificate is valid and not expired
3. Microsoft.Identity.Web automatically selects newest valid certificate
4. Remove old certificate after grace period

**Using Thumbprint:**

1. Deploy new certificate to certificate store
2. Update configuration with new thumbprint
3. Restart application
4. Remove old certificate

---

## File Path

**Recommended for:** Development, testing, and simple deployments

### Why File Path?

**Simple setup:**
- ✅ Easy to deploy certificate with application
- ✅ No external dependencies
- ✅ Works on any platform
- ✅ Container-friendly

**Development scenarios:**
- ✅ Local development
- ✅ Automated testing
- ✅ CI/CD pipelines (with secure file handling)
- ✅ Simple container deployments

**⚠️ Security Warning:** Not recommended for production. Use Key Vault or Certificate Store for production workloads.

### Configuration

#### JSON Configuration

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "your-tenant-id",
    "ClientId": "your-client-id",
    "ClientCredentials": [
      {
        "SourceType": "Path",
        "CertificateDiskPath": "/app/certificates/mycert.pfx",
        "CertificatePassword": "certificate-password"
      }
    ]
  }
}
```

#### C# Code Configuration

```csharp
using Microsoft.Identity.Abstractions;

var credentialDescription = CredentialDescription.FromCertificatePath(
    "/app/certificates/mycert.pfx",
    "certificate-password");
```

### Setup Guide

#### Step 1: Generate or Export Certificate

**Generate Self-Signed (Development):**

```bash
# Linux/macOS: Generate self-signed certificate with OpenSSL
openssl req -x509 -newkey rsa:2048 -keyout key.pem -out cert.pem -days 365 -nodes -subj "/CN=MyAppCertificate"

# Create PFX from PEM files
openssl pkcs12 -export -out mycert.pfx -inkey key.pem -in cert.pem -passout pass:your-password

# Extract public key for app registration
openssl pkcs12 -in mycert.pfx -clcerts -nokeys -out public-cert.cer -passin pass:your-password
```

**Windows PowerShell:**

```powershell
# Generate self-signed and export to PFX
$cert = New-SelfSignedCertificate `
    -Subject "CN=MyAppCertificate" `
    -CertStoreLocation "Cert:\CurrentUser\My" `
    -KeyExportPolicy Exportable `
    -KeySpec Signature

$pfxPassword = ConvertTo-SecureString -String "your-password" -Force -AsPlainText

Export-PfxCertificate -Cert $cert -FilePath "mycert.pfx" -Password $pfxPassword
Export-Certificate -Cert $cert -FilePath "public-cert.cer"
```

#### Step 2: Secure the Certificate File

**File permissions:**

```bash
# Linux: Restrict access to certificate file
chmod 600 /app/certificates/mycert.pfx
chown app-user:app-group /app/certificates/mycert.pfx
```

**Container secrets (Docker):**

```dockerfile
# Dockerfile: Copy certificate securely
COPY --chown=app:app certificates/mycert.pfx /app/certificates/
RUN chmod 600 /app/certificates/mycert.pfx
```

**Environment-specific paths:**

```json
{
  "AzureAd": {
    "ClientCredentials": [
      {
        "SourceType": "Path",
        "CertificateDiskPath": "/app/certificates/${ENVIRONMENT}-cert.pfx",
        "CertificatePassword": "${CERT_PASSWORD}"
      }
    ]
  }
}
```

#### Step 3: Upload Public Key to App Registration

(Same as Certificate Store Step 3)

### Security Best Practices for File-Based Certificates

**DO:**
- ✅ Use restrictive file permissions (600 on Linux, ACLs on Windows)
- ✅ Store password separately (Key Vault, environment variable, secrets manager)
- ✅ Encrypt file system where certificate is stored
- ✅ Use container secrets for containerized apps
- ✅ Rotate certificates regularly

**DON'T:**
- ❌ Commit certificates to source control
- ❌ Store passwords in plaintext configuration
- ❌ Use world-readable file permissions
- ❌ Leave certificates on disk after deployment (if possible to load into memory)
- ❌ Use in production (prefer Key Vault or Certificate Store)

---

## Base64 Encoded

**Recommended for:** Development, testing, and configuration-embedded certificates

### Why Base64 Encoded?

**Configuration simplicity:**
- ✅ Certificate embedded in configuration
- ✅ No file system dependency
- ✅ Easy to pass via environment variables
- ✅ Works in serverless environments

**Container scenarios:**
- ✅ Kubernetes secrets
- ✅ Docker environment variables
- ✅ Configuration management tools

**⚠️ Security Warning:** Not recommended for production. Secrets exposed in configuration files.

### Configuration

#### JSON Configuration

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "your-tenant-id",
    "ClientId": "your-client-id",
    "ClientCredentials": [
      {
        "SourceType": "Base64Encoded",
        "Base64EncodedValue": "MIIKcQIBAzCCCi0GCSqGSIb3DQEHAaCCCh4EggoaMIIKFjCCBg8GCSqGSIb3... (truncated)"
      }
    ]
  }
}
```

#### C# Code Configuration

```csharp
using Microsoft.Identity.Abstractions;

var credentialDescription = CredentialDescription.FromBase64String(
    "MIIKcQIBAzCCCi0GCSqGSIb3DQEHAaCCCh4EggoaMIIKFjCCBg8GCSqGSIb3... (truncated)");
```

#### Environment Variable Pattern

```bash
# Linux/macOS: Set certificate as environment variable
export CERT_BASE64=$(cat mycert.pfx | base64)

# Windows PowerShell
$certBytes = [System.IO.File]::ReadAllBytes("mycert.pfx")
$certBase64 = [System.Convert]::ToBase64String($certBytes)
[System.Environment]::SetEnvironmentVariable("CERT_BASE64", $certBase64, "User")
```

```csharp
// Read from environment variable in code
var certBase64 = Environment.GetEnvironmentVariable("CERT_BASE64");
var credentialDescription = CredentialDescription.FromBase64String(certBase64);
```

### Setup Guide

#### Step 1: Convert Certificate to Base64

**Linux/macOS:**

```bash
# Convert PFX to base64
base64 -i mycert.pfx -o mycert-base64.txt

# Or inline
CERT_BASE64=$(cat mycert.pfx | base64 | tr -d '\n')
echo $CERT_BASE64
```

**Windows PowerShell:**

```powershell
# Convert PFX to base64
$certBytes = [System.IO.File]::ReadAllBytes("mycert.pfx")
$certBase64 = [System.Convert]::ToBase64String($certBytes)
$certBase64 | Out-File -FilePath "mycert-base64.txt"
```

#### Step 2: Store Base64 String Securely

**Kubernetes Secret:**

```yaml
apiVersion: v1
kind: Secret
metadata:
  name: app-certificate
type: Opaque
data:
  certificate: <base64-encoded-certificate>
```

**Azure App Service Configuration:**

```bash
az webapp config appsettings set \
    --name <app-name> \
    --resource-group <resource-group> \
    --settings CERT_BASE64="<base64-encoded-certificate>"
```

**Docker Compose:**

```yaml
services:
  app:
    environment:
      - CERT_BASE64=${CERT_BASE64}
```

#### Step 3: Reference in Configuration

```json
{
  "AzureAd": {
    "ClientCredentials": [
      {
        "SourceType": "Base64Encoded",
        "Base64EncodedValue": "${CERT_BASE64}"
      }
    ]
  }
}
```

---

## Certificate Requirements

### Technical Requirements

**Supported algorithms:**
- ✅ RSA (2048-bit or higher recommended)
- ✅ ECDSA (P-256, P-384, P-521)

**Supported formats:**
- ✅ PFX/PKCS#12 (.pfx, .p12)
- ✅ PEM (for Key Vault and some scenarios)

**Certificate must include:**
- ✅ Private key
- ✅ Key usage: Digital Signature
- ✅ Extended key usage: Client Authentication (optional but recommended)

### App Registration Requirements

1. **Public key uploaded** to app registration (Certificates & secrets)
2. **Matching thumbprint** between uploaded public key and certificate used
3. **Valid certificate** (not expired, trusted chain)

---

## Multiple Certificates

You can configure multiple certificates for fallback or rotation scenarios:

```json
{
  "AzureAd": {
    "ClientCredentials": [
      {
        "SourceType": "KeyVault",
        "KeyVaultUrl": "https://keyvault.vault.azure.net",
        "KeyVaultCertificateName": "NewCertificate"
      },
      {
        "SourceType": "KeyVault",
        "KeyVaultUrl": "https://keyvault.vault.azure.net",
        "KeyVaultCertificateName": "OldCertificate"
      }
    ]
  }
}
```

Microsoft.Identity.Web tries certificates in order until one succeeds.

---

## Certificate Rotation Strategies

### Zero-Downtime Rotation

**Step 1:** Add new certificate

- Upload new certificate to Key Vault/Certificate Store
- Add new public key to app registration
- Keep old certificate active

**Step 2:** Deploy configuration with both certificates

```json
{
  "ClientCredentials": [
    { "SourceType": "KeyVault", "KeyVaultCertificateName": "NewCert" },
    { "SourceType": "KeyVault", "KeyVaultCertificateName": "OldCert" }
  ]
}
```

**Step 3:** Wait for all instances to update

- Verify new certificate works
- Monitor authentication success

**Step 4:** Remove old certificate

- Remove old certificate from configuration
- Remove old public key from app registration
- Delete old certificate from Key Vault/store

### Automated Rotation with Key Vault

1. Enable Key Vault auto-renewal
2. Use Distinguished Name in Certificate Store
3. Microsoft.Identity.Web automatically picks up new certificate
4. Update app registration with new public key (can be automated)

---

## Troubleshooting

### Problem: "Certificate not found"

**Possible causes:**
- Certificate doesn't exist at specified location
- Incorrect path, thumbprint, or distinguished name
- Permission issues accessing certificate

**Solutions:**
```bash
# Verify Key Vault certificate exists
az keyvault certificate show --vault-name <keyvault-name> --name <cert-name>

# Verify Certificate Store (PowerShell)
Get-ChildItem -Path "Cert:\CurrentUser\My" | Where-Object {$_.Thumbprint -eq "YOUR_THUMBPRINT"}

# Verify file exists
ls -la /path/to/certificate.pfx
```

### Problem: "The provided client credential is not valid"

**Possible causes:**
- Private key not accessible
- Certificate expired
- Wrong certificate used (thumbprint mismatch)
- Public key not uploaded to app registration

**Solutions:**
1. Verify certificate is valid:
   ```bash
   # Check expiration
   openssl pkcs12 -in mycert.pfx -nokeys -passin pass:password | openssl x509 -noout -dates
   ```
2. Verify thumbprint matches app registration
3. Check private key permissions
4. Ensure public key is uploaded to app registration

### Problem: "Access to Key Vault was denied"

**Possible causes:**
- Managed identity doesn't have permissions
- Access policy not configured
- Network connectivity issues

**Solutions:**
```bash
# Verify access policy
az keyvault show --name <keyvault-name> --query properties.accessPolicies

# Grant access
az keyvault set-policy --name <keyvault-name> --object-id <principal-id> --certificate-permissions get --secret-permissions get
```

### Problem: Certificate works locally but fails in production

**Possible causes:**
- Different certificate stores (CurrentUser vs LocalMachine)
- File path differences between environments
- Permission differences

**Solutions:**
1. Use environment-specific configuration
2. Verify certificate location in production
3. Check application identity permissions
4. Use Key Vault for consistent behavior across environments

---

## Security Best Practices

### Certificate Storage

- ✅ **Production:** Use Azure Key Vault or Hardware Security Module (HSM)
- ✅ **Windows:** Use LocalMachine store with proper ACLs
- ⚠️ **Development:** File-based with restricted permissions
- ❌ **Never:** Commit certificates to source control

### Key Protection

- ✅ Use strong private key encryption
- ✅ Limit private key access to necessary identities
- ✅ Enable audit logging for key access
- ✅ Consider HSM for highly sensitive scenarios

### Certificate Lifecycle

- ✅ Rotate certificates before expiration
- ✅ Use certificates with appropriate validity periods (12-24 months)
- ✅ Automate renewal where possible (Key Vault)
- ✅ Monitor expiration dates
- ✅ Test rotation procedures regularly

### Access Control

- ✅ Grant least-privilege permissions
- ✅ Use managed identities instead of service principals when possible
- ✅ Audit certificate access
- ✅ Review permissions regularly

---

## Additional Resources

- **[Azure Key Vault Certificates](https://learn.microsoft.com/azure/key-vault/certificates/about-certificates)** - Key Vault certificate documentation
- **[Certificate Management Best Practices](https://learn.microsoft.com/azure/active-directory/develop/howto-create-service-principal-portal#option-1-upload-a-certificate)** - Microsoft Entra ID guidance
- **[X.509 Certificates](https://learn.microsoft.com/azure/active-directory/develop/active-directory-certificate-credentials)** - Certificate credentials overview

---

## Next Steps

- **[Back to Credentials Overview](./README.md)** - Compare all credential types
- **[Certificateless Authentication](./certificateless.md)** - Alternative to certificates
- **[Client Secrets](./client-secrets.md)** - Simple authentication for development
- **[Calling Downstream APIs](../../calling-downstream-apis/README.md)** - Use certificates to call APIs

---

**Need help?** [Open an issue](https://github.com/AzureAD/microsoft-identity-web/issues) or check [troubleshooting guides](../../scenarios/web-apps/troubleshooting.md).