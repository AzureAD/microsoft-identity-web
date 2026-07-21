<#
.SYNOPSIS
    Deploys a prebuilt package to an Azure App Service using the publish profile (Kudu Zip Deploy).

.DESCRIPTION
    Reads the web app publish profile XML from the PUBLISH_PROFILE_XML environment variable
    (sourced from Key Vault), extracts the SCM basic-auth credentials from the MSDeploy entry,
    and POSTs the zip to the Kudu '/api/zipdeploy' endpoint. No ARM service connection is required
    because the publish profile carries its own SCM credentials.

.PARAMETER ZipPath
    Full path of the deployment zip to upload.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)] [string] $ZipPath,
    [string] $ExpectedWebAppName
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path -Path $ZipPath)) {
    throw "Deployment zip not found: $ZipPath"
}

$profileXml = $env:PUBLISH_PROFILE_XML
if ([string]::IsNullOrWhiteSpace($profileXml)) {
    throw "The 'PUBLISH_PROFILE_XML' environment variable is empty. Map the publish-profile Key Vault secret to it via the task 'env:' block."
}

[xml] $publishData = $profileXml
$msDeployProfile = $publishData.publishData.publishProfile |
    Where-Object { $_.publishMethod -eq 'MSDeploy' } |
    Select-Object -First 1

if ($null -eq $msDeployProfile) {
    throw 'No MSDeploy entry found in the publish profile.'
}

$scmHost = ($msDeployProfile.publishUrl -split ':')[0]

# Guard against a mis-scoped publish-profile secret deploying to the wrong App Service: the SCM
# host must belong to the web app the pipeline intends to test.
if (-not [string]::IsNullOrWhiteSpace($ExpectedWebAppName)) {
    if ($scmHost -notmatch "^$([regex]::Escape($ExpectedWebAppName))\.") {
        throw "Publish profile SCM host '$scmHost' does not match expected web app '$ExpectedWebAppName'. Aborting to avoid deploying to the wrong App Service."
    }
    Write-Host "Publish profile SCM host '$scmHost' matches expected web app '$ExpectedWebAppName'."
}

$authHeader = [Convert]::ToBase64String(
    [System.Text.Encoding]::ASCII.GetBytes("$($msDeployProfile.userName):$($msDeployProfile.userPWD)"))
$zipDeployUri = "https://$scmHost/api/zipdeploy"

Write-Host "Deploying $((Get-Item $ZipPath).Length) bytes to $zipDeployUri"
Invoke-RestMethod -Uri $zipDeployUri -Method Post -InFile $ZipPath -ContentType 'application/zip' `
    -Headers @{ Authorization = "Basic $authHeader" } -TimeoutSec 300 | Out-Null
Write-Host 'Deployment complete.'
