<#
.SYNOPSIS
    Assembles a classic ASP.NET (.NET Framework) deployment package for Azure App Service.

.DESCRIPTION
    Regenerates the Web.config <assemblyBinding> redirects from the actual built assemblies
    (so the Microsoft.Identity.Web net472 dependency graph loads on .NET Framework regardless of
    dependency version drift), then assembles the App Service layout: Global.asax, the rewritten
    Web.config, and appsettings.json at the package root, with all built assemblies under /bin.
    Produces a zip ready for Kudu zip deploy.

.PARAMETER ProjectDirectory
    The web app project directory (contains Global.asax, Web.config, appsettings.json).

.PARAMETER BuildOutputDirectory
    The build output directory containing the compiled assemblies (e.g. bin\Release\net48).

.PARAMETER ZipPath
    Full path of the deployment zip to create.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)] [string] $ProjectDirectory,
    [Parameter(Mandatory = $true)] [string] $BuildOutputDirectory,
    [Parameter(Mandatory = $true)] [string] $ZipPath
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path -Path $BuildOutputDirectory)) {
    throw "Build output directory not found: $BuildOutputDirectory"
}

# 1) Generate <dependentAssembly> redirects from the strong-named assemblies actually built.
$dependentAssemblies = New-Object System.Text.StringBuilder
$redirectCount = 0
foreach ($dll in Get-ChildItem -Path $BuildOutputDirectory -Filter *.dll | Sort-Object Name) {
    try {
        $assemblyName = [System.Reflection.AssemblyName]::GetAssemblyName($dll.FullName)
        $publicKeyToken = $assemblyName.GetPublicKeyToken()
        if (-not $publicKeyToken -or $publicKeyToken.Length -eq 0) {
            continue  # skip non-strong-named assemblies (no redirect needed)
        }
        $token = ([BitConverter]::ToString($publicKeyToken) -replace '-', '').ToLowerInvariant()
        $version = $assemblyName.Version.ToString()
        [void]$dependentAssemblies.Append(
            "<dependentAssembly><assemblyIdentity name=`"$($assemblyName.Name)`" publicKeyToken=`"$token`" culture=`"neutral`" />" +
            "<bindingRedirect oldVersion=`"0.0.0.0-$version`" newVersion=`"$version`" /></dependentAssembly>")
        $redirectCount++
    }
    catch {
        Write-Host "Skipping assembly (could not read name): $($dll.Name) - $($_.Exception.Message)"
    }
}

# 2) Rewrite Web.config <runtime><assemblyBinding> with the generated redirects.
$webConfigPath = Join-Path $ProjectDirectory 'Web.config'
if (-not (Test-Path -Path $webConfigPath)) {
    throw "Web.config not found: $webConfigPath"
}
[xml]$webConfig = Get-Content -Path $webConfigPath -Raw
$runtimeNode = $webConfig.configuration.runtime
if ($null -eq $runtimeNode) {
    $runtimeNode = $webConfig.CreateElement('runtime')
    [void]$webConfig.configuration.AppendChild($runtimeNode)
}
# Replace ONLY the <assemblyBinding> element, preserving any other <runtime> settings
# (e.g. AppContext switches, GC or assembly-probing configuration) that may be added later.
$existingBindings = @($runtimeNode.ChildNodes | Where-Object { $_.LocalName -eq 'assemblyBinding' })
foreach ($binding in $existingBindings) { [void]$runtimeNode.RemoveChild($binding) }
$fragment = $webConfig.CreateDocumentFragment()
$fragment.InnerXml = "<assemblyBinding xmlns=`"urn:schemas-microsoft-com:asm.v1`">$($dependentAssemblies.ToString())</assemblyBinding>"
[void]$runtimeNode.AppendChild($fragment)

# 3) Assemble the classic ASP.NET App Service layout (root: Global.asax/Web.config/appsettings.json, /bin: assemblies).
$stagingDirectory = Join-Path ([System.IO.Path]::GetTempPath()) ("netfx-webapp-" + [guid]::NewGuid().ToString('N'))
$binStaging = Join-Path $stagingDirectory 'bin'
New-Item -ItemType Directory -Force -Path $binStaging | Out-Null
Copy-Item -Path (Join-Path $BuildOutputDirectory '*') -Destination $binStaging -Recurse -Force
Copy-Item -Path (Join-Path $ProjectDirectory 'Global.asax') -Destination $stagingDirectory -Force
Copy-Item -Path (Join-Path $ProjectDirectory 'appsettings.json') -Destination $stagingDirectory -Force
$webConfig.Save((Join-Path $stagingDirectory 'Web.config'))

if (Test-Path -Path $ZipPath) { Remove-Item -Path $ZipPath -Force }
Compress-Archive -Path (Join-Path $stagingDirectory '*') -DestinationPath $ZipPath -Force
Remove-Item -Path $stagingDirectory -Recurse -Force -ErrorAction SilentlyContinue

Write-Host "Packaged $((Get-Item $ZipPath).Length) bytes to $ZipPath (generated $redirectCount binding redirects)."
