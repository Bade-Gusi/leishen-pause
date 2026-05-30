#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Bump version number across the entire project.
.DESCRIPTION
    Updates version in VersionInfo.cs, version.txt, and CHANGELOG.md.
    Supports patch, minor, and major bumps following semver.
.PARAMETER Type
    Bump type: patch, minor, or major. Default: patch.
.PARAMETER DryRun
    Show what would change without writing files.
.EXAMPLE
    ./scripts/update-version.ps1 -Type minor   # 2.1.0 -> 2.2.0
    ./scripts/update-version.ps1 -Type patch    # 2.1.0 -> 2.1.1
    ./scripts/update-version.ps1 -Type major    # 2.1.0 -> 3.0.0
#>
param(
    [ValidateSet('patch', 'minor', 'major')]
    [string]$Type = 'patch',
    [switch]$DryRun
)

$repoRoot = Split-Path -Parent $PSScriptRoot
$versionFile = Join-Path $repoRoot "leishen" "VersionInfo.cs"
$versionTxt = Join-Path $repoRoot "version.txt"
$changelog = Join-Path $repoRoot "CHANGELOG.md"

# --- Read current version ---
$content = Get-Content $versionFile -Raw
if ($content -match 'public const string Version = "(?<ver>[^"]+)";') {
    $current = $Matches['ver']
} else {
    Write-Error "Cannot parse version from VersionInfo.cs"
    exit 1
}

Write-Host "Current version: $current" -ForegroundColor Cyan

# --- Bump ---
$parts = $current.Split('.')
$major = [int]$parts[0]
$minor = [int]$parts[1]
$patch = [int]$parts[2]

switch ($Type) {
    'major' { $major++; $minor = 0; $patch = 0 }
    'minor' { $minor++; $patch = 0 }
    'patch' { $patch++ }
}

$newVersion = "$major.$minor.$patch"
$newTag = "v$newVersion"
$today = Get-Date -Format "yyyy-MM-dd"

Write-Host "New version:     $newVersion ($Type bump)" -ForegroundColor Green

if ($DryRun) {
    Write-Host "`n[Dry Run] No files were modified." -ForegroundColor Yellow
    exit 0
}

# --- Update VersionInfo.cs ---
$content = $content -replace 'public const string Version = "[^"]+";', "public const string Version = `"$newVersion`";"
Set-Content $versionFile $content -Encoding utf8
Write-Host "  ✓ Updated VersionInfo.cs" -ForegroundColor Green

# --- Update version.txt ---
Set-Content $versionTxt $newVersion -Encoding utf8 -NoNewline
Write-Host "  ✓ Updated version.txt" -ForegroundColor Green

# --- Add CHANGELOG section header (content to be filled by developer) ---
$changelogContent = Get-Content $changelog -Raw
$newSection = @"

## $newTag ($today)

### ✨ 新功能
-

### 🔧 修复
-

### ⚡ 优化
-

---

$changelogContent
"@
Set-Content $changelog $newSection -Encoding utf8
Write-Host "  ✓ Added CHANGELOG section for $newTag" -ForegroundColor Green

Write-Host "`n✅ Version bumped $current → $newVersion" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "  1. Edit CHANGELOG.md to fill in release notes"
Write-Host "  2. Commit: git add -A && git commit -m ""chore: bump v$current → v$newVersion"""
Write-Host "  3. Tag:    git tag v$newVersion"
Write-Host "  4. Push:   git push && git push --tags"
