param(
    [Parameter(Mandatory = $true)]
    [string] $MsalSourcePath
)

$ErrorActionPreference = 'Stop'
$expectedRevision = '322bafb15cff4752d79d055266ea90a5c1544793'
$revision = git -C $MsalSourcePath rev-parse HEAD
if ($LASTEXITCODE -ne 0 -or $revision -ne $expectedRevision)
{
    throw "MsalSourcePath must point to PR 6229 revision $expectedRevision."
}

$packagePath = Join-Path $PSScriptRoot 'bin\local-packages'
$msalProject = Join-Path $MsalSourcePath 'src\client\Microsoft.Identity.Client\Microsoft.Identity.Client.csproj'
$testProject = Join-Path $PSScriptRoot 'Microsoft.Identity.Web.TokenCache.SnapshotPoc.csproj'

Push-Location $PSScriptRoot
try
{
    # A cache-pipeline POC, not a trimming/AOT validation run.
    dotnet pack $msalProject -c Release -p:TargetFrameworks=net8.0 -p:IsAotCompatible=false -p:MicrosoftIdentityClientVersion=4.90.2-snapshot6229 -o $packagePath --verbosity quiet
    if ($LASTEXITCODE -ne 0) { throw 'Building the MSAL snapshot package failed.' }

    dotnet test $testProject -c Release -p:TargetFrameworks=net8.0 -p:IsAotCompatible=false "-p:RestoreAdditionalProjectSources=$packagePath" --logger 'console;verbosity=normal'
    if ($LASTEXITCODE -ne 0) { throw 'Snapshot cache-pipeline tests failed.' }
}
finally
{
    Pop-Location
}
