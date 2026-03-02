---
name: entra-id-aspire-provisioning
description: |
  Provision Entra ID (Azure AD) app registrations for .NET Aspire applications and update configuration.
  Use after adding Microsoft.Identity.Web authentication code to create or update app registrations,
  configure scopes, credentials, and update appsettings.json files.
  Triggers: "provision entra id", "create app registration", "register azure ad app", 
  "configure entra id apps", "set up authentication apps".
---

# Entra ID Provisioning for .NET Aspire

Provision Entra ID app registrations for Aspire solutions and update `appsettings.json` configuration.

## Prerequisites

### Install Microsoft Graph PowerShell

```powershell
# Install the required modules (only if needed, one-time setup)
Install-Module Microsoft.Graph.Applications -Scope CurrentUser -Force
Install-Module Microsoft.Graph.Identity.SignIns -Scope CurrentUser -Force

# Note: Microsoft.Graph.Users is NOT required - this skill uses Invoke-MgGraphRequest
# to get current user info, which avoids module version compatibility issues.
```

### Connect to Microsoft Graph

```powershell
# Connect with required scopes
Connect-MgGraph -Scopes "Application.ReadWrite.All", "Directory.ReadWrite.All"

# Verify connection
Get-MgContext
```

> **Note**: You may be prompted to consent to permissions on first use.

## Provisioning Checklist

Use this checklist to verify all provisioning steps are complete:

### For Each Web API Project
- [ ] App registration created (or existing one found or user-provided)
- [ ] App ID URI set (`api://{clientId}`)
- [ ] `access_as_user` scope configured
- [ ] Service principal created
- [ ] Current user added as owner
- [ ] `appsettings.json` updated with `TenantId` and `ClientId`

### For Each Web App Project
- [ ] App registration created (or existing one found or user-provided)
- [ ] Redirect URIs configured (from `launchSettings.json`)
- [ ] Client secret generated and stored in user-secrets
- [ ] API permission added (to call the web API)
- [ ] Admin consent granted (or manual steps provided)
- [ ] Service principal created
- [ ] Current user added as owner
- [ ] `appsettings.json` updated with `TenantId`, `ClientId`, and `Scopes`

### Final Verification
- [ ] API provisioned **before** web app (web app needs API's ClientId and ScopeId)
- [ ] All `appsettings.json` files have real GUIDs (no placeholders)
- [ ] Client secret stored in user-secrets (not in `appsettings.json`)
- [ ] `Disconnect-MgGraph` called when done

## When to Use This Skill

Use this skill **after** the `entra-id-aspire-authentication` skill has added authentication code. This skill:
- Creates or updates Entra ID app registrations
- Configures App ID URIs and scopes for APIs
- Sets up redirect URIs for web apps
- Generates client secrets and stores them securely
- Updates `appsettings.json` with `TenantId`, `ClientId`, and scopes

## Workflow

### Step 1: Detect Project Types

Scan `Program.cs` files to identify which projects need app registrations:

```powershell
# Detect projects with Microsoft.Identity.Web
Get-ChildItem -Recurse -Filter "Program.cs" | ForEach-Object {
    $content = Get-Content $_.FullName -Raw
    $projectDir = Split-Path $_.FullName -Parent
    $projectName = Split-Path $projectDir -Leaf
    
    if ($content -match "AddMicrosoftIdentityWebApi") {
        Write-Host "API: $projectName"
    } elseif ($content -match "AddMicrosoftIdentityWebApp") {
        Write-Host "WebApp: $projectName"
    }
}
```

### Step 2: Gather Configuration

Before provisioning, the agent MUST gather required information interactively.

#### 2a. Get Tenant ID

First, detect the default tenant from the current connection if Microsoft Graph powershell is connected:

```powershell
$context = Get-MgContext
if ($context) {
    $defaultTenant = $context.TenantId
    Write-Host "Connected to tenant: $defaultTenant"
} else {
    Write-Host "Not connected. Run: Connect-MgGraph -TenantId '<tenant-id>' -Scopes 'Application.ReadWrite.All'"
}
```

**AGENT: Ask the user:**
> "I detected tenant ID `{defaultTenant}`. Should I use this tenant, or would you like to specify a different one?"

- If user confirms → use `$defaultTenant`
- If user provides different ID → use that value
- If not connected → instruct user to run `Connect-MgGraph` first

#### 2b. Check for Existing ClientIds in appsettings.json

Before asking about new vs. existing apps, scan `appsettings.json` files:

```powershell
# === Detect existing ClientIds from appsettings.json ===

$projects = @()

Get-ChildItem -Recurse -Filter "Program.cs" | ForEach-Object {
    $content = Get-Content $_.FullName -Raw
    $projectDir = Split-Path $_.FullName -Parent
    $projectName = Split-Path $projectDir -Leaf
    
    # Skip AppHost and ServiceDefaults
    if ($projectName -match "AppHost|ServiceDefaults") { return }
    
    $appSettingsPath = Join-Path $projectDir "appsettings.json"
    $existingClientId = $null
    $isPlaceholder = $false
    
    if (Test-Path $appSettingsPath) {
        $appSettings = Get-Content $appSettingsPath -Raw | ConvertFrom-Json
        if ($appSettings.AzureAd.ClientId) {
            $clientId = $appSettings.AzureAd.ClientId
            # Check if it's a placeholder value
            if ($clientId -match "^<.*>$" -or $clientId -match "YOUR_" -or $clientId -eq "") {
                $isPlaceholder = $true
            } else {
                $existingClientId = $clientId
            }
        }
    }
    
    $projectType = $null
    if ($content -match "AddMicrosoftIdentityWebApi") {
        $projectType = "API"
    } elseif ($content -match "AddMicrosoftIdentityWebApp") {
        $projectType = "WebApp"
    }
    
    if ($projectType) {
        $projects += @{
            Name = $projectName
            Path = $projectDir
            Type = $projectType
            ExistingClientId = $existingClientId
            IsPlaceholder = $isPlaceholder
        }
    }
}

# Output findings
$projects | ForEach-Object {
    if ($_.ExistingClientId) {
        Write-Host "$($_.Type): $($_.Name) - EXISTING ClientId: $($_.ExistingClientId)" -ForegroundColor Yellow
    } elseif ($_.IsPlaceholder) {
        Write-Host "$($_.Type): $($_.Name) - Placeholder ClientId (needs provisioning)" -ForegroundColor Cyan
    } else {
        Write-Host "$($_.Type): $($_.Name) - No ClientId configured" -ForegroundColor Cyan
    }
}
```

**AGENT: Based on findings, ask the user:**

**If existing ClientIds found:**
> "I found existing app registrations in your configuration:
> - **API** (`{apiProjectName}`): ClientId `{apiClientId}`
> - **Web App** (`{webProjectName}`): ClientId `{webClientId}`
> 
> Should I:
> 1. **Use these existing apps** and complement them if needed (add missing scopes, redirect URIs)?
> 2. **Create new app registrations** and update the configuration?"

**If only placeholders or no ClientIds:**
> "No existing app registrations found in `appsettings.json`. I'll create new ones."

- If user chooses **existing** → use the "Existing App Flow" section with detected ClientIds
- If user chooses **new** → proceed to Step 3

#### 2c. Confirm or Provide ClientIds

Based on the detection results, present options to the user:

**AGENT: Ask the user:**
> "I found the following configuration:
> - **API** (`{apiProjectName}`): {`ClientId: {id}` OR `No ClientId configured`}
> - **Web App** (`{webProjectName}`): {`ClientId: {id}` OR `No ClientId configured`}
> 
> What would you like to do?
> 1. **Create new app registrations** for projects without valid ClientIds
> 2. **Use existing app registrations** — provide ClientIds if not detected
> 3. **Replace all** — create new apps even if ClientIds exist"

**If user provides ClientIds manually:**
> "Please provide the ClientIds:
> - API ClientId: ___
> - Web App ClientId: ___"

Store the final decision:
```powershell
# Final configuration after user input
$apiConfig = @{
    ProjectName = "MyService.ApiService"
    ProjectPath = "path/to/api"
    ClientId = $null  # Or user-provided/detected GUID
    Action = "Create" # Or "UseExisting"
}

$webConfig = @{
    ProjectName = "MyService.Web"
    ProjectPath = "path/to/web"
    ClientId = $null  # Or user-provided/detected GUID
    Action = "Create" # Or "UseExisting"
}
```

**Decision logic:**
- If `Action = "Create"` → proceed to Step 3 (provision new app)
- If `Action = "UseExisting"` → use the "Existing App Flow" section with the ClientId (detected or user-provided)

> **Important for existing apps:**
> - **Web APIs**: The Existing App Flow checks for and adds `access_as_user` scope if missing
> - **Web Apps**: Run Step 5 (Discover Redirect URIs) first, then pass URIs to Existing App Flow to add any missing redirect URIs
> - **Both**: App ID URI and service principal are created if missing

### Step 3: Provision API App Registration

For each project with `AddMicrosoftIdentityWebApi`:

```powershell
# === Provision API App Registration ===

param(
    [Parameter(Mandatory=$true)][string]$TenantId,
    [Parameter(Mandatory=$true)][string]$DisplayName,
    [string]$SignInAudience = "AzureADMyOrg"
)

Write-Host "Creating API app registration: $DisplayName" -ForegroundColor Cyan

# Create the app registration
$apiApp = New-MgApplication -DisplayName $DisplayName -SignInAudience $SignInAudience

$apiClientId = $apiApp.AppId
$apiObjectId = $apiApp.Id

Write-Host "Created app: $apiClientId"

# Set App ID URI
$appIdUri = "api://$apiClientId"
Update-MgApplication -ApplicationId $apiObjectId -IdentifierUris @($appIdUri)
Write-Host "Set App ID URI: $appIdUri"

# Expose scope: access_as_user
$scopeId = [guid]::NewGuid().ToString()
$scope = @{
    Id = $scopeId
    AdminConsentDescription = "Allow the application to access $DisplayName on behalf of the signed-in user."
    AdminConsentDisplayName = "Access $DisplayName"
    IsEnabled = $true
    Type = "User"
    UserConsentDescription = "Allow the application to access $DisplayName on your behalf."
    UserConsentDisplayName = "Access $DisplayName"
    Value = "access_as_user"
}

$api = @{
    Oauth2PermissionScopes = @($scope)
}

Update-MgApplication -ApplicationId $apiObjectId -Api $api
Write-Host "Added scope: access_as_user (id: $scopeId)"

# Create service principal
New-MgServicePrincipal -AppId $apiClientId | Out-Null
Write-Host "Created service principal"

# Add current user as owner (using Invoke-MgGraphRequest for robustness - avoids module version issues)
$currentUser = Invoke-MgGraphRequest -Method GET -Uri "https://graph.microsoft.com/v1.0/me"
if ($currentUser) {
    $ownerRef = @{
        "@odata.id" = "https://graph.microsoft.com/v1.0/directoryObjects/$($currentUser.id)"
    }
    New-MgApplicationOwnerByRef -ApplicationId $apiObjectId -BodyParameter $ownerRef
    Write-Host "Added owner: $($currentUser.userPrincipalName)"
}

# Output for next steps
Write-Host ""
Write-Host "=== API Provisioning Complete ===" -ForegroundColor Green
Write-Host "ClientId: $apiClientId"
Write-Host "AppIdUri: $appIdUri"
Write-Host "ScopeId: $scopeId"
Write-Host "Owner: $($currentUser.userPrincipalName)"
```

### Step 4: Update API appsettings.json

Update the API project's `appsettings.json`:

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "<tenant-id>",
    "ClientId": "<api-client-id>",
    "Audiences": ["api://<api-client-id>"]
  }
}
```

### Step 5: Discover Redirect URIs

Parse `Properties/launchSettings.json` for the web project:

```powershell
# === Discover Redirect URIs ===

param(
    [Parameter(Mandatory=$true)][string]$ProjectPath
)

$launchSettingsPath = Join-Path $ProjectPath "Properties/launchSettings.json"
$launchSettings = Get-Content $launchSettingsPath | ConvertFrom-Json

$redirectUris = @()

foreach ($profile in $launchSettings.profiles.PSObject.Properties) {
    $appUrl = $profile.Value.applicationUrl
    if ($appUrl) {
        $urls = $appUrl -split ";"
        foreach ($url in $urls) {
            if ($url -match "^https://") {
                $redirectUris += "$url/signin-oidc"
            }
        }
    }
}

Write-Host "Redirect URIs: $($redirectUris -join ', ')"
$redirectUris
```

### Step 6: Provision Web App Registration

For each project with `AddMicrosoftIdentityWebApp`:

```powershell
# === Provision Web App Registration ===

param(
    [Parameter(Mandatory=$true)][string]$TenantId,
    [Parameter(Mandatory=$true)][string]$DisplayName,
    [Parameter(Mandatory=$true)][string]$ApiClientId,
    [Parameter(Mandatory=$true)][string]$ApiScopeId,
    [Parameter(Mandatory=$true)][string[]]$RedirectUris,
    [string]$SignInAudience = "AzureADMyOrg"
)

Write-Host "Creating Web app registration: $DisplayName" -ForegroundColor Cyan

# Configure web platform with redirect URIs and enable ID tokens
$webConfig = @{
    RedirectUris = $RedirectUris
    ImplicitGrantSettings = @{
        EnableIdTokenIssuance = $true
    }
}

# Create the app registration
$webApp = New-MgApplication `
    -DisplayName $DisplayName `
    -SignInAudience $SignInAudience `
    -Web $webConfig

$webClientId = $webApp.AppId
$webObjectId = $webApp.Id

Write-Host "Created app: $webClientId"

# Add API permission for access_as_user scope
# First, get the Microsoft Graph resource ID for the API
$apiServicePrincipal = Get-MgServicePrincipal -Filter "appId eq '$ApiClientId'"

$requiredResourceAccess = @{
    ResourceAppId = $ApiClientId
    ResourceAccess = @(
        @{
            Id = $ApiScopeId
            Type = "Scope"
        }
    )
}

Update-MgApplication -ApplicationId $webObjectId -RequiredResourceAccess @($requiredResourceAccess)
Write-Host "Added API permission for $ApiClientId"

# Create client secret
$passwordCredential = @{
    DisplayName = "dev-secret"
    EndDateTime = (Get-Date).AddYears(1)
}

$secret = Add-MgApplicationPassword -ApplicationId $webObjectId -PasswordCredential $passwordCredential
$secretValue = $secret.SecretText

Write-Host "Created client secret"

# Create service principal for the web app
New-MgServicePrincipal -AppId $webClientId | Out-Null
Write-Host "Created service principal"

# Add current user as owner (using Invoke-MgGraphRequest for robustness - avoids module version issues)
$currentUser = Invoke-MgGraphRequest -Method GET -Uri "https://graph.microsoft.com/v1.0/me"
if ($currentUser) {
    $ownerRef = @{
        "@odata.id" = "https://graph.microsoft.com/v1.0/directoryObjects/$($currentUser.id)"
    }
    New-MgApplicationOwnerByRef -ApplicationId $webObjectId -BodyParameter $ownerRef
    Write-Host "Added owner: $($currentUser.userPrincipalName)"
}

# Output for next steps
Write-Host ""
Write-Host "=== Web App Provisioning Complete ===" -ForegroundColor Green
Write-Host "ClientId: $webClientId"
Write-Host "Secret: $secretValue"
Write-Host "Owner: $($currentUser.userPrincipalName)"
Write-Host ""
Write-Host "IMPORTANT: Store this secret securely. It will not be shown again."
```

### Step 7: Store Secret in User Secrets

```powershell
# === Store secret in dotnet user-secrets ===

param(
    [Parameter(Mandatory=$true)][string]$ProjectPath,
    [Parameter(Mandatory=$true)][string]$Secret
)

Push-Location $ProjectPath

# Initialize user-secrets if needed
$csproj = Get-ChildItem -Filter "*.csproj" | Select-Object -First 1
$csprojContent = Get-Content $csproj.FullName -Raw

if ($csprojContent -notmatch "UserSecretsId") {
    dotnet user-secrets init
    Write-Host "Initialized user-secrets"
}

# Set the secret
dotnet user-secrets set "AzureAd:ClientSecret" $Secret
Write-Host "Stored ClientSecret in user-secrets"

Pop-Location
```

### Step 8: Update Web App appsettings.json

Update the web project's `appsettings.json`:

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "<tenant-id>",
    "ClientId": "<web-client-id>",
    "CallbackPath": "/signin-oidc"
  },
  "DownstreamApi": {
    "Scopes": ["api://<api-client-id>/.default"]
  }
}
```

> **Note**: The `ClientSecret` is stored in user-secrets, not in `appsettings.json`.

## Existing App Flow

When using an existing app registration (detected from `appsettings.json` or provided by user), this flow **complements** it by adding any missing configuration:

| Check | API | Web App |
|-------|-----|---------|
| App ID URI (`api://{clientId}`) | ✅ Add if missing | — |
| `access_as_user` scope | ✅ Add if missing | — |
| Redirect URIs | — | ✅ Add missing URIs |
| API Permission to call API | — | ✅ Add if missing |
| Service Principal | ✅ Create if missing | ✅ Create if missing || Owner (current user) | ✅ Add if not owner | ✅ Add if not owner |
### Complement Existing API App

```powershell
# === Complement Existing API App Registration ===

param(
    [Parameter(Mandatory=$true)][string]$ClientId
)

Write-Host "Fetching existing API app: $ClientId" -ForegroundColor Cyan

# Get the application by AppId
$app = Get-MgApplication -Filter "appId eq '$ClientId'"
$objectId = $app.Id

# Check App ID URI
if (-not $app.IdentifierUris -or $app.IdentifierUris.Count -eq 0) {
    Write-Host "Adding App ID URI..."
    Update-MgApplication -ApplicationId $objectId -IdentifierUris @("api://$ClientId")
}

# Check for access_as_user scope
$existingScope = $app.Api.Oauth2PermissionScopes | Where-Object { $_.Value -eq "access_as_user" }
$scopeId = $null

if (-not $existingScope) {
    Write-Host "Adding access_as_user scope..."
    $scopeId = [guid]::NewGuid().ToString()
    $displayName = $app.DisplayName ?? "API"
    
    # Get existing scopes and add new one
    $existingScopes = @($app.Api.Oauth2PermissionScopes)
    $newScope = @{
        Id = $scopeId
        AdminConsentDescription = "Allow access on behalf of signed-in user"
        AdminConsentDisplayName = "Access $displayName"
        IsEnabled = $true
        Type = "User"
        UserConsentDescription = "Allow access on your behalf"
        UserConsentDisplayName = "Access $displayName"
        Value = "access_as_user"
    }
    
    $api = @{
        Oauth2PermissionScopes = $existingScopes + $newScope
    }
    
    Update-MgApplication -ApplicationId $objectId -Api $api
} else {
    $scopeId = $existingScope.Id
    Write-Host "access_as_user scope already exists (id: $scopeId)"
}

# Check service principal
$sp = Get-MgServicePrincipal -Filter "appId eq '$ClientId'" -ErrorAction SilentlyContinue
if (-not $sp) {
    New-MgServicePrincipal -AppId $ClientId | Out-Null
    Write-Host "Created service principal"
}

# Check and add current user as owner if not already (using Invoke-MgGraphRequest for robustness)
$currentUser = Invoke-MgGraphRequest -Method GET -Uri "https://graph.microsoft.com/v1.0/me"
if ($currentUser) {
    $existingOwners = Get-MgApplicationOwner -ApplicationId $objectId
    $isOwner = $existingOwners | Where-Object { $_.Id -eq $currentUser.id }
    if (-not $isOwner) {
        $ownerRef = @{
            "@odata.id" = "https://graph.microsoft.com/v1.0/directoryObjects/$($currentUser.id)"
        }
        New-MgApplicationOwnerByRef -ApplicationId $objectId -BodyParameter $ownerRef
        Write-Host "Added owner: $($currentUser.userPrincipalName)"
    } else {
        Write-Host "Current user is already an owner"
    }
}

Write-Host "API app registration updated" -ForegroundColor Green
Write-Host "ScopeId: $scopeId"

# Return scope ID for web app configuration
$scopeId
```

### Complement Existing Web App

```powershell
# === Complement Existing Web App Registration ===

param(
    [Parameter(Mandatory=$true)][string]$ClientId,
    [Parameter(Mandatory=$true)][string]$ApiClientId,
    [Parameter(Mandatory=$true)][string]$ApiScopeId,
    [string[]]$RequiredRedirectUris = @()
)

Write-Host "Fetching existing Web app: $ClientId" -ForegroundColor Cyan

# Get the application by AppId
$app = Get-MgApplication -Filter "appId eq '$ClientId'"
$objectId = $app.Id

# Check redirect URIs
if ($RequiredRedirectUris.Count -gt 0) {
    $existingUris = @($app.Web.RedirectUris)
    $missingUris = $RequiredRedirectUris | Where-Object { $_ -notin $existingUris }
    if ($missingUris.Count -gt 0) {
        Write-Host "Adding missing redirect URIs: $($missingUris -join ', ')"
        $allUris = $existingUris + $missingUris
        
        $webConfig = @{
            RedirectUris = $allUris
            ImplicitGrantSettings = @{
                EnableIdTokenIssuance = $true
            }
        }
        
        Update-MgApplication -ApplicationId $objectId -Web $webConfig
    } else {
        Write-Host "All redirect URIs already configured"
    }
}

# Check API permission
$existingPermission = $app.RequiredResourceAccess | Where-Object { $_.ResourceAppId -eq $ApiClientId }
if (-not $existingPermission) {
    Write-Host "Adding API permission for $ApiClientId..."
    
    $requiredResourceAccess = @{
        ResourceAppId = $ApiClientId
        ResourceAccess = @(
            @{
                Id = $ApiScopeId
                Type = "Scope"
            }
        )
    }
    
    # Preserve existing permissions and add new one
    $allPermissions = @($app.RequiredResourceAccess) + $requiredResourceAccess
    Update-MgApplication -ApplicationId $objectId -RequiredResourceAccess $allPermissions
    Write-Host "Added API permission"
} else {
    Write-Host "API permission already configured"
}

# Check service principal
$sp = Get-MgServicePrincipal -Filter "appId eq '$ClientId'" -ErrorAction SilentlyContinue
if (-not $sp) {
    $sp = New-MgServicePrincipal -AppId $ClientId
    Write-Host "Created service principal"
}

# Check and add current user as owner if not already (using Invoke-MgGraphRequest for robustness)
$currentUser = Invoke-MgGraphRequest -Method GET -Uri "https://graph.microsoft.com/v1.0/me"
if ($currentUser) {
    $existingOwners = Get-MgApplicationOwner -ApplicationId $objectId
    $isOwner = $existingOwners | Where-Object { $_.Id -eq $currentUser.id }
    if (-not $isOwner) {
        $ownerRef = @{
            "@odata.id" = "https://graph.microsoft.com/v1.0/directoryObjects/$($currentUser.id)"
        }
        New-MgApplicationOwnerByRef -ApplicationId $objectId -BodyParameter $ownerRef
        Write-Host "Added owner: $($currentUser.userPrincipalName)"
    } else {
        Write-Host "Current user is already an owner"
    }
}

# Grant admin consent for the web app to call the API
Write-Host "Attempting to grant admin consent for API access..."
try {
    $apiSp = Get-MgServicePrincipal -Filter "appId eq '$ApiClientId'"
    
    # Check if consent already exists
    $existingGrant = Get-MgOauth2PermissionGrant -Filter "clientId eq '$($sp.Id)' and resourceId eq '$($apiSp.Id)'" -ErrorAction SilentlyContinue
    
    if (-not $existingGrant) {
        $grant = @{
            ClientId = $sp.Id
            ConsentType = "AllPrincipals"
            ResourceId = $apiSp.Id
            Scope = "access_as_user"
        }
        New-MgOauth2PermissionGrant -BodyParameter $grant | Out-Null
        Write-Host "Admin consent granted successfully" -ForegroundColor Green
    } else {
        Write-Host "Admin consent already exists"
    }
} catch {
    Write-Host ""
    Write-Host "⚠️  Could not grant admin consent automatically." -ForegroundColor Yellow
    Write-Host "   This requires DelegatedPermissionGrant.ReadWrite.All permission." -ForegroundColor Yellow
    Write-Host ""
    Write-Host "   To grant consent manually:" -ForegroundColor Cyan
    Write-Host "   1. Go to Azure Portal > Entra ID > App registrations" -ForegroundColor Cyan
    Write-Host "   2. Select the web app: $($app.DisplayName)" -ForegroundColor Cyan
    Write-Host "   3. Go to 'API permissions'" -ForegroundColor Cyan
    Write-Host "   4. Click 'Grant admin consent for [tenant]'" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "   Alternatively, users will be prompted for consent on first sign-in." -ForegroundColor Cyan
    Write-Host ""
}

Write-Host "Web app registration updated" -ForegroundColor Green
```

## Error Handling: Admin Script Fallback

If the user lacks permissions, generate a script for an admin:

```powershell
# === Generate Admin Script ===

$scriptContent = @"
# ============================================================
# Admin Script: Entra ID App Provisioning
# ============================================================
# This script requires Application Administrator or Global Administrator role.
# Generated: $(Get-Date -Format "yyyy-MM-dd HH:mm")
# Solution: $SolutionName
# Tenant: $TenantId
# ============================================================

# Prerequisites - run once
# Install-Module Microsoft.Graph.Applications -Scope CurrentUser -Force

# Connect with admin privileges
Connect-MgGraph -Scopes "Application.ReadWrite.All", "Directory.ReadWrite.All"

Write-Host "Provisioning Entra ID apps..." -ForegroundColor Cyan

# [Full provisioning script content here]

Write-Host ""
Write-Host "=== PROVISIONING COMPLETE ===" -ForegroundColor Green
Write-Host "API ClientId: `$apiClientId"
Write-Host "Web ClientId: `$webClientId"
Write-Host ""
Write-Host "Please provide these values to the developer."

# Cleanup
Disconnect-MgGraph
"@

$scriptPath = "entra-provision-admin.ps1"
$scriptContent | Out-File -FilePath $scriptPath -Encoding UTF8
Write-Host "Admin script saved to: $scriptPath" -ForegroundColor Yellow
```

## Configuration Reference

### API appsettings.json

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "YOUR_TENANT_ID",
    "ClientId": "YOUR_API_CLIENT_ID",
    "Audiences": ["api://YOUR_API_CLIENT_ID"]
  }
}
```

### Web App appsettings.json

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "YOUR_TENANT_ID",
    "ClientId": "YOUR_WEB_CLIENT_ID",
    "CallbackPath": "/signin-oidc"
  },
  "DownstreamApi": {
    "Scopes": ["api://YOUR_API_CLIENT_ID/.default"]
  }
}
```

## Best Practices

1. **Provision API first** — Web app needs the API's Client ID and scope ID
2. **Use `.default` scope** — Safer for downstream API calls in composed scenarios
3. **Store secrets in user-secrets** — Never commit secrets to source control
4. **Single tenant by default** — Use `AzureADMyOrg`; switch to `AzureADMultipleOrgs` only when needed
5. **Parse launchSettings.json** — Get accurate redirect URIs for all launch profiles
6. **Complement, don't duplicate** — When using existing apps, only add what's missing
7. **Disconnect when done** — Run `Disconnect-MgGraph` after provisioning

## Related

- [Entra ID Aspire Authentication Skill](../entra-id-aspire-authentication/SKILL.md) — Code wiring (run first)
- [Aspire Framework Docs](../../docs/frameworks/aspire.md) — Full integration guide
- [Microsoft Graph PowerShell SDK](https://learn.microsoft.com/powershell/microsoftgraph/) — Reference
- [New-MgApplication](https://learn.microsoft.com/powershell/module/microsoft.graph.applications/new-mgapplication) — App registration cmdlet
