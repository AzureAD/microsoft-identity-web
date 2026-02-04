<#
.SYNOPSIS
    Checks for broken internal links in markdown documentation files.

.DESCRIPTION
    This script scans all markdown (.md) files in a directory and its subdirectories,
    extracts internal links, and verifies that the target files exist.
    External links (http/https), anchor-only links (#), and mailto links are skipped.

.PARAMETER Path
    The root directory to scan for markdown files. Defaults to the script's parent directory.

.PARAMETER IncludeExternal
    If specified, also checks external HTTP/HTTPS links for validity (slower).

.PARAMETER OutputFormat
    Output format: 'Table' (default), 'List', 'Json', or 'Csv'

.EXAMPLE
    .\Check-BrokenLinks.ps1
    Scans the docs folder for broken links and displays results in a table.

.EXAMPLE
    .\Check-BrokenLinks.ps1 -Path "C:\MyDocs" -OutputFormat Json
    Scans a custom path and outputs results as JSON.

.EXAMPLE
    .\Check-BrokenLinks.ps1 | Export-Csv -Path "broken-links.csv" -NoTypeInformation
    Exports broken links to a CSV file.
#>

[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [string]$Path = (Split-Path $PSScriptRoot -Parent),

    [switch]$IncludeExternal,

    [ValidateSet('Table', 'List', 'Json', 'Csv')]
    [string]$OutputFormat = 'Table'
)

$ErrorActionPreference = 'Stop'

function Test-MarkdownLink {
    param(
        [string]$SourceFile,
        [string]$LinkTarget
    )

    # Skip external links, anchors, and mailto
    if ($LinkTarget -match '^https?://' -or 
        $LinkTarget -match '^#' -or 
        $LinkTarget -match '^mailto:') {
        return $null
    }

    # Handle anchor links (file.md#section)
    $targetPath = ($LinkTarget -split '#')[0]
    
    # Skip empty paths (pure anchor links handled above)
    if (-not $targetPath.Trim()) {
        return $null
    }

    # Resolve the full path relative to the source file
    $sourceDir = Split-Path $SourceFile -Parent
    $fullPath = Join-Path $sourceDir $targetPath
    
    # Normalize the path
    try {
        $normalizedPath = [System.IO.Path]::GetFullPath($fullPath)
    }
    catch {
        return [PSCustomObject]@{
            SourceFile = $SourceFile
            LinkTarget = $LinkTarget
            Status     = 'Invalid Path'
            Exists     = $false
        }
    }

    # Check if file/directory exists
    $exists = Test-Path $normalizedPath

    if (-not $exists) {
        return [PSCustomObject]@{
            SourceFile = $SourceFile
            LinkTarget = $LinkTarget
            Status     = 'Not Found'
            Exists     = $false
        }
    }

    return $null
}

function Test-ExternalLink {
    param([string]$Url)
    
    try {
        $response = Invoke-WebRequest -Uri $Url -Method Head -TimeoutSec 10 -UseBasicParsing -ErrorAction Stop
        return $response.StatusCode -eq 200
    }
    catch {
        return $false
    }
}

# Main script
Write-Host "`n📂 Scanning for broken links in: $Path`n" -ForegroundColor Cyan

$brokenLinks = @()
$fileCount = 0
$linkCount = 0

# Get all markdown files
$mdFiles = Get-ChildItem -Path $Path -Recurse -Filter "*.md" -File

foreach ($file in $mdFiles) {
    $fileCount++
    $content = Get-Content $file.FullName -Raw -ErrorAction SilentlyContinue
    
    if (-not $content) { continue }

    # Extract all markdown links: [text](target)
    $linkMatches = [regex]::Matches($content, '\[([^\]]+)\]\(([^)]+)\)')
    
    foreach ($match in $linkMatches) {
        $linkText = $match.Groups[1].Value
        $linkTarget = $match.Groups[2].Value
        $linkCount++

        # Check internal links
        $result = Test-MarkdownLink -SourceFile $file.FullName -LinkTarget $linkTarget
        
        if ($result) {
            $result | Add-Member -NotePropertyName 'LinkText' -NotePropertyValue $linkText
            $result | Add-Member -NotePropertyName 'RelativeSource' -NotePropertyValue ($file.FullName -replace [regex]::Escape("$Path\"), '')
            $brokenLinks += $result
        }

        # Optionally check external links
        if ($IncludeExternal -and $linkTarget -match '^https?://') {
            Write-Host "  Checking external: $linkTarget" -ForegroundColor Gray
            if (-not (Test-ExternalLink -Url $linkTarget)) {
                $brokenLinks += [PSCustomObject]@{
                    SourceFile     = $file.FullName
                    RelativeSource = ($file.FullName -replace [regex]::Escape("$Path\"), '')
                    LinkTarget     = $linkTarget
                    LinkText       = $linkText
                    Status         = 'External Link Failed'
                    Exists         = $false
                }
            }
        }
    }
}

# Output results
Write-Host "📊 Scan complete!" -ForegroundColor Green
Write-Host "   Files scanned: $fileCount"
Write-Host "   Links checked: $linkCount"
Write-Host "   Broken links:  $($brokenLinks.Count)`n" -ForegroundColor $(if ($brokenLinks.Count -gt 0) { 'Yellow' } else { 'Green' })

if ($brokenLinks.Count -eq 0) {
    Write-Host "✅ No broken links found!" -ForegroundColor Green
    return
}

# Format output
$output = $brokenLinks | Select-Object RelativeSource, LinkTarget, Status | Sort-Object RelativeSource, LinkTarget

switch ($OutputFormat) {
    'Table' {
        $output | Format-Table -AutoSize -Wrap
    }
    'List' {
        $output | Format-List
    }
    'Json' {
        $output | ConvertTo-Json -Depth 3
    }
    'Csv' {
        $output | ConvertTo-Csv -NoTypeInformation
    }
}

# Group by missing target pattern for summary
Write-Host "`n📋 Summary by missing path pattern:" -ForegroundColor Cyan
$brokenLinks | 
    Group-Object { 
        $target = $_.LinkTarget
        if ($target -match '^\.\./scenarios/') { 'scenarios/*' }
        elseif ($target -match '^\.\./deployment/') { 'deployment/*' }
        elseif ($target -match '^\.\./migration/') { 'migration/*' }
        elseif ($target -match '^\.\./packages/') { 'packages/*' }
        elseif ($target -match 'README\.md$') { '*/README.md references' }
        elseif ($target -match '^\./') { 'Same-directory files' }
        else { 'Other' }
    } | 
    Sort-Object Count -Descending |
    ForEach-Object {
        Write-Host "   $($_.Count) - $($_.Name)" -ForegroundColor Yellow
    }

Write-Host ""

# Return the broken links for pipeline usage
return $output
