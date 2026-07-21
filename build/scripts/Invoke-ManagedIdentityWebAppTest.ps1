<#
.SYNOPSIS
    Verifies the Easy Auth-protected ManagedIdentityWebApp endpoint for system- and user-assigned MI.

.DESCRIPTION
    Acquires an app-only Entra token as the Easy Auth app registration using the LabAuth client
    certificate (certificate-based client credentials), then calls the protected endpoint with a
    bearer token for both system-assigned and (optionally) user-assigned managed identity, asserting
    the web app successfully acquired a managed-identity token. Easy Auth rejects unauthenticated
    callers with HTTP 401.

.PARAMETER ClientId
    App (client) ID of the Easy Auth app registration.

.PARAMETER TenantId
    Tenant (directory) ID that hosts the app registration and web app.

.PARAMETER WebAppName
    Name of the App Service (without the .azurewebsites.net suffix).

.PARAMETER ResourceUri
    Resource URI the web app should acquire a managed-identity token for.

.PARAMETER UserAssignedClientId
    Optional client ID of a user-assigned managed identity to additionally verify.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)] [string] $ClientId,
    [Parameter(Mandatory = $true)] [string] $TenantId,
    [Parameter(Mandatory = $true)] [string] $WebAppName,
    [Parameter(Mandatory = $true)] [string] $ResourceUri,
    [string] $UserAssignedClientId
)

$ErrorActionPreference = 'Stop'

$pfxPath = $env:LABAUTH_PFX_PATH
if ([string]::IsNullOrWhiteSpace($pfxPath)) {
    throw "The 'LABAUTH_PFX_PATH' environment variable is empty. It should point to the LabAuth PFX installed by template-install-dependencies.yaml (e.g. `$(Build.SourcesDirectory)\TestCert.pfx)."
}

function ConvertTo-Base64Url {
    param([byte[]] $Bytes)
    return [Convert]::ToBase64String($Bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_')
}

# Load the certificate for signing only, keeping the private key in memory (EphemeralKeySet).
$cert = [System.Security.Cryptography.X509Certificates.X509Certificate2]::new(
    $pfxPath, '', [System.Security.Cryptography.X509Certificates.X509KeyStorageFlags]::EphemeralKeySet)

$tokenEndpoint = "https://login.microsoftonline.com/$TenantId/oauth2/v2.0/token"
$now = [DateTimeOffset]::UtcNow.ToUnixTimeSeconds()

$header = @{ alg = 'RS256'; typ = 'JWT'; x5t = (ConvertTo-Base64Url $cert.GetCertHash()) } | ConvertTo-Json -Compress
$payload = @{
    aud = $tokenEndpoint; iss = $ClientId; sub = $ClientId
    jti = [guid]::NewGuid().ToString(); nbf = $now; exp = $now + 600
} | ConvertTo-Json -Compress

$unsigned = (ConvertTo-Base64Url ([System.Text.Encoding]::UTF8.GetBytes($header))) + '.' +
            (ConvertTo-Base64Url ([System.Text.Encoding]::UTF8.GetBytes($payload)))

$rsa = [System.Security.Cryptography.X509Certificates.RSACertificateExtensions]::GetRSAPrivateKey($cert)
if ($null -eq $rsa) {
    throw 'Unable to load the RSA private key from the LabAuth certificate. Ensure the PFX contains an accessible private key.'
}
$signature = $rsa.SignData(
    [System.Text.Encoding]::UTF8.GetBytes($unsigned),
    [System.Security.Cryptography.HashAlgorithmName]::SHA256,
    [System.Security.Cryptography.RSASignaturePadding]::Pkcs1)
$clientAssertion = $unsigned + '.' + (ConvertTo-Base64Url $signature)

$tokenRequestBody = @{
    client_id             = $ClientId
    scope                 = "api://$ClientId/.default"
    grant_type            = 'client_credentials'
    client_assertion_type = 'urn:ietf:params:oauth:client-assertion-type:jwt-bearer'
    client_assertion      = $clientAssertion
}

$tokenResponse = Invoke-RestMethod -Uri $tokenEndpoint -Method Post -Body $tokenRequestBody -ContentType 'application/x-www-form-urlencoded'
$accessToken = $tokenResponse.access_token
if ([string]::IsNullOrEmpty($accessToken)) {
    throw 'Failed to acquire an access token for the Easy Auth app registration.'
}

$baseUrl = "https://$WebAppName.azurewebsites.net/AppService?resourceuri=$ResourceUri"
$authHeaders = @{ Authorization = "Bearer $accessToken" }

function Assert-EasyAuthChallenge {
    param([string] $Url)

    # Easy Auth must reject unauthenticated callers with HTTP 401 *before* the request reaches the
    # app. If an unauthenticated request succeeds, the endpoint is not actually protected.
    for ($attempt = 1; $attempt -le 6; $attempt++) {
        $code = $null
        try {
            $unauth = Invoke-WebRequest -Uri $Url -Method Get -UseBasicParsing -TimeoutSec 60
            $code = [int] $unauth.StatusCode
        }
        catch {
            if ($null -ne $_.Exception.Response) {
                $code = [int] $_.Exception.Response.StatusCode
                if ($code -eq 401) {
                    Write-Host 'Easy Auth OK: unauthenticated request returned 401.'
                    return
                }
            }
            Write-Host "Easy Auth check attempt ${attempt}: status '$code' (warming up?), retrying..."
            Start-Sleep -Seconds 20
            continue
        }

        # No exception => a 2xx/3xx without credentials => the endpoint is NOT protected.
        throw "Easy Auth check FAILED: unauthenticated request returned $code (endpoint is not protected by Easy Auth)."
    }

    throw 'Easy Auth check FAILED: never observed a 401 for an unauthenticated request after multiple attempts.'
}

function Invoke-MiEndpoint {
    param(
        [string] $Label,
        [string] $Url,
        [string] $ExpectedAppId,
        [string] $ForbiddenAppId
    )

    Write-Host "[$Label] Calling: $Url"
    $lastInfo = $null

    # Allow the freshly deployed app to warm up; retry transient failures and not-yet-ready bodies.
    for ($attempt = 1; $attempt -le 6; $attempt++) {
        $response = $null
        try {
            $response = Invoke-RestMethod -Uri $Url -Method Get -Headers $authHeaders -TimeoutSec 120
        }
        catch {
            $lastInfo = $_.Exception.Message
            Write-Host "[$Label] attempt $attempt failed: $lastInfo"
            Start-Sleep -Seconds 20
            continue
        }

        $body = "$response"
        if ($body -notmatch 'Access token received') {
            # App reached but managed-identity acquisition failed (or app still warming up): retry.
            $lastInfo = "unexpected body: $body"
            Write-Host "[$Label] attempt $attempt - $lastInfo"
            Start-Sleep -Seconds 20
            continue
        }

        # Success body -> validate the identity actually used. A wrong identity is terminal
        # (retrying will not change it), so assert and throw rather than loop.
        $appId = if ($body -match 'appid=([^;]+)') { $Matches[1].Trim() } else { '' }
        $aud   = if ($body -match 'aud=(\S+)')     { $Matches[1].Trim() } else { '' }

        if ([string]::IsNullOrWhiteSpace($appId)) {
            throw "[$Label] Response did not include a token appid. Body: $body"
        }
        if (-not [string]::IsNullOrWhiteSpace($ExpectedAppId) -and ($appId -ne $ExpectedAppId)) {
            throw "[$Label] Wrong managed identity: token appid '$appId' != expected '$ExpectedAppId'. Body: $body"
        }
        if (-not [string]::IsNullOrWhiteSpace($ForbiddenAppId) -and ($appId -eq $ForbiddenAppId)) {
            throw "[$Label] Managed identity selector ignored: token appid '$appId' must not equal '$ForbiddenAppId'. Body: $body"
        }
        if ([string]::IsNullOrWhiteSpace($aud)) {
            # aud format is resource-dependent (a resource URI or the resource app-id GUID), so we
            # only assert a token audience is present; the appid check above verifies the identity.
            throw "[$Label] Response did not include a token audience. Body: $body"
        }

        Write-Host "[$Label] Response: $body"
        Write-Host "[$Label] OK (appid=$appId, aud=$aud)"
        return
    }

    throw "[$Label] All attempts to verify the protected endpoint failed. Last: $lastInfo"
}

# 0) Easy Auth must challenge unauthenticated callers (verifies the endpoint is actually protected).
Assert-EasyAuthChallenge -Url $baseUrl

# 1) System-assigned MI: expect a real identity that is NOT the user-assigned one.
Invoke-MiEndpoint -Label 'SAMI' -Url $baseUrl -ForbiddenAppId $UserAssignedClientId

# 2) User-assigned MI: the token's appid MUST be the requested user-assigned client id
#    (catches a silent fallback to the system-assigned identity).
if (-not [string]::IsNullOrWhiteSpace($UserAssignedClientId)) {
    Invoke-MiEndpoint -Label 'UAMI' -Url ($baseUrl + '&userAssignedId=' + $UserAssignedClientId) -ExpectedAppId $UserAssignedClientId
}
else {
    Write-Host 'UserAssignedClientId not provided; skipping user-assigned managed identity check.'
}

Write-Host 'All managed identity checks passed.'
